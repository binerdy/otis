// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

[JsonConverter(typeof(JsonStringEnumConverter<SmoothingLevel>))]
public enum SmoothingLevel
{
    Off,
    Light,
    Medium,
    Strong,
}
