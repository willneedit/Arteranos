using System;

namespace Adrenak.UniVoice {
    /// <summary>
    /// A chatroom specific networking interface for creating & joining 
    /// chatrooms and sending & receiving data to and from chatroom peers. 
    /// </summary>
    public interface IChatroomNetworkV2 : IChatroomNetwork, IDisposable
    {
        // ====================================================================
        #region EVENTS
        // ====================================================================
        #endregion

        // ====================================================================
        #region PROPERTIES
        // ====================================================================
        #endregion

        // ====================================================================
        #region METHODS
        // ====================================================================

        void SendAudioSegment(short[] peerID, ChatroomAudioSegment data);
        #endregion
    }
}
