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

using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.DataControllers
{
    public abstract class DataControl
    {
        public double GetBitDuration(int BPS) => 1.0 / BPS;

        /// <param name="text">data to be converted.</param>
        /// <param name="text">data to be converted.</param>
        /// <returns>Array of 8 bit pairs.</returns>
        public bool[] StringToBinary(string text)
        {
            // Initialize a list to store the boolean values
            List<bool> boolList = new List<bool>();

            // Loop through each character in the input string
            foreach (char c in text.ToCharArray())
            {
                // Convert the character to a binary string, padded to 8 bits
                string binaryString = Convert.ToString(c, 2).PadLeft(8, '0');

                // Loop through each character in the binary string
                foreach (char bit in binaryString)
                {
                    // Add the boolean value (true for '1', false for '0') to the list
                    boolList.Add(bit == '1');
                }
            }

            // Convert the list to an array and return it
            return boolList.ToArray();
        }

        /// <param name="binary">Array of 8 bit pairs.</param>
        public string BinaryToString(bool[] binary)
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

        public abstract string GetName();

        public bool[] DecodeAudioToData(string fileName, int SampleRate)
        {
            return DecodeAudioToData(LoadAudioFromFile(fileName), SampleRate);
        }

        public abstract bool[] DecodeAudioToData(float[] audioData, int SampleRate);

        public bool[] MakeLengthMultipleOf(bool[] boolArray, int multiple)
        {
            int originalLength = boolArray.Length;
            int newLength = originalLength;

            // Calculate the new length that is a multiple of 8
            while (newLength % multiple != 0)
            {
                newLength--;
            }

            // Create a new array with the adjusted length
            bool[] adjustedArray = new bool[newLength];

            // Copy elements from the original array to the adjusted array
            Array.Copy(boolArray, adjustedArray, newLength);

            return adjustedArray;
        }

        #region Audio

        public void PlayAudio(string fileName)
        {
            try
            {
                using (var audioFile = new AudioFileReader(fileName))
                {
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.DeviceNumber = 0;
                        outputDevice.Volume = 0.2f;
                        outputDevice.Play();
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public TimeSpan GetWavFileDuration(string fileName)
        {
            using (var writer = new WaveFileReader(fileName))
            {
                return writer.TotalTime;
            }
        }

        public abstract float[] EncodeDataToAudio(bool[] data, float noise, int SampleRate);

        public float[] PadArrayWithZeros(float[] original, int paddingAmount)
        {
            int newSize = original.Length + 2 * paddingAmount;
            float[] paddedArray = new float[newSize];

            Array.Copy(original, 0, paddedArray, paddingAmount, original.Length);

            return paddedArray;
        }

        public float[] AddNoise(float[] soundArray, float noiseLevel)
        {
            Random rand = new Random();

            for (int i = 0; i < soundArray.Length; i++)
            {
                // Generate noise in the range of -noiseLevel to +noiseLevel
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * noiseLevel;
                soundArray[i] += noise;

                // Ensure that the values stay within the valid range
                if (soundArray[i] > 1.0f)
                    soundArray[i] = 1.0f;
                else if (soundArray[i] < -1.0f)
                    soundArray[i] = -1.0f;
            }
            return soundArray;
        }


        /// <returns>the file name.</returns>
        public string SaveAudioToFile(float[] audioData, string fileName, int SampleRate)
        {
            using (var writer = new WaveFileWriter(fileName, new WaveFormat(SampleRate, 1)))
            {
                writer.WriteSamples(audioData, 0, audioData.Length);
            }
            return fileName;
        }

        public double Goertzel(float[] samples, double targetFrequency, int SampleRate)
        {
            int N = samples.Length;
            double k = (int)(0.5 + N * targetFrequency / SampleRate);
            double omega = 2.0 * Math.PI * k / N;
            double sine = Math.Sin(omega);
            double cosine = Math.Cos(omega);
            double coeff = 2.0 * cosine;
            double q0 = 0, q1 = 0, q2 = 0;

            for (int i = 0; i < N; i++)
            {
                q0 = coeff * q1 - q2 + samples[i];
                q2 = q1;
                q1 = q0;
            }

            return Math.Sqrt(q1 * q1 + q2 * q2 - q1 * q2 * coeff);
        }

        public float[] LoadAudioFromFile(string fileName)
        {
            using (var reader = new AudioFileReader(fileName))
            {
                var audioData = new float[reader.Length / sizeof(float)];
                reader.Read(audioData, 0, audioData.Length);
                return audioData;
            }
        }

        /// <summary>
        /// Removes the handshake that occurs before the transmission. and all bits that occur before it.
        /// </summary>
        /// <returns>Updated data that starts when the data starts.</returns>
        public bool[] RemoveBeforeHandShake(bool[] input)
        {
            var handshake = GenerateStartHandshake();
            var EncodedHandshake = GenerateStartHandshakeEncoded();
            int handshakeLength = EncodedHandshake.Length;
            int inputLength = input.Length;

            // Iterate through the input array to find the handshake pattern, starting from the end
            for (int i = 0; i <= inputLength - handshakeLength; i++)
            {
                var tempArray = new bool[handshakeLength];
                Array.Copy(input, i, tempArray, 0, handshakeLength);

                bool[] output = new bool[0];

                try
                {
                    output = MessageEncoder.GroupDecode(tempArray, 8);
                }
                catch
                {
                    continue;
                }

                if (output.Length == 0)
                {
                    continue;
                }

                bool isMatch = true;
                for (int j = 0; j < output.Length; j++)
                {
                    if (output[j] != handshake[j])
                    {
                        isMatch = false;
                        break;
                    }
                }

                // If the handshake pattern is found, return the array up to the start of the handshake
                if (isMatch)
                {
                    int newLength = inputLength - (i + handshakeLength);
                    bool[] result = new bool[newLength];
                    Array.Copy(input, i + handshakeLength, result, 0, newLength);
                    return result;
                }
            }
            // If no handshake pattern is found, return a empty array.
            return new bool[0];
        }

        /// <summary>
        /// Removes the handshake that occurs at the end of the transmission and all bits that occur after it.
        /// </summary>
        /// <returns>All the data that was sent before the handshake</returns>
        public bool[] RemoveAfterHandShake(bool[] input)
        {
            var handshake = GenerateEndHandshake();
            var EncodedHandshake = GenerateEndHandshakeEncoded();
            int handshakeLength = EncodedHandshake.Length;
            int inputLength = input.Length;

            // Iterate through the input array to find the handshake pattern, starting from the end
            for (int i = inputLength - handshakeLength; i >= 0; i--)
            {
                var tempArray = new bool[handshakeLength];
                Array.Copy(input, i, tempArray, 0, handshakeLength);

                bool[] output = new bool[0];

                try
                {
                    output = MessageEncoder.GroupDecode(tempArray, 8);
                }
                catch
                {
                    continue;
                }

                if (output.Length == 0)
                {
                    continue;
                }

                bool isMatch = true;
                for (int j = 0; j < output.Length; j++)
                {
                    if (output[j] != handshake[j])
                    {
                        isMatch = false;
                        break;
                    }
                }

                // If the handshake pattern is found, return the array up to the start of the handshake
                if (isMatch)
                {
                    bool[] result = new bool[i];
                    Array.Copy(input, result, i);
                    return result;
                }
            }
            // If no handshake pattern is found, return a empty array.
            return new bool[0];
        }

        public bool[] GenerateStartHandshake()
        {
            return StringToBinary("S\u0002");
        }

        public bool[] GenerateStartHandshakeEncoded()
        {
            return MessageEncoder.GroupEncode(GenerateStartHandshake(), 8);
        }

        public bool[] GenerateEndHandshake()
        {
            return StringToBinary("\u0003E");
        }

        public bool[] GenerateEndHandshakeEncoded()
        {
            return MessageEncoder.GroupEncode(GenerateEndHandshake(), 8);
        }

        #endregion
    }
}
