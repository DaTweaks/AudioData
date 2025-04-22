using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.Encoders.Encoding
{
    public class HammingEncoder : Encoder
    {
        private int byteSize;

        public override string EncoderName => "Hamming Encoder";

        public HammingEncoder(int byteSize = 8)
        {
            this.byteSize = byteSize;

            if (byteSize != 8 && byteSize != 4)
                throw new Exception("byteSize must be 8 or 4.");
        }

        public override bool[] Decode(bool[] data) => GroupDecode(data, byteSize);

        public override bool[] Encode(bool[] data) => GroupEncode(data, byteSize);

        /// <returns>Array of Encoded Hamming code bits</returns>
        /// <param name="byteSize">The prededermined size of the bits. Sizes of 4 and 8 Are supported.</param>
        private bool[] GroupEncode(bool[] code, int byteSize)
        {
            if (byteSize != 4 && byteSize != 8)
                throw new Exception("byteSize must be 8 or 4.");

            var length = GetLength(byteSize);

            List<bool[]> result = new List<bool[]>();

            for (int i = 0; i < code.Length; i += 8)
            {
                bool[] chunk = new bool[Math.Min(8, code.Length - i)];
                Array.Copy(code, i, chunk, 0, chunk.Length);
                result.Add(SingleEncode(chunk));
            }

            // Calculate the total length of the combined array
            int totalLength = 0;
            foreach (var chunk in result)
            {
                totalLength += chunk.Length;
            }

            // Create the combined array
            bool[] combinedArray = new bool[totalLength];

            // Copy each chunk into the combined array
            int currentIndex = 0;
            foreach (var chunk in result)
            {
                Array.Copy(chunk, 0, combinedArray, currentIndex, chunk.Length);
                currentIndex += chunk.Length;
            }

            return combinedArray;
        }

        /// <param name="code">Array of Encoded Hamming code bits.</param>
        /// <param name="byteSize">The prededermined size of the bits. (AKA the size of what you expect to come out)</param>
        /// <returns>Array of pairs in bytesize</returns>
        private bool[] GroupDecode(bool[] code, int byteSize)
        {
            if (byteSize != 4 && byteSize != 8)
                throw new Exception("byteSize must be 8 or 4.");

            int length = GetLength(byteSize);

            List<bool[]> result = new List<bool[]>();

            for (int i = 0; i < code.Length; i += length)
            {
                bool[] chunk = new bool[Math.Min(length, code.Length - i)];
                Array.Copy(code, i, chunk, 0, chunk.Length);
                result.Add(SingleDecode(chunk));
            }

            // Calculate the total length of the combined array
            int totalLength = 0;
            foreach (var chunk in result)
            {
                totalLength += chunk.Length;
            }

            // Create the combined array
            bool[] combinedArray = new bool[totalLength];

            // Copy each chunk into the combined array
            int currentIndex = 0;
            foreach (var chunk in result)
            {
                Array.Copy(chunk, 0, combinedArray, currentIndex, chunk.Length);
                currentIndex += chunk.Length;
            }

            return combinedArray;
        }

        private bool[] SingleEncode(bool[] code)
        {
            var parityPositions = GetParityPositions(code);

            int length = code.Length + parityPositions.Length;

            var encoded = new bool[length];

            // Insert data into the hamming code. 
            for (int i = 0, j = 0; i < length; i++)
            {
                ;
                if (parityPositions.Contains(i))
                {
                    // Parity bits added later.
                    continue;
                }
                encoded[i] = code[j];
                j++;
            }

            // Calculate parity bits
            foreach (var parity in parityPositions)
                encoded[parity] = doXoringForPosition(encoded, length, parity);

            return encoded;
        }

        private bool[] SingleDecode(bool[] encoded)
        {
            int faultyBitPosition = ErrorSyndrome(encoded);
            if (faultyBitPosition != -1 && faultyBitPosition < encoded.Length)
            {
                encoded[faultyBitPosition] = !encoded[faultyBitPosition];
            }

            var decoded = new List<bool>();

            for (int i = 0, j = 0; i < encoded.Length; i++)
            {
                if (PowerOf2(i + 1))
                {
                    continue;
                }
                decoded.Add(encoded[i]);
                j++;
            }

            return decoded.ToArray();
        }

        /// <param name="encoded">Encoded bits.</param>
        /// <returns>The position in the array where the faulty bit is. -1 is that there is none.</returns>
        private int ErrorSyndrome(bool[] encoded)
        {
            int syndrome = 0;
            for (int i = 0, j = 0; encoded.Length > i; i++)
            {
                if (PowerOf2(i + 1))
                {
                    syndrome += Convert.ToInt32(doXoringForPosition(encoded, encoded.Length, i) ^ encoded[i]) << j;
                    j++;
                }
            }
            return syndrome - 1;
        }

        private int GetLength(int byteSize)
        {
            int parityAmount = 0;
            for (int i = 0; byteSize > i; i++)
                if (PowerOf2(i + 1)) // Calculate all the positions for parity.
                    parityAmount++;
            return parityAmount + byteSize;
        }

        private int[] GetParityPositions(bool[] code)
        {
            var parityPositions = new List<int>();
            for (int i = 0; code.Length > i; i++)
            {
                if (PowerOf2(i + 1)) // Calculate all the positions for parity.
                {
                    parityPositions.Add(i);
                }
            }
            return parityPositions.ToArray();
        }

        private bool PowerOf2(int x)
        {
            return x > 0 && (x & x - 1) == 0;
        }

        private bool doXoringForPosition(bool[] vector, int length, int currentHammingPosition)
        {
            try
            {
                var positions = getPositionsForXoring(length, currentHammingPosition);
                if (positions.Length == 0)
                {
                    // Return a default value (e.g., `false`) when no positions are found
                    return false;
                }

                return positions
                    .Select(x => vector[x - 1])
                    .Aggregate((x, y) => x ^ y);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private int[] getPositionsForXoring(int length, int currentHammingPosition)
        {
            var positions = new List<int>();
            for (int i = 1; i <= length; i++)
            {
                if ((i & currentHammingPosition + 1) > 0 && !PowerOf2(i))
                    positions.Add(i);
            }
            return positions.ToArray();
        }
    }
}
