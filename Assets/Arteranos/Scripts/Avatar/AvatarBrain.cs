﻿using System.Collections;
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
using Ipfs;
using Random = UnityEngine.Random;
using Arteranos.Core.Cryptography;
using Ipfs.Core.Cryptography.Proto;

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

        public string AvatarCidString { get => m_AvatarCidString; private set => CmdPropagateAvatarURL(value); }

        public float AvatarHeight { get => m_AvatarHeight; private set => CmdPropagateAvatarHeight(value); }

        public UserPrivacy UserPrivacy { get => m_UserPrivacy; private set => CmdPropagateUserPrivacy(value); }

        // Okay to use the setter. The SyncVar would yell at you if you fiddle with the privilege client-side
        public UserID UserID { get => m_userID; set => m_userID = value; }

        public PublicKey AgreePublicKey { get => m_AgreePublicKey; set => m_AgreePublicKey = value; }

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

                if (oldNetAS != newNetAS)
                    // Spread the word...
                    CmdPropagateInt(AVKeys.NetAppearanceStatus, newNetAS);
                else
                    // Or, follow through.
                    UpdateNetAppearanceStatus(value);
            }
        }

        public void SetAppearanceStatusBit(int ASBit, bool set)
        {
            if (set)
                AppearanceStatus |= ASBit;
            else
                AppearanceStatus &= ~ASBit;
        }

        public IAvatarBody Body => GetComponent<IAvatarBody>();

        public void NotifyBubbleBreached(IAvatarBrain touchy, bool isFriend, bool entered)
        {
            // Not for the desired sphere of influence
            if (isFriend != SocialState.IsFriends(touchy)) return;

            touchy.SetAppearanceStatusBit(Avatar.AppearanceStatus.Bubbled, entered);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Debug Messages

        private string PrefixMessage(object message)
        {
            string modestr = string.Empty;

            if (isServer && isClient) modestr += "Host";
            else if (isServer) modestr += "Server";
            else if (isClient) modestr += "Client";
            else modestr += "?";

            if (isOwned) modestr += ", Owned";

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

            Subconscious.OnStartClientSlaved();

            if (isOwned)
            {
                // That's me, set aside from the unwashed crowd. :)
                XRControl.Me = this;

                DownloadClientSettings();

                _ = BubbleCoordinatorFactory.New(this);

                PostOffice.Load();

                StartCoroutine(DoTextMessageLoop());
            }
            else
            {
                // Alien avatars get the hit capsules to target them to call up the nameplates.
                HitBox = AvatarHitBoxFactory.New(this);
            }
        }

        private void CommitAvatarChanged(string CidString, float Height)
        {
            if (isOwned)
            {
                AvatarCidString = CidString;
                AvatarHeight = Height;
            }
        }

        public override void OnStopClient()
        {
            // Maybe it isn't owned anymore, but it would be worse the other way round.
            if (isOwned) XRControl.Me = null;

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

            string avatarCidString = cs.AvatarCidString;

            // Schrödinger's cat and today's view of people's gender... all alike.
            // ¯\_(ツ)_/¯
            avatarCidString ??= (Random.Range(0, 100) < 50)
                    ? SettingsManager.DefaultFemaleAvatar
                    : SettingsManager.DefaultMaleAvatar;

            CommitAvatarChanged(avatarCidString, cs.AvatarHeight);

            UserPrivacy = cs.UserPrivacy;
        }

        #endregion
        // ---------------------------------------------------------------
        #region Running

        private IEnumerator DoTextMessageLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(2);

                DoTextMessage();
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region Networking

        private readonly SyncDictionary<AVKeys, int> m_ints = new();

        [SyncVar(hook = nameof(OnUserIDChanged))]
        private UserID m_userID = null;

        [SyncVar]
        private PublicKey m_AgreePublicKey = null;

        [SyncVar]
        private UserPrivacy m_UserPrivacy = null;

        [SyncVar] // Default if someone circumvented the Authenticator and the Network Manager.
        private ulong m_UserState = (Core.UserState.Banned | Core.UserState.Exploiting);

        [SyncVar(hook = nameof(OnAvatarCidStringChanged))]
        private string m_AvatarCidString = null;

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

        private void OnAvatarCidStringChanged(string _1, string _2) => Body?.ReloadAvatar(m_AvatarCidString, m_AvatarHeight);

        private void OnAvatarHeightChanged(float _1, float _2) => Body?.ReloadAvatar(m_AvatarCidString, m_AvatarHeight);

        private void OnUserIDChanged(UserID _1, UserID userID)
        {
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
        private void CmdPropagateAvatarURL(string URL) => m_AvatarCidString = URL;

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


                    ListOfSerialized<T> packets = new() { entries = new() };
                    if (page.payload != null)
                        packets.entries.AddRange(page.payload);

                    Server.TransmitMessage(packets.Serialize(), AgreePublicKey, out CMSPacket packet);
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
                ServerConfig.ServerPerformServerPacket(this, type, p);
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

        public void MakeWorkdToChange(Cid Cid)
        {
            WorldInfo wmd = WorldInfo.DBLookup(Cid);

            if (wmd?.ContentRating != null)
            {
                // Remotely connected user tries to sneak in something gross or raunchy?
                if (wmd.ContentRating.IsInViolation(SettingsManager.ActiveServerData.Permissions))
                {
                    IDialogUI dialog = DialogUIFactory.New();
                    dialog.Text = "Rejected world: Content permission violation";
                    dialog.Buttons = new string[] { "Got it" };
                }
            }

            CmdMakeWorldToChange(Cid);
        }

        [Command]
        private void CmdMakeWorldToChange(string CidString)
        {
            if (!IsAbleTo(UserCapabilities.CanInitiateWorldTransition, null)) return;

            WorldInfo wmd = WorldInfo.DBLookup(CidString);
            if (wmd?.ContentRating != null && wmd.ContentRating.IsInViolation(SettingsManager.Server.Permissions)) return;

            SettingsManager.PingServerChangeWorld(UserID, CidString);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Social state negotiation

        public ulong GetSocialStateTo(UserID to) 
            => SettingsManager.Client.Me.SocialList.TryGetValue(to, out ulong state)
                ? state : SocialState.None;

        public ulong GetSocialStateTo(IAvatarBrain receiver)
            => GetSocialStateTo(receiver.UserID);

        public void SendCTCPacket(IAvatarBrain receiver, CTCPacket packet)
        {
            if (!receiver?.gameObject)
            {
                Debug.LogWarning($"Discarding CTCP for an already logged out user");
                return;
            }

            // Wrap up, encrypt and send
            try
            {
                packet.sender = UserID;

                byte[] data = packet.Serialize();
                Client.TransmitMessage(data, receiver.AgreePublicKey, out CMSPacket messageData);
                NetworkClient.Send(new CTCPacketEnvelope()
                {
                    receiver = receiver.UserID,
                    CTCPayload = messageData
                });
            }
            catch { }
        }

        public void ReceiveCTCPacket(CTCPacketEnvelope envelope)
        {
            CTCPacket packet;
            try
            {
                Client.ReceiveMessage(envelope.CTCPayload, out byte[] data, out PublicKey signerPublicKey);
                packet = CTCPacket.Deserialize(data);

                if (packet.sender != signerPublicKey)
                    throw new Exception($"Supposed sender {packet.sender} doesn't match its key");

                if (packet is CTCPUserState packetUS) ReactReceivedSSE(packetUS);
                else if(packet is CTCPTextMessage packetTxt) ReactReceiveTextMessage(packetTxt);
                else throw new InvalidOperationException("Unknown CTC Packet, discarded");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            // NOTREACHED
        }

        private void ReactReceivedSSE(CTCPUserState received)
        {
            UserID sender = received.sender;

            // Sync with the mirrored social states
            ulong state = SocialState.ReflectSocialState(received.state, GetSocialStateTo(sender));

            // Save, for now.
            SettingsManager.Client.SaveSocialStates(sender, state);

            // And take it the immediate effects, like blocking.
            IAvatarBrain senderB = NetworkStatus.GetOnlineUser(sender);
            UpdateSSEffects(senderB, state);
        }

        private void ReactReceiveTextMessage(CTCPTextMessage received)
        {
            UserID sender = received.sender;

            PostOffice.EnqueueIncoming(sender, sender, received.text);
            PostOffice.Save();
        }

        public void OfferFriendship(IAvatarBrain receiver, bool offering = true)
        {
            ulong state = GetSocialStateTo(receiver.UserID);
            SocialState.SetFriendState(ref state, offering);

            SendSocialState(receiver, state);
        }

        public void BlockUser(IAvatarBrain receiver, bool blocking = true)
        {
            ulong state = GetSocialStateTo(receiver.UserID);
            if (blocking)
                SocialState.SetFriendState(ref state, false);

            SocialState.SetBlockState(ref state, blocking);

            SendSocialState(receiver, state);
        }

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

            SendCTCPacket(receiver, new CTCPTextMessage()
            {
                text = text
            });
        }

        private void SendSocialState(IAvatarBrain receiver, ulong state)
        {
            SettingsManager.Client.SaveSocialStates(receiver.UserID, state);

            // Maybe the intended receiver logged off while you tried to send a goodbye message.
            if (receiver == null) return;

            SendCTCPacket(receiver, new CTCPUserState()
            {
                state = state
            });
        }


        #endregion
        // ---------------------------------------------------------------
        #region Text message reception

        private volatile IDialogUI m_txtMessageBox = null;

        private volatile IAvatarBrain replyTo = null;

        private bool IsTextMessageOccupied()
        {
            bool busy = false;

            // "I'm swamped with the messages."
            busy |= m_txtMessageBox != null;

            // "Dammit! Not right now!"
            busy |= SettingsManager.Client.UserPrivacy.Visibility != Core.Visibility.Online;
            return busy;
        }

        private void DoTextMessage()
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
            Vector3 offset = transform.rotation * Vector3.up * (Body.AvatarMeasures.FullHeight * 1.10f);
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
                UserCapabilities.CanEnableFly => isAnyAdmin || (SettingsManager.ActiveServerData?.Permissions.Flying ?? true),

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
