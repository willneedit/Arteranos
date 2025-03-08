/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Ipfs;
using Ipfs.Unity;
using System.IO;
using Arteranos.Core;
using System.Threading;
using System;
using System.Threading.Tasks;
using Arteranos.Core.Cryptography;
using System.Linq;
using Ipfs.Cryptography.Proto;
using Ipfs.Http;
using System.Text;
using IPAddress = System.Net.IPAddress;
using Arteranos.Core.Operations;
using TaskScheduler = Arteranos.Core.TaskScheduler;
using Ipfs.CoreApi;

using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Collections.Concurrent;


namespace Arteranos.Services
{
    public class IPFSService : MonoBehaviour, IIPFSService
    {
        public IpfsClientEx Ipfs { get => ipfs; }
        public Peer Self { get => self; }
        public SignKey ServerKeyPair { get => serverKeyPair; }
        public Cid IdentifyCid { get; protected set; }
        public Cid CurrentSDCid { get; protected set; } = null;

        public bool EnableUploadDefaultAvatars = true;

        public static string CachedPTOSNotice { get; private set; } = null;

        private const string ANNOUNCERTOPIC = "/X-Arteranos";
        private readonly List<string> ARTERANOSBOOTSTRAP = new() 
        { 
            "/dns4/prime.arteranos.ddnss.eu/tcp/4001/p2p/12D3KooWA1qSpKLjHqWemSW1gU5wJQdP8piBbDSQi6EEgqPVVkyc" 
        };
        private const string PATH_USER_PRIVACY_NOTICE = "Privacy_TOS_Notice.md";

        private bool ForceIPFSShutdown = true;

        private IpfsClientEx ipfs = null;
        private Peer self = null;
        private SignKey serverKeyPair = null;

        private DateTime last = DateTime.MinValue;

        private string versionString = null;

        private CancellationTokenSource cts = null;

        // ---------------------------------------------------------------
        #region Start & Stop

        public void Awake()
        {
            G.IPFSService = this;
        }

        public void Start()
        {
            IEnumerator InitializeIPFSCoroutine()
            {
                IEnumerator EnsureIPFSStarted()
                {
                    IEnumerator StartupIPFSExeCoroutine()
                    {
                        IPFSDaemonConnection.Status res = IPFSDaemonConnection.CheckRepository(false);

                        if(res != IPFSDaemonConnection.Status.OK)
                        {
                            G.TransitionProgress?.OnProgressChanged(0.15f, "Initializing IPFS repository");
                            res = IPFSDaemonConnection.CheckRepository(true);
                        }

                        if (res != IPFSDaemonConnection.Status.OK)
                        {
                            G.TransitionProgress?.OnProgressChanged(0.00f, "FAILED IPFS REPO INIT");
                            Debug.LogError(@"
**************************************************************************
* !!! Failed to initialize the IPFS repository                       !!! *
* Possible causes are....                                                *
*  * Corrupted user data                                                 *
*  * Corrupted program installation                                      *
**************************************************************************
");
                            yield return new WaitForSeconds(10);
                            SettingsManager.Quit();
                            yield break;
                        }

                        G.TransitionProgress?.OnProgressChanged(0.19f, "Setting bootstrap");

                        foreach(string entry in ARTERANOSBOOTSTRAP)
                            res = IPFSDaemonConnection.RunDaemonCommand($"bootstrap add {entry}", true);

                        G.TransitionProgress?.OnProgressChanged(0.20f, "Starting IPFS backend");

                        res = IPFSDaemonConnection.StartDaemon(false);

                        if(res != IPFSDaemonConnection.Status.OK)
                        {
                            G.TransitionProgress?.OnProgressChanged(0.00f, "FAILED TO START DAEMON");
                            Debug.LogError(@"
**************************************************************************
* !!! Failed to start the IPFS daemon                                !!! *
* Possible causes are....                                                *
*  * Corrupted program installation                                      *
**************************************************************************
");
                            yield return new WaitForSeconds(10);
                            SettingsManager.Quit();
                            yield break;
                        }
                    }

                    IPFSDaemonConnection.Status res = IPFSDaemonConnection.Status.CommandFailed;

                    int port = IPFSDaemonConnection.GetAPIPort();

                    if (port >= 0)
                    {
                        Debug.Log($"Present configuration says API port {port}");
                        yield return Asyncs.Async2Coroutine(() => IPFSDaemonConnection.CheckAPIConnection(1, false), _res => res = _res);
                    }
                    else
                        Debug.Log("No configuration present, starting from scratch.");

                    if (res != IPFSDaemonConnection.Status.OK)
                    {
                        yield return StartupIPFSExeCoroutine();
                    }
                }

                try
                {
                    versionString = Core.Version.Load().MMP;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Internal error: Missing version information - use Arteranos->Build->Update version");
                    Debug.LogException(ex);
                }

                IPFSDaemonConnection.IPFSAccessible();

                // Find and start the IPFS Server. If it's not already running.
                yield return EnsureIPFSStarted();

                // Keep the IPFS synced - it needs the IPFS node alive.
                yield return Asyncs.Async2Coroutine(InitializeIPFS);

                yield return new WaitForSeconds(1);

                // Default avatars
                yield return UploadDefaultAvatars();

                // Start to emit the server description.
                StartCoroutine(EmitServerDescriptionCoroutine());

                // Start to emit the server online data.
                StartCoroutine(EmitServerOnlineDataCoroutine());

                // Start the subscriber loop
                StartCoroutine(SubscriberCoroutine());

                // Start the IPNS publisher loop
                StartCoroutine(IPNSPublisherCoroutine());

                // Ready to proceed, past the point we can manually shut down the IPFS backend
                ForceIPFSShutdown = false;
            }

            IEnumerator SubscriberCoroutine()
            {
                Debug.Log("Subscribing Arteranos PubSub channel");

                while (true)
                {
                    using CancellationTokenSource cts_sub = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

                    // Truly async.
                    Task t = ipfs.PubSub.SubscribeAsync(ANNOUNCERTOPIC, ParseArteranosMessage, cts_sub.Token);

                    yield return new WaitForSeconds(5);

                    if (t.IsCompleted)
                        yield return new WaitUntil(() => false); // NOTREACHED, until Service is destroyed

                    Debug.LogWarning("Subscription task seems to be stuck, retrying...");
                    cts_sub.Cancel();
                    yield return new WaitForSeconds(1);
                }
            }

            IEnumerator IPNSPublisherCoroutine()
            {
                Cid PreviousSDCid = null;
                DateTime publishTime = DateTime.MinValue;

                while(true)
                {
                    // Valid server description, and
                    //  - differs from the last one or
                    //  - older than one day
                    if(CurrentSDCid != null && 
                        (PreviousSDCid != CurrentSDCid 
                        || publishTime < (DateTime.UtcNow - TimeSpan.FromDays(1))))
                    {
                        using CancellationTokenSource cts_pub = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

                        Debug.Log("Publishing new Server Description to IPNS...");

                        Task t = ipfs.Name.PublishAsync(CurrentSDCid, cancel: cts_pub.Token);

                        Debug.Log("... IPNS publish finished.");

                        PreviousSDCid = CurrentSDCid;
                        publishTime = DateTime.UtcNow;
                    }

                    yield return new WaitForSeconds(60);
                }
            }
            
            async Task InitializeIPFS()
            {
                ipfs = null;

                IPFSDaemonConnection.Status status = await IPFSDaemonConnection.EvadePortSquatters();

                if(status != IPFSDaemonConnection.Status.OK)
                {
                    G.TransitionProgress?.OnProgressChanged(0.00f, "CANNOT USE IPFS INTERFACE");
                    Debug.LogError(@"
**************************************************************************
* !!! Cannot use IPFS Interface                                      !!! *
* Possible causes are....                                                *
*  * Corrupted install                                                   *
*  * System with MANY running services (port exhaustion)                 *
**************************************************************************
");
                    SettingsManager.Quit();
                    throw new ApplicationException();
                }

                self = IPFSDaemonConnection.Self;
                ipfs = IPFSDaemonConnection.Ipfs;

                // If it doesn't exist, write down the template in the config directory.
                if (!ConfigUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
                {
                    ConfigUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, SettingsManager.DefaultTOStext);
                    Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
                }

                CachedPTOSNotice = ConfigUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

                G.TransitionProgress?.OnProgressChanged(0.30f, "Announcing its service");

                StringBuilder sb = new();
                sb.Append("Arteranos Server, built by willneedit\n");
                sb.Append(Core.Version.VERSION_MIN);

                // Put up the identifier file
                if (G.Server.Public)
                {
                    IdentifyCid = (await ipfs.FileSystem.AddTextAsync(sb.ToString())).Id;
                }
                else
                {
                    // rm -f the identifier file to not to show the local node in other's FindProviders()
                    AddFileOptions ao = new() { OnlyHash = true };
                    IdentifyCid = (await ipfs.FileSystem.AddTextAsync(sb.ToString(), ao)).Id;

                    try
                    {
                        await ipfs.Pin.RemoveAsync(IdentifyCid).ConfigureAwait(false);
                        await ipfs.Block.RemoveAsync(IdentifyCid, true).ConfigureAwait(false);
                    }
                    catch { }
                }
                Debug.Log("---- IPFS Backend init complete ----\n" +
                    $"IPFS Node's ID\n" +
                    $"   {self.Id}\n"
                    + $"Discovery identifier file's CID\n" +
                    $"   {IdentifyCid}\n"
                    );

                // Reuse the IPFS peer key for the multiplayer server to ensure its association
                serverKeyPair = IPFSDaemonConnection.ServerKeyPair;
                G.Server.UpdateServerKey(serverKeyPair);

                G.TransitionProgress?.OnProgressChanged(0.40f, "Connected to IPFS");
            }

            ipfs = null;
            IdentifyCid = null;

            last = DateTime.MinValue;
            cts = new();

            StartCoroutine(InitializeIPFSCoroutine());

        }

        public void OnDisable()
        {
            // await FlipServerDescription();

            cts?.Cancel();

            // If we're started the backend on our own, shut it down, too.
            if (ForceIPFSShutdown)
            {
                Debug.Log("Shutting down the IPFS node, because the service didn't completely start.");
                if (ipfs != null) IPFSDaemonConnection.StopDaemon();
            }
            else if (G.Server.ShutdownIPFS)
            {
                Debug.Log("Shutting down the IPFS node.");
                if (ipfs != null) IPFSDaemonConnection.StopDaemon();
            }
            else
                Debug.Log("NOT Shutting down the IPFS node, because you want it to remain running.");

            cts?.Dispose();
        }

        private IEnumerator UploadDefaultAvatars()
        {
            G.TransitionProgress?.OnProgressChanged(0.50f, "Uploading default resources");

            Cid cid = null;
            IEnumerator UploadAvatar(string resourceMA)
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(resourceMA, false); // Plsin GLB files

                yield return ao.ExecuteCoroutine(co);

                cid = AssetUploader.GetUploadedCid(co);

            }

            if(EnableUploadDefaultAvatars)
            {
                yield return UploadAvatar("resource:///Avatar/6394c1e69ef842b3a5112221.glb");
                G.DefaultAvatar.Male = cid;
                yield return UploadAvatar("resource:///Avatar/63c26702e5b9a435587fba51.glb");
                G.DefaultAvatar.Female = cid;
            }
        }

#endregion
        // ---------------------------------------------------------------
        #region Server information publishing

        public IEnumerator EmitServerDescriptionCoroutine()
        {
            while (true)
            {
                yield return Asyncs.Async2Coroutine(FlipServerDescription);

                // One day
                yield return new WaitForSeconds(24 * 60 * 60);
            }
            // NOTREACHED
        }

        private IEnumerator EmitServerOnlineDataCoroutine()
        {
            bool saidOnline = false;

            while (true)
            {
                if (G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Server ||
                    G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Host)
                {
                    saidOnline = true;
                    if(G.Server.Public) FlipServerOnlineData_();
                }
                else if(saidOnline)
                {
                    // First time after being online, wave the community goodbye.
                    saidOnline = false;
                    SendServerGoodbye_();
                }

                yield return new WaitForSeconds(G.Server.AnnounceRefreshTime);
            }
            // NOTREACHED
        }

        public async Task FlipServerDescription()
        {
            async Task<IFileSystemNode> CreateSDFile()
            {
                Server server = G.Server;
                IEnumerable<string> q = from entry in G.ServerUsers.Base
                                        where UserState.IsSAdmin(entry.userState)
                                        select ((string)entry.userID);

                ServerDescription ServerDescription = new()
                {
                    Name = server.Name,
                    ServerPort = server.ServerPort,
                    Description = server.Description,
                    ServerIcon = server.ServerIcon,
                    Version = versionString,
                    MinVersion = Core.Version.VERSION_MIN,
                    Permissions = server.Permissions,
                    PrivacyTOSNotice = CachedPTOSNotice,
                    AdminNames = q.ToArray(),
                    PeerID = self.Id.ToString(),
                    LastModified = server.ConfigLastChanged
                };

                using MemoryStream ms = new();
                ServerDescription.Serialize(serverKeyPair, ms);
                ms.Position = 0;
                return await ipfs.FileSystem.AddAsync(ms, "ServerDescription").ConfigureAwait(false);
            }

            async Task<IFileSystemNode> CreateTOSFile()
            {
                using MemoryStream ms = new(Encoding.UTF8.GetBytes(CachedPTOSNotice));
                ms.Position = 0;
                return await ipfs.FileSystem.AddAsync(ms, "Privacy_TOS_Notice.md").ConfigureAwait(!false);
            }

            async Task<IFileSystemNode> CreateLauncherHTMLFile()
            {
                string linkto = $"arteranos://{self.Id}/";
                string html
    = "<html>\n"
    + "<head>\n"
    + "<title>Launch Arteranos connection</title>\n"
    + $"<meta http-equiv=\"refresh\" content=\"0; url={linkto}\" />\n"
    + "</head>\n"
    + "<body>\n"
    + $"Trouble with redirection? <a href=\"{linkto}\">Click here.</a>\n"
    + "</body>\n"
    + "</html>\n";

                using MemoryStream ms = new(Encoding.UTF8.GetBytes(html));
                ms.Position = 0;
                return await ipfs.FileSystem.AddAsync(ms, "launcher.html").ConfigureAwait(!false);
            }

            async Task<IFileSystemNode> CreateServerDescription()
            {
                List<IFileSystemLink> list = new()
                {
                    (await CreateSDFile().ConfigureAwait(false)).ToLink(),
                    (await CreateTOSFile().ConfigureAwait(false)).ToLink(),
                    (await CreateLauncherHTMLFile().ConfigureAwait(false)).ToLink()
                };

                return await CreateDirectory(list).ConfigureAwait(false);
            }

            IFileSystemNode fsn = await CreateServerDescription().ConfigureAwait(false);
            CurrentSDCid = fsn.Id;

// Definitely unnecessary, _for now_. We just need server descriptions if the server itself is online,
// and it's sending online data.
#if false
            if (G.Server.Public)
            {
                Stopwatch sw = Stopwatch.StartNew();
                // Submit the announce in Pubsub as well, for all who's listening
                ServerDescriptionLink sdl = new() { ServerDescriptionCid = CurrentSDCid };
                using MemoryStream ms = new();
                sdl.Serialize(ms);
                ms.Position = 0;

                await ipfs.PubSub.PublishAsync(AnnouncerTopic, ms.ToArray());
                Debug.Log($"Publishing server description via PubSub(): {sw.Elapsed}");
            }
            else
                Debug.Log($"Server description CID would be: {CurrentSDCid}");
#endif

        }

        private void FlipServerOnlineData_()
        {
            // If we're explicitely set the shorter time, we lower the flood throttling, too.
            int floodmark = G.Server.AnnounceRefreshTime < 30 ? G.Server.AnnounceRefreshTime : 30;

            // Flood mitigation
            if (last > DateTime.UtcNow - TimeSpan.FromSeconds(floodmark)) return;
            last = DateTime.UtcNow;

            List<byte[]> UserFingerprints = (from user in G.NetworkStatus.GetOnlineUsers()
                                where user.UserPrivacy != null && user.UserPrivacy.Visibility != Visibility.Invisible
                                select CryptoHelpers.GetFingerprint(user.UserID)).ToList();

            List<IPAddress> addrs = G.NetworkStatus?.IPAddresses;

            if(addrs == null)
            {
                Debug.LogWarning("No IP addresses yet, skipping Server Online data for this turn");
                return;
            }

            List<string> IPAddresses = new();
            foreach (IPAddress entry in addrs) if(entry != null) IPAddresses.Add(entry.ToString());

            ServerOnlineData sod = new()
            {
                CurrentWorldCid = G.World.Cid,
                CurrentWorldName = G.World.Name,
                UserFingerprints = UserFingerprints,
                ServerDescriptionCid = CurrentSDCid,
                // LastOnline = last, // Not serialized - set on receive
                OnlineLevel = G.NetworkStatus.GetOnlineLevel(),
                IPAddresses = IPAddresses,
                Timestamp = DateTime.UtcNow,
                Firewalled = G.NetworkStatus.GetConnectivityLevel() != ConnectivityLevel.Unrestricted
            };

            using MemoryStream ms = new();
            sod.Serialize(ms);

            // Announce the server online data, too - fire and forget.
            _ = ipfs.PubSub.PublishAsync(ANNOUNCERTOPIC, ms.ToArray());
        }

        private void SendServerGoodbye_()
        {
            ServerOnlineData sod = new()
            {
                CurrentWorldCid = null,
                CurrentWorldName = "Offline",
                UserFingerprints = null,
                ServerDescriptionCid = CurrentSDCid,
                OnlineLevel = OnlineLevel.Offline,
                IPAddresses = null,
                Timestamp = DateTime.UtcNow,
            };
            using MemoryStream ms = new();
            sod.Serialize(ms);

            // Just wave a goodbye.
            _ = ipfs.PubSub.PublishAsync(ANNOUNCERTOPIC, ms.ToArray());
        }

#endregion
        // ---------------------------------------------------------------
        #region Peer communication and data exchange

        public void PostMessageTo(MultiHash peerID, byte[] message)
        {
            _ = ipfs.PubSub.PublishAsync(ANNOUNCERTOPIC, message);
        }

        private void ParseArteranosMessage(IPublishedMessage message)
        {
            MultiHash SenderPeerID = message.Sender.Id;

            // Pubsub MAY loop back the messages, but no need.
            if (SenderPeerID == self.Id) return;

            try
            {
                using MemoryStream ms = new(message.DataBytes);
                PeerMessage pm = PeerMessage.Deserialize(ms);

                if(pm is IDirectedPeerMessage dm)
                {
                    if (dm.ToPeerID != self.Id.ToString())
                    {
                        // Debug.Log("Discarding a message directed to another peer");
                        return;
                    }
                    // else Debug.Log($"Directed message accepoted: {dm.ToPeerID}");
                }

                // Maybe obsolete.
                if (pm is ServerDescriptionLink sdl) // Too big for pubsub, this is only a link
                {
                    // Debug.Log($"New server description from {SenderPeerID}");
                    DownloadServerDescription(SenderPeerID, sdl.ServerDescriptionCid);
                }
                else if (pm is ServerOnlineData sod) // As-is.
                {
                    // Debug.Log($"New server online data from {SenderPeerID}");
                    DownloadServerDescription(SenderPeerID, sod.ServerDescriptionCid);
                    sod.LastOnline = DateTime.UtcNow; // Not serialized
                    sod.DBInsert(SenderPeerID.ToString());
                }
                else if (pm is NatPunchRequestData nprd)
                {
                    InitiateNatPunch(nprd);
                }
                else
                    Debug.LogWarning($"Discarding unknown message from {SenderPeerID}");
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        private struct SDEntry
        {
            public string path;         // CID of server description
            public DateTime lastSeen;   // Last seen on the network
        }

        private readonly ConcurrentDictionary<MultiHash, SDEntry> KnownServerDescriptions = new();

        // Server description are generated by the Server Discovery
        private void DownloadServerDescription(MultiHash SenderPeerID, string sddPath)
        {

            DateTime now = DateTime.UtcNow;

            async Task DownloadSDAsync(MultiHash SenderPeerID, string sddPath)
            {
                try
                {
                    // Mark it now because download in progress.
                    SDEntry newValue = new()
                    {
                        path = sddPath,
                        lastSeen = now
                    };

                    KnownServerDescriptions.AddOrUpdate(SenderPeerID, newValue, (sender, value) => newValue);

                    using CancellationTokenSource cts = new(20000);

                    // Server description is published under the peer ID. (= self)
                    // If it's cached, it's already in the local repo.
                    // And the TTL reduce the traffic.
                    using Stream s = await ipfs.FileSystem.ReadFileAsync(sddPath + "/ServerDescription", cts.Token).ConfigureAwait(false);

                    // TODO Maybe unneccessary?
                    PublicKey pk = PublicKey.FromId(SenderPeerID);
                    ServerDescription sd = ServerDescription.Deserialize(pk, s);

                    sd.LastSeen = now;

                    // Invalid, most probably outdated server. But, it just given a sign of life.
                    if (sd)
                    {
                        sd.DBUpdate();
                        Debug.Log($"Successfully downloaded server description ({sddPath}) from {SenderPeerID}");
                    }
                    else
                        Debug.Log($"Rejecting server description ({sddPath}) from {SenderPeerID}");

                }
                catch (Exception ex)
                {
                    // Debug.LogException(ex);
                    Debug.LogWarning($"Failed downloading server description ({sddPath}) from {SenderPeerID}: {ex.Message}");

                    // Remove the marking, the attempt or result is invalid.
                    KnownServerDescriptions.TryRemove(SenderPeerID, out _);
                }
            }

            Func<Task> MakeDownloadTask(MultiHash SenderPeerID, string sddPath)
            {
                Task a() => DownloadSDAsync(SenderPeerID, sddPath);
                return a;
            }

            // Empty message, maybe just going online.
            if (string.IsNullOrEmpty(sddPath)) return;

            // Already known and saved, Or, attempting to.
            if (KnownServerDescriptions.ContainsKey(SenderPeerID))
            {
                SDEntry entry = KnownServerDescriptions[SenderPeerID];
                if (entry.path == sddPath)
                {
                    // Every thirty minutes, at most, refresh the entry without bothering the IPFS.
                    if (entry.lastSeen <= now - TimeSpan.FromMinutes(30))
                    {
                        ServerDescription sd = ServerDescription.DBLookup(SenderPeerID.ToString());

                        // Maybe already pruned, freshen only existing entries.
                        if (sd != null)
                        {
                            sd.LastSeen = now;
                            sd.DBUpdate();
                        }

                        SDEntry newValue = new()
                        {
                            path = sddPath,
                            lastSeen = now
                        };

                        KnownServerDescriptions.AddOrUpdate(SenderPeerID, newValue, (sender, value) => newValue);
                    }

                    return;
                }
            }

            TaskScheduler.Schedule(MakeDownloadTask(SenderPeerID, sddPath));
        }

        private void InitiateNatPunch(NatPunchRequestData nprd)
        {
            Debug.Log($"Contacting peer wants us to initiate Nat punch, relay={nprd.relayIP}:{nprd.relayPort}, token={nprd.token}");
            G.ConnectionManager.Peer_InitateNatPunch(nprd);
        }

        #endregion
        // ---------------------------------------------------------------
        #region IPFS Lowlevel interface
        public Task PinCid(Cid cid, bool pinned, CancellationToken token = default)
        {
            if (pinned)
                return ipfs.Pin.AddAsync(cid, cancel: token);
            else
                return ipfs.Pin.RemoveAsync(cid, cancel: token);
        }

        public async Task<MemoryStream> ReadIntoMS(string path, Action<long> reportProgress = null, CancellationToken cancel = default)
        {
            MemoryStream ms = new();

            using Stream instr = await ipfs.FileSystem.ReadFileAsync(path, cancel).ConfigureAwait(false);
            byte[] buffer = new byte[128 * 1024];

            // No cancel. We have to do it until the end, else the RPC client would choke.
            long totalBytes = 0;
            long lastreported = 0;
            while (true)
            {
                int n = instr.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;
                totalBytes += n;
                try
                {
                    // Report rate throttling
                    if(lastreported < totalBytes - buffer.Length)
                    {
                        reportProgress?.Invoke(totalBytes);
                        lastreported = totalBytes;
                    }
                }
                catch { } // Whatever may come, the show must go on.
                ms.Write(buffer, 0, n);
            }

            instr.Close();

            // One last report.
            reportProgress?.Invoke(totalBytes);

            ms.Position = 0;
            return ms;
        }

        public async Task<byte[]> ReadBinary(string path, Action<long> reportProgress = null, CancellationToken cancel = default)
        {
            using MemoryStream ms = await ReadIntoMS(path, reportProgress, cancel).ConfigureAwait(false);

            return ms.ToArray();
        }

        public async Task<IEnumerable<Cid>> ListPinned(CancellationToken cancel = default)
            => await Ipfs.Pin.ListAsync(cancel).ConfigureAwait(false);
        public async Task<Stream> ReadFile(string path, CancellationToken cancel = default)
            => await Ipfs.FileSystem.ReadFileAsync(path, cancel).ConfigureAwait(false);

        public async Task<Stream> Get(string path, CancellationToken cancel = default)
            => await Ipfs.FileSystem.GetAsync(path, cancel: cancel).ConfigureAwait(false);
        public async Task<IFileSystemNode> AddStream(Stream stream, string name = "", AddFileOptions options = null, CancellationToken cancel = default)
            => await Ipfs.FileSystem.AddAsync(stream, name, options, cancel).ConfigureAwait(false);
        public async Task<IFileSystemNode> ListFile(string path, CancellationToken cancel = default)
            => await Ipfs.FileSystem.ListAsync(path, cancel).ConfigureAwait(false);
        public async Task<IFileSystemNode> AddDirectory(string path, bool recursive = true, AddFileOptions options = null, CancellationToken cancel = default)
            => await Ipfs.FileSystem.AddDirectoryAsync(path, recursive, options, cancel).ConfigureAwait(false);
        public async Task RemoveGarbage(CancellationToken cancel = default)
            => await Ipfs.BlockRepository.RemoveGarbageAsync(cancel).ConfigureAwait(false);

        public async Task<Cid> ResolveToCid(string path, CancellationToken cancel = default)
        {
            string resolved = await Ipfs.ResolveAsync(path, cancel: cancel).ConfigureAwait(false);
            if (resolved == null || resolved.Length < 6 || resolved[0..6] != "/ipfs/") return null;
            return resolved[6..];
        }

        public async Task<FileSystemNode> CreateDirectory(IEnumerable<IFileSystemLink> links, bool pin = true, CancellationToken cancel = default)
            => await Ipfs.FileSystemEx.CreateDirectoryAsync(links, pin, cancel).ConfigureAwait(false);

        #endregion
    }
}
