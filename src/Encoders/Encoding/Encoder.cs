using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.Encoders.Encoding
{
    public abstract class Encoder
    {
        public abstract string EncoderName { get; }
        // Encoding

        public bool[] Encode(string s) => Encode(StringToBinary(s));

        public bool[] Encode(byte[] data) => Encode(BytesToBools(data));

        public abstract bool[] Encode(bool[] binary);


        // Decoding

        public string DecodeString(bool[] binary) => BinaryToString(Decode(binary));

        public byte[] DecodeBytes(bool[] data) => BoolsToBytes(Decode(data));

        public abstract bool[] Decode(bool[] binary);

        #region Utils

        public static string boolArrayToPrettyString(bool[] arr)
        {
            return string.Join("", arr.Select(x => Convert.ToInt32(x)));
        }

        public static bool[] prettyStringToBoolArray(string s)
        {
            return s.ToArray().Select(x => Convert.ToInt32(x) - 48 > 0).ToArray();
        }

        public static bool[] BytesToBools(byte[] bytes)
        {
            if (bytes == null)
                return new bool[0];
            bool[] bits = new bool[bytes.Length * 8];
            for (int i = 0; i < bytes.Length; i++)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    bits[i * 8 + bit] = (bytes[i] & 1 << 7 - bit) != 0;
                }
            }
            return bits;
        }

        public static byte[] BoolsToBytes(bool[] bits)
        {
            int byteCount = (bits.Length + 7) / 8;
            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    bytes[i / 8] |= (byte)(1 << 7 - i % 8);
                }
            }
            return bytes;
        }

        public static long ComputeShortHash(string text)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);

                // Truncate to 8 bytes for a shorter hash (64 bits)
                return BitConverter.ToInt64(hashBytes, 0);
            }
        }

        /// <param name="text">data to be converted.</param>
        /// <param name="text">data to be converted.</param>
        /// <returns>Array of 8 bit pairs.</returns>
        public static bool[] StringToBinary(string text)
        {
            return BytesToBools(System.Text.Encoding.UTF8.GetBytes(text));
        }

        /// <param name="binary">Array of 8 bit pairs.</param>
        public static string BinaryToString(bool[] binary)
        {
            StringBuilder sb = new StringBuilder();

            if (binary.Length % 8 != 0)
            {
                binary = MakeLengthMultipleOf(binary, 8);
            }

            for (int i = 0; i < binary.Length; i += 8)
            {
                // Create a string representing 8 bits
                StringBuilder byteStringBuilder = new StringBuilder();
                for (int j = 0; j < 8; j++)
                {
                    byteStringBuilder.Append(binary[i + j] ? '1' : '0');
                }

                string byteString = byteStringBuilder.ToString();
                sb.Append((char)Convert.ToByte(byteString, 2));
            }

            return sb.ToString();
        }

        public static bool[] MakeLengthMultipleOf(bool[] binary, int multiple)
        {
            int originalLength = binary.Length;
            int newLength = originalLength;

            // Calculate the new length that is a multiple of 8
            while (newLength % multiple != 0)
            {
                newLength--;
            }

            // Create a new array with the adjusted length
            bool[] adjustedArray = new bool[newLength];

            // Copy elements from the original array to the adjusted array
            Array.Copy(binary, adjustedArray, newLength);

            return adjustedArray;
        }

        #endregion

        #region BitFlipping
        /// <param name="binary">the array to be flipped.</param>
        /// <param name="amount">amount of bits that will be flipped.</param>
        public static void MixinRandomError(bool[] binary, int amount)
        {
            var rng = new Random();
            for (int i = 0; i < amount; i++)
            {
                var randomPosition = rng.Next(binary.Length);

                binary[randomPosition] = !binary[randomPosition];
            }
        }

        public static bool[] FlipRandomBits(bool[] binary, double flipChance = 0.01, int? seed = null)
        {
            if (flipChance < 0 || flipChance > 1)
                throw new ArgumentOutOfRangeException(nameof(flipChance), "Must be between 0.0 and 1.0");

            Random rand = seed.HasValue ? new Random(seed.Value) : new Random();
            bool[] output = new bool[binary.Length];

            for (int i = 0; i < binary.Length; i++)
            {
                bool shouldFlip = rand.NextDouble() < flipChance;
                output[i] = shouldFlip ? !binary[i] : binary[i];
            }

            return output;
        }

        // Obliterate a shard: randomly choose an 8-bit section and set it to 0s (obliterate it)
        public static bool[] ObliterateShard(bool[] binary, int? seed = null)
        {
            Random rand = seed.HasValue ? new Random(seed.Value) : new Random();

            // Randomly choose a position in the bool array to obliterate
            int bitToObliterate = rand.Next(0, binary.Length / 8) * 8; // Find any byte boundary

            for (int i = bitToObliterate; i < bitToObliterate + 8 && i < binary.Length; i++)
            {
                binary[i] = false; // Set the 8 bits to 0 (obliterate)
            }

            return binary;
        }

        // Flip exactly one random bit in the bool array
        public static bool[] FlipOneBit(bool[] binary, int? seed = null)
        {
            Random rand = seed.HasValue ? new Random(seed.Value) : new Random();

            // Pick a random bit index to flip
            int bitToFlip = rand.Next(0, binary.Length);

            // Flip that one bit
            binary[bitToFlip] = !binary[bitToFlip];

            return binary;
        }

        #endregion
    }
}
