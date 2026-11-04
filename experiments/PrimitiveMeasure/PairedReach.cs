namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Two closed occupied prefixes of one outward landmark run. The
/// first stop is their common origin. Each row names the tip at either dial
/// setting. The joint setting port belongs to the supplying structure.</summary>
public sealed class PairedReach
{
    private readonly EndpointRows tips = new();
    private readonly PresencePairPort settings;
    public LandmarkRun Rail { get; }

    public PairedReach(LandmarkRun rail, PresencePairPort settings,
        (int Resting, int Extended) left, (int Resting, int Extended) right)
    {
        if (!rail.IsChain) throw new ArgumentException("This gauge requires a supplied outward chain.", nameof(rail));
        Rail = rail;
        this.settings = settings;
        foreach (var tip in new[] { left, right })
        {
            if (Rail.Compare(tip.Resting, tip.Extended) is not (LandmarkRead.Before or LandmarkRead.SameSite))
                throw new ArgumentException("Each extended tip must coincide with or follow its resting stop.");
            tips.Append(rail.Bits, tip.Resting, tip.Extended);
        }
    }

    public byte[] TipBytes() => tips.PackedBytes(2, Rail.Bits);

    /// <summary>The output bits say whether each occupied prefix includes the
    /// selected stop. An unrelated rail needs its own correspondence first.</summary>
    public PresencePairPort At(LandmarkRun basis, int stop)
    {
        if (!ReferenceEquals(basis, Rail) || Rail.Compare(stop, stop) == LandmarkRead.Outside)
            return PresencePairPort.Bind(0);
        return Project((left, right) => Pair(Reaches(left, stop), Reaches(right, stop)));
    }

    /// <summary>Left reaches at least as far as right, and conversely.
    /// Both bits set means their tips agree in this projection.</summary>
    public PresencePairPort CompareTips() => Project((left, right) => Pair(Reaches(left, right), Reaches(right, left)));

    private bool Reaches(int tip, int stop) =>
        Rail.Compare(stop, tip) is LandmarkRead.Before or LandmarkRead.SameSite;

    private static int Pair(bool left, bool right) => (left ? 1 : 0) | (right ? 2 : 0);

    private PresencePairPort Project(Func<int, int, int> read)
    {
        var left = tips.Read(new(2, Rail.Bits, 0));
        var right = tips.Read(new(2, Rail.Bits, 1));
        int fields = 0;
        for (int state = 0; state < 4; state++)
            fields |= read(left[state & 1], right[state >> 1]) << (2 * state);
        return settings.Map((byte)fields);
    }
}
