// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

public sealed record OtisProject
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } = CurrentFormatVersion;

    public int PageResolution { get; init; } = 400;

    public required FontSettings Font { get; init; }

    public IReadOnlyList<string> Sources { get; init; } = [];

    public IReadOnlyDictionary<string, Cutout> Cutouts { get; init; } = new Dictionary<string, Cutout>(StringComparer.Ordinal);

    public DerivationRules Derivations { get; init; } = DerivationRules.Default;

    public IReadOnlyList<BuildRecord> Builds { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Unknown { get; init; }

    [JsonIgnore]
    public BuildRecord? LatestBuild => this.Builds.Count == 0 ? null : this.Builds.MaxBy(b => b.Version);

    public static OtisProject Create(string familyName) => new() { Font = new FontSettings { FamilyName = familyName } };

    public OtisProject WithCutout(string glyph, Cutout? cutout)
    {
        var cutouts = new Dictionary<string, Cutout>(this.Cutouts, StringComparer.Ordinal);
        if (cutout is null)
        {
            cutouts.Remove(glyph);
        }
        else
        {
            cutouts[glyph] = cutout;
        }

        return this with { Cutouts = cutouts };
    }
}
