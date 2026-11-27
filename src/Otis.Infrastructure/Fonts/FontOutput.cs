// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using Otis.Core.Building;
using Otis.Core.Projects;

namespace Otis.Infrastructure.Fonts;

public static class FontOutput
{
    public static string Folder(string projectPath, OtisProject project) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, project.Font.OutputFolder));

    public static string FontPath(string projectPath, OtisProject project, BuildRecord build) =>
        Path.Combine(Folder(projectPath, project), build.FileName);

    public static string SpecimenPath(string projectPath, OtisProject project, BuildRecord build) =>
        Path.ChangeExtension(FontPath(projectPath, project, build), ".png");

    // Earlier versions stay on disk; only a permitted rebuild of the same version replaces its files.
    public static BuildRecord Write(string projectPath, OtisProject project, FontBuild build, DateTimeOffset builtAt)
    {
        var folder = Folder(projectPath, project);
        Directory.CreateDirectory(folder);
        var fileName = build.FileName(project.Font);
        var fontPath = Path.Combine(folder, fileName);

        File.WriteAllBytes(fontPath, build.FontFile.Span);
        File.WriteAllBytes(Path.ChangeExtension(fontPath, ".png"), SpecimenRenderer.Render(build.FontFile.Span, project.Font.SampleText));

        return new BuildRecord
        {
            Version = build.Version,
            BuiltAt = builtAt,
            FileName = fileName,
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(build.FontFile.Span)),
            CutOut = build.Coverage.CutOut,
            Derived = build.Coverage.Derived,
            Missing = build.Coverage.Missing,
        };
    }
}
