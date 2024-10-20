﻿/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using Arteranos.UI;
using Ipfs;
using System.Net;
using System.Threading;
using System.Linq;

namespace Arteranos.Services
{
    public class ConnectionManager : MonoBehaviour, IConnectionManager
    {
        private void Awake() => G.ConnectionManager = this;

        public IEnumerator ConnectToServer(MultiHash PeerID, Action<bool> callback)
        {

            bool? result = null;

            if (G.NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline)
            {
                // Anything but idle, cut off all connections before connecting to the desired server.
                Task t0 = G.NetworkStatus.StopHost(false);

                yield return new WaitUntil(() => t0.IsCompleted);
            }

            ServerInfo si = new(PeerID);

            Factory.NewAgreement(si, () => result = false, () => result = true);

            yield return new WaitUntil(() => result != null);

            // User denied privacy agreement, backing out.
            if (result == false)
            {
                callback?.Invoke(false);
                yield break;
            }

            yield return CommenceConnection(si, _result => result = _result);

            callback?.Invoke(result.Value);
        }

        private IEnumerator CommenceConnection(ServerInfo si, Action<bool> callback)
        {
            // In any case, go into the transitional phase.
            yield return TransitionProgress.TransitionFrom();

            G.TransitionProgress?.OnProgressChanged(0.10f, "Searching server");

            if(!si.IPAddresses.Any())
            {
                ConnectionResponse(false, "Server reports no IP address");
                yield return TransitionProgress.TransitionTo(null);
                yield break;
            }

            // Save it for now even before the connection negotiation and authentication
            // During the remainder of the ongoing frame, we have to have the PeerID available --
            // the complete login sequence can be commenced before WaitForEndOfFrame() let us continue.
            G.NetworkStatus.RemotePeerId = si.PeerID;

            int i = 0;
            foreach (IPAddress addr in si.IPAddresses)
            {
                G.TransitionProgress?.OnProgressChanged(0.50f, $"Connecting ({++i})...");

                // TODO Needs cleanup!
                G.NetworkStatus.OnClientConnectionResponse = null;

                // FIXME Telepathy Transport specific.
                Uri connectionUri = addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                    ? new($"tcp4://[{addr}]:{si.ServerPort}")
                    : new($"tcp4://{addr}:{si.ServerPort}");

                G.NetworkStatus.StartClient(connectionUri);

                // https://www.youtube.com/watch?v=dQw4w9WgXcQ
                while (G.NetworkStatus.IsClientConnecting) yield return new WaitForEndOfFrame();

                if(G.NetworkStatus.IsClientConnected)
                    break;
            }

            // Client failed to connect. Maybe an invalid IP, or a misconfigured firewall.
            // Fall back to the offline world.
            if (!G.NetworkStatus.IsClientConnected)
            {
                ConnectionResponse(false, "Cannot connect to server");
                callback?.Invoke(false);
                yield return TransitionProgress.TransitionTo(null);
                yield break;
            }

            ExpectConnectionResponse();
            callback?.Invoke(true);
            // Just-connected server will tell which world we're going to.
            // If the server is borked, the disconnecting server will cause the client to
            // fall back to the offline world.
        }

        public void ExpectConnectionResponse()
        {
            G.NetworkStatus.OnClientConnectionResponse = ConnectionResponse;
        }

        bool wasOnline = false;
        string reason = null;

        private void ConnectionResponse(bool success, string message)
        {
            Debug.Log($"Onlinelevel={G.NetworkStatus.GetOnlineLevel()}, Success={success}, wasOnline={wasOnline}, reason={reason}");

            if (!success)
            {
                bool remoteDisconnected = G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Client
                    || G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Host;

                if (reason != null)
                {
                    message = reason;
                    reason = null;
                }
                else if (message == null)
                {
                    // Only if you're disconnecting of your own volition
                    if (remoteDisconnected && wasOnline) message = "Disconnected from the server.";
                    // Only if we were never online in the first place
                    else if (!wasOnline) message = "Failed to connect to this server.";
                }
            }
            wasOnline = success;
            // G.NetworkStatus.OnClientConnectionResponse = null;

            if (message == null) return;

            IDialogUI dialog = Factory.NewDialog();
            dialog.Buttons = new[] { "Okay" };
            dialog.Text = message;
        }

        public void DeliverDisconnectReason(string reason) => this.reason = reason;
    }
}
