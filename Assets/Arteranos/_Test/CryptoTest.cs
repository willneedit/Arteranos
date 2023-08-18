/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.Core;
using System;

namespace Arteranos
{
    public class CryptoTest : MonoBehaviour
    {
        Crypto alice = null;
        Crypto bob = null;

        void Start() => DefaultCryptoTest();

        private void DefaultCryptoTest()
        {
            alice = new();
            bob = new();

            Debug.Log($"Alice SHA256 fingerprint: {alice}");
            Debug.Log($"Alice short Base64 fingerprint: {alice.ToString(Crypto.FP_Base64_10)}");
            Debug.Log($"Alice four words fingerprint: {alice.ToString(Crypto.FP_Dice_4)}");

            EqualityTest();

            CopyTest();

            EncryptTest("This is the first message");

            UserID testuser = new(alice.Fingerprint);

            EncryptStructTest(testuser);

            EncryptTest("This is the second message");

            SignTest("This is the signed message");
        }

        private void EqualityTest()
        {
            Crypto bobComplete = new(bob.Export(true));

            Crypto bobPubOnly = new(bob.Export(false));

            if(bob != bobComplete)
                Debug.Log("FAILED: Equality (with complete key)");

            if(bob != bobPubOnly)
                Debug.Log("FAILED: Equality (with public-only key)");

            if(bob == alice)
                Debug.Log("FAILED: Equality negative test");

        }
        private void CopyTest()
        {
            // Key is supposed to be complete
            Crypto bob2 = new(bob.Export(true));

            Crypto.Encrypt("Testtext", bob.PublicKey, out CryptPacket p1);

            bob2.Decrypt(p1, out string decryptedMessage);

            if(decryptedMessage != "Testtext")
                Debug.LogError($"FAILED: Decrypted message with copied key (+)");

            // This time, it is supposed to be only the public key. (Export(false) == Public Key)
            Crypto bob3 = new(bob.Export(false));

            try
            {
                bob3.Decrypt(p1, out string nothing);
                Debug.LogError($"FAILED: Decrypted message with copied key (-)");
            }
            catch(Exception)
            {
                // Debug.Log("Expected: Caught exception due to failed decryption");
            }

        }

        private void EncryptTest(string msg)
        {
            Crypto.Encrypt(msg, bob.PublicKey, out CryptPacket p);

            bob.Decrypt(p, out string decryptedMessage);

            if(decryptedMessage != msg)
                Debug.LogError($"FAILED: Decrypted message (+)");

            p.encryptedMessage[0] = (byte) ((p.encryptedMessage[0] + 1) % 256);

            try
            {
                bob.Decrypt(p, out string notDecryptedMessage);
                Debug.LogError($"FAILED: Decrypted message (-)");
            }
            catch(Exception)
            {
                // Debug.Log("Expected: Caught exception due to failed decryption");
            }

        }

        private void EncryptStructTest(UserID userID)
        {
            Crypto.Encrypt(userID, bob.PublicKey, out CryptPacket p);

            bob.Decrypt(p, out UserID userID1);

            if(userID != userID1)
                Debug.LogError($"FAILED: Decrypted message is garbled - supposed: {userID1}");

        }

        private void SignTest(string msg)
        {
            alice.Sign(msg, out byte[] signature);

            if(!Crypto.Verify(msg, signature, alice.PublicKey))
                Debug.LogError($"FAILED: Signature verification (+)");

            // Breaking signature for negative test
            signature[0] = (byte) ((signature[0] + 1) % 256);

            if(Crypto.Verify(msg, signature, alice.PublicKey))
                Debug.LogError($"FAILED: Signature verification (-)");

        }
    }
}
