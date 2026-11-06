namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One local need is a finite strip of distinguishable sites. Moving a
/// marker from outstanding to accounted relieves one position in this basis.
/// The other needs have their own strips and their own tick conventions.</summary>
internal sealed class ChoiceNeed : IPrimitiveReadPort
{
    public const int Width = 48;
    private readonly PortStructure owner = new();
    private readonly int[] unpaid;
    private readonly PassageConnections movement;
    private readonly ConnectionRow[] pay, reopen;

    public ChoiceNeed(int initial)
    {
        if (initial < 0 || initial > Width) throw new ArgumentOutOfRangeException(nameof(initial));
        int Site(bool present) => owner.Publish(owner.Create(0, 0, 1, present ? 1UL : 0UL));
        unpaid = Enumerable.Range(0, Width).Select(i => Site(i < initial)).ToArray();
        var accounted = Enumerable.Range(0, Width).Select(i => Site(i >= initial)).ToArray();
        int control = Site(true);
        movement = new(owner, PassageLaw.Controlled());
        pay = unpaid.Select((port, i) => movement.Connect(7, port, control, accounted[i])).ToArray();
        reopen = accounted.Select((port, i) => movement.Connect(7, port, control, unpaid[i])).ToArray();
    }

    public int Remaining => unpaid.Count(port => owner.ReadPort(port) == 1);
    public byte ReadSupport() => unpaid.Any(port => owner.ReadPort(port) == 1) ? (byte)2 : (byte)1;

    public bool MoveOne(bool relieve)
    {
        foreach (var row in relieve ? pay : reopen)
        {
            var support = movement.Observe(row);
            if (support != 1 << PassageLaw.State(true, true, false)) continue;
            return movement.TryStep(row, support, out _);
        }
        return false;
    }
}

/// <summary>One conserved opportunity visits at most one of three action outlets.
/// Only advertised connections, current access, a selected need and retained
/// observations enter the chooser. The effect rows belong to the world side of
/// the outlet and never participate in scoring an action.</summary>
public sealed class TensionChoiceMachine
{
    public const int Unseen = int.MinValue;
    private const int Count = 3;
    private readonly ChoiceNeed[] needs;
    private readonly PortStructure opportunity = new(), memory = new(), signals = new();
    private readonly int[,] samples = new int[Count, Count];
    private readonly int[] access = new int[Count], gates = new int[Count], primed = new int[Count];
    private readonly int[,] offers = new int[Count, Count];
    private readonly ReadConnections[] selectors = new ReadConnections[Count];
    private readonly ConnectionRow[,] permissions = new ConnectionRow[Count, Count];
    private readonly int[] permissionTables = new int[Count];
    private readonly PassageConnections grant, recycle;
    private readonly ConnectionRow[] grants = new ConnectionRow[Count], returns = new ConnectionRow[Count];
    private readonly EndpointRows effects = new();
    private readonly EndpointRows feedback = new();
    private readonly ReadConnections useMemory = new();
    private readonly int rememberUse;
    private readonly ConnectionRow[] useRows = new ConnectionRow[Count];
    private readonly PulseCircuit rotation = new(3);
    private int turn;

    // Codes here are in a local outcome basis: unseen, worse, unchanged,
    // one step relieved, two steps relieved. This is not ODV addition.
    private static readonly BitRelation Rank = Binary(5, 2, (sample, open) =>
        open == 0 ? 0 : sample switch { 0 => 3, 3 => 1, 4 => 2, _ => 0 });
    private static readonly BitRelation NoLower = Binary(4, 4, (first, second) =>
        first > 0 && first >= second ? 1 : 0);
    private static BitRelation Binary(int left, int right, Func<int,int,int> result) =>
        BitRelation.FromTuples(3, from a in Enumerable.Range(0, left)
            from b in Enumerable.Range(0, right) select new byte[] { (byte)a, (byte)b, (byte)result(a,b) });

    public TensionChoiceMachine(int[] initialNeeds, int[][] effects, int fastAction = -1,
        int[,]? priors = null, int[]? offerMasks = null)
    {
        if (initialNeeds.Length != Count || effects.Length != Count || effects.Any(row => row.Length != Count))
            throw new ArgumentException("Supply three need banks and three action rows of three effects.");
        if (effects.SelectMany(row => row).Any(value => value < -1 || value > 2))
            throw new ArgumentOutOfRangeException(nameof(effects));
        if (fastAction < -1 || fastAction >= Count || (fastAction >= 0 && effects[fastAction][0] != 1))
            throw new ArgumentOutOfRangeException(nameof(fastAction));
        if (priors != null && (priors.GetLength(0) != Count || priors.GetLength(1) != Count))
            throw new ArgumentException("One forecast per action and need.", nameof(priors));
        if (offerMasks != null && (offerMasks.Length != Count || offerMasks.Any(mask => mask is < 0 or > 7)))
            throw new ArgumentException("One three-action offer mask per need.", nameof(offerMasks));
        needs = initialNeeds.Select(i => new ChoiceNeed(i)).ToArray();

        int Site(PortStructure owner, bool present = false) => owner.Publish(owner.Create(0,0,1,present ? 1UL : 0UL));
        int idle = Site(opportunity, true);
        var outlets = Enumerable.Range(0, Count).Select(_ => Site(opportunity)).ToArray();
        for (int i = 0; i < Count; i++) gates[i] = Site(opportunity);
        int always = Site(opportunity, true);
        grant = new(opportunity, PassageLaw.Controlled());
        recycle = new(opportunity, PassageLaw.Unconditional());
        rememberUse = useMemory.AddTable(ConnectionLaws.Presence());
        for (int action = 0; action < Count; action++)
        {
            grants[action] = grant.Connect(3, idle, gates[action], outlets[action]);
            returns[action] = recycle.Connect(3, outlets[action], always, idle);
            access[action] = Site(signals);
            primed[action] = Site(signals);
            int seen = useMemory.Import(signals.Connect(primed[action]));
            useRows[action] = useMemory.Wire(rememberUse, 4, seen,
                useMemory.Import(opportunity.Connect(outlets[action])), seen);
            for (int basis = 0; basis < Count; basis++)
            {
                int gain = effects[action][basis];
                for (int i = 0; i < Math.Abs(gain); i++)
                    this.effects.Append(2, action, basis, gain < 0 ? 1 : 0, 0);
                if (action == fastAction && basis == 0) this.effects.Append(2, action, basis, 0, 1);
                bool advertised = offerMasks is null ? gain != 0 : (offerMasks[basis] & (1 << action)) != 0;
                offers[action,basis] = Site(signals, advertised);
                int initial = priors?[action,basis] ?? Unseen;
                samples[action,basis] = memory.Publish(memory.Create(2, 0, 3, Code(initial)));
                feedback.Append(4, outlets[action], basis, samples[action,basis]);
            }
        }
        for (int basis = 0; basis < Count; basis++) WirePreference(basis);
        var phases = Enumerable.Range(0, Count).Select(i => rotation.State(i == 0)).ToArray();
        var clock = rotation.Guard(true);
        var cycle = rotation.AddTable(PassageLaw.Unconditional());
        for (int i = 0; i < Count; i++) rotation.Link(cycle, phases[i], clock, phases[(i + 1) % Count]);
    }

    private void WirePreference(int basis)
    {
        var reader = new ReadConnections();
        int and = reader.AddTable(RaceExperiments.And), rank = reader.AddTable(Rank), compare = reader.AddTable(NoLower);
        selectors[basis] = reader;
        permissionTables[basis] = and;
        int Link(int law, int a, int b) => reader.Connect(law, 6, a, b).Endpoint;
        var demand = reader.Import(needs[basis]);
        var ranks = new int[Count];
        for (int i = 0; i < Count; i++)
        {
            var admitted = Link(and, reader.Import(signals.Connect(access[i])), reader.Import(signals.Connect(offers[i,basis])));
            var enabled = Link(and, demand, admitted);
            ranks[i] = Link(rank, reader.Import(memory.Connect(samples[i,basis])), enabled);
        }
        for (int i = 0; i < Count; i++)
        {
            var first = Link(compare, ranks[i], ranks[(i+1)%Count]);
            var second = Link(compare, ranks[i], ranks[(i+2)%Count]);
            permissions[i,basis] = reader.Wire(and, 6, first, second, reader.Import(opportunity.Connect(gates[i])));
        }
    }

    private static byte Code(int relief) => relief switch
    {
        Unseen => 0, -1 => 1, 0 => 2, 1 => 3, 2 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(relief))
    };

    private int? Forecast(int action, int basis)
    {
        int code = memory.ReadPort(samples[action,basis]);
        return code == 0 ? null : code - 2;
    }

    public IPrimitiveReadPort Need(int basis) => needs[basis];
    public int[] Needs => needs.Select(need => need.Remaining).ToArray();
    public byte OfferedActions(int basis)
    {
        byte mask = 0;
        for (int i = 0; i < Count; i++) mask |= (byte)(signals.ReadPort(offers[i,basis]) << i);
        return mask;
    }

    public byte[] ChoiceRows() => grant.PackedRows(3);
    public byte[] ReliefRows() => effects.PackedBytes(4, 2);
    public byte[] FeedbackRows() => feedback.PackedBytes(3, 4);
    public byte[] MemoryBytes() => memory.PackedBytes(2, 0, 3);

    // Focus words: 0 keeps the opportunity idle, 1..3 select local bases 0..2.
    // The argument is their support mask; absence and ambiguous focus hold.
    public TensionChoiceBeat StepWithFocus(byte focusSupport, byte accessMask = 7, byte feedbackMask = 7)
    {
        if (focusSupport > 15 || accessMask > 7 || feedbackMask > 7)
            throw new ArgumentOutOfRangeException(nameof(focusSupport));
        if (ReadSupport.TrySingle(focusSupport, out var word) && word > 0)
            return Step(word - 1, accessMask, feedbackMask);
        var before = Needs;
        var phase = rotation.Active!.Value;
        var parked = (byte)(opportunity.PackedBytes(0,0,1)[0] & 15);
        rotation.Pulse();
        return new TensionChoiceBeat(++turn, -1, accessMask, feedbackMask, -1, before, Needs,
            new int?[3], new int?[3], new int?[3], Samples(), parked, 0, phase, MemoryBytes())
            { FocusSupport = focusSupport };
    }

    private int?[][] Samples() => Enumerable.Range(0, Count).Select(a =>
        Enumerable.Range(0, Count).Select(b => Forecast(a,b)).ToArray()).ToArray();

    public TensionChoiceBeat Step(int focus, byte accessMask = 7, byte feedbackMask = 7)
    {
        if ((uint)focus >= Count || accessMask > 7 || feedbackMask > 7)
            throw new ArgumentOutOfRangeException(nameof(focus));
        var before = Needs;
        var forecasts = Enumerable.Range(0, Count).Select(action => Forecast(action,focus)).ToArray();
        for (int i = 0; i < Count; i++) signals.StoreObservation(access[i], false, false, (ulong)((accessMask >> i) & 1));
        byte selection = 0;
        for (int i = 0; i < Count; i++)
        {
            if (!selectors[focus].TryCapture(permissionTables[focus], permissions[i,focus], opportunity, gates[i]))
                throw new InvalidOperationException("The bounded selector requires known local offer and access reads.");
            if (opportunity.ReadPort(gates[i]) == 1) selection |= (byte)(1 << i);
        }
        int phase = rotation.Active!.Value;
        for (int i = 0; i < Count; i++)
        {
            var row = grants[(phase + i) % Count];
            grant.TryStep(row, grant.Observe(row), out _);
        }
        byte opportunityBits = (byte)(opportunity.PackedBytes(0,0,1)[0] & 15);
        int chosen = Array.FindIndex(grants, row => opportunity.ReadPort(grant.Wiring(row)[2]) == 1);
        int?[] prediction = new int?[Count], observed = new int?[Count];
        int[] blocked = new int[Count];
        if (chosen >= 0)
        {
            for (int i = 0; i < Count; i++) prediction[i] = Forecast(chosen,i);
            Deliver(blocked);
        }
        var after = Needs;
        // Only the spent opportunity's actual outlet can write its attached
        // observation destinations. Observing a bank alone cannot train an
        // unused action. Absent feedback leaves that particular sample intact.
        for (int i = 0; i < feedback.Count(3,4); i++)
        {
            var row = feedback.Read(new(3,4,i));
            int basis = row[1];
            if (opportunity.ReadPort(row[0]) == 0 || (feedbackMask & (1 << basis)) == 0) continue;
            int delta = before[basis] - after[basis];
            observed[basis] = delta;
            byte code = Code(delta);
            memory.StoreObservation(row[2], (code & 4) != 0, (code & 2) != 0, (ulong)(code & 1));
        }
        for (int i = 0; i < Count; i++)
            useMemory.TryCapture(rememberUse, useRows[i], signals, primed[i]);
        foreach (var row in returns) recycle.TryStep(row, recycle.Observe(row), out _);
        rotation.Pulse();
        return new TensionChoiceBeat(++turn, focus, accessMask, feedbackMask, chosen, before, after, prediction, forecasts,
            observed, Samples(), opportunityBits, selection, phase, MemoryBytes())
            { FocusSupport = (byte)(1 << (focus + 1)), OfferMask = OfferedActions(focus), BlockedEffect = blocked };
    }

    // Each selected outlet communicates activation to each attached service
    // row. The marker that moves belongs to the serviced need's own bank.
    private void Deliver(int[] blocked)
    {
        for (int i = 0; i < effects.Count(4,2); i++)
        {
            var row = effects.Read(new(4,2,i));
            var outlet = grant.Wiring(grants[row[0]])[2];
            if (opportunity.ReadPort(outlet) == 0) continue;
            if (row[3] != 0 && signals.ReadPort(primed[row[0]]) == 0) continue;
            bool relieve = row[2] == 0;
            if (!needs[row[1]].MoveOne(relieve)) blocked[row[1]] += relieve ? 1 : -1;
        }
    }
}
