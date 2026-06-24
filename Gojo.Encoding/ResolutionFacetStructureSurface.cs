using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding;

/// <summary>
/// Native relation footholds for the resolution facet samples. These helpers
/// keep compact text such as Part(...) and Approx(...) as debug reads over
/// ordinary Gojo structure instead of making those strings the body itself.
/// </summary>
public static class ResolutionFacetStructureSurface
{
    public readonly record struct PartitionRead(
        IElement Source,
        IElement Count,
        IElement[] Sites,
        IElement ResidueOrTension);

    public readonly record struct UnitComparisonRead(
        IElement Source,
        IElement Unit,
        IElement ResidueOrTension);

    public readonly record struct UnitResolutionRead(
        IElement Comparison,
        IElement Witness,
        IElement ResidueOrTension);

    public readonly record struct ComparisonRead(
        IElement Source,
        IElement Target,
        IElement? Resolution,
        IElement ResidueOrTension);

    public readonly record struct ResolutionRungRead(
        string Label,
        CarrierLocation Location);

    public readonly record struct ObserverBasisRead(
        IElement Source,
        IElement ResidueOrTension);

    public readonly record struct ObserverResolutionRead(
        IElement Source,
        IElement Basis,
        IElement Resolution,
        IElement ResidueOrTension);

    public readonly record struct SlotRead(
        IElement Role,
        Placeholder Slot);

    public readonly record struct TargetRead(
        Placeholder Slot,
        IElement ResidueOrTension);

    public readonly record struct ApproximationRead(
        IElement Comparison,
        IElement Resolution,
        IElement ResidueOrTension);

    public readonly record struct AmbiguousBasisRead(
        IElement Source,
        Placeholder BasisSlot,
        IElement ResidueOrTension);

    public readonly record struct RungRead(
        IElement Source,
        IElement Resolution,
        IElement ResidueOrTension);

    public readonly record struct CorrespondenceRead(
        IElement Source,
        IElement Target);

    public readonly record struct LadderResolutionRead(
        IElement RoughRung,
        IElement FineRung,
        IElement Correspondence,
        IElement ResidueOrTension);

    public readonly record struct RuntimeStepRead(
        string Label,
        CarrierLocation Location);

    public readonly record struct RuntimeInputRead(
        Placeholder Slot,
        IElement ResidueOrTension);

    public readonly record struct RuntimeResolutionRead(
        IElement Source,
        IElement Input,
        IElement Step,
        IElement Resolution,
        IElement ResidueOrTension);

    public static Relation Partition(
        Carrier root,
        IElement source,
        IElement count,
        IReadOnlyList<IElement> sites,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            source ?? UnknownPoint(),
            count ?? UnknownPoint(),
            Relation.OrderedGroup(new Carrier(), [.. sites]),
            residueOrTension ?? UnknownPoint());

    public static Relation UnitComparison(
        Carrier root,
        IElement source,
        IElement unit,
        IElement? residueOrTension = null)
    {
        var result = Relation.OrderedGroup(new Carrier(), source ?? UnknownPoint(), unit ?? UnknownPoint());
        return ReadWitness.Create(
            RootOrNew(root),
            source ?? UnknownPoint(),
            unit ?? UnknownPoint(),
            result,
            residueOrTension ?? UnknownPoint(),
            unit ?? UnknownPoint());
    }

    public static Relation UnitResolution(
        Carrier root,
        IElement comparison,
        IElement? residueOrTension = null)
    {
        var witness = ReadWitness.Create(
            new Carrier(),
            comparison ?? UnknownPoint(),
            comparison ?? UnknownPoint(),
            comparison ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            comparison ?? UnknownPoint());
        return Relation.OrderedGroup(
            RootOrNew(root),
            comparison ?? UnknownPoint(),
            witness,
            residueOrTension ?? UnknownPoint());
    }

    public static Relation Comparison(
        Carrier root,
        IElement source,
        IElement target,
        IElement? resolution = null,
        IElement? residueOrTension = null) =>
        ReadWitness.Create(
            RootOrNew(root),
            source ?? UnknownPoint(),
            target ?? UnknownPoint(),
            resolution ?? target ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            resolution ?? target ?? UnknownPoint());

    public static Relation ResolutionRung(Carrier root, Carrier rungCarrier, string label, long ticks) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            Relation.Point(RootOrNew(rungCarrier), ticks),
            ConceptAddressSurface.Atom(new Carrier(), label));

    public static Relation ObserverBasis(
        Carrier root,
        IElement source,
        IElement? residueOrTension = null) =>
        BasisReadSurface.Create(
            RootOrNew(root),
            source ?? UnknownPoint(),
            source ?? UnknownPoint(),
            source ?? UnknownPoint(),
            UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            source ?? UnknownPoint());

    public static Relation ObserverResolution(
        Carrier root,
        IElement source,
        IElement basis,
        IElement resolution,
        IElement? residueOrTension = null) =>
        ReadWitness.Create(
            RootOrNew(root),
            source ?? UnknownPoint(),
            basis ?? UnknownPoint(),
            resolution ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            resolution ?? UnknownPoint());

    public static Relation Slot(Carrier root, IElement role) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            role ?? UnknownPoint(),
            new Placeholder());

    public static Relation Target(Carrier root, IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            new Placeholder(),
            residueOrTension ?? UnknownPoint());

    public static Relation Approximation(
        Carrier root,
        IElement comparison,
        IElement resolution,
        IElement? residueOrTension = null)
    {
        var witness = ReadWitness.Create(
            new Carrier(),
            comparison ?? UnknownPoint(),
            resolution ?? UnknownPoint(),
            comparison ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            resolution ?? UnknownPoint());
        return RefinementRead.Create(
            RootOrNew(root),
            comparison ?? UnknownPoint(),
            resolution ?? UnknownPoint(),
            witness,
            residueOrTension ?? UnknownPoint(),
            resolution ?? UnknownPoint());
    }

    public static Relation AmbiguousBasis(
        Carrier root,
        IElement source,
        IElement? residueOrTension = null)
    {
        var basisSlot = new Placeholder();
        return BasisReadSurface.Create(
            RootOrNew(root),
            source ?? UnknownPoint(),
            basisSlot,
            basisSlot,
            UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            source ?? UnknownPoint());
    }

    public static Relation Rung(
        Carrier root,
        IElement source,
        IElement resolution,
        IElement? residueOrTension = null) =>
        ReadWitness.Create(
            RootOrNew(root),
            source ?? UnknownPoint(),
            resolution ?? UnknownPoint(),
            source ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            resolution ?? UnknownPoint());

    public static Relation Correspondence(Carrier root, IElement source, IElement target) =>
        FocusSurface.Bind(source ?? UnknownPoint(), target ?? UnknownPoint());

    public static Relation LadderResolution(
        Carrier root,
        IElement roughRung,
        IElement fineRung,
        IElement correspondence,
        IElement? residueOrTension = null)
    {
        var witness = ReadWitness.Create(
            new Carrier(),
            roughRung ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            fineRung ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            correspondence ?? UnknownPoint());
        return RefinementRead.Create(
            RootOrNew(root),
            roughRung ?? UnknownPoint(),
            fineRung ?? UnknownPoint(),
            witness,
            residueOrTension ?? UnknownPoint(),
            correspondence ?? UnknownPoint());
    }

    public static Relation RuntimeInput(Carrier root, IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            ConceptAddressSurface.Atom(new Carrier(), "input"),
            new Placeholder(),
            residueOrTension ?? UnknownPoint());

    public static Relation RuntimeStep(Carrier root, Carrier clock, string label, long ticks) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            Relation.Point(RootOrNew(clock), ticks),
            ConceptAddressSurface.Atom(new Carrier(), "t"),
            ConceptAddressSurface.Atom(new Carrier(), label));

    public static Relation RuntimeResolution(
        Carrier root,
        Carrier clock,
        IElement source,
        IElement input,
        IElement step,
        IElement resolution,
        IElement? residueOrTension = null)
    {
        var stepRecord = TimeStepSequence.Step(
            new Carrier(),
            RootOrNew(clock),
            0,
            1,
            source ?? UnknownPoint(),
            input ?? UnknownPoint(),
            resolution ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint());
        return Relation.OrderedGroup(
            RootOrNew(root),
            stepRecord,
            source ?? UnknownPoint(),
            input ?? UnknownPoint(),
            step ?? UnknownPoint(),
            resolution ?? UnknownPoint());
    }

    public static bool TryReadPartition(IElement element, out PartitionRead read)
    {
        var surface = SurfaceOf(element);
        if (TryReadGroup(surface, out var elements) &&
            elements.Length == 4 &&
            elements[2] is Relation sitesFrame &&
            EncodedTextSurface.TryReadRootElements(sitesFrame, out var sites) &&
            sites.All(site => site is Relation point && EncodedTextSurface.TryReadPoint(point, out _)))
        {
            read = new PartitionRead(elements[0], elements[1], sites, elements[3]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadUnitComparison(IElement element, out UnitComparisonRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var source,
                out var unit,
                out var result,
                out var residue,
                out _) &&
            result is Relation resultFrame &&
            EncodedTextSurface.TryReadRootElements(resultFrame, out var resultElements) &&
            resultElements.Length == 2 &&
            ReferenceEquals(resultElements[0], source) &&
            ReferenceEquals(resultElements[1], unit))
        {
            read = new UnitComparisonRead(source, unit, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadUnitResolution(IElement element, out UnitResolutionRead read)
    {
        var surface = SurfaceOf(element);
        if (TryReadGroup(surface, out var elements) &&
            elements.Length == 3 &&
            elements[1] is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var selected,
                out var readSurface,
                out var result,
                out var residue,
                out _) &&
            ReferenceEquals(elements[0], selected) &&
            ReferenceEquals(selected, readSurface) &&
            ReferenceEquals(selected, result))
        {
            read = new UnitResolutionRead(elements[0], witness, IsUnknown(elements[2]) ? residue : elements[2]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadComparison(IElement element, out ComparisonRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var source,
                out var target,
                out var result,
                out var residue,
                out _))
        {
            var resolution = ReferenceEquals(result, target)
                ? null
                : result;
            read = new ComparisonRead(source, target, resolution, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadResolutionRung(IElement element, out ResolutionRungRead read)
    {
        if (TryReadLabeledPoint(SurfaceOf(element), out var location, out var label))
        {
            read = new ResolutionRungRead(label, location);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadObserverBasis(IElement element, out ObserverBasisRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation basisRead &&
            BasisReadSurface.TryRead(
                basisRead,
                out var source,
                out var axis,
                out var lane,
                out _,
                out var residue,
                out _) &&
            ReferenceEquals(source, axis) &&
            ReferenceEquals(source, lane))
        {
            read = new ObserverBasisRead(source, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadObserverResolution(IElement element, out ObserverResolutionRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var source,
                out var basis,
                out var resolution,
                out var residue,
                out _) &&
            TryReadObserverBasis(basis, out _) &&
            TryReadResolutionRung(resolution, out _))
        {
            read = new ObserverResolutionRead(source, basis, resolution, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadSlot(IElement element, out SlotRead read)
    {
        if (TryReadGroup(SurfaceOf(element), out var elements) &&
            elements.Length == 2 &&
            elements[1] is Placeholder slot)
        {
            read = new SlotRead(elements[0], slot);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadTarget(IElement element, out TargetRead read)
    {
        if (TryReadGroup(SurfaceOf(element), out var elements) &&
            elements.Length == 2 &&
            elements[0] is Placeholder slot)
        {
            read = new TargetRead(slot, elements[1]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadApproximation(IElement element, out ApproximationRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation refinement &&
            RefinementRead.TryRead(
                refinement,
                out var comparison,
                out var resolution,
                out var witness,
                out var residue,
                out _) &&
            TryReadResolutionRung(resolution, out _) &&
            ReadWitness.TryRead(
                witness,
                out var selected,
                out var readSurface,
                out var result,
                out _,
                out _) &&
            ReferenceEquals(comparison, selected) &&
            ReferenceEquals(resolution, readSurface) &&
            ReferenceEquals(comparison, result))
        {
            read = new ApproximationRead(comparison, resolution, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadAmbiguousBasis(IElement element, out AmbiguousBasisRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation basisRead &&
            BasisReadSurface.TryRead(
                basisRead,
                out var source,
                out var axis,
                out var lane,
                out _,
                out var residue,
                out _) &&
            axis is Placeholder axisSlot &&
            lane is Placeholder laneSlot &&
            axisSlot.SameSlotAs(laneSlot))
        {
            read = new AmbiguousBasisRead(source, axisSlot, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadAmbiguousResolution(IElement element, out ObserverResolutionRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var source,
                out var basis,
                out _,
                out var residue,
                out _) &&
            TryReadAmbiguousBasis(basis, out _))
        {
            read = new ObserverResolutionRead(source, basis, UnknownPoint(), residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadRung(IElement element, out RungRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var source,
                out var resolution,
                out var result,
                out var residue,
                out _) &&
            ReferenceEquals(source, result) &&
            TryReadResolutionRung(resolution, out _))
        {
            read = new RungRead(source, resolution, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadCorrespondence(IElement element, out CorrespondenceRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation relation)
        {
            read = new CorrespondenceRead(relation.Left, relation.Right);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadLadderResolution(IElement element, out LadderResolutionRead read)
    {
        var surface = SurfaceOf(element);
        if (surface is Relation refinement &&
            RefinementRead.TryRead(
                refinement,
                out var rough,
                out var fine,
                out var witness,
                out var residue,
                out var correspondence) &&
            TryReadRung(rough, out _) &&
            TryReadRung(fine, out _) &&
            TryReadCorrespondence(correspondence, out _) &&
            ReadWitness.TryRead(
                witness,
                out var selected,
                out var readSurface,
                out var result,
                out _,
                out _) &&
            ReferenceEquals(rough, selected) &&
            ReferenceEquals(correspondence, readSurface) &&
            ReferenceEquals(fine, result))
        {
            read = new LadderResolutionRead(rough, fine, correspondence, residue);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadRuntimeInput(IElement element, out RuntimeInputRead read)
    {
        if (TryReadGroup(SurfaceOf(element), out var elements) &&
            elements.Length == 3 &&
            ConceptAddressSurface.TryReadAtom(elements[0], out var role, out _) &&
            role == "input" &&
            elements[1] is Placeholder slot)
        {
            read = new RuntimeInputRead(slot, elements[2]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadRuntimeStep(IElement element, out RuntimeStepRead read)
    {
        if (TryReadGroup(SurfaceOf(element), out var elements) &&
            elements.Length == 3 &&
            elements[0] is Relation point &&
            EncodedTextSurface.TryReadPoint(point, out var location) &&
            ConceptAddressSurface.TryReadAtom(elements[1], out var role, out _) &&
            role == "t" &&
            ConceptAddressSurface.TryReadAtom(elements[2], out var label, out _))
        {
            read = new RuntimeStepRead(label, location);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadRuntimeResolution(IElement element, out RuntimeResolutionRead read)
    {
        var surface = SurfaceOf(element);
        if (TryReadGroup(surface, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation step &&
            TimeStepSequence.TryReadStep(
                step,
                out _,
                out _,
                out _,
                out var before,
                out var transition,
                out var after,
                out var residue) &&
            ReferenceEquals(before, elements[1]) &&
            ReferenceEquals(transition, elements[2]) &&
            ReferenceEquals(after, elements[4]) &&
            ReferenceEquals(elements[3], elements[3]) &&
            TryReadRuntimeStep(elements[3], out _) &&
            TryReadResolutionRung(elements[4], out _))
        {
            read = new RuntimeResolutionRead(elements[1], elements[2], elements[3], elements[4], residue);
            return true;
        }

        read = default;
        return false;
    }

    public static IElement[] PartitionSites(Relation span, int divisions)
    {
        if (divisions <= 0 ||
            !TryReadSegment(span, out var start, out var end) ||
            start.Carrier != end.Carrier)
        {
            return [];
        }

        var sites = new List<IElement>();
        var delta = end.Ticks - start.Ticks;
        for (var index = 0; index <= divisions; index++)
        {
            var ticks = start.Ticks + (delta * index / divisions);
            sites.Add(Relation.Point(start.Carrier, ticks));
        }

        return [.. sites];
    }

    private static bool TryReadLabeledPoint(IElement element, out CarrierLocation location, out string label)
    {
        if (TryReadGroup(element, out var elements) &&
            elements.Length == 2 &&
            elements[0] is Relation point &&
            EncodedTextSurface.TryReadPoint(point, out var pointLocation) &&
            ConceptAddressSurface.TryReadAtom(elements[1], out var text, out _))
        {
            location = pointLocation;
            label = text;
            return true;
        }

        location = default;
        label = string.Empty;
        return false;
    }

    private static bool TryReadGroup(IElement element, out IElement[] elements)
    {
        if (element is Relation relation &&
            EncodedTextSurface.TryReadRootElements(relation, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static IElement SurfaceOf(IElement element) =>
        ConceptDefinitionSurface.TryRead(element, out var definition)
            ? definition.Body
            : element;

    private static bool TryReadSegment(
        IElement element,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (element is Relation segment &&
            segment.Left is Relation leftPoint &&
            segment.Right is Relation rightPoint &&
            EncodedTextSurface.TryReadPoint(leftPoint, out start) &&
            EncodedTextSurface.TryReadPoint(rightPoint, out end) &&
            start.Carrier.SameIdentityAs(end.Carrier))
        {
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    private static bool IsUnknown(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element);

    private static Relation UnknownPoint() =>
        EncodedTextSurface.UnknownPoint();

    private static Carrier RootOrNew(Carrier carrier) =>
        EncodedTextSurface.RootOrNew(carrier);
}
