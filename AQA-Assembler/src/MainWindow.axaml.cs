using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using System.Diagnostics;
using NAudio.Wave;

namespace AQA_Assembler;
using System.IO;
using OpenTK.Audio.OpenAL;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e) 
    {
        if (e.Key == Key.F5)
        {
            TextBox? textBoxContent = this.FindControl<TextBox>("SourceCode");
            if (textBoxContent == null) { 
                return;
            }
            Console.WriteLine($"You entered: {textBoxContent.Text}");
        }
    }

    private void MyTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl)
        {
            var selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab != null)
            {
                string? tabHeader = selectedTab.Header as string;
                if (tabHeader == null)
                {
                    return;
                }
                if (tabHeader == "Secret")
                {
                    Play("/Users/bill/Projects/CSNEA/AQA-Assembler/Assets/funky.wav");
                }
            }
        }
    }


    private void Play(string path)  
    {
        string filePath = path; // Change to your WAV file

        // Initialize OpenAL
        string deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
        ALDevice? device = ALC.OpenDevice(deviceName); // Open default audio device
        if (device == null)
        {
            Console.WriteLine("NO DEVICE FOUND");
            return;
        }

        int[]? attributes = null;
        
        ALContext context = ALC.CreateContext((ALDevice) device, attributes);
        ALC.MakeContextCurrent(context);

        Console.WriteLine($"BUFFER: {AL.GetError()}");
        // Generate OpenAL buffer and source
        AL.GenBuffer(out int buffer);
        int source = AL.GenSource();

        // Load WAV file into OpenAL buffer
        byte[] wavData = LoadWav(filePath, out ALFormat format, out int sampleRate);
        AL.BufferData(buffer, format, ref wavData[0], wavData.Length, sampleRate);

        // Attach buffer to source and play
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.SourcePlay(source);
        Console.WriteLine($"PLAYING: {AL.GetError()}");
        Console.WriteLine("Playing audio... Press Enter to stop.");
        Console.ReadLine(); // Wait for user input

        // Cleanup
        AL.DeleteSource(source);
        AL.DeleteBuffer(buffer);
        ALC.DestroyContext(context);
        ALC.CloseDevice((ALDevice) device);
    }

    static byte[] LoadWav(string filePath, out ALFormat format, out int sampleRate)
    {
        using (var memoryStream = new MemoryStream())
        using (var reader = new WaveFileReader(filePath))
        {
            // Create a buffer to read data
            byte[] buffer = new byte[reader.Length];
            int bytesRead;

            // Read the WAV file into the buffer
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
            }
            sampleRate = reader.WaveFormat.SampleRate;

            int numChannels = reader.WaveFormat.Channels;
            int bitsPerSample = reader.WaveFormat.BitsPerSample;
            if (numChannels == 1 && bitsPerSample == 8) format = ALFormat.Mono8;
            else if (numChannels == 1 && bitsPerSample == 16) format = ALFormat.Mono16;
            else if (numChannels == 2 && bitsPerSample == 8) format = ALFormat.Stereo8;
            else if (numChannels == 2 && bitsPerSample == 16) format = ALFormat.Stereo16;
            else throw new Exception("Unsupported WAV format!");
            return memoryStream.ToArray(); 
        }
    }
}


        // using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        // using (BinaryReader reader = new BinaryReader(fs))
        // {
        //     // Read WAV header
        //     reader.ReadChars(4); // "RIFF"
        //     reader.ReadInt32(); // File size
        //     reader.ReadChars(4); // "WAVE"
        //     reader.ReadChars(4); // "fmt "
        //     reader.ReadInt32(); // Subchunk size
        //     reader.ReadInt16(); // Audio format
        //     ushort numChannels = reader.ReadUInt16(); // Channels
        //     sampleRate = reader.ReadInt32(); // Sample rate
        //     reader.ReadInt32(); // Byte rate
        //     reader.ReadInt16(); // Block align
        //     ushort bitsPerSample = reader.ReadUInt16(); // Bits per sample

        //     // Read "data" chunk
        //     reader.ReadChars(4); // "data"
        //     int dataSize = reader.ReadInt32();

        //     // Determine OpenAL format
        //     if (numChannels == 1 && bitsPerSample == 8) format = ALFormat.Mono8;
        //     else if (numChannels == 1 && bitsPerSample == 16) format = ALFormat.Mono16;
        //     else if (numChannels == 2 && bitsPerSample == 8) format = ALFormat.Stereo8;
        //     else if (numChannels == 2 && bitsPerSample == 16) format = ALFormat.Stereo16;
        //     else throw new Exception("Unsupported WAV format!");

        //     return reader.ReadBytes(dataSize); // Read audio data
        // }
    //     // Path to the Rust executable
    //     string rustAppPath = "/Users/bill/Projects/CSNEA/audio_wizard/target/release/audio_wizard"; // Adjust this to your actual path


    //     try
    //     {
    //         // Start the Rust executable and pass the arguments
    //         ProcessStartInfo startInfo = new ProcessStartInfo
    //         {
    //             FileName = rustAppPath,
    //             Arguments = path,
    //             RedirectStandardOutput = false,  //std output
    //             RedirectStandardError = true,  
    //             UseShellExecute = false, 
    //             CreateNoWindow = true 
    //         };

    //         using (Process? process = Process.Start(startInfo))
    //         {
    //             if  (process == null)
    //             {
    //                 return;
    //             }
    //             string output = process.StandardOutput.ReadToEnd();
    //             string error = process.StandardError.ReadToEnd();

    //             process.WaitForExit();

    //             Console.WriteLine("Output: " + output);
    //             Console.WriteLine("Error " + error);
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine("An error occurred: " + ex.Message);
    //     }
    // }