using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.PrimitiveRuntime;

/// <summary>
/// Optional authored metric correspondence. Unequal gaps intentionally prevent
/// interpreting ordinal addresses as magnitudes. Projection is observation.
/// This adapter uses the existing exact numeric kernel, outside the hot loop.
/// </summary>
public sealed class MotionCalibration
{
    private readonly Carrier axis = new();
    private readonly long[] positions = [0, 3, 8, 12];
    private readonly MotionDefinition definition;
    public Relation Surface { get; }
    public IReadOnlyList<long> Positions => Array.AsReadOnly(positions);

    public MotionCalibration(MotionDefinition definition)
    {
        this.definition = definition;
        Surface = StructuralConstraint.Closed(definition.Landmarks.Select((site, index) =>
            (IElement)new Relation(site, Relation.Point(axis, positions[index]))).ToArray());
    }

    public MetricObservation Observe(int landmark, bool reversed, int quantum)
    {
        if (landmark < 0 || landmark >= positions.Length) throw new ArgumentOutOfRangeException(nameof(landmark));
        if (quantum <= 0) throw new ArgumentOutOfRangeException(nameof(quantum));
        var target = new Carrier(UnitShape.ResolutionOf(axis.UnitShape, quantum));
        // Reversal is a separate observer basis. Signed positions and carrier
        // faces are not changed. Both output frames have positive progression.
        var sourceFrame = reversed ? Relation.Segment(axis, 12, 0) : Relation.Segment(axis, 0, 12);
        // Use a quantum-sized source frame so arbitrary positive quantum keeps
        // an exact scale, including those that do not divide the whole span.
        var quantumFrame = reversed ? Relation.Segment(axis, 12, checked(12L - quantum))
            : Relation.Segment(axis, 0, quantum);
        var projection = new Relation(quantumFrame, Relation.Segment(target, 0, 1));
        var source = new CarrierLocation(axis, positions[landmark]);
        if (!projection.TryProject(source, target, out var read) ||
            !read.TryReadProjection(out var value, out var residue) ||
            !ProjectionResidue.TryRead(residue, out _, out var numerator, out var denominator))
            throw new InvalidOperationException("Selected calibrated frame must admit its landmark.");
        var selected = new Relation(definition.Landmarks[landmark], source);
        var correspondence = new Relation(new Relation(Surface, sourceFrame),
            new Relation(new Relation(selected, projection), read));
        return new MetricObservation(reversed ? 12 - positions[landmark] : positions[landmark],
            value.Ticks, numerator, denominator, correspondence);
    }
}

public sealed record MetricObservation(long Ticks, long CoarseTicks, long ResidueNumerator,
    long ResidueDenominator, Relation Witness);
