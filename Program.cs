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
using System.IO;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AudioData.DataControllers;

class Program
{
    static void Main(string[] args)
    {
        UnitTestFSK();
    }

    #region HammingEncoder

    static void TestHamming() // EXAMPLE USAGE OF HAMMING ENCODER!
    {
        string input = "10110110";
        Console.WriteLine(input);

        var encoded = MessageEncoder.GroupDecode(Helpers.prettyStringToBoolArray(input), input.Length);

        Console.WriteLine(Helpers.boolArrayToPrettyString(encoded));
        MessageEncoder.MixinRandomError(encoded, 1);

        Console.WriteLine(Helpers.boolArrayToPrettyString(MessageEncoder.GroupDecode(encoded, input.Length)));
    }

    #endregion

    #region QPSK

    public static void TestQPSK() 
    { 
        var datacontrol = new QPSK();

        string encryptionKey = "hemligtLösenord";

        string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

        Console.WriteLine($"Modulating Data: {data}");

        var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

        var binary = datacontrol.StringToBinary(encryptedData);

        var audioData = datacontrol.EncodeDataToAudio(binary);

        datacontrol.SaveAudioToFile(audioData, "OUTPUT.wav");

        var editedBinary = datacontrol.DecodeAudioToData(audioData);

        var dataConvertedData = AESEncryption.DecryptString(datacontrol.BinaryToString(editedBinary), encryptionKey);

        Console.WriteLine($"Demodulated data: {dataConvertedData} Correct Demodulation: {dataConvertedData == data}");

        Console.ReadKey();
    }

    #endregion

    #region FSK

    public static Dictionary<float, float> totalTries = new Dictionary<float, float>();

    public static List<bool> tries = new List<bool>();

    public static void UnitTestFSK()
    {
        float  startingNoiseValue = 0.0f;
        //Thread thread = new Thread(updateTries);
        var datacontrol = new FSK();
        //thread.Start();
        while (totalTries.Count != 40)
        {
            string encryptionKey = "hemligtLösenord";

            string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

            var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

            var binary = datacontrol.StringToBinary(encryptedData);

            if (tries.Count >= 1000)
            {
                datacontrol.SaveAudioToFile(datacontrol.EncodeDataToAudio(binary, startingNoiseValue), $"OUTPUT_NOISE{startingNoiseValue}.wav");
                Console.WriteLine($"Complete with noise level: {startingNoiseValue} Percent Calculated: {trypercent()}%");
                totalTries.Add(startingNoiseValue, (float)trypercent());
                tries.Clear();
                startingNoiseValue += 0.1f;
            }

            binary = MessageEncoder.GroupEncode(binary, 8);

            var audioData = datacontrol.EncodeDataToAudio(binary, startingNoiseValue);

            var editedBinary = datacontrol.DecodeAudioToData(audioData);

            var dataConvertedData = AESEncryption.DecryptString(datacontrol.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

            tries.Add(dataConvertedData == data);

            if (GetSuccess() == 0)
                datacontrol.SaveAudioToFile(audioData, "ERR.wav");
        }

        string filePath = "data.txt";

        // Use StreamWriter to save the dictionary to a file
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var kvp in totalTries)
            {
                // Write the key and both float values to the file
                writer.WriteLine($"NOISE: {kvp.Key}     -   {kvp.Value}%");
            }
        }

        Console.WriteLine("DONE!");

        //thread.Abort();

        // WHICH SAVES THE FIRST AND THE LAST NUMBER.
    }

    public static void TestFSK()
    {
        float startingNoiseValue = 3f;
        Thread thread = new Thread(updateTries);
        var datacontrol = new FSK();
        thread.Start();
        while (true)
        {
            string encryptionKey = "hemligtLösenord";

            string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

            var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

            var binary = datacontrol.StringToBinary(encryptedData);

            binary = MessageEncoder.GroupEncode(binary, 8);

            var audioData = datacontrol.EncodeDataToAudio(binary, startingNoiseValue);

            var editedBinary = datacontrol.DecodeAudioToData(audioData);

            var dataConvertedData = AESEncryption.DecryptString(datacontrol.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

            tries.Add(dataConvertedData == data);
        }
    }

    public static void updateTries()
    {
        while (true)
        {
            Thread.Sleep(10);
            Console.SetCursorPosition(0, 0);
            Console.Write($"Tried this many times: {tries.Count} succeeded this many times: {GetSuccess()} SuccessPercent: {trypercent()}%                  ");
        }
    }

    public static double trypercent()
    {
        int success = GetSuccess();
        if (success != 0 || tries.Count != 0)
            return Math.Round(((double)success / (double)tries.Count) * 100, 2);

        return 0;
    }

    public static int GetSuccess()
    {
        int succeeded = 0;

        for (int i = 0; i < tries.Count; i++)
        {
            if (tries[i])
            {
                succeeded++;
            }
        }

        return succeeded;
    }

    #endregion
}
