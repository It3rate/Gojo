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
    /// Creates focus projection evidence:
    /// source focus, target source, correspondence, projected focus, residue.
    /// </summary>
    public static Relation Projection(
        Carrier root,
        Relation sourceFocus,
        IElement targetSource,
        IElement correspondence,
        Relation projectedFocus,
        IElement residueOrTension) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            sourceFocus ?? UnknownPoint(),
            targetSource ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            projectedFocus ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint());

    /// <summary>
    /// Projects a focus through correspondence into a target source. The
    /// correspondence is ordinary binding structure; unmatched source pieces
    /// remain visible in the projected focus.
    /// </summary>
    public static bool TryProject(
        Relation focus,
        Carrier root,
        IElement targetSource,
        IElement correspondence,
        IElement residueOrTension,
        out Relation projectedFocus,
        out Relation projection)
    {
        if (!TryRead(
                focus,
                out _,
                out var focusBasis,
                out var participation,
                out var complement,
                out var replacement,
                out _,
                out var focusResidue,
                out var provenanceOrTimeline))
        {
            projectedFocus = null!;
            projection = null!;
            return false;
        }

        var bindings = ReadCorrespondenceBindings(correspondence);
        var projectionResidue = residueOrTension ?? focusResidue ?? UnknownPoint();
        var projectionProvenance = Frame(
            new Carrier(),
            focus,
            correspondence ?? UnknownPoint(),
            provenanceOrTimeline ?? UnknownPoint());
        projectedFocus = Create(
            RootOrNew(root),
            targetSource ?? ApplyCorrespondence(focus, bindings),
            ApplyCorrespondence(focusBasis, bindings),
            ApplyCorrespondence(participation, bindings),
            ApplyCorrespondence(complement, bindings),
            ApplyCorrespondence(replacement, bindings),
            correspondence ?? UnknownPoint(),
            projectionResidue,
            projectionProvenance);
        projection = Projection(
            new Carrier(),
            focus,
            targetSource ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            projectedFocus,
            projectionResidue);
        return true;
    }

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

    /// <summary>
    /// Creates a common read witness where the selected source is read through
    /// this focus surface to produce a result.
    /// </summary>
    public static bool TryCreateResultWitness(
        Carrier root,
        Relation focus,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation witness)
    {
        if (TryRead(focus, out var source, out _, out _, out _, out _, out _, out var focusResidue, out var focusProvenance))
        {
            witness = ReadWitness.Create(
                RootOrNew(root),
                source,
                focus,
                result ?? UnknownPoint(),
                residueOrTension ?? focusResidue ?? UnknownPoint(),
                provenance ?? focusProvenance ?? UnknownPoint());
            return true;
        }

        witness = null!;
        return false;
    }

    /// <summary>
    /// Creates a refinement read where the focus is the parameter being refined
    /// and the result is the candidate binding.
    /// </summary>
    public static bool TryCreateRefinementRead(
        Carrier root,
        Relation focus,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation refinement)
    {
        if (TryCreateResultWitness(
                new Carrier(),
                focus,
                result,
                residueOrTension,
                provenance,
                out var witness))
        {
            refinement = RefinementRead.Create(
                RootOrNew(root),
                focus,
                result ?? UnknownPoint(),
                witness,
                residueOrTension ?? UnknownPoint(),
                provenance ?? UnknownPoint());
            return true;
        }

        refinement = null!;
        return false;
    }

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
            elements.Length == 8)
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
            elements.Length == 4)
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

    /// <summary>Reads focus projection evidence.</summary>
    public static bool TryReadProjection(
        Relation projection,
        out Relation sourceFocus,
        out IElement targetSource,
        out IElement correspondence,
        out Relation projectedFocus,
        out IElement residueOrTension)
    {
        if (TryReadRootElements(projection, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation sourceFocusRelation &&
            elements[3] is Relation projectedFocusRelation)
        {
            sourceFocus = sourceFocusRelation;
            targetSource = elements[1];
            correspondence = elements[2];
            projectedFocus = projectedFocusRelation;
            residueOrTension = elements[4];
            return true;
        }

        sourceFocus = null!;
        targetSource = default!;
        correspondence = default!;
        projectedFocus = null!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads a timed step whose transition evidence is a focus surface.</summary>
    public static bool TryReadStep(
        Relation step,
        out Relation span,
        out CarrierLocation start,
        out CarrierLocation end,
        out IElement before,
        out Relation focus,
        out IElement after,
        out IElement residueOrTension)
    {
        if (TimeStepSequence.TryReadStep(
                step,
                out var timeSpan,
                out var startLocation,
                out var endLocation,
                out var beforeElement,
                out var transition,
                out var afterElement,
                out var residue) &&
            transition is Relation focusSurface &&
            TryRead(focusSurface, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            span = timeSpan;
            start = startLocation;
            end = endLocation;
            before = beforeElement;
            focus = focusSurface;
            after = afterElement;
            residueOrTension = residue;
            return true;
        }

        span = null!;
        start = default;
        end = default;
        before = default!;
        focus = null!;
        after = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads a result witness whose read surface is a focus surface.</summary>
    public static bool TryReadResultWitness(
        Relation witness,
        out Relation focus,
        out IElement source,
        out IElement result,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (ReadWitness.TryRead(
                witness,
                out var selected,
                out var readSurface,
                out var witnessResult,
                out var witnessResidue,
                out var witnessProvenance) &&
            readSurface is Relation focusSurface &&
            TryRead(focusSurface, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            focus = focusSurface;
            source = selected;
            result = witnessResult;
            residueOrTension = witnessResidue;
            provenance = witnessProvenance;
            return true;
        }

        focus = null!;
        source = default!;
        result = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads a refinement read whose parameter is a focus surface.</summary>
    public static bool TryReadRefinement(
        Relation refinement,
        out Relation focus,
        out IElement result,
        out Relation witness,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (RefinementRead.TryRead(
                refinement,
                out var parameter,
                out var binding,
                out var readWitness,
                out var refinementResidue,
                out var refinementProvenance) &&
            parameter is Relation focusSurface &&
            TryRead(focusSurface, out _, out _, out _, out _, out _, out _, out _, out _) &&
            TryReadResultWitness(readWitness, out var witnessedFocus, out _, out var witnessedResult, out _, out _) &&
            Equals(focusSurface, witnessedFocus) &&
            Equals(binding, witnessedResult))
        {
            focus = focusSurface;
            result = binding;
            witness = readWitness;
            residueOrTension = refinementResidue;
            provenance = refinementProvenance;
            return true;
        }

        focus = null!;
        result = default!;
        witness = null!;
        residueOrTension = default!;
        provenance = default!;
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
            TryReadOrderedGroupChain(structure.Left, holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadOrderedGroupChain(IElement root, Carrier holder, out IElement[] elements)
    {
        var collected = new List<IElement>();
        if (CollectOrderedGroupChain(root, holder, collected))
        {
            elements = [.. collected];
            return true;
        }

        elements = [];
        return false;
    }

    private static bool CollectOrderedGroupChain(IElement element, Carrier holder, List<IElement> elements)
    {
        if (IsEmptyGroupMarker(element, holder))
        {
            return true;
        }

        if (TryReadDirectAdditiveAttachment(element, holder, out var participant))
        {
            elements.Add(participant);
            return true;
        }

        if (element is Relation relation)
        {
            return CollectOrderedGroupChain(relation.Left, holder, elements) &&
                CollectOrderedGroupChain(relation.Right, holder, elements);
        }

        return false;
    }

    private static bool TryReadDirectAdditiveAttachment(IElement element, Carrier holder, out IElement participant)
    {
        if (element is Relation relation &&
            relation.Left is Relation holderOrigin &&
            TryReadPoint(holderOrigin, out var origin, out var originCarrier) &&
            originCarrier.SameIdentityAs(holder) &&
            origin.Ticks == 0)
        {
            participant = relation.Right;
            return true;
        }

        participant = default!;
        return false;
    }

    private static bool IsEmptyGroupMarker(IElement element, Carrier holder) =>
        TryReadPoint(element, out var location, out var carrier) &&
        carrier.SameIdentityAs(holder) &&
        location.Ticks == 1;

    private static bool TryReadPoint(IElement element, out CarrierLocation location, out Carrier carrier)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            carrier = pointCarrier;
            return true;
        }

        location = default;
        carrier = null!;
        return false;
    }

    private static Relation[] ReadCorrespondenceBindings(IElement correspondence) =>
        TryReadBindingFrame(correspondence, out var bindings)
            ? bindings
            : correspondence is Relation binding
                ? [binding]
                : [];

    private static IElement ApplyCorrespondence(IElement element, IReadOnlyList<Relation> bindings)
    {
        foreach (var binding in bindings)
        {
            if (Equals(element, binding.Left))
            {
                return binding.Right;
            }
        }

        if (element is not Relation relation)
        {
            return element;
        }

        var left = ApplyCorrespondence(relation.Left, bindings);
        var right = ApplyCorrespondence(relation.Right, bindings);
        return ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right)
            ? relation
            : new Relation(left, right);
    }

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
