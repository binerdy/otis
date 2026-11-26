// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.ApiTests.Support;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Directory.CreateDirectory(this.Path);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "otis-tests", Guid.NewGuid().ToString("N"));

    public string File(string name) => System.IO.Path.Combine(this.Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(this.Path, recursive: true);
        }
        catch (IOException)
        {
            // A font file may still be loaded by GDI for a moment; the temp folder is cleaned up later.
        }
    }
}
