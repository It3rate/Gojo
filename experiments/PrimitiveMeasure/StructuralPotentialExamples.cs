namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Observation-only apparatus: meaning stays in ordinary value slots,
/// ordered endpoint rows and law bits. Labels below only present those reads.</summary>
public static class StructuralPotentialExamples
{
    public static void Run()
    {
        var singleValues = new PortStructure();
        var singleBinding = singleValues.Create(0, 0, 1, 0);
        var singleReads = new ReadConnections();
        var singleA = Attach(singleValues, singleReads, singleBinding);
        var identity = singleReads.AddTable(ConnectionLaws.Identity());
        var single = singleReads.Connect(identity, 2, singleA);

        var pairValues = new PortStructure();
        var pairFirst = pairValues.Create(0, 0, 1, 0);
        var pairSecond = pairValues.Create(0, 0, 1, 0);
        var pairReads = new ReadConnections();
        var pairA = Attach(pairValues, pairReads, pairFirst);
        var pairB = Attach(pairValues, pairReads, pairSecond);
        var pairPresenceTable = pairReads.AddTable(ConnectionLaws.Presence());
        var pairExclusiveTable = pairReads.AddTable(ConnectionLaws.ExclusivePresence());
        var pairPresence = pairReads.Connect(pairPresenceTable, 2, pairA, pairB);
        var pairExclusive = pairReads.Connect(pairExclusiveTable, 2, pairA, pairB);

        var repeatedValues = new PortStructure();
        var repeatedBinding = repeatedValues.Create(0, 0, 1, 0);
        var repeatedReads = new ReadConnections();
        var repeatedA = Attach(repeatedValues, repeatedReads, repeatedBinding);
        var repeatedPresenceTable = repeatedReads.AddTable(ConnectionLaws.Presence());
        var repeatedExclusiveTable = repeatedReads.AddTable(ConnectionLaws.ExclusivePresence());
        var repeatedPresence = repeatedReads.Connect(repeatedPresenceTable, 2, repeatedA, repeatedA);
        var repeatedExclusive = repeatedReads.Connect(repeatedExclusiveTable, 2, repeatedA, repeatedA);

        Console.WriteLine("One empty site, two distinct sites, and one site used twice.");
        Console.WriteLine("Every value starts at O=0, D=0, V=0. Each arrangement has its own local endpoints.");
        Console.WriteLine("Observations below are supplied explicitly; these read connections move no material.");
        Row("single identity", singleReads, single);
        Row("pair presence", pairReads, pairPresence);
        Row("pair exclusive presence", pairReads, pairExclusive);
        Row("repeated presence", repeatedReads, repeatedPresence);
        Row("repeated exclusive presence", repeatedReads, repeatedExclusive);
        Console.WriteLine("\nStage | single V | pair roles / presence / exclusive | repeated roles / presence / exclusive");
        Sample("empty");
        singleValues.StoreObservation(singleBinding, false, false, 1);
        pairValues.StoreObservation(pairFirst, false, false, 1);
        repeatedValues.StoreObservation(repeatedBinding, false, false, 1);
        Sample("A present");
        pairValues.StoreObservation(pairSecond, false, false, 1);
        Sample("B also present in pair");
        singleValues.StoreObservation(singleBinding, false, false, 0);
        pairValues.StoreObservation(pairFirst, false, false, 0);
        pairValues.StoreObservation(pairSecond, false, false, 0);
        repeatedValues.StoreObservation(repeatedBinding, false, false, 0);
        Sample("empty again");
        Console.WriteLine("The original rows survive all four stages. Equal reads never merge the two pair sites.");

        pairValues.StoreObservation(pairFirst, false, false, 1);
        var before = pairValues.Locate(pairFirst);
        if (!pairValues.TryMove(pairFirst, 0, 0, 3)) throw new InvalidOperationException("Demonstration move failed.");
        var after = pairValues.Locate(pairFirst);
        Console.WriteLine($"\nMove pair A from {Address(before)} to {Address(after)}.");
        Console.WriteLine($"Old slot live: {pairValues.TryReadLocation(before, out _)}; presence {Show(pairPresence.ReadSupport())}; exclusive {Show(pairExclusive.ReadSupport())}.");
        Row("pair presence after move", pairReads, pairPresence);
        Console.WriteLine("Moving adds storage capacity, not new measurement. The binding keeps existing connections attached.");

        UnknownReads();
        Console.WriteLine("\nThe presence read is a small structural ladder: 00 -> 0; 01, 10, 11 -> 1.");
        Console.WriteLine("A coarse 1 can be refined by learning which connected parts are present, without contradicting that 1.");
        Console.WriteLine("Two roles alone establish no distance, geometric direction, or hidden magnitude.");

        void Sample(string stage)
        {
            Console.WriteLine($"{stage} | {Show(single.ReadSupport())} | " +
                $"[{Show(pairReads.Read(pairA))},{Show(pairReads.Read(pairB))}] / {Show(pairPresence.ReadSupport())} / {Show(pairExclusive.ReadSupport())} | " +
                $"[{Show(repeatedReads.Read(repeatedA))},{Show(repeatedReads.Read(repeatedA))}] / {Show(repeatedPresence.ReadSupport())} / {Show(repeatedExclusive.ReadSupport())}");
        }
    }

    private static void UnknownReads()
    {
        var reads = new ReadConnections();
        // Each exposed read admits ODV 000 or 001. No hidden quantity is supplied.
        var a = reads.Import(new UnresolvedPresence());
        var b = reads.Import(new UnresolvedPresence());
        var table = reads.AddTable(ConnectionLaws.ExclusivePresence());
        var distinct = reads.Connect(table, 2, a, b);
        var repeated = reads.Connect(table, 2, a, a);
        Console.WriteLine("\nBoth inputs unresolved, each admitting V=0 or V=1:");
        Console.WriteLine($"Distinct endpoints: exclusive presence {Show(distinct.ReadSupport())}; same endpoint twice: {Show(repeated.ReadSupport())}.");
        Console.WriteLine("Different endpoints leave agreement unresolved; repeating one endpoint requires one shared candidate word.");
        Console.WriteLine("This proves presence agreement, not cancellation of hidden amounts. Separate derived paths can still lose correlation.");
    }

    private static int Attach(PortStructure owner, ReadConnections reads, int binding) =>
        reads.Import(owner.Connect(owner.Publish(binding)));

    private static void Row(string label, ReadConnections reads, CalculatedPort output)
    {
        var location = reads.LocationOf(output.Endpoint);
        Console.WriteLine($"{label}: [{string.Join(',', reads.Wiring(location.Table, location.Row))}], " +
            $"2 bits/reference, packed table bytes {Convert.ToHexString(reads.PackedRows(location.Table, 2))}");
    }

    private static string Address(SlotAddress address) =>
        $"(O group {address.OrientationGroup}, D group {address.DirectionGroup}, width {address.Width}, index {address.Index})";

    // All displayed observations use fixed O=0/D=0, so showing V loses no alternatives here.
    private static string Show(byte support) => "{" + string.Join(',', ReadSupport.Words(support).Select(word => word & 1)) + "}";

    private sealed class UnresolvedPresence : IPrimitiveReadPort
    {
        public byte ReadSupport() => 0b00000011;
    }
}
