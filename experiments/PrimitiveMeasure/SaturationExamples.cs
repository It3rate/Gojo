namespace Gojo.Experiments.PrimitiveMeasure;

public static class SaturationExamples
{
    public static void Run()
    {
        Console.WriteLine("Overview: an occupied receiver can block a whole handoff; explicit rows route the pending contents elsewhere.");
        Console.WriteLine("An eight-byte transition law lists permitted before/after patterns for [source, control, target].");
        Console.WriteLine("These examples use existing passage laws. The words left/right describe intended drawings, not encoded geometry.");
        Console.WriteLine("V=1 means unmeasured contents; receiver occupancy blocks only because this particular law cannot combine contents.");

        Console.WriteLine("\nForward first, then escape if forward is occupied. Site order [pending, forward, side].");
        for (byte initial = 0; initial < 8; initial++)
        {
            var circuit = Overflow(initial);
            var predicted = circuit.Predict(circuit.Observe());
            if (!circuit.TryStep(circuit.Observe(), out var after)) throw new InvalidOperationException("Resolved fixture failed.");
            Console.WriteLine($"{initial:X2} -> {after:X2}; joint prediction {predicted:X2}");
        }
        var uncertain = Overflow(3);
        var before = uncertain.Observe(0b101);
        Console.WriteLine($"Forward unobserved: support {before:X2} -> {uncertain.Predict(before):X2}.");
        Console.WriteLine($"Execute uncertain support: {uncertain.TryStep(before, out _)}; stored remains {uncertain.Stored:X2}.");
        Console.WriteLine($"Forward row [0,2,1]: {Convert.ToHexString(uncertain.Forward.PackedRows(2))}; law {Convert.ToHexString(uncertain.Forward.LawBytes())}");
        Console.WriteLine($"Escape row [0,1,2]: {Convert.ToHexString(uncertain.Present.PackedRows(2))}; law {Convert.ToHexString(uncertain.Present.LawBytes())}");

        Show("Overshoot beyond a landmark: [pending, landmark, beyond]", Single(3, true));
        Show("Closed passage retains pending contents", Single(1, false));
        Show("Two outlets, left visited first: [pending, control, left, right]", Fork(1, false));
        Show("Same outlets, right visited first", Fork(1, true));
        Show("First outlet occupied: second can accept", Fork(5, false));
        Show("Both occupied: source stays pending", Fork(13, false));

        Console.WriteLine("\nPre-numeric geometry uses an additional declared perpendicular frame, not the O/D bits.");
        var owner = new PortStructure();
        var a = owner.Publish(owner.Create(0, 0, 1, 1));
        var b = owner.Publish(owner.Create(0, 0, 1, 1));
        var path = new OrthogonalPathFixture(owner.Connect(a), owner.Connect(b));
        path.Connect(0, 0, 1, 4);
        path.Connect(1, 1, 2, 5);
        path.Connect(2, 2, 3, 4);
        path.Connect(3, 3, 0, 5);
        for (var lane = 0; lane < 4; lane++)
            Console.WriteLine($"Lane {lane}: [{string.Join(',', path.Wiring(lane))}], bytes {Convert.ToHexString(path.PackedLane(lane))}");
        Console.WriteLine($"Both reaches nonzero: {path.Read()}");
        owner.StoreObservation(b, false, false, 0);
        Console.WriteLine($"Supply a zero reach observation: {path.Read()}; all four corner roles and edge rows still exist.");
        Console.WriteLine("Parallel-boundary interpretation: the same shared reach fixes the top and bottom; the other fixes the two offsets.");
        Console.WriteLine("This establishes a constrained rectangle family, not a filled sheet, radiation dynamics, retained material, or automatic growth.");
        Console.WriteLine("See SaturationExperiments.md for the situation matrix, exact charts, optional retention, and the missing laws.");
    }

    public static PassageFixture Overflow(byte initial)
    {
        var circuit = new PassageFixture(3, initial);
        circuit.Add(circuit.Forward, 0, 2, 1);
        circuit.Add(circuit.Present, 0, 1, 2);
        return circuit;
    }

    public static PassageFixture Fork(byte initial, bool rightFirst)
    {
        var circuit = new PassageFixture(4, initial);
        circuit.Add(circuit.Forward, 0, 1, rightFirst ? 3 : 2);
        circuit.Add(circuit.Forward, 0, 1, rightFirst ? 2 : 3);
        return circuit;
    }

    private static PassageFixture Single(byte initial, bool open)
    {
        var circuit = new PassageFixture(3, initial);
        circuit.Add(open ? circuit.Forward : circuit.Present, 0, 1, 2);
        return circuit;
    }

    private static void Show(string name, PassageFixture circuit)
    {
        var before = circuit.Stored;
        if (!circuit.TryStep(circuit.Observe(), out var after)) throw new InvalidOperationException("Resolved fixture failed.");
        Console.WriteLine($"{name}: {before:X2} -> {after:X2}");
    }
}
