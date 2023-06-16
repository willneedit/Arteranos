/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Arteranos.Core.Crypto;
using System.Linq;

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
            alice = new();
            bob = new();

            Debug.Log($"Pubkey Alice: {Hex(alice.PublicKey)}");
            Debug.Log($"Pubkey Bob  : {Hex(bob.PublicKey)}");


            EncryptTest("This is the first message");

            EncryptTest("This is the second message");
        }

        private void EncryptTest(string msg)
        {
            alice.Encrypt(msg, bob.PublicKey, out byte[] iv, out byte[] encryptedSessionKey, out byte[] encryptedMessage);

            Debug.Log($"Ciphertext: {Hex(encryptedMessage)}");

            bob.Decrypt(iv, encryptedSessionKey, encryptedMessage, out string decryptedMessage);

            if(decryptedMessage != msg)
                Debug.LogError($"FAILED: Decrypted message is garbled - supposed: {msg}");
        }
    }
}
