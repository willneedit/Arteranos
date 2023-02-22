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
        public static ChatroomAgent ChatroomAgent { get; private set; }

        private bool serverActive = false;
        private bool clientActive = false;

        private void Awake()
        {
            Instance = this;

            ChatroomAgent = new(
                UVTelepathyNetwork.New(Port),
                new UVMicInput(),
                new UVAudioOutput.Factory());
        }

        // Update is called once per frame
        void Update()
        {
            // Follow the voice server setup (or connection) state through the
            // world server's (or connection's) state.

            // Transition offline --> Server or Host
            if(NetworkServer.active && !serverActive)
            {
                ChatroomAgent.Network.HostChatroom();
                serverActive = true;
            }
            // Transition Server or Host --> offline 
            else if (!NetworkServer.active && serverActive) 
            {
                ChatroomAgent.Network.CloseChatroom();                
                serverActive = false;
            }

            bool isRemoteClient = !NetworkServer.active && NetworkClient.isConnected;

            // Transition offline --> (remote) Client
            if (isRemoteClient && !clientActive)
            {
                ChatroomAgent.Network.JoinChatroom(NetworkManager.singleton.networkAddress);
                clientActive = true;
            }

            // Transition (remote) Client --> offline
            if (!isRemoteClient && clientActive)
            {
                // We may have been disconnected to the voice server together with the world server.
                // That would be okay, but no point trying to announce my leaving.
                if(ChatroomAgent.CurrentMode != ChatroomAgentMode.Unconnected)
                    ChatroomAgent.Network.LeaveChatroom();

                clientActive = false;
            }
        }
    }
}

