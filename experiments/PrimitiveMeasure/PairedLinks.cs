namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Two possible ordered links in one owner's site basis. Bit i
/// selects row i within each complete candidate published by the pair port.</summary>
public sealed class PairedLinks
{
    private readonly EndpointRows links = new();
    private readonly int siteBits;
    private readonly PresencePairPort participating;

    public PairedLinks(int siteBits, (int From, int To) first, (int From, int To) second,
        PresencePairPort participating)
    {
        this.siteBits = siteBits;
        this.participating = participating;
        links.Append(siteBits, first.From, first.To);
        links.Append(siteBits, second.From, second.To);
    }

    public byte[] LinkBytes() => links.PackedBytes(2, siteBits);

    // Read either one ordered pair or all outgoing links from this site.
    // Both roles remain available together to the recipient.
    public PresencePairPort From(int site, int? to = null)
    {
        int roles = 0;
        for (int i = 0; i < 2; i++)
        {
            var link = links.Read(new(2, siteBits, i));
            if (link[0] == site && (to is null || link[1] == to)) roles |= 1 << i;
        }
        int fields = 0;
        for (int state = 0; state < 4; state++) fields |= (state & roles) << (2 * state);
        return participating.Map((byte)fields);
    }
}
