using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Valo.App;

public partial class MainWindow : Window
{
    private readonly string[] _args;

    public MainWindow(string[] args)
    {
        _args = args;

        if (Design.IsDesignMode) {
            Design.SetDataContext(
                this,
                new MainWindowViewModel(StorageProvider, new GameBoyService())
            );
        }

        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        GameScreen.Focus();

        if (_args.Length >= 1 && DataContext is MainWindowViewModel vm) {
            await vm.LoadFile(_args[0]);
        }
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
