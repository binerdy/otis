// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using Otis.Core.Finding;
using Otis.Core.Projects;

namespace Otis.Infrastructure.Recognition;

// A band of the page, scaled so its long side fits what the model sees without being shrunk again.
public sealed record PageStrip(PixelRect Area, double Scale)
{
    public const int LongestSide = 1568;
    private const int MostStrips = 3;
    private const double Overlap = 0.08;

    public int ImageWidth => Math.Max(1, (int)Math.Round(this.Area.Width * this.Scale));

    public int ImageHeight => Math.Max(1, (int)Math.Round(this.Area.Height * this.Scale));

    public static IReadOnlyList<PageStrip> For(int pageWidth, int pageHeight)
    {
        var count = Math.Clamp((int)Math.Ceiling((double)Math.Max(pageWidth, pageHeight) / LongestSide), 1, MostStrips);
        var overlap = count == 1 ? 0 : (int)(pageHeight * Overlap);
        var strips = new List<PageStrip>();
        for (var i = 0; i < count; i++)
        {
            var top = Math.Max(0, (pageHeight * i / count) - overlap);
            var bottom = Math.Min(pageHeight, (pageHeight * (i + 1) / count) + overlap);
            var area = new PixelRect(0, top, pageWidth, bottom);
            var scale = Math.Min(1.0, (double)LongestSide / Math.Max(area.Width, area.Height));
            strips.Add(new PageStrip(area, scale));
        }

        return strips;
    }

    public IReadOnlyList<ReadWord> ParseWords(string json)
    {
        using var document = JsonDocument.Parse(json);
        var words = new List<ReadWord>();
        if (!document.RootElement.TryGetProperty("words", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return words;
        }

        foreach (var item in list.EnumerateArray())
        {
            if (item.TryGetProperty("text", out var text) && text.GetString() is { Length: > 0 } value
                && item.TryGetProperty("box", out var box) && box.ValueKind == JsonValueKind.Array && box.GetArrayLength() == 4
                && this.ToPage(box) is { } rect)
            {
                words.Add(new ReadWord(value.Trim(), rect));
            }
        }

        return words;
    }

    private PixelRect? ToPage(JsonElement box)
    {
        var v = box.EnumerateArray().Select(e => e.TryGetDouble(out var d) ? d : double.NaN).ToArray();
        if (v.Any(double.IsNaN))
        {
            return null;
        }

        int X(double x) => this.Area.Left + (int)Math.Round(Math.Clamp(x, 0, this.ImageWidth) / this.Scale);
        int Y(double y) => this.Area.Top + (int)Math.Round(Math.Clamp(y, 0, this.ImageHeight) / this.Scale);
        var rect = new PixelRect(X(Math.Min(v[0], v[2])), Y(Math.Min(v[1], v[3])), X(Math.Max(v[0], v[2])), Y(Math.Max(v[1], v[3])));
        return rect.Width < 2 || rect.Height < 2 ? null : rect;
    }
}
