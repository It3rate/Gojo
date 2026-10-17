using Gojo.Core;
using Gojo.Structures;
using System.Runtime.CompilerServices;

namespace GojoCAD.Structures;

/// <summary>
/// Relation-shaped concept surfaces for drawable structure before geometry is
/// parameterized. These helpers name construction patterns only; the resulting
/// Gojo structure carries the meaning through ports, evidence, residue, and
/// ladder relations.
/// </summary>
public static class GraphicConceptShape
{
    /// <summary>Creates an unresolved local site that can later settle into position evidence.</summary>
    public static Placeholder Site() => new();

    /// <summary>
    /// Creates a drawable port surface:
    /// site, exposed flow, tangent, orthogonal/normal, residue.
    /// </summary>
    public static Relation Port(Carrier root) =>
        Frame(
            root,
            Site(),
            FlowSlot(),
            TangentSlot(),
            OrthogonalSlot(),
            TensionSlot());

    /// <summary>Creates an unparameterized open drawable section between two ports.</summary>
    public static Relation Section(Carrier root) =>
        Section(root, Port(new Carrier()), TraceSlot(), Port(new Carrier()));

    /// <summary>Creates an unparameterized open drawable section from explicit port surfaces.</summary>
    public static Relation Section(Carrier root, IElement startPort, IElement trace, IElement endPort) =>
        Frame(
            root,
            startPort ?? Port(new Carrier()),
            trace ?? TraceSlot(),
            endPort ?? Port(new Carrier()),
            TensionSlot());

    /// <summary>Elaborates a section with straightness evidence.</summary>
    public static Relation StraightSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), StraightEvidence(new Carrier()));

    /// <summary>Creates straightness evidence for a section or composite-section refinement.</summary>
    public static Relation StraightEvidenceSurface(Carrier root) =>
        StraightEvidence(RootOrNew(root));

    /// <summary>Creates generic curve evidence for a section or composite-section refinement.</summary>
    public static Relation CurveEvidenceSurface(Carrier root) =>
        CurveEvidence(RootOrNew(root));

    /// <summary>Creates circular-arc evidence for a section or composite-section refinement.</summary>
    public static Relation CircularArcEvidenceSurface(Carrier root) =>
        CircularArcEvidence(RootOrNew(root));

    /// <summary>Creates Bezier control evidence for a section or composite-section refinement.</summary>
    public static Relation BezierEvidenceSurface(Carrier root, params IElement[] controls) =>
        BezierEvidence(RootOrNew(root), controls ?? []);

    /// <summary>Creates clothoid transition evidence for a section or composite-section refinement.</summary>
    public static Relation ClothoidEvidenceSurface(Carrier root) =>
        ClothoidEvidence(RootOrNew(root));

    /// <summary>Creates involute/unwinding evidence for a section or composite-section refinement.</summary>
    public static Relation InvoluteEvidenceSurface(Carrier root) =>
        InvoluteEvidence(RootOrNew(root));

    /// <summary>Creates spline/control-chain evidence for a section or composite-section refinement.</summary>
    public static Relation SplineEvidenceSurface(Carrier root) =>
        SplineEvidence(RootOrNew(root));

    /// <summary>Elaborates a section with generic curve evidence.</summary>
    public static Relation CurveSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), CurveEvidence(new Carrier()));

    /// <summary>Elaborates a section with circular-arc evidence.</summary>
    public static Relation CircularArcSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), CircularArcEvidence(new Carrier()));

    /// <summary>Elaborates a section with generic Bezier control evidence.</summary>
    public static Relation BezierSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), BezierEvidence(new Carrier(), ControlSlot(), ControlSlot()));

    /// <summary>Elaborates a section with one Bezier control slot.</summary>
    public static Relation QuadraticBezierSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), BezierEvidence(new Carrier(), ControlSlot()));

    /// <summary>Elaborates a section with two Bezier control slots.</summary>
    public static Relation CubicBezierSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), BezierEvidence(new Carrier(), ControlSlot(), ControlSlot()));

    /// <summary>Elaborates a section with clothoid transition evidence.</summary>
    public static Relation ClothoidSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), ClothoidEvidence(new Carrier()));

    /// <summary>Elaborates a section with involute/unwinding evidence.</summary>
    public static Relation InvoluteSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), InvoluteEvidence(new Carrier()));

    /// <summary>Elaborates a section with spline/control-chain evidence.</summary>
    public static Relation SplineSection(Carrier root) =>
        Elaborate(root, Section(new Carrier()), SplineEvidence(new Carrier()));

    /// <summary>Creates a higher rung that exposes direction/flow across a section.</summary>
    public static Relation WithDirection(Carrier root, IElement section) =>
        Elaborate(root, section, Frame(new Carrier(), FlowSlot(), DirectionSlot(), TensionSlot()));

    /// <summary>Creates a higher rung that exposes start/end tangent evidence.</summary>
    public static Relation WithEndpointTangents(Carrier root, IElement section) =>
        Elaborate(root, section, Frame(new Carrier(), TangentSlot(), TangentSlot(), TensionSlot()));

    /// <summary>Creates a higher rung that exposes orthogonal/normal evidence.</summary>
    public static Relation WithOrthogonal(Carrier root, IElement section) =>
        Elaborate(root, section, Relation.OrthogonalOutlet(section ?? TensionSlot()));

    /// <summary>Creates continuity evidence between two adjacent sections, then folds them into one section.</summary>
    public static Relation TangentJoin(Carrier root, IElement incoming, IElement outgoing) =>
        FoldSections(
            root,
            incoming,
            outgoing,
            Frame(new Carrier(), TangentSlot(), TangentSlot(), TensionSlot()));

    /// <summary>
    /// Creates cusp evidence between two adjacent sections, then folds them
    /// into one section while preserving the unresolved tangent continuity.
    /// </summary>
    public static Relation CuspJoin(Carrier root, IElement incoming, IElement outgoing) =>
        FoldSections(root, incoming, outgoing, CuspJoinEvidence(new Carrier()));

    /// <summary>
    /// Creates a structural cusp/corner evidence surface:
    /// incoming tangent, outgoing tangent, local orthogonal/normal,
    /// expected tangent continuity, residue/tension.
    /// </summary>
    public static Relation CuspJoinEvidenceSurface(Carrier root) =>
        CuspJoinEvidence(RootOrNew(root));

    /// <summary>
    /// Creates terminal/tip evidence for an open drawable path endpoint:
    /// local port, outgoing/ending flow, unresolved continuation residue.
    /// </summary>
    public static Relation LineTipEvidenceSurface(Carrier root) =>
        LineTipEvidence(RootOrNew(root));

    /// <summary>
    /// Creates circular cap evidence for a selected terminal/tip:
    /// terminal evidence, circular boundary evidence, local normal, residue.
    /// </summary>
    public static Relation CircularTipCapEvidenceSurface(Carrier root) =>
        CircularTipCapEvidence(RootOrNew(root));

    /// <summary>
    /// Creates rounded-cusp evidence for a selected cusp join:
    /// cusp evidence, circular transition evidence, local normal, residue.
    /// </summary>
    public static Relation RoundedCuspJoinEvidenceSurface(Carrier root) =>
        RoundedCuspJoinEvidence(RootOrNew(root));

    /// <summary>Creates an ambiguous connection between two drawable sections or paths, then folds them into one section.</summary>
    public static Relation Join(Carrier root, IElement incoming, IElement outgoing) =>
        Join(root, incoming, outgoing, TensionSlot());

    /// <summary>Creates an ambiguous connection with explicit join evidence, then folds it into a current section.</summary>
    public static Relation Join(Carrier root, IElement incoming, IElement outgoing, IElement joinEvidence) =>
        FoldSections(root, incoming, outgoing, joinEvidence);

    /// <summary>Creates raw port-to-port join evidence without folding a current surface.</summary>
    public static Relation PortJoin(Carrier root, IElement incomingPort, IElement outgoingPort) =>
        PortJoin(root, incomingPort, outgoingPort, TensionSlot());

    /// <summary>Creates raw port-to-port join evidence with explicit continuity or tension evidence.</summary>
    public static Relation PortJoin(Carrier root, IElement incomingPort, IElement outgoingPort, IElement joinEvidence) =>
        Frame(
            root,
            incomingPort ?? TensionSlot(),
            outgoingPort ?? TensionSlot(),
            joinEvidence ?? TensionSlot(),
            TensionSlot());

    /// <summary>Reads raw port-to-port join evidence created by <see cref="PortJoin(Carrier,IElement,IElement,IElement)"/>.</summary>
    public static bool TryReadPortJoin(
        Relation join,
        out IElement incomingPort,
        out IElement outgoingPort,
        out IElement joinEvidence,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(join, out var elements) &&
            elements.Length == 4 &&
            elements[0] is Relation incomingRelation &&
            elements[1] is Relation outgoingRelation &&
            TryReadPortConcept(incomingRelation, out _, out _, out _, out _, out _) &&
            TryReadPortConcept(outgoingRelation, out _, out _, out _, out _, out _))
        {
            incomingPort = elements[0];
            outgoingPort = elements[1];
            joinEvidence = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        incomingPort = default!;
        outgoingPort = default!;
        joinEvidence = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads cusp/corner evidence created by <see cref="CuspJoinEvidenceSurface"/>.</summary>
    public static bool TryReadCuspJoinEvidence(
        Relation evidence,
        out IElement incomingTangent,
        out IElement outgoingTangent,
        out IElement orthogonal,
        out IElement tangentContinuityExpectation,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(evidence, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation incomingTangentRelation &&
            elements[1] is Relation outgoingTangentRelation &&
            elements[2] is Relation orthogonalRelation &&
            elements[3] is Relation continuityRelation &&
            elements[4] is Relation residueRelation &&
            TryReadFrameElements(incomingTangentRelation, out var incomingTangentElements) &&
            incomingTangentElements.Length == 3 &&
            TryReadFrameElements(outgoingTangentRelation, out var outgoingTangentElements) &&
            outgoingTangentElements.Length == 3 &&
            TryReadFrameElements(continuityRelation, out var continuityElements) &&
            continuityElements.Length == 3 &&
            ReferenceEquals(continuityElements[0], elements[0]) &&
            ReferenceEquals(continuityElements[1], elements[1]))
        {
            incomingTangent = elements[0];
            outgoingTangent = elements[1];
            orthogonal = orthogonalRelation;
            tangentContinuityExpectation = continuityRelation;
            residueOrTension = residueRelation;
            return true;
        }

        incomingTangent = default!;
        outgoingTangent = default!;
        orthogonal = default!;
        tangentContinuityExpectation = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads rounded-cusp evidence created by <see cref="RoundedCuspJoinEvidenceSurface"/>.</summary>
    public static bool TryReadRoundedCuspJoinEvidence(
        Relation evidence,
        out IElement cuspEvidence,
        out IElement circularEvidence,
        out IElement orthogonal,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(evidence, out var elements) &&
            elements.Length == 4 &&
            elements[0] is Relation cuspRelation &&
            elements[1] is Relation circularRelation &&
            elements[2] is Relation orthogonalRelation &&
            elements[3] is Relation residueRelation &&
            TryReadCuspJoinEvidence(cuspRelation, out _, out _, out _, out _, out _) &&
            TryReadCircularArcEvidence(circularRelation, out _, out _, out _, out _))
        {
            cuspEvidence = elements[0];
            circularEvidence = elements[1];
            orthogonal = orthogonalRelation;
            residueOrTension = residueRelation;
            return true;
        }

        cuspEvidence = default!;
        circularEvidence = default!;
        orthogonal = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads circular-arc evidence created by <see cref="CircularArcEvidenceSurface"/>.</summary>
    public static bool TryReadCircularArcEvidence(
        Relation evidence,
        out IElement centerOrSite,
        out IElement offset,
        out IElement direction,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(evidence, out var elements) &&
            elements.Length == 4 &&
            elements[1] is Relation offsetRelation &&
            elements[2] is Relation directionRelation &&
            GraphicOffsetSurface.TryReadEvidence(
                offsetRelation,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _) &&
            TryReadDirectionSlot(directionRelation))
        {
            centerOrSite = elements[0];
            offset = offsetRelation;
            direction = directionRelation;
            residueOrTension = elements[3];
            return true;
        }

        centerOrSite = default!;
        offset = default!;
        direction = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads terminal/tip evidence created by <see cref="LineTipEvidenceSurface"/>.</summary>
    public static bool TryReadLineTipEvidence(
        Relation evidence,
        out IElement port,
        out IElement flow,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(evidence, out var elements) &&
            elements.Length == 3 &&
            elements[0] is Relation portRelation &&
            elements[1] is Relation flowRelation &&
            TryReadPortConcept(portRelation, out _, out _, out _, out _, out _))
        {
            port = portRelation;
            flow = flowRelation;
            residueOrTension = elements[2];
            return true;
        }

        port = default!;
        flow = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads circular terminal-cap evidence created by <see cref="CircularTipCapEvidenceSurface"/>.</summary>
    public static bool TryReadCircularTipCapEvidence(
        Relation evidence,
        out IElement terminalTip,
        out IElement circularBoundary,
        out IElement orthogonal,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(evidence, out var elements) &&
            elements.Length == 4 &&
            elements[0] is Relation terminalRelation &&
            elements[1] is Relation circularRelation &&
            elements[2] is Relation orthogonalRelation &&
            TryReadLineTipEvidence(terminalRelation, out _, out _, out _) &&
            TryReadCuspOrCircularEvidence(circularRelation))
        {
            terminalTip = terminalRelation;
            circularBoundary = circularRelation;
            orthogonal = orthogonalRelation;
            residueOrTension = elements[3];
            return true;
        }

        terminalTip = default!;
        circularBoundary = default!;
        orthogonal = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Folds two open sections into a new current two-port composite section.</summary>
    public static Relation FoldSections(Carrier root, IElement first, IElement second) =>
        FoldSections(root, first, second, TensionSlot());

    /// <summary>Folds two open sections into a new current two-port composite section.</summary>
    public static Relation FoldSections(Carrier root, IElement first, IElement second, IElement joinEvidence) =>
        FoldSections(root, first, second, joinEvidence, TensionSlot());

    /// <summary>
    /// Folds two open sections into a new current two-port composite section
    /// while carrying a prior composite as provenance.
    /// </summary>
    public static Relation FoldSections(
        Carrier root,
        IElement first,
        IElement second,
        IElement joinEvidence,
        IElement previousRung)
    {
        IElement firstStart = Port(new Carrier());
        IElement firstEnd = Port(new Carrier());
        IElement secondStart = Port(new Carrier());
        IElement secondEnd = Port(new Carrier());
        var residue = TensionSlot();

        if (first is Relation firstRelation &&
            TryReadSectionConcept(firstRelation, out var readFirstStart, out _, out var readFirstEnd, out _))
        {
            firstStart = readFirstStart;
            firstEnd = readFirstEnd;
        }

        if (second is Relation secondRelation &&
            TryReadSectionConcept(secondRelation, out var readSecondStart, out _, out var readSecondEnd, out _))
        {
            secondStart = readSecondStart;
            secondEnd = readSecondEnd;
        }

        var portJoin = PortJoin(new Carrier(), firstEnd, secondStart, joinEvidence ?? residue);
        var lowerStructure = Frame(new Carrier(), first ?? TensionSlot(), second ?? TensionSlot());
        var trace = Frame(new Carrier(), first ?? TensionSlot(), portJoin, second ?? TensionSlot());

        return CompositeSection(
            RootOrNew(root),
            firstStart,
            trace,
            secondEnd,
            lowerStructure,
            [portJoin],
            previousRung ?? TensionSlot(),
            residue);
    }

    /// <summary>Creates an ordered open path by repeatedly folding adjacent sections.</summary>
    public static Relation OpenPath(Carrier root, params IElement[] sections)
    {
        var orderedSections = sections ?? [];
        if (orderedSections.Length == 0)
        {
            return CompositeShape(
                RootOrNew(root),
                [],
                TensionSlot(),
                Frame(new Carrier()),
                [],
                TensionSlot(),
                TensionSlot());
        }

        if (orderedSections.Length == 1)
        {
            return SingleSectionComposite(root, orderedSections[0]);
        }

        var current = FoldSections(new Carrier(), orderedSections[0], orderedSections[1]);
        for (var index = 2; index < orderedSections.Length; index++)
        {
            current = FoldSections(
                index == orderedSections.Length - 1 ? RootOrNew(root) : new Carrier(),
                current,
                orderedSections[index],
                TensionSlot(),
                current);
        }

        return current;
    }

    /// <summary>Creates branch evidence: one incoming section surface to many outgoing surfaces.</summary>
    public static Relation Branch(Carrier root, IElement incoming, params IElement[] outgoing) =>
        Elaborate(
            root,
            JunctionFromDirectedSections(incoming, outgoing ?? [], merge: false),
            Frame(new Carrier(), FlowSlot(), DirectionSlot(), TensionSlot()));

    /// <summary>Creates merge evidence: many incoming section surfaces to one outgoing surface.</summary>
    public static Relation Merge(Carrier root, IReadOnlyList<IElement> incoming, IElement outgoing) =>
        Elaborate(
            root,
            JunctionFromDirectedSections(outgoing, incoming?.ToArray() ?? [], merge: true),
            Frame(new Carrier(), FlowSlot(), DirectionSlot(), TensionSlot()));

    /// <summary>
    /// Creates a generic current composite shape:
    /// exposed ports, visible surface, lower structure, interior joins,
    /// previous rung/provenance, residue/tension.
    /// </summary>
    public static Relation CompositeShape(
        Carrier root,
        IReadOnlyList<IElement> exposedPorts,
        IElement visibleSurface,
        IElement lowerStructure,
        IReadOnlyList<IElement> internalJoins,
        IElement previousRung,
        IElement residueOrTension) =>
        Frame(
            root,
            Frame(new Carrier(), exposedPorts?.ToArray() ?? []),
            visibleSurface ?? TensionSlot(),
            lowerStructure ?? TensionSlot(),
            Frame(new Carrier(), internalJoins?.ToArray() ?? []),
            previousRung ?? TensionSlot(),
            residueOrTension ?? TensionSlot());

    /// <summary>Creates a current two-port composite whose visible surface is a section.</summary>
    public static Relation CompositeSection(
        Carrier root,
        IElement startPort,
        IElement trace,
        IElement endPort,
        IElement lowerStructure,
        IReadOnlyList<IElement> internalJoins,
        IElement previousRung,
        IElement residueOrTension)
    {
        var start = startPort ?? Port(new Carrier());
        var end = endPort ?? Port(new Carrier());
        var visibleSection = Section(new Carrier(), start, trace ?? TraceSlot(), end);

        return CompositeShape(
            RootOrNew(root),
            [start, end],
            visibleSection,
            lowerStructure ?? visibleSection,
            internalJoins ?? [],
            previousRung ?? TensionSlot(),
            residueOrTension ?? TensionSlot());
    }

    /// <summary>Creates a current multi-port composite around an interior junction.</summary>
    public static Relation JunctionShape(
        Carrier root,
        IReadOnlyList<IElement> exposedPorts,
        IElement lowerStructure,
        IElement internalJunction,
        IElement previousRung,
        IElement residueOrTension) =>
        JunctionShape(
            root,
            exposedPorts,
            lowerStructure,
            internalJunction,
            [internalJunction ?? TensionSlot()],
            previousRung,
            residueOrTension);

    /// <summary>Creates a current multi-port composite around interior junction evidence.</summary>
    public static Relation JunctionShape(
        Carrier root,
        IReadOnlyList<IElement> exposedPorts,
        IElement lowerStructure,
        IElement visibleJunction,
        IReadOnlyList<IElement> internalJoins,
        IElement previousRung,
        IElement residueOrTension)
    {
        var visibleSurface = Frame(
            new Carrier(),
            Frame(new Carrier(), exposedPorts?.ToArray() ?? []),
            visibleJunction ?? TensionSlot(),
            TensionSlot());

        return CompositeShape(
            RootOrNew(root),
            exposedPorts ?? [],
            visibleSurface,
            lowerStructure ?? TensionSlot(),
            internalJoins ?? [],
            previousRung ?? TensionSlot(),
            residueOrTension ?? TensionSlot());
    }

    /// <summary>
    /// Attaches a section to an interior join of an existing composite,
    /// producing a new current multi-port junction while preserving the prior
    /// composite as provenance.
    /// </summary>
    public static Relation AttachSectionAtJoin(Carrier root, IElement composite, IElement internalJoin, IElement section)
    {
        var exposedPorts = new List<IElement>();
        var internalJoins = new List<IElement>();

        if (composite is Relation compositeRelation &&
            TryReadCompositeShape(
                compositeRelation,
                out var priorExposed,
                out _,
                out _,
                out var priorJoins,
                out _,
                out _))
        {
            exposedPorts.AddRange(priorExposed);
            internalJoins.AddRange(priorJoins);
        }
        else if (composite is Relation sectionRelation &&
            TryReadSectionConcept(sectionRelation, out var startPort, out _, out var endPort, out _))
        {
            exposedPorts.Add(startPort);
            exposedPorts.Add(endPort);
        }

        if (exposedPorts.Count == 0)
        {
            exposedPorts.Add(Port(new Carrier()));
            exposedPorts.Add(Port(new Carrier()));
        }

        IElement attachedStart = Port(new Carrier());
        IElement attachedEnd = Port(new Carrier());
        if (section is Relation attachedRelation &&
            TryReadSectionConcept(attachedRelation, out var readAttachedStart, out _, out var readAttachedEnd, out _))
        {
            attachedStart = readAttachedStart;
            attachedEnd = readAttachedEnd;
        }

        exposedPorts.Add(attachedEnd);
        var attachment = PortJoin(new Carrier(), internalJoin ?? TensionSlot(), attachedStart);
        internalJoins.Add(attachment);
        var visibleJunction = Frame(new Carrier(), internalJoin ?? TensionSlot(), attachedStart, attachment);
        var lowerStructure = Frame(new Carrier(), composite ?? TensionSlot(), section ?? TensionSlot());

        return JunctionShape(
            RootOrNew(root),
            exposedPorts,
            lowerStructure,
            visibleJunction,
            internalJoins,
            composite ?? TensionSlot(),
            TensionSlot());
    }

    /// <summary>Creates a closed-boundary elaboration over a section or path.</summary>
    public static Relation ClosedBoundary(Carrier root, IElement boundary) =>
        Elaborate(root, boundary, ClosureEvidence(new Carrier()));

    /// <summary>Creates positive area participation over a closed boundary concept.</summary>
    public static Relation Area(Carrier root, IElement closedBoundary) =>
        Elaborate(root, closedBoundary, AreaEvidence(new Carrier(), inverse: false));

    /// <summary>Creates inverse/anti-area participation over a closed boundary concept.</summary>
    public static Relation AntiArea(Carrier root, IElement closedBoundary) =>
        Elaborate(root, closedBoundary, AreaEvidence(new Carrier(), inverse: true));

    /// <summary>Creates thickened-section evidence without choosing offset distance yet.</summary>
    public static Relation ThickenedSection(Carrier root, IElement section) =>
        Elaborate(root, section, Frame(new Carrier(), OrthogonalSlot(), OffsetEvidence(new Carrier()), TensionSlot()));

    /// <summary>Creates parallel/offset evidence that can later settle as linear or circular offset.</summary>
    public static Relation OffsetSection(Carrier root, IElement section) =>
        Elaborate(root, section, OffsetEvidence(new Carrier()));

    /// <summary>Creates parallel/offset evidence with an explicit distance/radius surface.</summary>
    public static Relation OffsetSection(Carrier root, IElement section, IElement distance) =>
        Elaborate(root, section, GraphicOffsetSurface.Evidence(new Carrier(), distance));

    /// <summary>Creates parallel/offset evidence from an explicit offset evidence surface.</summary>
    public static Relation OffsetSectionWithEvidence(Carrier root, IElement section, IElement evidence) =>
        Elaborate(root, section, evidence ?? OffsetEvidence(new Carrier()));

    /// <summary>
    /// Creates an elaborated concept shape:
    /// lower rung, new evidence, unresolved residue/tension.
    /// </summary>
    public static Relation Elaborate(Carrier root, IElement lowerRung, IElement evidence) =>
        Frame(
            root,
            lowerRung ?? TensionSlot(),
            evidence ?? TensionSlot(),
            TensionSlot());

    /// <summary>
    /// Records an elaboration as a structural ladder. The output remains a
    /// normal ladder surface, so lower and higher rungs stay independently
    /// traversable.
    /// </summary>
    public static Relation ElaborationLadder(Carrier root, IElement lowerRung, IElement higherRung, IElement evidence)
    {
        var lower = lowerRung ?? TensionSlot();
        var higher = higherRung ?? Elaborate(new Carrier(), lower, evidence);
        var focus = FocusSurface.Create(new Carrier(), lower, evidence ?? TensionSlot(), lower);
        var correspondence = FocusSurface.Bind(lower, higher);

        return StructuralLadderSurface.TryCreateRefinedRungFromFocus(
            new Carrier(),
            focus,
            higher,
            correspondence,
            TensionSlot(),
            evidence ?? TensionSlot(),
            out var elaborationRung)
            ? StructuralLadderSurface.CreateWithRungs(
                RootOrNew(root),
                StructuralLadderSurface.BaseRung(new Carrier(), lower),
                elaborationRung)
            : StructuralLadderSurface.CreateWithRungs(
                RootOrNew(root),
                StructuralLadderSurface.BaseRung(new Carrier(), lower));
    }

    /// <summary>Creates an ordered Gojo frame for local graphic concept evidence.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>Reads a concept port surface created by <see cref="Port"/>.</summary>
    public static bool TryReadPortConcept(Relation port, out IElement site, out IElement flow, out IElement tangent, out IElement orthogonal, out IElement residueOrTension)
    {
        if (TryReadFrameElements(port, out var elements) &&
            elements.Length == 5)
        {
            site = elements[0];
            flow = elements[1];
            tangent = elements[2];
            orthogonal = elements[3];
            residueOrTension = elements[4];
            return true;
        }

        site = default!;
        flow = default!;
        tangent = default!;
        orthogonal = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads a section concept, following elaboration and composite visible surfaces locally.</summary>
    public static bool TryReadSectionConcept(
        Relation section,
        out IElement startPort,
        out IElement trace,
        out IElement endPort,
        out IElement residueOrTension) =>
        TryReadSectionConcept(section, [], out startPort, out trace, out endPort, out residueOrTension);

    /// <summary>Reads a current composite concept shape.</summary>
    public static bool TryReadCompositeShape(
        Relation shape,
        out IElement[] exposedPorts,
        out IElement visibleSurface,
        out IElement lowerStructure,
        out IElement[] internalJoins,
        out IElement previousRung,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(shape, out var elements) &&
            elements.Length == 6 &&
            elements[0] is Relation exposedFrame &&
            elements[3] is Relation joinFrame &&
            TryReadFrameElements(exposedFrame, out exposedPorts) &&
            TryReadFrameElements(joinFrame, out internalJoins))
        {
            visibleSurface = elements[1];
            lowerStructure = elements[2];
            previousRung = elements[4];
            residueOrTension = elements[5];
            return true;
        }

        if (TryReadFrameElements(shape, out elements) &&
            elements.Length == 3 &&
            elements[0] is Relation lowerComposite &&
            TryReadCompositeShape(
                lowerComposite,
                out exposedPorts,
                out visibleSurface,
                out lowerStructure,
                out internalJoins,
                out previousRung,
                out residueOrTension))
        {
            return true;
        }

        exposedPorts = [];
        visibleSurface = default!;
        lowerStructure = default!;
        internalJoins = [];
        previousRung = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>
    /// Discovers a current composite concept shape from existing section/join
    /// evidence. This does not claim original provenance; it builds a fresh
    /// concept surface from shared ports and explicit port joins.
    /// </summary>
    public static bool TryDiscoverCompositeShape(IElement structure, out Relation composite)
    {
        if (structure is Relation existingComposite &&
            TryReadCompositeShape(
                existingComposite,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            composite = existingComposite;
            return true;
        }

        var sections = new List<DiscoveredSection>();
        var explicitJoins = new List<IElement>();
        CollectLocalCompositeEvidence(structure, sections, explicitJoins);

        if (sections.Count == 0)
        {
            composite = null!;
            return false;
        }

        if (sections.Count == 1)
        {
            composite = SingleSectionComposite(new Carrier(), sections[0].Source);
            return true;
        }

        var endpoints = sections
            .SelectMany(section => new[]
            {
                new EndpointUse(section, section.StartPort),
                new EndpointUse(section, section.EndPort)
            })
            .ToArray();
        var consumedPorts = new HashSet<IElement>(ElementReferenceComparer.Instance);
        var internalJoins = new List<IElement>();

        foreach (var explicitJoin in explicitJoins)
        {
            internalJoins.Add(explicitJoin);
            if (explicitJoin is Relation joinRelation &&
                TryReadPortJoin(joinRelation, out var incomingPort, out var outgoingPort, out _, out _))
            {
                consumedPorts.Add(incomingPort);
                consumedPorts.Add(outgoingPort);
            }
        }

        foreach (var group in endpoints.GroupBy(endpoint => endpoint.Port, ElementReferenceComparer.Instance))
        {
            var uses = group.ToArray();
            if (uses.Length < 2)
            {
                continue;
            }

            consumedPorts.Add(group.Key);
            internalJoins.Add(SharedPortJoin(group.Key, uses.Select(use => use.Section.Source).ToArray()));
        }

        var exposedPorts = endpoints
            .Where(endpoint => !consumedPorts.Contains(endpoint.Port))
            .Select(endpoint => endpoint.Port)
            .Distinct(ElementReferenceComparer.Instance)
            .ToArray();
        var lowerStructure = structure ?? Frame(new Carrier(), sections.Select(section => section.Source).ToArray());
        var joinFrame = Frame(new Carrier(), [.. internalJoins]);

        composite = exposedPorts.Length == 2
            ? CompositeSection(
                new Carrier(),
                exposedPorts[0],
                Frame(new Carrier(), lowerStructure, joinFrame, TensionSlot()),
                exposedPorts[1],
                lowerStructure,
                internalJoins,
                TensionSlot(),
                TensionSlot())
            : JunctionShape(
                new Carrier(),
                exposedPorts,
                lowerStructure,
                joinFrame,
                internalJoins,
                TensionSlot(),
                TensionSlot());
        return true;
    }

    /// <summary>
    /// Creates a new current two-port section surface over an existing
    /// composite. The child sections and joins remain lower structure; the
    /// supplied evidence becomes the higher visible section.
    /// </summary>
    public static Relation RefineCompositeSection(Carrier root, IElement composite, IElement sectionEvidence)
    {
        var sourceComposite = TryReadCompositeShape(
                composite as Relation ?? TensionSlot(),
                out _,
                out _,
                out _,
                out _,
                out _,
                out _)
            ? composite
            : TryDiscoverCompositeShape(composite, out var discovered)
                ? discovered
                : SingleSectionComposite(new Carrier(), composite ?? TensionSlot());

        if (sourceComposite is not Relation sourceRelation ||
            !TryReadCompositeShape(
                sourceRelation,
                out var exposedPorts,
                out var visibleSurface,
                out var lowerStructure,
                out var internalJoins,
                out _,
                out var residueOrTension))
        {
            return Elaborate(RootOrNew(root), composite ?? TensionSlot(), sectionEvidence ?? TensionSlot());
        }

        if (exposedPorts.Length != 2)
        {
            return CompositeShape(
                RootOrNew(root),
                exposedPorts,
                Elaborate(new Carrier(), visibleSurface, sectionEvidence ?? TensionSlot()),
                Frame(new Carrier(), lowerStructure, sourceRelation),
                internalJoins,
                sourceRelation,
                residueOrTension ?? TensionSlot());
        }

        var refinedTrace = Frame(
            new Carrier(),
            visibleSurface,
            sectionEvidence ?? TensionSlot(),
            TensionSlot());
        var refinedBase = Section(new Carrier(), exposedPorts[0], refinedTrace, exposedPorts[1]);
        var refinedVisible = Elaborate(new Carrier(), refinedBase, sectionEvidence ?? TensionSlot());

        return CompositeShape(
            RootOrNew(root),
            exposedPorts,
            refinedVisible,
            Frame(new Carrier(), lowerStructure, visibleSurface, sourceRelation),
            internalJoins,
            sourceRelation,
            residueOrTension ?? TensionSlot());
    }

    /// <summary>Creates unresolved opposed-face residue/tension.</summary>
    public static Relation TensionSlot()
    {
        var slot = Site();
        return new Relation(slot, slot.Dual());
    }

    private static Relation TraceSlot() =>
        Frame(new Carrier(), Site(), TensionSlot());

    private static Relation FlowSlot()
    {
        var flow = Site();
        return new Relation(flow.Dual(), flow);
    }

    private static Relation DirectionSlot() =>
        Frame(new Carrier(), FlowSlot(), Site(), TensionSlot());

    private static Relation TangentSlot() =>
        Frame(new Carrier(), Site(), FlowSlot(), TensionSlot());

    private static Relation OrthogonalSlot() =>
        Relation.OrthogonalOutlet(Site());

    private static Relation CuspJoinEvidence(Carrier root)
    {
        var incomingTangent = TangentSlot();
        var outgoingTangent = TangentSlot();
        return Frame(
            root,
            incomingTangent,
            outgoingTangent,
            OrthogonalSlot(),
            Frame(new Carrier(), incomingTangent, outgoingTangent, TensionSlot()),
            TensionSlot());
    }

    private static Relation LineTipEvidence(Carrier root) =>
        Frame(root, Port(new Carrier()), FlowSlot(), TensionSlot());

    private static Relation CircularTipCapEvidence(Carrier root) =>
        Frame(
            root,
            LineTipEvidence(new Carrier()),
            CircularArcEvidence(new Carrier()),
            OrthogonalSlot(),
            TensionSlot());

    private static Relation RoundedCuspJoinEvidence(Carrier root) =>
        Frame(
            root,
            CuspJoinEvidence(new Carrier()),
            CircularArcEvidence(new Carrier()),
            OrthogonalSlot(),
            TensionSlot());

    private static bool TryReadCuspOrCircularEvidence(Relation evidence) =>
        TryReadCircularArcEvidence(evidence, out _, out _, out _, out _);

    private static bool TryReadDirectionSlot(Relation direction) =>
        TryReadFrameElements(direction, out var elements) &&
        elements.Length == 3 &&
        elements[0] is Relation;

    private static Relation ControlSlot() =>
        Frame(new Carrier(), Site(), TangentSlot(), TensionSlot());

    private static Relation StraightEvidence(Carrier root) =>
        Frame(root, FlowSlot(), TensionSlot());

    private static Relation CurveEvidence(Carrier root) =>
        Frame(root, TraceSlot(), TangentSlot(), TensionSlot());

    private static Relation CircularArcEvidence(Carrier root) =>
        Frame(root, Site(), OffsetEvidence(new Carrier()), DirectionSlot(), TensionSlot());

    private static Relation BezierEvidence(Carrier root, params IElement[] controls) =>
        Frame(root, Frame(new Carrier(), controls ?? []), TangentSlot(), TensionSlot());

    private static Relation ClothoidEvidence(Carrier root) =>
        Frame(
            root,
            CircularArcEvidence(new Carrier()),
            CircularArcEvidence(new Carrier()),
            DirectionSlot(),
            OffsetEvidence(new Carrier()),
            TensionSlot());

    private static Relation InvoluteEvidence(Carrier root) =>
        Frame(
            root,
            CircularArcEvidence(new Carrier()),
            TangentSlot(),
            OffsetEvidence(new Carrier()),
            TensionSlot());

    private static Relation SplineEvidence(Carrier root) =>
        Frame(root, Frame(new Carrier(), ControlSlot(), ControlSlot(), ControlSlot()), TensionSlot());

    private static Relation OffsetEvidence(Carrier root) =>
        GraphicOffsetSurface.Evidence(root);

    private static Relation ClosureEvidence(Carrier root)
    {
        var closureSite = Site();
        return Frame(root, new Relation(closureSite, closureSite), TensionSlot());
    }

    private static Relation AreaEvidence(Carrier root, bool inverse)
    {
        var inside = Site();
        var outside = inside.Dual();
        return inverse
            ? Frame(root, outside, inside, TensionSlot())
            : Frame(root, inside, outside, TensionSlot());
    }

    private static Relation SingleSectionComposite(Carrier root, IElement section)
    {
        if (section is Relation relation &&
            TryReadSectionConcept(relation, out var startPort, out var trace, out var endPort, out var residue))
        {
            return CompositeSection(
                RootOrNew(root),
                startPort,
                trace,
                endPort,
                section,
                [],
                TensionSlot(),
                residue);
        }

        return CompositeShape(
            RootOrNew(root),
            [],
            section ?? TensionSlot(),
            section ?? TensionSlot(),
            [],
            TensionSlot(),
            TensionSlot());
    }

    private static void CollectLocalCompositeEvidence(
        IElement structure,
        List<DiscoveredSection> sections,
        List<IElement> explicitJoins)
    {
        if (structure is not Relation relation)
        {
            return;
        }

        if (TryReadFrameElements(relation, out var elements))
        {
            var directSections = new List<DiscoveredSection>();
            var directJoins = new List<IElement>();
            foreach (var element in elements)
            {
                if (element is not Relation child)
                {
                    continue;
                }

                if (TryReadSectionConcept(child, out var childStartPort, out _, out var childEndPort, out _))
                {
                    directSections.Add(new DiscoveredSection(child, childStartPort, childEndPort));
                }
                else if (TryReadPortJoin(child, out _, out _, out _, out _))
                {
                    directJoins.Add(child);
                }
            }

            if (directSections.Count > 0 || directJoins.Count > 0)
            {
                sections.AddRange(directSections);
                explicitJoins.AddRange(directJoins);
                return;
            }
        }

        if (TryReadSectionConcept(relation, out var startPort, out _, out var endPort, out _))
        {
            sections.Add(new DiscoveredSection(relation, startPort, endPort));
            return;
        }

        if (TryReadPortJoin(relation, out _, out _, out _, out _))
        {
            explicitJoins.Add(relation);
        }
    }

    private static Relation SharedPortJoin(IElement sharedPort, IReadOnlyList<IElement> participants) =>
        Frame(
            new Carrier(),
            sharedPort ?? TensionSlot(),
            Frame(new Carrier(), participants?.ToArray() ?? []),
            TensionSlot());

    private static Relation JunctionFromDirectedSections(IElement primary, IReadOnlyList<IElement> secondary, bool merge)
    {
        var exposed = new List<IElement>();
        var consumed = new List<IElement>();
        var lower = new List<IElement> { primary ?? TensionSlot() };
        lower.AddRange(secondary ?? []);

        if (primary is Relation primaryRelation &&
            TryReadSectionConcept(primaryRelation, out var primaryStart, out _, out var primaryEnd, out _))
        {
            if (merge)
            {
                consumed.Add(primaryStart);
                exposed.Add(primaryEnd);
            }
            else
            {
                exposed.Add(primaryStart);
                consumed.Add(primaryEnd);
            }
        }
        else
        {
            exposed.Add(Port(new Carrier()));
            consumed.Add(Port(new Carrier()));
        }

        foreach (var section in secondary ?? [])
        {
            if (section is Relation relation &&
                TryReadSectionConcept(relation, out var start, out _, out var end, out _))
            {
                if (merge)
                {
                    exposed.Add(start);
                    consumed.Add(end);
                }
                else
                {
                    consumed.Add(start);
                    exposed.Add(end);
                }
            }
            else
            {
                consumed.Add(Port(new Carrier()));
                exposed.Add(Port(new Carrier()));
            }
        }

        var junction = Frame(new Carrier(), [.. consumed]);
        return JunctionShape(
            new Carrier(),
            exposed,
            Frame(new Carrier(), [.. lower]),
            junction,
            TensionSlot(),
            TensionSlot());
    }

    private static bool TryReadSectionConcept(
        Relation section,
        HashSet<ulong> visited,
        out IElement startPort,
        out IElement trace,
        out IElement endPort,
        out IElement residueOrTension)
    {
        if (!visited.Add(section.Id))
        {
            startPort = default!;
            trace = default!;
            endPort = default!;
            residueOrTension = default!;
            return false;
        }

        if (TryReadCompositeShape(
                section,
                out _,
                out var visibleSurface,
                out _,
                out _,
                out _,
                out _) &&
            visibleSurface is Relation visibleRelation &&
            TryReadSectionConcept(visibleRelation, visited, out startPort, out trace, out endPort, out residueOrTension))
        {
            return true;
        }

        if (TryReadFrameElements(section, out var elements) &&
            elements.Length == 4 &&
            elements[0] is Relation startRelation &&
            elements[2] is Relation endRelation &&
            TryReadPortConcept(startRelation, out _, out _, out _, out _, out _) &&
            TryReadPortConcept(endRelation, out _, out _, out _, out _, out _))
        {
            startPort = elements[0];
            trace = elements[1];
            endPort = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        if (TryReadFrameElements(section, out elements) &&
            elements.Length == 3 &&
            elements[0] is Relation lower &&
            TryReadSectionConcept(lower, visited, out startPort, out trace, out endPort, out residueOrTension))
        {
            return true;
        }

        startPort = default!;
        trace = default!;
        endPort = default!;
        residueOrTension = default!;
        return false;
    }

    private static bool TryReadFrameElements(Relation frame, out IElement[] elements)
    {
        if (frame?.Right is Carrier holder &&
            frame.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Carrier RootOrNew(Carrier root) => root ?? new Carrier();

    private sealed record DiscoveredSection(IElement Source, IElement StartPort, IElement EndPort);

    private sealed record EndpointUse(DiscoveredSection Section, IElement Port);

    private sealed class ElementReferenceComparer : IEqualityComparer<IElement>
    {
        public static readonly ElementReferenceComparer Instance = new();

        private ElementReferenceComparer()
        {
        }

        public bool Equals(IElement? x, IElement? y) => ReferenceEquals(x, y);

        public int GetHashCode(IElement obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
