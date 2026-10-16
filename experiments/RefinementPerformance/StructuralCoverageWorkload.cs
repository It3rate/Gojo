using Gojo.Core;
using Gojo.Equations;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.RefinementPerformance;

/// <summary>
/// A bounded, witnessed complement to the recurrent circuit benchmark. Each
/// cell repeatedly audits one authored three-event episode: generation and
/// recognition, resolution transport, support refinement, held observation,
/// replay refusal, and finite-law obligations. Repetition is a new read of the
/// episode, not a claim that its event identities have advanced in time.
/// </summary>
public sealed class StructuralCoverageWorkload : IPerformanceWorkload
{
    public const string Description =
        "Pattern generation/recognition and role constraints; ordinal composition; " +
        "resolution image/preimage with open support; same-event refinement; " +
        "independent schedules, held observation and replay refusal; native law " +
        "obligations and counterexample. Measured mode additionally supplies the " +
        "same state/site identities through finite numeric publications, calibrated " +
        "Gojo claims/equations and numeric timeline spans.";

    private readonly Cell[] cells;
    private readonly bool measured;
    private readonly bool compiled;

    public IElement Surface { get; }
    public Relation? LatestWitness { get; private set; }
    public int Size => cells.Length;

    public StructuralCoverageWorkload(int size, bool measured = false, bool compiled = false)
    {
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size));
        this.measured = measured;
        this.compiled = compiled;
        cells = Enumerable.Range(0, size).Select(index => new Cell(index, measured)).ToArray();
        Surface = StructuralConstraint.Closed(cells.Select(cell => (IElement)cell.Surface).ToArray());
    }

    /// <summary>
    /// Only the latest bounded episode is retained. All previous reads remain
    /// independent observations; this workload promises no exact run archive.
    /// The checksum reads structural outcomes and excludes identity allocation.
    /// </summary>
    public long Run(int rounds)
    {
        if (rounds < 0) throw new ArgumentOutOfRangeException(nameof(rounds));
        long checksum = 0;
        for (var round = 0; round < rounds; round++)
            foreach (var cell in cells)
            {
                var result = cell.Read(measured, compiled);
                LatestWitness = result.Witness;
                checksum = checked(checksum + result.Checksum);
            }
        return checksum;
    }

    private sealed class Cell
    {
        private readonly IElement source = new Placeholder();
        private readonly IElement subject = new Placeholder();
        private readonly IElement producerBefore = new Placeholder();
        private readonly IElement producerEvent = new Placeholder();
        private readonly IElement producerAfter = new Placeholder();
        private readonly IElement receiverBefore = new Placeholder();
        private readonly IElement receiverFirst = new Placeholder();
        private readonly IElement receiverSecond = new Placeholder();
        private readonly IElement receiverThird = new Placeholder();
        private readonly IElement boundary = new Placeholder();
        private readonly IElement interior = new Placeholder();
        private readonly IElement coarseBasis = new Placeholder();
        private readonly IElement comparisonA = new Placeholder();
        private readonly IElement comparisonB = new Placeholder();
        private readonly IElement comparisonC = new Placeholder();
        private readonly StructuralLandmarkFixture landmarks = new();
        private readonly QualitativeStockFixture stock = new();
        private readonly OrdinalReferenceFrame order = new();
        private readonly ResolutionCorrespondence resolution;
        private readonly EventScheduleConnection schedule;
        private readonly Relation siteDomain;
        private readonly Relation template;
        private readonly Relation siteSupport;
        private readonly Relation stateSupport;
        private readonly Relation obligations;
        private readonly Relation nativeSite;
        private readonly Relation nativeState;
        private readonly Relation coarseSite;
        private readonly Relation coarseState;
        private readonly Relation openSite;
        private readonly Relation ordinalFirst;
        private readonly Relation ordinalSecond;
        private readonly Relation initialSchedule;
        private readonly MeasuredStructuralSupplier? supplier;
        private readonly Carrier? publicationRoot;
        private readonly Carrier? time;
        private readonly Carrier? timelineRoot;
        private readonly Relation? timedHold;
        private readonly long siteTicks;
        private readonly long stateTicks;

        public Relation Surface { get; }

        public Cell(int index, bool measured)
        {
            var selection = index % 3;
            template = new[] { landmarks.ETemplate, landmarks.HTemplate, landmarks.BTemplate }[selection];
            IElement[] sites = [landmarks.Beginning, landmarks.Interior, landmarks.End];
            IElement[] states = [stock.Empty, stock.Available, stock.Full];
            if (measured)
                supplier = new MeasuredStructuralSupplier(stock, landmarks, new Carrier());
            // Publication refinement requires the same advertised contract,
            // including its domain identity; an equal-looking scope is not it.
            siteDomain = supplier?.SiteDomain ?? StructuralConstraint.Closed(sites);
            siteSupport = StructuralConstraint.Closed(sites[selection]);
            stateSupport = StructuralConstraint.Closed(states[selection]);
            siteTicks = new[] { 0L, 7L, 20L }[selection];
            stateTicks = selection * 10L;
            var coarseDomain = StructuralConstraint.Closed(boundary, interior);
            resolution = ResolutionCorrespondence.FromRows(landmarks.Frame, coarseBasis,
                siteDomain, coarseDomain,
                new Relation(landmarks.Beginning, boundary),
                new Relation(landmarks.Interior, interior),
                new Relation(landmarks.End, boundary));

            // One producer sample serves two faster receiver events. The third
            // event is outside its validity window and therefore is refused.
            schedule = new EventScheduleConnection(source, subject,
                StructuralConstraint.Closed(new Relation(producerBefore, producerEvent),
                    new Relation(producerEvent, producerAfter)),
                StructuralConstraint.Closed(new Relation(receiverBefore, receiverFirst),
                    new Relation(receiverFirst, receiverSecond),
                    new Relation(receiverSecond, receiverThird)),
                StructuralConstraint.Closed(EventScheduleConnection.Window(producerEvent,
                    receiverFirst, receiverSecond)));
            initialSchedule = schedule.Initial(receiverBefore);
            nativeSite = QualitativePublication.Create(source, landmarks.Frame, producerEvent,
                landmarks.Frame, siteDomain, siteSupport, landmarks.Surface);
            nativeState = QualitativePublication.Create(source, stock.Receiver, producerEvent,
                stock.Surface, stock.StateDomain, stateSupport, stock.EqualQuantum);
            coarseSite = QualitativePublication.Create(source, landmarks.Frame, producerEvent,
                landmarks.Frame, siteDomain, siteDomain, landmarks.Surface);
            coarseState = QualitativePublication.Create(source, stock.Receiver, producerEvent,
                stock.Surface, stock.StateDomain, stock.StateDomain, stock.Surface);
            openSite = QualitativePublication.Create(source, landmarks.Frame, producerEvent,
                landmarks.Frame, siteDomain, StructuralConstraint.Open(sites[selection]), landmarks.Frame);

            ordinalFirst = order.Observe(source, comparisonA, comparisonB, producerEvent,
                StructuralConstraint.Closed(order.Higher));
            ordinalSecond = order.Observe(source, comparisonB, comparisonC, receiverFirst,
                StructuralConstraint.Closed(selection == 1 ? order.Lower : order.Higher));
            var equal = StructuralConstraint.Closed(order.Equal);
            var higher = StructuralConstraint.Closed(order.Higher);
            var lower = StructuralConstraint.Closed(order.Lower);
            obligations = StructuralConstraint.Closed(
                FiniteLawConformance.Equation(
                    FiniteLawConformance.Apply(order.CompositionLaw, equal, higher), higher),
                // An increase followed by a decrease does not prove return.
                FiniteLawConformance.Equation(
                    FiniteLawConformance.Apply(order.CompositionLaw, higher, lower), equal),
                FiniteLawConformance.Equation(
                    FiniteLawConformance.Apply(order.CompositionLaw,
                        StructuralConstraint.Open(order.Higher), higher), higher));

            IElement metric = StructuralConstraint.Closed();
            if (measured)
            {
                publicationRoot = new Carrier();
                time = new Carrier();
                timelineRoot = new Carrier();
                timedHold = Equation.Affine(time, 0, 2, time, 0, 2);
                metric = new Relation(supplier!.Surface, timedHold);
            }
            Surface = new Relation(new Relation(landmarks.Surface, stock.Surface),
                new Relation(new Relation(resolution.Surface, schedule.Surface),
                    new Relation(new Relation(order.Surface, obligations),
                        new Relation(new Relation(coarseSite, coarseState), metric))));
        }

        public (Relation Witness, long Checksum) Read(bool measured, bool compiled)
        {
            var generated = landmarks.Generate(template);
            Require(StructuralConstraint.TryReadUnique(generated, out var instance, out _) &&
                instance is Relation, "The authored landmark template must generate one native instance.");
            var glyph = (Relation)instance;
            var recognized = landmarks.Recognize(template, StructuralConstraint.Closed(glyph));
            Require(StructuralConstraint.TryReadUnique(recognized, out _, out _),
                "The generated pattern must remain recognizable.");
            var roles = landmarks.ReadRoles(glyph);
            Require(StructuralConstraint.TryReadUnique(roles, out _, out _),
                "The coarser role read must retain the same shared landmark.");

            var site = nativeSite;
            var state = nativeState;
            IElement measurement = StructuralConstraint.Closed();
            if (measured)
            {
                // Stable known samples are reread through advertised finite
                // surfaces. No private value is recovered from a coarse port.
                var axis = supplier!.Axis;
                axis.Set(stateTicks);
                var stateValue = FiniteReadSurface.Publish(publicationRoot!, supplier.Capacity,
                    axis.ValueSlot, stock.Surface);
                var siteValue = FiniteReadSurface.Publish(publicationRoot!, supplier.Capacity,
                    new CarrierLocation(axis, siteTicks), landmarks.Frame);
                state = supplier.ObserveState(supplier.Envelope(source, producerEvent, stateValue));
                site = supplier.ObserveLandmark(supplier.Envelope(source, producerEvent, siteValue));
                Require(supplier.TryReadSitePosition(landmarks.Interior, out var interiorPosition) &&
                    interiorPosition.Ticks == 7, "Calibration must preserve the non-midpoint shared landmark.");
                measurement = new Relation(new Relation(stateValue, siteValue),
                    new Relation(state, site));
            }

            Require(QualitativePublication.TryRefine(coarseSite, site, out var refinedSite, out var siteRefinement),
                "The same-event site supplier must refine the original identity domain.");
            Require(QualitativePublication.TryRefine(coarseState, state, out _, out var stateRefinement),
                "The same-event state supplier must refine the original identity domain.");
            Require(QualitativePublication.TryForward(resolution, refinedSite,
                out var coarse, out var forward), "The declared resolution connection must read.");
            Require(QualitativePublication.TryBackward(resolution, coarse,
                out var preimage, out var backward), "The backward read must expose lost alternatives.");
            Require(QualitativePublication.TryForward(resolution, openSite,
                out var uncertain, out var uncertainty), "Open support remains a valid lower-resolution read.");
            Require(QualitativePublication.TryRead(preimage, out _, out _, out _, out _, out _,
                out var preimageSupport, out _), "A preimage must remain a native publication.");
            Require(QualitativePublication.TryRead(uncertain, out _, out _, out _, out _, out _,
                out var uncertainSupport, out _), "An uncertain result must remain a native publication.");
            Require(StructuralConstraint.TryReadScope(uncertainSupport, out _, out var seam, out var tail) &&
                !seam.Equals(tail), "Transport cannot close the missing input support.");

            var composition = order.Compose(ordinalFirst, ordinalSecond, compiled);
            Require(order.TryRead(composition, out _, out _, out _, out _, out var composedSupport, out _),
                "Ordinal composition must retain its actual shared pivot.");
            var resolutionCheck = resolution.CheckForward();
            var lawCheck = FiniteLawConformance.Check(order.CompositionLaw, obligations);
            Require(FiniteLawConformance.TryReadReport(lawCheck, out _, out _,
                out var passed, out var violated, out var pending) &&
                FiniteLaw.Entries(passed).Length == 1 && FiniteLaw.Entries(violated).Length == 1 &&
                FiniteLaw.Entries(pending).Length == 1, "The law audit must keep its counterexample and open premise.");

            var first = schedule.Deliver(initialSchedule, coarse, producerEvent, receiverFirst);
            Require(schedule.TryReadDelivery(first, out _, out var afterFirst, out var acceptedFirst,
                out _, out _) && FiniteLaw.Entries(acceptedFirst).Length == 1,
                "The producer observation must serve the first receiver event.");
            var held = schedule.Deliver(afterFirst, coarse, producerEvent, receiverSecond);
            Require(schedule.TryReadDelivery(held, out _, out var afterHeld, out var acceptedHeld,
                out _, out _) && FiniteLaw.Entries(acceptedHeld).Length == 1,
                "The same observation must be reusable inside its explicit window.");
            var replay = schedule.Deliver(afterHeld, coarse, producerEvent, receiverSecond);
            var expired = schedule.Deliver(afterHeld, coarse, producerEvent, receiverThird);
            Require(schedule.TryReadDelivery(replay, out _, out var replayAfter, out _, out var replayBlocked, out _) &&
                ReferenceEquals(replayAfter, afterHeld) && FiniteLaw.Entries(replayBlocked).Length == 1,
                "Replay must preserve the advanced causal head and refusal evidence.");
            Require(schedule.TryReadDelivery(expired, out _, out var expiredAfter, out _, out var expiredBlocked, out _) &&
                ReferenceEquals(expiredAfter, afterHeld) && FiniteLaw.Entries(expiredBlocked).Length == 1,
                "The expired observation must remain blocked at the old head.");

            IElement timeline = new Relation(first, held);
            if (measured)
            {
                var holdRead = Equation.Predict(timedHold!, new CarrierLocation(time!, 1), time!);
                Require(Equation.TryReadPrediction(holdRead, out var mapped, out _) && mapped.Ticks == 1,
                    "The numeric schedule correspondence must preserve the selected event coordinate.");
                timeline = TimeStepSequence.Create(timelineRoot!, time!, initialSchedule,
                    TimeStepSequence.Step(timelineRoot!, time!, 0, 1, initialSchedule,
                        schedule.Surface, afterFirst, first),
                    TimeStepSequence.Step(timelineRoot!, time!, 1, 2, afterFirst,
                        schedule.Surface, afterHeld, new Relation(held, holdRead)));
            }
            var witness = new Relation(Surface, StructuralConstraint.Closed(generated, recognized, roles,
                siteRefinement, stateRefinement, forward, backward, uncertainty, composition,
                resolutionCheck, lawCheck, timeline, replay, expired, measurement));
            var checksum = 100L + FiniteLaw.Entries(preimageSupport).Length +
                FiniteLaw.Entries(composedSupport).Length + FiniteLaw.Entries(passed).Length +
                FiniteLaw.Entries(violated).Length + FiniteLaw.Entries(pending).Length +
                FiniteLaw.Entries(acceptedFirst).Length + FiniteLaw.Entries(acceptedHeld).Length +
                FiniteLaw.Entries(replayBlocked).Length + FiniteLaw.Entries(expiredBlocked).Length;
            return (witness, checksum);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
