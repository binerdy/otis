// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

public sealed class FontVersionConverter : JsonConverter<FontVersion>
{
    public override FontVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        FontVersion.TryParse(reader.GetString(), out var version) ? version : throw new JsonException("Expected a font version like \"1.004\".");

    public override void Write(Utf8JsonWriter writer, FontVersion value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
