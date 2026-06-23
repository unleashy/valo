using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;

namespace Valo.App;

public partial class MainWindowViewModel(
    IStorageProvider storage,
    GameBoyService gameBoyService
) : ViewModelBase
{
    [RelayCommand]
    public async Task OpenFile()
    {
        var fileType = new FilePickerFileType("Game Boy ROM") { Patterns = ["*.gb"] };
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Open Game Boy ROM",
            AllowMultiple = false,
            FileTypeFilter = [fileType, FilePickerFileTypes.All],
            SuggestedFileType = fileType,
        });

        if (files.Count == 0) return;

        using var file = files[0];
        await LoadCartridge(file);
    }

    public async Task LoadFile(string path)
    {
        using var file =
            await storage.TryGetFileFromPathAsync(new Uri(path)) ??
            throw new FileNotFoundException("File not found", path);

        await LoadCartridge(file);
    }

    private async Task LoadCartridge(IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();

        using var contents = new MemoryStream();
        await stream.CopyToAsync(contents);

        gameBoyService.LoadCartridge(contents.GetBuffer());
        await gameBoyService.RunAsync();
    }
}
