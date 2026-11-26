// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Finding;
using Otis.Core.Imaging;
using Otis.Core.Projects;
using Otis.Infrastructure.Pages;
using SkiaSharp;
using TesseractOCR;
using TesseractOCR.Enums;

namespace Otis.Infrastructure.Recognition;

public sealed class TesseractRecognizer(string tessdataFolder, string model) : ILetterRecognizer, IDisposable
{
    public const string BundledModel = "deu_latf";

    // Half resolution brings 400 dpi scans to the size Tesseract reads best, four times faster.
    private const int Reduction = 2;
    private readonly Lock gate = new();
    private Engine? engine;

    public string Model { get; } = model;

    // A model placed in Otis.Data\tessdata, such as frak2021, wins over the bundled one.
    public static TesseractRecognizer ForApp(string dataDirectory)
    {
        var own = Path.Combine(dataDirectory, "tessdata");
        var added = Directory.Exists(own) ? Directory.GetFiles(own, "*.traineddata").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault() : null;
        return added is null
            ? new TesseractRecognizer(Path.Combine(AppContext.BaseDirectory, "tessdata"), BundledModel)
            : new TesseractRecognizer(own, Path.GetFileNameWithoutExtension(added));
    }

    public IReadOnlyList<RecognizedLetter> Recognize(RgbImage page, CancellationToken cancellationToken)
    {
        var small = page.Shrink(Reduction);
        using var bitmap = Bitmaps.ToBitmap(small);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var image = TesseractOCR.Pix.Image.LoadFromMemory(data.ToArray());
        var letters = new List<RecognizedLetter>();
        lock (this.gate)
        {
            this.engine ??= new Engine(tessdataFolder, this.Model, EngineMode.LstmOnly);
            using var result = this.engine.Process(image, PageSegMode.Auto);
            foreach (var block in result.Layout)
            {
                foreach (var paragraph in block.Paragraphs)
                {
                    foreach (var line in paragraph.TextLines)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (var word in line.Words)
                        {
                            var wordConfidence = word.Confidence;
                            foreach (var symbol in word.Symbols)
                            {
                                if (symbol.BoundingBox is { } box && !string.IsNullOrWhiteSpace(symbol.Text))
                                {
                                    var rect = new PixelRect(box.X1 * Reduction, box.Y1 * Reduction, box.X2 * Reduction, box.Y2 * Reduction);
                                    letters.Add(new RecognizedLetter(symbol.Text.Trim(), rect, symbol.Confidence, wordConfidence));
                                }
                            }
                        }
                    }
                }
            }
        }

        return letters;
    }

    public void Dispose() => this.engine?.Dispose();
}
