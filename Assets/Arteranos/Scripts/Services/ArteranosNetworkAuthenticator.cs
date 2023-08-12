/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Net;
using Mirror;

using Arteranos.Core;
using UnityEngine;
using System.Threading.Tasks;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-authenticators
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/

namespace Arteranos.Services
{
    public class ArteranosNetworkAuthenticator : NetworkAuthenticator
    {
        #region Messages

        internal struct AuthGreetingMessage : NetworkMessage
        {
            public Version ServerVersion;
            public string ServerName;
            public byte[] ServerPublicKey;
        }

        internal struct AuthRequestPayload
        {
            public string Nickname;
            public bool isGuest;
            public bool isCustom;
            public string deviceUID;
        }

        internal struct AuthRequestMessage : NetworkMessage
        {
            public Version ClientVersion;
            public CryptPacket Payload;
        }

        internal struct AuthResponseMessage : NetworkMessage
        {
            public HttpStatusCode status;
            public string message;
            public ServerSettingsJSON ServerSettings;
        }

        #endregion

        #region Server

        /// <summary>
        /// Called on server from StartServer to initialize the Authenticator
        /// <para>Server message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStartServer() =>
            // register a handler for the authentication request we expect from client
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);

        /// <summary>
        /// Called on server from StopServer to reset the Authenticator
        /// <para>Server message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStopServer() =>
            // unregister the handler for the authentication request
            NetworkServer.UnregisterHandler<AuthRequestMessage>();

        /// <summary>
        /// Called on server from OnServerConnectInternal when a client needs to authenticate
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        public override void OnServerAuthenticate(NetworkConnectionToClient conn) 
        {
            // Take the first step to authenticate.
            ServerSettings ss = SettingsManager.Server;

            AuthGreetingMessage authGreetingMessage = new()
            {
                ServerVersion = Version.Load(),
                ServerName = ss.Name,
                ServerPublicKey = ss.ServerPublicKey
            };

            conn.Send(authGreetingMessage);
        }

        /// <summary>
        /// Called on server when the client's AuthRequestMessage arrives
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        /// <param name="msg">The message payload</param>
        private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
            => Task.Run(() =>
            {
                try
                {
                    DecideAuthenthicity(conn, msg);
                }
                catch(System.Exception e)
                {
                    Debug.LogException(e);
                    ServerReject(conn);
                }
            });

        private void DecideAuthenthicity(NetworkConnectionToClient conn, AuthRequestMessage encryptedMsg)
        {
            // DEBUG
            // System.Threading.Thread.Sleep(3000);

            // if(connectionsPendingDisconnect.Contains(conn)) return;

            ServerSettings ss = SettingsManager.Server;

            ss.Decrypt(encryptedMsg.Payload, out AuthRequestPayload msg);

            AuthResponseMessage authResponseMessage;

            if(!encryptedMsg.ClientVersion.IsGE(Version.VERSION_MIN))
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
                    message = $"Guests are disallowed.\nPlease login yourself to a login provider of your choice.",
                };
            }
            else if(msg.isCustom && !(SettingsManager.Server.Permissions.CustomAvatars ?? false))
            {
                authResponseMessage = new()
                {
                    status = HttpStatusCode.Unauthorized,
                    message = $"Custom avatars are disallowed.\nPlease use an avatar generated by the offered avatar provider.",
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

            authResponseMessage.ServerSettings = SettingsManager.Server.Strip();

            conn.Send(authResponseMessage);

            bool accepted = authResponseMessage.status == HttpStatusCode.OK;

            Debug.Log($"{(accepted ? "Accept" : "Reject")}ed connection from {conn.address}");
            if(!accepted)
                Debug.Log($"  Reason: {authResponseMessage.status}, Message:\n{authResponseMessage.message}");

            if(accepted)
                ServerAccept(conn);
            else
                ServerReject(conn);
        }

        #endregion

        #region Client

        /// <summary>
        /// Called on client from StartClient to initialize the Authenticator
        /// <para>Client message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStartClient()
        {
            // register a handler for the server's initial greeting message
            NetworkClient.RegisterHandler<AuthGreetingMessage>(OnAuthGreetingMessage, false);

            // register a handler for the authentication response we expect from server
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
        }

        /// <summary>
        /// Called on client from StopClient to reset the Authenticator
        /// <para>Client message handlers should be unregistered in this method.</para>
        /// </summary>
        public override void OnStopClient()
        {
            // unregister a handler for the server's initial greeting message
            NetworkClient.UnregisterHandler<AuthGreetingMessage>();

            // unregister the handler for the authentication response
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
        }

        /// <summary>
        /// Called on client from OnClientConnectInternal when a client needs to authenticate
        /// </summary>
        public override void OnClientAuthenticate()
        {
            // Do nothing. Until we got the server's greeting message, we cennot do anything.
        }

        /// <summary>
        /// Called on client when the server's initial greeting message arrives
        /// </summary>
        /// <param name="msg">Self explanatory</param>
        private void OnAuthGreetingMessage(AuthGreetingMessage msg)
        {
            Debug.Log($"[Client] Server name   : {msg.ServerName}");
            Debug.Log($"[Client] Server version: {msg.ServerVersion.Full}");

            ClientSettings cs = SettingsManager.Client;

            Crypto.Encrypt(new AuthRequestPayload()
            {
                Nickname = cs.Me.Nickname,
                isGuest = cs.Me.Login.IsGuest,
                isCustom = cs.Me.CurrentAvatar.IsCustom,
                deviceUID = SystemInfo.deviceUniqueIdentifier
            }, msg.ServerPublicKey, out CryptPacket p);

            NetworkClient.Send(new AuthRequestMessage()
            {
                ClientVersion = Version.Load(),
                Payload = p
            });
        }

        /// <summary>
        /// Called on client when the server's AuthResponseMessage arrives
        /// </summary>
        /// <param name="msg">The message payload</param>
        private void OnAuthResponseMessage(AuthResponseMessage msg)
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
            {
                SettingsManager.CurrentServer = msg.ServerSettings;
                // Authentication has been accepted
                ClientAccept();
            }
        }

        #endregion
    }
}
