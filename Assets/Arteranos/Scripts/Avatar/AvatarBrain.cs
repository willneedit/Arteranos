using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;

using Arteranos.Services;
using Arteranos.Core;
using System;
using Arteranos.Web;
using Arteranos.UI;
using Arteranos.XR;
using Arteranos.Social;
using System.Linq;

namespace Arteranos.Avatar
{
    internal enum AVKeys : byte
    {
        _Invalid = 0,
        _ChatOwnID,
        AvatarURL,
        CurrentWorld,
        NetAppearanceStatus
    }

    public class AvatarBrain : NetworkBehaviour, IAvatarBrain
    {
        #region Interface
        public event Action<string> OnAvatarChanged;
        public event Action<int> OnAppearanceStatusChanged;

        private int LocalAppearanceStatus = 0;

        private IHitBox HitBox = null;

        private AvatarSubconscious Subconscious { get; set; } = null;

        public uint NetID => netIdentity.netId;

        public string AvatarURL
        {
            get => m_strings.ContainsKey(AVKeys.AvatarURL) ? m_strings[AVKeys.AvatarURL] : null;
            private set => CmdPropagateString(AVKeys.AvatarURL, value);
        }

        public UserID UserID { get => m_userID; set => m_userID = value; }

        public string Nickname { get => m_userID; }

        public UserPrivacy UserPrivacy
        {
            get => m_UserPrivacy;
            private set => CmdPropagateUserPrivacy(value);
        }

        // Okay to use the setter. The SyncVar would yell at you if you fiddle with the privilege client-side
        public ulong UserState { get => m_UserState; set => m_UserState = value; }

        public string Address { get => m_Address; set => m_Address = value; }

        public string DeviceID { get => m_DeviceID; set => m_DeviceID = value; }

        public int AppearanceStatus
        {
            get
            {
                // Join the local and the net-wide status
                int status = m_ints.ContainsKey(AVKeys.NetAppearanceStatus)
                    ? m_ints[AVKeys.NetAppearanceStatus]
                    : Avatar.AppearanceStatus.OK;

                return status | LocalAppearanceStatus;
            }

            set
            {
                // Split the local and the net-wide status
                LocalAppearanceStatus = value & Avatar.AppearanceStatus.MASK_LOCAL;

                int oldNetAS = m_ints.ContainsKey(AVKeys.NetAppearanceStatus)
                    ? m_ints[AVKeys.NetAppearanceStatus]
                    : Avatar.AppearanceStatus.OK;
                int newNetAS = value & ~Avatar.AppearanceStatus.MASK_LOCAL;

                if(oldNetAS != newNetAS)
                    // Spread the word...
                    CmdPropagateInt(AVKeys.NetAppearanceStatus, newNetAS);
                else
                    // Or, follow through.
                    UpdateNetAppearanceStatus(value);
            }
        }

        public void SetAppearanceStatusBit(int ASBit, bool set)
        {
            if(set)
                AppearanceStatus |= ASBit;
            else
                AppearanceStatus &= ~ASBit;
        }

        public IAvatarLoader Body => GetComponent<IAvatarLoader>();

        public void NotifyBubbleBreached(IAvatarBrain touchy, bool isFriend, bool entered)
        {
            // Not for the desired sphere of influence
            if(isFriend != SocialState.IsFriends(touchy)) return;

            touchy.SetAppearanceStatusBit(Avatar.AppearanceStatus.Bubbled, entered);
        }


        private string PrefixMessage(object message)
        {
            string modestr = string.Empty;

            if(isServer && isClient) modestr += "Host";
            else if(isServer) modestr += "Server";
            else if(isClient) modestr += "Client";
            else modestr += "?";

            if(isOwned) modestr += ", Owned";

            string objmsg = (message is string msgstr) ? msgstr : message.ToString();
            return UserID != null 
                ? $"[{Nickname}][{modestr}] {objmsg}"
                : $"[???][{modestr}] {objmsg}";
        }

        public void LogDebug(object message) => Debug.unityLogger.Log(LogType.Log, PrefixMessage(message));

        public void LogWarning(object message) => Debug.unityLogger.Log(LogType.Warning, PrefixMessage(message));

        public void LogError(object message) => Debug.unityLogger.Log(LogType.Error, PrefixMessage(message));

        #endregion
        // ---------------------------------------------------------------
        #region Start & Stop
        public override void OnStartServer()
        {
            base.OnStartServer();

            m_strings[AVKeys.CurrentWorld] = SettingsManager.Server.WorldURL;
        }

        public override void OnStartClient()
        {
            ClientSettings cs = SettingsManager.Client;

            base.OnStartClient();

            m_ints.Callback += OnMIntsChanged;
            m_floats.Callback += OnMFloatsChanged;
            m_strings.Callback += OnMStringsChanged;
            m_blobs.Callback += OnMBlobsChanged;

            SettingsManager.Client.OnAvatarChanged += (x) => { if(isOwned) AvatarURL = x; };
            SettingsManager.Client.OnUserPrivacyChanged += (x) => { if (isOwned) UserPrivacy = x; };
            SettingsManager.Server.OnWorldURLChanged += CommitWorldChanged;

            ResyncInitialValues();

            DownloadClientSettings();

            if(isOwned)
            {
                // That's me, set aside from the unwashed crowd. :)
                XRControl.Me = this;

                // Invoked by command line - only once
                if(SettingsManager.StartupTrigger)
                {
                    string world = SettingsManager.ResetStartupTrigger();

                    m_strings.TryGetValue(AVKeys.CurrentWorld, out string serversworld);

                    // Only an 'empty' server regards the startup world URL.
                    if (!string.IsNullOrEmpty(serversworld))
                        world = serversworld;

                    if(!string.IsNullOrEmpty(world))
                    {
                        Debug.Log($"Invoking startup world '{world}'");
                        WorldTransition.InitiateTransition(world);
                    }
                    else
                    {
                        Debug.Log("Connected to a server at startup, neither server has a world nor the commandline URL.");
                    }
                }

                // The server already uses a world, so download and transition into the targeted world immediately.
                else if(!string.IsNullOrEmpty(m_strings[AVKeys.CurrentWorld]))
                {
                    WorldTransition.InitiateTransition(m_strings[AVKeys.CurrentWorld]);
                }

                _ = BubbleCoordinatorFactory.New(this);

                PostOffice.Load();
            }
            else
            {
                // Alien avatars get the hit capsules to target them to call up the nameplates.
                HitBox = AvatarHitBoxFactory.New(this);
            }

            // Avatar startup complete, all systems go.
            Subconscious.ReadyState = AvatarSubconscious.READY_COMPLETE;
        }

        public override void OnStopClient()
        {
            // Maybe it isn't owned anymore, but it would be worse the other way round.
            if(isOwned) XRControl.Me = null;

            SettingsManager.Server.OnWorldURLChanged -= CommitWorldChanged;
            SettingsManager.Client.OnAvatarChanged -= (x) =>
            {
                if(isOwned) AvatarURL = x;
            };

            m_ints.Callback -= OnMIntsChanged;
            m_floats.Callback -= OnMFloatsChanged;
            m_strings.Callback -= OnMStringsChanged;
            m_blobs.Callback -= OnMBlobsChanged;

            base.OnStopClient();
        }

        /// <summary>
        /// Download the user's client settings to his avatar's brain, and announce
        /// the data to the server to spread it to the clones.
        /// </summary>
        private void DownloadClientSettings()
        {
            ClientSettings cs = SettingsManager.Client;

            if(isOwned)
            {
                AvatarURL = cs.AvatarURL;

                UserPrivacy = cs.UserPrivacy;
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region Running
        private void Update()
        {
            if((Subconscious.ReadyState & AvatarSubconscious.READY_COMPLETE) == 0)
                return;

            // The text message reception loop
            if(isOwned)
                DoTextMessageLoop();
        }

        #endregion
        // ---------------------------------------------------------------
        #region Networking

        private readonly SyncDictionary<AVKeys, int> m_ints = new();
        private readonly SyncDictionary<AVKeys, float> m_floats = new();
        private readonly SyncDictionary<AVKeys, string> m_strings = new();
        private readonly SyncDictionary<AVKeys, byte[]> m_blobs = new();

        [SyncVar(hook = nameof(OnUserIDChanged))]
        private UserID m_userID = null;

        [SyncVar(hook = nameof(OnUserPrivacyChanged))]
        private UserPrivacy m_UserPrivacy = null;

        [SyncVar] // Default if someone circumvented the Authenticator and the Network Manager.
        private ulong m_UserState = (Core.UserState.Banned | Core.UserState.Exploiting);

        // No [SyncVar] - server only for privacy reasons
        private string m_Address = null;

        // No [SyncVar] - server only for privacy reasons
        private string m_DeviceID = null;

        void Awake()
        {
            Subconscious = GetComponent<AvatarSubconscious>();

            syncDirection = SyncDirection.ServerToClient;
        }

        private void ResyncInitialValues()
        {
            foreach(KeyValuePair<AVKeys, int> kvpi in m_ints)
                OnMIntsChanged(SyncDictionary<AVKeys, int>.Operation.OP_ADD, kvpi.Key, kvpi.Value);

            foreach(KeyValuePair<AVKeys, float> kvpf in m_floats)
                OnMFloatsChanged(SyncDictionary<AVKeys, float>.Operation.OP_ADD, kvpf.Key, kvpf.Value);

            foreach(KeyValuePair<AVKeys, string> kvps in m_strings)
                OnMStringsChanged(SyncDictionary<AVKeys, string>.Operation.OP_ADD, kvps.Key, kvps.Value);

            foreach(KeyValuePair<AVKeys, byte[]> kvpb in m_blobs)
                OnMBlobsChanged(SyncDictionary<AVKeys, byte[]>.Operation.OP_ADD, kvpb.Key, kvpb.Value);
        }


        private void OnMIntsChanged(SyncDictionary<AVKeys, int>.Operation op, AVKeys key, int value)
        {
            switch(key)
            {
                case AVKeys.NetAppearanceStatus:
                    // Either been self-muted or by other means, the announcement comes down from the server.
                    UpdateNetAppearanceStatus(value); break;
            }
        }

        private void OnMFloatsChanged(SyncIDictionary<AVKeys, float>.Operation op, AVKeys key, float value)
        {
            // Reserved for future use
        }

        private void OnMStringsChanged(SyncIDictionary<AVKeys, string>.Operation op, AVKeys key, string value)
        {
            switch(key)
            {
                case AVKeys.AvatarURL:
                    OnAvatarChanged?.Invoke(value); break;
            }
        }

        private void OnMBlobsChanged(SyncIDictionary<AVKeys, byte[]>.Operation op, AVKeys key, byte[] value)
        {
        }

        private void OnUserIDChanged(UserID _1, UserID userID)
        {
            Subconscious.ReadyState = AvatarSubconscious.READY_USERID;
            Subconscious.ReadyState = AvatarSubconscious.READY_CRYPTO;

            if(isServer)
            {
                int connId = GetComponent<NetworkIdentity>().connectionToClient.connectionId;
                name = $"Avatar [{userID.Nickname}][connId={connId}, netId={netId}]";
            }
            else
            {
                name = $"Avatar [{userID.Nickname}][netId={netId}]";
            }
        }

        private void OnUserPrivacyChanged(UserPrivacy _1, UserPrivacy _2)
        {

        }

        [Command]
        private void CmdPropagateInt(AVKeys key, int value) => m_ints[key] = value;

        [Command]
        private void CmdPropagateFloat(AVKeys key, float value) => m_floats[key] = value;

        [Command]
        private void CmdPropagateString(AVKeys key, string value) => m_strings[key] = value;

        [Command]
        private void CmdPropagateBlob(AVKeys key, byte[] value) => m_blobs[key] = value;

        [Command]
        private void CmdPropagateUserPrivacy(UserPrivacy userPrivacy) => m_UserPrivacy = userPrivacy;

        [Command]
        private void CmdPerformEmote(string emojiName) => RpcPerformEmote(emojiName);

        [ClientRpc]
        private void RpcPerformEmote(string emojiName)
        {
            // Invisible users wouldn't show the emotes
            if(Avatar.AppearanceStatus.IsInvisible(AppearanceStatus)) return;

            LoopPerformEmoji(emojiName);
        }

        #region User kicking and banning

        [Command]
        private void CmdAttemptKickUser(GameObject targetGO, CMSPacket p)
        {
            IAvatarBrain target = targetGO.GetComponent<IAvatarBrain>();

            byte[] expectedSignatureKey = UserID;
            bool allowed;
            ServerUserState banPacket;

            try
            {
                ServerSettings.ReceiveMessage(p, ref expectedSignatureKey, out banPacket);

                // Fill up the banPacket's fields server-side
                if (banPacket.userID != null) banPacket.userID = target.UserID;
                if (banPacket.address != null) banPacket.address = target.Address;
                if (banPacket.deviceUID != null) banPacket.deviceUID = target.DeviceID;

                allowed = Core.UserState.IsBanned(banPacket.userState)
                    ? IsAbleTo(UserCapabilities.CanBanUser, target)
                    : IsAbleTo(UserCapabilities.CanKickUser, target);
            }
            catch (Exception e)
            {

                Debug.LogWarning($"PRIVILEGE ESCALATION DETECTED: Maliciously constructed banPacket, supposedly from {Nickname}");
                Debug.LogWarning(e.Message);
                return;
            }

            // No retaliatory action because risking using malicious packets as a denial-of-service attack
            if (!allowed) return;

            // If banned, save the target user's (or the hacker's) data...
            if (Core.UserState.IsBanned(banPacket.userState))
            {
                SettingsManager.ServerUsers.AddUser(banPacket);
                SettingsManager.ServerUsers.Save();
            }

            // ... and gone.
            ServerKickUser(target, banPacket);
        }

        [Server]
        private void ServerKickUser(IAvatarBrain target, ServerUserState bp)
        {
            IEnumerator KNBCoroutine(NetworkConnectionToClient targetConn, string text)
            {
                TargetDeliverMessage(targetConn, text);

                // Let the target have the message before the disconnect
                yield return new WaitForSeconds(0.5f);

                targetConn.Disconnect();
            }

            GameObject targetGO = target.gameObject;
            NetworkConnectionToClient targetConn = targetGO.GetComponent<NetworkIdentity>().connectionToClient;

            string text = Core.UserState.IsBanned(bp.userState)
                ? "You've been banned from this server."
                : "You've been kicked from this server.";

            StartCoroutine(KNBCoroutine(targetConn, text));
        }


        [TargetRpc]
        private void TargetDeliverMessage(NetworkConnectionToClient _, string message)
            => ConnectionManager.DeliverDisconnectReason(message);

        #endregion

        #region Server side queries and actions interface

        [Command]
        private void CmdQueryServerPacket(SCMType type)
        {
            switch (type)
            {
                case SCMType.SrvReportUserInfo:
                    ServerQueryServerPacket(type, ServerConfig.QueryLocalUserBase());
                    break;
                default:
                    throw new NotImplementedException();

            }
        }

        [Server]
        private void ServerQueryServerPacket<T>(SCMType type, IEnumerable<T> data)
        {
            IEnumerator EmitSusPages(SCMType type, T[] data)
            {
                int pagenum = 1;
                while (true)
                {
                    Core.Utils.Paginated<T> page = Core.Utils.Paginate(data, pagenum++);


                    List<T> packets = new();
                    if (page.payload != null)
                        packets = page.payload.ToList();

                    ServerSettings.TransmitMessage(packets, UserID.PublicKey, out CMSPacket packet);
                    TargetDeliverServerPacket(type, packet);

                    if (page.payload == null) break;

                    yield return new WaitForEndOfFrame();
                }
            }

            StartCoroutine(EmitSusPages(type, data.ToArray()));
        }

        [TargetRpc]
        private void TargetDeliverServerPacket(SCMType type, CMSPacket packet)
        {
            switch (type)
            {
                case SCMType.SrvReportUserInfo:
                    ClientDeliverServerPacket(packet, ref currentCallback_sus);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        [Client]
        private void ClientDeliverServerPacket<T>(CMSPacket packet, ref Action<T> callback)
        {
            try
            {
                byte[] serverPublicKey = SettingsManager.CurrentServer.ServerPublicKey;
                ClientSettings.ReceiveMessage(packet, ref serverPublicKey, out List<T> packets);

                if (packets.Count == 0) callback = null;
                foreach (T entry in packets) callback?.Invoke(entry);
            }
            catch (Exception)
            {
                // Ignore injected messages.
            }
        }

        [Command]
        private void CmdPerformServerPacket(SCMType type, CMSPacket p)
        {
            byte[] expectedSignatureKey = UserID;

            try
            {
                expectedSignatureKey = ServerPerformServerPacket(type, p, expectedSignatureKey);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                // Something is wrong for the user administration, no matter what.
                return;
            }

        }
        [Server]
        private static byte[] ServerPerformServerPacket(SCMType type, CMSPacket p, byte[] expectedSignatureKey)
        {
            switch (type)
            {
                case SCMType.ClnUpdateUserInfo:
                    ServerSettings.ReceiveMessage(p, ref expectedSignatureKey, out ServerUserState user);
                    ServerConfig.UpdateLocalUserState(user);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return expectedSignatureKey;
        }
        #endregion

        #endregion
        // ---------------------------------------------------------------
        #region Appearance Status

        private void UpdateNetAppearanceStatus(int _)
        {
            // Special case - self-modifying appearance status, like making yourself invisible,
            // and, to a lesser extent, self-muting.
            if(HitBox != null) HitBox.interactable = !Avatar.AppearanceStatus.IsInvisible(AppearanceStatus);
            if(Body != null) Body.invisible = Avatar.AppearanceStatus.IsInvisible(AppearanceStatus);

            OnAppearanceStatusChanged?.Invoke(AppearanceStatus);
        }


        #endregion
        // ---------------------------------------------------------------
        #region World change event handling

        [ClientRpc]
        private void ReceiveWorldTransition(string worldURL)
        {
            ServerSettings ss = SettingsManager.Server;

            Debug.Log($"Received world transition: isServer={isServer}, isOwned={isOwned}, Source World={ss.WorldURL}, Target World={worldURL}");

            // worldURL could be null means reloading the same world.
            if(worldURL == null) return;

            // It could be a latecoming user entering, or a laggard answering the dialogue.
            if(ss.WorldURL == worldURL)
            {
                Debug.Log("World transition - already in that targeted world.");
                return;
            }

            // Now that's the real deal.
            Debug.Log($"WORLD TRANSITION: From {ss.WorldURL} to {worldURL} - Choose now!");

            ShowWorldChangeDialog(worldURL,
            (response) => OnWorldChangeAnswer(worldURL, response));
        }

        private static void ShowWorldChangeDialog(string worldURL, Action<int> resposeCallback)
        {

            WorldMetaData md = WorldGallery.RetrieveWorldMetaData(worldURL);

            string worldname = md?.WorldName ?? worldURL;

            IDialogUI dialog = DialogUIFactory.New();

            dialog.Text =
                "This server is about to change the world to\n" +
                $"{worldname}\n" +
                "What to do?";

            dialog.Buttons = new string[]
            {
                "Go offline",
                "Stay",
                "Follow"
            };

            dialog.OnDialogDone += resposeCallback;
        }
        private void OnWorldChangeAnswer(string worldURL, int response)
        {
            // Disconnect, go offline
            if(response == 0)
            {
                // Only the client disconnected, but the server part of the host
                // remains
                NetworkClient.Disconnect();
                return;
            }

            // TODO dispatch to a server selector in an offline world

            // Here on now, remote triggered world change.
            if(response == 2)
                WorldTransition.InitiateTransition(worldURL);
        }

        [Command]
        private void PropagateWorldTransition(string worldURL)
        {
            // Pure server needs to be notified and transitioned, too.
            if(isServer && !isClient && !string.IsNullOrEmpty(worldURL))
            {
                SettingsManager.Server.WorldURL = worldURL;
                WorldTransition.InitiateTransition(worldURL);
            }
            else
            {
                ReceiveWorldTransition(worldURL);
            }
        }

        private void CommitWorldChanged(string worldURL)
        {
            // We already have transitioned, now we have to tell that the world has changed,
            // and we have to wake up, and for us it is just to find ourselves.
            if(isOwned)
                PropagateWorldTransition(worldURL);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Social state negotiation

        public void OfferFriendship(IAvatarBrain receiver, bool offering = true)
            => Subconscious.OfferFriendship(receiver, offering);

        public void BlockUser(IAvatarBrain receiver, bool blocking = true)
            => Subconscious.BlockUser(receiver, blocking);

        public void UpdateSSEffects(IAvatarBrain receiver, ulong state)
        {
            if(receiver == null) return;

            LogDebug($"I feel about {receiver.Nickname}: {state}");

            // You blocked him.
            bool own_blocked = SocialState.IsBlocked(state);
            receiver.SetAppearanceStatusBit(Avatar.AppearanceStatus.Blocked, own_blocked);
            if(own_blocked) return;

            // Retaliatory blocking.
            bool them_blocked = SocialState.IsBeingBlocked(state);
            receiver.SetAppearanceStatusBit(Avatar.AppearanceStatus.Blocking, them_blocked);
            if(them_blocked) return;
        }

        public void SendTextMessage(IAvatarBrain receiver, string text)
        {
            // Maybe the intended receiver logged off while you tried to send a goodbye message.
            if(receiver == null) return;

            Subconscious.SendTextMessage(receiver, text);
        }

        public ulong GetOwnState(IAvatarBrain receiver)
            => Subconscious.GetOwnState(receiver.UserID);
        #endregion
        // ---------------------------------------------------------------
        #region Text message reception

        private volatile IDialogUI m_txtMessageBox = null;

        private volatile IAvatarBrain replyTo = null;

        public void ReceiveTextMessage(IAvatarBrain sender, string text)
        {
            if(IsTextMessageOccupied())
            {
                // Put the message onto the pile... together with the dozens others...
                PostOffice.EnqueueIncoming(sender.UserID, sender.Nickname, text);
                PostOffice.Save();
                return;
            }

            ShowTextMessage(sender, sender.Nickname, text);
        }

        private bool IsTextMessageOccupied()
        {
            bool busy = false;

            // "I'm swamped with the messages."
            busy |= m_txtMessageBox != null;

            // "Dammit! Not right now!"
            busy |= SettingsManager.Client.Visibility != Core.Visibility.Online;
            return busy;
        }

        private void DoTextMessageLoop()
        {
            if(replyTo != null)
            {
                TextMessageUIFactory.New(replyTo);
                replyTo= null;
                return;
            }

            if(IsTextMessageOccupied()) return;

            PostOffice.DequeueIncoming(null, out MessageEntryJSON message);

            if(message == null) return;

            PostOffice.Save();

            IAvatarBrain sender = NetworkStatus.GetOnlineUser(message.UserID);

            ShowTextMessage(sender, message.Nickname, message.Text);
        }

        public async void ShowTextMessage(IAvatarBrain sender, string Nickname, string text)
        {
            string[] replyText = { "OK", "Reply" };
            string[] okText = { "OK" };

            m_txtMessageBox = DialogUIFactory.New();

            int rc = await m_txtMessageBox.PerformDialogAsync(
                $"Message from {Nickname}:\n\n" +
                text,
                (sender == null) ? okText : replyText);

            m_txtMessageBox = null;

            // "Goodbye!" -- "Bye!" -- "Rest we...." Drat, logged off....
            if (rc != 0 && sender != null)
                replyTo = sender;
        }
        #endregion
        // ---------------------------------------------------------------
        #region Emotes performance

        private IEnumerator CleanupEmojiPS(ParticleSystem ps)
        {
            yield return new WaitForSeconds(5);

            Destroy(ps.gameObject);
        }

        private void LoopPerformEmoji(string emojiName)
        {
            ParticleSystem ps = EmojiSettings.Load().GetEmotePS(emojiName);

            if(ps == null) return;

            ps.transform.SetParent(transform, false);

            // A little bit 'up' (relative to the user)
            Vector3 offset = transform.rotation * Vector3.up * (Body.FullHeight * 1.10f);
            ps.transform.SetLocalPositionAndRotation(offset, Quaternion.identity);

            StartCoroutine(CleanupEmojiPS(ps));
        }

        public void PerformEmote(string emoteName) => CmdPerformEmote(emoteName);

        #endregion
        // ---------------------------------------------------------------
        #region Server User Management

        public bool IsAbleTo(UserCapabilities cap, IAvatarBrain target)
        {
            bool friends = (target != null) && SocialState.IsFriendRequested(target);

            bool isAnyAdmin = (Core.UserState.IsSAdmin(UserState) || Core.UserState.IsWAdmin(UserState));

            bool targetIsAnyAdmin = (target != null && (Core.UserState.IsSAdmin(target.UserState) || Core.UserState.IsWAdmin(target.UserState)));

            bool userHigher = target == null || 
                (UserState & Core.UserState.GOOD_MASK) > (target.UserState & Core.UserState.GOOD_MASK);

            return cap switch
            {
                // User can enable flying if the current(!) server permits it
                // TODO - maybe world restrictions.
                // Maybe add SettingsManager.Client.PingXRControllersChanged();
                // in the world transition, too.
                UserCapabilities.CanEnableFly => isAnyAdmin || (SettingsManager.CurrentServer?.Permissions.Flying ?? true),

                // Maybe some accesss restriction for requesting friendships...?
                UserCapabilities.CanFriendUser => !friends,

                // Every user can mute others on their own clients, but they cannot mute admins.
                UserCapabilities.CanMuteUser => !targetIsAnyAdmin,

                // Users should not block admins. Friends should not block friends, too.
                UserCapabilities.CanBlockUser => !friends && !targetIsAnyAdmin,

                // Gag User is reserved to to admins. Note that they _can_ gag higher tiers in
                // case the don't notice when their microphone is noisy.
                UserCapabilities.CanGagUser => isAnyAdmin,

                // Kicking users is reserved to any admin, but only the lower tiers
                UserCapabilities.CanKickUser => isAnyAdmin && userHigher,

                // Banning users is for server admin only,
                UserCapabilities.CanBanUser => Core.UserState.IsSAdmin(UserState) && userHigher,

                // Edit server's user base (server admins and its deputies)
                UserCapabilities.CanAdminServerUsers => Core.UserState.IsSAdmin(UserState),

                // Edit server configuration itself (only true server admins)
                UserCapabilities.CanEditServer => Bit64field.IsAny(UserState, Core.UserState.Srv_admin),

                // Admins can view user IDs, even if the users don't want to.
                // Especially for the risk of user impersonation
                UserCapabilities.CanViewUsersID => isAnyAdmin || (target != null && SocialState.IsPermitted(target, target.UserPrivacy.UIDVisibility)),

                // Admin can send text, even to higher tiers, for a reason
                UserCapabilities.CanSendText => isAnyAdmin || (target != null && SocialState.IsPermitted(target, target.UserPrivacy.TextReception)),


                _ => throw new NotImplementedException(),
            };
        }

        public void AttemptKickUser(IAvatarBrain target, ServerUserState user)
        {
            bool allowed = Core.UserState.IsBanned(user.userState)
                ? IsAbleTo(UserCapabilities.CanBanUser, target)
                : IsAbleTo(UserCapabilities.CanKickUser, target);

            if (!allowed) return; // Client side. Last chance to back off.

            // Sign, encrypt and transmit.
            ClientSettings.TransmitMessage(
                user, 
                SettingsManager.CurrentServer.ServerPublicKey,
                out CMSPacket p);

            CmdAttemptKickUser(target.gameObject, p);
        }

        private Action<ServerUserState> currentCallback_sus = null;
        public void QueryServerUserBase(Action<ServerUserState> callback)
        {
            // callback == null means to abort
            if (callback == null)
            {
                currentCallback_sus = null;
                return;
            }

            // Requests on top of an ongoing requests are ignored.
            if (currentCallback_sus != null) return;

            currentCallback_sus = callback;

            // Turn it over to the current(!) server
            CmdQueryServerPacket(SCMType.SrvReportUserInfo);
        }

        public void UpdateServerUserState(ServerUserState user)
        {
            // Sign, encrypt and transmit.
            ClientSettings.TransmitMessage(
                user,
                SettingsManager.CurrentServer.ServerPublicKey,
                out CMSPacket p);

            CmdPerformServerPacket(SCMType.ClnUpdateUserInfo, p);
        }

        #endregion
    }
}
