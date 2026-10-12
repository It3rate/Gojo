using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveAgency;

/// <summary>The external deterministic environment and sensor instrument.
/// Charge, heat, position, and outcome counters never become brain memory.</summary>
public sealed class ChargeWorld
{
    public const int Cells = 31;
    public const int Maximum = 100;
    public const int RelocationEvery = 160;
    public const int QueryChargeCost = 1;
    private static readonly int[] ChargerLocations = [23, 6, 25, 10];
    private int previousCenterLight;
    private IElement previousCenterEvent = new Placeholder();
    private Relation[] latestSensors = [];
    private bool cycleArmed;
    public int X { get; private set; } = 15;
    public int Charge { get; private set; } = 60;
    public int Heat { get; private set; }
    public int Cycles { get; private set; }
    public int StarvationTicks { get; private set; }
    public int OverheatTicks { get; private set; }
    public int Movements { get; private set; }
    public long ChargeSum { get; private set; }
    public int ChargerX(int tick) => ChargerLocations[tick / RelocationEvery % ChargerLocations.Length];
    private int LightAt(int x, int tick) => Math.Max(0, Maximum - 4 * Math.Abs(x - ChargerX(tick)));

    public ChargeWorld() => previousCenterLight = LightAt(X, 0);

    public Relation[] Sense(NativeBrain brain, int tick)
    {
        var currentCenter = LightAt(X, tick);
        var trend = currentCenter.CompareTo(previousCenterLight);
        var earlierEvent = previousCenterEvent;
        var currentEvent = new Placeholder();
        // The instrument retains private readings and publishes only a sign.
        var comparison = ReadWitness.Create(new Carrier(), new Relation(currentEvent, earlierEvent),
            Relation.Segment(brain.TrendAxis, -1, 1), new CarrierLocation(brain.TrendAxis, trend),
            new Placeholder(), new Relation(currentEvent, earlierEvent));
        previousCenterLight = currentCenter;
        previousCenterEvent = currentEvent;
        latestSensors =
        [
            CoarseReadSurface.Measure(new Carrier(), brain.LightPartition, LightAt(X - 1, tick), new Placeholder()),
            CoarseReadSurface.Measure(new Carrier(), brain.LightPartition, LightAt(X + 1, tick), new Placeholder()),
            CoarseReadSurface.Measure(new Carrier(), brain.BodyPartition, Charge, new Placeholder()),
            CoarseReadSurface.Measure(new Carrier(), brain.HeatPartition, Heat, new Placeholder()),
            CoarseReadSurface.Measure(new Carrier(), brain.TrendPartition, trend, comparison)
        ];
        return latestSensors;
    }

    public Relation SpatialComparator(NativeBrain brain, int tick)
    {
        var pair = new Relation(latestSensors[1], latestSensors[0]);
        var sign = LightAt(X + 1, tick).CompareTo(LightAt(X - 1, tick));
        var instrument = ReadWitness.Create(new Carrier(), pair, Relation.Segment(brain.SpatialOrderAxis, -1, 1),
            new CarrierLocation(brain.SpatialOrderAxis, sign), new Placeholder(), pair);
        // The ordering instrument establishes only a sign class, never an
        // exact light difference of one unit on the light-magnitude axis.
        return CoarseReadSurface.Publish(new Carrier(), brain.GradientPartition, brain.SignMask(sign), instrument);
    }

    public void Advance(int tick, int action, bool queried)
    {
        var before = X;
        X = Math.Clamp(X + action, 0, Cells - 1);
        if (X != before) Movements++;
        var distance = Math.Abs(X - ChargerX(tick));
        var income = distance switch { 0 => 9, 1 => 6, 2 => 3, _ => -1 };
        Heat = Math.Clamp(Heat + (distance <= 2 ? 5 : -4), 0, Maximum);
        Charge = Math.Clamp(Charge + income - (X != before ? 1 : 0) - (Heat >= 90 ? 3 : 0) - (queried ? QueryChargeCost : 0), 0, Maximum);
        if (Charge <= 5) StarvationTicks++;
        if (Heat >= 90) OverheatTicks++;
        ChargeSum += Charge;
        if (Charge >= 80) cycleArmed = true;
        if (cycleArmed && Heat <= 20 && distance >= 3 && Charge >= 20) { Cycles++; cycleArmed = false; }
    }
}

/// <summary>A builder and narrow local reader for the native controller. Rule
/// support, responses, calibration, and all persistent memory live in Gojo.</summary>
public sealed class NativeBrain
{
    public Carrier LightAxis { get; } = new();
    public Carrier BodyAxis { get; } = new();
    public Carrier HeatAxis { get; } = new();
    public Carrier TrendAxis { get; } = new();
    public Carrier SpatialOrderAxis { get; } = new();
    public Carrier ActionAxis { get; } = new();
    public Carrier ModeMemory { get; } = new();
    public Carrier HeadingMemory { get; } = new();
    public Relation LightPartition { get; }
    public Relation GradientPartition { get; }
    public Relation BodyPartition { get; }
    public Relation HeatPartition { get; }
    public Relation TrendPartition { get; }
    public Relation ActionPartition { get; }
    public Relation ModePartition { get; }
    public Relation HeadingPartition { get; }
    public Relation DifferenceLaw { get; }
    public Relation DifferenceTable { get; }
    public Relation Root { get; }
    public Relation Snapshot { get; private set; }
    public long TotalOperations { get; private set; }
    public int AmbiguityTicks { get; private set; }
    public int InitialActionUnresolvedTicks { get; private set; }
    public int RefinementQueries { get; private set; }
    private readonly Carrier leftRole = new(), rightRole = new(), chargeRole = new(), heatRole = new(), trendRole = new(), gradientRole = new(), modeRole = new(), headingRole = new(), actionRole = new();
    private readonly Relation modeRules, actionRules, refineRules, fallbackRules, headingRules;
    private readonly Dictionary<Relation, string> labels = new(ReferenceEqualityComparer.Instance);
    private readonly string backend;
    private readonly CoarseDifferencePlan? packed;

    public NativeBrain(int round, string strategy, string executionBackend, ReceiverVariant? variant = null)
    {
        variant ??= ReceiverVariant.Baseline;
        backend = executionBackend;
        LightPartition = Partition(LightAxis, variant.LightRegions);
        GradientPartition = variant.Deadband == 0
            ? Partition(LightAxis, (-100, -1), (0, 0), (1, 100))
            : Partition(LightAxis, (-100, -variant.Deadband - 1), (-variant.Deadband, -1), (0, 0), (1, variant.Deadband), (variant.Deadband + 1, 100));
        BodyPartition = Partition(BodyAxis, (0, 24), (25, 49), (50, 74), (75, 100));
        HeatPartition = Partition(HeatAxis, (0, 24), (25, 49), (50, 74), (75, 100));
        TrendPartition = Partition(TrendAxis, (-1, -1), (0, 0), (1, 1));
        ActionPartition = Partition(ActionAxis, (-1, -1), (0, 0), (1, 1));
        ModePartition = Partition(ModeMemory, (0, 0), (1, 1));
        HeadingPartition = Partition(HeadingMemory, (0, 0), (1, 1), (2, 2));
        DifferenceLaw = CoarseReadSurface.DifferenceLaw(new Carrier(), LightPartition, GradientPartition);
        DifferenceTable = CoarseReadSurface.CompileDifference(new Carrier(), DifferenceLaw);
        if (backend == "Packed") packed = CoarseDifferencePlan.Capture(DifferenceTable) ?? throw new ArgumentException("The difference table could not be captured.");
        HeadingMemory.Set(2);
        Relation Condition(Carrier role, Relation partition, params int[] regions) =>
            Group(role, partition, Group(Regions(partition, regions)));
        Relation Charge(params int[] regions) => Condition(chargeRole, BodyPartition, regions);
        Relation Heat(params int[] regions) => Condition(heatRole, HeatPartition, regions);
        Relation Mode(int mode) => Condition(modeRole, ModePartition, mode);
        Relation Gradient(params int[] signClasses) => Condition(gradientRole, GradientPartition, signClasses);
        Relation Trend(int signClass) => Condition(trendRole, TrendPartition, signClass);
        Relation Constant(string label, Carrier output, int value, params Relation[] conditions) =>
            Rule(label, Group(conditions), new CarrierLocation(output, value), Identity(output));
        Relation Heading(string label, bool reversed, params Relation[] conditions) =>
            Rule(label, Group(conditions), HeadingMemory.ValueSlot,
                new Relation(Relation.Segment(HeadingMemory, 0, 2), Relation.Segment(ActionAxis, reversed ? 1 : -1, reversed ? -1 : 1)));

        if (round == 0)
        {
            modeRules = Group(Constant("Baseline has no charge policy", ModeMemory, 0));
            actionRules = Group(Constant(strategy == "stay" ? "Baseline stays" : "Baseline continues right", ActionAxis, strategy == "stay" ? 0 : 1));
            refineRules = Group(Constant("No optional query", ActionAxis, 0));
            fallbackRules = actionRules;
        }
        else
        {
            modeRules = round == 1
                ? Group(Constant("Heat guard: leave the source", ModeMemory, 1, Heat(3)),
                    Constant("High charge: leave the source", ModeMemory, 1, Charge(3)),
                    Constant("Seek charge", ModeMemory, 0))
                : Group(Constant("Heat guard: enter cool mode", ModeMemory, 1, Heat(3)),
                    Constant("Charge low and cooled: seek again", ModeMemory, 0, Mode(1), Charge(0, 1), Heat(0)),
                    Constant("Charge high: enter cool mode", ModeMemory, 1, Mode(0), Charge(3)),
                    Rule("Hold hysteresis mode", Group(), ModeMemory.ValueSlot, Identity(ModeMemory)));
            var actions = new List<IElement>();
            if (round == 2) actions.Add(Constant("Cooled and charged: rest", ActionAxis, 0, Mode(1), Heat(0), Charge(2, 3)));
            var negative = RegionIndexes(GradientPartition, (low, high) => high < 0);
            var positive = RegionIndexes(GradientPartition, (low, high) => low > 0);
            var zero = RegionIndexes(GradientPartition, (low, high) => low == 0 && high == 0);
            if (variant.Deadband > 0)
            {
                var near = RegionIndexes(GradientPartition, (low, high) => low >= -variant.Deadband && high <= variant.Deadband);
                actions.Add(Constant("Seek: entire difference support is small, stay", ActionAxis, 0, Mode(0), Gradient(near)));
                actions.Add(Heading("Cool: entire difference support is small, continue motion", false, Mode(1), Gradient(near)));
            }
            actions.AddRange([
                Constant("Seek: right is brighter", ActionAxis, 1, Mode(0), Gradient(positive)),
                Constant("Seek: left is brighter", ActionAxis, -1, Mode(0), Gradient(negative)),
                Constant("Seek: matched light, stay to charge", ActionAxis, 0, Mode(0), Gradient(zero)),
                Constant("Cool: leave towards the left", ActionAxis, -1, Mode(1), Gradient(positive)),
                Constant("Cool: leave towards the right", ActionAxis, 1, Mode(1), Gradient(negative)),
                Heading("Cool: leave the matched-light peak", false, Mode(1), Gradient(zero))
            ]);
            actionRules = Group(actions.ToArray());
            refineRules = round == 2 && strategy != "round2_no_query"
                ? Group(Constant("Refine: seeking needs a direction", ActionAxis, 1, Mode(0)),
                    Constant("Refine: cooling still needs to leave", ActionAxis, 1, Mode(1), Heat(1, 2, 3)),
                    Constant("No optional query", ActionAxis, 0))
                : Group(Constant("No optional query", ActionAxis, 0));
            fallbackRules = Group(Heading("Seek fallback: dimming, reverse", true, Mode(0), Trend(0)),
                Heading("Cool fallback: brightening, reverse", true, Mode(1), Trend(2)),
                Heading("Fallback: continue last motion", false));
        }
        headingRules = Group(Constant("Remember left motion", HeadingMemory, 0, Condition(actionRole, ActionPartition, 0)),
            Constant("Remember right motion", HeadingMemory, 2, Condition(actionRole, ActionPartition, 2)),
            Rule("Keep last nonzero motion", Group(), HeadingMemory.ValueSlot, Identity(HeadingMemory)));
        Root = Group(Group(LightPartition, GradientPartition, BodyPartition, HeatPartition, TrendPartition, ModePartition, HeadingPartition, ActionPartition,
                Relation.Segment(SpatialOrderAxis, -1, 1)),
            Group(leftRole, rightRole, chargeRole, heatRole, trendRole, gradientRole, modeRole, headingRole, actionRole),
            Group(ModeMemory, HeadingMemory), modeRules, actionRules, refineRules, fallbackRules, headingRules,
            DifferenceLaw, DifferenceTable);
        Snapshot = Root;
    }

    public Decision Decide(Relation[] sensors, Func<Relation> spatialComparator)
    {
        TotalOperations += sensors.Length;
        var bindings = new Dictionary<Carrier, Relation>(ReferenceEqualityComparer.Instance)
        {
            [leftRole] = sensors[0], [rightRole] = sensors[1], [chargeRole] = sensors[2], [heatRole] = sensors[3], [trendRole] = sensors[4],
            [modeRole] = CoarseReadSurface.Measure(new Carrier(), ModePartition, (int)ModeMemory.Value, ModeMemory),
            [headingRole] = CoarseReadSurface.Measure(new Carrier(), HeadingPartition, (int)HeadingMemory.Value, HeadingMemory)
        };
        var gradient = backend switch
        {
            "Lookup" => CoarseReadSurface.DifferenceLookup(new Carrier(), DifferenceTable, sensors[1], sensors[0]),
            "Packed" => packed!.Evaluate(new Carrier(), sensors[1], sensors[0]),
            _ => CoarseReadSurface.DifferenceComputed(new Carrier(), DifferenceLaw, sensors[1], sensors[0])
        };
        TotalOperations += 3;
        bindings[gradientRole] = gradient;
        var gradientMask = Mask(gradient, GradientPartition);
        var ambiguous = !gradientMask.HasValue || BitCount(gradientMask.Value) != 1;
        if (ambiguous) AmbiguityTicks++;
        var mode = Select(modeRules, bindings);
        Apply(ModeMemory, mode);
        bindings[modeRole] = CoarseReadSurface.Measure(new Carrier(), ModePartition, (int)ModeMemory.Value, ModeMemory);
        TotalOperations++;
        var action = Select(actionRules, bindings);
        var initialActionUnresolved = !action.Resolved;
        if (initialActionUnresolved) InitialActionUnresolvedTicks++;
        var initialAction = action.Witness;
        var refined = false;
        Relation? query = null;
        Selection? gate = null;
        if (!action.Resolved)
        {
            gate = Select(refineRules, bindings);
            if (gate.Resolved && gate.Value.Ticks == 1)
            {
                query = spatialComparator(); RefinementQueries++; TotalOperations++;
                bindings[gradientRole] = query;
                action = Select(actionRules, bindings);
                refined = true;
            }
            if (!action.Resolved) action = Select(fallbackRules, bindings);
        }
        bindings[actionRole] = CoarseReadSurface.Measure(new Carrier(), ActionPartition, action.Resolved ? (int)action.Value.Ticks : 0, action.Witness);
        var memory = Select(headingRules, bindings);
        Apply(HeadingMemory, memory);
        TotalOperations++;
        Snapshot = Group(Root, Group(bindings.Select(binding => (IElement)new Relation(binding.Key, binding.Value)).ToArray()),
            mode.Witness, Group(initialAction, action.Witness), memory.Witness, gate?.Witness ?? (IElement)new Placeholder(), gradient, query ?? (IElement)new Placeholder());
        return new(Mask(sensors[0], LightPartition), Mask(sensors[1], LightPartition), Mask(sensors[2], BodyPartition), Mask(sensors[3], HeatPartition), gradientMask,
            Scalar(sensors[4], TrendPartition), (int)ModeMemory.Value, action.Resolved ? (int)action.Value.Ticks : 0,
            refined, action.Label, ambiguous, query is null ? null : Mask(query, GradientPartition), initialActionUnresolved);
    }

    // The external comparator promises ordering only. Its publication therefore
    // admits every native region with that sign, including near/far alternatives.
    public int SignMask(int sign) => RegionIndexes(GradientPartition, (low, high) =>
        sign < 0 ? high < 0 : sign > 0 ? low > 0 : low == 0 && high == 0)
        .Aggregate(0, (mask, index) => mask | 1 << index);

    public string[] LightAlphabet => Bounds(LightPartition).Select(region => $"{region.Low}–{region.High}").ToArray();
    public string[] GradientAlphabet => Bounds(GradientPartition).Select(region =>
        region.Low == 0 && region.High == 0 ? "0 · equal" : $"{region.Low}…{region.High} · {(region.High < 0 ? "left brighter" : "right brighter")}").ToArray();

    // This display read follows the first native seek-action condition. It is
    // descriptive only; the action reader still follows the native rule surface.
    public int ReadDeadband()
    {
        actionRules.TryReadOrderedGroup(out var rules);
        foreach (var entry in rules)
            if (entry is Relation rule && rule.TryReadOrderedGroup(out var fields) && fields[0] is Relation conditions && conditions.TryReadOrderedGroup(out var checks))
                foreach (var check in checks)
                    if (check is Relation condition && condition.TryReadOrderedGroup(out var parts) && ReferenceEquals(parts[0], gradientRole) && parts[2] is Relation allowed && allowed.TryReadOrderedGroup(out var regions))
                    {
                        var spans = regions.Cast<Relation>().Select(Span).ToArray();
                        return spans.Length == 3 && spans.Any(span => span.Low == 0 && span.High == 0)
                            ? spans.Max(span => Math.Max(Math.Abs(span.Low), Math.Abs(span.High))) : 0;
                    }
        return 0;
    }

    public static (int Low, int High)[] Bounds(Relation partition)
    {
        if (!CoarseReadSurface.TryReadPartition(partition, out _, out var regions)) throw new InvalidDataException("Invalid native partition.");
        return regions.Select(Span).ToArray();
    }
    private static (int Low, int High) Span(Relation region)
    {
        static CarrierLocation Point(IElement element) => element switch
        {
            CarrierLocation point => point,
            Relation { Left: CarrierLocation point } => point,
            _ => throw new InvalidDataException("Invalid native region endpoint.")
        };
        return ((int)Point(region.Left).Ticks, (int)Point(region.Right).Ticks);
    }
    private static int[] RegionIndexes(Relation partition, Func<int, int, bool> predicate) =>
        Bounds(partition).Select((region, index) => (region, index)).Where(item => predicate(item.region.Low, item.region.High)).Select(item => item.index).ToArray();

    // Read a portable description using correspondences *inside* one graph.
    // Carrier identities and transient-tick representation are not compared
    // across an encoded/decoded independent world.
    public static string ActionRuleSignature(Relation policy)
    {
        static IElement[] Fields(Relation relation)
        {
            if (!relation.TryReadOrderedGroup(out var fields)) throw new InvalidDataException("Expected native ordered group.");
            return fields;
        }
        static CarrierLocation Point(IElement element) => element switch
        {
            CarrierLocation point => point,
            Relation { Left: CarrierLocation point } => point,
            _ => throw new InvalidDataException("Expected native coordinate.")
        };
        var fields = Fields(policy);
        var partitions = Fields((Relation)fields[0]).Take(8).Cast<Relation>().ToArray();
        var roles = Fields((Relation)fields[1]).Cast<Carrier>().ToArray();
        var cells = Fields((Relation)fields[2]).Cast<Carrier>().ToArray();
        var axes = partitions.Select(partition =>
            CoarseReadSurface.TryReadPartition(partition, out var axis, out _) ? axis : throw new InvalidDataException("Invalid native condition partition.")).ToArray();
        string Coordinate(CarrierLocation location) => $"{Array.FindIndex(axes, axis => axis.SameIdentityAs(location.Carrier))}:{location.Ticks}";
        string Frame(Relation frame) => $"{Coordinate(Point(frame.Left))}..{Coordinate(Point(frame.Right))}";
        var result = new StringBuilder();
        foreach (var entry in Fields((Relation)fields[4]))
        {
            var rule = Fields((Relation)entry);
            foreach (var check in Fields((Relation)rule[0]))
            {
                var condition = Fields((Relation)check);
                var roleIndex = Array.FindIndex(roles, role => role.SameIdentityAs((Carrier)condition[0]));
                var partitionIndex = Array.FindIndex(partitions, partition => partition.Equals(condition[1]));
                if (roleIndex < 0 || partitionIndex < 0) throw new InvalidDataException("Action condition lost its local role or partition.");
                CoarseReadSurface.TryReadPartition(partitions[partitionIndex], out _, out var regions);
                var admitted = Fields((Relation)condition[2]).Select(region => Array.FindIndex(regions, candidate => candidate.Equals(region))).ToArray();
                if (admitted.Any(index => index < 0)) throw new InvalidDataException("Action condition lost an admitted native region.");
                result.Append($"[{roleIndex}/{partitionIndex}:{string.Join(',', admitted)}]");
            }
            var source = Point(rule[1]);
            var liveCell = Array.FindIndex(cells, cell => cell.ValueSlot.SameTickRefAs(source));
            var calibration = (Relation)rule[2];
            result.AppendLine($"=>{Coordinate(source)};live={liveCell};{Frame((Relation)calibration.Left)}->{Frame((Relation)calibration.Right)}");
        }
        return result.ToString();
    }

    private Selection Select(Relation rules, Dictionary<Carrier, Relation> bindings)
    {
        rules.TryReadOrderedGroup(out var entries);
        var evidence = new List<IElement>();
        foreach (var element in entries)
        {
            if (element is not Relation rule || !rule.TryReadOrderedGroup(out var fields) || fields.Length != 3 ||
                fields[0] is not Relation conditions || !conditions.TryReadOrderedGroup(out var checks) ||
                fields[1] is not CarrierLocation source || fields[2] is not Relation calibration) continue;
            var admitted = true;
            foreach (var check in checks)
            {
                TotalOperations++;
                if (check is not Relation condition || !condition.TryReadOrderedGroup(out var parts) || parts.Length != 3 ||
                    parts[0] is not Carrier role || parts[1] is not Relation partition || parts[2] is not Relation permitted ||
                    !permitted.TryReadOrderedGroup(out var accepted) || !bindings.TryGetValue(role, out var publication) ||
                    !TrySupport(publication, partition, out var support) || support.Length == 0 ||
                    support.Any(region => !accepted.Any(candidate => candidate is Relation admittedRegion && admittedRegion.Equals(region))))
                { admitted = false; break; }
                evidence.Add(new Relation(condition, publication));
            }
            if (!admitted) continue;
            TotalOperations++;
            if (calibration.Right is not Relation target || target.Left is not Relation point || point.Left is not CarrierLocation start ||
                !calibration.TryProject(source, start.Carrier, out var prediction) || prediction.Left is not CarrierLocation result) continue;
            var witness = ReadWitness.Create(new Carrier(), rules, rule, result, prediction.Right, Group(evidence.Append(prediction).ToArray()));
            return new(true, result, witness, labels[rule]);
        }
        return new(false, default, ReadWitness.Create(new Carrier(), rules, rules, new Placeholder(), Group(evidence.ToArray()), rules), "Unresolved action");
    }

    private void Apply(Carrier memory, Selection selection)
    {
        if (!selection.Resolved || !selection.Value.Carrier.SameIdentityAs(memory)) return;
        memory.Set(selection.Value.Ticks);
        TotalOperations++;
    }

    private Relation Rule(string label, Relation conditions, CarrierLocation source, Relation calibration)
    {
        var relation = Group(conditions, source, calibration);
        labels.Add(relation, label); return relation;
    }
    private static Relation Identity(Carrier axis) => new(Relation.Segment(axis, 0, 1), Relation.Segment(axis, 0, 1));
    private static Relation Partition(Carrier axis, params (int Low, int High)[] regions) => CoarseReadSurface.Partition(new Carrier(), axis, regions);
    private static IElement[] Regions(Relation partition, int[] indexes)
    {
        CoarseReadSurface.TryReadPartition(partition, out _, out var regions);
        return indexes.Select(index => (IElement)regions[index]).ToArray();
    }
    private static bool TrySupport(Relation publication, Relation partition, out IElement[] support)
    {
        if (CoarseReadSurface.TryReadMask(publication, partition, out _) &&
            ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _) && result is Relation group &&
            group.TryReadOrderedGroup(out support)) return true;
        support = []; return false;
    }
    public static int? Mask(Relation publication, Relation partition) =>
        CoarseReadSurface.TryReadMask(publication, partition, out var mask) ? mask : null;
    private static int? Scalar(Relation publication, Relation partition)
    {
        var mask = Mask(publication, partition);
        return mask switch { 1 => -1, 2 => 0, 4 => 1, _ => null };
    }
    private static int BitCount(int value) => System.Numerics.BitOperations.PopCount((uint)value);
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private sealed record Selection(bool Resolved, CarrierLocation Value, Relation Witness, string Label);
}

public record Decision(int? LeftMask, int? RightMask, int? ChargeMask, int? HeatMask, int? GradientMask, int? Trend, int Mode, int Action,
    bool Refined, string Reason, bool Ambiguous, int? QueryMask, bool InitialActionUnresolved);
public record WorldStep(int Tick, int X, int ChargerX, int Charge, int Heat, int? LeftMask, int? RightMask, int? ChargeMask, int? HeatMask,
    int? GradientMask, int? Trend, int Mode, int Action, bool Refined, string Reason, int Cycles, bool Ambiguous, int? QueryMask, bool InitialActionUnresolved);
public record TrialSummary(int CompletedCycles, int StarvationTicks, int OverheatTicks, double MeanCharge, int Movements,
    int AmbiguityTicks, int RefinementQueries, long TotalOperations, double ElapsedMs, long AllocatedBytes,
    string ParityHash, long ArchiveBytes, int FinalCharge, int FinalHeat, int InitialActionUnresolvedTicks);
public record Trial(string Id, string Label, int Round, string Backend, TrialSummary Summary, WorldStep[] Steps, string ArchiveFile,
    string VariantId, string GroupLabel, string InterpretationLabel, string InterpretationDescription, int Deadband,
    string[] LightMaskAlphabet, string[] GradientAlphabet, string[] QueryAlphabet, string Phase);

// These are manual fixture recipes for constructing native surfaces, not a
// second semantic controller. After construction the connected rules decide.
public sealed record ReceiverVariant(string Id, string LightLabel, (int Low, int High)[] LightRegions, string InterpretationLabel, int Deadband)
{
    public static ReceiverVariant Baseline => All[0];
    public static ReceiverVariant[] All { get; } = Create();
    private static ReceiverVariant[] Create()
    {
        var lights = new (string Id, string Label, (int, int)[] Regions)[]
        {
            ("baseline", "Baseline light buckets", [(0,14),(15,44),(45,79),(80,100)]),
            ("uniform", "Uniform light buckets", [(0,24),(25,49),(50,74),(75,100)]),
            ("dimfocused", "Dim-focused light buckets", [(0,7),(8,23),(24,55),(56,100)]),
            ("brightfocused", "Bright-focused light buckets", [(0,49),(50,79),(80,91),(92,100)])
        };
        return lights.SelectMany(light => new[] { ("strict0",0), ("gentle4",4), ("broad10",10) }.Select(interpretation =>
            new ReceiverVariant($"{light.Id}_{interpretation.Item1}", light.Label, light.Regions, interpretation.Item1, interpretation.Item2))).ToArray();
    }
}

public static class Trials
{
    public static Trial Run(int round, string strategy, string backend, int ticks, string output, ReceiverVariant? variant = null, string phase = "default")
    {
        Directory.CreateDirectory(output);
        var brain = new NativeBrain(round, strategy, backend, variant);
        var world = new ChargeWorld();
        var steps = new WorldStep[ticks];
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        for (var tick = 0; tick < ticks; tick++)
        {
            var decision = brain.Decide(world.Sense(brain, tick), () => world.SpatialComparator(brain, tick));
            steps[tick] = new(tick, world.X, world.ChargerX(tick), world.Charge, world.Heat, decision.LeftMask,
                decision.RightMask, decision.ChargeMask, decision.HeatMask, decision.GradientMask, decision.Trend, decision.Mode, decision.Action,
                decision.Refined, decision.Reason, world.Cycles, decision.Ambiguous, decision.QueryMask, decision.InitialActionUnresolved);
            world.Advance(tick, decision.Action, decision.Refined);
        }
        timer.Stop();
        var allocation = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var id = $"{(phase is "default" or "discovery" ? "" : phase + "_")}{(variant is null ? "" : variant.Id + "_")}{strategy}_{backend.ToLowerInvariant()}";
        var bytes = GojoNumericFormat.Encode(brain.Snapshot);
        var archiveFile = id + ".gojo";
        File.WriteAllBytes(Path.Combine(output, archiveFile), bytes);
        var restored = GojoNumericFormat.Decode(bytes);
        if (restored.Length != 1 || restored[0] is not Relation state || !state.TryReadOrderedGroup(out var restoredFields) ||
            restoredFields.Length != 8 || restoredFields[0] is not Relation policy || !policy.TryReadOrderedGroup(out var policyFields) ||
            policyFields[0] is not Relation partitions || !partitions.TryReadOrderedGroup(out var frames) ||
            frames[0] is not Relation restoredLight || frames[1] is not Relation restoredGradient ||
            !NativeBrain.Bounds(restoredLight).SequenceEqual(NativeBrain.Bounds(brain.LightPartition)) ||
            !NativeBrain.Bounds(restoredGradient).SequenceEqual(NativeBrain.Bounds(brain.GradientPartition)) ||
            policyFields[2] is not Relation memory || !memory.TryReadOrderedGroup(out var cells) || cells.Length != 2 ||
            cells[0] is not Carrier mode || mode.Value != brain.ModeMemory.Value || cells[1] is not Carrier heading || heading.Value != brain.HeadingMemory.Value)
            throw new InvalidDataException("Brain archive did not retain its native partitions and memory.");
        if (NativeBrain.ActionRuleSignature(policy) != NativeBrain.ActionRuleSignature(brain.Root))
            throw new InvalidDataException("Brain archive action rule comparison failed.");
        if (restoredFields[6] is not Relation originalGradient || NativeBrain.Mask(originalGradient, restoredGradient) != steps[^1].GradientMask)
            throw new InvalidDataException("Brain archive coarse support comparison failed.");
        if (steps[^1].Refined && (restoredFields[7] is not Relation query || NativeBrain.Mask(query, restoredGradient) != steps[^1].QueryMask))
            throw new InvalidDataException("Brain archive query support comparison failed.");
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(steps)))).ToLowerInvariant();
        var label = strategy switch { "stay" => "Stay", "continue" => "Continue right", "seek_avoid" => "Round 1 · seek / avoid",
            "round2_no_query" => "Round 2 · memory / no query", _ => "Round 2 · memory / heat / query" };
        return new(id, label, round, backend, new(world.Cycles, world.StarvationTicks, world.OverheatTicks,
            (double)world.ChargeSum / ticks, world.Movements, brain.AmbiguityTicks, brain.RefinementQueries, brain.TotalOperations,
            timer.Elapsed.TotalMilliseconds, allocation, digest, bytes.Length, world.Charge, world.Heat, brain.InitialActionUnresolvedTicks), steps, archiveFile,
            variant?.Id ?? ReceiverVariant.Baseline.Id, phase == "default" ? "Original controller rounds" : phase == "discovery" ? variant!.LightLabel : $"Long validation · {phase}",
            brain.ReadDeadband() == 0 ? "strict0" : $"{(brain.ReadDeadband() == 4 ? "gentle" : "broad")}{brain.ReadDeadband()}", brain.ReadDeadband() == 0
                ? "Whole negative/positive support steers; only exact zero stays when seeking."
                : $"Zero remains separate. Only when the whole exposed support fits the near-negative/zero/near-positive union within ±{brain.ReadDeadband()} may seeking hold and cooling continue its remembered heading. Sign-only queries retain near/far alternatives.",
            brain.ReadDeadband(), brain.LightAlphabet, brain.GradientAlphabet, brain.GradientAlphabet, phase);
    }
}
