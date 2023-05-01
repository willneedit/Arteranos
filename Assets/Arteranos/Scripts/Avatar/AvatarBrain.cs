using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;
using Arteranos.UniVoice;

using Arteranos.Services;
using Arteranos.Core;
using Arteranos.Audio;
using System;

namespace Arteranos.NetworkIO
{
    public enum AVIntKeys : byte
    {
        _Invalid = 0,
        ChatOwnID,
    }

    public enum AVFloatKeys : byte
    {
        _Invalid = 0,
    }

    public enum AVStringKeys : byte
    {
        _Invalid = 0,
        AvatarURL,
    }

    public enum AVBlobKeys : byte
    {
        _Invalid = 0,
        UserHash,
    }

    public class AvatarBrain : NetworkBehaviour
    {
        public event Action<IVoiceOutput> OnVoiceOutputChanged;
        public event Action<string> OnAvatarChanged;

        private Transform Voice = null;

        public int ChatOwnID
        {
            get => m_ints.ContainsKey(AVIntKeys.ChatOwnID) ? m_ints[AVIntKeys.ChatOwnID] : -1;
            set => PropagateInt(AVIntKeys.ChatOwnID, value);
        }

        public string AvatarURL
        {
            get => m_strings.ContainsKey(AVStringKeys.AvatarURL) ? m_strings[AVStringKeys.AvatarURL] : null;
            set => PropagateString(AVStringKeys.AvatarURL, value);
        }

        public byte[] UserHash
        {
            get => m_blobs.ContainsKey(AVBlobKeys.UserHash) ? m_blobs[AVBlobKeys.UserHash] : null;
            private set => PropagateBlob(AVBlobKeys.UserHash, value); 
        }

        public string UserID
        {
            get
            {
                string hashString = string.Empty;
                foreach(byte x in UserHash) hashString += String.Format("{0:x2}", x);
                return hashString;
            }
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
                UserHash = cs.UserHash;
            }
        }

        // ---------------------------------------------------------------
        #region Networking

#if UNITY_EDITOR
        public readonly SyncDictionary<AVIntKeys, int> m_ints = new();
        public readonly SyncDictionary<AVFloatKeys, float> m_floats = new();
        public readonly SyncDictionary<AVStringKeys, string> m_strings = new();
        public readonly SyncDictionary<AVBlobKeys, byte[]> m_blobs = new();
#else
        private readonly SyncDictionary<AVIntKeys, int> m_ints = new();
        private readonly SyncDictionary<AVFloatKeys, float> m_floats = new();
        private readonly SyncDictionary<AVStringKeys, string> m_strings = new();
        private readonly SyncDictionary<AVBlobKeys, byte[]> m_blobs = new();
#endif
        void Awake()
        {
            syncDirection = SyncDirection.ServerToClient;
            cran = VoiceManager.ChatroomAgent.Network;
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

            // Using directly from Client Settings, because the UserID derived from UserHash hasn't
            // done the full round trip from the server propagation.
            if(isOwned)
                RegisterUser(cs.UserID);

            // TODO Need to implement the server data maintenance in the connection establishment,
            // like the Server List UI.
            //if(isOwned)
            //    SettingsManager.Client.ConnectedServer = null;

            InitializeVoice();
        }

        public override void OnStopClient()
        {
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
            if(isServer)
                SettingsManager.UnregisterUser(UserID);

            if(isOwned)
                SettingsManager.Client.ConnectedServer = null;
        }

        private void ResyncInitialValues()
        {
            foreach(KeyValuePair<AVIntKeys, int> kvpi in m_ints) 
                OnMIntsChanged(SyncDictionary<AVIntKeys, int>.Operation.OP_ADD, kvpi.Key, kvpi.Value);

            foreach(KeyValuePair<AVFloatKeys, float> kvpf in m_floats)
                OnMFloatsChanged(SyncDictionary<AVFloatKeys, float>.Operation.OP_ADD, kvpf.Key, kvpf.Value);

            foreach(KeyValuePair<AVStringKeys, string> kvps in m_strings)
                OnMStringsChanged(SyncDictionary<AVStringKeys, string>.Operation.OP_ADD, kvps.Key, kvps.Value);

            foreach(KeyValuePair<AVBlobKeys, byte[]> kvpb in m_blobs)
                OnMBlobsChanged(SyncDictionary<AVBlobKeys, byte[]>.Operation.OP_ADD, kvpb.Key, kvpb.Value);

        }

        private void OnMIntsChanged(SyncDictionary<AVIntKeys, int>.Operation op, AVIntKeys key, int value)
        {
            // Give that avatar its corresponding voice - except its owner.
            switch(key)
            {
                case AVIntKeys.ChatOwnID:
                    UpdateVoiceID(value); break;
            }
        }

        private void OnMFloatsChanged(SyncIDictionary<AVFloatKeys, float>.Operation op, AVFloatKeys key, float value)
        {
            // Reserved for future use
        }

        private void OnMStringsChanged(SyncIDictionary<AVStringKeys, string>.Operation op, AVStringKeys key, string value)
        {
            switch(key)
            {
                case AVStringKeys.AvatarURL:
                    OnAvatarChanged?.Invoke(value); break;
            }
        }

        private void OnMBlobsChanged(SyncIDictionary<AVBlobKeys, byte[]>.Operation op, AVBlobKeys key, byte[] value)
        {
            // Reserved for future use
        }


        [Command]
        private void PropagateInt(AVIntKeys key, int value) => m_ints[key] = value;

#pragma warning disable IDE0051 // Nicht verwendete private Member entfernen
        [Command]
        private void PropagateFloat(AVFloatKeys key, float value) => m_floats[key] = value;
#pragma warning restore IDE0051 // Nicht verwendete private Member entfernen

        [Command]
        private void PropagateString(AVStringKeys key, string value) => m_strings[key] = value;

        [Command]
        private void PropagateBlob(AVBlobKeys key, byte[] value) => m_blobs[key] = value;


        [Command]
        private void RegisterUser(string UserHash) => SettingsManager.RegisterUser(UserHash);

        #endregion
        // ---------------------------------------------------------------
        #region Voice handling

        private IChatroomNetworkV2 cran = null;
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
                cran.OnJoinedChatroom += (x) => ChatOwnID = x;

                string ip = NetworkManager.singleton.networkAddress;
                int port = SettingsManager.Client.ConnectedServer.VoicePort;

                cran.JoinChatroom($"{ip}:{port}");
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
                cran.LeaveChatroom();
                cran.OnJoinedChatroom -= (x) => ChatOwnID = x;
            }
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

            // Only differing if it's the remote change announce - the local is done
            // in the PrefPanel_Moderation.
            if(ss.WorldURL == worldURL)
            {
                Debug.Log("World transition - already in that targeted world.");
                return;
            }

            // Now that's the real deal.
            Debug.Log($"WORLD TRANSITION: From {ss.WorldURL} to {worldURL} - Choose now!");

            UI.WorldTransitionUI.ShowWorldChangeDialog(worldURL, 
            (response) => OnWorldChangeAnswer(worldURL, response));
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
                UI.WorldTransitionUI.InitiateTransition(worldURL);
        }

        [Command]
        private void PropagateWorldTransition(string worldURL)
        {
            ReceiveWorldTransition(worldURL);

            // Pure server needs to be notified and transitioned, too.
            if(isServer && !isClient && !string.IsNullOrEmpty(worldURL))
            {
                SettingsManager.Server.WorldURL = worldURL;
                UI.WorldTransitionUI.InitiateTransition(worldURL);
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

    }
}
