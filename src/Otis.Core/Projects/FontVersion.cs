// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

[JsonConverter(typeof(FontVersionConverter))]
public readonly record struct FontVersion(int Major, int Minor) : IComparable<FontVersion>
{
    public static FontVersion First { get; } = new(1, 0);

    public static bool operator <(FontVersion left, FontVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(FontVersion left, FontVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(FontVersion left, FontVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(FontVersion left, FontVersion right) => left.CompareTo(right) >= 0;

    public static bool TryParse(string? text, out FontVersion version)
    {
        version = default;
        var parts = text?.Trim().Split('.');
        if (parts is not [var major, var minor] || minor.Length != 3
            || !int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out var ma)
            || !int.TryParse(minor, NumberStyles.None, CultureInfo.InvariantCulture, out var mi)
            || ma is < 1 or > 999)
        {
            return false;
        }

        version = new FontVersion(ma, mi);
        return true;
    }

    public static FontVersion Parse(string text) =>
        TryParse(text, out var version) ? version : throw new FormatException($"'{text}' is not a font version like 1.004.");

    public FontVersion Next() => this.Minor == 999 ? new FontVersion(this.Major + 1, 0) : this with { Minor = this.Minor + 1 };

    public int CompareTo(FontVersion other) => (this.Major, this.Minor).CompareTo((other.Major, other.Minor));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{this.Major}.{this.Minor:000}");
}
