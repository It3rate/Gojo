namespace Gojo.Experiments.PrimitiveMeasure;

public static class PortExamples
{
    public static void Run()
    {
        var structure = new PortStructure();
        var value = structure.Create(0, 1, 1, 1);
        var connection = structure.Connect(structure.Publish(value));
        var secondPort = structure.Connect(structure.Publish(value));
        Console.WriteLine("One value; two ports; constants come from array positions, not stored headers.");
        Console.WriteLine("Each connection keeps its owner and port position throughout these moves.");
        Console.WriteLine("Path = [orientation group, direction group, total width, entry index].");
        Console.WriteLine("Raw bits printed high to low; packed entries start at index * width, low bit first.");
        Print("Fixed O/D");
        Move("Own D", 0, 2, 2);
        Move("Wider value field", 0, 2, 3);
        Move("Own O and D", 2, 0, 4);
        Console.WriteLine("\nRetired locations keep dirty bytes; none is a live second value:");
        foreach (var (o, d, width) in new[] { (0, 1, 1), (0, 2, 2), (0, 2, 3), (2, 0, 4) })
            Console.WriteLine($"[{o},{d},{width}] bytes {Convert.ToHexString(structure.PackedBytes(o, d, width))}, " +
                $"slots {structure.Count(o, d, width)}, live {structure.LiveCount(o, d, width)}");
        Console.WriteLine("No compaction, slot reuse, reference counting, or garbage collector runs here.");
        Console.WriteLine("Wider storage adds capacity; both ports still report only orientation, direction, zero/nonzero.");
        Console.WriteLine("This moves an encoding; it does not measure finer magnitude or perform material transfer.");

        void Move(string label, int o, int d, int width)
        {
            if (!structure.TryMove(value, o, d, width)) throw new InvalidOperationException("Example move was rejected.");
            Print(label);
        }

        void Print(string label)
        {
            var a = structure.Locate(value);
            if (!structure.TryReadLocation(a, out var raw)) throw new InvalidOperationException("Example value is not live.");
            Console.WriteLine($"{label,-19} [{a.OrientationGroup},{a.DirectionGroup},{a.Width},{a.Index}] " +
                $"raw {Convert.ToString((long)raw, 2).PadLeft(a.Width, '0')}, " +
                $"port {connection.PortIndex}: {Bits(connection.Read())}, " +
                $"port {secondPort.PortIndex}: {Bits(secondPort.Read())}");
        }
    }

    private static string Bits(byte word) => Convert.ToString(word, 2).PadLeft(3, '0');
}
