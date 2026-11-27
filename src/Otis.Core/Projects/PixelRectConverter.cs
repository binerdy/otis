// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

public sealed class PixelRectConverter : JsonConverter<PixelRect>
{
    public override PixelRect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var v = IntArray.Read(ref reader, 4);
        return new PixelRect(v[0], v[1], v[2], v[3]);
    }

    public override void Write(Utf8JsonWriter writer, PixelRect value, JsonSerializerOptions options) =>
        writer.WriteRawValue(
            string.Create(CultureInfo.InvariantCulture, $"[{value.Left}, {value.Top}, {value.Right}, {value.Bottom}]"),
            skipInputValidation: true);
}
