using AudioData.Encoders.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Encoder = AudioData.Encoders.Encoding.Encoder;

namespace AudioData.Encoders.Crypto
{
    public abstract class Crypto
    {
        // TODO: SAVE KEYS IN HERE, AND MAKE THE USER SELECT ONE THEY WANT TO USE.

        public abstract string CryptoName { get; }

        // Encryption

        public byte[] Encrypt(string s, byte[] key) => Encrypt(UTF8Encoding.UTF8.GetBytes(s), key);

        public bool[] EncryptBinary(string s, byte[] key) => Encoder.BytesToBools(Encrypt(UTF8Encoding.UTF8.GetBytes(s), key));

        public byte[] Encrypt(bool[] data, byte[] key) => Encrypt(Encoder.BoolsToBytes(data), key);

        public abstract byte[] Encrypt(byte[] input, byte[] key);


        // Decryption

        public string DecryptString(byte[] input, byte[] key) => UTF8Encoding.UTF8.GetString(Decrypt(input, key));

        public bool[] DecryptBinary(bool[] data, byte[] key) => Encoder.BytesToBools(Encrypt(Encoder.BoolsToBytes(data), key));

        public abstract byte[] Decrypt(byte[] input, byte[] key);

        // KeyGeneration

        public abstract byte[] GenerateKey(string s);
    }
}
