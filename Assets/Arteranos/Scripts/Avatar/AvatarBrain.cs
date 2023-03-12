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
    public enum AVIntKeys : int
    {
        _Invalid = 0,
        ChatOwnID,
    }

    public enum AVFloatKeys : int
    {
        _Invalid = 0,
    }

    public enum AVStringKeys : int
    {
        _Invalid = 0,
        AvatarURL,
    }

    public class AvatarBrain : NetworkBehaviour
    {
        public readonly SyncDictionary<AVIntKeys, int> m_ints = new();
        public readonly SyncDictionary<AVFloatKeys, float> m_floats = new();
        public readonly SyncDictionary<AVStringKeys, string> m_strings = new();

        public Transform Voice { get; private set; } = null;

        public event Action<IVoiceOutput> OnVoiceOutputChanged;
        public event Action<string> OnAvatarChanged;

        void Awake()
        {
            syncDirection = SyncDirection.ServerToClient;
            cran = VoiceManager.ChatroomAgent.Network;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            m_ints.Callback += OnMIntsChanged;
            m_floats.Callback += OnMFloatsChanged;
            m_strings.Callback += OnMStringsChanged;

            SettingsManager.Client.OnAvatarChanged += PropagateAvatarChanged;

            ResyncInitialValues();

            DownloadClientSettings();

            InitializeVoice();
        }

        public override void OnStopClient()
        {
            DeinitializeVoice();

            SettingsManager.Client.OnAvatarChanged -= PropagateAvatarChanged;

            m_ints.Callback -= OnMIntsChanged;
            m_floats.Callback -= OnMFloatsChanged;
            m_strings.Callback -= OnMStringsChanged;

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
                PropagateAvatarChanged(cs.AvatarURL);
            }
        }

        private void ResyncInitialValues()
        {
            foreach(KeyValuePair<AVIntKeys, int> kvpi in m_ints) 
                OnMIntsChanged(SyncDictionary<AVIntKeys, int>.Operation.OP_ADD, kvpi.Key, kvpi.Value);

            foreach(KeyValuePair<AVFloatKeys, float> kvpf in m_floats)
                OnMFloatsChanged(SyncDictionary<AVFloatKeys, float>.Operation.OP_ADD, kvpf.Key, kvpf.Value);

            foreach(KeyValuePair<AVStringKeys, string> kvps in m_strings)
                OnMStringsChanged(SyncDictionary<AVStringKeys, string>.Operation.OP_ADD, kvps.Key, kvps.Value);
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


        // ---------------------------------------------------------------
        #region Voice handling

        private IChatroomNetworkV2 cran = null;
        private IEnumerator fdv_cr = null;

        [Command]
        private void PropagateVoiceChatID(short OwnID) => m_ints[AVIntKeys.ChatOwnID] = OwnID;

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
                PropagateVoiceChatID(0);
            }
            else if(isOwned)
            {
                cran.OnJoinedChatroom += PropagateVoiceChatID;
                cran.JoinChatroom(NetworkManager.singleton.networkAddress);
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
                cran.OnJoinedChatroom -= PropagateVoiceChatID;
            }
        }


        #endregion
        // ---------------------------------------------------------------

        [Command]
        private void PropagateAvatarChanged(string _new) => m_strings[AVStringKeys.AvatarURL] = _new;
    }
}
