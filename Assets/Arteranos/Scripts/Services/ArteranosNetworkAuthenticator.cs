using System.Net;
using Mirror;

using Arteranos.Core;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-authenticators
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/

namespace Arteranos.Services
{
    public class ArteranosNetworkAuthenticator : NetworkAuthenticator
    {
        #region Messages

        public struct AuthRequestMessage : NetworkMessage
        {
            public Version ClientVersion;
            public string Nickname;
            public byte[] ClientUserHash;
            public bool isGuest;
            public bool isCustom;
        }

        public struct AuthResponseMessage : NetworkMessage
        {
            public HttpStatusCode status;
            public string message;
        }

        #endregion

        #region Server

        /// <summary>
        /// Called on server from StartServer to initialize the Authenticator
        /// <para>Server message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStartServer()
        {
            // register a handler for the authentication request we expect from client
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
        }

        /// <summary>
        /// Called on server from StopServer to reset the Authenticator
        /// <para>Server message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStopServer()
        {
            // unregister the handler for the authentication request
            NetworkServer.UnregisterHandler<AuthRequestMessage>();
        }

        /// <summary>
        /// Called on server from OnServerConnectInternal when a client needs to authenticate
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }

        /// <summary>
        /// Called on server when the client's AuthRequestMessage arrives
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        /// <param name="msg">The message payload</param>
        public void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
        {
            // if(connectionsPendingDisconnect.Contains(conn)) return;

            AuthResponseMessage authResponseMessage;

            if(!msg.ClientVersion.IsGE(Version.VERSION_MIN))
            {
                // Insufficient version
                authResponseMessage = new()
                {
                    status = HttpStatusCode.NotAcceptable,
                    message = $"Your client is outdated.\nVersion {Version.VERSION_MIN} required,\nPlease update.",
                };
            }
            else if(msg.isGuest && !(SettingsManager.Server.Permissions.Guests ?? false))
            {
                authResponseMessage = new()
                {
                    status = HttpStatusCode.Unauthorized,
                    message = $"Guests are disallowed.\nPlease login yourself to a login provider of your choice."
                };
            }
            else if(msg.isCustom && !(SettingsManager.Server.Permissions.CustomAvatars ?? false))
            {
                authResponseMessage = new()
                {
                    status = HttpStatusCode.Unauthorized,
                    message = $"Custom avatars are disallowed.\nPlease use an avatar generated by the offered avatar provider."
                };
            }
            else
            {
                // All checks pan out.
                authResponseMessage = new()
                {
                    status = HttpStatusCode.OK,
                    message = "OK",
                };
            }


            conn.Send(authResponseMessage);

            if(authResponseMessage.status == HttpStatusCode.OK)
            {

                // Accept the successful authentication
                // TODO move the registering of the server user hash from the AvatarBrain
                ServerAccept(conn);
            }
            else
            {
                ServerReject(conn);
            }
        }

        #endregion

        #region Client

        /// <summary>
        /// Called on client from StartClient to initialize the Authenticator
        /// <para>Client message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStartClient()
        {
            // register a handler for the authentication response we expect from server
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
        }

        /// <summary>
        /// Called on client from StopClient to reset the Authenticator
        /// <para>Client message handlers should be unregistered in this method.</para>
        /// </summary>
        public override void OnStopClient()
        {
            // unregister the handler for the authentication response
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
        }

        /// <summary>
        /// Called on client from OnClientConnectInternal when a client needs to authenticate
        /// </summary>
        public override void OnClientAuthenticate()
        {
            ClientSettings cs = SettingsManager.Client;

            AuthRequestMessage authRequestMessage = new()
            {
                ClientVersion = Core.Version.Load(),
                Nickname = cs.Me.Nickname,
                isGuest = cs.Me.Login.IsGuest,
                isCustom = cs.Me.CurrentAvatar.IsCustom,
                ClientUserHash = cs.UserHash // TODO Derive the server user hash
            };

            NetworkClient.Send(authRequestMessage);
        }

        /// <summary>
        /// Called on client when the server's AuthResponseMessage arrives
        /// </summary>
        /// <param name="msg">The message payload</param>
        public void OnAuthResponseMessage(AuthResponseMessage msg)
        {
            if(msg.status != HttpStatusCode.OK)
            {
                UI.IDialogUI dialog = UI.DialogUIFactory.New();

                dialog.Buttons = new string[] { "OK" };
                dialog.Text = $"Cannot connect.\n\nServer says:\n{msg.message}";

                // Just only some formality, the server will disconnect anyway.
                ClientReject();
            }
            else
                // Authentication has been accepted
                ClientAccept();
        }

        #endregion
    }
}
