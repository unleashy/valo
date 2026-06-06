using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Valo.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        GameScreen.Focus();
    }

    public void ExitOnClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        Close();
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        GameScreen.AcceptKeyDown(e);
    }
}
