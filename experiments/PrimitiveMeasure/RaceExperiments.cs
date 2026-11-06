namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Read-only observations captured after each explicit input or inspection.
/// HostReads counts the upper decision's public rank queries; SupplierInspections
/// counts course continuity inspections. Neither counts rendering or elapsed time.</summary>
public sealed record RaceBeat(string Label, int Elapsed, byte StartSignal, byte ARequest, byte BRequest,
    byte AAccess, byte BAccess, int AState, int BState, byte GoalReached, byte FinishOrder,
    byte FavoriteWon, int HostReads, long SupplierInspections, byte Assistance)
{
    public byte BEffectiveAccess { get; init; }
    public byte AStore { get; init; }
    public byte BStore { get; init; }
}

public sealed record RaceTrial(string Name, RaceBeat[] Timeline, byte LandmarkOrder, byte FinishOrder,
    byte FavoriteOrder, byte FavoriteWon, byte GoalReached, int HostReads, long SupplierInspections,
    byte[] LandmarkBytes, byte[] FinishBytes, byte[] PassageRows, byte[] PassageLawBytes,
    byte[] ConsumerLawBytes, int PrivateSteps, int[] CoursePrefix, StructuralRouteEdit[] CourseEdit)
{
    public RaceAssistance? AssistanceDevice { get; init; }
    public byte[] ConsumerRow { get; init; } = [];
    public bool FirstRoleIsB { get; init; }
    public bool ForecastMatchesRace { get; init; }
}

public static class RaceExperiments
{
    public static readonly string[] Cases =
    [
        "The favorite finishes first", "The favorite finishes second", "Exchange the reader's entrant roles",
        "Reshape the private course", "Two crossings in one coarse observation", "One witnessed shared finish",
        "Withhold the starting signal", "Obstruct B, then open the passage", "Forecast for another race",
        "Forecast for another entrant", "Correct the course record and recheck", "Withdraw the goal connection",
        "Connect the goal to a reserve", "Withdraw the goal from the reserve"
    ];

    internal static readonly BitRelation And = BitRelation.FromTuples(3,
        [[0, 0, 0], [0, 1, 0], [1, 0, 0], [1, 1, 1]]);

    public static RaceTrial Run(int scenario, int privateSteps = 16)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)scenario, (uint)Cases.Length);
        if (privateSteps is < 2 or > 16_384) throw new ArgumentOutOfRangeException(nameof(privateSteps));

        // One shared course supplies both admissions. A different ordering of
        // interior site references changes the course without measuring length.
        var vertices = Enumerable.Range(0, privateSteps + 1).ToArray();
        if (scenario == 3) Array.Reverse(vertices, 1, privateSteps - 1);
        var pairs = vertices.Zip(vertices.Skip(1), (source, target) => new[] { source, target }).ToArray();
        var course = StructuralRoute.Pairs(privateSteps + 2, pairs);
        course.Inspect(privateSteps);
        var start = new Sensor();
        var a = new Runner(start.Port, course.Continuity);
        var b = new Runner(start.Port, course.Continuity);
        var events = new FinishEvents(course.Continuity, scenario == 2);
        var goal = new CorrespondenceProbe(And, a.Finished, scenario is 11 or 13 ? Missing.Instance : b.Finished);
        RaceAssistance? assistance = null;
        if (scenario is 12 or 13)
        {
            assistance = new RaceAssistance(goal, b.Running, b.Request.Port, b.Access.Port);
            b.BindAccess(assistance.EffectiveAccess);
        }

        // Rank the same entrants on two different rails. The forecast remains
        // a projection made before the contest and cannot feed either lane.
        var landmark = new LandmarkRun(1, [0, 1]);
        bool exchanged = scenario == 2;
        var landmarkRead = new RaceOrderRead(landmark, exchanged ? 1 : 0, exchanged ? 0 : 1);
        var board = new LandmarkRun(1, [0, 1]);
        var forecast = new RaceOrderRead(board, exchanged ? 1 : 0, exchanged ? 0 : 1);
        var frame = new RaceFrame(events.Contest, exchanged ? b.Finished : a.Finished, exchanged ? a.Finished : b.Finished);
        var boardFrame = frame;
        if (scenario == 8) boardFrame = boardFrame with { Contest = new Sensor().Port };
        if (scenario == 9) boardFrame = boardFrame with { First = new Sensor().Port };
        var decision = new RaceDecision(new(boardFrame, forecast), new(frame, events));
        var beats = new List<RaceBeat>();
        var edits = new List<StructuralRouteEdit>();
        var elapsed = 0;

        void Capture(string label)
        {
            var won = decision.ReadSupport();
            beats.Add(new(label, elapsed, start.Port.ReadSupport(), a.Request.Port.ReadSupport(),
                b.Request.Port.ReadSupport(), a.Access.Port.ReadSupport(), b.Access.Port.ReadSupport(),
                a.State, b.State, goal.ReadSupport(), events.ReadSupport(), won,
                decision.HostReads, course.Inspections, assistance?.Released.ReadSupport() ?? 0)
                { BEffectiveAccess = b.EffectiveAccess.ReadSupport(), AStore = a.Store, BStore = b.Store });
        }

        void Advance(string label, bool requestA = false, bool requestB = false,
            bool accessB = true, bool sharedEvent = false)
        {
            elapsed++;
            // The reserve gets the same opportunity every beat. It reads the
            // preceding beat's observations before new stimuli are imported.
            assistance?.Step();
            a.Request.Observe(requestA);
            b.Request.Observe(requestB);
            b.Access.Observe(accessB);
            // Both passages read signals for this one beat. Source call order
            // supplies no precedence to the finish clock.
            var first = a.Advance();
            var second = b.Advance();
            events.Observe(first, second, sharedEvent);
            Capture(label);
        }

        Capture("Waiting at the common start");
        start.Observe(scenario != 6);
        Advance(scenario == 6 ? "Starting signal withheld" : "Common start released");
        start.Observe(false);
        if (scenario == 6)
            Advance("Finish requests while still at the start", true, true);
        else if (scenario is 4 or 5)
            Advance(scenario == 4 ? "Two crossing contacts in one coarse beat" : "Both contacts share a witnessed event",
                true, true, sharedEvent: scenario == 5);
        else if (scenario is 7 or 12 or 13)
        {
            Advance("Both request passage; B is blocked", true, true, false);
            if (assistance is null)
                Advance("Open B's passage and retry", requestB: true);
            else
            {
                Advance("Reserve samples the preceding goal and obstruction", accessB: false);
                Advance("Retry B with the ordinary passage still blocked", requestB: true, accessB: false);
            }
        }
        else
        {
            bool bFirst = scenario == 1;
            Advance(bFirst ? "B crosses first" : "A crosses first", !bFirst, bFirst);
            Advance(bFirst ? "A crosses next" : "B crosses next", bFirst, !bFirst);
        }

        if (scenario == 10)
        {
            // Correct the join in this race's course record. Two pair rows
            // share its replacement by explicit edits, preserving outer ends.
            int interior = privateSteps / 2;
            edits.Add(course.TryRetargetEndpoint(interior - 1, 1, privateSteps + 1));
            edits.Add(course.TryRetargetEndpoint(interior, 0, privateSteps + 1));
            Capture("Course record edited; previous qualification withdrawn");
            course.Inspect(privateSteps / 2);
            Capture("Part of the corrected course checked");
            course.Inspect(privateSteps);
            Capture("Whole course rechecked");
        }

        var last = beats[^1];
        var prefix = new List<int> { course.Edge(0)[0] };
        for (int i = 0; i < Math.Min(3, privateSteps); i++) prefix.Add(course.Edge(i)[1]);
        return new(Cases[scenario], beats.ToArray(), landmarkRead.ReadSupport(), last.FinishOrder,
            forecast.ReadSupport(), last.FavoriteWon, last.GoalReached, last.HostReads, last.SupplierInspections,
            landmark.SiteBytes(), events.Bytes(), a.Rows, a.Law, decision.LawBytes(), privateSteps,
            prefix.ToArray(), edits.ToArray())
        {
            AssistanceDevice = assistance,
            ConsumerRow = decision.RowBytes(),
            FirstRoleIsB = frame.First.SamePortAs(b.Finished),
            ForecastMatchesRace = decision.Compatible
        };
    }

    private sealed class Sensor
    {
        private readonly PortStructure owner = new();
        private readonly int binding;
        public PortConnection Port { get; }

        public Sensor(bool present = false)
        {
            binding = owner.Create(0, 0, 1, present ? 1UL : 0UL);
            Port = owner.Connect(owner.Publish(binding));
        }

        public void Observe(bool present) => owner.StoreObservation(binding, false, false, present ? 1UL : 0UL);
    }

    private sealed class Missing : IPrimitiveReadPort
    {
        public static readonly Missing Instance = new();
        public byte ReadSupport() => 0;
    }

    private sealed class Runner
    {
        private readonly PulseCircuit lane = new(3);
        private readonly int table, admit;
        private readonly IPrimitiveReadPort course;
        public Sensor Request { get; } = new();
        public Sensor Access { get; } = new(true);
        public PortConnection Finished { get; }
        public IPrimitiveReadPort Running { get; }
        public IPrimitiveReadPort EffectiveAccess { get; private set; }
        public int State => lane.Active!.Value;
        public byte Store => lane.ValueBytes()[0];
        public byte[] Rows => lane.PackedRows(table);
        public byte[] Law => lane.LawBytes(table);

        public Runner(IPrimitiveReadPort start, IPrimitiveReadPort course)
        {
            int ready = lane.State(true), running = lane.State(), finish = lane.State();
            this.course = course;
            int release = lane.Guard();
            admit = lane.Guard();
            lane.Bind(release, start);
            EffectiveAccess = Access.Port;
            BindAccess(EffectiveAccess);
            table = lane.AddTable(PassageLaw.Controlled());
            lane.Link(table, ready, release, running);
            lane.Link(table, running, admit, finish);
            Finished = (PortConnection)lane.Expose(finish);
            Running = lane.Expose(running);
        }

        public void BindAccess(IPrimitiveReadPort access)
        {
            EffectiveAccess = access;
            var legal = new CorrespondenceProbe(And, course, access);
            lane.Bind(admit, new CorrespondenceProbe(And, Request.Port, legal));
        }

        public PulseStep Advance() => lane.Pulse();
    }

    /// <summary>A finish contact gets an event site only upon an actual
    /// running-to-finish transfer. Two contacts may be wired to *one* event
    /// or to independent events in the same coarse beat. Only strictly
    /// different observed beats establish a precedence row.</summary>
    private sealed class FinishEvents(IPrimitiveReadPort course, bool exchanged) : IPrimitiveReadPort
    {
        // A race's own finish contact distinguishes separate contests even
        // when both receive the identical external starting signal.
        private readonly Sensor finishContact = new();
        private readonly Sensor[] tickSlots = [new(), new()];
        private int usedTicks;
        private readonly int?[] contacts = new int?[2];
        private LandmarkRun? evidence;
        private EndpointRows? bindings;
        private RaceOrderRead? order;
        public PortConnection Contest => finishContact.Port;

        public void Observe(PulseStep first, PulseStep second, bool sharedEvent)
        {
            bool a = first is { Moved: true, Before: 1, After: 2 };
            bool b = second is { Moved: true, Before: 1, After: 2 };
            if (!a && !b) return;
            finishContact.Observe(true);
            // An earlier retained contact precedes the new one. Two contacts
            // arriving in the same sample supply no precedence in either direction.
            int? earlier = contacts[0] ?? contacts[1];
            if (a) contacts[0] = Tick();
            if (b) contacts[1] = sharedEvent && a ? contacts[0] : Tick();
            if (contacts[0] is not { } left || contacts[1] is not { } right) return;

            int[][] precedes = earlier.HasValue ? [[earlier.Value, a ? left : right]] : [];
            evidence = new LandmarkRun(1, [0, 1], precedes);
            bindings = new EndpointRows();
            var row = bindings.Append(1, left, right);
            var sites = bindings.Read(row);
            order = new RaceOrderRead(evidence, sites[exchanged ? 1 : 0], sites[exchanged ? 0 : 1]);
        }

        private int Tick()
        {
            int site = usedTicks++;
            tickSlots[site].Observe(true);
            return site;
        }

        public byte ReadSupport() => course.ReadSupport() switch
        {
            2 => order?.ReadSupport() ?? 7,
            3 => 7,
            _ => 0
        };

        public byte[] Bytes() => evidence is null ? [] :
            [.. bindings!.PackedBytes(2, 1), .. evidence.ClaimBytes()];
    }
}
