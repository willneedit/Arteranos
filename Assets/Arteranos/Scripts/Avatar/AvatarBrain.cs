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

namespace Arteranos.Avatar
{
    internal enum AVKeys : byte
    {
        _Invalid = 0,
        ChatOwnID,
        VoicePort,
        AvatarURL,
        CurrentWorld,
        Nickname,
        UserHash,
        AudioStatus,
    }

    public class AvatarBrain : NetworkBehaviour, IAvatarBrain
    {
        public event Action<IVoiceOutput> OnVoiceOutputChanged;
        public event Action<string> OnAvatarChanged;
        public event Action<int> OnNetMuteStatusChanged;

        private Transform Voice = null;

        public uint NetID => netIdentity.netId;

        public bool IsOwned => isOwned;

        public int ChatOwnID
        {
            get => m_ints.ContainsKey(AVKeys.ChatOwnID) ? m_ints[AVKeys.ChatOwnID] : -1;
            set => PropagateInt(AVKeys.ChatOwnID, value);
        }

        public string AvatarURL
        {
            get => m_strings.ContainsKey(AVKeys.AvatarURL) ? m_strings[AVKeys.AvatarURL] : null;
            set => PropagateString(AVKeys.AvatarURL, value);
        }

        public byte[] UserHash
        {
            get => m_blobs.ContainsKey(AVKeys.UserHash) ? m_blobs[AVKeys.UserHash] : null;
            private set => PropagateBlob(AVKeys.UserHash, value);
        }

        public string Nickname
        {
            get => m_strings.ContainsKey(AVKeys.Nickname) ? m_strings[AVKeys.Nickname] : null;
            private set => PropagateString(AVKeys.Nickname, value);
        }

        public int NetMuteStatus
        {
            get => m_ints.ContainsKey(AVKeys.AudioStatus) ? m_ints[AVKeys.AudioStatus] : Avatar.NetMuteStatus.OK;
            set => PropagateInt(AVKeys.AudioStatus, value);
        }

        public bool ClientMuted
        {
            get => AudioManager.Instance.MuteOther((short) ChatOwnID);
            set => AudioManager.Instance.MuteOther((short) (ChatOwnID), value);
        }

        public IAvatarLoader Body => GetComponent<IAvatarLoader>();

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
                UserHash = cs.UserHash;
            }
        }

        // ---------------------------------------------------------------
        #region Networking

        private readonly SyncDictionary<AVKeys, int> m_ints = new();
        private readonly SyncDictionary<AVKeys, float> m_floats = new();
        private readonly SyncDictionary<AVKeys, string> m_strings = new();
        private readonly SyncDictionary<AVKeys, byte[]> m_blobs = new();

        void Awake() => syncDirection = SyncDirection.ServerToClient;

        public override void OnStartServer()
        {
            base.OnStartServer();

            m_strings[AVKeys.CurrentWorld] = SettingsManager.Server.WorldURL;
            m_ints[AVKeys.VoicePort] = SettingsManager.Server.VoicePort;
        }

        public override void OnStartClient()
        {
            ClientSettings cs = SettingsManager.Client;

            base.OnStartClient();

            m_ints.Callback += OnMIntsChanged;
            m_floats.Callback += OnMFloatsChanged;
            m_strings.Callback += OnMStringsChanged;
            m_blobs.Callback += OnMBlobsChanged;

            SettingsManager.Client.OnAvatarChanged += (x) => AvatarURL = x;
            SettingsManager.Server.OnWorldURLChanged += CommitWorldChanged;

            ResyncInitialValues();

            DownloadClientSettings();

            if(isOwned)
            {
                // That's me, set aside from the unwashed crowd. :)
                XRControl.Me = gameObject;

                // Using directly from Client Settings, because the UserID derived from UserHash hasn't
                // done the full round trip from the server propagation.
                RegisterUser(cs.UserID);

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
                    WorldTransition.InitiateTransition(m_strings[AVKeys.CurrentWorld]);

            }

            InitializeVoice();
        }

        public override void OnStopClient()
        {
            XRControl.Me = null;

            DeinitializeVoice();

            SettingsManager.Server.OnWorldURLChanged -= CommitWorldChanged;
            SettingsManager.Client.OnAvatarChanged -= (x) => AvatarURL = x;

            m_ints.Callback -= OnMIntsChanged;
            m_floats.Callback -= OnMFloatsChanged;
            m_strings.Callback -= OnMStringsChanged;
            m_blobs.Callback -= OnMBlobsChanged;

            base.OnStopClient();
        }

        public void OnDestroy()
        {
            ClientSettings cs = SettingsManager.Client;

            if(isServer)
                SettingsManager.UnregisterUser(cs.UserID);
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
                case AVKeys.ChatOwnID:
                    // Give that avatar its corresponding voice - except its owner.
                    UpdateVoiceID(value); break;
                case AVKeys.AudioStatus:
                    // Either been self-muted or by other means, the announcement comes down from the server.
                    UpdateAudioStatus(value); break;
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
            // Reserved for future use
        }


        [Command]
        private void PropagateInt(AVKeys key, int value) => m_ints[key] = value;

#pragma warning disable IDE0051 // Nicht verwendete private Member entfernen
        [Command]
        private void PropagateFloat(AVKeys key, float value) => m_floats[key] = value;
#pragma warning restore IDE0051 // Nicht verwendete private Member entfernen

        [Command]
        private void PropagateString(AVKeys key, string value) => m_strings[key] = value;

        [Command]
        private void PropagateBlob(AVKeys key, byte[] value) => m_blobs[key] = value;


        [Command]
        private void RegisterUser(string UserHash) => SettingsManager.RegisterUser(UserHash);

        #endregion
        // ---------------------------------------------------------------
        #region Voice handling

        private IEnumerator fdv_cr = null;

        IEnumerator FindDelayedVoice(int ChatOwnID)
        {
            while(Voice == null)
            {
                yield return new WaitForSeconds(1);

                Voice = SettingsManager.Purgatory.Find("UniVoice Peer #" + ChatOwnID);
            }

            Voice.SetParent(transform);
            Voice.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            OnVoiceOutputChanged?.Invoke(Voice.GetComponent<IVoiceOutput>());

            fdv_cr = null;
        }

        private void UpdateVoiceID(int value)
        {
            if(!isOwned)
            {
                if(fdv_cr != null)
                    StopCoroutine(fdv_cr);

                fdv_cr = FindDelayedVoice(value);

                StartCoroutine(fdv_cr);
            }
        }

        private void LoseVoice()
        {
            if(fdv_cr != null) StopCoroutine(fdv_cr);

            if(Voice != null)
            {
                OnVoiceOutputChanged?.Invoke(null);
                Voice.SetParent(SettingsManager.Purgatory);
                Voice.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                Voice = null;
            }
        }

        private void InitializeVoice()
        {
            if(isServer && isOwned)
            {
                // Host mode, this is the host user. Voice chat is always implied.
                ChatOwnID = 0;
            }
            else if(isOwned)
            {
                AudioManager.OnJoinedChatroom += (x) => ChatOwnID = x;

                string ip = NetworkManager.singleton.networkAddress;
                int port = m_ints[AVKeys.VoicePort];

                AudioManager.JoinChatroom($"{ip}:{port}");
            }
        }

        private void DeinitializeVoice()
        {
            LoseVoice();

            if(isServer && isOwned)
            {

            }
            else if(isOwned)
            {
                AudioManager.LeaveChatroom();
                AudioManager.OnJoinedChatroom -= (x) => ChatOwnID = x;
            }
        }

        private void UpdateAudioStatus(int value)
        {
            // Update the own voice status, and for alien avatar it's only informational value,
            // like the status update on the nameplate.
            if(isOwned)
                AudioManager.Instance.MuteSelf = (value != 0);
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
                ReceiveWorldTransition(worldURL);

        }

        private void CommitWorldChanged(string worldURL)
        {
            // We already have transitioned, now we have to tell that the world has changed,
            // and we have to wake up, and for us it is just to find ourselves.
            if(isOwned)
                PropagateWorldTransition(worldURL);
        }

        #endregion

    }
}
