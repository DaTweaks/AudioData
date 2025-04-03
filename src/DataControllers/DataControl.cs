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
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AudioData.DataControllers
{
    public abstract class DataControl
    {
        /// <summary>
        /// Gets the time it sends each data bit. 
        /// </summary>
        public double GetBitDuration(int BPS) => 1.0 / BPS;

        /// <summary>
        /// Gets how fast the program sends its data. (DO NOT USE FOR VITAL CALCULATIONS) USE GetBitDuration for this!
        /// </summary>
        public abstract int GetBitsPerSecond();

        /// <summary>
        /// Gets the full name which is the name and description with a " - " in the middle.
        /// </summary>
        public string GetFullName() => GetName() + " - " + GetDescription();

        /// <summary>
        /// Gets the name, usually shortened.
        /// </summary>
        public abstract string GetName();

        /// <summary>
        /// Gets the description, where it tells the full name of the modulation method.
        /// </summary>
        public abstract string GetDescription();

        /// <summary>
        /// demodulates the given audio to data.
        /// </summary>
        /// <param name="fileSpace">filespace of the given audio.</param>
        /// <param name="sampleRate">sample rate of the program</param>
        /// <returns>the data that is demodulated.</returns>
        public bool[] DecodeAudioToData(string fileSpace, int sampleRate)
        {
            return DecodeAudioToData(LoadAudioFromFile(fileSpace), sampleRate);
        }

        /// <summary>
        /// demodulates the given audio to data.
        /// </summary>
        /// <param name="audioData">audio to decode</param>
        /// <param name="sampleRate">sample rate of the program</param>
        /// <returns>the data that is demodulated.</returns>
        public bool[] DecodeAudioToData(float[] audioData, int sampleRate)
        {
            int samplesPerBit = (int)(sampleRate * GetBitDuration(GetBitsPerSecond()));

            List<int> validOffsets = new List<int>();

            for (int offset = 0; offset < samplesPerBit; offset++)
            {
                bool[] bitdata = DecodeAudio(audioData, sampleRate, offset);

                if (bitdata.Length != 0)
                {
                    validOffsets.Add(offset);
                }
                else if (validOffsets.Count > 0)
                {
                    // Stop when it loses the signal
                    break;
                }
            }

            // If no valid offset was found, return empty
            if (validOffsets.Count == 0)
            {
                return new bool[0];
            }

            // Compute the mean offset
            int meanOffset = (int)validOffsets.Average();

            // Decode again using the mean offset
            return DecodeAudio(audioData, sampleRate, meanOffset);
        }

        /// <summary>
        /// demodulates the given audio to data.
        /// </summary>
        /// <param name="audioData">audio to decode</param>
        /// <param name="sampleRate">sample rate of the program</param>
        /// <returns>the data that is demodulated.</returns>
        protected abstract bool[] DecodeAudio(float[] audioData, int sampleRate, int offset);

        /// <summary>
        /// Encodes the data into audio using the chosen modulation.
        /// </summary>
        /// <param name="data">the data to be modulated</param>
        /// <param name="sampleRate"></param>
        /// <param name="noise">the noise that will be applied.</param>
        /// <returns>modulated sound</returns>
        public abstract float[] EncodeDataToAudio(bool[] data, int sampleRate, float noise = 0f);

        #region Bit Manipulation

        /// <summary>
        /// Makes the length of the bits a multiple to allow it to be decoded by the hamming encoder and binary to string converter.
        /// </summary>
        /// <param name="boolArray">the array of bits</param>
        /// <param name="multiple">what multiple of bools you want. so with a multiple of 4, when you have 11, it gives you a length of 12.</param>
        /// <returns></returns>
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

        public bool[] ConvertByteArrayToBinary(byte[] bytes)
        {
            bool[] output = new bool[bytes.Length*8];
            for (int i = 0; i < bytes.Length; i++)
            {
                ConvertByteToBoolArray(bytes[i], output, i);
            }

            return output.ToArray();
        }

        public byte[] ConvertBinaryToByteArray(bool[] binary)
        {
            if (binary.Length % 8 != 0)
            {
                binary = MakeLengthMultipleOf(binary, 8);
            }

            byte[] list = new byte[binary.Length/8];

            for (int i = 0; i < list.Length; i ++)
            {
                list[i] = ConvertBinaryToByte(binary, i*8);
            }

            return list;
        }

        private byte ConvertBinaryToByte(bool[] source, int index)
        {
            byte result = 0;

            // Loop through the array
            for (int i = 7 + index; i >= index; i--)
                result = (byte)((result & ~((byte)1 << i)) | ((source[i] ? (byte)1 : (byte)0) << i));
            

            return result;
        }

        private void ConvertByteToBoolArray(byte b, bool[] binary, int index)
        {
            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 7+index; i >= index; i--)
                binary[i] = (b & (1 << i)) != 0;
        }

        public bool[] SerializeToBinary(object obj)
        {
            return ConvertByteArrayToBinary(JsonSerializer.SerializeToUtf8Bytes(obj)); // Serialize to byte array
        }

        public T DeserializeFromBinary<T>(bool[] binary)
        {
            return JsonSerializer.Deserialize<T>(ConvertBinaryToByteArray(binary)); // Deserialize back from byte array
        }

        #endregion

        #region Audio

        /// <summary>
        /// Plays the given audiofile on the main speaker.
        /// </summary>
        /// <param name="fileSpace">the file to be played</param>
        public void PlayAudio(string fileSpace)
        {
            try
            {
                using (var audioFile = new AudioFileReader(fileSpace))
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

        /// <summary>
        /// gets the time it takes to play audio
        /// </summary>
        /// <param name="fileSpace"></param>
        /// <returns></returns>
        public TimeSpan GetWavFileDuration(string fileSpace)
        {
            using (var writer = new WaveFileReader(fileSpace))
            {
                return writer.TotalTime;
            }
        }

        /// <summary>
        /// Padds the sound with silence on the beginning and end to simulate that the samples are not at the exact right time.
        /// </summary>
        /// <param name="original">sound</param>
        /// <param name="paddingAmount">amount of padding.</param>
        /// <returns>padded sound</returns>
        public float[] PadSoundWithSilence(float[] original, int paddingAmount)
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
                // Generate Gaussian noise using Box-Muller transform
                double u1 = 1.0 - rand.NextDouble(); // Uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Random normal(0,1)
                float noise = (float)(randStdNormal * noiseLevel);

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
        public string SaveAudioToFile(float[] audioData, string fileSpace, int sampleRate)
        {
            using (var writer = new WaveFileWriter(fileSpace, new WaveFormat(sampleRate, 1)))
            {
                writer.WriteSamples(audioData, 0, audioData.Length);
            }
            return fileSpace;
        }

        /// <param name="samples">Samples that need to be checked.</param>
        /// <param name="targetFrequency">the frequency in hz that needs to be detected</param>
        /// <param name="sampleRate">The Sample rate of the samples</param>
        /// <returns>The power of the given frequency.</returns>
        public double Goertzel(float[] samples, double targetFrequency, int sampleRate)
        {
            int N = samples.Length;
            double k = (int)(0.5 + N * targetFrequency / sampleRate);
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

        public float[] LoadAudioFromFile(string fileSpace)
        {
            using (var reader = new AudioFileReader(fileSpace))
            {
                var audioData = new float[reader.Length / sizeof(float)];
                reader.Read(audioData, 0, audioData.Length);
                return audioData;
            }
        }

        #endregion

        #region Handshakes

        /// <summary>
        /// Removes the handshake that occurs before the transmission. and all bits that occur before it.
        /// </summary>
        /// <returns>Updated data that starts when the data starts.</returns>
        public bool[] RemoveBeforeHandShake(bool[] input)
        {
            var handshake = GenerateStartHandshake();
            int handshakeLength = GenerateStartHandshakeEncoded().Length;
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
            int handshakeLength = GenerateEndHandshakeEncoded().Length;
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

        public float[] GenerateTone(float[] input, int samplerate, int frequency)
        {
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input cannot be null or empty.");

            float[] output = new float[input.Length];
            double angularFrequency = 2 * Math.PI * frequency / samplerate;

            for (int i = 0; i < input.Length; i++)
            {
                output[i] = input[i] + (float)(Math.Sin(i * angularFrequency));
            }

            return output;
        }


        private bool[] GenerateStartHandshake()
        {
            return StringToBinary("S\u0002");
        }

        public bool[] GenerateStartHandshakeEncoded()
        {
            return MessageEncoder.GroupEncode(GenerateStartHandshake(), 8);
        }

        private bool[] GenerateEndHandshake()
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
