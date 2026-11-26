// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otis.Infrastructure.Storage;

public sealed partial class SettingsStore(string dataDirectory)
{
    private string FilePath => Path.Combine(dataDirectory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            return File.Exists(this.FilePath)
                ? JsonSerializer.Deserialize(File.ReadAllText(this.FilePath), SettingsJson.Default.AppSettings) ?? new AppSettings()
                : new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(this.FilePath, JsonSerializer.Serialize(settings, SettingsJson.Default.AppSettings));
    }

    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(AppSettings))]
    private sealed partial class SettingsJson : JsonSerializerContext;
}
