using System.Text;
using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Relation-shaped text surface for encoding names, debug labels, and source
/// words as integer Gojo structure. The canonical surface stores Unicode scalar
/// values; UTF-8 integer helpers are provided for compact binary-oriented
/// packet layers.
/// </summary>
public static class EncodedTextSurface
{
    public static Relation Create(Carrier root, string? text)
    {
        var codePointAxis = new Carrier();
        var elements = (text ?? string.Empty)
            .EnumerateRunes()
            .Select(rune => (IElement)Relation.Point(codePointAxis, rune.Value))
            .ToArray();

        return Relation.OrderedGroup(RootOrNew(root), elements);
    }

    public static bool TryRead(IElement element, out string text)
    {
        if (element is not Relation textSurface ||
            !TryReadRootElements(textSurface, out var elements))
        {
            text = string.Empty;
            return false;
        }

        var builder = new StringBuilder();
        foreach (var item in elements)
        {
            if (!TryReadPoint(item, out var location) ||
                location.Ticks < 0 ||
                location.Ticks > int.MaxValue ||
                !Rune.TryCreate((int)location.Ticks, out var rune))
            {
                text = string.Empty;
                return false;
            }

            builder.Append(rune.ToString());
        }

        text = builder.ToString();
        return true;
    }

    public static long[] ToUtf8Integers(string? text) =>
        [.. System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty).Select(value => (long)value)];

    public static bool TryReadUtf8Integers(IReadOnlyList<long> values, out string text)
    {
        if (values.Any(value => value < byte.MinValue || value > byte.MaxValue))
        {
            text = string.Empty;
            return false;
        }

        var bytes = values.Select(value => (byte)value).ToArray();
        text = System.Text.Encoding.UTF8.GetString(bytes);
        return true;
    }

    internal static Relation UnknownPoint() => Relation.Point(Carrier.Unknown, 0);

    internal static Carrier RootOrNew(Carrier carrier) =>
        carrier is null || carrier.IsUnknown ? new Carrier() : carrier;

    internal static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    internal static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    internal static bool IsUnknownPoint(IElement element) =>
        element is Relation relation &&
        relation.Left is CarrierLocation location &&
        relation.Right is Carrier carrier &&
        carrier.IsUnknown &&
        location.Carrier.IsUnknown &&
        location.Ticks == 0;
}
