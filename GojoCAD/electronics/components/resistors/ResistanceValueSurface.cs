using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;

namespace GojoCAD.Electronics.Components.Resistors;

/// <summary>
/// First relation-shaped foothold for resistor value encodings. The helpers
/// build ordinary Gojo structure for E6 nominal values, relative tolerance
/// resolution, and zero-ohm bridge reads.
/// </summary>
public static class ResistanceValueSurface
{
    public const long E6StepsPerDecade = 6;

    private static readonly string[] E6PhaseLabels = ["1.0", "1.5", "2.2", "3.3", "4.7", "6.8"];

    /// <summary>
    /// Creates a nominal resistance value as a repeated E6 transform position.
    /// The step decomposition is a read witness whose result is a radix-6
    /// resolution ladder, so phase and decade carry are Gojo carrier state.
    /// </summary>
    public static Relation E6Nominal(
        Carrier root,
        IElement selectedResistanceParameter,
        long e6Steps,
        IElement? referenceRung = null,
        IElement? residueOrTension = null,
        IElement? provenance = null,
        int carryLevelCount = 4)
    {
        var residue = residueOrTension ?? UnknownPoint();
        var reference = referenceRung ?? E6ReferenceRung(new Carrier());
        var repeat = E6Repeat(new Carrier(), selectedResistanceParameter, reference, e6Steps, residue);
        var carryRead = E6CarryRead(new Carrier(), repeat, e6Steps, residue, provenance, carryLevelCount);

        return Relation.OrderedGroup(
            RootOrNew(root),
            selectedResistanceParameter ?? UnknownPoint(),
            reference,
            repeat,
            carryRead,
            residue,
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates relative tolerance resolution such as one part in 20 for 5%.
    /// This does not encode exact ohm endpoints.
    /// </summary>
    public static Relation RelativeTolerance(
        Carrier root,
        IElement selectedNominalRead,
        long partsPerNominal,
        IElement? residueOrTension = null,
        IElement? provenance = null)
    {
        var relativeAxis = new Carrier();
        return Relation.OrderedGroup(
            RootOrNew(root),
            selectedNominalRead ?? UnknownPoint(),
            Relation.Segment(relativeAxis, 0, Math.Max(0, partsPerNominal)),
            Relation.Point(relativeAxis, 1),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates a zero-ohm resistor as a bridge/collapsed-route read, separate
    /// from the positive E-series ladder.
    /// </summary>
    public static Relation ZeroOhmBridge(
        Carrier root,
        IElement terminalA,
        IElement terminalB,
        IElement? residueOrTension = null,
        IElement? provenance = null)
    {
        var bridge = new Relation(terminalA ?? UnknownPoint(), terminalB ?? UnknownPoint());
        return Relation.OrderedGroup(
            RootOrNew(root),
            terminalA ?? UnknownPoint(),
            terminalB ?? UnknownPoint(),
            bridge,
            PositiveLadderNotSelected(new Carrier()),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>Reads a nominal E6 value surface.</summary>
    public static bool TryReadE6Nominal(
        Relation nominal,
        out IElement selectedResistanceParameter,
        out IElement referenceRung,
        out Relation repeat,
        out Relation carryRead,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(nominal, out var elements) &&
            elements.Length >= 6 &&
            elements[2] is Relation repeatRelation &&
            RepeatSurface.TryRead(repeatRelation, out _, out _, out _, out _, out _, out _) &&
            elements[3] is Relation carryReadRelation &&
            ReadWitness.TryRead(carryReadRelation, out _, out _, out _, out _, out _))
        {
            selectedResistanceParameter = elements[0];
            referenceRung = elements[1];
            repeat = repeatRelation;
            carryRead = carryReadRelation;
            residueOrTension = elements[4];
            provenance = elements[5];
            return true;
        }

        selectedResistanceParameter = default!;
        referenceRung = default!;
        repeat = null!;
        carryRead = null!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads the E6 step count from the repeat controller.</summary>
    public static bool TryReadE6StepCount(Relation repeat, out CarrierLocation stepCount)
    {
        if (RepeatSurface.TryRead(
                repeat,
                out _,
                out var controller,
                out _,
                out _,
                out _,
                out _) &&
            RepeatSurface.TryReadSpan(controller, out _, out var end))
        {
            stepCount = end;
            return true;
        }

        stepCount = default;
        return false;
    }

    /// <summary>
    /// Reads the Gojo-computed radix-6 phase and decade carry from an E6 value.
    /// </summary>
    public static bool TryReadE6Position(
        Relation nominal,
        out long steps,
        out long phase,
        out long decadeCarry)
    {
        if (TryReadE6Nominal(
                nominal,
                out _,
                out _,
                out var repeat,
                out var carryRead,
                out _,
                out _) &&
            TryReadE6StepCount(repeat, out var stepCount) &&
            TryReadE6CarryRead(carryRead, out var levels))
        {
            steps = stepCount.Ticks;
            phase = ReadLevel(levels, 0);
            decadeCarry = ReadLevel(levels, 1);
            return true;
        }

        steps = 0;
        phase = 0;
        decadeCarry = 0;
        return false;
    }

    /// <summary>Reads the resolution-ladder result from an E6 carry witness.</summary>
    public static bool TryReadE6CarryRead(Relation carryRead, out Carrier[] levels)
    {
        if (ReadWitness.TryRead(
                carryRead,
                out _,
                out _,
                out var result,
                out _,
                out _) &&
            result is Relation ladder &&
            ladder.Right is Carrier holder &&
            ResolutionLadder.TryReadLevels(ladder, holder, out levels))
        {
            return true;
        }

        levels = [];
        return false;
    }

    /// <summary>Reads the conventional E6 phase label for inspection.</summary>
    public static bool TryReadE6PhaseLabel(long phase, out string label)
    {
        if (phase >= 0 && phase < E6PhaseLabels.Length)
        {
            label = E6PhaseLabels[phase];
            return true;
        }

        label = string.Empty;
        return false;
    }

    /// <summary>Reads a relative tolerance resolution surface.</summary>
    public static bool TryReadRelativeTolerance(
        Relation tolerance,
        out IElement selectedNominalRead,
        out Relation relativeResolutionSpan,
        out CarrierLocation selectedGrain,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(tolerance, out var elements) &&
            elements.Length >= 5 &&
            elements[1] is Relation span &&
            TryReadPoint(elements[2], out selectedGrain))
        {
            selectedNominalRead = elements[0];
            relativeResolutionSpan = span;
            residueOrTension = elements[3];
            provenance = elements[4];
            return true;
        }

        selectedNominalRead = default!;
        relativeResolutionSpan = null!;
        selectedGrain = default;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads a zero-ohm bridge surface.</summary>
    public static bool TryReadZeroOhmBridge(
        Relation zeroOhm,
        out IElement terminalA,
        out IElement terminalB,
        out Relation bridge,
        out IElement positiveLadderEvidence,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(zeroOhm, out var elements) &&
            elements.Length >= 6 &&
            elements[2] is Relation bridgeRelation)
        {
            terminalA = elements[0];
            terminalB = elements[1];
            bridge = bridgeRelation;
            positiveLadderEvidence = elements[3];
            residueOrTension = elements[4];
            provenance = elements[5];
            return true;
        }

        terminalA = default!;
        terminalB = default!;
        bridge = null!;
        positiveLadderEvidence = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    private static Relation E6Repeat(
        Carrier root,
        IElement selectedResistanceParameter,
        IElement referenceRung,
        long e6Steps,
        IElement residueOrTension)
    {
        var stepAxis = RepeatSurface.CreateAxis(new Carrier());
        return RepeatSurface.Create(
            RootOrNew(root),
            E6StepTemplate(new Carrier(), selectedResistanceParameter, referenceRung),
            RepeatSurface.Count(stepAxis, Math.Max(0, e6Steps)),
            E6StepTransform(new Carrier()),
            E6CarryFeedback(new Carrier()),
            residueOrTension ?? UnknownPoint());
    }

    private static Relation E6CarryRead(
        Carrier root,
        IElement selected,
        long e6Steps,
        IElement residueOrTension,
        IElement? provenance,
        int carryLevelCount)
    {
        var ladderRoot = new Carrier();
        var ladder = ResolutionLadder.AttachCarry(
            ladderRoot,
            Math.Max(2, carryLevelCount),
            E6StepsPerDecade,
            out var levels,
            out _);

        if (e6Steps >= 0)
        {
            ResolutionLadder.AddToLevel(levels, 0, (ulong)e6Steps);
        }

        var residue = e6Steps >= 0
            ? residueOrTension ?? UnknownPoint()
            : Relation.Point(new Carrier(), e6Steps);

        return ReadWitness.Create(
            RootOrNew(root),
            selected ?? UnknownPoint(),
            E6CarryReadSurface(new Carrier()),
            ladder,
            residue,
            provenance ?? UnknownPoint());
    }

    private static Relation E6StepTemplate(Carrier root, IElement selectedResistanceParameter, IElement referenceRung) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            selectedResistanceParameter ?? UnknownPoint(),
            referenceRung ?? UnknownPoint(),
            E6Text("one-e6-step"));

    private static Relation E6StepTransform(Carrier root)
    {
        var stepAxis = new Carrier();
        var decadeAxis = new Carrier();
        return Relation.OrderedGroup(
            RootOrNew(root),
            Relation.Segment(stepAxis, 0, 1),
            new Relation(
                Relation.Segment(stepAxis, 0, E6StepsPerDecade),
                Relation.Point(decadeAxis, 1)),
            E6Text("e6-repeated-log-step"));
    }

    private static Relation E6CarryFeedback(Carrier root)
    {
        var phaseAxis = new Carrier();
        var decadeAxis = new Carrier();
        return Relation.OrderedGroup(
            RootOrNew(root),
            new Relation(
                Relation.Segment(phaseAxis, 0, E6StepsPerDecade),
                Relation.Point(decadeAxis, 1)),
            E6Text("six-e6-steps-carry-one-decade"));
    }

    private static Relation E6CarryReadSurface(Carrier root) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            E6Text("e6-carry-read"),
            Relation.Segment(new Carrier(), 0, E6StepsPerDecade));

    private static Relation E6ReferenceRung(Carrier root) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            E6Text("e6-reference-rung"),
            Relation.Point(new Carrier(), 0));

    private static Relation PositiveLadderNotSelected(Carrier root) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            E6Text("positive-e-series-ladder-not-selected"),
            UnknownPoint());

    private static Relation E6Text(string text) =>
        EcadTextSurface.Create(new Carrier(), text);

    private static long ReadLevel(IReadOnlyList<Carrier> levels, int index)
    {
        if (index < 0 || index >= levels.Count)
        {
            return 0;
        }

        return levels[index].Value > long.MaxValue
            ? long.MaxValue
            : (long)levels[index].Value;
    }

    private static Relation UnknownPoint() => Relation.Point(Carrier.Unknown, 0);

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier is null || carrier.IsUnknown ? new Carrier() : carrier;

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

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }
}
