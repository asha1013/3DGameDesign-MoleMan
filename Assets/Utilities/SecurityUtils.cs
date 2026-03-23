// 
// Copyright (c) 2024 Off The Beaten Track UG
// All rights reserved.
// 
// Maintainer: Jens Bahr
//

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Sparrow.Utilities
{
    public class SecurityUtils
    {
        private const string CryptoKey = "YgievgP7rpg6wbWd5qQrAwBdGzgHeMcY";
        private const int PBKDF2_ITERATIONS = 10000;

        public static string EncryptString(string plainText, string key, byte version = 1)
        {
            if (version == 1)
            {
                return EncryptStringV1(plainText, key);
            }
            else if (version == 2)
            {
                return EncryptStringV2(plainText, key);
            }
            throw new ArgumentException($"Unsupported encryption version: {version}");
        }

        public static string DecryptString(string cipherText, string key, byte version = 1)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                if (version == 1)
                {
                    return DecryptStringV1(cipherText, key);
                }
                else if (version == 2)
                {
                    return DecryptStringV2(cipherText, key);
                }
                throw new ArgumentException($"Unsupported encryption version: {version}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Decryption failed: {ex.Message}");
                return string.Empty;
            }
        }

        // Version 1: Original XOR-based encryption
        private static string EncryptStringV1(string plainText, string key)
        {
            byte[] salt = GenerateSalt();
            key = ApplySaltToKey(key, salt);
            byte[] plainData = Encoding.UTF8.GetBytes(plainText);
            byte[] keyData = GenerateKey(key, plainData.Length);
            byte[] encryptedData = Encrypt(plainData, keyData);
            byte[] result = new byte[salt.Length + encryptedData.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(encryptedData, 0, result, salt.Length, encryptedData.Length);
            return Convert.ToBase64String(result);
        }

        private static string DecryptStringV1(string cipherText, string key)
        {
            byte[] encryptedWithSalt = Convert.FromBase64String(cipherText);
            byte[] salt = new byte[16];
            byte[] encryptedData = new byte[encryptedWithSalt.Length - salt.Length];
            Buffer.BlockCopy(encryptedWithSalt, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(encryptedWithSalt, salt.Length, encryptedData, 0, encryptedData.Length);
            key = ApplySaltToKey(key, salt);
            byte[] keyData = GenerateKey(key, encryptedData.Length);
            byte[] decryptedData = Decrypt(encryptedData, keyData);
            return Encoding.UTF8.GetString(decryptedData);
        }

        // Version 2: AES-based encryption
        private static string EncryptStringV2(string plainText, string key)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] salt = GenerateSalt();
                byte[] iv = aes.IV;  // Generate a new IV

                // Derive key using PBKDF2
                using (var deriveBytes = new Rfc2898DeriveBytes(key, salt, PBKDF2_ITERATIONS))
                {
                    aes.Key = deriveBytes.GetBytes(32);  // 256 bits
                }

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // Format: version(1) + salt(16) + iv(16) + encrypted_data
                    msEncrypt.WriteByte(2);  // Version marker
                    msEncrypt.Write(salt, 0, salt.Length);
                    msEncrypt.Write(iv, 0, iv.Length);

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private static string DecryptStringV2(string cipherText, string key)
        {
            byte[] cipherData = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            using (MemoryStream msDecrypt = new MemoryStream(cipherData))
            {
                // Read version byte
                int version = msDecrypt.ReadByte();
                if (version != 2) throw new ArgumentException("Invalid version in V2 decrypt");

                // Read salt and IV
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];
                msDecrypt.Read(salt, 0, salt.Length);
                msDecrypt.Read(iv, 0, iv.Length);

                // Derive key using PBKDF2
                using (var deriveBytes = new Rfc2898DeriveBytes(key, salt, PBKDF2_ITERATIONS))
                {
                    aes.Key = deriveBytes.GetBytes(32);
                    aes.IV = iv;
                }

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        // Existing helper methods
        private static byte[] Encrypt(byte[] plainData, byte[] key)
        {
            byte[] encryptedData = new byte[plainData.Length];
            for (int i = 0; i < plainData.Length; i++)
            {
                encryptedData[i] = (byte)(plainData[i] ^ key[i]);
            }
            return encryptedData;
        }

        private static byte[] Decrypt(byte[] encryptedData, byte[] key)
        {
            return Encrypt(encryptedData, key); // XOR is symmetric
        }

        private static byte[] GenerateKey(string key, int length)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] keyData = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                byte[] extendedKey = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    extendedKey[i] = keyData[i % keyData.Length];
                }
                return extendedKey;
            }
        }

        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private static string ApplySaltToKey(string key, byte[] salt)
        {
            return Convert.ToBase64String(salt) + key;
        }

        public static string GenerateRandomKey(int length = 32)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!§$%&/()=?";
            StringBuilder sb = new StringBuilder();
            System.Random rand = new System.Random();
            while (0 < length--)
            {
                sb.Append(validChars[rand.Next(validChars.Length)]);
            }
            return sb.ToString();
        }

        public static string GenerateKey(string externalKey)
        {
            return CombineKeys(externalKey, CryptoKey);
        }

        private static string CombineKeys(string key1, string key2)
        {
            string ret = "";
            for (int i = 0; i < Math.Max(key1.Length, key2.Length); i++)
                ret += ((i < key1.Length ? key1[i] : 'w') + (i < key2.Length ? key2[i] : 'o'));
            return ret;
        }
    }
}
