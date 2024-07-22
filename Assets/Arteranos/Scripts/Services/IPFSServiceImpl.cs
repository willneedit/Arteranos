/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

// Use Pubsub. kubo's implementation is deemed to be experimental but
// it persists in 0.28.0 and works fine and yields a better accuracy of
// the server online data.
#define USE_PUBSUB

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
using System.Collections.Concurrent;
using IPAddress = System.Net.IPAddress;
using Arteranos.Core.Operations;
using System.Net.Sockets;
using TaskScheduler = Arteranos.Core.TaskScheduler;
using ProtoBuf;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Arteranos.Services
{
    public class IPFSServiceImpl : IPFSService
    {
        public override IpfsClientEx Ipfs { get => ipfs; }
        public override Peer Self { get => self; }
        public override SignKey ServerKeyPair { get => serverKeyPair; }
        public override Cid IdentifyCid { get; protected set; }
        public override Cid CurrentSDCid { get; protected set; } = null;
        public override bool UsingPubsub { get; protected set; } =
#if USE_PUBSUB
            true
#else
            false
#endif
            ;

        public bool EnableUploadDefaultAvatars = true;
        public bool EnablePublishServerData = true;
        public bool EnableServerDiscovery = true;

        public static string CachedPTOSNotice { get; private set; } = null;

        private const string PATH_USER_PRIVACY_NOTICE = "Privacy_TOS_Notice.md";

        private const int heartbeatSeconds = 60;
        private const string AnnouncerTopic = "/X-Arteranos";
        private IpfsClientEx ipfs = null;
        private Peer self = null;
        private SignKey serverKeyPair = null;

        private string IPFSExe = "ipfs.exe";

        private DateTime last = DateTime.MinValue;

        private string versionString = null;

        private Cid sod_key_id = null;

        private CancellationTokenSource cts = null;

        private ConcurrentDictionary<MultiHash, Peer> DiscoveredPeers = null;

        // ---------------------------------------------------------------
        #region Start & Stop
        private void Awake()
        {
            G.IPFSService = this;
        }

        private void Start()
        {
            IpfsClientEx ipfsTmp = null;

            // Shared directory between Desktop and Dedicated Server - one node, one daemon.
            string repodir = $"{Application.persistentDataPath}/.ipfs";


            IEnumerator InitializeIPFSCoroutine()
            {
                MultiAddress apiAddr = null;

                try
                {
                    apiAddr = IpfsClientEx.ReadDaemonAPIAddress(repodir);
                    int port = -1;
                    foreach(NetworkProtocol protocol in apiAddr.Protocols)
                        if(protocol.Code == 6)
                        {
                            port = int.Parse(protocol.Value);
                            break;
                        }

                    Debug.Log($"Present configuration says API port {port}");
                    ipfsTmp = new($"http://localhost:{port}");
                }
                catch
                {
                    Debug.LogWarning($"No usable API address, assuming default one. Or, initializing a new repo.");
                    ipfsTmp = new();
                }

                // First, see if there is an already running and accessible IPFS daemon.
                {
                    self = null;
                    yield return Asyncs.Async2Coroutine(ipfsTmp.IdAsync(), _self => self = _self, _e =>
                    {
                        // No daemon, but that's okay. Yet.
                    });
                }

                // If not....
                if (self == null)
                {
                    IPFSExe = "ipfs.exe";

                    // In App: Next to the Arteranos (dedicated server) executable
                    // In Editor: Project root, ignored in Git repo, generated along with the project build
                    yield return FindIPFSExecutable();

                    // Even worse...
                    if (IPFSExe == null)
                    {
                        TransitionProgressStatic.Instance?.OnProgressChanged(0.00f, "No IPFS daemon -- aborting!");

                        yield return new WaitForSeconds(10);

                        Debug.LogError("Cannot find any ipfs executable!");
                        SettingsManager.Quit();
                        yield break;
                    }

                    // If there isn't a repo, initialize it, and start the daenon afterwards.
                    yield return StartupIPFSExeCoroutine();
                }

                // Keep the IPFS synced - it needs the IPFS node alive.
                yield return Asyncs.Async2Coroutine(InitializeIPFS());

                // Default avatars
                yield return UploadDefaultAvatars();

                // Initiate the Arteranos peer discovery.
                StartCoroutine(DiscoverPeersCoroutine());

                // Start to emit the server description.
                StartCoroutine(EmitServerDescriptionCoroutine());

                // Start to emit the server online data.
                StartCoroutine(EmitServerOnlineDataCoroutine());
            }
            
            IEnumerator FindIPFSExecutable()
            {
                bool IPFSAccessible()
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = IPFSExe,
                        Arguments = "help",
                        UseShellExecute = false,
                        RedirectStandardError = false,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        CreateNoWindow = true,
                    };

                    try
                    {
                        Process process = Process.Start(psi);
                        return true;
                    }
                    catch
                    {
                        Debug.LogWarning("No installed IPFS daemon available - needs to be acquired");
                    }

                    return false;
                }

                if (!IPFSAccessible())
                {
                    IPFSExe = null;
                    yield break;
                }
            }

            IEnumerator StartupIPFSExeCoroutine()
            {
                PrivateKey pk = null;

                try
                {
                    pk = IpfsClientEx.ReadDaemonPrivateKey(repodir);
                }
                catch
                {
                }

                if(pk == null)
                {
                    Debug.LogWarning("No IPFS repo, initializing...");
                    TransitionProgressStatic.Instance.OnProgressChanged(0.15f, "Initializing IPFS repository");
                    TryInitIPFS();

                    yield return new WaitForSeconds(2);
                }

                TransitionProgressStatic.Instance?.OnProgressChanged(0.20f, "Starting IPFS daemon");

                TryStartIPFS();

                for(int i = 0; i < 5; i++)
                {
                    yield return new WaitForSeconds(1);

                    Task<Peer> t = ipfsTmp.IdAsync();

                    yield return new WaitUntil(() => t.IsCompleted);

                    if (t.IsCompletedSuccessfully)
                    {
                        self = t.Result;
                        yield break;
                    }
                }

                TransitionProgressStatic.Instance.OnProgressChanged(0.00f, "Failed to start daemon");
                yield return new WaitForSeconds(10);
                SettingsManager.Quit();
            }

            bool TryStartIPFS()
            {

                ProcessStartInfo psi = new()
                {
                    FileName = IPFSExe,
                    Arguments = UsingPubsub
                    ? $"--repo-dir={repodir} daemon --enable-pubsub-experiment"
                    : $"--repo-dir={repodir} daemon",
                    UseShellExecute = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                };

                try
                {
                    Process process = Process.Start(psi);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }

                return true;
            }

            bool TryInitIPFS()
            {
                ProcessStartInfo psi = new()
                {
                    FileName = IPFSExe,
                    Arguments = $"--repo-dir={repodir} init",
                    UseShellExecute = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                };

                try
                {
                    Process process = Process.Start(psi);

                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }

                return true;
            }

            async Task InitializeIPFS()
            {
                ipfs = null;
                if (self == null) throw new InvalidOperationException("Dead daemon");

                PrivateKey pk = IpfsClientEx.ReadDaemonPrivateKey(repodir);

                try
                {
                    await ipfsTmp.VerifyDaemonAsync(pk);
                }
                catch (InvalidDataException)
                {
                    Debug.LogError("Daemon doesn't match with its supposed private key");
                    SettingsManager.Quit();
                    return;
                }
                catch
                {
                    Debug.LogError("Daemon communication");
                    SettingsManager.Quit();
                    return;
                }

                // If it doesn't exist, write down the template in the config directory.
                if (!FileUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
                {
                    FileUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, SettingsManager.DefaultTOStext);
                    Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
                }

                CachedPTOSNotice = FileUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

                try
                {
                    versionString = Core.Version.Load().MMP;
                }
                catch(Exception ex)
                {
                    Debug.LogError("Internal error: Missing version information - use Arteranos->Build->Update version");
                    Debug.LogException(ex);
                }

                TransitionProgressStatic.Instance?.OnProgressChanged(0.30f, "Announcing its service");

                StringBuilder sb = new();
                sb.Append("Arteranos Server, built by willneedit\n");
                sb.Append(Core.Version.VERSION_MIN);

                // Put up the identifier file
                if (SettingsManager.Server.Public)
                {
                    IdentifyCid = (await ipfsTmp.FileSystem.AddTextAsync(sb.ToString())).Id;
                }
                else
                {
                    // rm -f the identifier file
                    AddFileOptions ao = new() { OnlyHash = true };
                    IdentifyCid = (await ipfsTmp.FileSystem.AddTextAsync(sb.ToString(), ao)).Id;

                    try
                    {
                        await ipfsTmp.Pin.RemoveAsync(IdentifyCid).ConfigureAwait(false);
                        await ipfsTmp.Block.RemoveAsync(IdentifyCid, true).ConfigureAwait(false);
                    }
                    catch { }
                }

                IEnumerable<IKey> keychain = await ipfsTmp.Key.ListAsync();

                foreach (IKey key in keychain)
                {
                    if(key.Name == "key_sod")
                    {
                        Debug.Log($"Using existing key for Server Online Data publishing");
                        sod_key_id = key.Id; break;
                    }
                }

                if(sod_key_id == null) 
                {
                    Debug.Log($"Generating key for Server Online Data publishing");
                    IKey key = await ipfsTmp.Key.CreateAsync("key_sod", "ed25519", 0);
                    sod_key_id = key.Id;
                }

                if(UsingPubsub)
                    _ = ipfsTmp.PubSub.SubscribeAsync(AnnouncerTopic, ParseArteranosMessage, cts.Token);

                Debug.Log("---- IPFS Daemon init complete ----\n" +
                    $"IPFS Node's ID\n" +
                    $"   {self.Id}\n" +
                    $"Discovery identifier file's CID\n" +
                    $"   {IdentifyCid}\n" +
                    $"Server Online Data publish key\n" +
                    $"   {sod_key_id}\n" +
                    $"Using Pubsub\n" +
                    $"   {UsingPubsub}");

                ipfs = ipfsTmp;

                // Reuse the IPFS peer key for the multiplayer server to ensure its association
                serverKeyPair = SignKey.ImportPrivateKey(pk);
                SettingsManager.Server.UpdateServerKey(serverKeyPair);

                TransitionProgressStatic.Instance?.OnProgressChanged(0.40f, "Connected to IPFS");
            }

            ipfs = null;
            IdentifyCid = null;
            last = DateTime.MinValue;
            DiscoveredPeers = new();
            cts = new();

            StartCoroutine(InitializeIPFSCoroutine());

        }

        private async void OnDisable()
        {
            await this.FlipServerDescription(false);

            Debug.Log("Shutting down the IPFS node.");

            cts?.Cancel();

            // TODO Daemon Shutdown
            // await ipfs.ShutdownAsync();

            cts?.Dispose();
        }

        private IEnumerator DiscoverPeersCoroutine()
        {
            async Task<List<Peer>> DoDiscovery()
            {
                IEnumerable<Peer> peers = await ipfs.Routing.FindProvidersAsync(IdentifyCid,
                    1000, // FIXME Maybe configurable.
                    _peer => OnDiscoveredPeer(_peer)).ConfigureAwait(false);

                return peers.ToList();
            }

            // TransitionProgressStatic.Instance?.OnProgressChanged(0.70f, "Discovering other servers");

            if(EnableServerDiscovery)
            {
                Debug.Log($"Starting node discovery");
                while (true)
                {
                    List<Peer> peers = null;
                    yield return Asyncs.Async2Coroutine(DoDiscovery(), _p => peers = _p);

                    yield return new WaitForSeconds(heartbeatSeconds * 2);
                }
                // NOTREACHED
            }
        }
        private IEnumerator UploadDefaultAvatars()
        {
            TransitionProgressStatic.Instance?.OnProgressChanged(0.50f, "Uploading default resources");

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
                SettingsManager.DefaultMaleAvatar = cid;
                yield return UploadAvatar("resource:///Avatar/63c26702e5b9a435587fba51.glb");
                SettingsManager.DefaultFemaleAvatar = cid;
            }
        }


        public override async Task<IPAddress> GetPeerIPAddress(MultiHash PeerID, CancellationToken token = default)
        {
            IEnumerable<(IPAddress, ProtocolType, int)> ipAddresses = await ipfs.Routing.FindPeerAddressesAsync(PeerID, token).ConfigureAwait(false);

            if (!ipAddresses.Any()) return null;

            // Prefer IPv6 - less NAT or port forwarding issues
            foreach (var ipAddress in ipAddresses)
                if(ipAddress.Item1.AddressFamily == AddressFamily.InterNetworkV6)
                    return ipAddress.Item1;

            return ipAddresses.First().Item1;
        }

        #endregion
        // ---------------------------------------------------------------
        #region Server information publishing

        public IEnumerator EmitServerDescriptionCoroutine()
        {
            while (true)
            {
                yield return Asyncs.Async2Coroutine(FlipServerDescription(true));

                // One day
                yield return new WaitForSeconds(24 * 60 * 60);
            }
            // NOTREACHED
        }

        private IEnumerator EmitServerOnlineDataCoroutine()
        {
            while (true)
            {
                if (G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Server ||
                    G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Host)
                {
                    yield return Asyncs.Async2Coroutine(FlipServerOnlineData_());
                }

                yield return new WaitForSeconds(heartbeatSeconds);
            }
            // NOTREACHED
        }

        public override async Task FlipServerDescription(bool reload)
        {
            async Task<IFileSystemNode> CreateSDFile()
            {
                Server server = SettingsManager.Server;
                IEnumerable<string> q = from entry in SettingsManager.ServerUsers.Base
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
                    LastModified = server.ConfigLastChanged,
                    ServerOnlineDataLinkCid = sod_key_id
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

                return await ipfs.FileSystemEx.CreateDirectoryAsync(list).ConfigureAwait(false);
            }

            if (!reload) return;

            IFileSystemNode fsn = await CreateServerDescription().ConfigureAwait(false);
            CurrentSDCid = fsn.Id;

            if (SettingsManager.Server.Public)
            {
                if(EnablePublishServerData)
                {
                    // Lasting for two days max, cache refresh needs one minute
                    await ipfs.NameEx.PublishAsync(
                        CurrentSDCid,
                        lifetime: new TimeSpan(2, 0, 0, 0),
                        ttl: new TimeSpan(0, 0, 1, 0)).ConfigureAwait(false);
                }

                if (UsingPubsub)
                {
                    // Submit the announce in Pubsub as well, for all who's listening
                    ServerDescriptionLink sdl = new() { ServerDescriptionCid = CurrentSDCid };
                    using MemoryStream ms = new();
                    sdl.Serialize(ms);
                    ms.Position = 0;

                    await ipfs.PubSub.PublishAsync(AnnouncerTopic, ms.ToArray());
                }

                Debug.Log($"Published server description CID: {CurrentSDCid}");
            }
            else
                Debug.Log($"Server description CID would be: {CurrentSDCid}");

        }

        private async Task FlipServerOnlineData_()
        {
            // Flood mitigation
            if (last > DateTime.UtcNow - TimeSpan.FromSeconds(30)) return;
            last = DateTime.UtcNow;

            List<byte[]> UserFingerprints = (from user in G.NetworkStatus.GetOnlineUsers()
                                where user.UserPrivacy != null && user.UserPrivacy.Visibility != Visibility.Invisible
                                select CryptoHelpers.GetFingerprint(user.UserID)).ToList();

            ServerOnlineData sod = new()
            {
                CurrentWorldCid = SettingsManager.WorldCid,
                CurrentWorldName = SettingsManager.WorldName,
                UserFingerprints = UserFingerprints,
                // LastOnline = last, // Not serialized - set on receive
                OnlineLevel = G.NetworkStatus.GetOnlineLevel()
            };

            using MemoryStream ms = new();

            sod.Serialize(ms);
            ms.Position = 0;

            // No IPNS together with Pubsub, because latecomers get updated online data at max. one minute.
            if (UsingPubsub)
            {
                // Announce the server online data, too - fire and forget.
                _ = ipfs.PubSub.PublishAsync(AnnouncerTopic, ms.ToArray());
            }
            else
            {
                IFileSystemNode fsn = await ipfs.FileSystem.AddAsync(ms, "ServerOnlineData").ConfigureAwait(false);

                if(EnablePublishServerData)
                {
                    // Lasting for five minutes, ttl 60s, under the secondary key
                    await ipfs.NameEx.PublishAsync(
                        fsn.Id,
                        key: "key_sod",
                        lifetime: new TimeSpan(2, 0, 0, 0),
                        ttl: new TimeSpan(0, 0, 1, 0)).ConfigureAwait(false);
                }
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region Peer communication and data exchange

        private void ParseArteranosMessage(IPublishedMessage message)
        {
            MultiHash SenderPeerID = message.Sender.Id;

#if !UNITY_EDITOR
            // No self-gratification.
            if (SenderPeerID == self.Id) return;
#endif

            try
            {
                using MemoryStream ms = new(message.DataBytes);
                PeerMessage pm = PeerMessage.Deserialize(ms);

                if (pm is ServerDescriptionLink sdl) // Too big for pubsub, this is only a link
                {
                    // Debug.Log($"New server description from {SenderPeerID}");
                    DownloadServerDescription(SenderPeerID, sdl.ServerDescriptionCid);
                }
                else if (pm is ServerOnlineData sod) // As-is.
                {
                    // Debug.Log($"New server online data from {SenderPeerID}");
                    sod.LastOnline = DateTime.UtcNow; // Not serialized
                    sod.DBInsert(SenderPeerID.ToString());
                }
                else
                    Debug.LogWarning($"Discarding unknown message from {SenderPeerID}");
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        // Server description are generated by the Server Discovery
        private void DownloadServerDescription(MultiHash SenderPeerID, string sddPath = null)
        {

            async Task DownloadSDAsync(MultiHash SenderPeerID, string sddPath)
            {
                try
                {
                    using CancellationTokenSource cts = new(30000);

                    // Server description is published under the peer ID. (= self)
                    // If it's cached, it's already in the local repo.
                    // And the TTL reduce the traffic.
                    using Stream s = await ipfs.FileSystem.ReadFileAsync(sddPath + "/ServerDescription", cts.Token).ConfigureAwait(false);

                    // TODO Maybe unneccessary?
                    PublicKey pk = PublicKey.FromId(SenderPeerID);
                    ServerDescription sd = ServerDescription.Deserialize(pk, s);

                    sd.DBUpdate();
                }
                catch
                {
                    Debug.Log($"Timeout while downloading server online data from {SenderPeerID} - likely offline.");
                }
            }

            Func<Task> MakeDownloadTask(MultiHash SenderPeerID, string sddPath)
            {
                Task a() => DownloadSDAsync(SenderPeerID, sddPath);
                return a;
            }

            // If we have the CID, okay. If not, use the IPNS resolver.
            sddPath ??= "/ipns/" + SenderPeerID;
            TaskScheduler.Schedule(MakeDownloadTask(SenderPeerID, sddPath));
        }

        // Server Online Data are generated on demand from the server list and the world
        // panel generation.
        public override void DownloadServerOnlineData(MultiHash SenderPeerID, Action callback = null)
        {
            async Task DownloadTask(string sodPath)
            {
                try
                {
                    using CancellationTokenSource cts = new(10000);

                    using Stream s = await ipfs.FileSystem.ReadFileAsync(sodPath).ConfigureAwait(false);
                    ServerOnlineData sod = Serializer.Deserialize<ServerOnlineData>(s);

                    sod.LastOnline = DateTime.UtcNow; // Not serialized

                    sod.DBInsert(SenderPeerID.ToString());
                }
                catch (Exception e)
                {
                    // Seems to be offline.
                    Debug.LogWarning($"No SOD from {sodPath} ({SenderPeerID}): {e.Message}");
                }

                TaskScheduler.ScheduleCallback(callback);
            }

            Func<Task> MakeDownloadTask(string sodPath)
            {
                Task a() => DownloadTask(sodPath);
                return a;
            }

            ServerDescription sd = ServerDescription.DBLookup(SenderPeerID.ToString());
            if (sd == null) return;

            string sodPath = "/ipns/" + sd.ServerOnlineDataLinkCid;

            TaskScheduler.Schedule(MakeDownloadTask(sodPath));
        }

        private void OnDiscoveredPeer(Peer found)
        {
            MultiHash peerId = found.Id;
            if (DiscoveredPeers.ContainsKey(peerId)) return;
            DiscoveredPeers[peerId] = found;

            if (peerId == self.Id)
            {
#if !UNITY_EDITOR
                Debug.Log($"Discovered node {peerId} is self, skipping.");
                return;
#else
                // Enable loopback for debugging
                Debug.Log($"Discovered node {peerId} is self, adding for debugging purposes.");
#endif
            }

            ServerDescription serverDescription = ServerDescription.DBLookup(peerId.ToString());
            if(serverDescription == null)
            {
                Debug.Log($"Discovered node {peerId} is new, downloading data");
                DownloadServerDescription(peerId);
            }
            else
                Debug.Log($"Rediscovered node {peerId}.");

        }
        #endregion
        // ---------------------------------------------------------------
        #region IPFS Lowlevel interface
        public override Task PinCid(Cid cid, bool pinned, CancellationToken token = default)
        {
            if (pinned)
                return ipfs.Pin.AddAsync(cid, cancel: token);
            else
                return ipfs.Pin.RemoveAsync(cid, cancel: token);
        }

        public override async Task<byte[]> ReadBinary(string path, Action<long> reportProgress = null, CancellationToken cancel = default)
        {
            using MemoryStream ms = new();
            using Stream instr = await ipfs.FileSystem.ReadFileAsync(path, cancel).ConfigureAwait(false);
            byte[] buffer = new byte[128 * 1024];

            // No cancel. We have to do it until the end, else the RPC client would choke.
            long totalBytes = 0;
            while (true)
            {
                int n = instr.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;
                totalBytes += n;
                try
                {
                    reportProgress?.Invoke(totalBytes);
                }
                catch { } // Whatever may come, the show must go on.
                ms.Write(buffer, 0, n);
            }

            return ms.ToArray();
        }
        #endregion
    }
}
