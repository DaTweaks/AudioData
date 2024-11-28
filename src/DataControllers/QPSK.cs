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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AudioData.DataControllers
{
    public class QPSK : DataControl
    {
        const double BPS = 80;
        const double BitDuration = 1.0 / BPS; // seconds
        const double BaseFrequency = 1000; // Frequency for binary 0

        public override string GetName() => "QPSK   -   Quadrature Shift Keying";

        public QPSK()
        {
            modulators.Add("00", 1);
            modulators.Add("01", (Math.PI / 2));
            modulators.Add("10", Math.PI);
            modulators.Add("11", (3 * Math.PI / 2));
        }

        private Dictionary<string, double> modulators = new Dictionary<string, double>();

        #region Audio

        public override float[] EncodeDataToAudio(bool[] data, float noise, int SampleRate)
        {
            data = GenerateStartHandshakeEncoded()
           .Concat(data)
           .Concat(GenerateEndHandshakeEncoded())
           .ToArray();

            int samplesPerBit = (int)(SampleRate * BitDuration);

            float[] audioData = new float[(data.Length * samplesPerBit)];


            for (int i = 0; i < data.Length; i += 2)
            {
                string bitpair = (data[i] == true ? "1" : "0") + (data[i+1] == true ? "1" : "0"); // Could make it a bit better than using strings here.

                double frequency = modulators[bitpair]*BaseFrequency;

                for (int j = 0; j < samplesPerBit; j++)
                {
                    double t = (double)j / SampleRate;
                    audioData[i * samplesPerBit + j] = (float)Math.Sin(2 * Math.PI * frequency * t);
                }
            }

            //var list = PadArrayWithZeros(audioData, 50000);

            return AddNoise(audioData, noise); // Should give like a 50% chance of it coming through completely fine. Will rework encoder.
        }

        public override bool[] DecodeAudioToData(float[] audioData, int SampleRate)
        {
            // Calculate where the bits will be placed.
            int samplesPerBit = (int)(SampleRate * BitDuration);

            string decodedStringData = "";
            for (int i = 0; i < audioData.Length; i += samplesPerBit)
            {
                float[] bitData = audioData.Skip(i).Take(samplesPerBit).ToArray();
                decodedStringData += DetectFrequency(bitData, SampleRate);
            }

            return RemoveBeforeHandShake(RemoveAfterHandShake(Helpers.prettyStringToBoolArray(decodedStringData)));
        }

        private string DetectFrequency(float[] samples, int SampleRate)
        {
            string bestGuess = "";
            double power = 0.0;

            foreach(var kvp in modulators)
            {
                var tempPower = Goertzel(samples, BaseFrequency * kvp.Value, SampleRate);
                if (tempPower > power)
                {
                    bestGuess = kvp.Key;
                    power = tempPower;
                }
            }

            return bestGuess;
        }

        #endregion
    }
}
