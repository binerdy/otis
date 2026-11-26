// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    IndentSize = 2,
    NewLine = "\n",
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OtisProject))]
internal sealed partial class ProjectJsonContext : JsonSerializerContext;
