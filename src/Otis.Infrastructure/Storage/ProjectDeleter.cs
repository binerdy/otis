// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Infrastructure.Storage;

public sealed class ProjectDeleter(IFileRemover remover)
{
    // Only files Otis made are listed: the project, its backups and, on request, the fonts named in its build history.
    public static IReadOnlyList<string> FilesOf(string projectPath, OtisProject? project, bool includeFonts)
    {
        var folder = Path.GetDirectoryName(projectPath)!;
        var name = Path.GetFileName(projectPath)[..^ProjectStore.Extension.Length];
        var files = new List<string> { projectPath };

        var backups = Path.Combine(folder, ProjectStore.BackupFolder);
        if (Directory.Exists(backups))
        {
            files.AddRange(Directory.GetFiles(backups, $"{name}-*{ProjectStore.Extension}"));
        }

        if (includeFonts && project is not null)
        {
            var fonts = Path.GetFullPath(Path.Combine(folder, project.Font.OutputFolder));
            foreach (var build in project.Builds)
            {
                var font = Path.Combine(fonts, build.FileName);
                files.Add(font);
                files.Add(Path.ChangeExtension(font, ".png"));
            }
        }

        var sources = project?.Sources.Select(s => ProjectStore.SourcePath(projectPath, s)).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        return [.. files.Where(File.Exists).Where(f => !sources.Contains(Path.GetFullPath(f))).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    public void Delete(string projectPath, bool includeFonts)
    {
        var project = File.Exists(projectPath) ? ProjectStore.Load(projectPath).Project : null;
        remover.Remove(FilesOf(projectPath, project, includeFonts));

        var backups = Path.Combine(Path.GetDirectoryName(projectPath)!, ProjectStore.BackupFolder);
        if (Directory.Exists(backups) && !Directory.EnumerateFileSystemEntries(backups).Any())
        {
            Directory.Delete(backups);
        }
    }
}
