using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;

using Arteranos.Services;
using Arteranos.Core;
using Arteranos.Audio;
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
        Nickname,
        NetAppearanceStatus,
        PublicKey
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
            set => CmdPropagateString(AVKeys.AvatarURL, value);
        }

        public UserID UserID
        {
            get => m_userID;
            private set => CmdPropagateUserID(value);
        }

        public byte[] PublicKey
        {
            get => m_blobs[AVKeys.PublicKey];
            set => CmdPropagateBlob(AVKeys.PublicKey, value);
        }

        public string Nickname
        {
            get => m_strings.ContainsKey(AVKeys.Nickname) ? m_strings[AVKeys.Nickname] : null;
            private set => CmdPropagateString(AVKeys.Nickname, value);
        }

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
            if(isFriend != Subconscious.IsMutualFriends(touchy)) return;

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
            return $"[{Nickname}][{modestr}] {objmsg}";
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

            if(isServer)
            {
                int connId = GetComponent<NetworkIdentity>().connectionToClient.connectionId;
                name = $"Avatar [???][connId={connId}, netId={netId}]";
            }
            else
            {
                name = $"Avatar [???][netId={netId}]";
            }

            m_ints.Callback += OnMIntsChanged;
            m_floats.Callback += OnMFloatsChanged;
            m_strings.Callback += OnMStringsChanged;
            m_blobs.Callback += OnMBlobsChanged;

            SettingsManager.Client.OnAvatarChanged += (x) => { if(isOwned) AvatarURL = x; };
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

                    Debug.Log($"Invoking startup world '{world}'");
                    if(!string.IsNullOrEmpty(world))
                        WorldTransition.InitiateTransition(world);
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
                Nickname = cs.Me.Nickname;

                // Distribute the with the server's name derived hash to the server's user
                // list, _not_ the original hash.
                // Underived hashes are for close friends only.
                UserID = new(cs.UserID.Hash, SettingsManager.CurrentServer.Name);
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
                case AVKeys.Nickname:
                    if(isServer)
                    {
                        int connId = GetComponent<NetworkIdentity>().connectionToClient.connectionId;
                        name = $"Avatar [{value}][connId={connId}, netId={netId}]";
                    }
                    else
                    {
                        name = $"Avatar [{value}][netId={netId}]";
                    }

                    break;
            }
        }

        private void OnMBlobsChanged(SyncIDictionary<AVKeys, byte[]>.Operation op, AVKeys key, byte[] value)
        {
            switch(key)
            {
                case AVKeys.PublicKey:
                    Subconscious.ReadyState = AvatarSubconscious.READY_CRYPTO;
                    break;
            }
        }

        private void OnUserIDChanged(UserID _1, UserID _2)
            => Subconscious.ReadyState = AvatarSubconscious.READY_USERID;


        [Command]
        private void CmdPropagateInt(AVKeys key, int value) => m_ints[key] = value;

        [Command]
        private void CmdPropagateFloat(AVKeys key, float value) => m_floats[key] = value;

        [Command]
        private void CmdPropagateString(AVKeys key, string value) => m_strings[key] = value;

        [Command]
        private void CmdPropagateBlob(AVKeys key, byte[] value) => m_blobs[key] = value;

        [Command]
        private void CmdPropagateUserID(UserID userID) => m_userID = userID;


        [Command]
        private void CmdPerformEmote(string emojiName)
        {
            RpcPerformEmote(emojiName);
        }

        [ClientRpc]
        private void RpcPerformEmote(string emojiName)
        {
            // Invisible users wouldn't see the emotes
            if(Avatar.AppearanceStatus.IsInvisible(AppearanceStatus)) return;

        }

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

        public void UpdateSSEffects(IAvatarBrain receiver, int state)
        {
            if(receiver == null) return;

            LogDebug($"I feel about {receiver.Nickname}: {state}");

            // You blocked him.
            bool own_blocked = (state & SocialState.Own_Blocked) != 0;
            receiver.SetAppearanceStatusBit(Avatar.AppearanceStatus.Blocked, own_blocked);
            if(own_blocked) return;

            // Retaliatory blocking.
            bool them_blocked = (state & SocialState.Them_Blocked) != 0;
            receiver.SetAppearanceStatusBit(Avatar.AppearanceStatus.Blocking, them_blocked);
            if(them_blocked) return;

            Subconscious.AttemptFriendNegotiation(receiver);
        }

        public void SendTextMessage(IAvatarBrain receiver, string text)
        {
            // Maybe the intended receiver logged off while you tried to send a goodbye message.
            if(receiver == null) return;

            Subconscious.SendTextMessage(receiver, text);
        }

        public int GetOwnState(IAvatarBrain receiver)
            => Subconscious.GetOwnState(receiver);
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
    }
}
