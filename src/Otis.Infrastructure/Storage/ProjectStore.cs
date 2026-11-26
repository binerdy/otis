// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using Otis.Core.Projects;

namespace Otis.Infrastructure.Storage;

public sealed class ProjectStore(TimeProvider time)
{
    public const string Extension = ".otis.json";
    public const string BackupFolder = ".otis-backups";
    private const int KeptBackups = 50;

    public static LoadedProject Load(string path) => ProjectJson.Read(File.ReadAllText(path));

    public static string SourcePath(string projectPath, string source) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, source.Replace('/', Path.DirectorySeparatorChar)));

    public static string RelativeSource(string projectPath, string sourcePath) =>
        Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, sourcePath).Replace(Path.DirectorySeparatorChar, '/');

    public void Save(string path, OtisProject project)
    {
        if (project.FormatVersion > OtisProject.CurrentFormatVersion)
        {
            throw new InvalidOperationException("This project was made with a newer Otis and is read-only.");
        }

        var bytes = ProjectJson.WriteUtf8(project);
        if (File.Exists(path))
        {
            if (File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
            {
                return;
            }

            this.Backup(path);
        }

        var temporary = path + ".tmp";
        File.WriteAllBytes(temporary, bytes);
        Replace(temporary, path);
    }

    private static string BaseName(string path) => Path.GetFileName(path)[..^Extension.Length];

    // Virus scanners and the search indexer briefly hold a file that was just written; Windows then refuses the move.
    private static void Replace(string source, string target)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                File.Move(source, target, overwrite: true);
                return;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException && ++attempt < 10)
            {
                Thread.Sleep(20 * attempt);
            }
        }
    }

    private void Backup(string path)
    {
        var folder = Path.Combine(Path.GetDirectoryName(path)!, BackupFolder);
        Directory.CreateDirectory(folder);
        var stamp = time.GetUtcNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        File.Copy(path, Path.Combine(folder, $"{BaseName(path)}-{stamp}{Extension}"), overwrite: true);

        var old = Directory.GetFiles(folder, $"{BaseName(path)}-*{Extension}").Order(StringComparer.Ordinal).ToList();
        foreach (var file in old.Take(Math.Max(0, old.Count - KeptBackups)))
        {
            File.Delete(file);
        }
    }
}
