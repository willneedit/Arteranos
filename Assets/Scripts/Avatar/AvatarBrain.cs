using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;
using Adrenak.UniVoice;

using Arteranos.Services;
using Arteranos.Core;

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
    }

    public class AvatarBrain : NetworkBehaviour
    {
        public readonly SyncDictionary<AVIntKeys, int> m_ints = new();
        public readonly SyncDictionary<AVFloatKeys, float> m_floats = new();
        public readonly SyncDictionary<AVStringKeys, string> m_strings = new();

        void Awake()
        {
            syncDirection = SyncDirection.ServerToClient;
            cra = VoiceManager.ChatroomAgent;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            m_ints.Callback += OnMIntsChanged;

            ResyncInitialValues();

            if(isServer && isOwned)
            {
                // Host mode, this is the host user. Voice chat is always implied.
                PropagateVoiceChatID(0);
            }
            else if(isOwned)
            {
                cra.Network.OnJoinedChatroom += PropagateVoiceChatID;
                cra.Network.JoinChatroom(NetworkManager.singleton.networkAddress);
            }
        }

        public override void OnStopClient()
        {
            m_ints.Callback -= OnMIntsChanged;

            LoseVoice();

            if(isServer && isOwned)
            {

            }
            else if(isOwned)
            {
                cra.Network.LeaveChatroom();
                cra.Network.OnJoinedChatroom -= PropagateVoiceChatID;
            }

            base.OnStopClient();
        }

        // TODO Incomplete
        private void ResyncInitialValues()
        {
            foreach(KeyValuePair<AVIntKeys, int> kvpi in m_ints) 
                OnMIntsChanged(SyncDictionary<AVIntKeys, int>.Operation.OP_ADD, kvpi.Key, kvpi.Value);
        }

        // TODO Incomplete
        private void OnMIntsChanged(SyncDictionary<AVIntKeys, int>.Operation op, AVIntKeys key, int value)
        {
            // Give that avatar its corresponding voice - except its owner.
            if(key == AVIntKeys.ChatOwnID)
                UpdateVoiceID(value);
        }


        // ---------------------------------------------------------------
        #region Voice handling

        private ChatroomAgentV2 cra = null;
        private IEnumerator fdv_cr = null;

        [Command]
        private void PropagateVoiceChatID(short OwnID) => m_ints[AVIntKeys.ChatOwnID] = OwnID;

        IEnumerator FindDelayedVoice(int ChatOwnID)
        {
            Transform voice = null;

            while(voice == null)
            {
                yield return new WaitForSeconds(1);

                voice = SettingsManager.Purgatory.Find("UniVoice Peer #" + ChatOwnID);
            }

            voice.SetParent(transform);
            voice.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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

            // At first when your mortal body will go out, your voice stays, for a time.
            if(m_ints.TryGetValue(AVIntKeys.ChatOwnID, out int chatid))
            {
                Transform voice = transform.Find("UniVoice Peer #" + chatid);
                if(voice != null)
                {
                    voice.SetParent(SettingsManager.Purgatory);
                    voice.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
            }
        }

        #endregion
        // ---------------------------------------------------------------
    }
}
