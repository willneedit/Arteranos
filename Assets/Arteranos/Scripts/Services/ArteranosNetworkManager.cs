using UnityEngine;
using Mirror;
using Arteranos.Core;
using Arteranos.Avatar;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

using DERSerializer;
using System.Threading.Tasks;
using Arteranos.Web;
using Arteranos.XR;
using Arteranos.UI;
using Ipfs;
using Arteranos.Core.Operations;

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
            public ArteranosNetworkAuthenticator.AuthResponseMessage response;
        }

        internal struct WorldChangeAnnounceMessage : NetworkMessage
        {
            public string Invoker;
            public string WorldInfoCidString;
            public string Message;
        }

        // Overrides the base singleton so we don't
        // have to cast to this type everywhere.
        public static ArteranosNetworkManager Instance { get; private set; }

        private readonly Dictionary<int, AuthSequence> ResponseMessages = new();

        private readonly Dictionary<int, DateTime> SCLastUpdatedToClient = new();

        private DateTime SCSnapshotCutoff = DateTime.MinValue;

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
            SCSnapshotCutoff = DateTime.MinValue;
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

                EmitToClientCurrentWorld(conn);
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
        }

        /// <summary>
        /// This is invoked when the client is started.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            SCSnapshotCutoff = DateTime.MinValue;

            NetworkClient.RegisterHandler<WorldChangeAnnounceMessage>(OnClientGotWCA);
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
        }

        /// <summary>
        /// This is called when a client is stopped.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();

            NetworkClient.UnregisterHandler<WorldChangeAnnounceMessage>();

        }

        #endregion

        #region World Change Announcements

        [Server]
        public async Task EmitToClientsWCAAsync(string invoker, Cid WorldCid)
        {
            // First, download, do the consistency checks and set up the server's idea
            // of the world.
            Exception ex = await WorldTransition.VisitWorldAsync(WorldCid);

            string message = null;

            // Something have gone wrong - the server is already in the offline world. Tell
            // the clients about the changed direction.
            if (ex != null)
            {
                WorldCid = null;
                message = "Error in loading the world.";
            }

            WorldInfo wi = WorldInfo.DBLookup(WorldCid);

            WorldChangeAnnounceMessage wca = new()
            {
                Invoker = invoker,
                WorldInfoCidString = wi.WorldInfoCid,
                Message = message,
            };

            Debug.Log("[Server] Announcing the clients about the changed world.");
            NetworkServer.SendToAll(wca);
        }

        [Server]
        public void EmitToClientCurrentWorld(NetworkConnectionToClient conn)
        {
            WorldChangeAnnounceMessage wca = new()
            {
                Invoker = null,
                WorldInfoCidString = SettingsManager.WorldInfoCid
            };

            Debug.Log($"[Server] sending worldURL '{SettingsManager.WorldInfoCid}' to latecoming conn {conn.connectionId}");
            conn.Send(wca);
        }

        [Client]
        private async void OnClientGotWCA(WorldChangeAnnounceMessage message)
        {
            Debug.Log($"[Client] Server announce: Changed world from {SettingsManager.WorldInfoCid} to {message.WorldInfoCidString} by {message.Invoker}");
            
            if(!string.IsNullOrEmpty(message.Message))
            {
                IDialogUI dialog = DialogUIFactory.New();
                dialog.Text = message.Message;
                dialog.Buttons = new string[] { "OK" };
            }

            // We've already moved to the target world as the server, so no need to
            // hassle ourselves.
            if (NetworkStatus.GetOnlineLevel() == OnlineLevel.Host)
            {
                Debug.Log($"[Client] ... but we're in the host mode, we're already in it.");
                return;
            }

            WorldInfo wi = await WorldInfo.RetrieveAsync(message.WorldInfoCidString);

            Debug.Log($"[Client] Info={message.WorldInfoCidString} is world {wi.WorldCid}. Dragging your client along.");
            _ = await WorldTransition.VisitWorldAsync(wi.WorldCid);
        }


        #endregion
    }
}
