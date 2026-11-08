using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Four exposed extents projected through explicit affine maps into
/// one shared-reach read. Projection observes; no carrier state is imported.
/// A coarse tick can therefore contribute several fine reaches without
/// changing its stable unit or pretending its sample has a different unit.</summary>
public sealed class CalibratedShapePublication
{
    // surface = (template -> common read basis) -> Closed(port -> calibration,...)
    // Ordered slots: upper width, lower width, upper height, lower height.
    // witness retains actual port -> captured source sample -> affine projection,
    // followed by the role-bound native drawing and its reach correspondence.
    private readonly CoupledShapeTemplate.Template template;
    private readonly Carrier basis;
    public Relation Surface { get; }
    public sealed record Read(bool Complete, CoupledShapeTemplate.Realization? Picture, Relation Witness);

    public CalibratedShapePublication(CoupledShapeTemplate.Template template, Carrier basis, params Relation[] bindings)
    {
        if (bindings.Length != 4) throw new ArgumentException("Four ordered extent bindings are required.", nameof(bindings));
        this.template = template; this.basis = basis;
        Surface = new Relation(new Relation(template.Surface, basis), StructuralConstraint.Closed(bindings));
    }

    public static Relation Bind(Relation port, Carrier target, long reachesPerTick)
    {
        if (reachesPerTick <= 0 || port is not { Left: CarrierLocation at, Right: Carrier owner } ||
            !ReferenceEquals(at.Carrier, owner) || !at.SameTickRefAs(owner.ValueSlot))
            throw new ArgumentException("A positive read calibration needs an actual exposed value port.");
        return new(port, new Relation(Relation.Segment(owner, 0, 1), Relation.Segment(target, 0, reachesPerTick)));
    }

    public Read Observe()
    {
        var evidence = new List<IElement>(); var projected = new List<CarrierLocation>();
        foreach (var item in FiniteLaw.Entries((Relation)Surface.Right))
        {
            if (item is not Relation { Left: Relation port, Right: Relation map } ||
                port is not { Left: CarrierLocation live, Right: Carrier owner } ||
                !ReferenceEquals(live.Carrier, owner) || !live.SameTickRefAs(owner.ValueSlot))
                return new(false, null, StructuralConstraint.Open(Surface, StructuralConstraint.Closed(evidence.ToArray())));
            var sample = new CarrierLocation(owner, live.Ticks);
            var projection = map.Project(sample, basis);
            evidence.Add(new Relation(new Relation(port, sample), projection));
            if (!projection.TryReadProjection(out var value, out var residue) ||
                !ProjectionResidue.TryRead(residue, out _, out var numerator, out _) || numerator != 0 ||
                !ReferenceEquals(value.Carrier, basis) || value.Ticks is < 0 or > 64)
                return new(false, null, StructuralConstraint.Open(Surface, StructuralConstraint.Closed(evidence.ToArray())));
            projected.Add(new CarrierLocation(basis, value.Ticks));
        }
        var picture = CoupledShapeTemplate.Realize(template, checked((int)projected[0].Ticks), checked((int)projected[1].Ticks),
            checked((int)projected[2].Ticks), checked((int)projected[3].Ticks), StructuralConstraint.Closed(projected.Cast<IElement>().ToArray()));
        return new(true, picture, new Relation(Surface, StructuralConstraint.Closed(StructuralConstraint.Closed(evidence.ToArray()), picture.Surface)));
    }
}
