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
        await using var stream = await file.OpenReadAsync();

        using var contents = new MemoryStream();
        await stream.CopyToAsync(contents);

        gameBoyService.LoadCartridge(contents.GetBuffer());
    }
}
