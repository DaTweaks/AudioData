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

namespace AudioData.DataControllers.DartControllers
{
    public class FSK : DataController
    {
        const double BPS = 80;
        const double BitDuration = 1.0 / BPS;
        const double Frequency0 = 1000; // Frequency for binary 0
        const double Frequency1 = 3000; // Frequency for binary 1

        public override int GetBitsPerSecond() => (int)BPS;

        public override string GetName() => "FSK";
        public override string GetDescription() => "Frequency Shift Keying";

        public override int GetOptimalDetectionTone() => 3500;

        public override float[] EncodeDataToAudio(bool[] data, int SampleRate, float noise = 0f)
        {
            data = GenerateStartHandshakeEncoded()
           .Concat(data)
           .Concat(GenerateEndHandshakeEncoded())
           .ToArray();

            int samplesPerBit = (int)(SampleRate * BitDuration);

            float[] audioData = new float[data.Length * samplesPerBit];

            // Iterate over each bit in the data array
            for (int i = 0; i < data.Length; i++)
            {
                double frequency = data[i] ? Frequency1 : Frequency0;  // Set frequency based on the bit value (true/false)

                // For each sample in the current bit duration
                for (int j = 0; j < samplesPerBit; j++)
                {
                    // Create the sine wave for the current sample
                    double t = (double)j / SampleRate;
                    audioData[i * samplesPerBit + j] = (float)Math.Sin(2 * Math.PI * frequency * t);
                }
            }

            //audioData = PadArrayWithZeros(audioData, 5000);

            if (noise > 0f)
            {
                audioData = AddNoise(audioData, noise);
            }

            return audioData;
        }


        protected override bool[] DecodeAudio(float[] audioData, int sampleRate, int offset)
        {
            float[] shifted = audioData.Skip(offset).ToArray();

            // Calculate where the bits will be placed.
            int samplesPerBit = (int)(sampleRate * BitDuration);

            List<bool> decodedStringData = new List<bool>();
            for (int i = 0; i < shifted.Length; i += samplesPerBit)
            {
                float[] bitData = shifted.Skip(i).Take(samplesPerBit).ToArray();
                double frequency = DetectFrequency(bitData, sampleRate);
                decodedStringData.Add(frequency == Frequency0 ? false : true);
            }

            //Console.WriteLine("Demodulated Data: "+Helpers.boolArrayToPrettyString(decodedStringData.ToArray()));

            return RemoveBeforeHandShake(RemoveAfterHandShake(decodedStringData.ToArray()));
        }

        private double DetectFrequency(float[] samples, int sampleRate)
        {
            double power0 = Goertzel(samples, Frequency0, sampleRate);
            double power1 = Goertzel(samples, Frequency1, sampleRate);
            //Console.WriteLine("Bit0: " + power0 + " Bit1: " + power1);
            return power0 > power1 ? Frequency0 : Frequency1;
        }

        private bool ReadBit(float[] samples, int SampleRate)
        {
            double power0 = Goertzel(samples, Frequency0, SampleRate);
            double power1 = Goertzel(samples, Frequency1, SampleRate);
            //Console.WriteLine("Bit0: " + power0 + " Bit1: " + power1);
            return power0 > power1 ? false : true;
        }
    }
}
