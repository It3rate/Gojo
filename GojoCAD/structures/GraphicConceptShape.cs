using Gojo.Core;
using Gojo.Structures;

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

    private static Relation ControlSlot() =>
        Frame(new Carrier(), Site(), TangentSlot(), TensionSlot());

    private static Relation StraightEvidence(Carrier root) =>
        Frame(root, FlowSlot(), TensionSlot());

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
        Frame(root, OrthogonalSlot(), TensionSlot());

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
}
