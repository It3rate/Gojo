namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Ordered connections within the fixed eight-bit participation tile.
/// Endpoints are physical bit offsets, not object IDs. A link has no amount,
/// cached opening, or lifetime. Its reader selects observation or material use.
/// Moving/repacking a table requires explicit endpoint rebinding.</summary>
public static class LocalConnection
{
    // Low to high: source (3 bits), target (3 bits).
    public static byte Link(int source, int target)
    {
        CheckSite(source); CheckSite(target);
        return (byte)(source | (target << 3));
    }

    public static int Source(byte link) { CheckLink(link); return link & 7; }
    public static int Target(byte link) { CheckLink(link); return link >> 3; }

    // An explicitly grouped pair, first link then second link. The material
    // reader requires positive then negative, with common source/target columns.
    public static ushort Pair(byte first, byte second)
    {
        CheckLink(first); CheckLink(second);
        return (ushort)(first | (second << 6));
    }

    public static byte At(ushort pair, int index)
    {
        if (pair > 0xFFF) throw new ArgumentOutOfRangeException(nameof(pair));
        if (index is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(index));
        return (byte)((pair >> (6 * index)) & 63);
    }

    // Ten bits: material link (6), control-site offset (3), matching face (1).
    // The selected control bit is observed; material still enters at link.Source.
    public static ushort When(byte link, int controlSite, bool matchOne)
    {
        CheckLink(link); CheckSite(controlSite);
        return (ushort)(link | (controlSite << 6) | (matchOne ? 0x200 : 0));
    }

    public static byte MaterialLink(ushort conditional)
    {
        CheckConditional(conditional);
        return (byte)(conditional & 63);
    }

    public static int ControlSite(ushort conditional)
    {
        CheckConditional(conditional);
        return (conditional >> 6) & 7;
    }

    public static bool MatchingFace(ushort conditional)
    {
        CheckConditional(conditional);
        return (conditional & 0x200) != 0;
    }

    public static bool IsOpen(byte tile, ushort conditional) =>
        ((tile & (1 << ControlSite(conditional))) != 0) == MatchingFace(conditional);

    private static void CheckSite(int site)
    {
        if (site is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(site));
    }

    private static void CheckLink(byte link)
    {
        if (link > 63) throw new ArgumentOutOfRangeException(nameof(link));
    }

    private static void CheckConditional(ushort conditional)
    {
        if (conditional > 0x3FF) throw new ArgumentOutOfRangeException(nameof(conditional));
    }
}
