using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.Encoders.Crypto
{
    public class NoCrypto : Crypto
    {
        public override string CryptoName => "Ingen Kryptering";

        public override byte[] Decrypt(byte[] input, byte[] key)
        {
            return input;
        }

        public override byte[] Encrypt(byte[] input, byte[] key)
        {
            return input;
        }

        public override byte[] GenerateKey(string s)
        {
            return new byte[0];
        }
    }
}
