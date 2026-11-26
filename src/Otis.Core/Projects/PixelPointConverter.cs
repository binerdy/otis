// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

public sealed class PixelPointConverter : JsonConverter<PixelPoint>
{
    public override PixelPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = IntArray.Read(ref reader, 2);
        return new PixelPoint(values[0], values[1]);
    }

    // Raw values keep each point on one line in the indented project file, which keeps diffs small.
    public override void Write(Utf8JsonWriter writer, PixelPoint value, JsonSerializerOptions options) =>
        writer.WriteRawValue(string.Create(CultureInfo.InvariantCulture, $"[{value.X}, {value.Y}]"), skipInputValidation: true);
}
