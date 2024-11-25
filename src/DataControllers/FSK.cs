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
    public class FSK : DataControl
    {
        const double BPS = 80;
        const double BitDuration = 1.0 / BPS; // seconds
        const double Frequency0 = 1000; // Frequency for binary 0
        const double Frequency1 = 3000; // Frequency for binary 1

        #region Audio

        public override float[] EncodeDataToAudio(bool[] data, float noise, int SampleRate)
        {
            data = GenerateStartHandshakeEncoded()
           .Concat(data)
           .Concat(GenerateEndHandshakeEncoded())
           .ToArray();

            int samplesPerBit = (int)(SampleRate * BitDuration);

            float[] audioData = new float[(data.Length * samplesPerBit)];

            for (int i = 0; i < data.Length; i++)
            {
                double frequency = data[i] == false ? Frequency0 : Frequency1;
                for (int j = 0; j < samplesPerBit; j++)
                {
                    double t = (double)j / SampleRate;
                    audioData[i * samplesPerBit + j] = (float)Math.Sin(2 * Math.PI * frequency * t);
                }
            }

            //var list = PadArrayWithZeros(audioData, 50000);

            return AddNoise(audioData, noise); // Should give like a 50% chance of it coming through completely fine. Will rework encoder.
        }

        public override bool[] DecodeAudioToData(string fileName, int SampleRate)
        {
            // Get AudioData from the file;
            float[] audioData = LoadAudioFromFile(fileName);
            // Calculate where the bits will be placed.
            int samplesPerBit = (int)(SampleRate * BitDuration);

            List<bool> decodedStringData = new List<bool>();
            for (int i = 0; i < audioData.Length; i += samplesPerBit)
            {
                float[] bitData = audioData.Skip(i).Take(samplesPerBit).ToArray();
                double frequency = DetectFrequency(bitData, SampleRate);
                decodedStringData.Add(frequency == Frequency0 ? false : true);
            }

            return RemoveBeforeHandShake(RemoveAfterHandShake(decodedStringData.ToArray()));
        }
        
        public override bool[] DecodeAudioToData(float[] audioData, int SampleRate)
        {
            // Calculate where the bits will be placed.
            int samplesPerBit = (int)(SampleRate * BitDuration);

            List<bool> decodedStringData = new List<bool>();
            for (int i = 0; i < audioData.Length; i += samplesPerBit)
            {
                float[] bitData = audioData.Skip(i).Take(samplesPerBit).ToArray();
                double frequency = DetectFrequency(bitData, SampleRate);
                decodedStringData.Add(frequency == Frequency0 ? false : true);
            }

            return RemoveBeforeHandShake(RemoveAfterHandShake(decodedStringData.ToArray()));
        }

        /// <summary>
        /// Removes the handshake that occurs before the transmission. and all bits that occur before it.
        /// </summary>
        /// <returns>Updated data that starts when the data starts.</returns>
        private bool[] RemoveBeforeHandShake(bool[] input)
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
        private bool[] RemoveAfterHandShake(bool[] input)
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

        private bool[] GenerateStartHandshake()
        {
            return StringToBinary("S\u0002");
        }

        private bool[] GenerateStartHandshakeEncoded()
        {
            return MessageEncoder.GroupEncode(GenerateStartHandshake(), 8);
        }

        private bool[] GenerateEndHandshake()
        {
            return StringToBinary("\u0003E");
        }

        private bool[] GenerateEndHandshakeEncoded()
        {
            return MessageEncoder.GroupEncode(GenerateEndHandshake(), 8);
        }

        private double DetectFrequency(float[] samples, int SampleRate)
        {
            double power0 = Goertzel(samples, Frequency0, SampleRate);
            double power1 = Goertzel(samples, Frequency1, SampleRate);

            return power0 > power1 ? Frequency0 : Frequency1;
        }

        private double Goertzel(float[] samples, double targetFrequency, int SampleRate)
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

        #endregion
    }
}
