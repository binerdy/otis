// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Infrastructure.Storage;

public static class DataDirectory
{
    public const string OverrideVariable = "OTIS_DATA_DIR";

    // Kept apart from the program folder %LOCALAPPDATA%\Otis so that uninstalling never deletes it.
    public static string Resolve()
    {
        var overridden = Environment.GetEnvironmentVariable(OverrideVariable);
        var path = string.IsNullOrWhiteSpace(overridden)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Otis.Data")
            : overridden;
        Directory.CreateDirectory(path);
        return path;
    }
}
