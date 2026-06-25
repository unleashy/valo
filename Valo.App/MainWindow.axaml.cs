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
                new MainWindowViewModel(StorageProvider)
            );
        }

        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_args.Length >= 1 && DataContext is MainWindowViewModel vm) {
            vm.LoadFileCommand.Execute(_args[0]);
        }

        MinHeight += Menu.Bounds.Height;
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
