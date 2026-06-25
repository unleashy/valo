using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;

namespace Valo.App;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IStorageProvider _storage;
    private readonly GameBoyService _gameBoyService;

    public AvaloniaLcd Lcd { get; } = new();

    public MainWindowViewModel(IStorageProvider storage)
    {
        _storage = storage;
        _gameBoyService = new GameBoyService(Lcd);
    }

    [RelayCommand]
    public async Task OpenFile()
    {
        var fileType = new FilePickerFileType("Game Boy ROM") { Patterns = ["*.gb"] };
        var files = await _storage.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Open Game Boy ROM",
            AllowMultiple = false,
            FileTypeFilter = [fileType, FilePickerFileTypes.All],
            SuggestedFileType = fileType,
        });

        if (files.Count == 0) return;

        using var file = files[0];
        await LoadCartridge(file);
    }

    [RelayCommand]
    public async Task LoadFile(string path)
    {
        using var file =
            await _storage.TryGetFileFromPathAsync(new Uri(path)) ??
            throw new FileNotFoundException("File not found", path);

        await LoadCartridge(file);
    }

    private async Task LoadCartridge(IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();

        using var contents = new MemoryStream();
        await stream.CopyToAsync(contents);

        _gameBoyService.LoadCartridge(contents.GetBuffer());
        await _gameBoyService.RunAsync();
    }
}
