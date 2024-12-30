using Avalonia.Interactivity;
namespace AQA_Assembler;
using System;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.KeyDown += OnKeyDown;

    }

    // private void OnButtonClick(object sender, RoutedEventArgs e){
    //     TextBox? textBoxContent = this.FindControl<TextBox>("MyTextBox");
    //     if (textBoxContent == null) { 
    //         return;
    //     }
    //     Console.WriteLine($"You entered: {textBoxContent.Text}");
    // }

    private void OnKeyDown(object? sender, KeyEventArgs e) {
        if (e.Key == Key.F5)
        {
            TextBox? textBoxContent = this.FindControl<TextBox>("MyTextBox");
            if (textBoxContent == null) { 
                return;
            }
            Console.WriteLine($"You entered: {textBoxContent.Text}");
        }
    }

    private void Refresh()
    {
        Console.WriteLine("Refresh executed!");
    }
}