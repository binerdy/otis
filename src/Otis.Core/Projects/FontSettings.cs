// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

public sealed record FontSettings
{
    public required string FamilyName { get; init; }

    public FontVersion Version { get; init; } = FontVersion.First;

    public string Designer { get; init; } = string.Empty;

    public int UnitsPerEm { get; init; } = 1000;

    public int XHeight { get; init; } = 400;

    public int CapHeight { get; init; } = 720;

    public int Sidebearing { get; init; } = 16;

    public int CapitalRightSidebearing { get; init; } = -10;

    public int WordSpace { get; init; } = 340;

    public string OutputFolder { get; init; } = "fonts";

    public string SampleText { get; init; } = "Hamburgefonstiv";

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Unknown { get; init; }

    [JsonIgnore]
    public string PostScriptName => string.Concat(this.FamilyName.Where(char.IsAsciiLetterOrDigit)) + "-Regular";
}
