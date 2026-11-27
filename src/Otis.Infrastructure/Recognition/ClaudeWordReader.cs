// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Otis.Core.Finding;
using Otis.Core.Imaging;
using Otis.Infrastructure.Pages;
using SkiaSharp;

namespace Otis.Infrastructure.Recognition;

public sealed class ClaudeWordReader(string apiKey) : IWordReader
{
    public const string Model = "claude-opus-5-5";

    private static readonly Dictionary<string, JsonElement> Schema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            words = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string" },
                        box = new { type = "array", items = new { type = "number" } },
                    },
                    required = new[] { "text", "box" },
                    additionalProperties = false,
                },
            },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "words" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    public async Task<IReadOnlyList<ReadWord>> ReadAsync(RgbImage page, CancellationToken cancellationToken)
    {
        using var client = new AnthropicClient { ApiKey = apiKey };
        var words = new List<ReadWord>();
        foreach (var strip in PageStrip.For(page.Width, page.Height))
        {
            words.AddRange(await ReadStripAsync(client, page, strip, cancellationToken).ConfigureAwait(false));
        }

        return ReadWords.WithoutRepeats(words);
    }

    private static string Instructions(PageStrip strip) =>
        $"""
        This image ({strip.ImageWidth} × {strip.ImageHeight} pixels) is part of a scanned lettering sample sheet,
        often blackletter (Fraktur) or calligraphy. List every word written on it, exactly as written,
        keeping capitals, umlauts and the long s (ſ). Give each word's box as [left, top, right, bottom]
        in pixels of this image, tight around the ink of the whole word. Leave out words cut off at the
        image edge and words you cannot read with confidence.
        """;

    private static async Task<IReadOnlyList<ReadWord>> ReadStripAsync(AnthropicClient client, RgbImage page, PageStrip strip, CancellationToken cancellationToken)
    {
        var parameters = new MessageCreateParams
        {
            Model = Model,
            MaxTokens = 16000,
            OutputConfig = new OutputConfig
            {
                Effort = Effort.High,
                Format = new JsonOutputFormat { Schema = Schema },
            },
            Messages =
            [
                new UserMessageParam
                {
                    Content = new List<ContentBlockParam>
                    {
                        new ImageBlockParam { Source = new Base64ImageSource { Data = Png(page, strip), MediaType = MediaType.ImagePng } },
                        new TextBlockParam { Text = Instructions(strip) },
                    },
                },
            ],
        };

        Message response;
        try
        {
            response = await client.Messages.Create(parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (AnthropicUnauthorizedException e)
        {
            throw new WordReadingException(WordReadingProblem.InvalidKey, e.Message, e);
        }
        catch (AnthropicForbiddenException e)
        {
            throw new WordReadingException(WordReadingProblem.NoAccess, e.Message, e);
        }
        catch (AnthropicRateLimitException e)
        {
            throw new WordReadingException(WordReadingProblem.TooManyRequests, e.Message, e);
        }
        catch (Anthropic5xxException e)
        {
            throw new WordReadingException(WordReadingProblem.ServiceBusy, e.Message, e);
        }
        catch (AnthropicIOException e)
        {
            throw new WordReadingException(WordReadingProblem.NoConnection, e.Message, e);
        }
        catch (AnthropicApiException e)
        {
            throw new WordReadingException(WordReadingProblem.Other, e.Message, e);
        }

        if (response.StopReason == "refusal")
        {
            throw new WordReadingException(WordReadingProblem.Declined, response.StopDetails?.Explanation ?? "refusal");
        }

        var text = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        try
        {
            return strip.ParseWords(text);
        }
        catch (JsonException e)
        {
            throw new WordReadingException(WordReadingProblem.Unreadable, e.Message, e);
        }
    }

    private static string Png(RgbImage page, PageStrip strip)
    {
        using var full = Bitmaps.ToBitmap(page.Crop(strip.Area));
        using var scaled = full.Resize(new SKImageInfo(strip.ImageWidth, strip.ImageHeight), new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var data = scaled.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(data.AsSpan());
    }
}
