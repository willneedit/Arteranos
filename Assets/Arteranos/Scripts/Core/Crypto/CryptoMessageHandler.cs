/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ipfs.Core.Cryptography.Proto;
using ProtoBuf;

namespace Arteranos.Core.Cryptography
{
    [ProtoContract]
    internal class SignedMessage
    {
        [ProtoMember(1)]
        public byte[] data;
        [ProtoMember(2)]
        public PublicKey signerPubKey;
        [ProtoMember(3)]
        public byte[] signature;
    }

    [ProtoContract]
    public class ReceiverKey
    {
        [ProtoMember(1)]
        public byte[] receiverAgrFingerprint;
        [ProtoMember(2)]
        public byte[] wrapIV;
        [ProtoMember(3)]
        public byte[] wrappedSessionKey;
    }

    [ProtoContract]
    public class CMSPacket
    {
        [ProtoMember(1)]
        public PublicKey senderAgrPubKey;
        [ProtoMember(2)]
        public List<ReceiverKey> receiverKeys;
        [ProtoMember(3)]
        public byte[] IV;
        [ProtoMember(4)]
        public byte[] encryptedSignedMessage;
    }

    public class CryptoMessageHandler : IDisposable
    {
        private readonly ISignKey OwnerSignKey = null;
        private IAgreeKey SessionKey = null;

        public CryptoMessageHandler(ISignKey ownerSignKey) 
        {
            OwnerSignKey = ownerSignKey;
            SessionKey = AgreeKey.Generate();
        }

        public PublicKey AgreePublicKey => SessionKey.PublicKey;

        public PublicKey SignPublicKey => OwnerSignKey?.PublicKey;

        #region Public

        public void ReceiveMessage(CMSPacket message, out byte[] data, out PublicKey signerPublicKey)
        {
            byte[] signedMessageData = null;
            if (message.receiverKeys?.Count > 0)
            {
                // Decrypt message (if it's for this handler)
                using SymmetricKey decryptKey = FindReceiverKey(message);
                decryptKey.Decrypt(message.encryptedSignedMessage, out signedMessageData);
            }
            else // No encryption, take message as plaintext
                signedMessageData = message.encryptedSignedMessage;

            using MemoryStream ms = new(signedMessageData);
            SignedMessage signedMessage = Serializer.Deserialize<SignedMessage>(ms);

            data = signedMessage.data;

            // No signature?
            if (signedMessage.signerPubKey == null)
                signerPublicKey = null;
            else
            {
                // If there's one, check it.
                signerPublicKey = signedMessage.signerPubKey;
                signerPublicKey.Verify(signedMessage.data, signedMessage.signature);
            }
        }

        public void TransmitMessage(byte[] data, PublicKey[] receivers, out CMSPacket message)
        {
            message = TransmitMessage(data, receivers);
        }

        public void TransmitMessage(byte[] data, PublicKey receiver, out CMSPacket message) 
            => TransmitMessage(data, new[] { receiver }, out message);

        public void Sign(byte[] data, out byte[] signature)
            => OwnerSignKey.Sign(data, out signature);

        #endregion

        #region Receiver

        internal SymmetricKey MatchReceiverKey(ReceiverKey attempt, PublicKey senderAgrPubKey, byte[] messageIV)
        {
            // Only when this receiver's AgreeKey Fingerprint is the right one.
            if(!attempt.receiverAgrFingerprint.SequenceEqual(SessionKey.Fingerprint))
                return null;

            // Combine your and the sender's AgreeKey halves into the shared secret
            SessionKey.Agree(senderAgrPubKey, out byte[] sharedSecret);

            // Treat the shared secret as an ephemeral key for this session
            using SymmetricKey unwrapKey = SymmetricKey.Import(sharedSecret, attempt.wrapIV);

            // And decrypt the actual message key
            unwrapKey.Decrypt(attempt.wrappedSessionKey, out byte[] messageKey);

            // Now we're ready to decrypt the actual message.
            return SymmetricKey.Import(messageKey, messageIV);
        }

        internal SymmetricKey FindReceiverKey(CMSPacket message)
        {
            foreach(ReceiverKey receiverKey in message.receiverKeys)
            {
                SymmetricKey key = MatchReceiverKey(receiverKey, message.senderAgrPubKey, message.IV);
                if(key != null) return key;
            }

            throw new CryptographicException("Message is not supposed for this handler");
        }

        #endregion

        #region Transmitter

        internal void SignMessage(SignedMessage message)
        {
            if (OwnerSignKey == null)
            {
                message.signerPubKey = null;
                message.signature = null;
                return;
            }

            OwnerSignKey.Sign(message.data, out message.signature);
            message.signerPubKey = OwnerSignKey.PublicKey;
        }

        internal SymmetricKey WrapMessage(byte[] data, out CMSPacket encryptedMessage)
        {
            SignedMessage signed = new()
            {
                data = data
            };

            SignMessage(signed);

            SymmetricKey messageKey = SymmetricKey.Generate();

            using MemoryStream ms = new();
            Serializer.Serialize(ms, signed);
            ms.Position = 0;

            encryptedMessage = new()
            {
                senderAgrPubKey = AgreePublicKey,
                IV = messageKey.IV,
                encryptedSignedMessage = ms.ToArray()
            };

            // Not yet encrypted. Leave it until we have the receivers found out.
            return messageKey;
        }

        internal ReceiverKey WrapForReceiver(SymmetricKey messageKey, PublicKey receiverAgrPublicKey)
        {
            SessionKey.Agree(receiverAgrPublicKey, out byte[] sharedSecret);
            ReceiverKey receiverKey = new();

            Random rand = new();
            receiverKey.wrapIV = new byte[16];
            rand.NextBytes(receiverKey.wrapIV);
            using SymmetricKey ephem = SymmetricKey.Import(sharedSecret, receiverKey.wrapIV);
            ephem.Encrypt(messageKey.Key, out receiverKey.wrappedSessionKey);
            receiverKey.receiverAgrFingerprint = CryptoHelpers.GetFingerprint(receiverAgrPublicKey);

            return receiverKey;
        }

        internal CMSPacket TransmitMessage(byte[] data, PublicKey[] receivers)
        {
            using SymmetricKey messageKey = WrapMessage(data, out CMSPacket message);
            if(receivers?.Length > 0)
            {
                messageKey.Encrypt(message.encryptedSignedMessage, out message.encryptedSignedMessage);
                message.receiverKeys = new();
                foreach(PublicKey receiver in receivers)
                    message.receiverKeys.Add(WrapForReceiver(messageKey, receiver));
            }
            else
            {
                // Leave unencrypted, no receivers
                message.IV = null;
                message.receiverKeys = null;
            }

            return message;
        }

        #endregion

        #region Boilerplate
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SessionKey = null;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
