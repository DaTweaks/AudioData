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
        public int GetSampleRate() => 44100;
        public double GetBitDuration(int BPS) => 1.0 / BPS;

        /// <param name="text">data to be converted.</param>
        /// <returns>Array of 8 bit pairs.</returns>
        public abstract bool[] StringToBinary(string text);

        /// <param name="binary">Array of 8 bit pairs.</param>
        public abstract string BinaryToString(bool[] binary);

        public abstract bool[] DecodeAudioToData(string fileName);

        public abstract bool[] DecodeAudioToData(float[] audioData);

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

        public abstract float[] EncodeDataToAudio(bool[] data, float noise);

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
        public string SaveAudioToFile(float[] audioData, string fileName)
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
