/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.UniVoice;
using Arteranos.Audio;
using Mirror;
using System.Collections;
using UnityEngine;
using Arteranos.Core;

namespace Arteranos.Services
{
    public class VoiceManager : MonoBehaviour
    {
        public static VoiceManager Instance { get; private set; }
        public static ChatroomAgentV2 ChatroomAgent { get; private set; }

        private bool serverActive = false;

        private IEnumerator cs_cr = null;

        private void Awake()
        {
            Instance = this;

        }

        private void Start()
        {
            // FIXME Client/User setting, desired sample setting
            ChatroomAgent = new(
                UVTelepathyNetwork.New(SettingsManager.Server.VoicePort),
                UVMicInput.New(4, 24000),
                new UVAudioOutput.Factory());

            cs_cr = ManageChatServer();

            StartCoroutine(cs_cr);
        }

        private void OnDestroy()
        {
            if(cs_cr != null)
                StopCoroutine(cs_cr);
        }

        private IEnumerator ManageChatServer()
        {
            while(true)
            {
                yield return new WaitForSeconds(2);

                // Follow the voice server setup (or connection) state through the
                // world server's (or connection's) state.

                // Transition offline --> Server or Host
                if(NetworkServer.active && !serverActive)
                {
                    ChatroomAgent.Network.HostChatroom();
                    ChatroomAgent.MuteSelf = true;
                    serverActive = true;
                }
                // Transition Server or Host --> offline 
                else if(!NetworkServer.active && serverActive)
                {
                    ChatroomAgent.Network.CloseChatroom();
                    serverActive = false;
                }

            }
        }
    }
}

