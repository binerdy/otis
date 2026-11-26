// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using Otis.Infrastructure.Storage;

namespace Otis.App.Services;

public static class CrashLog
{
    public static void Write(Exception exception)
    {
        try
        {
            var line = string.Create(CultureInfo.InvariantCulture, $"{TimeProvider.System.GetUtcNow():u} {exception}{Environment.NewLine}{Environment.NewLine}");
            File.AppendAllText(Path.Combine(DataDirectory.Resolve(), "crash.log"), line);
        }
        catch (IOException)
        {
            // Logging must never be the reason Otis fails.
        }
    }
}
