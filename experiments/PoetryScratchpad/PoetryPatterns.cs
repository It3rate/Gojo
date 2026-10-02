using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

namespace PoetryScratchpad;

internal static class PoetryPatterns
{
    internal static void Run(Relation handsSource, Relation sunSource, Action<string, IElement> record)
    {
        HandsAndPlanets(handsSource, record);
        SunAndRepetition(sunSource, record);
    }

    private static void HandsAndPlanets(Relation source, Action<string, IElement> record)
    {
        // These are an analyst's proposed roles. The source line does not
        // supply a physical size calibration, orbital system, or anatomy model.
        var handPhysical = new Carrier();
        var planetPhysical = new Carrier();
        var bodyPhysical = new Carrier();
        var skinPhysical = new Carrier();
        var handRoles = new Carrier();
        var planetRoles = new Carrier();
        var leftHandRole = Relation.Point(handRoles, 1);
        var rightHandRole = Relation.Point(handRoles, 2);
        var bodyCenterRole = Relation.Point(handRoles, 0);
        var firstPlanetRole = Relation.Point(planetRoles, 1);
        var secondPlanetRole = Relation.Point(planetRoles, 2);
        var planetaryCenterRole = Relation.Point(planetRoles, 0);
        var skinAndBody = new Relation(skinPhysical, bodyPhysical);
        var physicalGrounding = Group(
            new Relation(leftHandRole, handPhysical),
            new Relation(rightHandRole, handPhysical),
            new Relation(bodyCenterRole, bodyPhysical),
            new Relation(firstPlanetRole, planetPhysical),
            new Relation(secondPlanetRole, planetPhysical),
            skinAndBody);
        var selected = Group(
            new Relation(leftHandRole, bodyCenterRole),
            new Relation(rightHandRole, bodyCenterRole),
            skinAndBody);
        var target = Group(
            new Relation(firstPlanetRole, planetaryCenterRole),
            new Relation(secondPlanetRole, planetaryCenterRole),
            planetPhysical);
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(leftHandRole, firstPlanetRole),
            FocusSurface.Bind(rightHandRole, secondPlanetRole),
            FocusSurface.Bind(bodyCenterRole, planetaryCenterRole));
        var scaleCalibration = new Relation(
            new Relation(handPhysical, planetPhysical), new Placeholder());
        var unresolved = Group(scaleCalibration, skinAndBody);
        var provenance = Group(source, physicalGrounding,
            Text("Analyst-supplied partial analogy: two peripheral roles and a center. " +
                 "This is one candidate reading, not a discovered orbital claim. " +
                 "Skin/body material participation and physical scale are not mapped."));
        var focus = FocusSurface.Create(new Carrier(), source, handRoles,
            selected, physicalGrounding, new Placeholder(), correspondence,
            unresolved, provenance);

        Require(FocusSurface.TryProject(focus, new Carrier(), target,
            correspondence, unresolved, out var projected, out var projection),
            "Partial analogy projection did not produce a focus.");
        Require(FocusSurface.TryRead(projected, out _, out _, out var participation,
            out _, out _, out _, out var residue, out _),
            "Projected analogy focus was not readable.");
        Require(FocusSurface.TryReadFrame(participation, out var parts) &&
            parts.Length == 3 &&
            parts[0].Equals(new Relation(firstPlanetRole, planetaryCenterRole)) &&
            parts[1].Equals(new Relation(secondPlanetRole, planetaryCenterRole)) &&
            parts[2].Equals(skinAndBody) && ReferenceEquals(residue, unresolved),
            "Partial mapping lost either mapped roles or unmapped material.");

        var claimedEquality = OperationClaim.Equality(new Carrier(),
            new CarrierLocation(handPhysical, 1),
            new CarrierLocation(planetPhysical, 1), new Carrier());
        Require(!OperationClaim.TryReadResolvedViolation(claimedEquality, out _),
            "Uncalibrated physical carriers unexpectedly established equality.");
        var physicalProjectionSucceeded = correspondence.TryProject(
            new CarrierLocation(handPhysical, 1), planetPhysical,
            out var physicalProjection);
        Require(!physicalProjectionSucceeded &&
            !Equation.TryReadPrediction(physicalProjection, out _, out _),
            "A role correspondence unexpectedly supplied physical scale.");

        record("hands/planets: Gojo substituted two peripheral roles and their center " +
               "through an explicitly supplied partial correspondence. Skin/body remains " +
               "unmapped; physical calibration remains open; literal equality and size " +
               "projection do not resolve. The role selection is an analyst interpretation.",
            Group(source, focus, projected, projection, physicalGrounding,
                scaleCalibration, claimedEquality, physicalProjection, provenance));
    }

    private static void SunAndRepetition(Relation source, Action<string, IElement> record)
    {
        var current = new Placeholder();
        var pulse = new Placeholder();
        var next = new Placeholder();
        var consequence = new Carrier();
        var equation = Equation.Sum(consequence,
            Equation.UnitPort(consequence), Equation.UnitPort(consequence));
        var input = new Relation(current, pulse);
        var inputs = RepeatSurface.Ports(new Carrier(), current, pulse);
        var outputs = RepeatSurface.Ports(new Carrier(), next);
        var template = RepeatSurface.Template(new Carrier(), inputs,
            new Relation(input, equation), outputs);
        var feedback = RepeatSurface.Frame(new Carrier(), RepeatSurface.Feed(next, current));
        var repetitionAxis = RepeatSurface.CreateAxis(new Carrier());
        var controller = RepeatSurface.Count(repetitionAxis, 3);
        var provenance = Group(source,
            Text("Experiment knobs, not poem measurements: three scheduled iterations; " +
                 "zero or two consequence ticks per recurrence. Blank versus real is a " +
                 "candidate distinction supplied by this reader, not an extracted magnitude. " +
                 "Gojo executes addition and feedback substitution. It does not infer " +
                 "harm, violence, semantic emphasis, or historical allegory from these ticks."));
        var liveRepeat = RepeatSurface.Create(new Carrier(), template, controller,
            equation, feedback, new Placeholder());

        Relation RunPulse(long pulseTicks, out CarrierLocation total)
        {
            Relation[] frame =
            [
                StructuralPattern.Bind(current, new CarrierLocation(consequence, 0)),
                StructuralPattern.Bind(pulse, new CarrierLocation(consequence, pulseTicks))
            ];
            var initial = RepeatSurface.Frame(new Carrier(), frame);
            var clock = new Carrier();
            var steps = new List<Relation>();
            var observed = new CarrierLocation(consequence, 0);
            // Scheduling is host orchestration; no domain value is added here.
            foreach (var (start, end) in new[] { (0L, 1L), (1L, 2L), (2L, 3L) })
            {
                var before = RepeatSurface.Frame(new Carrier(), frame);
                var complete = EquationPattern.TryPredict(equation, input,
                    consequence, frame, out var prediction, out var unbound);
                Require(complete && unbound.Length == 0,
                    "A fully bound recurrence did not execute.");
                Require(Equation.TryReadPrediction(prediction, out observed, out var residue),
                    "Recurrence prediction was not scalar.");
                var computedFrame = frame.Append(StructuralPattern.Bind(next, observed)).ToArray();
                Require(RepeatSurface.TryReadFrame(feedback, out var feedbackBindings),
                    "Feedback structure was not readable.");
                frame = RepeatSurface.ApplyFeedbackFrame(computedFrame, feedbackBindings,
                    [StructuralPattern.Bind(pulse, pulse)]);
                var after = RepeatSurface.Frame(new Carrier(), frame);
                steps.Add(TimeStepSequence.Step(new Carrier(), clock, start, end,
                    before, Group(equation, prediction, feedback), after, residue));
            }
            Require(StructuralPattern.TryReadBoundSlot(current, frame, out var final) &&
                final is CarrierLocation value && value.Equals(observed),
                "Native feedback did not carry the final native prediction.");
            total = observed;
            var timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
            var run = RepeatSurface.Create(new Carrier(), template, controller,
                equation, feedback, new Placeholder(), timeline);
            return Group(source, liveRepeat, run, total, provenance);
        }

        var blankRun = RunPulse(0, out var blankTotal);
        var liveRun = RunPulse(2, out var liveTotal);
        // Expected values are harness assertions, never inputs to the recurrence.
        Require(blankTotal.Ticks == 0 && liveTotal.Ticks == 6,
            "Native feedback totals differ from the stipulated arithmetic control.");
        var missingComplete = EquationPattern.TryPredict(equation, input, consequence,
            [StructuralPattern.Bind(current, new CarrierLocation(consequence, 0))],
            out var missingPrediction, out var missingSlots);
        Require(!missingComplete && missingSlots.Contains(pulse) &&
            !Equation.TryReadPrediction(missingPrediction, out _, out _),
            "Missing pulse was misrepresented as an established numeric answer.");
        var differenceEquation = Equation.Difference(consequence, consequence, consequence);
        Require(Equation.TryPredict(differenceEquation,
            new Relation(blankTotal, liveTotal), consequence, out var difference),
            "Native total comparison did not execute.");

        record($"sun repetition: the same native equation, template, controller and feedback " +
               $"produce totals {blankTotal.Ticks} and {liveTotal.Ticks} under supplied pulses " +
               "0 and 2 across three stipulated iterations. The missing-pulse branch remains " +
               "unresolved. These are witnessed, externally scheduled executions; the controller is not an autonomous scheduler and the recorded timelines are not live derived circuits. These are mechanism probes, not measured poem meanings.",
            Group(source, liveRepeat, blankRun, liveRun, differenceEquation, difference,
                missingPrediction, Group(missingSlots), provenance));
    }

    private static Relation Group(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    private static Relation Text(string text) => EncodedTextSurface.Create(new Carrier(), text);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Poetry harness assertion: " + message);
    }
}
