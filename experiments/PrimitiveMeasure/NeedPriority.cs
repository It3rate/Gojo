namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Chooses a basis through public need and offer reads. Its output
/// names one local role: 0 waits, 1 purse, 2 standing, 3 pickup. This priority
/// is supplied in the connections. It does not compare unlike amounts.</summary>
public sealed class NeedPriority
{
    private const int ReferenceBits = 5;
    private readonly ReadConnections reads = new();
    private readonly List<int> priorityTables = [];
    private readonly CalculatedPort selected;
    private readonly Signal access = new(), due = new();

    private sealed class Signal : IPrimitiveReadPort
    {
        public byte Support { get; set; } = 1;
        public byte ReadSupport() => Support;
    }

    private sealed class Offer(TensionChoiceMachine machine, int basis) : IPrimitiveReadPort
    {
        public byte ReadSupport() => Gojo.Experiments.PrimitiveMeasure.ReadSupport.Of(machine.OfferedActions(basis));
    }

    public NeedPriority(TensionChoiceMachine machine)
    {
        var overlapTable = reads.AddTable(BitRelation.FromTuples(3,
            from open in Enumerable.Range(0, 8)
            from offered in Enumerable.Range(0, 8)
            select new byte[] { (byte)open, (byte)offered, (byte)((open & offered) != 0 ? 1 : 0) }));
        var andTable = reads.AddTable(RaceExperiments.And);
        var available = reads.Import(access);
        var ready = new int[3];
        for (var basis = 0; basis < ready.Length; basis++)
        {
            var offered = reads.Import(new Offer(machine, basis));
            var reachable = reads.Connect(overlapTable, ReferenceBits, available, offered);
            ready[basis] = reads.Connect(andTable, ReferenceBits, reads.Import(machine.Need(basis)), reachable.Endpoint).Endpoint;
        }
        ready[2] = reads.Connect(andTable, ReferenceBits, ready[2], reads.Import(due)).Endpoint;
        var wait = reads.Import(new Signal());
        // A higher row replaces the earlier choice only with a known active need
        // and an advertised open outlet. Pickup also requires a due signal.
        var standing = Prefer(ready[1], wait, 2);
        var purse = Prefer(ready[0], standing.Endpoint, 1);
        selected = Prefer(ready[2], purse.Endpoint, 3);
    }

    private CalculatedPort Prefer(int eligible, int fallback, byte role)
    {
        var law = BitRelation.FromTuples(3,
            from active in Enumerable.Range(0, 2)
            from lower in Enumerable.Range(0, 4)
            select new byte[] { (byte)active, (byte)lower, active == 1 ? role : (byte)lower });
        var table = reads.AddTable(law);
        priorityTables.Add(table);
        return reads.Connect(table, ReferenceBits, eligible, fallback);
    }

    public byte Select(byte openMask, byte pickupDueSupport)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(openMask, (byte)7);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pickupDueSupport, (byte)3);
        access.Support = ReadSupport.Of(openMask);
        due.Support = pickupDueSupport;
        return selected.ReadSupport();
    }

    public byte[] PriorityRows() => priorityTables.SelectMany(t => reads.PackedRows(t, ReferenceBits)).ToArray();

    public static TensionChoiceTrial RunExample()
    {
        // The pickup outlet costs purse relief. The winning outlet can pay a
        // purse claim and improve standing on their own separate connections.
        var machine = new TensionChoiceMachine([8, 4, 2],
            [[1, 0, 0], [1, 1, 0], [-1, 0, 1]], offerMasks: [3, 2, 4]);
        var priority = new NeedPriority(machine);
        var history = new List<TensionChoiceBeat>();
        void Opportunity(string description, byte access, byte dueSupport)
        {
            var basisSupport = priority.Select(access, dueSupport);
            history.Add(machine.StepWithFocus(basisSupport, access) with
                { Event = description, PickupDueSupport = dueSupport });
        }
        Opportunity("Pickup is outstanding but is not due yet", 7, 1);
        Opportunity("A second ordinary opportunity", 7, 1);
        Opportunity("Pickup is due but its outlet is closed", 3, 2);
        Opportunity("Pickup outlet opens; deadline signal is uncertain", 7, 3);
        Opportunity("Confirmed deadline and open pickup outlet", 7, 2);
        Opportunity("Pickup still has a second outstanding claim", 7, 2);
        Opportunity("Pickup is satisfied; read the remaining needs", 7, 2);
        Opportunity("All outlets are closed", 0, 2);
        return new("Let a due pickup take priority", history.ToArray(), machine.ChoiceRows(), machine.ReliefRows())
            { PriorityRows = priority.PriorityRows(), FeedbackRows = machine.FeedbackRows() };
    }
}
