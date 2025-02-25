/*
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
using System.Linq;
using Mirror;
using System.Collections.Generic;
using System.IO;

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
            IEnumerator TryConnectLoop(IEnumerable<IPAddress> addrs)
            {
                // Get the scheme as we use it for outgoing connections as
                // same as we use it ourselves as a server.
                // Maybe to use the scheme same as with the port in the ServerDescription...?
                string scheme = GetTransportScheme();

                // Save it for now even before the connection negotiation and authentication
                // During the remainder of the ongoing frame, we have to have the PeerID available --
                // the complete login sequence can be commenced before WaitForEndOfFrame() let us continue.
                G.NetworkStatus.RemotePeerId = si.PeerID;

                int i = 0;
                foreach (IPAddress addr in addrs)
                {
                    G.TransitionProgress?.OnProgressChanged(0.50f, $"Connecting ({++i})...");

                    G.NetworkStatus.OnClientConnectionResponse = null;

                    Uri connectionUri = addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                        ? new($"{scheme}://[{addr}]:{si.ServerPort}")
                        : new($"{scheme}://{addr}:{si.ServerPort}");

                    G.NetworkStatus.StartClient(connectionUri);

                    // https://www.youtube.com/watch?v=dQw4w9WgXcQ
                    while (G.NetworkStatus.IsClientConnecting) yield return new WaitForEndOfFrame();

                    if (G.NetworkStatus.IsClientConnected)
                    {
                        // Success! Remember where we got so far.
                        si.UpdateLastUsedIPAddress(addr);
                        break;
                    }
                }
            }

            Transport transport = Transport.active;

            if (transport is INatPunchAddon ina)
            {
                ina.ClientNeedsNatPunch = si.IsFirewalled;
                Debug.Log($"Emabling NAT punching addon: {ina.ClientNeedsNatPunch}");

                ina.OnInitiatingNatPunch = t => Ina_OnInitiatingNatPunch(t, si.PeerID, ina);
            }
            else
                Debug.Log("NAT punching is not supported with this transport");

            string GetTransportScheme()
            {
                return transport switch
                {
                    TelepathyTransport => "tcp4",
                    LiteNetLibTransport => "litenet",
                    _ => throw new InvalidOperationException("Unrecognized transport layer")
                };
            }

            // In any case, go into the transitional phase.
            yield return TransitionProgress.TransitionFrom();

            // Attempt quick connect with its last successful connection.
            if(si.LastUsedIPAddress != null)
            {
                List<IPAddress> addrs = new() { IPAddress.Parse(si.LastUsedIPAddress) };

                yield return TryConnectLoop(addrs);
            }

            // If wi didn't connect so far (e.g. server changed its location from the last time)
            // fall back with the IP addresses from the Online data
            if(!G.NetworkStatus.IsClientConnected)
            {
                // Server may be known, e.g. from commandline or previous sessions, but
                // may not announced itself, or went offline. Wait for its connectivity.
                int retrySeconds;
                for (retrySeconds = 120; retrySeconds > 0; retrySeconds--)
                {
                    G.TransitionProgress?.OnProgressChanged(0.10f, $"Searching server ({retrySeconds}s left)");

                    if (si.IPAddresses != null && si.IPAddresses.Any()) break;

                    yield return new WaitForSeconds(1);
                    si = new(si.PeerID);
                }

                // Well, we've tried...
                if (retrySeconds <= 0)
                {
                    ConnectionResponse(false, "Server reports no IP address");
                    yield return TransitionProgress.TransitionTo(null);
                    yield break;
                }

                yield return TryConnectLoop(si.IPAddresses);
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

        private void Ina_OnInitiatingNatPunch(IPEndPoint target, MultiHash peerID, INatPunchAddon ina)
        {
            // First IPv4: External IP, beyond NAT
            // First IPv6: Well....
            IEnumerable<IPAddress> possible =
                from ipadddr in (List<IPAddress>)G.NetworkStatus.IPAddresses
                where ipadddr.AddressFamily == target.AddressFamily
                select ipadddr;

            Guid tokenGuid = Guid.NewGuid();

            IEnumerable<ServerInfo> possibleRelays = from entry in ServerInfo.Dump()
                                                     where entry.IsOnline && !entry.IsFirewalled
                                                     select entry;

            List<ServerInfo> relayList = possibleRelays.ToList();

            string token = tokenGuid.ToString();
            IPEndPoint relayEP = new(IPAddress.None, 0);
            IPEndPoint clientEP = new(IPAddress.None, 0);

            if (relayList.Count != 0)
            {
                // Pick one... 
                ServerInfo relaySI = relayList[UnityEngine.Random.Range(0, relayList.Count)];
                relayEP = new(relaySI.IPAddresses.ToList().First(), relaySI.ServerPort);
                // ... and try relayed NAT punching
                ina.InitiateNatPunch(relayEP, token);
            }
            else if(!possible.Any())
            {
                Debug.LogWarning("Target is unreachable (IPv6 vs. IPv4)");
                return;
            }      
            else
            {
                clientEP = new(possible.First(), ina.LocalPort);
                Debug.Log($"No viable relays, trying relayless with {clientEP}...");
            }

            // Notify server via IPFS, too, to make it work in tandem
            NatPunchRequestData nprd = new()
            {
                serverPeerID = peerID.ToString(),
                relayIP = relayEP.Address.ToString(),
                relayPort = relayEP.Port,
                clientIP = clientEP.Address.ToString(),
                clientPort = clientEP.Port,
                token = token,
            };

            using MemoryStream ms = new();
            nprd.Serialize(ms);

            Debug.Log($"Sending peer the Nat punch request for relay={nprd.relayIP}:{nprd.relayPort}, token={nprd.token}");
            G.IPFSService.PostMessageTo(peerID, ms.ToArray());
        }

        public void Peer_InitateNatPunch(NatPunchRequestData nprd)
        {
            // Other peer wants us to initiate Nat punch
            if (Transport.active is INatPunchAddon ina)
            {
                IPEndPoint relay = new(IPAddress.Parse(nprd.relayIP), nprd.relayPort);
                IPEndPoint client = new(IPAddress.Parse(nprd.clientIP), nprd.clientPort);

                if(!relay.Address.Equals(IPAddress.None))
                {
                    Debug.Log($"Relayed NAT punching process via {relay}");
                    ina.InitiateNatPunch(relay, nprd.token);
                }
                else
                {
                    Debug.Log($"Relayless NAT punching process towards {client}");
                    ina.ConnectBackTo(client);
                }

            }
            else
                Debug.LogWarning("... but this peer's transport doesn't support Nat punching.");
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
