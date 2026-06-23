using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Valo.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = CreateMainWindow(desktop.Args);
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow CreateMainWindow(string[]? args)
    {
        var window = new MainWindow(args ?? []);

        window.DataContext = new MainWindowViewModel(
            window.StorageProvider,
            new GameBoyService()
        );

        return window;
    }
}
