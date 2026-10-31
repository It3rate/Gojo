namespace Gojo.Experiments.PrimitiveMeasure;

public static class RelationExamples
{
    public static void Run()
    {
        Presence();
        Addition();
        Opposition();
    }

    private static void Presence()
    {
        var values = new PortStructure();
        var first = values.Create(0, 0, 1, 0);
        var second = values.Create(0, 0, 1, 0);
        var reads = new ReadConnections();
        var a = reads.Import(values.Connect(values.Publish(first)));
        var b = reads.Import(values.Connect(values.Publish(second)));
        var table = reads.AddTable(ConnectionLaws.Presence());
        var coarse = reads.Connect(table, 2, a, b);
        Console.WriteLine("Qualitative ladder: two distinguishable parts -> one coarse presence read.");
        Console.WriteLine($"Row [{a},{b},{coarse.Endpoint}], 2 bits per reference: {Convert.ToHexString(reads.PackedRows(table, 2))}.");
        for (var pattern = 0; pattern < 4; pattern++)
        {
            values.StoreObservation(first, false, false, (ulong)(pattern & 1));
            values.StoreObservation(second, false, false, (ulong)(pattern >> 1));
            Console.WriteLine($"fine V1V0 {Convert.ToString(pattern, 2).PadLeft(2, '0')} -> coarse ODV {Show(coarse.ReadSupport())}");
        }
        values.StoreObservation(first, false, false, 0);
        values.StoreObservation(second, false, false, 0);
        Console.WriteLine($"Both parts return to zero: row [{string.Join(',', reads.Wiring(table, reads.LocationOf(coarse.Endpoint).Row))}] remains.");
        Console.WriteLine("A coarse 1 admits fine 01, 10, or 11; it does not select a finer pattern.");
        Console.WriteLine("The row retains endpoint roles. No distance, spatial direction, or radial dynamics is supplied here.");
    }

    private static void Addition()
    {
        var values = new PortStructure();
        var first = values.Create(0, 2, 2, 1);
        var second = values.Create(0, 2, 2, 0);
        var held = values.Create(0, 2, 2, 2);
        var reads = new ReadConnections();
        var a = reads.Import(values.Connect(values.Publish(first)));
        var b = reads.Import(values.Connect(values.Publish(second)));
        var addTable = reads.AddTable(ConnectionLaws.Addition());
        var sum = reads.Connect(addTable, 2, a, b);
        var heldPort = values.Connect(values.Publish(held));
        var target = reads.Import(heldPort);
        var copyTable = reads.AddTable(ConnectionLaws.Identity());
        var capture = reads.Wire(copyTable, 3, sum.Endpoint, target);
        Console.WriteLine("\nAddition uses the same endpoint row shape, with a different visible law bitmap.");
        Console.WriteLine($"Calculated row [{a},{b},{sum.Endpoint}]: {Convert.ToHexString(reads.PackedRows(addTable, 2))}; " +
            $"capture row [{sum.Endpoint},{target}]: {Convert.ToHexString(reads.PackedRows(copyTable, 3))}.");
        Sample("+? plus +0");
        values.StoreObservation(second, false, true, 1);
        Sample("+? plus -?");
        values.StoreObservation(first, false, false, 0);
        Sample("+0 plus -?");
        values.StoreObservation(first, false, true, 0);
        values.StoreObservation(second, false, true, 0);
        Sample("-0 plus -0");
        Console.WriteLine("Calculated output owns no value slot. The held sample changes only on a successful explicit capture.");
        Console.WriteLine("Support mask 00 means no admitted answer; 01 means word 000; 0F admits four ODV alternatives.");

        void Sample(string label)
        {
            var possible = sum.ReadSupport();
            var before = heldPort.Read();
            var accepted = reads.TryCapture(copyTable, capture, values, held);
            Console.WriteLine($"{label}: predicted {Show(possible)}, held {Bits(before)}, capture {accepted} -> held {Bits(heldPort.Read())}");
        }
    }

    private static void Opposition()
    {
        var rows = new EndpointRows();
        var row = rows.Append(2, 0, 1, 2, 3);
        Console.WriteLine("\nOpposed settlement: [active+, active-, retained+, retained-], row [0,1,2,3] = E4.");
        Console.WriteLine("The following fine amounts belong to the demonstration world, not the packed connection or coarse predictor.");
        foreach (var (positive, negative, previous) in new (long, long, byte)[] { (5, 2, 1), (2, 5, 1), (5, 5, 3) })
        {
            long[] cells = [positive, negative, 0, 0];
            var matched = OpposedConnectionFixture.Settle(cells, rows, row);
            var result = OpposedConnectionFixture.ReadSettled(cells, rows, row, previous);
            Console.WriteLine($"[{positive},{negative},0,0] -> [{string.Join(',', cells)}], matched {matched}, settled ODV {Bits(result)}");
        }
        Console.WriteLine("Same coarse inputs settle positive, negative, or zero. Both retained sides remain present after cancellation.");
        Console.WriteLine($"The four endpoint roles remain encoded as {Convert.ToHexString(rows.PackedBytes(4, 2))}, even at zero net.");
        Console.WriteLine("A single net word cannot reconstruct those roles or the retained amounts. Zero direction comes from explicit memory.");
    }

    private static string Bits(byte word) => Convert.ToString(word, 2).PadLeft(3, '0');
    private static string Show(byte support) => "{" + string.Join(',', ReadSupport.Words(support).Select(Bits)) + "}";
}
