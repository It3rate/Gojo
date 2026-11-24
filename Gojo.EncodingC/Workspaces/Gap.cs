namespace Gojo.EncodingC;

/// <summary>Where a workspace and its source disagree, bit by bit, as a surface a
/// law can read by reference. It has the workspace's positions and planes; a bit is
/// set where the workspace holds a value and the source now reads another.
/// <para>What the gap means depends on what the workspace holds. A workspace that ran a
/// law ahead of its source is a prediction, and the gap after the source
/// moves is how the prediction missed. A workspace with a wanted state written
/// into it is a goal, and the gap is what is left to do; it is empty when the
/// source has arrived. Both are the same read, so the same structure can use
/// a miss to trust a predictor less and a distance to keep a valve open.</para>
/// <para>The gap is never stored and never written. It is a difference that
/// exists while both sides do, in the same medium as what it compares. An
/// introduced position has no source to differ from and reads clear.</para>
/// <para><b>What it is, and what it is not.</b> The gap is a per-bit
/// disagreement: it says which bits differ, and nothing else. For a unary
/// count the bits it sets are the units between the two counts as a set, not
/// as a count: 1 (<c>0001</c>) against 3 (<c>0111</c>) reads <c>0110</c>, which is
/// not a unary prefix, so a law reading it as a count finds it unread. And it
/// has no direction: one unit too few and one too many give the same gap.
/// It serves well as a detector of a miss, bit by bit; a law that must respond
/// to how far off and which way still has to read both sides
/// (<c>FlowIsSteeredTowardAGoalByTheGapItLeaves</c> does).</para>
/// <para><b>A direction to take later.</b> An error that can drive laws
/// directly would have two faces, as two-way traffic does: a deficit (too
/// low) and an excess (too high), each a unary count, never merged into one
/// signed value and never both present. A response law would then read one
/// operand: a deficit opens the way in, an excess the way out. The likely
/// form is a relation over (have, want, deficit, excess) built once, which can
/// leave both open while the inputs are; cheaper live views (want and not
/// have, have and not want) would carry the direction but, like this gap, a
/// set of units rather than a count. Other per-bit reads at this level may
/// need the same treatment when they come to drive responses.</para></summary>
public sealed class Gap : Surface
{
    private readonly Workspace workspace;

    internal Gap(Workspace workspace) => this.workspace = workspace;

    public override int Count => workspace.Count;

    public override int PlaneCount => workspace.PlaneCount;

    public override bool IsLive(int index) => workspace.IsLive(index);

    public override bool Read(int plane, int index) => workspace.Misses(plane, index);

    internal override bool ThroughPort => false;

    internal override Store? Home => workspace.Home;

    /// <summary>A gap is a read of a workspace, so it is supposed: a relation
    /// reading it narrows only what is supposed, never what is known.</summary>
    internal override Surface Underlying => workspace;

    internal override bool ReadOnly => true;

    internal override bool IsConstant(int plane) => false;

    internal override void Set(int plane, int index, bool value) =>
        throw new InvalidOperationException("A gap is a read of a workspace and its source. Change one of them.");

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => (this, index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys) => workspace.ReadKeys(index, keys);

    internal override (Surface, int) WriteKey(int index) => (this, index);
}
