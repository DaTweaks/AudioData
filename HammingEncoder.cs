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

using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AudioData
{
    internal class HammingEncoder
    {
        /// <returns>Array of Encoded Hamming code bits</returns>
        /// <param name="byteSize">The prededermined size of the bits. Sizes of 4 and 8 Are supported.</param>
        public static bool[] GroupEncode(bool[] code, int byteSize)
        {
            if (byteSize != 4 && byteSize != 8)
                throw new Exception("byteSize must be 8 or 4.");

            var length = Helpers.GetLength(byteSize);

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
        public static bool[] GroupDecode(bool[] code, int byteSize)
        {
            if (byteSize != 4 && byteSize != 8)
                throw new Exception("byteSize must be 8 or 4.");

            int length = Helpers.GetLength(byteSize);

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

        public static bool[] SingleEncode(bool[] code)
        {
            var parityPositions =  Helpers.GetParityPositions(code);

            int length = code.Length + parityPositions.Length;

            var encoded = new bool[length];

            // Insert data into the hamming code. 
            for (int i = 0, j = 0; i < length; i++)
            {;
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
                encoded[parity] = Helpers.doXoringForPosition(encoded, length, parity);

            return encoded;
        }

        public static bool[] SingleDecode(bool[] encoded)
        {
            int faultyBitPosition = ErrorSyndrome(encoded);
            if (faultyBitPosition != -1 && faultyBitPosition < encoded.Length)
            {
                encoded[faultyBitPosition] = !encoded[faultyBitPosition];
            }

            var decoded = new List<bool>();

            for (int i = 0, j = 0; i < encoded.Length; i++)
            {
                if (Helpers.PowerOf2(i + 1))
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
        public static int ErrorSyndrome(bool[] encoded)
        {
            int syndrome = 0;
            for (int i = 0, j = 0; encoded.Length > i; i++)
            {
                if (Helpers.PowerOf2(i+1))
                {
                    syndrome += (Convert.ToInt32(Helpers.doXoringForPosition(encoded, encoded.Length, i) ^ encoded[i]) << j);
                    j++;
                }
            }
            return syndrome - 1;
        }

        /// <summary>
        /// If you want to determine the position of the bit use the ErrorSyndrome Function.
        /// </summary>
        /// <param name="encoded">Encoded bits./param>
        /// <returns>If it has a flipped bit.</returns>
        public static bool HasError(bool[] encoded)
        {
            return ErrorSyndrome(encoded) == -1;
        }

        /// <param name="encoded">the array to be flipped.</param>
        /// <param name="amount">amount of bits that will be flipped.</param>
        public static void MixinRandomError(bool[] encoded, int amount)
        {
            var rng = new Random();
            for (int i = 0; i < amount; i++)
            {
                var randomPosition = rng.Next(encoded.Length);

                encoded[randomPosition] = !encoded[randomPosition];
            }
        }
    }

    public class Helpers
    {
        public static int GetLength(int byteSize)
        {
            int parityAmount = 0;
            for (int i = 0; byteSize > i; i++)
                if (Helpers.PowerOf2(i + 1)) // Calculate all the positions for parity.
                    parityAmount++;
            return parityAmount+byteSize;
        }

        public static int[] GetParityPositions(bool[] code)
        {
            var parityPositions = new List<int>();
            for (int i = 0; code.Length > i; i++)
            {
                if (Helpers.PowerOf2(i + 1)) // Calculate all the positions for parity.
                {
                    parityPositions.Add(i);
                }
            }
            return parityPositions.ToArray();
        }

        public static System.String boolArrayToPrettyString(bool[] arr)
        {
            return System.String.Join("", arr.Select(x => Convert.ToInt32(x)));
        }

        public static bool[] prettyStringToBoolArray(System.String s)
        {
            return s.ToArray().Select(x => ((Convert.ToInt32(x) - 48) > 0)).ToArray();
        }

        public static bool PowerOf2(int x)
        {
            return (x > 0) && ((x & (x - 1)) == 0);
        }

        public static int[] getPositionsForXoring(int length, int currentHammingPosition)
        {
            var positions = new List<int>();
            for (int i = 1; i <= length; i++)
            {
                if ((i & (currentHammingPosition+1)) > 0 && !PowerOf2(i))
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
