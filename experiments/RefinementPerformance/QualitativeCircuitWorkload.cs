using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.RefinementPerformance;

// These select diagnostic execution backends, never a Gojo operation meaning.
public enum CircuitBackend { NativeWitness, CompiledWitness, BatchedMasks }

/// <summary>
/// A native finite signal/control program, repeated over a ring of exposed
/// cell ports. Arrays are a derived execution state; Surface retains initial
/// bindings, laws, stage wiring and feedback. Snapshot decodes current masks.
/// </summary>
public sealed class QualitativeCircuitWorkload : IPerformanceWorkload
{
    public const int StagesPerCellRound = 10;
    private readonly CircuitBackend backend;
    private readonly QualitativeReflexFixture reflex = new();
    private readonly Stage compare, compose, product, input, stock, after, policy, response, drive;
    private readonly IElement[] fieldSlots, memorySlots;
    private ulong[] fields, previous, memory;
    private readonly ulong[] neighbor, neighborTrend, gradient, trend, composed, products, inputs, transitions,
        newMemory, actions, responseField, nextField, context, forcing;
    private readonly Relation fieldDomain;
    public IElement Surface { get; }
    public Relation? LatestWitness { get; private set; }
    public int Size => fields.Length;
    public long ExecutedCellRounds { get; private set; }

    public QualitativeCircuitWorkload(int size, CircuitBackend backend)
    {
        if (size < 2) throw new ArgumentOutOfRangeException(nameof(size));
        this.backend = backend;
        var frame = reflex.LightFrame;
        fieldDomain = frame.Domain;
        IElement[] levels = [frame.Lower, frame.Equal, frame.Higher];
        var fixedContext = new Placeholder();
        var fixedDomain = StructuralConstraint.Closed(fixedContext);
        var comparisonRows = new List<Relation>();
        var productRows = new List<Relation>();
        var responseRows = new List<Relation>();
        var driveRows = new List<Relation>();
        IElement[] commands = [reflex.MoveToA, reflex.Rest, reflex.MoveToB];
        for (var a = 0; a < 3; a++)
            for (var b = 0; b < 3; b++)
            {
                comparisonRows.Add(FiniteLaw.Row(levels[a], levels[b], S(levels[a < b ? 0 : a > b ? 2 : 1])));
                var sign = (a - 1) * (b - 1);
                productRows.Add(FiniteLaw.Row(levels[a], levels[b], S(levels[sign + 1])));
                responseRows.Add(FiniteLaw.Row(levels[a], commands[b], S(levels[Math.Clamp(a + b - 1, 0, 2)])));
                // An explicitly authored three-phase disturbance permutation;
                // no hidden continuous amplitude is queried by this model.
                driveRows.Add(FiniteLaw.Row(levels[a], levels[b], S(levels[(a + b + 1) % 3])));
            }
        compare = new Stage(FiniteLaw.Create(fieldDomain, fieldDomain, fieldDomain, comparisonRows.ToArray()));
        compose = new Stage(frame.CompositionLaw);
        product = new Stage(FiniteLaw.Create(fieldDomain, fieldDomain, fieldDomain, productRows.ToArray()));
        input = new Stage(FiniteLaw.Create(fieldDomain, fixedDomain, reflex.Stock.InputDomain,
            FiniteLaw.Row(frame.Lower, fixedContext, S(reflex.Stock.Draw)),
            FiniteLaw.Row(frame.Equal, fixedContext, S(reflex.Stock.Hold)),
            FiniteLaw.Row(frame.Higher, fixedContext, S(reflex.Stock.Charge))));
        stock = new Stage(reflex.Stock.Law);
        var afterRows = FiniteLaw.Entries(reflex.Stock.TransitionDomain).Select(t =>
        {
            QualitativeStockFixture.TryReadTransition(t, out _, out _, out var result, out _, out _);
            return FiniteLaw.Row(t, fixedContext, S(result));
        }).ToArray();
        after = new Stage(FiniteLaw.Create(reflex.Stock.TransitionDomain, fixedDomain, reflex.Stock.StateDomain, afterRows));
        policy = new Stage(reflex.Policy);
        response = new Stage(FiniteLaw.Create(fieldDomain, reflex.Actions, fieldDomain, responseRows.ToArray()));
        drive = new Stage(FiniteLaw.Create(fieldDomain, fieldDomain, fieldDomain, driveRows.ToArray()));
        fields = new ulong[size]; previous = new ulong[size]; memory = new ulong[size];
        neighbor = new ulong[size]; neighborTrend = new ulong[size]; gradient = new ulong[size]; trend = new ulong[size];
        composed = new ulong[size]; products = new ulong[size]; inputs = new ulong[size]; transitions = new ulong[size];
        newMemory = new ulong[size]; actions = new ulong[size]; responseField = new ulong[size]; nextField = new ulong[size];
        context = Enumerable.Repeat(1UL, size).ToArray(); forcing = new ulong[size];
        fieldSlots = new IElement[size]; memorySlots = new IElement[size];
        var cells = new IElement[size];
        for (var i = 0; i < size; i++)
        {
            fields[i] = previous[i] = 1UL << (i % 3);
            memory[i] = 1UL << (i % 3);
            fieldSlots[i] = new Placeholder(); memorySlots[i] = new Placeholder();
        }
        for (var i = 0; i < size; i++)
            cells[i] = new Relation(new Relation(fieldSlots[i], memorySlots[i]),
                new Relation(fieldSlots[(i + 1) % size], new Relation(
                    new Relation(fieldSlots[i], compare.Left(fields[i])),
                    new Relation(memorySlots[i], stock.Left(memory[i])))));
        // Explicit stage ports make dependencies inspectable apart from names.
        IElement fieldPort = new Placeholder(), previousPort = new Placeholder(), neighborPort = new Placeholder();
        IElement gradientPort = new Placeholder(), trendPort = new Placeholder(), memoryPort = new Placeholder();
        IElement neighborTrendPort = new Placeholder();
        IElement inputPort = new Placeholder(), transitionPort = new Placeholder(), actionPort = new Placeholder();
        IElement responsePort = new Placeholder(), nextPort = new Placeholder(), contextPort = new Placeholder();
        IElement forcingPort = new Placeholder(), composedPort = new Placeholder(), productPort = new Placeholder();
        Relation Apply(Stage stage, IElement left, IElement right, IElement output) =>
            new(new Relation(new Relation(left, right), stage.Law), output);
        var program = S(
            Apply(compare, fieldPort, neighborPort, gradientPort),
            Apply(compare, fieldPort, previousPort, trendPort),
            Apply(compose, gradientPort, neighborTrendPort, composedPort),
            Apply(product, trendPort, gradientPort, productPort),
            Apply(input, trendPort, contextPort, inputPort),
            Apply(stock, memoryPort, inputPort, transitionPort),
            Apply(after, transitionPort, contextPort, memoryPort),
            Apply(policy, memoryPort, gradientPort, actionPort),
            Apply(response, fieldPort, actionPort, responsePort),
            Apply(drive, responsePort, forcingPort, nextPort));
        var trendSlots = Enumerable.Range(0, size).Select(_ => (IElement)new Placeholder()).ToArray();
        for (var i = 0; i < size; i++)
        {
            var localPrevious = new Placeholder(); var localNext = new Placeholder();
            var localForcing = new Placeholder();
            // Instantiate every template role at this cell. Neighbor operands
            // bind to the actual adjacent cell's field/trend slots, so the
            // array shifts are a derived read of connected native aliases.
            var bindings = S(new Relation(fieldPort, fieldSlots[i]),
                new Relation(neighborPort, fieldSlots[(i + 1) % size]),
                new Relation(previousPort, localPrevious), new Relation(memoryPort, memorySlots[i]),
                new Relation(trendPort, trendSlots[i]),
                new Relation(neighborTrendPort, trendSlots[(i + 1) % size]),
                new Relation(gradientPort, new Placeholder()), new Relation(composedPort, new Placeholder()),
                new Relation(productPort, new Placeholder()), new Relation(inputPort, new Placeholder()),
                new Relation(transitionPort, new Placeholder()), new Relation(actionPort, new Placeholder()),
                new Relation(responsePort, new Placeholder()), new Relation(nextPort, localNext),
                new Relation(contextPort, fixedContext), new Relation(forcingPort, localForcing));
            cells[i] = new Relation(cells[i], new Relation(bindings,
                S(new Relation(localNext, fieldSlots[i]), new Relation(fieldSlots[i], localPrevious),
                    new Relation(localPrevious, compare.Left(previous[i])),
                    new Relation(localForcing, levels[i % 3]))));
        }
        Surface = new Relation(new Relation(reflex.Surface, S(cells)),
            new Relation(program, S(new Relation(nextPort, fieldPort), new Relation(fieldPort, previousPort),
                new Relation(contextPort, fixedContext),
                new Relation(forcingPort, S(new Relation(levels[0], levels[1]),
                    new Relation(levels[1], levels[2]), new Relation(levels[2], levels[0]))))));
    }

    public long Run(int rounds)
    {
        if (rounds < 0) throw new ArgumentOutOfRangeException(nameof(rounds));
        long hash = 17;
        for (var round = 0; round < rounds; round++)
        {
            for (var i = 0; i < Size; i++)
            {
                neighbor[i] = fields[(i + 1) % Size];
                forcing[i] = 1UL << (int)((ExecutedCellRounds / Size + i) % 3);
            }
            Execute(compare, fields, neighbor, gradient);
            Execute(compare, fields, previous, trend);
            for (var i = 0; i < Size; i++) neighborTrend[i] = trend[(i + 1) % Size];
            Execute(compose, gradient, neighborTrend, composed);
            Execute(product, trend, gradient, products);
            Execute(input, trend, context, inputs);
            Execute(stock, memory, inputs, transitions);
            Execute(after, transitions, context, newMemory);
            Execute(policy, newMemory, gradient, actions);
            Execute(response, fields, actions, responseField);
            Execute(drive, responseField, forcing, nextField);
            for (var i = 0; i < Size; i++)
            {
                hash = unchecked(hash * 397 ^ (long)(nextField[i] | newMemory[i] << 8 | actions[i] << 16 |
                    composed[i] << 24 | products[i] << 32 | transitions[i] << 40));
                previous[i] = fields[i]; fields[i] = nextField[i]; memory[i] = newMemory[i];
            }
            ExecutedCellRounds += Size;
        }
        return hash;
    }

    /// <summary>A selected final-state read, outside the compute-only timer.</summary>
    public Relation Snapshot() => S(Enumerable.Range(0, Size).Select(i => (IElement)new Relation(
        new Relation(fieldSlots[i], compare.Left(fields[i])),
        new Relation(memorySlots[i], stock.Left(memory[i])))).ToArray());

    /// <summary>Decodes current state and one selected last-step native proof;
    /// this cannot reconstruct an event history the compute lane did not keep.</summary>
    public Relation Audit()
    {
        if (ExecutedCellRounds == 0) return Snapshot();
        if (!drive.Plan.TryCreateWitness(responseField[^1], forcing[^1], out var proof))
            throw new InvalidOperationException("The selected final computation no longer matches its source.");
        return new Relation(Snapshot(), proof);
    }

    private void Execute(Stage stage, ulong[] left, ulong[] right, ulong[] output)
    {
        if (backend == CircuitBackend.BatchedMasks)
        {
            if (!stage.Plan.TryEvaluateBatch(left, right, output))
                throw new InvalidOperationException("A native program dependency changed or a support was foreign.");
            return;
        }
        for (var i = 0; i < Size; i++)
        {
            Relation witness;
            if (backend == CircuitBackend.CompiledWitness)
            {
                if (!stage.Plan.TryCreateWitness(left[i], right[i], out witness))
                    throw new InvalidOperationException("A compiled witness was refused.");
            }
            else witness = FiniteLaw.Evaluate(stage.Law, stage.Left(left[i]), stage.Right(right[i]));
            if (!FiniteLaw.TryReadResult(witness, out var support, out var residue, out _) ||
                FiniteLaw.Entries(residue).Length != 0 || !stage.Plan.TryEncodeOutput(support, out output[i]))
                throw new InvalidOperationException("An admitted closed read became unresolved.");
            LatestWitness = witness;
        }
    }

    private sealed class Stage
    {
        public Relation Law { get; }
        public FiniteLawBatchPlan Plan { get; }
        private readonly Relation[] left, right;
        public Stage(Relation law)
        {
            Law = law;
            Plan = FiniteLawBatchPlan.Capture(law) ?? throw new InvalidOperationException("Authored finite law did not compile.");
            left = Supports(Plan.LeftDomain); right = Supports(Plan.RightDomain);
        }
        public Relation Left(ulong mask) => left[checked((int)mask)];
        public Relation Right(ulong mask) => right[checked((int)mask)];
        private static Relation[] Supports(Relation domain)
        {
            var entries = FiniteLaw.Entries(domain);
            return Enumerable.Range(0, 1 << entries.Length).Select(mask =>
                S(entries.Where((_, i) => (mask & 1 << i) != 0).ToArray())).ToArray();
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
