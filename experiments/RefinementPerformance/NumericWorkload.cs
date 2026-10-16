using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.RefinementPerformance;

/// <summary>
/// Measured counterpart to the categorical ring. The operational carriers,
/// equation ports and neighbor connections are reused. The host orders events;
/// weighted sums, differences, products, conditions and capacity are Gojo reads
/// or settlement. Only the most recent read is retained, not an exact archive.
/// </summary>
public sealed class NumericWorkload : IPerformanceWorkload
{
    // A cell exposes its signal, command and stock. Neighbor signal/command
    // ports feed reusable equations; their predictions are explicitly imported
    // through signed delta settlement only after every cell has been sampled.
    // A clamped stock retains excess/demand as tension. It does not implement
    // the categorical fixture's rejection of a blocked discrete pulse.
    private readonly Cell[] cells;
    private readonly Carrier forcing = new();
    private readonly Carrier clock = new();
    private readonly Relation forcingPort;
    private readonly Relation schedule;
    private readonly bool compiled;
    private long elapsedRounds;
    private IElement? latestEvent;

    public IElement Surface { get; }
    public Relation? LatestWitness { get; private set; }

    /// <summary>Five equation reads, two conditional selections and one stock
    /// settlement per cell-round, plus signal/previous/command delta imports.
    /// This is a descriptive work count, not equal-cost primitive operations.</summary>
    public int OperationsPerCellRound => 8;

    public static string Description =>
        "Live numeric ring: weighted neighbor/command feedback, threshold/trend/gradient differences, " +
        "product, conditional charge/draw and movement, clamped stock with retained tension; latest native evidence.";

    public NumericWorkload(int size, bool compiled = false)
    {
        if (size < 2) throw new ArgumentOutOfRangeException(nameof(size));
        this.compiled = compiled;
        forcing.Set(48);
        forcingPort = new Relation(forcing, Relation.Segment(forcing, 16, 80));
        schedule = new Relation(Relation.Segment(clock, 0, 1),
            new Relation(new Placeholder(), new Placeholder()));
        cells = Enumerable.Range(0, size).Select(index => new Cell(index, compiled)).ToArray();
        for (var index = 0; index < size; index++)
            cells[index].Connect(cells[(index + size - 1) % size], cells[(index + 1) % size],
                forcing, forcingPort);
        Surface = new Relation(new Relation(forcingPort, schedule),
            StructuralConstraint.Closed(cells.Select(cell => (IElement)cell.Surface).ToArray()));
    }

    public long Run(int rounds)
    {
        if (rounds < 0) throw new ArgumentOutOfRangeException(nameof(rounds));
        long checksum = 17;
        for (var round = 0; round < rounds; round++)
        {
            if (compiled)
                foreach (var cell in cells)
                    if (!cell.IsCurrent) throw new InvalidOperationException("A compiled numeric dependency changed.");
            var eventNumber = elapsedRounds++;
            // A deterministic external source; this arithmetic generates input,
            // and does not calculate the modeled network's response.
            forcing.Set(16 + eventNumber % 65 * 17 % 65);
            var eventIdentity = new CarrierLocation(clock, eventNumber);
            latestEvent = eventIdentity;
            foreach (var cell in cells)
                if (compiled) cell.SampleCompiled(); else cell.Sample(eventIdentity);
            foreach (var cell in cells)
            {
                cell.Commit();
                checksum = unchecked(checksum * 31 + cell.ReadChecksum());
            }
            // The retained evidence is bounded: the final cell's current step
            // and the shared schedule/forcing contract, not all past rounds.
            if (!compiled) LatestWitness = new Relation(new Relation(eventIdentity, schedule),
                new Relation(forcingPort, cells[^1].LatestWitness!));
        }
        return checksum;
    }

    /// <summary>Selected final evidence, outside the compute-only timer. The
    /// compiled lane reconstructs native predictions, including exact retained
    /// source-basis residue, from its last frozen operand samples.</summary>
    public Relation Snapshot()
    {
        if (compiled && latestEvent is not null)
        {
            foreach (var cell in cells)
                if (!cell.IsCurrent) throw new InvalidOperationException("A compiled numeric dependency changed.");
            LatestWitness = new Relation(new Relation(latestEvent, schedule),
                new Relation(forcingPort, cells[^1].Snapshot(latestEvent)));
        }
        return new Relation(Surface, new Relation(
            StructuralConstraint.Closed(cells.Select(cell => (IElement)cell.StateSnapshot()).ToArray()),
            (IElement?)LatestWitness ?? StructuralConstraint.Open()));
    }

    private sealed class Cell
    {
        private readonly Carrier signal = new();
        private readonly Carrier previous;
        private readonly Carrier stock = new();
        private readonly Carrier command = new();
        private readonly Carrier difference = new();
        private readonly Carrier algebra = new();
        private readonly Carrier root = new();
        private readonly Placeholder pulseRole = new();
        private readonly Placeholder stockRole = new();
        private readonly Placeholder gradientRole = new();
        private readonly Relation stockCapacity;
        private readonly Relation signalPort;
        private readonly Relation commandPort;
        private readonly Relation stockPort;
        private readonly Relation pulsePolicy;
        private readonly Relation commandPolicy;
        private readonly Relation product;
        private readonly bool compiled;
        private NumericEquationPlan recurrencePlan = null!, thresholdPlan = null!, trendPlan = null!,
            gradientPlan = null!, productPlan = null!;
        private NumericIntervalPolicyPlan pulsePlan = null!, commandPlan = null!;
        private CarrierLocation[] recurrenceSlots = null!, thresholdSlots = null!, trendSlots = null!, gradientSlots = null!;
        private long[] recurrenceSamples = null!, thresholdSamples = null!, trendSamples = null!, gradientSamples = null!;
        private readonly long[] productSamples = new long[2];
        private long beforeStockValue, beforeStockTension, afterStockValue, afterStockTension;
        private CarrierLocation lastPulse;
        private Relation recurrence = null!;
        private Relation recurrenceInput = null!;
        private Relation thresholdDifference = null!;
        private Relation thresholdInput = null!;
        private Relation trendDifference = null!;
        private Relation trendInput = null!;
        private Relation gradientDifference = null!;
        private Relation gradientInput = null!;
        private CarrierLocation nextSignal;
        private CarrierLocation nextCommand;
        private long sampledSignal;
        private long algebraValue;

        public Relation Surface { get; private set; } = null!;
        public Relation? LatestWitness { get; private set; }

        public bool IsCurrent => recurrencePlan.IsCurrent && thresholdPlan.IsCurrent && trendPlan.IsCurrent &&
            gradientPlan.IsCurrent && productPlan.IsCurrent && pulsePlan.IsCurrent && commandPlan.IsCurrent;

        public Cell(int index, bool compiled)
        {
            this.compiled = compiled;
            previous = new Carrier(signal.UnitShape);
            signal.Set(24 + index % 57);
            previous.Set(checked((long)signal.Value));
            stockCapacity = new Relation(Relation.Point(stock, 20), Relation.Point(stock, 20));
            stock.Attach(stockCapacity);
            stock.Set(index % 21);
            command.Set(index % 3);
            signalPort = new Relation(signal, Relation.Segment(signal, 0, 128));
            commandPort = new Relation(command, Relation.Segment(command, 0, 2));
            stockPort = new Relation(stock, stockCapacity);
            product = Equation.Product(algebra, Equation.UnitPort(difference), Equation.UnitPort(difference));
            pulsePolicy = SupportDecisionSurface.Create(root,
                PulseRule(-1, -2048, -1), PulseRule(0, 0, 0), PulseRule(1, 1, 2048));
            var rules = new List<Relation>();
            for (var mode = 0; mode < 2; mode++)
            {
                // A low stock moves toward the brighter neighbor; a higher
                // stock reverses the same ordered comparison. Numeric frames
                // and the response points carry all six authored conditions.
                rules.Add(CommandRule(mode, -2048, -1, mode == 0 ? 0 : 2));
                rules.Add(CommandRule(mode, 0, 0, 1));
                rules.Add(CommandRule(mode, 1, 2048, mode == 0 ? 2 : 0));
            }
            commandPolicy = SupportDecisionSurface.Create(root, rules.ToArray());
        }

        public void Connect(Cell left, Cell right, Carrier source, Relation sourcePort)
        {
            var selfWeight = Equation.WeightedPort(signal, 0, 2, 0, 1);
            var leftWeight = Equation.WeightedPort(left.signal, 0, 4, 0, 1);
            var rightWeight = Equation.WeightedPort(right.signal, 0, 8, 0, 1);
            var sourceWeight = Equation.WeightedPort(source, 0, 8, 0, 1);
            var commandWeight = Equation.WeightedPort(command, 0, 2, -1, 1);
            var neighborCommandWeight = Equation.WeightedPort(left.command, 0, 2, -1, 1);
            recurrence = Equation.Sum(signal, selfWeight, leftWeight, rightWeight,
                sourceWeight, commandWeight, neighborCommandWeight);
            recurrenceInput = Chain(signal.ValueSlot, left.signal.ValueSlot, right.signal.ValueSlot,
                source.ValueSlot, command.ValueSlot, left.command.ValueSlot);
            thresholdDifference = Equation.Difference(signal, signal, difference);
            thresholdInput = new Relation(new CarrierLocation(signal, 40), signal.ValueSlot);
            trendDifference = Equation.Difference(previous, signal, difference);
            trendInput = new Relation(previous.ValueSlot, signal.ValueSlot);
            gradientDifference = Equation.Difference(left.signal, right.signal, difference);
            gradientInput = new Relation(left.signal.ValueSlot, right.signal.ValueSlot);
            var inputs = StructuralConstraint.Closed(
                new Relation(signalPort, selfWeight), new Relation(left.signalPort, leftWeight),
                new Relation(right.signalPort, rightWeight), new Relation(sourcePort, sourceWeight),
                new Relation(commandPort, commandWeight), new Relation(left.commandPort, neighborCommandWeight));
            var body = new Relation(Relation.Segment(signal, 0, 7), Relation.Segment(signal, 7, 20));
            Surface = new Relation(new Relation(body, inputs),
                new Relation(new Relation(signalPort, new Relation(stockPort, commandPort)),
                    StructuralConstraint.Closed(new Relation(recurrence, recurrenceInput),
                        new Relation(thresholdDifference, thresholdInput), new Relation(trendDifference, trendInput),
                        new Relation(gradientDifference, gradientInput), product, pulsePolicy, commandPolicy)));
            if (compiled)
            {
                recurrencePlan = NumericEquationPlan.Capture(recurrence, signal) ?? throw Unreadable();
                thresholdPlan = NumericEquationPlan.Capture(thresholdDifference, difference) ?? throw Unreadable();
                trendPlan = NumericEquationPlan.Capture(trendDifference, difference) ?? throw Unreadable();
                gradientPlan = NumericEquationPlan.Capture(gradientDifference, difference) ?? throw Unreadable();
                productPlan = NumericEquationPlan.Capture(product, algebra) ?? throw Unreadable();
                pulsePlan = NumericIntervalPolicyPlan.Capture(pulsePolicy) ?? throw Unreadable();
                commandPlan = NumericIntervalPolicyPlan.Capture(commandPolicy) ?? throw Unreadable();
                recurrenceSlots = Slots(recurrenceInput); thresholdSlots = Slots(thresholdInput);
                trendSlots = Slots(trendInput); gradientSlots = Slots(gradientInput);
                recurrenceSamples = new long[recurrenceSlots.Length]; thresholdSamples = new long[thresholdSlots.Length];
                trendSamples = new long[trendSlots.Length]; gradientSamples = new long[gradientSlots.Length];
            }
        }

        public void SampleCompiled()
        {
            sampledSignal = checked((long)signal.Value);
            SampleSlots(recurrenceSlots, recurrenceSamples); SampleSlots(thresholdSlots, thresholdSamples);
            SampleSlots(trendSlots, trendSamples); SampleSlots(gradientSlots, gradientSamples);
            nextSignal = new CarrierLocation(signal, recurrencePlan.Evaluate(recurrenceSamples));
            var threshold = thresholdPlan.Evaluate(thresholdSamples);
            productSamples[0] = trendPlan.Evaluate(trendSamples);
            productSamples[1] = gradientPlan.Evaluate(gradientSamples);
            algebraValue = productPlan.Evaluate(productSamples);
            if (!pulsePlan.TrySelect(pulseRole, difference, threshold, null, null, 0, out lastPulse, out _)) throw Unreadable();
            beforeStockValue = checked((long)stock.Value); beforeStockTension = stock.Tension;
            if (lastPulse.Ticks > 0) stock.Add(checked((ulong)lastPulse.Ticks));
            else if (lastPulse.Ticks < 0) stock.Remove(checked((ulong)-lastPulse.Ticks));
            else stock.Add(0);
            afterStockValue = checked((long)stock.Value); afterStockTension = stock.Tension;
            if (!commandPlan.TrySelect(stockRole, stock, afterStockValue, gradientRole, difference,
                productSamples[1], out nextCommand, out _)) throw Unreadable();
        }

        public Relation Snapshot(IElement eventIdentity)
        {
            var signalRead = Predict(recurrence, Frozen(recurrenceSlots, recurrenceSamples), signal, out var signalValue);
            var thresholdInput = Frozen(thresholdSlots, thresholdSamples);
            var thresholdRead = Predict(thresholdDifference, thresholdInput, difference, out var threshold);
            var trendRead = Predict(trendDifference, Frozen(trendSlots, trendSamples), difference, out var trend);
            var gradientInput = Frozen(gradientSlots, gradientSamples);
            var gradientRead = Predict(gradientDifference, gradientInput, difference, out var gradient);
            var productRead = Predict(product, new Relation(trend, gradient), algebra, out var combined);
            if (signalValue.Ticks != nextSignal.Ticks || combined.Ticks != algebraValue) throw Unreadable();
            var pulseRead = SupportDecisionSurface.Select(root, pulsePolicy,
                SupportDecisionSurface.Binding(root, pulseRole, Publication(thresholdDifference, thresholdInput, thresholdRead)));
            if (Response(pulseRead, stock).Ticks != lastPulse.Ticks) throw Unreadable();
            var before = new Relation(new CarrierLocation(stock, beforeStockValue), new CarrierLocation(stock, beforeStockTension));
            var after = new Relation(new CarrierLocation(stock, afterStockValue), new CarrierLocation(stock, afterStockTension));
            var stockRead = ReadWitness.Create(root, after, stockPort, new CarrierLocation(stock, afterStockValue),
                new CarrierLocation(stock, afterStockTension), new Relation(before, pulseRead));
            var commandRead = SupportDecisionSurface.Select(root, commandPolicy,
                SupportDecisionSurface.Binding(root, stockRole, stockRead),
                SupportDecisionSurface.Binding(root, gradientRole, Publication(gradientDifference, gradientInput, gradientRead)));
            if (Response(commandRead, command).Ticks != nextCommand.Ticks) throw Unreadable();
            return new Relation(new Relation(eventIdentity, Surface), StructuralConstraint.Closed(signalRead,
                thresholdRead, trendRead, gradientRead, productRead, pulseRead,
                new Relation(before, new Relation(lastPulse, after)), commandRead));
        }

        public Relation StateSnapshot() => new(new Relation(signalPort, new CarrierLocation(signal, checked((long)signal.Value))),
            new Relation(new Relation(stockPort, new Relation(new CarrierLocation(stock, checked((long)stock.Value)), stock.TensionSlot)),
                new Relation(commandPort, new CarrierLocation(command, checked((long)command.Value)))));

        public void Sample(IElement eventIdentity)
        {
            sampledSignal = checked((long)signal.Value);
            var signalRead = Predict(recurrence, recurrenceInput, signal, out nextSignal);
            var thresholdRead = Predict(thresholdDifference, thresholdInput, difference, out var threshold);
            var trendRead = Predict(trendDifference, trendInput, difference, out var trend);
            var gradientRead = Predict(gradientDifference, gradientInput, difference, out var gradient);
            var productInput = new Relation(trend, gradient);
            var productRead = Predict(product, productInput, algebra, out var combined);
            algebraValue = combined.Ticks;
            var thresholdPublication = Publication(thresholdDifference, thresholdInput, thresholdRead);
            var pulseRead = SupportDecisionSurface.Select(root, pulsePolicy,
                SupportDecisionSurface.Binding(root, pulseRole, thresholdPublication));
            var pulse = Response(pulseRead, stock);
            var beforeStock = new Relation(new CarrierLocation(stock, checked((long)stock.Value)),
                new CarrierLocation(stock, stock.Tension));
            if (pulse.Ticks > 0) stock.Add(checked((ulong)pulse.Ticks));
            else if (pulse.Ticks < 0) stock.Remove(checked((ulong)-pulse.Ticks));
            // Hold is an explicit zero arrival as in the other two responses.
            else stock.Add(0);
            var afterStock = new Relation(new CarrierLocation(stock, checked((long)stock.Value)),
                new CarrierLocation(stock, stock.Tension));
            var stockRead = ReadWitness.Create(root, afterStock, stockPort,
                new CarrierLocation(stock, checked((long)stock.Value)),
                new CarrierLocation(stock, stock.Tension), new Relation(beforeStock, pulseRead));
            var commandRead = SupportDecisionSurface.Select(root, commandPolicy,
                SupportDecisionSurface.Binding(root, stockRole, stockRead),
                SupportDecisionSurface.Binding(root, gradientRole,
                    Publication(gradientDifference, gradientInput, gradientRead)));
            nextCommand = Response(commandRead, command);
            LatestWitness = new Relation(new Relation(eventIdentity, Surface),
                StructuralConstraint.Closed(signalRead, thresholdRead, trendRead, gradientRead, productRead,
                    pulseRead, new Relation(beforeStock, new Relation(pulse, afterStock)), commandRead));
        }

        public void Commit()
        {
            // These are explicit output imports; Set feeds a signed delta into
            // the persistent carrier, and cannot bypass attached capacity.
            previous.Set(sampledSignal);
            signal.Set(nextSignal.Ticks);
            command.Set(nextCommand.Ticks);
        }

        public long ReadChecksum() => unchecked(checked((long)signal.Value) * 17 +
            checked((long)stock.Value) * 31 + stock.Tension * 7 + checked((long)command.Value) + algebraValue);

        private Relation PulseRule(long pulse, long minimum, long maximum) =>
            SupportDecisionSurface.Rule(root, new CarrierLocation(stock, pulse),
                SupportDecisionSurface.Condition(root, pulseRole, Relation.Segment(difference, minimum, maximum)));

        private Relation CommandRule(int mode, long minimum, long maximum, long response) =>
            SupportDecisionSurface.Rule(root, new CarrierLocation(command, response),
                SupportDecisionSurface.Condition(root, stockRole, Relation.Segment(stock, mode == 0 ? 0 : 10, mode == 0 ? 9 : 20)),
                SupportDecisionSurface.Condition(root, gradientRole, Relation.Segment(difference, minimum, maximum)));

        private Relation Publication(Relation equation, IElement input, Relation prediction)
        {
            if (!Equation.TryReadPrediction(prediction, out var result, out var residue))
                throw new InvalidOperationException("The authored numeric equation became unreadable.");
            return ReadWitness.Create(root, input, equation, result, residue, prediction);
        }

        private static Relation Predict(Relation equation, IElement input, Carrier target, out CarrierLocation value)
        {
            if (!Equation.TryPredict(equation, input, target, out var prediction) ||
                !Equation.TryReadPrediction(prediction, out value, out _))
                throw new InvalidOperationException("The authored numeric equation became unreadable.");
            return prediction;
        }

        private static CarrierLocation Response(Relation read, Carrier expected)
        {
            if (!SupportDecisionSurface.TryReadSelection(read, out _, out var response, out _, out _) ||
                response is not CarrierLocation location || !location.Carrier.SameIdentityAs(expected))
                throw new InvalidOperationException("The authored numeric policy left the admitted range.");
            return location;
        }

        private static Relation Chain(params IElement[] elements)
        {
            IElement group = elements[0];
            for (var index = 1; index < elements.Length; index++) group = new Relation(group, elements[index]);
            return (Relation)group;
        }

        private static CarrierLocation[] Slots(IElement input)
        {
            var result = new List<CarrierLocation>();
            Collect(input); return result.ToArray();
            void Collect(IElement element)
            {
                if (element is CarrierLocation location) result.Add(location);
                else if (element is Relation relation) { Collect(relation.Left); Collect(relation.Right); }
                else throw Unreadable();
            }
        }

        private static void SampleSlots(CarrierLocation[] slots, long[] samples)
        {
            for (var index = 0; index < slots.Length; index++) samples[index] = slots[index].Ticks;
        }

        private static Relation Frozen(CarrierLocation[] slots, long[] samples) =>
            Chain(slots.Select((slot, index) => (IElement)new CarrierLocation(slot.Carrier, samples[index])).ToArray());

        private static InvalidOperationException Unreadable() =>
            new("The authored numeric program could not be read by its narrow compiled backend.");
    }
}
