using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using System.Diagnostics;

namespace AQA_Assembler;
using System.IO;
using OpenAL;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;

        foreach (var x in Application.Current.Resources)
        {
            Console.WriteLine($"ASSET {x}");
        }
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
                    Play("/Users/bill/Projects/CSNEA/AQA-Assembler/Assets/funky.mp3");
                }
            }
        }
    }


    private void Play(string path) 
    {
        string filePath = path; // Change this to your actual WAV file path

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found: " + filePath);
            return;
        }

        // Initialize OpenAL
        IntPtr device = Alc.OpenDevice(null);
        if (device == IntPtr.Zero)
        {
            Console.WriteLine("Failed to open OpenAL device.");
            return;
        }

        IntPtr context = Alc.CreateContext(device, (int[])null);
        Alc.MakeContextCurrent(context);
        // Load WAV file
        var audioData = LoadWave(filePath, out int channels, out int sampleRate, out ALFormat format);
        // Generate buffer and source
        uint buffer, source;
        AL.GenBuffers(1, out buffer);
        AL.GenSources(1, out source);

        // Buffer the audio data
        AL.BufferData(buffer, format, audioData, audioData.Length, sampleRate);
        AL.Source(source, ALSourcei.Buffer, (int)buffer);

        // Play the sound
        AL.SourcePlay(source);
        Console.WriteLine("Playing audio... Press Enter to exit.");
        Console.ReadLine();

        // Cleanup
        AL.SourceStop(source);
        AL.DeleteSources(1, ref source);
        AL.DeleteBuffers(1, ref buffer);
        Alc.MakeContextCurrent(IntPtr.Zero);
        Alc.DestroyContext(context);
        Alc.CloseDevice(device);
    }

    static byte[] LoadWave(string filename, out int channels, out int sampleRate, out ALFormat format)
    {
        using (var reader = new BinaryReader(File.OpenRead(filename)))
        {
            // Skip RIFF header
            reader.BaseStream.Seek(22, SeekOrigin.Begin);
            channels = reader.ReadInt16();
            sampleRate = reader.ReadInt32();
            reader.BaseStream.Seek(34, SeekOrigin.Begin);
            int bitsPerSample = reader.ReadInt16();
            reader.BaseStream.Seek(40, SeekOrigin.Begin);
            int dataSize = reader.ReadInt32();
            byte[] data = reader.ReadBytes(dataSize);

            format = (channels == 1 && bitsPerSample == 8) ? ALFormat.Mono8 :
                     (channels == 1 && bitsPerSample == 16) ? ALFormat.Mono16 :
                     (channels == 2 && bitsPerSample == 8) ? ALFormat.Stereo8 :
                     (channels == 2 && bitsPerSample == 16) ? ALFormat.Stereo16 :
                     throw new NotSupportedException("Unsupported audio format");

            return data;
        }



    }


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