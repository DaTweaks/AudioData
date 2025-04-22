using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.Encoders.Crypto
{
    public class AESCrypto : Crypto
    {
        public override string CryptoName => "AES Crypto";

        public override byte[] Encrypt(byte[] input, byte[] key)
        {
            // Generate a random IV for each encryption (unique for each message)
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    // Write the input to the CryptoStream
                    cryptoStream.Write(input, 0, input.Length);
                    cryptoStream.FlushFinalBlock();

                    // Get the encrypted data
                    byte[] encryptedData = memoryStream.ToArray();

                    // Create a new array with the IV prepended to the encrypted data
                    byte[] result = new byte[iv.Length + encryptedData.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(encryptedData, 0, result, iv.Length, encryptedData.Length);

                    return result; // Return the combined IV + encrypted data
                }
            }
        }

        public override byte[] Decrypt(byte[] input, byte[] key)
        {
            if (input.Length < 16)
            {
                return input;
            }
            // Extract the IV from the first 16 bytes of the input
            byte[] iv = new byte[16];
            Buffer.BlockCopy(input, 0, iv, 0, iv.Length);

            // Extract the actual ciphertext (after the IV)
            byte[] ciphertext = new byte[input.Length - iv.Length];
            Buffer.BlockCopy(input, iv.Length, ciphertext, 0, ciphertext.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv; // Use the IV extracted from the input

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(ciphertext))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (MemoryStream resultStream = new MemoryStream())
                {
                    // Decrypt the data and write it to resultStream
                    cryptoStream.CopyTo(resultStream);
                    return resultStream.ToArray(); // Return the decrypted data
                }
            }
        }

        public override byte[] GenerateKey(string s)
        {
            byte[] keyBytes = UTF8Encoding.UTF8.GetBytes(s);

            if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
                throw new ArgumentException("Key must be 16, 24, or 32 bytes long for AES.");

            return keyBytes;
        }

    }
}
