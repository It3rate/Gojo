using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace Gojo.Experiments.QuickDraw;

/// <summary>Exact finite JSON number. Lexeme is retained separately from its reduced
/// rational read; binary floating point never participates in acquisition.</summary>
public sealed record ExactNumber(string Lexeme, BigInteger Numerator, BigInteger Denominator)
{
    public static ExactNumber Parse(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number) throw new InvalidDataException("A coordinate or time must be a JSON number.");
        var lexeme = value.GetRawText();
        var parts = lexeme.Split(['e', 'E']);
        var mantissa = parts[0];
        var exponent = parts.Length == 1 ? 0 : int.Parse(parts[1], CultureInfo.InvariantCulture);
        var point = mantissa.IndexOf('.');
        var decimals = point < 0 ? 0 : mantissa.Length - point - 1;
        var scale = checked(decimals - exponent);
        // A declared resource bound, not a numerical rounding rule.
        if (Math.Abs((long)scale) > 1000 || mantissa.Length > 1000)
            throw new InvalidDataException("The numeric token exceeds this pilot's exact parsing capacity (1,000 digits/exponent places).");
        var numerator = BigInteger.Parse(mantissa.Replace(".", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
        var denominator = scale > 0 ? BigInteger.Pow(10, scale) : BigInteger.One;
        if (scale < 0) numerator *= BigInteger.Pow(10, -scale);
        var common = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        return new(lexeme, numerator / common, denominator / common);
    }

    public int CompareTo(ExactNumber other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
}

/// <summary>These records are the input adapter, not the native meaning of a sketch.</summary>
public sealed record QuickDrawSample(ExactNumber X, ExactNumber Y, ExactNumber? Time);
public sealed record QuickDrawStroke(IReadOnlyList<QuickDrawSample> Samples);
public sealed record QuickDrawRecord(string KeyId, string? Word, bool? Recognized,
    string OriginalJson, IReadOnlyList<QuickDrawStroke> Strokes, bool HasTiming)
{
    public static QuickDrawRecord Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("A Quick Draw record must be an object.");
        if (root.EnumerateObject().GroupBy(p => p.Name, StringComparer.Ordinal).Any(g => g.Count() != 1))
            throw new InvalidDataException("Duplicate record properties are ambiguous.");
        if (!root.TryGetProperty("key_id", out var key) || key.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(key.GetString()))
            throw new InvalidDataException("key_id must be a nonempty string; it is never converted to a floating-point number.");
        if (!root.TryGetProperty("drawing", out var drawing) || drawing.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("drawing must be an array of strokes.");
        string? word = null;
        if (root.TryGetProperty("word", out var wordElement))
        {
            if (wordElement.ValueKind != JsonValueKind.String) throw new InvalidDataException("word must be a string when supplied.");
            word = wordElement.GetString();
        }
        bool? recognized = null;
        if (root.TryGetProperty("recognized", out var recognizedElement))
        {
            if (recognizedElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidDataException("recognized must be boolean when supplied.");
            recognized = recognizedElement.GetBoolean();
        }
        var strokes = new List<QuickDrawStroke>();
        int? dimensions = null;
        foreach (var stroke in drawing.EnumerateArray())
        {
            if (stroke.ValueKind != JsonValueKind.Array || stroke.GetArrayLength() is not (2 or 3))
                throw new InvalidDataException("Each stroke must contain x/y or x/y/t arrays.");
            var arity = stroke.GetArrayLength();
            if (dimensions.HasValue && dimensions != arity) throw new InvalidDataException("Mixed raw and simplified strokes are not a supported record format.");
            dimensions = arity;
            var axes = stroke.EnumerateArray().ToArray();
            if (axes.Any(axis => axis.ValueKind != JsonValueKind.Array) || axes.Any(axis => axis.GetArrayLength() != axes[0].GetArrayLength()))
                throw new InvalidDataException("Stroke coordinate and timing arrays must have identical lengths.");
            var values = axes.Select(axis => axis.EnumerateArray().Select(ExactNumber.Parse).ToArray()).ToArray();
            var samples = Enumerable.Range(0, values[0].Length).Select(i =>
                new QuickDrawSample(values[0][i], values[1][i], arity == 3 ? values[2][i] : null)).ToArray();
            // Irregular/negative/nonmonotonic recorded times remain available;
            // acquisition order is separate and must not be repaired by sorting.
            strokes.Add(new(Array.AsReadOnly(samples)));
        }
        return new(key.GetString()!, word, recognized, json, strokes.AsReadOnly(), dimensions == 3);
    }

    public static IEnumerable<(int Line, QuickDrawRecord Record)> ReadNdjson(TextReader reader)
    {
        string? line; var number = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            number++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            QuickDrawRecord record;
            try { record = Parse(line); }
            catch (Exception error) when (error is JsonException or InvalidDataException or FormatException or OverflowException)
            { throw new InvalidDataException($"Invalid Quick Draw record on line {number}: {error.Message}", error); }
            yield return (number, record);
        }
    }
}
