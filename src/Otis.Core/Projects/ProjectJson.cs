// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text;
using System.Text.Json;

namespace Otis.Core.Projects;

public static class ProjectJson
{
    public static LoadedProject Read(string json)
    {
        var project = JsonSerializer.Deserialize(json, ProjectJsonContext.Default.OtisProject)
            ?? throw new JsonException("The project file is empty.");
        project = project with { Cutouts = new Dictionary<string, Cutout>(project.Cutouts, StringComparer.Ordinal) };
        return new LoadedProject(project, project.FormatVersion > OtisProject.CurrentFormatVersion);
    }

    public static string Write(OtisProject project)
    {
        var ordered = project with
        {
            Cutouts = project.Cutouts.OrderBy(c => c.Key, StringComparer.Ordinal).ToDictionary(StringComparer.Ordinal),
            Builds = [.. project.Builds.OrderBy(b => b.Version)],
        };
        return JsonSerializer.Serialize(ordered, ProjectJsonContext.Default.OtisProject) + "\n";
    }

    public static byte[] WriteUtf8(OtisProject project) => Encoding.UTF8.GetBytes(Write(project));
}
