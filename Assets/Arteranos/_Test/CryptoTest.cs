/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.Core;

namespace Arteranos
{
    public class CryptoTest : MonoBehaviour
    {
        Crypto alice = null;
        Crypto bob = null;

        string Hex(byte[] b)
        {
            string str = string.Empty;
            foreach(byte x in b) { str += string.Format("{0:x2}", x);  }
            return str;
        }

        void Start()
        {
            DefaultCryptoTest();
        }

        private void DefaultCryptoTest()
        {
            alice = new();
            bob = new();

            Debug.Log($"Pubkey Alice: {Hex(alice.PublicKey)}");
            Debug.Log($"Pubkey Bob  : {Hex(bob.PublicKey)}");


            EncryptTest("This is the first message");

            CopyTest();

            UserID testuser = new("Google", "6785874", null);

            EncryptStructTest(testuser);

            EncryptTest("This is the second message");

            SignTest("This is the signed message");
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
            catch(System.Exception)
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
            catch(System.Exception)
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
