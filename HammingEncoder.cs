//	Copyright (c) 2024 David Hornemark 
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AudioData
{
    internal class HammingEncoder
    {
        public const bool t = true;
        public const bool f = false;
        public const int startWith = 0;
        public static int length = 12;

        /// <param name="code">Array of 8 bit pairsArray of Hamming code bits.</param>
        /// <returns>Array of Encoded Hamming code bits in sizes of 12.</returns>
        public static bool[] Encode(bool[] code)
        {
            List<bool[]> result = new List<bool[]>();

            for (int i = 0; i < code.Length; i += 8)
            {
                bool[] chunk = new bool[Math.Min(8, code.Length - i)];
                Array.Copy(code, i, chunk, 0, chunk.Length);
                result.Add(HEncode(chunk));
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

        /// <param name="code">Array of Encoded Hamming code bits in sizes of 12.</param>
        /// <returns>Array of 8 bit pairs</returns>
        public static bool[] Decode(bool[] code)
        {
            List<bool[]> result = new List<bool[]>();

            for (int i = 0; i < code.Length; i += length)
            {
                bool[] chunk = new bool[Math.Min(length, code.Length - i)];
                Array.Copy(code, i, chunk, 0, chunk.Length);
                result.Add(HDecode(chunk));
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

        /// <param name="code">8 bits inside of an Array (MUST BE 8 BITS)</param>
        /// <returns>Encoded bits of sizes 12</returns>
        private static bool[] HEncode(bool[] code)
        {
            var encoded = new bool[length];
            int[] parityPositions = { 0, 1, 3, 7 }; // 1-based positions for parity bits

            int i = 0, j = 0;
            for (int k = 0; k < length; k++)
            {
                if (Array.Exists(parityPositions, pos => pos == k))
                {
                    // Parity bit positions will be calculated later
                    continue;
                }
                encoded[k] = code[j];
                j++;
            }

            // Calculate parity bits
            encoded[0] = Helpers.doXoringForPosition(encoded, length, 1);
            encoded[1] = Helpers.doXoringForPosition(encoded, length, 2);
            encoded[3] = Helpers.doXoringForPosition(encoded, length, 4);
            encoded[7] = Helpers.doXoringForPosition(encoded, length, 8);

            return encoded;
        }

        /// <param name="code">Encoded bits of sizes 12</param>
        /// <returns>8 bits inside of an Array</returns>
        private static bool[] HDecode(bool[] encoded)
        {
            int faultyBitPosition = ErrorSyndrome(encoded);
            if (faultyBitPosition != -1 && faultyBitPosition < encoded.Length)
            {
                encoded[faultyBitPosition] = !encoded[faultyBitPosition];
            }

            var decoded = new bool[8]; // 8 data bits

            int i = 0, j = 0;
            int[] parityPositions = { 0, 1, 3, 7 };

            for (int k = 0; k < length; k++)
            {
                if (Array.Exists(parityPositions, pos => pos == k))
                {
                    continue;
                }
                decoded[j] = encoded[k];
                j++;
            }

            return decoded;
        }

        /// <param name="encoded">Encoded bits of sizes 12</param>
        /// <returns>The position in the array where the faulty bit is. -1 is that there is none.</returns>
        public static int ErrorSyndrome(bool[] encoded)
        {
            int syndrome =
                (Convert.ToInt32(Helpers.doXoringForPosition(encoded, length, 1) ^ encoded[0])) +
                (Convert.ToInt32(Helpers.doXoringForPosition(encoded, length, 2) ^ encoded[1]) << 1) +
                (Convert.ToInt32(Helpers.doXoringForPosition(encoded, length, 4) ^ encoded[3]) << 2) +
                (Convert.ToInt32(Helpers.doXoringForPosition(encoded, length, 8) ^ encoded[7]) << 3);

            return syndrome - 1;
        }

        /// <summary>
        /// If you want to determine the position of the bit use the ErrorSyndrome Function.
        /// </summary>
        /// <param name="encoded">Encoded bits of sizes 12</param>
        /// <returns>If it has a flipped bit.</returns>
        public static bool HasError(bool[] encoded)
        {
            return ErrorSyndrome(encoded) == -1;
        }

        /// <param name="encoded">the array to be flipped.</param>
        /// <param name="amount">amount of bits that will be flipped.</param>
        /// <returns>the same array </returns>
        public static bool[] MixinRandomError(bool[] encoded, int amount)
        {
            var rng = new Random();
            for (int i = 0; i < amount; i++)
            {
                var randomPosition = rng.Next(encoded.Length);

                encoded[randomPosition] = !encoded[randomPosition];
            }
            return encoded;
        }
    }

    public class Helpers
    {
        public static System.String boolArrayToPrettyString(bool[] arr)
        {
            return System.String.Join("", arr.Select(x => Convert.ToInt32(x)));
        }

        public static bool[] prettyStringToBoolArray(System.String s)
        {
            return s.ToArray().Select(x => ((Convert.ToInt32(x) - 48) > 0)).ToArray();
        }

        public static bool notPowerOf2(int x)
        {
            return !(x == 1 || x == 2 || x == 4 || x == 8);
        }

        public static int[] getPositionsForXoring(int length, int currentHammingPosition)
        {
            var positions = new List<int>();
            for (int i = 1; i <= length; i++)
            {
                if ((i & currentHammingPosition) > 0 && notPowerOf2(i))
                    positions.Add(i);

            }
            return positions.ToArray();
        }

        public static bool doXoringForPosition(bool[] vector, int length, int currentHammingPosition)
        {
            return getPositionsForXoring(length, currentHammingPosition)
                .Select(x => vector[x - 1])
                .Aggregate((x, y) => x ^ y);
        }
    }
}
