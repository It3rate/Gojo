using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Relation-shaped surface for structural ladders. A structural rung is a
/// complete structure plus optional focus/refinement evidence that relates it
/// to another rung. The helper only builds and reads ordinary Gojo structure.
/// </summary>
public static class StructuralLadderSurface
{
    /// <summary>
    /// Creates one rung:
    /// source, focus, result, correspondence, residue/tension,
    /// result witness, refinement read, provenance/timeline.
    /// </summary>
    public static Relation Rung(
        Carrier root,
        IElement source,
        IElement focus,
        IElement result,
        IElement correspondence,
        IElement residueOrTension,
        IElement provenanceOrTimeline)
    {
        var sourceElement = source ?? UnknownPoint();
        var focusElement = focus ?? UnknownPoint();
        var resultElement = result ?? sourceElement;
        var residue = residueOrTension ?? UnknownPoint();
        var provenance = provenanceOrTimeline ?? UnknownPoint();
        var witness = TryCreateFocusWitness(focusElement, resultElement, residue, provenance, out var focusWitness)
            ? focusWitness
            : UnknownPoint();
        var refinement = TryCreateFocusRefinement(focusElement, resultElement, residue, provenance, out var focusRefinement)
            ? focusRefinement
            : UnknownPoint();

        return Relation.OrderedGroup(
            RootOrNew(root),
            sourceElement,
            focusElement,
            resultElement,
            correspondence ?? UnknownPoint(),
            residue,
            witness,
            refinement,
            provenance);
    }

    /// <summary>Creates a base rung whose result is the supplied structure.</summary>
    public static Relation BaseRung(Carrier root, IElement structure) =>
        Rung(
            root,
            structure,
            UnknownPoint(),
            structure,
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint());

    /// <summary>
    /// Creates a refined rung from a real focus surface. The rung source is
    /// read from the focus itself so refinement does not duplicate selection
    /// intent in code-side arguments.
    /// </summary>
    public static bool TryCreateRefinedRungFromFocus(
        Carrier root,
        Relation focus,
        IElement result,
        IElement correspondence,
        IElement residueOrTension,
        IElement provenanceOrTimeline,
        out Relation rung)
    {
        if (!FocusSurface.TryRead(
                focus,
                out var source,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            rung = null!;
            return false;
        }

        var candidate = Rung(
            RootOrNew(root),
            source,
            focus,
            result ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenanceOrTimeline ?? UnknownPoint());

        if (TryReadRung(candidate, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            rung = candidate;
            return true;
        }

        rung = null!;
        return false;
    }

    /// <summary>Creates an ordered frame of ladder rungs.</summary>
    public static Relation RungFrame(Carrier root, params Relation[] rungs) =>
        Relation.OrderedGroup(RootOrNew(root), rungs ?? []);

    /// <summary>
    /// Creates a structural ladder surface:
    /// base rung, rung frame, history/timeline, residue/tension, provenance.
    /// </summary>
    public static Relation Create(
        Carrier root,
        Relation baseRung,
        IElement rungFrame,
        IElement historyOrTimeline,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            baseRung ?? BaseRung(new Carrier(), UnknownPoint()),
            rungFrame ?? RungFrame(new Carrier()),
            historyOrTimeline ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>Creates a structural ladder with an ordered set of rungs and no explicit timeline yet.</summary>
    public static Relation CreateWithRungs(Carrier root, Relation baseRung, params Relation[] rungs) =>
        Create(
            root,
            baseRung,
            (IElement)RungFrame(new Carrier(), rungs ?? []),
            (IElement)UnknownPoint(),
            (IElement)UnknownPoint(),
            (IElement)UnknownPoint());

    /// <summary>
    /// Creates one timed ladder transition. The transition evidence is the
    /// focus surface; before and after are rung records.
    /// </summary>
    public static Relation Step(
        Carrier root,
        Carrier clock,
        long startTicks,
        long endTicks,
        Relation beforeRung,
        Relation focus,
        Relation afterRung,
        IElement residueOrTension) =>
        TimeStepSequence.Step(
            RootOrNew(root),
            RootOrNew(clock),
            startTicks,
            endTicks,
            beforeRung ?? BaseRung(new Carrier(), UnknownPoint()),
            focus ?? UnknownPoint(),
            afterRung ?? BaseRung(new Carrier(), UnknownPoint()),
            residueOrTension ?? UnknownPoint());

    /// <summary>Creates a ladder timeline rooted at the base rung.</summary>
    public static Relation Timeline(
        Carrier root,
        Carrier clock,
        Relation baseRung,
        params Relation[] steps) =>
        TimeStepSequence.Create(
            RootOrNew(root),
            RootOrNew(clock),
            baseRung ?? BaseRung(new Carrier(), UnknownPoint()),
            steps ?? []);

    /// <summary>
    /// Creates a change read:
    /// source, selected participation, carry-forward complement, replacement,
    /// correspondence, result, residue/tension.
    /// </summary>
    public static Relation ChangeRead(
        Carrier root,
        IElement source,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement correspondence,
        IElement result,
        IElement residueOrTension) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            source ?? UnknownPoint(),
            participation ?? UnknownPoint(),
            complement ?? UnknownPoint(),
            replacement ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            result ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint());

    /// <summary>Reads a rung created by <see cref="Rung"/>.</summary>
    public static bool TryReadRung(
        Relation rung,
        out IElement source,
        out IElement focus,
        out IElement result,
        out IElement correspondence,
        out IElement residueOrTension,
        out IElement witness,
        out IElement refinement,
        out IElement provenanceOrTimeline)
    {
        if (TryReadRootElements(rung, out var elements) &&
            elements.Length == 8 &&
            IsValidRungElements(elements))
        {
            source = elements[0];
            focus = elements[1];
            result = elements[2];
            correspondence = elements[3];
            residueOrTension = elements[4];
            witness = elements[5];
            refinement = elements[6];
            provenanceOrTimeline = elements[7];
            return true;
        }

        source = default!;
        focus = default!;
        result = default!;
        correspondence = default!;
        residueOrTension = default!;
        witness = default!;
        refinement = default!;
        provenanceOrTimeline = default!;
        return false;
    }

    /// <summary>Reads a structural ladder surface.</summary>
    public static bool TryRead(
        Relation ladder,
        out Relation baseRung,
        out IElement rungFrame,
        out IElement historyOrTimeline,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(ladder, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation baseRungRelation &&
            TryReadRung(baseRungRelation, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            baseRung = baseRungRelation;
            rungFrame = elements[1];
            historyOrTimeline = elements[2];
            residueOrTension = elements[3];
            provenance = elements[4];
            return true;
        }

        baseRung = null!;
        rungFrame = default!;
        historyOrTimeline = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads ordered rungs from a rung frame.</summary>
    public static bool TryReadRungFrame(IElement rungFrame, out Relation[] rungs)
    {
        if (rungFrame is Relation frame &&
            TryReadRootElements(frame, out var elements))
        {
            rungs = [.. elements
                .OfType<Relation>()
                .Where(rung => TryReadRung(rung, out _, out _, out _, out _, out _, out _, out _, out _))];
            return true;
        }

        rungs = [];
        return false;
    }

    /// <summary>Reads a timed ladder step whose transition is a focus surface.</summary>
    public static bool TryReadStep(
        Relation step,
        out Relation span,
        out CarrierLocation start,
        out CarrierLocation end,
        out Relation beforeRung,
        out Relation focus,
        out Relation afterRung,
        out IElement residueOrTension)
    {
        if (FocusSurface.TryReadStep(
                step,
                out var timeSpan,
                out var startLocation,
                out var endLocation,
                out var before,
                out var focusSurface,
                out var after,
                out var residue) &&
            before is Relation beforeRelation &&
            after is Relation afterRelation &&
            TryReadRung(beforeRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            TryReadRung(afterRelation, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            span = timeSpan;
            start = startLocation;
            end = endLocation;
            beforeRung = beforeRelation;
            focus = focusSurface;
            afterRung = afterRelation;
            residueOrTension = residue;
            return true;
        }

        span = null!;
        start = default;
        end = default;
        beforeRung = null!;
        focus = null!;
        afterRung = null!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>
    /// Creates a local change read from a refined rung. This is a read over the
    /// rung and its focus surface; it does not replace the rung structure.
    /// </summary>
    public static bool TryCreateChangeRead(Carrier root, Relation rung, out Relation change)
    {
        if (TryReadRung(
                rung,
                out var source,
                out var focus,
                out var result,
                out var rungCorrespondence,
                out var rungResidue,
                out _,
                out _,
                out _) &&
            focus is Relation focusSurface &&
            FocusSurface.TryRead(
                focusSurface,
                out _,
                out _,
                out var participation,
                out var complement,
                out var replacement,
                out var focusCorrespondence,
                out var focusResidue,
                out _))
        {
            change = ChangeRead(
                RootOrNew(root),
                source,
                participation,
                complement,
                replacement,
                IsUnknownPoint(rungCorrespondence) ? focusCorrespondence : rungCorrespondence,
                result,
                IsUnknownPoint(rungResidue) ? focusResidue : rungResidue);
            return true;
        }

        change = null!;
        return false;
    }

    /// <summary>Reads a change read created by <see cref="ChangeRead"/>.</summary>
    public static bool TryReadChange(
        Relation change,
        out IElement source,
        out IElement participation,
        out IElement complement,
        out IElement replacement,
        out IElement correspondence,
        out IElement result,
        out IElement residueOrTension)
    {
        if (TryReadRootElements(change, out var elements) &&
            elements.Length >= 7)
        {
            source = elements[0];
            participation = elements[1];
            complement = elements[2];
            replacement = elements[3];
            correspondence = elements[4];
            result = elements[5];
            residueOrTension = elements[6];
            return true;
        }

        source = default!;
        participation = default!;
        complement = default!;
        replacement = default!;
        correspondence = default!;
        result = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads the history when it is a time-step sequence.</summary>
    public static bool TryReadTimeline(Relation ladder, out Relation timeline)
    {
        if (TryRead(ladder, out _, out _, out var historyOrTimeline, out _, out _) &&
            historyOrTimeline is Relation candidate &&
            !IsUnknownPoint(candidate) &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    /// <summary>
    /// Reads the currently settled rung. Timeline after-states take priority;
    /// otherwise the last framed rung is used, then the base rung.
    /// </summary>
    public static bool TryReadCurrentRung(Relation ladder, out Relation currentRung)
    {
        if (!TryRead(ladder, out var baseRung, out var rungFrame, out _, out _, out _))
        {
            currentRung = null!;
            return false;
        }

        if (TryReadRungPath(ladder, out var path) &&
            path.Length > 0)
        {
            currentRung = path[^1];
            return true;
        }

        if (TryReadRungFrame(rungFrame, out var framedRungs) &&
            framedRungs.Length > 0)
        {
            currentRung = framedRungs[^1];
            return true;
        }

        currentRung = baseRung;
        return true;
    }

    /// <summary>
    /// Reads an ordered rung path. A timeline contributes its initial rung and
    /// each readable after-rung. Without a timeline, the rung frame is used.
    /// </summary>
    public static bool TryReadRungPath(Relation ladder, out Relation[] path)
    {
        if (!TryRead(ladder, out var baseRung, out var rungFrame, out var historyOrTimeline, out _, out _))
        {
            path = [];
            return false;
        }

        if (historyOrTimeline is Relation timeline &&
            !IsUnknownPoint(timeline) &&
            TimeStepSequence.TryRead(timeline, out _, out var initial, out var steps))
        {
            var rungs = new List<Relation>();
            if (initial is Relation initialRung &&
                TryReadRung(initialRung, out _, out _, out _, out _, out _, out _, out _, out _))
            {
                rungs.Add(initialRung);
            }

            foreach (var step in steps)
            {
                if (TryReadStep(step, out _, out _, out _, out _, out _, out var after, out _))
                {
                    rungs.Add(after);
                }
            }

            if (rungs.Count > 0)
            {
                path = [.. rungs];
                return true;
            }
        }

        if (TryReadRungFrame(rungFrame, out var framedRungs) &&
            framedRungs.Length > 0)
        {
            path = framedRungs;
            return true;
        }

        path = [baseRung];
        return true;
    }

    private static bool TryCreateFocusWitness(
        IElement focus,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation witness)
    {
        if (focus is Relation focusRelation)
        {
            return FocusSurface.TryCreateResultWitness(
                new Carrier(),
                focusRelation,
                result,
                residueOrTension,
                provenance,
                out witness);
        }

        witness = null!;
        return false;
    }

    private static bool TryCreateFocusRefinement(
        IElement focus,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation refinement)
    {
        if (focus is Relation focusRelation)
        {
            return FocusSurface.TryCreateRefinementRead(
                new Carrier(),
                focusRelation,
                result,
                residueOrTension,
                provenance,
                out refinement);
        }

        refinement = null!;
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

    private static bool IsUnknownPoint(IElement element)
    {
        return element is Relation relation &&
            relation.Left is CarrierLocation location &&
            relation.Right is Carrier carrier &&
            carrier.IsUnknown &&
            location.Carrier.IsUnknown &&
            location.Ticks == 0;
    }

    private static bool IsValidRungElements(IReadOnlyList<IElement> elements)
    {
        var source = elements[0];
        var focus = elements[1];
        var result = elements[2];
        var witness = elements[5];
        var refinement = elements[6];

        if (IsUnknownPoint(focus) &&
            Equals(source, result) &&
            IsUnknownPoint(witness) &&
            IsUnknownPoint(refinement))
        {
            return true;
        }

        return focus is Relation focusRelation &&
            FocusSurface.TryRead(focusRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            witness is Relation witnessRelation &&
            FocusSurface.TryReadResultWitness(
                witnessRelation,
                out var witnessedFocus,
                out _,
                out var witnessedResult,
                out _,
                out _) &&
            Equals(focusRelation, witnessedFocus) &&
            Equals(result, witnessedResult) &&
            refinement is Relation refinementRelation &&
            FocusSurface.TryReadRefinement(
                refinementRelation,
                out var refinedFocus,
                out var refinedResult,
                out _,
                out _,
                out _) &&
            Equals(focusRelation, refinedFocus) &&
            Equals(result, refinedResult);
    }

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
