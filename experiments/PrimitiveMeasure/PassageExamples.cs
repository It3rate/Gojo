namespace Gojo.Experiments.PrimitiveMeasure;

public static class PassageExamples
{
    public static void Run()
    {
        var law = PassageLaw.Controlled();
        Console.WriteLine("Controlled whole handoff: three V roles [S,C,T], all with fixed O=0/D=0.");
        Console.WriteLine("V=1 means the whole unknown contents are present, not a quantity of one.");
        Console.WriteLine($"Shared law bytes: {Convert.ToHexString(law.PackedBytes())}; state=S+2*C+4*T; law bit=before+8*after.");
        for (byte before = 0; before < 8; before++)
            Console.WriteLine($"  {Tuple(before, 3)} (code {before}) -> {States(law.Predict((byte)(1 << before)), 3)}");

        var (singleOwner, singlePorts) = Stores(1, 0, 0);
        var single = new PassageConnections(singleOwner, law);
        var row = single.Connect(2, singlePorts[0], singlePorts[1], singlePorts[2]);
        Console.WriteLine($"\nSingle row [{string.Join(',', single.Wiring(row))}], 2-bit references: {Convert.ToHexString(single.PackedRows(2))}.");
        Step("control closed", singleOwner, single, row);
        singleOwner.StoreObservation(1, false, false, 1);
        Console.WriteLine("Supply control observation C=1 (separate from material transfer).");
        Step("control opened", singleOwner, single, row);
        Step("repeat after handoff", singleOwner, single, row);
        var (occupiedOwner, occupiedPorts) = Stores(1, 1, 1);
        var occupied = new PassageConnections(occupiedOwner, law);
        Step("occupied receiver: hold under this no-merge law", occupiedOwner, occupied,
            occupied.Connect(2, occupiedPorts[0], occupiedPorts[1], occupiedPorts[2]));

        Console.WriteLine("\nUncertain control: omit C from the observation; the stored bit remains concrete.");
        foreach (var actualControl in new byte[] { 0, 1 })
        {
            var (owner, ports) = Stores(1, actualControl, 0);
            var passage = new PassageConnections(owner, law);
            var link = passage.Connect(2, ports[0], ports[1], ports[2]);
            var observed = passage.Observe(link, visibleRoles: 0b101);
            var predicted = passage.Predict(link, observed);
            var before = Bytes(owner);
            var executed = passage.TryStep(link, observed, out _);
            Console.WriteLine($"observer before {observed:X2}={States(observed, 3)} -> after {predicted:X2}={States(predicted, 3)}; " +
                $"execute uncertain={executed}; stored {before}->{Bytes(owner)} (fixture C={actualControl}).");
            Step("reveal C and execute", owner, passage, link);
        }

        Chain(law);
    }

    private static void Chain(PassageLaw law)
    {
        var (owner, ports) = Stores(1, 1, 0, 1, 0);
        var passages = new PassageConnections(owner, law);
        var first = passages.Connect(3, ports[0], ports[1], ports[2]);
        var second = passages.Connect(3, ports[2], ports[3], ports[4]);
        Console.WriteLine("\nChain slots/ports [A,C1,B,C2,T]=[0,1,2,3,4]; rows [0,1,2] and [2,3,4].");
        Console.WriteLine($"Packed rows, 3 bits/reference with no inter-row padding: {Convert.ToHexString(passages.PackedRows(3))}.");
        Console.WriteLine($"Initial stored byte {Bytes(owner)}, tuple {Tuple(0b01011, 5)}. Each call executes one passage.");
        Step("P1: A -> B", owner, passages, first);
        Step("repeat P1", owner, passages, first);
        Step("P2: B -> T", owner, passages, second);
        Step("repeat P2", owner, passages, second);

        var (blockedOwner, blockedPorts) = Stores(1, 1, 0, 0, 0);
        var blocked = new PassageConnections(blockedOwner, law);
        var b1 = blocked.Connect(3, blockedPorts[0], blockedPorts[1], blockedPorts[2]);
        var b2 = blocked.Connect(3, blockedPorts[2], blockedPorts[3], blockedPorts[4]);
        Console.WriteLine("\nChain with C2 closed, then opened:");
        Step("P1", blockedOwner, blocked, b1);
        Step("P2 closed: contents remain at B", blockedOwner, blocked, b2);
        blockedOwner.StoreObservation(3, false, false, 1);
        Step("supply C2=1, then P2", blockedOwner, blocked, b2);

        var (reverseOwner, reversePorts) = Stores(1, 1, 0, 1, 0);
        var reverse = new PassageConnections(reverseOwner, law);
        var r1 = reverse.Connect(3, reversePorts[0], reversePorts[1], reversePorts[2]);
        var r2 = reverse.Connect(3, reversePorts[2], reversePorts[3], reversePorts[4]);
        Console.WriteLine("\nReversing the explicit call order changes the intermediate result:");
        Step("P2 before P1", reverseOwner, reverse, r2);
        Step("then P1: contents reach B", reverseOwner, reverse, r1);

        var (uncertainOwner, uncertainPorts) = Stores(1, 1, 0, 1, 0);
        var uncertain = new PassageConnections(uncertainOwner, law);
        var u1 = uncertain.Connect(3, uncertainPorts[0], uncertainPorts[1], uncertainPorts[2]);
        var u2 = uncertain.Connect(3, uncertainPorts[2], uncertainPorts[3], uncertainPorts[4]);
        Console.WriteLine("\nChain predictions retain whole five-site candidates; they never write the stores:");
        Predict("C1 uncertain, C2 open", 0b11101);
        Predict("both controls uncertain", 0b10101);
        var partial = uncertain.ObserveScope(uncertainPorts, 0b11101);
        var firstObservation = uncertain.ProjectScope(u1, uncertainPorts, partial);
        Console.WriteLine($"Attempt P1 with uncertain observation: {uncertain.TryStep(u1, firstObservation, out _)}; stored byte remains {Bytes(uncertainOwner)}.");
        Step("resolve C1, then P1", uncertainOwner, uncertain, u1);
        Step("P2", uncertainOwner, uncertain, u2);
        Console.WriteLine("Presence sites retain the whole-handoff promise at this resolution; no fine magnitude is stored or measured.");
        Console.WriteLine("Rows and value-site identities persist. No automatic scheduler, cross-owner write, merge, split, or unit capacity is inferred.");

        void Predict(string label, int visible)
        {
            var before = uncertain.ObserveScope(uncertainPorts, visible);
            var afterFirst = uncertain.PredictScope(u1, uncertainPorts, before);
            var afterSecond = uncertain.PredictScope(u2, uncertainPorts, afterFirst);
            Console.WriteLine($"{label}: support {before:X8} -> {afterFirst:X8} -> {afterSecond:X8}");
            Console.WriteLine($"  {States(before, 5)} -> {States(afterFirst, 5)} -> {States(afterSecond, 5)}");
        }
    }

    private static (PortStructure Owner, int[] Ports) Stores(params byte[] values)
    {
        var owner = new PortStructure();
        var ports = values.Select(value => owner.Publish(owner.Create(0, 0, 1, value))).ToArray();
        return (owner, ports);
    }

    private static void Step(string label, PortStructure owner, PassageConnections passages, ConnectionRow row)
    {
        var before = Bytes(owner);
        var observed = passages.Observe(row);
        var accepted = passages.TryStep(row, observed, out var after);
        var result = accepted ? Tuple(after, 3) : "unavailable";
        Console.WriteLine($"{label}: stored {before}->{Bytes(owner)}; resolved step={accepted}, local after {result}.");
    }

    private static string Bytes(PortStructure owner) => Convert.ToHexString(owner.PackedBytes(0, 0, 1));
    private static string Tuple(int state, int sites) => "[" + string.Join(',', Enumerable.Range(0, sites).Select(site => (state >> site) & 1)) + "]";
    private static string States(uint support, int sites) => "{" + string.Join(" or ", Enumerable.Range(0, 1 << sites)
        .Where(state => (support & (1u << state)) != 0).Select(state => Tuple(state, sites))) + "}";
}
