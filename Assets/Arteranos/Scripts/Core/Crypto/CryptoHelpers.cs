using Arteranos.Core;
using Ipfs.Cryptography.Proto;
using System;
using System.Numerics;
using System.Security.Cryptography;

/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

public static class CryptoHelpers
{

    public const string FP_SHA256 = "SHA256";          // Full SHA256 hexdump fingerprint (length: 64)
    public const string FP_SHA256_16 = "SHA256_8";     // Leading 16 hex digits (64 Bits entropy)
    public const string FP_SHA256_20 = "SHA256_10";    // Leading 20 hex digits (80 Bits entropy)

    public const string FP_Base64 = "Basde64";         // Full Base64 fingerprint (length: 44)
    public const string FP_Base64_8 = "Base64_8";      // Leading 8 'digits' (48 Bits entropy)
    public const string FP_Base64_10 = "Base64_10";    // Leading 10 'digits' (60 Bits entropy)
    public const string FP_Base64_15 = "Base64_15";    // Leading 15 'digits' (90 Bits entropy)

    public const string FP_Dice_4 = "Dice4";           // Four Diceware words (51 Bits entropy)
    public const string FP_Dice_5 = "Dice5";           // Five Diceware words (64 Bits entropy)


    public static string WordListSelector(byte[] fpBytes, int howmany)
    {
        // Add a null byte as the MSB and the cleared sign bit.
        byte[] unsignedfpbytes = new byte[fpBytes.Length + 1];
        fpBytes.CopyTo(unsignedfpbytes, 0);

        string[] words = new string[howmany];
        BigInteger fpBI = new(unsignedfpbytes);

        for(int i = 0; i < howmany; i++)
        {
            fpBI = BigInteger.DivRem(fpBI, Words.words.Length, out BigInteger rem);
            words[i] = Words.words[(int) rem];
        }

        return string.Join(" ", words);
    }

    public static byte[] GetFingerprint(UserID userID)
        => GetFingerprint(userID.SignPublicKey.Serialize());
    public static byte[] GetFingerprint(PublicKey publicKey) 
        => GetFingerprint(publicKey.Serialize());
    public static byte[] GetFingerprint(byte[] publicKey)
    {
        using IncrementalHash myHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        myHash.AppendData(publicKey);
        return myHash.GetHashAndReset();
    }

    public static string ToString(string v, UserID userID)
        => ToString(v, userID.SignPublicKey.Serialize());

    public static string ToString(string v, byte[] publicKey)
    {
        switch(v)
        {
            case FP_Dice_4:
                return WordListSelector(GetFingerprint(publicKey), 4);
            case FP_Dice_5:
                return WordListSelector(GetFingerprint(publicKey), 5);
            case FP_Base64_15:
                return ToString(FP_Base64, publicKey)[0..14];
            case FP_Base64_10:
                return ToString(FP_Base64, publicKey)[0..9];
            case FP_Base64_8:
                return ToString(FP_Base64, publicKey)[0..7];
            case FP_Base64:
                return Convert.ToBase64String(GetFingerprint(publicKey));
            case FP_SHA256_20:
                return ToString(FP_SHA256, publicKey)[0..19];
            case FP_SHA256_16:
                return ToString(FP_SHA256, publicKey)[0..15];
            case FP_SHA256:
            default:
                string hashString = string.Empty;
                foreach(byte x in GetFingerprint(publicKey)) { hashString += String.Format("{0:x2}", x); }
                return hashString;
        }
    }
}