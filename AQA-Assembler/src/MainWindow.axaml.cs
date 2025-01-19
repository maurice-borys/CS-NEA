using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;



namespace AQA_Assembler;

public partial class MainWindow : Window
{
    readonly AudioPlayer Player;
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        Player = new AudioPlayer();
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
                    Player.Play("funky");
                }
            }
        }
    }
}