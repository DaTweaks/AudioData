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
using NAudio.Wave.SampleProviders;
using Spectrogram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.DataControllers.SimpleController
{
    public abstract class SimpleDataController: IDataControl
    {
        public SimpleDataController()
        {
        }

        public double GetBitDuration(int BPS) => 1.0 / BPS;

        public abstract int GetBitsPerSecond();

        public abstract int GetOptimalDetectionTone();

        public string GetFullName() => GetName() + " - " + GetDescription();

        public abstract string GetName();
        public abstract string GetDescription();

        public bool[] DecodeAudioToData(string fileName, int SampleRate)
        {
            return DecodeAudioToData(LoadAudioFromFile(fileName), SampleRate);
        }

        /// <summary>
        /// demodulates the given audio to data.
        /// </summary>
        /// <param name="audioData">audio to decode</param>
        /// <param name="sampleRate">sample rate of the program</param>
        /// <returns>the data that is demodulated.</returns>
        public bool[] DecodeAudioToData(float[] audioData, int sampleRate)
        {
            return DecodeAudio(audioData, sampleRate);
        }

        /// <summary>
        /// demodulates the given audio to data.
        /// </summary>
        /// <param name="audioData">audio to decode</param>
        /// <param name="sampleRate">sample rate of the program</param>
        /// <returns>the data that is demodulated.</returns>
        protected abstract bool[] DecodeAudio(float[] audioData, int sampleRate);


        #region Audio

        public static async Task PlayAudio(string fileName, Guid speakerGuid)
        {
            // Use DirectSoundOut for playback
            var audioFile = new AudioFileReader(fileName);
            var waveChannel = new WaveChannel32(audioFile) { Volume = 1f }; // Initial volume setting, NEEDS TO BE ADJUSTED ACCORDING TO DEVICE. QUESTION IS WHICH???

            // Output device setup
            var outputDevice = new DirectSoundOut(speakerGuid);

            outputDevice.Init(waveChannel);
            outputDevice.Play();

            // Calculate the total playback duration
            int playbackDuration = (int)audioFile.TotalTime.TotalMilliseconds;
            int elapsedTime = 0;
            int delay = 500;

            // Event handler for cleanup once playback stops
            outputDevice.PlaybackStopped += (s, e) =>
            {
                // Cleanup resources once playback stops
                outputDevice.Dispose();
                waveChannel.Dispose();
                audioFile.Dispose();
            };

            // Wait for playback to finish or until the duration has passed
            while (outputDevice.PlaybackState == PlaybackState.Playing && elapsedTime < playbackDuration)
            {
                await Task.Delay(delay);
                elapsedTime += delay;
            }

            // Stop playback explicitly, triggering the PlaybackStopped event
            outputDevice.Stop();
        }

        public static async Task PlayAudio(float[] bitSound, float[] toneSound, int sampleRate, Guid speakerGuid)
        {
            // Create in-memory audio providers from the float arrays
            var bitProvider = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)) { DiscardOnBufferOverflow = true };
            var toneProvider = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)) { DiscardOnBufferOverflow = true };

            // Convert float[] to byte[] and add samples to the providers
            bitProvider.AddSamples(ConvertFloatArrayToByteArray(bitSound), 0, bitSound.Length * sizeof(float));
            toneProvider.AddSamples(ConvertFloatArrayToByteArray(toneSound), 0, toneSound.Length * sizeof(float));

            // Mix the two audio providers
            var mixer = new MixingSampleProvider(new[] { bitProvider.ToSampleProvider(), toneProvider.ToSampleProvider() });

            // Output device setup using DirectSoundOut
            var outputDevice = new DirectSoundOut(speakerGuid);
            outputDevice.Init(mixer);

            // Event handler to signal completion when playback stops
            outputDevice.PlaybackStopped += (s, e) =>
            {
                // Cleanup and signal task completion
                outputDevice.Dispose();  // Clean up the resources
            };

            // Calculate the playback duration in milliseconds (from total samples and sample rate)
            int totalSamples = bitSound.Length;
            double playbackDurationMs = (totalSamples / (double)sampleRate) * 1000;

            // Start playback
            outputDevice.Play();

            // Wait for the calculated playback time (no extra delay)
            await Task.Delay((int)playbackDurationMs);

        }





        // Helper method to convert float[] to byte[]
        private static byte[] ConvertFloatArrayToByteArray(float[] floatArray)
        {
            byte[] byteArray = new byte[floatArray.Length * sizeof(float)];
            Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        public static TimeSpan GetWavFileDuration(string fileName)
        {
            using (var writer = new WaveFileReader(fileName))
            {
                return writer.TotalTime;
            }
        }

        public abstract float[] EncodeDataToAudio(bool[] data, int SampleRate, float noise = 0f);

        public static float[] PadArrayWithZeros(float[] original, int paddingAmount)
        {
            int newSize = original.Length + 2 * paddingAmount;
            float[] paddedArray = new float[newSize];

            Array.Copy(original, 0, paddedArray, paddingAmount, original.Length);

            return paddedArray;
        }

        public static float[] AddNoise(float[] soundArray, float noiseLevel)
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
        } // TODO: CHANGE INTO ADDITIVE GUASSIAN NOISE!


        /// <returns>the file name.</returns>
        public static async Task<string> SaveAudioToFileAsync(float[] audioData, string fileName, int SampleRate)
        {
            var filepath = SaveAudioToFile(audioData, fileName, SampleRate);

            GenerateSpectrogram(filepath, SampleRate, fileName.Replace(".wav", ".png"));

            return filepath;
        }

        public static async void GenerateSpectrogram(string audioFile, int SampleRate, string name)
        {
            (double[] audio, int sampleRate) = ReadMono(audioFile);
            var sg = new SpectrogramGenerator(sampleRate, fftSize: 4096, stepSize: 250, maxFreq: 10000);
            sg.Add(audio);
            sg.SaveImage(name);
        }

        private static (double[] audio, int sampleRate) ReadMono(string filePath, double multiplier = 192_000)
        {
            using var afr = new AudioFileReader(filePath);
            int sampleRate = afr.WaveFormat.SampleRate;
            int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
            int sampleCount = (int)(afr.Length / bytesPerSample);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;

            // Collect the audio samples
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Scale the samples with the multiplier (making the sound louder)
                audio.AddRange(buffer.Take(samplesRead).Select(x => x * multiplier));
            }

            return (audio.ToArray(), sampleRate);
        }



        public static string SaveAudioToFile(float[] audioData, string fileName, int SampleRate)
        {
            using (var writer = new WaveFileWriter(fileName, new WaveFormat(SampleRate, 1)))
            {
                writer.WriteSamples(audioData, 0, audioData.Length);
            }
            return fileName;
        }

        public float[] PadSoundWithSilence(float[] original, int paddingAmount)
        {
            int newSize = original.Length + 2 * paddingAmount;
            float[] paddedArray = new float[newSize];

            Array.Copy(original, 0, paddedArray, paddingAmount, original.Length);

            return paddedArray;
        }

        public static float[] GenerateTone(float[] input, int samplerate, int frequency)
        {
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input cannot be null or empty.");

            float[] output = new float[input.Length];
            double angularFrequency = 2 * Math.PI * frequency / samplerate;

            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (float)Math.Sin(i * angularFrequency);
            }

            return output;
        }

        public static bool DetectTone(float[] input, int samplerate, int frequency)
        {
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input cannot be null or empty.");

            // Normalize input to avoid issues with quiet sounds
            NormalizeAudio(input);

            int n = input.Length;
            double normalizedFreq = (double)frequency / samplerate;
            double omega = 2 * Math.PI * normalizedFreq;

            double realPart = 0;
            double imagPart = 0;

            for (int i = 0; i < n; i++)
            {
                double angle = omega * i;
                realPart += input[i] * Math.Cos(angle);
                imagPart += input[i] * Math.Sin(angle);
            }

            // Compute power of the detected frequency
            double magnitudeSquared = realPart * realPart + imagPart * imagPart;

            // **Dynamic threshold** based on signal energy
            double avgEnergy = input.Average(x => x * x);
            double threshold = avgEnergy * 0.5;  // Adjust multiplier if needed

            return magnitudeSquared / n > threshold;
        }

        private static void NormalizeAudio(float[] audio)
        {
            float maxVal = audio.Max(Math.Abs);
            if (maxVal > 0)
            {
                for (int i = 0; i < audio.Length; i++)
                    audio[i] /= maxVal;
            }
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

        public static void PrintWithColor(bool[] input, bool[] encodedHandshakePattern, bool[] normalHandshakePattern)
        {
            int inputLength = input.Length;
            int handshakeLength = encodedHandshakePattern.Length;
            bool[] fullMatchMask = new bool[inputLength];

            // Find handshake occurrences (full matches only)
            for (int i = 0; i <= inputLength - handshakeLength; i++)
            {
                bool match = true;
                for (int j = 0; j < handshakeLength; j++)
                {
                    if (input[i + j] != encodedHandshakePattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    for (int j = 0; j < handshakeLength; j++)
                    {
                        fullMatchMask[i + j] = true;
                    }
                }
            }

            // Print bits with colored backgrounds
            for (int i = 0; i < input.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Black;

                if (fullMatchMask[i])
                {
                    Console.BackgroundColor = ConsoleColor.DarkGreen; // Dark Green for full match
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Red; // Red for non-matching bits
                }

                Console.Write(input[i] ? "1" : "0");
                Console.ResetColor(); // Reset color for the next character
            }

            Console.WriteLine(); // New line for formatting
        }

        #endregion
    }
}
