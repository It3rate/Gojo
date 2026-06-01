using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Relation-shaped focus/selection surface. A focus surface records a source
/// structure, the local basis used to select from it, selected participation,
/// carry-forward complement, replacement or anti-participation, correspondence,
/// residue/tension, and optional provenance or timeline.
/// </summary>
public static class FocusSurface
{
    /// <summary>
    /// Creates a focus surface:
    /// source, focus basis, participation, complement, replacement,
    /// correspondence, residue/tension, provenance/timeline.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement source,
        IElement focusBasis,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement correspondence,
        IElement residueOrTension,
        IElement provenanceOrTimeline)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            source ?? UnknownPoint(),
            focusBasis ?? UnknownPoint(),
            participation ?? UnknownPoint(),
            complement ?? UnknownPoint(),
            replacement ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenanceOrTimeline ?? UnknownPoint());
    }

    /// <summary>Creates a minimal focus surface with unresolved complement and residue.</summary>
    public static Relation Create(
        Carrier root,
        IElement source,
        IElement focusBasis,
        IElement participation) =>
        Create(
            root,
            source,
            focusBasis,
            participation,
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint());

    /// <summary>Creates an ordered focus frame. The elements remain ordinary Gojo structure.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>Creates an ordered frame of relation-shaped bindings.</summary>
    public static Relation BindingFrame(Carrier root, params Relation[] bindings) =>
        Relation.OrderedGroup(RootOrNew(root), bindings ?? []);

    /// <summary>
    /// Creates a local binding. In focus context this can mean source to result,
    /// selected part to replacement, or lower-rung site to higher-rung site.
    /// </summary>
    public static Relation Bind(IElement source, IElement target) =>
        new(source ?? UnknownPoint(), target ?? UnknownPoint());

    /// <summary>
    /// Creates a partial span record:
    /// whole span, selected span, carry-forward complement, residue/tension.
    /// </summary>
    public static Relation PartialSpan(
        Carrier root,
        IElement wholeSpan,
        IElement selectedSpan,
        IElement complement,
        IElement residueOrTension) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            wholeSpan ?? UnknownPoint(),
            selectedSpan ?? UnknownPoint(),
            complement ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint());

    /// <summary>Creates a partial span record with unresolved complement and residue.</summary>
    public static Relation PartialSpan(
        Carrier root,
        IElement wholeSpan,
        IElement selectedSpan) =>
        PartialSpan(root, wholeSpan, selectedSpan, UnknownPoint(), UnknownPoint());

    /// <summary>
    /// Creates a focus transition step using the shared time-step sequence
    /// shape. The focus surface is the transition evidence.
    /// </summary>
    public static Relation Step(
        Carrier root,
        Carrier clock,
        long startTicks,
        long endTicks,
        IElement before,
        IElement focus,
        IElement after,
        IElement residueOrTension) =>
        TimeStepSequence.Step(
            RootOrNew(root),
            RootOrNew(clock),
            startTicks,
            endTicks,
            before ?? UnknownPoint(),
            focus ?? UnknownPoint(),
            after ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint());

    /// <summary>Creates an ordered focus timeline rooted in an initial state.</summary>
    public static Relation Timeline(
        Carrier root,
        Carrier clock,
        IElement initial,
        params Relation[] steps) =>
        TimeStepSequence.Create(RootOrNew(root), RootOrNew(clock), initial ?? UnknownPoint(), steps ?? []);

    /// <summary>Reads a focus surface created by <see cref="Create(Carrier,IElement,IElement,IElement,IElement,IElement,IElement,IElement,IElement)"/>.</summary>
    public static bool TryRead(
        Relation focus,
        out IElement source,
        out IElement focusBasis,
        out IElement participation,
        out IElement complement,
        out IElement replacement,
        out IElement correspondence,
        out IElement residueOrTension,
        out IElement provenanceOrTimeline)
    {
        if (TryReadRootElements(focus, out var elements) &&
            elements.Length >= 8)
        {
            source = elements[0];
            focusBasis = elements[1];
            participation = elements[2];
            complement = elements[3];
            replacement = elements[4];
            correspondence = elements[5];
            residueOrTension = elements[6];
            provenanceOrTimeline = elements[7];
            return true;
        }

        source = default!;
        focusBasis = default!;
        participation = default!;
        complement = default!;
        replacement = default!;
        correspondence = default!;
        residueOrTension = default!;
        provenanceOrTimeline = default!;
        return false;
    }

    /// <summary>Reads a partial span record.</summary>
    public static bool TryReadPartialSpan(
        Relation partialSpan,
        out IElement wholeSpan,
        out IElement selectedSpan,
        out IElement complement,
        out IElement residueOrTension)
    {
        if (TryReadRootElements(partialSpan, out var elements) &&
            elements.Length >= 4)
        {
            wholeSpan = elements[0];
            selectedSpan = elements[1];
            complement = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        wholeSpan = default!;
        selectedSpan = default!;
        complement = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads an ordered focus frame.</summary>
    public static bool TryReadFrame(IElement frame, out IElement[] elements)
    {
        if (frame is Relation relation &&
            TryReadRootElements(relation, out var readElements))
        {
            elements = readElements;
            return true;
        }

        elements = [];
        return false;
    }

    /// <summary>Reads an ordered binding frame.</summary>
    public static bool TryReadBindingFrame(IElement frame, out Relation[] bindings)
    {
        if (TryReadFrame(frame, out var elements))
        {
            bindings = [.. elements.OfType<Relation>()];
            return true;
        }

        bindings = [];
        return false;
    }

    /// <summary>Projects a focus surface into the common read-witness skeleton.</summary>
    public static bool TryReadWitness(Relation focus, Carrier root, out Relation witness)
    {
        if (TryRead(
                focus,
                out var source,
                out var focusBasis,
                out var participation,
                out _,
                out _,
                out _,
                out var residueOrTension,
                out var provenanceOrTimeline))
        {
            witness = ReadWitness.Create(
                RootOrNew(root),
                source,
                focusBasis,
                participation,
                residueOrTension,
                provenanceOrTimeline);
            return true;
        }

        witness = null!;
        return false;
    }

    /// <summary>Reads the focus provenance when it is a time-step sequence.</summary>
    public static bool TryReadTimeline(Relation focus, out Relation timeline)
    {
        if (TryRead(
                focus,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var provenanceOrTimeline) &&
            provenanceOrTimeline is Relation candidate &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
