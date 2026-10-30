namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Human-readable results from the four fixed-layout experiments.
/// Names and text here explain bits; they are not used to execute them.</summary>
public static class ConnectionExamples
{
    public static void Run()
    {
        Console.WriteLine("All hex bytes below are actual encodings; bit positions count from the low bit.");
        Console.WriteLine("1. Shared context, instance bits, and explicit release");
        var positive = ContextTable.Pack(1, 0, [1, 0]);
        var negative = ContextTable.Pack(1, 2, [3, 2]);
        Console.WriteLine($"positive table [001,000]: {Convert.ToHexString(positive)}, 4 bits [O,D,V0,V1]");
        Console.WriteLine($"negative table [011,010]: {Convert.ToHexString(negative)}, 4 bits [O,D,V0,V1]");
        Console.WriteLine($"write negative into fixed-positive entry: {ContextTable.TryWrite(positive, 1, 2, 0, 3)}; bytes remain {Convert.ToHexString(positive)}");
        var released = ContextTable.Release(positive, 1, 2, 2);
        Console.WriteLine($"release sign: {Convert.ToHexString(released)}, 5 bits [O,V0,D0,V1,D1]");
        Console.WriteLine($"now write negative: {ContextTable.TryWrite(released, 2, 2, 0, 3)}; bytes {Convert.ToHexString(released)}");
        Console.WriteLine($"before release both entries share D at bit {ContextTable.BitPosition(1, 2, 0, 1)}; after release D sites are {ContextTable.BitPosition(2, 2, 0, 1)} and {ContextTable.BitPosition(2, 2, 1, 1)}");
        Console.WriteLine("The release changes a constraint and its layout; it moves no material.");

        Console.WriteLine("\n2. Retained participation under one net read");
        Console.WriteLine("tile bits 0..7: [O+=0,D+=0,A+V,B+V,O-=0,D-=1,A-V,B-V]");
        long[] empty = [0, 0, 0, 0];
        long[] balanced = [5, 0, 5, 0];
        Show("empty", empty);
        Show("balanced", balanced);
        Console.WriteLine($"tile 64 predicts net words [{string.Join(",", RetainedFlowFixture.PredictNet(0x64, 0).Select(Bits))}]; actual 5/5 reads zero");
        var signedRead = ContextTable.Pack(2, 0, [FineFlowFixture.Observe(RetainedFlowFixture.Net(balanced, 0))]);
        Console.WriteLine($"net observation fits sign-capable O|DV table {Convert.ToHexString(signedRead)}; retained participation stays in tile 64");

        var positiveLink = LocalConnection.Link(2, 3);
        var negativeLink = LocalConnection.Link(6, 7);
        var pair = LocalConnection.Pair(positiveLink, negativeLink);
        Console.WriteLine($"A+ bit2 -> B+ bit3: link {positiveLink:X2}; A- bit6 -> B- bit7: link {negativeLink:X2}");
        Console.WriteLine($"group both links: {pair:X3}, packed bytes {(byte)pair:X2} {(byte)(pair >> 8):X2}, 12 meaningful bits");
        var movedPair = RetainedFlowFixture.TransferPair(balanced, pair);
        Show($"paired transfer, {movedPair} in EACH side", balanced);
        Console.WriteLine($"repeat same transfer: {RetainedFlowFixture.TransferPair(balanced, pair)}; no duplicated participation");
        long[] single = [5, 0, 5, 0];
        RetainedFlowFixture.Transfer(single, positiveLink);
        Show("positive side only", single);
        long[] unbalanced = [7, 0, 5, 0];
        RetainedFlowFixture.TransferPair(unbalanced, pair);
        Show("paired transfer from 7/5", unbalanced);
        long[] limited = [2, 0, 0, 0];
        var moved = RetainedFlowFixture.TransferUpTo(limited, positiveLink, 5);
        Show($"request 5 from fixed-positive 2: moved {moved}, unmet {5 - moved}", limited);

        Console.WriteLine("\n3. The control attachment is encoded in the connection");
        var fromB = LocalConnection.Link(3, 2);
        var absent = LocalConnection.When(fromB, 2, false);
        var present = LocalConnection.When(fromB, 2, true);
        Console.WriteLine($"material B+ bit3 -> A+ bit2; control bit2 matches 0: {absent:X3}, matches 1: {present:X3}");
        Console.WriteLine("Ten bits: material endpoints (6), control-site offset (3), matching face (1). No stored open flag.");
        long[] supplied = [0, 4, 0, 0];
        Show("before zero-controlled passage", supplied);
        Console.WriteLine($"matching-zero passage open: {LocalConnection.IsOpen(RetainedFlowFixture.Observe(supplied), absent)}");
        RetainedFlowFixture.TransferWhen(supplied, absent);
        Show("after", supplied);
        Console.WriteLine($"same passage open now: {LocalConnection.IsOpen(RetainedFlowFixture.Observe(supplied), absent)}; complement: {LocalConnection.IsOpen(RetainedFlowFixture.Observe(supplied), present)}");

        Console.WriteLine("\n4. Resolution and direction memory are separate");
        Console.WriteLine("Memory frame bits: input[V,D,O], memory[V,D,O], then two zero padding bits.");
        long[] samples = [3, 1, 0, -1, -3, 0, 1, 3];
        foreach (long threshold in new long[] { 0, 2 })
        {
            byte state = 0;
            Console.WriteLine($"threshold {threshold}: input / observation / memory / whole frame byte");
            foreach (var amount in samples)
            {
                var observed = DirectionMemory.ObserveAtThreshold(amount, threshold);
                var frame = ThreeBitMeasure.Pack([observed, state]);
                DirectionMemory.UpdateFrame(frame);
                state = ThreeBitMeasure.At(frame, 1);
                Console.WriteLine($"{amount,3} / {Bits(observed)} / {Bits(state)} / {frame[0]:X2}");
            }
        }
        Console.WriteLine("Finer integers and thresholds belong to the demonstration apparatus, not hidden fields in the primitive.");
    }

    private static void Show(string label, long[] cells) =>
        Console.WriteLine($"{label}: fine [A+,B+,A-,B-]=[{string.Join(",", cells)}], tile {RetainedFlowFixture.Observe(cells):X2}, net A={RetainedFlowFixture.Net(cells, 0)}, B={RetainedFlowFixture.Net(cells, 1)}");

    private static string Bits(byte word) => Convert.ToString(word, 2).PadLeft(3, '0');
}
