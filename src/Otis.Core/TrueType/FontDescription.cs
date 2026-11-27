// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.TrueType;

public sealed record FontDescription
{
    public const string SubfamilyName = "Regular";

    public required string FamilyName { get; init; }

    public required string PostScriptName { get; init; }

    public required FontVersion Version { get; init; }

    public required DateTimeOffset Created { get; init; }

    public string Designer { get; init; } = string.Empty;

    public int UnitsPerEm { get; init; } = 1000;

    public int XHeight { get; init; } = 400;

    public int CapHeight { get; init; } = 720;

    public string FullName => this.FamilyName;

    public string VersionString => "Version " + this.Version;

    // Otis reads this tag back to recognise its own installed fonts and their version.
    public string UniqueId => $"{this.Version};OTIS;{this.PostScriptName}";
}
