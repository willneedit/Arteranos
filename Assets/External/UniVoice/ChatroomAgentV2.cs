using System;
using System.Linq;
using System.Collections.Generic;

namespace Adrenak.UniVoice {
    /// <summary>
    /// Provides the means to host or connect to a chatroom.
    /// </summary>
    public class ChatroomAgentV2 : IDisposable {
        // ====================================================================
        #region PROPERTIES
        // ====================================================================
        /// <summary>
        /// The underlying network which the agent uses to host or connect to 
        /// chatrooms, and send and receive data to and from peers
        /// </summary>
        public IChatroomNetworkV2 Network { get; private set; }

        /// <summary>
        /// Source of outgoing audio that can be 
        /// transmitted over the network to peers
        /// </summary>
        public IAudioInputV2 AudioInput { get; private set; }

        /// <summary>
        /// A factory that returns an <see cref="IAudioOutput"/> 
        /// instance. Used every time a Peer connects for that peer to get
        /// an output for that peer.
        /// </summary>
        public IAudioOutputFactoryV2 AudioOutputFactory { get; private set; }

        /// <summary>
        /// There is a <see cref="IAudioOutput"/> for each peer that gets
        /// created using the provided <see cref="AudioOutputFactory"/>
        /// The <see cref="IAudioOutput"/> instance corresponding to a peer is
        /// responsible for playing the audio that we receive that peer. 
        /// </summary>
        public Dictionary<short, IAudioOutputV2> PeerOutputs;

        /// <summary>
        /// The current <see cref="ChatroomAgentMode"/> of this agent
        /// </summary>
        public ChatroomAgentMode CurrentMode { get; private set; }

        /// <summary>
        /// Mutes all the peers. If set to true, no incoming audio from other 
        /// peers will be played. If you want to selectively mute a peer, use
        /// the <see cref="ChatroomPeerSettings.muteThem"/> flag in the 
        /// <see cref="PeerSettings"/> instance for that peer.
        /// Note that setting this will not change <see cref="PeerSettings"/>
        /// </summary>
        public bool MuteOthers { get; set; }

        /// <summary>
        /// Whether this agent is muted or not. If set to true, voice data will
        /// not be sent to ANY peer. If you want to selectively mute yourself 
        /// to a peer, use the <see cref="ChatroomPeerSettings.muteSelf"/> 
        /// flag in the <see cref="PeerSettings"/> instance for that peer.
        /// Note that setting this will not change <see cref="PeerSettings"/>
        /// </summary>
        public bool MuteSelf { get; set; }

        /// <summary>
        /// <see cref="ChatroomPeerSettings"/> for each peer which allows you
        /// to read or change the settings for a specific peer. Use [id] to get
        /// settings for a peer with ID id;
        /// </summary>
        public Dictionary<short, ChatroomPeerSettings> PeerSettings;
        #endregion

        // ====================================================================
        #region CONSTRUCTION / DISPOSAL
        // ====================================================================
        /// <summary>
        /// Creates and returns a new agent using the provided dependencies.
        /// The instance then makes the dependencies work together.
        /// </summary>
        /// 
        /// <param name="chatroomNetwork">The chatroom network implementation
        /// for chatroom access and sending data to peers in a chatroom.
        /// </param>
        /// 
        /// <param name="audioInput">The source of the outgoing audio</param>
        /// 
        /// <param name="audioOutputFactory">
        /// The factory used for creating <see cref="IAudioOutput"/> instances 
        /// for peers so that incoming audio from peers can be played.
        /// </param>
        public ChatroomAgentV2(
            IChatroomNetworkV2 chatroomNetwork,
            IAudioInputV2 audioInput,
            IAudioOutputFactoryV2 audioOutputFactory
        ) {
            AudioInput = audioInput ??
            throw new ArgumentNullException(nameof(audioInput));

            Network = chatroomNetwork ??
            throw new ArgumentNullException(nameof(chatroomNetwork));

            AudioOutputFactory = audioOutputFactory ??
            throw new ArgumentNullException(nameof(audioOutputFactory));

            CurrentMode = ChatroomAgentMode.Unconnected;
            MuteOthers = false;
            MuteSelf = false;
            PeerSettings = new Dictionary<short, ChatroomPeerSettings>();
            PeerOutputs = new Dictionary<short, IAudioOutputV2>();

            LinkDependencies();
        }

        /// <summary>
        /// Disposes the instance. WARNING: Calling this method will
        /// also dispose the dependencies passed to it in the constructor.
        /// Be mindful of this if you're sharing dependencies between multiple
        /// instances and/or using them outside this instance.
        /// </summary>
        public void Dispose() {
            AudioInput.Dispose();

            RemoveAllPeers();
            PeerSettings.Clear();
            PeerOutputs.Clear();

            Network.Dispose();
        }
        #endregion

        // ====================================================================
        #region INTERNAL 
        // ====================================================================
        void LinkDependencies() {
            // Network events
            Network.OnCreatedChatroom += () =>
                CurrentMode = ChatroomAgentMode.Host;
            Network.OnClosedChatroom += () => {
                CurrentMode = ChatroomAgentMode.Unconnected;
                RemoveAllPeers();
            };
            Network.OnJoinedChatroom += id => {
                CurrentMode = ChatroomAgentMode.Guest;
            };
            Network.OnLeftChatroom += () => {
                RemoveAllPeers();
                CurrentMode = ChatroomAgentMode.Unconnected;
            };
            Network.OnPeerJoinedChatroom += id => 
                AddPeer(id);
            Network.OnPeerLeftChatroom += id =>
                RemovePeer(id);

            // Stream the incoming audio data using the right peer output
            Network.OnAudioReceived += (peerID, data) => {
                // if we're muting all, no point continuing.
                if (MuteOthers) return;

                byte[] samples = data.samples;

                if (PeerSettings.ContainsKey(peerID) && !PeerSettings[peerID].muteThem)
                    PeerOutputs[peerID].Feed(samples);
            };

            AudioInput.OnSegmentReady += (index, samples) => {
                // If we're muting ourselves to all, no point continuing
                if (MuteSelf) return;

                // Get all the recipients we haven't muted ourselves to
                IEnumerable<short> recipients = Network.PeerIDs
                    .Where(x => PeerSettings.ContainsKey(x) && !PeerSettings[x].muteSelf);

                // Send the audio segment to every deserving recipient
                Network.SendAudioSegment(recipients.ToArray(), new ChatroomAudioSegmentV2 {
                    samples = samples
                });
            };
        }

        void AddPeer(short id) {
            if (!PeerSettings.ContainsKey(id))
                PeerSettings.Add(id, new ChatroomPeerSettings());
            if(!PeerOutputs.ContainsKey(id)) {
                IAudioOutputV2 output = AudioOutputFactory.Create(
                    AudioInput.Frequency,
                    AudioInput.ChannelCount
                );
                output.ID = id.ToString();
                PeerOutputs.Add(id, output);
            }
        }

        void RemovePeer(short id) {
            if (PeerSettings.ContainsKey(id))
                PeerSettings.Remove(id);
            if (PeerOutputs.ContainsKey(id)) {
                PeerOutputs[id].Dispose();
                PeerOutputs.Remove(id);
            }
        }

        void RemoveAllPeers() {
            List<short> peers = Network.PeerIDs;
            foreach(short peer in peers) 
                RemovePeer(peer);
        }
        #endregion
    }
}
