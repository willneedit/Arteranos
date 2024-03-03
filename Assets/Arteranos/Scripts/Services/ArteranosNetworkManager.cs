using UnityEngine;
using Mirror;
using Arteranos.Core;
using Arteranos.Avatar;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Arteranos.UI;
using Ipfs;
using Arteranos.Core.Operations;
using Ipfs.Core.Cryptography.Proto;
using Arteranos.Core.Cryptography;
using System.Collections;

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

                _ = EmitToClientCurrentWorld(conn, brain.AgreePublicKey);
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

            SettingsManager.WorldInfoCid = null;

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
        public async Task EmitToClientCurrentWorld(NetworkConnectionToClient conn, PublicKey agreePublicKey)
        {
            IEnumerator SendWCAJustForTheLaggard(NetworkConnectionToClient conn, PublicKey agreePublicKey, WorldInfo wi)
            {
                yield return null;

                EmitToClientCTSPacket(new CTSPWorldChangeAnnouncement()
                {
                    // WorldCid = wi?.WorldCid,
                    WorldInfo = wi,
                }, conn, agreePublicKey);
            }

            WorldInfo wi = await WorldInfo.RetrieveAsync(SettingsManager.WorldInfoCid);

            Debug.Log($"[Server] sending world CID '{SettingsManager.WorldInfoCid}' to latecoming conn {conn.connectionId}");

            SettingsManager.StartCoroutineAsync(() => SendWCAJustForTheLaggard(conn, agreePublicKey, wi));
        }



        #endregion
        // ---------------------------------------------------------------
        #region CTS Packets (Communication)

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

        public void EmitToClientCTSPacket(CTSPacket packet, IAvatarBrain to = null)
        {
            NetworkConnectionToClient connectionToClient = null;
            if (to != null)
                connectionToClient = to.gameObject.GetComponent<NetworkIdentity>().connectionToClient;

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

        #endregion
        // ---------------------------------------------------------------
        #region CTS Packets (on Server)

        // Entry point of the server side and offline mode
        public void ServerLocalCTSPacket(UserID sender, CTSPacket packet)
        {
            if(packet is CTSPWorldChangeAnnouncement wca) 
                _ = MakeWorldTransition(sender, true, wca);
            else
                throw new NotImplementedException();
        }

        #endregion
        // ---------------------------------------------------------------
        #region CTS Packets (on Client)

        // Entry point of the client side and offline mode
        private void ClientLocalCTSPacket(CTSPacket packet)
        {
            if(packet is CTSPWorldChangeAnnouncement wca)
                _ = MakeWorldTransition(null, false, wca);
            else
                throw new NotImplementedException();
        }
        #endregion
        // ---------------------------------------------------------------
        #region World Teansition (all)

        // Invoked by the owner by EmitToServerCTSPacket()
        // Stage 1: Server or host - check privilege and server's permissions, transition.
        // Stage 2: Client - transition (if not same as in host)
        private async Task MakeWorldTransition(UserID invoker, bool inServer, CTSPWorldChangeAnnouncement changeAnnouncement)
        {

            IEnumerator ShowDialogCoroutine(string message)
            {
                XR.ScreenFader.StartFading(0.0f);

                yield return null;

                IDialogUI dialog = DialogUIFactory.New();
                dialog.Text = message;
                dialog.Buttons = new string[] { "OK" };
            }

            IEnumerator EmitCoroutine(CTSPWorldChangeAnnouncement changeAnnouncement, IAvatarBrain to)
            {
                yield return null;

                EmitToClientCTSPacket(changeAnnouncement, to);
            }

            void StopWorldAction(string message)
            {
                // In any reason, unfade the screen
                XR.ScreenFader.StartFading(0.0f);

                // If we're not interactive, tell the client what's going on.
                if (NetworkStatus.GetOnlineLevel() == OnlineLevel.Server)
                {
                    changeAnnouncement.Message = message;
                    Debug.Log($"[Server] {message}");
                    PropagateWorldTransition();
                }
                else
                    SettingsManager.StartCoroutineAsync(() => ShowDialogCoroutine(message));
            }

            void PropagateWorldTransition()
            {
                IAvatarBrain to = null;
                if (changeAnnouncement.Message != null)
                    to = NetworkStatus.GetOnlineUser(invoker);

                SettingsManager.StartCoroutineAsync(() => EmitCoroutine(changeAnnouncement, to));
            }

            bool WorldNeedsPreloasding()
            {
                try
                {
                    if (changeAnnouncement.WorldInfo == null) return false; // It's the offline embedded world.

                    if (string.IsNullOrEmpty(WorldDownloader.GetWorldABF(changeAnnouncement.WorldInfo.WorldCid))) return true;

                    return false;
                }
                catch { return true; }

                // NOTREACHED
            }

            async Task<bool> PreloadWorld()
            {
                (Exception ex, Context _) = await WorldTransition.PreloadWorldDataAsync(changeAnnouncement.WorldInfo.WorldCid);

                if (ex != null)
                {
                    StopWorldAction(ex.Message);
                    return false;
                }

                return true;
            }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

            OnlineLevel onlineLevel = NetworkStatus.GetOnlineLevel();

            // It isn't the transition. Only the server telling what went wrong.
            if (changeAnnouncement.Message != null)
            {
                Debug.Log($"[Server to you] {changeAnnouncement.Message}");
                SettingsManager.StartCoroutineAsync(() => ShowDialogCoroutine(changeAnnouncement.Message));
                return;
            }

            if (inServer)
            {
                changeAnnouncement.invoker = invoker;
                Debug.Log($"[Server] {(string)changeAnnouncement.invoker} wants to change the world to {changeAnnouncement.WorldInfo?.WorldCid}");

                // If we're in the offline mode, we _are_ the only user, the one which are the server admin
                // If we're in the server or host mode, anyone could attempt it to invoke a change.
                if (NetworkServer.active)
                {
                    IAvatarBrain sender = NetworkStatus.GetOnlineUser(invoker);

                    if (sender == null || !sender.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null))
                    {
                        StopWorldAction("You're not allowed to change the server's world.");
                        return;
                    }
                }

                if(changeAnnouncement.WorldInfo != null)
                {
                    if (WorldNeedsPreloasding() && !(await PreloadWorld())) return;

                    // Server checks if the world's content rating goes along with its own permissions
                    if (changeAnnouncement.WorldInfo.ContentRating != null
                        && (changeAnnouncement.WorldInfo.ContentRating).IsInViolation(SettingsManager.Server.Permissions))
                    {
                        StopWorldAction("World is in violation of the server's content permission");
                        return;
                    }
                }

                if (SettingsManager.WorldInfoCid == changeAnnouncement.WorldInfo.WorldInfoCid)
                {
                    StopWorldAction("Already in the current world.");
                    return;
                }
            }
            else
            {
                Debug.Log($"[Client] {(string)changeAnnouncement.invoker} wants to change the world to {changeAnnouncement.WorldInfo?.WorldCid}");

                if (WorldNeedsPreloasding() && !(await PreloadWorld())) return;
            }

            // In Stage 1: We're really in the current world.
            // In Stage 2: In Host mode, the announcement came looped back in Stage 1
            if (SettingsManager.WorldInfoCid == changeAnnouncement.WorldInfo.WorldInfoCid)
            {
                if (onlineLevel == OnlineLevel.Client)
                    StopWorldAction("Already in the current world.");
                else
                    Debug.Log("Host or offline mode, we're already here.");


                if(onlineLevel != OnlineLevel.Host && onlineLevel != OnlineLevel.Offline)
                    XR.ScreenFader.StartFading(0.0f, 2.0f);

                return;
            }

            await WorldTransition.MoveToOnlineWorld(changeAnnouncement.WorldInfo?.WorldCid);

            // When we're done this, order everyone the transition, or the invoker what went wrong.
            if (inServer) PropagateWorldTransition();
        }
        #endregion
    }
}
