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
using System.Collections.Generic;
using System;
using Version = Arteranos.Core.Version;
using Random = UnityEngine.Random;
using System.Collections;
using System.Linq;
using Arteranos.UI;

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
            public byte[] ClientChallenge;
            public int SequenceNumber;
        }

        internal struct AuthRequestPayload
        {
            public int SequenceNumber;
            public byte[] ClientResponse;
            public byte[] ClientPublicKey;

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
            public byte[] ClientPubKeySig;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get (not necessarily cryptographically strong) random bytes for the challenge
        /// </summary>
        /// <param name="numBytes">How many bytes to get</param>
        public byte[] GetChallengeBytes(int numBytes)
        {
            byte[] challengeBytes = new byte[numBytes];

            for(int i = 0; i < challengeBytes.Length; i++)
                challengeBytes[i] = (byte) Random.Range(0, 255);

            return challengeBytes;
        }

        #endregion

        #region Start & Stop

        public override void OnStartServer()
        {
            // register a handler for the authentication request we expect from client
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);

            StartCoroutine(ScrubChallenges());
        }

        public override void OnStopServer()
        {
            // unregister the handler for the authentication request
            NetworkServer.UnregisterHandler<AuthRequestMessage>();

            StopAllCoroutines();
        }

        public override void OnStartClient()
        {
            // register a handler for the server's initial greeting message
            NetworkClient.RegisterHandler<AuthGreetingMessage>(OnAuthGreetingMessage, false);

            // register a handler for the authentication response we expect from server
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
        }

        public override void OnStopClient()
        {
            // unregister a handler for the server's initial greeting message
            NetworkClient.UnregisterHandler<AuthGreetingMessage>();

            // unregister the handler for the authentication response
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
        }

        #endregion

        #region Server

        internal struct ChallengeListEntry
        {
            public byte[] challenge;
            public DateTime time;
        }

        private readonly Dictionary<int, ChallengeListEntry> ChallengeList = new();

        // Every two seconds, look for the outdated login attempts to remove them.
        private IEnumerator ScrubChallenges()
        {
            while(true)
            {
                yield return new WaitForSeconds(2);

                DateTime now = DateTime.Now;

                int[] numbers = (from entry in ChallengeList
                                 where entry.Value.time < now
                                 select entry.Key).ToArray();

                foreach(int number in numbers)
                    ChallengeList.Remove(number);
            }
        }

        /// <summary>
        /// Called on server from OnServerConnectInternal when a client needs to authenticate
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        public override void OnServerAuthenticate(NetworkConnectionToClient conn) 
        {
            // Flooded. Too many login attempts at the same time.
            if(ChallengeList.Count > 12000)
            {
                Debug.LogWarning($"Login attempt flood: Discarding connection from {conn.address}");
                // conn.Disconnect();
                return;
            }
                
            int sequence = Random.Range(0, 1000000);
            while(ChallengeList.TryGetValue(sequence, out _))
                sequence = Random.Range(0, 1000000);

            // Allow sixty seconds between the greeting message and the client's reaction.
            ChallengeListEntry entry = new()
            {
                challenge = GetChallengeBytes(32),
                time = DateTime.Now + TimeSpan.FromSeconds(60),
            };

            // Take the first step to authenticate.
            ServerSettings ss = SettingsManager.Server;

            AuthGreetingMessage authGreetingMessage = new()
            {
                ServerVersion = Version.Load(),
                ServerName = ss.Name,
                ServerPublicKey = ss.ServerPublicKey,
                ClientChallenge = entry.challenge,
                SequenceNumber = sequence
            };

            conn.Send(authGreetingMessage);

            ChallengeList.Add(sequence, entry);
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
                catch(Exception e)
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
            else if(!ChallengeList.TryGetValue(msg.SequenceNumber, out ChallengeListEntry ce))
            {
                // Authentication attempt either out-of-band or expired. Or it's a replay attack.
                authResponseMessage = new()
                {
                    status = HttpStatusCode.RequestTimeout,
                    message = $"Request timed out",
                };
            }
            else if(!Crypto.Verify(ce.challenge, msg.ClientResponse, msg.ClientPublicKey))
            {
                // Fake Client Public Key, Challenge signing failed, ...
                authResponseMessage = new()
                {
                    status = HttpStatusCode.Unauthorized,
                    message = $"Signature check failed.",
                };
            }
            else if(msg.isGuest && !(SettingsManager.Server.Permissions.Guests ?? false))
            {
                authResponseMessage = new()
                {
                    status = HttpStatusCode.Forbidden,
                    message = $"Guests are disallowed.\nPlease login yourself to a login provider of your choice.",
                };
            }
            else if(msg.isCustom && !(SettingsManager.Server.Permissions.CustomAvatars ?? false))
            {
                authResponseMessage = new()
                {
                    status = HttpStatusCode.Forbidden,
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

                // Add the server settings and the signature of the client's public key.
                ss.Sign(msg.ClientPublicKey, out authResponseMessage.ClientPubKeySig);
                authResponseMessage.ServerSettings = SettingsManager.Server.Strip();
            }

            // Anyway, expire the thallenge.
            ChallengeList.Remove(msg.SequenceNumber);


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
            string address = NetworkClient.connection.address;
            Debug.Log($"[Client] Server address: {address}");
            Debug.Log($"[Client] Server name   : {msg.ServerName}");
            Debug.Log($"[Client] Server version: {msg.ServerVersion.Full}");

            ClientSettings cs = SettingsManager.Client;

            if(!cs.ServerKeys.TryGetValue(address, out byte[] pubKey))
            {
                // New server encountered. Yay!
                cs.ServerKeys.Add(address, msg.ServerPublicKey);
                SubmitAuthRequest(msg);
                cs.SaveSettings();
                return;
            }
            else if(msg.ServerPublicKey.SequenceEqual(pubKey))
            {
                // Known server, everything is okay.
                SubmitAuthRequest(msg);
                return;
            }
            else
            {
                // Something is very, very wrong....!
                IDialogUI dialog = DialogUIFactory.New();

                dialog.Text =
                    "!! SERVER KEY CHANGED !!\n\n" +
                    "Maybe someone is doing something\n" +
                    "very nasty!";

                dialog.Buttons = new[]
                {
                    "Go offline",
                    "Accept once",
                    "Accept"
                };

                dialog.OnDialogDone += (rc) => OnHostKeyChangedResponse(rc, msg);
            }
        }

        private void OnHostKeyChangedResponse(int rc, AuthGreetingMessage msg)
        {
            if(rc == 0)
            {
                ClientReject();
                return;
            }

            if(rc == 2)
            {
                ClientSettings cs = SettingsManager.Client;
                string address = NetworkClient.connection.address;

                cs.ServerKeys.Remove(address);
                cs.ServerKeys.Add(address, msg.ServerPublicKey);
                cs.SaveSettings();
            }

            SubmitAuthRequest(msg);
        }

        private static void SubmitAuthRequest(AuthGreetingMessage msg)
        {
            ClientSettings cs = SettingsManager.Client;

            cs.Sign(msg.ClientChallenge, out byte[] response);

            Crypto.Encrypt(new AuthRequestPayload()
            {
                SequenceNumber = msg.SequenceNumber,
                ClientResponse = response,
                ClientPublicKey = cs.UserPublicKey,

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
                IDialogUI dialog = DialogUIFactory.New();

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
