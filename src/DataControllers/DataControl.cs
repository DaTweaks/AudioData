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

        public abstract bool[] DecodeAudioToData(string fileName, int SampleRate);

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

        public float[] LoadAudioFromFile(string fileName)
        {
            using (var reader = new AudioFileReader(fileName))
            {
                var audioData = new float[reader.Length / sizeof(float)];
                reader.Read(audioData, 0, audioData.Length);
                return audioData;
            }
        }

        #endregion
    }
}
