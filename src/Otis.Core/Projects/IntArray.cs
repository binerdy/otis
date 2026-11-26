// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;

namespace Otis.Core.Projects;

internal static class IntArray
{
    public static int[] Read(ref Utf8JsonReader reader, int length)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected an array of {length} integers.");
        }

        var values = new int[length];
        for (var i = 0; i < length; i++)
        {
            reader.Read();
            values[i] = reader.GetInt32();
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException($"Expected an array of {length} integers.");
        }

        return values;
    }
}
