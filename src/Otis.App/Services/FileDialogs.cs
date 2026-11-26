// SPDX-License-Identifier: GPL-3.0-or-later
using Windows.Storage.Pickers;

namespace Otis.App.Services;

public static class FileDialogs
{
    public static nint WindowHandle { get; set; }

    public static async Task<string?> OpenProjectAsync()
    {
        var picker = Initialize(new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary });
        picker.FileTypeFilter.Add(".json");
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public static async Task<IReadOnlyList<string>> OpenSourcesAsync()
    {
        var picker = Initialize(new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary });
        foreach (var type in new[] { ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp" })
        {
            picker.FileTypeFilter.Add(type);
        }

        var files = await picker.PickMultipleFilesAsync();
        return files.Select(f => f.Path).ToList();
    }

    public static async Task<string?> SaveAsync(string suggestedName, string label, string extension)
    {
        var picker = Initialize(new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary, SuggestedFileName = suggestedName });
        picker.FileTypeChoices.Add(label, [extension]);
        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    private static T Initialize<T>(T picker)
    {
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHandle);
        return picker;
    }
}
