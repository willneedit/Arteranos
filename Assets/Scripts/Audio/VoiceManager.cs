/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Adrenak.UniVoice;
using Mirror;
using UnityEngine;

namespace Arteranos.Audio
{
    public class VoiceManager : MonoBehaviour
    {
        public int Port = 7778;

        public static VoiceManager Instance { get; private set; }
        public static ChatroomAgent ChatServer { get; private set; }

        private bool active = false;

        private void Awake()
        {
            Instance = this;

            ChatServer = new(
                UVTelepathyNetwork.New(Port),
                new UVMicInput(),
                new UVAudioOutput.Factory());
        }

        // Update is called once per frame
        void Update()
        {
            if (NetworkServer.active && !active) 
            {
                ChatServer.Network.HostChatroom();
                active = true;
            }
            else if (!NetworkServer.active && active)
            {
                ChatServer.Network.CloseChatroom();                
                active = false;
            }
        }
    }
}

