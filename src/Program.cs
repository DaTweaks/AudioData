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

using AudioData;
using AudioData.DataControllers;
using NAudio.Wave;
using Spectrogram;

class Program
{
    static void Main(string[] args)
    {
        sINGLE();
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
        int SampleRate = 44100;

        var datacontrol = new QPSK();

        string encryptionKey = "hemligtLösenord";

        string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

        Console.WriteLine($"Modulating Data: {data}");

        var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

        var binary = datacontrol.StringToBinary(encryptedData);

        var audioData = datacontrol.EncodeDataToAudio(binary, 0f, SampleRate);

        //datacontrol.PlayAudio(datacontrol.SaveAudioToFile(audioData, "QPSK.wav", SampleRate));

        datacontrol.SaveAudioToFile(audioData, "OUTPUT.wav", SampleRate);

        GenerateSpectrogram(audioData, SampleRate, "QPSK.png");

        var editedBinary = datacontrol.DecodeAudioToData(audioData, SampleRate);

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
        int SampleRate = 192000;
        float  startingNoiseValue = 2.4f;
        Thread thread = new Thread(updateTries);
        var datacontrol = new FSK();
        thread.Start();
        while (totalTries.Count != 70)
        {
            string encryptionKey = "hemligtLösenord";

            string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

            var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

            var binary = datacontrol.StringToBinary(encryptedData);

            if (tries.Count >= 100)
            {
                Console.SetCursorPosition(0, 1);
                Console.Write($"Complete with noise level: {startingNoiseValue} Percent Calculated: {trypercent()}%                ");
                totalTries.Add(startingNoiseValue, (float)trypercent());
                tries.Clear();
                startingNoiseValue += 0.1f;
            }

            binary = MessageEncoder.GroupEncode(binary, 8);

            var audioData = datacontrol.EncodeDataToAudio(binary, startingNoiseValue, SampleRate);

            var editedBinary = datacontrol.DecodeAudioToData(audioData, SampleRate);

            var dataConvertedData = AESEncryption.DecryptString(datacontrol.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

            tries.Add(dataConvertedData == data);
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
        int SampleRate = 192000;
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

            var audioData = datacontrol.EncodeDataToAudio(binary, startingNoiseValue, SampleRate);

            var editedBinary = datacontrol.DecodeAudioToData(audioData, SampleRate);

            var dataConvertedData = AESEncryption.DecryptString(datacontrol.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

            tries.Add(dataConvertedData == data);
        }
    }

    public static void SingleTestFSK()
    {
        int SampleRate = 192000;
        float startingNoiseValue = 3f;
        var datacontrol = new FSK();
        string encryptionKey = "hemligtLösenord";

        string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

        var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

        var binary = datacontrol.StringToBinary(encryptedData);

        binary = MessageEncoder.GroupEncode(binary, 8);

        var audioData = datacontrol.EncodeDataToAudio(binary, startingNoiseValue, SampleRate);

        GenerateSpectrogram(audioData, SampleRate, "FSK.png");

        var editedBinary = datacontrol.DecodeAudioToData(audioData, SampleRate);

        var dataConvertedData = AESEncryption.DecryptString(datacontrol.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

        tries.Add(dataConvertedData == data);
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
            return Math.Round(((double)success / (double)tries.Count) * 100, 4);

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

    #region Utils

    public static void GenerateSpectrogram(float[] data, int SampleRate, string name)
    {
        var sg = new SpectrogramGenerator(SampleRate, fftSize: 4096, stepSize: 500, maxFreq: 4000);

        double[] doubleData = new double[data.Length];

        for(int i = 0; i < data.Length; i++)
        {
            doubleData[i] = data[i];
        }

        sg.Add(doubleData);
        sg.SaveImage(name);
    }

    #endregion
}
