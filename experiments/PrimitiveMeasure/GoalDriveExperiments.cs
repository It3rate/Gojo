namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>These fields capture positive-context Boolean support. Zero means
/// a missing read. Fuel, Pending and Spent expose sites of one effort circuit.</summary>
public sealed record GoalDriveBeat(string Label, byte Target, byte Finished, byte FinishRead, byte Gap,
    byte Access, byte ExternalRequest, byte Fuel, byte Pending, byte Spent, byte HelpReleased, byte OnCourse);

public sealed record GoalDriveTrial(string Name, GoalDriveBeat[] Timeline, byte[] GapRow, byte[] WorkRows, byte[] BodyRows);

/// <summary>Two distinguishable resources. One activation marks the runner's
/// location. A second supplies a single opportunity for work. The target and
/// sensed finish select their lawful transfers. All scenarios run the same
/// clock; their wiring and the presented stimuli account for their differences.</summary>
public static class GoalDriveExperiments
{
    public static readonly string[] Names =
    [
        "Observe the finish goal", "One outside request opens the reserve",
        "Keep a local request until passage opens", "Keep the passage closed",
        "Start without work fuel", "Disconnect the finish read", "Conceal the finish read",
        "Withdraw and restore the target", "Start at the target",
        "Reach the finish through a second passage"
    ];

    public static GoalDriveTrial Run(int scenario)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)scenario, (uint)Names.Length);
        var device = new Device(localRows: scenario >= 2, helper: scenario == 1, fueled: scenario != 4,
            finished: scenario == 8, secondPassage: scenario == 9,
            substituteFinish: scenario switch { 5 => new Report(0), 6 => new Report(3), _ => null });
        var history = new List<GoalDriveBeat> { device.Observe("Before setting a target") };
        void Beat(string label, bool open = false, bool externalRequest = false)
        {
            device.Tick(open, externalRequest);
            history.Add(device.Observe(label));
        }
        void Target(string label, bool present)
        {
            device.Target.Set(present);
            history.Add(device.Observe(label));
        }

        Target("Set the finish target; only observe", true);
        if (scenario == 1)
        {
            Beat("One outside request meets closed ordinary access", externalRequest: true);
            Beat("The reserve samples the refusal; the outside request ends");
            Beat("Open assistance alone affords another attempt");
            Beat("A second outside request uses the opened passage", externalRequest: true);
        }
        else
        {
            Beat("First clock with the passage closed");
            Beat("Second clock with the passage closed");
            if (scenario == 7)
            {
                Target("Withdraw the target while work is pending", false);
                Beat("Open the passage while the target is withdrawn", open: true);
                Target("Restore the finish target", true);
            }
            if (scenario == 9)
                Beat("A separate authorized passage reaches the actual finish", externalRequest: true);
            Beat(scenario == 3 ? "Keep the passage closed" : "Open the passage", open: scenario != 3);
            Beat("Repeat the same opportunity", open: scenario != 3);
        }
        return new(Names[scenario], history.ToArray(), device.GapRow(), device.WorkRows(), device.BodyRows());
    }

    private sealed class Device
    {
        private readonly PulseCircuit body = new(2);
        private readonly IPrimitiveReadPort onCourse, finished, finishRead;
        private readonly CorrespondenceProbe gap;
        private readonly int bodyTable;
        private readonly Input access = new(), external = new();
        private readonly PulseCircuit? work;
        private readonly IPrimitiveReadPort? fuel, pending, spent;
        private readonly int workTable;
        private readonly RaceAssistance? assistance;
        public Input Target { get; } = new();

        public Device(bool localRows, bool helper, bool fueled, bool finished, bool secondPassage, IPrimitiveReadPort? substituteFinish)
        {
            var courseSite = body.State(!finished);
            var finishSite = body.State(finished);
            var move = body.Guard();
            onCourse = body.Expose(courseSite);
            this.finished = body.Expose(finishSite);
            finishRead = substituteFinish ?? this.finished;
            gap = new CorrespondenceProbe(ConnectionLaws.FirstWithoutSecond, Target.Port, finishRead);
            bodyTable = body.AddTable(PassageLaw.Controlled());
            body.Link(bodyTable, courseSite, move, finishSite);
            if (secondPassage)
            {
                var otherGate = body.Guard();
                body.Bind(otherGate, external.Port);
                body.Link(bodyTable, courseSite, otherGate, finishSite);
            }

            if (helper)
            {
                // The earlier race apparatus is operated as built: one outside
                // request can release the helper, but moving the body requires
                // the request to be present when access becomes available.
                var targetReached = new CorrespondenceProbe(Satisfied, Target.Port, finishRead);
                assistance = new(targetReached, onCourse, external.Port, access.Port);
                body.Bind(move, new CorrespondenceProbe(RaceExperiments.And, external.Port, assistance.EffectiveAccess));
                return;
            }

            work = new PulseCircuit(3);
            var stock = work.State(fueled);
            var waiting = work.State();
            var used = work.State();
            fuel = work.Expose(stock);
            pending = work.Expose(waiting);
            spent = work.Expose(used);
            workTable = work.AddTable(PassageLaw.Controlled());
            if (localRows)
            {
                var demand = work.Guard();
                var admission = work.Guard();
                work.Bind(demand, gap);
                work.Bind(admission, new CorrespondenceProbe(RaceExperiments.And, gap, access.Port));
                work.Link(workTable, stock, demand, waiting);
                work.Link(workTable, waiting, admission, used);
            }
            body.Bind(move, spent);
        }

        private static readonly BitRelation Satisfied = BitRelation.FromTuples(3,
            [[0, 0, 1], [0, 1, 1], [1, 0, 0], [1, 1, 1]]);

        public void Tick(bool open, bool outsideRequest)
        {
            // The helper samples the preceding beat. The local effort then
            // takes at most one step and the runner reads the committed result.
            assistance?.Step();
            access.Set(open);
            external.Set(outsideRequest);
            work?.Pulse();
            body.Pulse();
        }

        public GoalDriveBeat Observe(string label) => new(label, Target.Port.ReadSupport(),
            finished.ReadSupport(), finishRead.ReadSupport(), gap.ReadSupport(), access.Port.ReadSupport(),
            external.Port.ReadSupport(), fuel?.ReadSupport() ?? 0, pending?.ReadSupport() ?? 0,
            spent?.ReadSupport() ?? 0, assistance?.Released.ReadSupport() ?? 0, onCourse.ReadSupport());

        public byte[] GapRow() => gap.RowBytes();
        public byte[] WorkRows() => work?.PackedRows(workTable) ?? [];
        public byte[] BodyRows() => body.PackedRows(bodyTable);
    }

    private sealed class Input
    {
        private readonly PortStructure owner = new();
        private readonly int binding;
        public PortConnection Port { get; }

        public Input()
        {
            binding = owner.Create(0, 0, 1, 0);
            Port = owner.Connect(owner.Publish(binding));
        }

        public void Set(bool present) => owner.StoreObservation(binding, false, false, present ? 1UL : 0UL);
    }

    private sealed class Report(byte support) : IPrimitiveReadPort
    {
        public byte ReadSupport() => support;
    }
}
