using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Valo.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        if (Design.IsDesignMode) {
            Design.SetDataContext(
                this,
                new MainWindowViewModel(StorageProvider, new GameBoyService())
            );
        }

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
