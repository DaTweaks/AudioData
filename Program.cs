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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using AudioData;
using NAudio;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

class Program
{
    const int SampleRate = 44100; // Hz
    const double BitDuration = 0.0125; // seconds
    const double Frequency0 = 1000; // Frequency for binary 0
    const double Frequency1 = 2000; // Frequency for binary 1

    //static void Main(string[] args) // EXAMPLE USAGE OF HAMMING ENCODER!aa
    //{
    //    string input = "10110110";
    //    Console.WriteLine(input);

    //    var encoded = HammingEncoder.Encode(Helpers.prettyStringToBoolArray(input), input.Length);

    //    Console.WriteLine(Helpers.boolArrayToPrettyString(encoded));
    //    HammingEncoder.MixinRandomError(encoded, 1);

    //    Console.WriteLine(Helpers.boolArrayToPrettyString(HammingEncoder.Decode(encoded, input.Length)));
    //}

    static void Main(string[] args)
    {
        string data = "TEST";

        var binary = StringToBinary(data);

        binary = HammingEncoder.GroupEncode(binary, 8);

        var filespace = SaveAudioToFile(EncodeDataToAudio(binary), $"output.wav");

        Console.WriteLine($"Sending data. Send time is:  {GetWavFileDuration("output.wav").Seconds}.{GetWavFileDuration("output.wav").Milliseconds} Seconds");

        PlayAudio(filespace);

        Console.WriteLine("Data sent!");

        var editedBinary = DecodeAudioToData(filespace);

        HammingEncoder.MixinRandomError(editedBinary, 1); // Mix in an false bit for funsies :)

        var dataConvertedData = BinaryToString(HammingEncoder.GroupDecode(editedBinary, 8));

        Console.WriteLine(dataConvertedData);
        Console.WriteLine($"Was it a failure? : {dataConvertedData != data}");

        Console.ReadKey(true);
    }

    /// <param name="text">data to be converted.</param>
    /// <returns>Array of 8 bit pairs.</returns>
    static bool[] StringToBinary(string text)
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
    static string BinaryToString(bool[] binary)
    {
        StringBuilder sb = new StringBuilder();

        if (binary.Length % 8 != 0)
        {
            binary = MakeLengthMultipleOf8(binary);
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

    static bool[] MakeLengthMultipleOf8(bool[] boolArray)
    {
        int originalLength = boolArray.Length;
        int newLength = originalLength;

        // Calculate the new length that is a multiple of 8
        while (newLength % 8 != 0)
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

    static void PlayAudio(string fileName)
    {
        using (var audioFile = new AudioFileReader(fileName))
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

    static TimeSpan GetWavFileDuration(string fileName)
    {
        using (var writer = new WaveFileReader(fileName))
        {
            return writer.TotalTime;
        }
    }

    static float[] EncodeDataToAudio(bool[] data)
    {
        var handshake = GenerateHandshake();
        data = handshake.Concat(data).Concat(handshake).ToArray();

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

        // When trying to 

        var list = audioData.ToList();

        var random = new Random();

        for (int i = 0; i < 50000; i++) // Set random static at the beginning and end
        {
            list.Insert(0, 0);
            list.Insert(list.Count-1, 0);
        }

        return AddNoise(list.ToArray(), 2f);
    }

    static float[] AddNoise(float[] soundArray, float noiseLevel)
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

    static bool[] DecodeAudioToData(string fileName)
    {
        // Get AudioData from the file;
        float[] audioData = LoadAudioFromFile(fileName);
        // Calculate where the bits will be placed.
        int samplesPerBit = (int)(SampleRate * BitDuration);

        List<bool> decodedStringData = new List<bool>();
        for (int i = 0; i < audioData.Length; i += samplesPerBit)
        {
            float[] bitData = audioData.Skip(i).Take(samplesPerBit).ToArray();
            double frequency = DetectFrequency(bitData);
            decodedStringData.Add(frequency == Frequency0 ? false : true);
        }

        var handshake = GenerateHandshake();

        return RemoveBeforeHandShake(RemoveAfterHandShake(decodedStringData.ToArray(), handshake), handshake);
    }

    /// <summary>
    /// Removes the handshake that occurs before the transmission. and all bits that occur before it.
    /// </summary>
    /// <returns>Updated data that starts when the data starts.</returns>
    public static bool[] RemoveBeforeHandShake(bool[] input, bool[] handshake)
    {
        int handshakeLength = handshake.Length;
        int inputLength = input.Length;

        // Iterate through the input array to find the handshake pattern
        for (int i = 0; i <= inputLength - handshakeLength; i++)
        {
            bool isMatch = true;
            for (int j = 0; j < handshakeLength; j++)
            {
                if (input[i + j] != handshake[j])
                {
                    isMatch = false;
                    break;
                }
            }

            // If the handshake pattern is found, return the array from the end of the handshake to the end of the input
            if (isMatch)
            {
                int newLength = inputLength - (i + handshakeLength);
                bool[] result = new bool[newLength];
                Array.Copy(input, i + handshakeLength, result, 0, newLength);
                return result;
            }
        }

        // If no handshake pattern is found, return the original array
        return input;
    }

    /// <summary>
    /// Removes the handshake that occurs at the end of the transmission and all bits that occur after it.
    /// </summary>
    /// <returns>All the data that was sent before the handshake</returns>
    public static bool[] RemoveAfterHandShake(bool[] input, bool[] handshake)
    {
        int handshakeLength = handshake.Length;
        int inputLength = input.Length;

        // Iterate through the input array to find the handshake pattern, starting from the end
        for (int i = inputLength - handshakeLength; i >= 0; i--)
        {
            bool isMatch = true;
            for (int j = 0; j < handshakeLength; j++)
            {
                if (input[i + j] != handshake[j])
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

        // If no handshake pattern is found, return the original array
        return input;
    }

    public static bool[] GenerateHandshake()
    {
        int handshakeLength = 100;

        List<bool> result = new List<bool>();

        for (int i = 0; i < handshakeLength; i++)
        {
            result.Add(i > (handshakeLength / 2));
        }

        return result.ToArray();
    }
    static double DetectFrequency(float[] samples)
    {
        double power0 = Goertzel(samples, Frequency0);
        double power1 = Goertzel(samples, Frequency1);

        return power0 > power1 ? Frequency0 : Frequency1;
    }

    static double Goertzel(float[] samples, double targetFrequency)
    {
        int N = samples.Length;
        double k = (int)(0.5 + ((N * targetFrequency) / SampleRate));
        double omega = (2.0 * Math.PI * k) / N;
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

    /// <returns>the file name.</returns>
    static string SaveAudioToFile(float[] audioData, string fileName)
    {
        using (var writer = new WaveFileWriter(fileName, new WaveFormat(SampleRate, 1)))
        {
            writer.WriteSamples(audioData, 0, audioData.Length);
        }
        return fileName;
    }

    static float[] LoadAudioFromFile(string fileName)
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
