using UnityEngine;
using Mirror;
using Arteranos.Core;
using Arteranos.Avatar;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Arteranos.UI;
using Arteranos.Core.Operations;
using Ipfs.Core.Cryptography.Proto;
using Arteranos.Core.Cryptography;
using System.Collections;
using Arteranos.Social;
using Arteranos.Web;
using System.Linq;
using Ipfs;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

namespace Arteranos.Services
{
    public class ArteranosNetworkManager : NetworkManager
    {
        internal struct AuthSequence
        {
            public ArteranosNetworkAuthenticator.AuthRequestPayload request;
            public ArteranosNetworkAuthenticator.AuthResponsePayload response;
        }

        // Overrides the base singleton so we don't
        // have to cast to this type everywhere.
        public static ArteranosNetworkManager Instance { get; private set; }

        private readonly Dictionary<int, AuthSequence> ResponseMessages = new();

        private readonly Dictionary<int, DateTime> SCLastUpdatedToClient = new();

        // ---------------------------------------------------------------
        #region Utilities

        internal void AddResponseMessage(int connid, AuthSequence sequence)
        {
            if (ResponseMessages.ContainsKey(connid))
            {
                // Connid collision exploit?!
                sequence.response.UserState = 0; // Maybe banned if it's proven reliable as an attack indication.
                Debug.LogWarning($"ConnID {connid} assigned twice - race condition or a attack?");
            }

            ResponseMessages.Add(connid, sequence);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Unity Callbacks

        /// <summary>
        /// Runs on both Server and Client
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public override void OnValidate()
        {
            base.OnValidate();
        }

        /// <summary>
        /// Runs on both Server and Client
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// Runs on both Server and Client
        /// </summary>
        public override void LateUpdate()
        {
            base.LateUpdate();
        }

        /// <summary>
        /// Runs on both Server and Client
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion
        // ---------------------------------------------------------------
        #region Start & Stop

        /// <summary>
        /// Set the frame rate for a headless server.
        /// <para>Override if you wish to disable the behavior or set your own tick rate.</para>
        /// </summary>
        public override void ConfigureHeadlessFrameRate()
        {
            base.ConfigureHeadlessFrameRate();
        }

        /// <summary>
        /// called when quitting the application by closing the window / pressing stop in the editor
        /// </summary>
        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
        }

        #endregion
        // ---------------------------------------------------------------
        #region Scene Management

        /// <summary>
        /// This causes the server to switch scenes and sets the networkSceneName.
        /// <para>Clients that connect to this server will automatically switch to this scene. This is called automatically if onlineScene or offlineScene are set, but it can be called from user code to switch scenes again while the game is in progress. This automatically sets clients to be not-ready. The clients must call NetworkClient.Ready() again to participate in the new scene.</para>
        /// </summary>
        /// <param name="newSceneName"></param>
        public override void ServerChangeScene(string newSceneName)
        {
            base.ServerChangeScene(newSceneName);
        }

        /// <summary>
        /// Called from ServerChangeScene immediately before SceneManager.LoadSceneAsync is executed
        /// <para>This allows server to do work / cleanup / prep before the scene changes.</para>
        /// </summary>
        /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
        public override void OnServerChangeScene(string newSceneName) { }

        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
        /// </summary>
        /// <param name="sceneName">The name of the new scene.</param>
        public override void OnServerSceneChanged(string sceneName) { }

        /// <summary>
        /// Called from ClientChangeScene immediately before SceneManager.LoadSceneAsync is executed
        /// <para>This allows client to do work / cleanup / prep before the scene changes.</para>
        /// </summary>
        /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
        /// <param name="sceneOperation">Scene operation that's about to happen</param>
        /// <param name="customHandling">true to indicate that scene loading will be handled through overrides</param>
        public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) { }

        /// <summary>
        /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
        /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
        /// </summary>
        public override void OnClientSceneChanged()
        {
            base.OnClientSceneChanged();
        }

        #endregion
        // ---------------------------------------------------------------
        #region Server System Callbacks

        /// <summary>
        /// Called on the server when a new client connects.
        /// <para>Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            // Next time, prepare the next collection list as the full list
            // to cater to the latecomer.
            SCLastUpdatedToClient[conn.connectionId] = DateTime.MinValue;
        }

        /// <summary>
        /// Called on the server when a client is ready.
        /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
        }

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Transform startPos = User.SpawnManager.GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, player);

            // Initialize the avatar's brain directly from the server's resources as with the login history
            IAvatarBrain brain = player.GetComponent<IAvatarBrain>();

            if (ResponseMessages.TryGetValue(conn.connectionId, out AuthSequence seq))
            {
                ResponseMessages.Remove(conn.connectionId);

                brain.UserState = seq.response.UserState;
                brain.UserID = new UserID(seq.request.ClientSignPublicKey, seq.request.Nickname);
                brain.Address = conn.address;
                brain.DeviceID = seq.request.deviceUID;
                brain.AgreePublicKey = seq.request.ClientAgreePublicKey;

                EmitToClientCurrentWorld(conn, brain.AgreePublicKey);
            }
            else conn.Disconnect();  // No discussion, there'd be something fishy...
        }

        /// <summary>
        /// Called on the server when a client disconnects.
        /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            // Maybe the client never made it so far, just past the authentication.
            if (ResponseMessages.ContainsKey(conn.connectionId))
                ResponseMessages.Remove(conn.connectionId);

            if (SCLastUpdatedToClient.ContainsKey(conn.connectionId))
                SCLastUpdatedToClient.Remove(conn.connectionId);
        }

        /// <summary>
        /// Called on server when transport raises an exception.
        /// <para>NetworkConnection may be null.</para>
        /// </summary>
        /// <param name="conn">Connection of the client...may be null</param>
        /// <param name="exception">Exception thrown from the Transport.</param>
        public override void OnServerError(NetworkConnectionToClient conn, TransportError transportError, string message) { }

        #endregion
        // ---------------------------------------------------------------
        #region Client System Callbacks

        /// <summary>
        /// Called on the client when connected to a server.
        /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
        /// </summary>
        public override void OnClientConnect()
        {
            base.OnClientConnect();

            NetworkStatus.OnClientConnectionResponse?.Invoke(true, null);
        }

        /// <summary>
        /// Called on clients when disconnected from a server.
        /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
        /// </summary>
        public override void OnClientDisconnect()
        {
            NetworkStatus.OnClientConnectionResponse?.Invoke(false, null);

            SettingsManager.CurrentServer = null;

            // Fall back to the client's only permissions, like flying
            SettingsManager.Client.PingXRControllersChanged();
        }

        /// <summary>
        /// Called on clients when a servers tells the client it is no longer ready.
        /// <para>This is commonly used when switching scenes.</para>
        /// </summary>
        public override void OnClientNotReady() { }

        /// <summary>
        /// Called on client when transport raises an exception.</summary>
        /// </summary>
        /// <param name="exception">Exception thrown from the Transport.</param>
        public override void OnClientError(TransportError transportError, string message) { }

        #endregion
        // ---------------------------------------------------------------
        #region Start & Stop Callbacks

        // Since there are multiple versions of StartServer, StartClient and StartHost, to reliably customize
        // their functionality, users would need override all the versions. Instead these callbacks are invoked
        // from all versions, so users only need to implement this one case.

        /// <summary>
        /// This is invoked when a host is started.
        /// <para>StartHost has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public override void OnStartHost() { }

        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            ResponseMessages.Clear();
            SCLastUpdatedToClient.Clear();

            NetworkServer.RegisterHandler<CTSPacketEnvelope>(OnServerGotCTSPacket);
        }

        /// <summary>
        /// This is invoked when the client is started.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            NetworkClient.RegisterHandler<CTSPacketEnvelope>(OnClientGotCTSPacket);
        }

        /// <summary>
        /// This is called when a host is stopped.
        /// </summary>
        public override void OnStopHost() { }

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();

            SettingsManager.WorldCid = null;

            NetworkServer.UnregisterHandler<CTSPacketEnvelope>();
        }

        /// <summary>
        /// This is called when a client is stopped.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();

            NetworkClient.UnregisterHandler<CTSPacketEnvelope>();
        }

        #endregion
        // ---------------------------------------------------------------
        #region World Change Announcements

        [Server]
        public void EmitToClientCurrentWorld(NetworkConnectionToClient conn, PublicKey agreePublicKey)
        {
            IEnumerator SendWCAJustForTheLaggard(NetworkConnectionToClient conn, PublicKey agreePublicKey)
            {
                WorldInfo wi = null;

                yield return WorldInfo.RetrieveCoroutine(SettingsManager.WorldCid, (_wi) => wi = _wi);

                EmitToClientCTSPacket(new CTSPWorldChangeAnnouncement()
                {
                    WorldInfo = wi?.Strip(),
                }, conn, agreePublicKey);
            }


            Debug.Log($"[Server] sending world CID '{SettingsManager.WorldCid}' to latecoming conn {conn.connectionId}");

            SettingsManager.StartCoroutineAsync(() => SendWCAJustForTheLaggard(conn, agreePublicKey));
        }



        #endregion
        // ---------------------------------------------------------------
        #region CTS Packets (Communication)

        public void EmitToServerCTSPacket(CTSPacket packet)
        {
            Client client = SettingsManager.Client;
            UserID sender = new(client.UserSignPublicKey, client.Me.Nickname);

            if (!NetworkClient.active)
                // Directly slice the packet into the server logic
                ServerLocalCTSPacket(sender, packet);
            else
            {
                // Sign, encrypt and send it off to the connected server
                Client.TransmitMessage(
                    packet.Serialize(),
                    SettingsManager.ActiveServerData.ServerAgrPublicKey,
                    out CMSPacket payload);
                NetworkClient.Send(new CTSPacketEnvelope()
                {
                    CTSPayload = payload
                });
            }
        }

        public void EmitToClientCTSPacket(CTSPacket packet, IAvatarBrain to = null)
        {
            NetworkConnectionToClient connectionToClient = null;

            if (to != null)
            {
                if(to.gameObject == null)
                {
                    Debug.Log("[Server] Discarding CTSPacket to specific logged-out client");
                    return;
                }
                connectionToClient = to.gameObject.GetComponent<NetworkIdentity>().connectionToClient;
            }

            PublicKey agreePublicKey = to?.AgreePublicKey;

            EmitToClientCTSPacket(packet, connectionToClient, agreePublicKey);
        }

        private void EmitToClientCTSPacket(CTSPacket packet, NetworkConnectionToClient connectionToClient = null, PublicKey agreePublicKey = null)
        {
            if (!NetworkServer.active)
                // Directly slice the packet into the client logic
                ClientLocalCTSPacket(packet);
            else
            {
                // Sign, encrypt if it's toward to a specific user
                Server.TransmitMessage(
                    packet.Serialize(),
                    agreePublicKey, // Leave unencrypted if it's for everyone
                    out CMSPacket payload);

                CTSPacketEnvelope envelope = new() { CTSPayload = payload };

                // Send to all, or the specific user
                if (connectionToClient != null)
                    connectionToClient.Send(envelope);
                else
                    NetworkServer.SendToAll(envelope);
            }
        }

        private IEnumerator ShowDialogCoroutine(string message)
        {
            XR.ScreenFader.StartFading(0.0f);

            yield return null;

            IDialogUI dialog = DialogUIFactory.New();
            dialog.Text = message;
            dialog.Buttons = new string[] { "OK" };
        }

        private void EmitCTSPacket(CTSPacket packet, IAvatarBrain to)
        {
            EmitToClientCTSPacket(packet, to);
        }

        [Server]
        private void OnServerGotCTSPacket(NetworkConnectionToClient client, CTSPacketEnvelope envelope)
        {
            try
            {
                Server.ReceiveMessage(envelope.CTSPayload, out byte[] data, out PublicKey signerPublicKey);
                IAvatarBrain sender = NetworkStatus.GetOnlineUser(client.identity.netId);

                if (sender == null || sender.UserID != signerPublicKey)
                    throw new InvalidOperationException("Sender with invalid/forged packet signature");

                ServerLocalCTSPacket(sender.UserID, CTSPacket.Deserialize(data));
            }
            catch (Exception ex)
            {
                client.Disconnect();
                Debug.LogException(ex);
            }
        }

        [Client]
        private void OnClientGotCTSPacket(CTSPacketEnvelope envelope)
        {
            try
            {
                Client.ReceiveMessage(envelope.CTSPayload, out byte[] data, out PublicKey signerPublicKey);
                if (SettingsManager.ActiveServerData.ServerSignPublicKey != signerPublicKey)
                    throw new InvalidOperationException("Sender is not the active server");

                ClientLocalCTSPacket(CTSPacket.Deserialize(data));
            }
            catch (Exception ex)
            {
                // No disconnect because of injected messages from trolls.
                Debug.LogException(ex);
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region CTS Packets dispatch

        // Entry point of the server side and offline mode
        public void ServerLocalCTSPacket(UserID sender, CTSPacket packet)
        {
            if (packet is CTSPWorldChangeAnnouncement wca)
                StartCoroutine(MakeServerWorldTransition(sender, wca));
            else if (packet is CTSMessage message)
                throw new InvalidOperationException($"Attempting to display a message on server: {message.message}");
            else if (packet is CTSPUpdateUserState uss)
                _ = ServerUpdateUserState(sender, uss);
            else if (packet is STCUserInfo userInfoQuery)
                _ = ServerQueryUserState(sender, userInfoQuery);
            else
                throw new NotImplementedException();
        }

        // Entry point of the client side and offline mode
        private void ClientLocalCTSPacket(CTSPacket packet)
        {
            if (packet is CTSPWorldChangeAnnouncement wca)
                StartCoroutine(MakeClientWorldTransition(wca));
            else if (packet is CTSMessage message)
                StartCoroutine(ShowDialogCoroutine(message.message));
            else if (packet is CTSPUpdateUserState uss)
                _ = ClientInformUpdatedUserState(uss);
            else if (packet is STCUserInfo userInfo)
                _ = ClientQueryUserState(userInfo);
            else
                throw new NotImplementedException();
        }

        #endregion
        // ---------------------------------------------------------------
        #region World Teansition (all)

        private IEnumerator MakeServerWorldTransition(UserID invoker, CTSPWorldChangeAnnouncement wca)
        {
            wca.invoker = invoker;
            Debug.Log($"[Server] {(string)wca.invoker} wants to change the world to {wca.WorldInfo?.WorldCid}");

            IAvatarBrain sender = NetworkStatus.GetOnlineUser(invoker);

            // If we're in the offline mode, we _are_ the only user, the one which are the server admin
            // If we're in the server or host mode, anyone could attempt it to invoke a change.
            if (NetworkServer.active)
            {
                if (!Core.Utils.IsAbleTo(sender, UserCapabilities.CanInitiateWorldTransition, null))
                {
                    EmitToClientCTSPacket(new CTSMessage()
                    {
                        invoker = invoker,
                        message = "You are not allowed to switch worlds on this server."
                    }, sender);
                    yield break;
                }
            }

            yield return TransitionProgress.TransitionFrom();

            Cid WorldCid = null;
            string WorldName = null;
            if (wca.WorldInfo != null)
            {
                WorldCid = wca.WorldInfo.WorldCid;
                WorldName = wca.WorldInfo.WorldName;

                (AsyncOperationExecutor<Context> ao, Context co) =
                    WorldDownloader.PrepareGetWorldAsset(WorldCid);

                ao.ProgressChanged += TransitionProgress.Instance.OnProgressChanged;

                yield return ao.ExecuteCoroutine(co, (_ex, _co) => co = _co);

                if(co == null)
                {
                    EmitToClientCTSPacket(new CTSMessage()
                    {
                        invoker = invoker,
                        message = "Error in loading the world."
                    }, sender);

                    // Invalidate the world info, because we are in a transitional stage.
                    wca.WorldInfo = null;
                    WorldCid = null;
                    WorldName= null;
                }
            }

            yield return TransitionProgress.TransitionTo(WorldCid, WorldName);

            // If we're in offline mode, we're done it.
            // If we're in server or host mode, we need to announce the world change,
            // no matter we're successfully loaded or falling back to the offline world.
            // If we're in host mode, the message will be looped back, and sunk into /dev/null.
            if(NetworkServer.active)
                EmitCTSPacket(wca, null);
        }

        private IEnumerator MakeClientWorldTransition(CTSPWorldChangeAnnouncement wca)
        {
            OnlineLevel onlineLevel = NetworkStatus.GetOnlineLevel();
            if(onlineLevel == OnlineLevel.Host)
            {
                Debug.Log("[Client] Server wants to change the world, but we are in the host mode.");
                yield break;
            }

            // Only for the client mode, no offline or host mode
            yield return TransitionProgress.TransitionFrom();

            Cid WorldCid = null;
            string WorldName = null;
            if (wca.WorldInfo != null)
            {
                WorldCid = wca.WorldInfo.WorldCid;
                WorldName = wca.WorldInfo.WorldName;

                (AsyncOperationExecutor<Context> ao, Context co) =
                    WorldDownloader.PrepareGetWorldAsset(WorldCid);

                ao.ProgressChanged += TransitionProgress.Instance.OnProgressChanged;

                yield return ao.ExecuteCoroutine(co, (_ex, _co) => co = _co);

                if (co == null)
                {
                    yield return ShowDialogCoroutine($"Error in loading world '{WorldName}' by {(string)wca.invoker} - disconnecting");

                    // Invalidate the world info, because we are in a transitional stage.
                    wca.WorldInfo = null;
                    WorldCid = null;
                    WorldName = null;

                    // We're in the client mode, see above.
                    NetworkStatus.StopHost(false);
                }
            }

            yield return TransitionProgress.TransitionTo(WorldCid, WorldName);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Server User State updates (all)

        private async Task ServerUpdateUserState(UserID invoker, CTSPUpdateUserState uss)
        {
            uss.invoker = invoker;

            IAvatarBrain receiver = NetworkStatus.GetOnlineUser(uss.receiver);

            // Fill in the server-only data for the user record
            if (receiver != null)
            {
                if (uss.State.address != null)
                    uss.State.address = receiver.Address;

                if (uss.State.deviceUID != null)
                    uss.State.deviceUID = receiver.DeviceID;
            }

            if (NetworkServer.active)
            {
                IAvatarBrain sender = NetworkStatus.GetOnlineUser(invoker);

                UserCapabilities action = UserCapabilities.CanAdminServerUsers;

                if (UserState.IsBanned(uss.State.userState))
                {
                    action = UserCapabilities.CanBanUser;
                    uss.toDisconnect = true; // Of course.
                }
                else if (uss.toDisconnect)
                    action = UserCapabilities.CanKickUser;

                if (!Core.Utils.IsAbleTo(sender, action, receiver))
                    return;
            }

            SettingsManager.ServerUsers.RemoveUsers(uss.State);
            SettingsManager.ServerUsers.AddUser(uss.State);
            SettingsManager.ServerUsers.Save();

            // Admin list could have been changed
            // (Maybe a ban list, too, in future...)
            // so the server description has to be compiled and published.
            _ = IPFSService.FlipServerDescription(true);

            if (receiver != null)
            {
                // Leave out the details of the means of banning (IP, deviceID, ...)
                // to not to make ban evasion easier than it is.
                CTSPUpdateUserState cleaned = new()
                {
                    invoker = uss.invoker,
                    receiver = uss.receiver,
                    toDisconnect = uss.toDisconnect,
                    State = new()
                    {
                        userState = uss.State.userState,
                    }
                };

                // Tell the user what's going on.
                EmitCTSPacket(cleaned, receiver);

                // And, update the user's server-to-client SyncVar.
                receiver.UserState = uss.State.userState;

                // Disconnect if it's kicking at least.
                if (uss.toDisconnect)
                {
                    await Task.Delay(500);
                    NetworkIdentity nid = receiver.gameObject.GetComponent<NetworkIdentity>();
                    nid.connectionToClient.Disconnect();
                }
            }
        }

        private Task ClientInformUpdatedUserState(CTSPUpdateUserState uss)
        {
            if(uss.toDisconnect)
            {
                string reason = UserState.IsBanned(uss.State.userState)
                    ? "You've been banned from this server."
                    : "You've been kicked from this server.";

                // The server will disconnect this user anyway.
                ConnectionManager.DeliverDisconnectReason(reason);

                return Task.CompletedTask;
            }

            // Maybe some notification for promoting/demoting other users.

            return Task.CompletedTask;
        }

        #endregion
        // ---------------------------------------------------------------
        #region Server User State queries (all)

        private Task ServerQueryUserState(UserID invoker, STCUserInfo userInfoQuery)
        {
            IEnumerator EmitServerUserStateEntries(UserID invoker, IEnumerable<ServerUserState> q)
            {
                IAvatarBrain sender = NetworkStatus.GetOnlineUser(invoker);
                int i = 0;

                // toList() to freeze the collection's state because of concurrency issues
                // _between frames_
                foreach(ServerUserState serverUserState in q.ToList())
                {
                    // Give the server a breather...
                    if ((i++ % 20) == 0)
                    {
                        yield return new WaitForEndOfFrame();

                        // ... Phew. Are you still there?
                        sender = NetworkStatus.GetOnlineUser(invoker);

                        // User may have logged out, exit prematurely.
                        if (NetworkServer.active && sender == null) yield break;
                    }

                    // Tell the client the results, one at a time
                    EmitToClientCTSPacket(new STCUserInfo()
                    {
                        invoker = invoker,
                        UserID = serverUserState.userID,
                        State = serverUserState
                    }, sender);
                }
            }

            userInfoQuery.invoker = invoker;

            if (NetworkServer.active)
            {
                IAvatarBrain sender = NetworkStatus.GetOnlineUser(invoker);

                if (!Core.Utils.IsAbleTo(sender, UserCapabilities.CanAdminServerUsers, null))
                    return Task.CompletedTask;
            }

            // State as in the 'where' clause in SQL.
            IEnumerable<ServerUserState> q = SettingsManager.ServerUsers.FindUsers(userInfoQuery.State);

            StartCoroutine(EmitServerUserStateEntries(invoker, q));
            return Task.CompletedTask;
        }

        public event Action<UserID, ServerUserState> OnClientReceivedServerUserStateAnswer = null;

        private Task ClientQueryUserState(STCUserInfo userInfo)
        {
            // Whatever listened in the client's app, use the trampoline
            // to bounce the results to.
            OnClientReceivedServerUserStateAnswer?.Invoke(userInfo.UserID, userInfo.State);
            return Task.CompletedTask;
        }
        #endregion
    }
}
