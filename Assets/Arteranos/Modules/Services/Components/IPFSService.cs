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

        private const string PATH_USER_PRIVACY_NOTICE = "Privacy_TOS_Notice.md";

        private bool ForceIPFSShutdown = true;

        private const int heartbeatSeconds = 60;
        private const string AnnouncerTopic = "/X-Arteranos";
        private IpfsClientEx ipfs = null;
        private Peer self = null;
        private SignKey serverKeyPair = null;

        private string IPFSExe = "ipfs.exe";

        private DateTime last = DateTime.MinValue;

        private string versionString = null;

        private CancellationTokenSource cts = null;

        // ---------------------------------------------------------------
        #region Start & Stop

        private bool StartedIPFSDaemon = false;

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
                IEnumerator EnsureIPFSStarted()
                {
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

                        if (pk == null)
                        {
                            Debug.LogWarning("No IPFS repo, initializing...");
                            G.TransitionProgress?.OnProgressChanged(0.15f, "Initializing IPFS repository");
                            TryInitIPFS();

                            yield return new WaitForSeconds(2);
                        }

                        G.TransitionProgress?.OnProgressChanged(0.20f, "Starting IPFS daemon");

                        TryStartIPFS();

                        for (int i = 0; i < 5; i++)
                        {
                            yield return new WaitForSeconds(1);

                            Task<Peer> t = ipfsTmp.IdAsync();

                            yield return new WaitUntil(() => t.IsCompleted);

                            if (t.IsCompletedSuccessfully)
                            {
                                StartedIPFSDaemon = true;
                                self = t.Result;
                                yield break;
                            }
                        }

                        G.TransitionProgress?.OnProgressChanged(0.00f, "Failed to start daemon");
                        yield return new WaitForSeconds(10);
                        SettingsManager.Quit();
                    }

                    MultiAddress apiAddr = null;

                    try
                    {
                        apiAddr = IpfsClientEx.ReadDaemonAPIAddress(repodir);
                        int port = -1;
                        foreach (NetworkProtocol protocol in apiAddr.Protocols)
                            if (protocol.Code == 6)
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

                    // Increase max concurrent connections to prevent choking
                    // Not implemented -- WHY?!
                    //ipfsTmp.HttpMessageHandler = new HttpClientHandler()
                    //{
                    //    MaxConnectionsPerServer = 20,
                    //};

                    // First, see if there is an already running and accessible IPFS daemon.
                    self = null;
                    yield return Asyncs.Async2Coroutine(ipfsTmp.IdAsync(), _self => self = _self, _e =>
                    {
                        // No daemon, but that's okay. Yet.
                    });

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
                            G.TransitionProgress?.OnProgressChanged(0.00f, "No IPFS daemon -- aborting!");

                            yield return new WaitForSeconds(10);

                            Debug.LogError("Cannot find any ipfs executable!");
                            SettingsManager.Quit();
                            yield break;
                        }

                        // If there isn't a repo, initialize it, and start the daenon afterwards.
                        yield return StartupIPFSExeCoroutine();
                    }

                }

                // Find and start the IPFS Server. If it's not already running.
                yield return EnsureIPFSStarted();

                // Keep the IPFS synced - it needs the IPFS node alive.
                yield return Asyncs.Async2Coroutine(InitializeIPFS());

                yield return new WaitForSeconds(1);

                // Default avatars
                yield return UploadDefaultAvatars();

                // Start to emit the server description.
                StartCoroutine(EmitServerDescriptionCoroutine());

                // Start to emit the server online data.
                StartCoroutine(EmitServerOnlineDataCoroutine());

                // Start the Subscriber loop
                StartCoroutine(SubscriberCoroutine());

                // Ready to proceed, past the point we can manually shut down the IPFS daemon
                ForceIPFSShutdown = false;
            }

            IEnumerator SubscriberCoroutine()
            {
                Debug.Log("Subscribing Arteranos PubSub channel");

                int pubsubSeconds = 300;

                while(true)
                {
                    using CancellationTokenSource cts = new(pubsubSeconds * 1000 + 2000);

                    // Truly async.
                    _ = ipfs.PubSub.SubscribeAsync(AnnouncerTopic, ParseArteranosMessage, cts.Token);

                    yield return new WaitForSeconds(pubsubSeconds);

                    cts.Cancel();

                    yield return new WaitForSeconds(1);

                    // Debug.Log("Refreshing PubSub Listener loop");
               
                }
            }
            
            bool TryStartIPFS()
            {

                ProcessStartInfo psi = new()
                {
                    FileName = IPFSExe,
                    Arguments = $"--repo-dir={repodir} daemon --enable-pubsub-experiment",
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
                if (!ConfigUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
                {
                    ConfigUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, SettingsManager.DefaultTOStext);
                    Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
                }

                CachedPTOSNotice = ConfigUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

                try
                {
                    versionString = Core.Version.Load().MMP;
                }
                catch(Exception ex)
                {
                    Debug.LogError("Internal error: Missing version information - use Arteranos->Build->Update version");
                    Debug.LogException(ex);
                }

                G.TransitionProgress?.OnProgressChanged(0.30f, "Announcing its service");

                StringBuilder sb = new();
                sb.Append("Arteranos Server, built by willneedit\n");
                sb.Append(Core.Version.VERSION_MIN);

                // Put up the identifier file
                if (G.Server.Public)
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

                Debug.Log("---- IPFS Daemon init complete ----\n" +
                    $"IPFS Node's ID\n" +
                    $"   {self.Id}\n" +
                    $"Discovery identifier file's CID\n" +
                    $"   {IdentifyCid}\n");

                ipfs = ipfsTmp;

                // Reuse the IPFS peer key for the multiplayer server to ensure its association
                serverKeyPair = SignKey.ImportPrivateKey(pk);
                G.Server.UpdateServerKey(serverKeyPair);

                G.TransitionProgress?.OnProgressChanged(0.40f, "Connected to IPFS");
            }

            ipfs = null;
            IdentifyCid = null;
            last = DateTime.MinValue;
            cts = new();

            StartCoroutine(InitializeIPFSCoroutine());

        }

        private async void OnDisable()
        {
            await FlipServerDescription(false);


            cts?.Cancel();

            // If we're started the daemon on our own, shut it down, too.
            if(ForceIPFSShutdown)
            {
                Debug.Log("Shutting down the IPFS node, because the service didn't completely start.");
                await ipfs.ShutdownAsync();
            }
            if (G.Server.ShutdownIPFS)
            {
                Debug.Log("Shutting down the IPFS node.");
                await ipfs.ShutdownAsync();
            }
            else if(StartedIPFSDaemon)
                Debug.Log("NOT Shutting down the IPFS node, because you want it to remain running.");
            else
                Debug.Log("NOT Shutting down the IPFS node, because it's been already running.");

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
                yield return Asyncs.Async2Coroutine(FlipServerDescription(true));

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
                    FlipServerOnlineData_();
                }
                else if(saidOnline)
                {
                    // First time after being online, wave the community goodbye.
                    saidOnline = false;
                    SendServerGoodbye_();
                }

                yield return new WaitForSeconds(heartbeatSeconds);
            }
            // NOTREACHED
        }

        public async Task FlipServerDescription(bool reload)
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

                return await ipfs.FileSystemEx.CreateDirectoryAsync(list).ConfigureAwait(false);
            }

            if (!reload) return;

            IFileSystemNode fsn = await CreateServerDescription().ConfigureAwait(false);
            CurrentSDCid = fsn.Id;

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

        }

        private void FlipServerOnlineData_()
        {
            // Flood mitigation
            if (last > DateTime.UtcNow - TimeSpan.FromSeconds(30)) return;
            last = DateTime.UtcNow;

            List<byte[]> UserFingerprints = (from user in G.NetworkStatus.GetOnlineUsers()
                                where user.UserPrivacy != null && user.UserPrivacy.Visibility != Visibility.Invisible
                                select CryptoHelpers.GetFingerprint(user.UserID)).ToList();

            List<string> IPAddresses = (from entry in (List<IPAddress>)G.NetworkStatus.IPAddresses
                               select entry.ToString()).ToList();

            ServerOnlineData sod = new()
            {
                CurrentWorldCid = G.World.Cid,
                CurrentWorldName = G.World.Name,
                UserFingerprints = UserFingerprints,
                ServerDescriptionCid = CurrentSDCid,
                // LastOnline = last, // Not serialized - set on receive
                OnlineLevel = G.NetworkStatus.GetOnlineLevel(),
                IPAddresses = IPAddresses
            };

            using MemoryStream ms = new();
            sod.Serialize(ms);

            // Announce the server online data, too - fire and forget.
            _ = ipfs.PubSub.PublishAsync(AnnouncerTopic, ms.ToArray());
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
                IPAddresses = null
            };
            using MemoryStream ms = new();
            sod.Serialize(ms);

            // Just wave a goodbye.
            _ = ipfs.PubSub.PublishAsync(AnnouncerTopic, ms.ToArray());
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
                    DownloadServerDescription(SenderPeerID, sod.ServerDescriptionCid);
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
        private void DownloadServerDescription(MultiHash SenderPeerID, string sddPath)
        {

            async Task DownloadSDAsync(MultiHash SenderPeerID, string sddPath)
            {
                try
                {
                    using CancellationTokenSource cts = new(1000);

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
            TaskScheduler.Schedule(MakeDownloadTask(SenderPeerID, sddPath));
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
