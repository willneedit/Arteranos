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
        NetAppearanceStatus
    }

    public class AvatarBrain : NetworkBehaviour, IAvatarBrain
    {
        #region Interface
        public event Action<int> OnAppearanceStatusChanged;

        private int LocalAppearanceStatus = 0;

        private IHitBox HitBox = null;

        private AvatarSubconscious Subconscious { get; set; } = null;

        public uint NetID => netIdentity.netId;

        public string AvatarURL { get => m_AvatarURL; private set => CmdPropagateAvatarURL(value); }

        public float AvatarHeight { get => m_AvatarHeight; private set => CmdPropagateAvatarHeight(value); }

        public UserPrivacy UserPrivacy { get => m_UserPrivacy; private set => CmdPropagateUserPrivacy(value); }

        // Okay to use the setter. The SyncVar would yell at you if you fiddle with the privilege client-side
        public UserID UserID { get => m_userID; set => m_userID = value; }

        public string Nickname { get => m_userID; }

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

        #endregion
        // ---------------------------------------------------------------
        #region Debug Messages

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
        }

        public override void OnStartClient()
        {
            Client cs = SettingsManager.Client;

            base.OnStartClient();

            m_ints.Callback += OnMIntsChanged;

            SettingsManager.Client.OnAvatarChanged += CommitAvatarChanged;
            SettingsManager.Client.OnUserPrivacyChanged += (x) => { if (isOwned) UserPrivacy = x; };

            ResyncInitialValues();


            if (isOwned)
            {
                DownloadClientSettings();

                // That's me, set aside from the unwashed crowd. :)
                XRControl.Me = this;

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

        private void CommitAvatarChanged(string URL, float Height)
        {
            if (isOwned)
            {
                AvatarURL = URL;

                AvatarHeight = Height;
            }
        }

        public override void OnStopClient()
        {
            // Maybe it isn't owned anymore, but it would be worse the other way round.
            if(isOwned) XRControl.Me = null;

            SettingsManager.Client.OnAvatarChanged -= CommitAvatarChanged;

            m_ints.Callback -= OnMIntsChanged;

            base.OnStopClient();
        }

        /// <summary>
        /// Download the user's client settings to his avatar's brain, and announce
        /// the data to the server to spread it to the clones.
        /// </summary>
        private void DownloadClientSettings()
        {
            Client cs = SettingsManager.Client;

            CommitAvatarChanged(cs.AvatarURL, cs.AvatarHeight);

            UserPrivacy = cs.UserPrivacy;
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

        [SyncVar(hook = nameof(OnUserIDChanged))]
        private UserID m_userID = null;

        [SyncVar]
        private UserPrivacy m_UserPrivacy = null;

        [SyncVar] // Default if someone circumvented the Authenticator and the Network Manager.
        private ulong m_UserState = (Core.UserState.Banned | Core.UserState.Exploiting);

        [SyncVar(hook = nameof(OnAvatarURLChanged))]
        private string m_AvatarURL = null;

        [SyncVar(hook = nameof(OnAvatarHeightChanged))]
        private float m_AvatarHeight = 175;

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

        private void OnAvatarURLChanged(string _, string URL) => Body?.RequestAvatarURLChange(URL);

        private void OnAvatarHeightChanged(float _, float height) => Body?.RequestAvatarHeightChange(height);

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

        [Command]
        private void CmdPropagateInt(AVKeys key, int value) => m_ints[key] = value;

        [Command]
        private void CmdPropagateUserPrivacy(UserPrivacy userPrivacy) => m_UserPrivacy = userPrivacy;

        [Command]
        private void CmdPropagateAvatarURL(string URL) => m_AvatarURL = URL;

        [Command]
        private void CmdPropagateAvatarHeight(float height) => m_AvatarHeight = height;

        [Command]
        private void CmdPerformEmote(string emojiName) => RpcPerformEmote(emojiName);

        [ClientRpc]
        private void RpcPerformEmote(string emojiName)
        {
            // Invisible users wouldn't show the emotes
            if(Avatar.AppearanceStatus.IsInvisible(AppearanceStatus)) return;

            LoopPerformEmoji(emojiName);
        }

        #region User kicking

        [Server]
        public void ServerKickUser(string reason)
        {
            IEnumerator KNBCoroutine(NetworkConnectionToClient targetConn, string text)
            {
                TargetDeliverMessage(targetConn, text);

                // Let the target have the message before the disconnect
                yield return new WaitForSeconds(0.5f);

                targetConn.Disconnect();
            }

            NetworkConnectionToClient targetConn = gameObject.GetComponent<NetworkIdentity>().connectionToClient;

            StartCoroutine(KNBCoroutine(targetConn, reason));
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
                    ServerQueryServerPacket(type, ServerConfig.QueryLocalUserBase(this));
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

                    Server.TransmitMessage(packets, UserID.PublicKey, out CMSPacket packet);
                    TargetDeliverServerPacket(type, packet);

                    if (page.payload == null) break;

                    yield return new WaitForEndOfFrame();
                }
            }

            StartCoroutine(EmitSusPages(type, data.ToArray()));
        }

        [TargetRpc]
        private void TargetDeliverServerPacket(SCMType type, CMSPacket packet)
            => ServerConfig.TargetDeliverServerPacket(type, packet);

        [Command]
        private void CmdPerformServerPacket(SCMType type, CMSPacket p)
        {
            try
            {
                _ = ServerConfig.ServerPerformServerPacket(this, type, p);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                // Something is wrong for the user administration, no matter what.
                return;
            }

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
            if(Body != null) Body.Invisible = Avatar.AppearanceStatus.IsInvisible(AppearanceStatus);

            OnAppearanceStatusChanged?.Invoke(AppearanceStatus);
        }


        #endregion
        // ---------------------------------------------------------------
        #region World change event handling

        public void MakeWorkdToChange(string worldURL, bool forceReload = false)
        {
            CmdMakeWorldToChange(worldURL, forceReload);
        }

        [Command]
        private void CmdMakeWorldToChange(string worldURL, bool _ /* forceReload */)
        {
            if (!IsAbleTo(UserCapabilities.CanInitiateWorldTransition, null)) return;

            SettingsManager.PingServerChangeWorld(UserID, worldURL);
        }

#if false
        [ClientRpc]
        private void ReceiveWorldTransition(string worldURL)
        {
            Server ss = SettingsManager.Server;

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
            // The hacked client may display the new world, but we won't distribute the
            // world throughout the server.
            if (!IsAbleTo(UserCapabilities.CanInitiateWorldTransition, null)) return;

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
#endif
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
            busy |= SettingsManager.Client.UserPrivacy.Visibility != Core.Visibility.Online;
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

                // Both server and world admins can initiate the world transition
                UserCapabilities.CanInitiateWorldTransition => isAnyAdmin,

                _ => throw new NotImplementedException(),
            };
        }

        public void QueryServerPacket(SCMType type)
            => CmdQueryServerPacket(type);

        public void PerformServerPacket(SCMType type, CMSPacket p)
            => CmdPerformServerPacket(type, p);

        #endregion
    }
}
