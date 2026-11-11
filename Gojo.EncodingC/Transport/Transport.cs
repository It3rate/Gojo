namespace Gojo.EncodingC;

/// <summary>A machine as bits: its structures, the ports they expose, the laws
/// with their accounting, the wirings with their rows, and the order the
/// engine runs them in. Loading needs nothing but these bits, so whatever
/// code built the machine can be thrown away. The restored machine runs the
/// same and leaves the same things unresolved: bits that are not codes,
/// several possible next states, retired positions and rows, and rows whose
/// roles meet at one bit.
/// <para>Everything is a whole number or a run of bits, in a fixed order, with
/// no names, no type tags beyond a bit or two where a choice is made, and no
/// fixed widths: numbers are Elias gamma codes and row references are packed
/// at the width their largest value needs. What a structure is identified by
/// is its place in the order.</para>
/// <para>Rungs are not part of a machine. They are working views: commit or
/// drop them first.</para></summary>
public static class Transport
{
    private const int MaxCount = 1 << 24;

    /// <summary>Saves the engine's wirings in order, every structure they reach,
    /// and any further structures given. Structures given come first, in the
    /// order given, then the ones the wirings reach, in the order reached.</summary>
    public static byte[] Save(Engine engine, params Structure[] structures)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(structures);
        var order = new List<Structure>();
        var place = new Dictionary<Structure, int>(ReferenceEqualityComparer.Instance);
        void Reach(Structure structure)
        {
            if (place.TryAdd(structure, order.Count)) order.Add(structure);
        }
        foreach (var structure in structures) Reach(structure);
        var laws = new List<Law>();
        var lawPlace = new Dictionary<Law, int>(ReferenceEqualityComparer.Instance);
        foreach (var wiring in engine.Wirings)
        {
            if (lawPlace.TryAdd(wiring.Law, laws.Count)) laws.Add(wiring.Law);
            foreach (var binding in wiring.Bindings)
                Reach(binding.Surface switch
                {
                    Structure structure => structure,
                    Port port => port.Owner,
                    _ => throw new InvalidOperationException("A rung, or the gap between a rung and its source, is a working view and not part of a machine. Commit or drop it before saving."),
                });
        }

        var bits = new BitWriter();
        bits.Number(order.Count);
        foreach (var structure in order) WriteStructure(bits, structure);
        bits.Number(laws.Count);
        foreach (var law in laws) WriteLaw(bits, law);
        bits.Number(engine.Wirings.Count);
        foreach (var wiring in engine.Wirings) WriteWiring(bits, wiring, lawPlace, place);
        return bits.ToArray();
    }

    /// <summary>Restores a machine: an engine with its wirings in order, and the
    /// structures in the order they were saved.</summary>
    public static (Engine Engine, IReadOnlyList<Structure> Structures) Load(byte[] machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        try
        {
            var bits = new BitReader(machine);
            var structures = new Structure[bits.Count(MaxCount)];
            for (var i = 0; i < structures.Length; i++) structures[i] = ReadStructure(bits);
            var laws = new Law[bits.Count(MaxCount)];
            for (var i = 0; i < laws.Length; i++) laws[i] = ReadLaw(bits);
            var engine = new Engine();
            var wirings = bits.Count(MaxCount);
            for (var i = 0; i < wirings; i++) engine.Add(ReadWiring(bits, laws, structures));
            bits.End();
            return (engine, structures);
        }
        catch (Exception refused) when (refused is ArgumentException or OverflowException)
        {
            // Bits that parse but describe something no builder would accept: a
            // law that breaks its accounting, a port naming a plane twice.
            throw new FormatException("The bits do not describe a machine.", refused);
        }
    }

    private static void WriteStructure(BitWriter bits, Structure structure)
    {
        bits.Number(structure.PlaneCount);
        for (var p = 0; p < structure.PlaneCount; p++)
        {
            var constant = structure.IsConstantPlane(p);
            bits.Bit(constant);
            if (constant) bits.Bit(structure.ConstantValue(p));
        }
        bits.Number(structure.Count);
        bits.Bits(structure.Live, structure.Count);
        for (var p = 0; p < structure.PlaneCount; p++)
            if (!structure.IsConstantPlane(p)) bits.Bits(structure.Read(p), structure.Count);
        bits.Number(structure.Ports.Count);
        foreach (var port in structure.Ports)
        {
            bits.Number(port.Count);
            for (var lane = 0; lane < port.Count; lane++) bits.Number(port.Lane(lane));
            bits.Number(port.PlaneCount);
            foreach (var plane in port.OwnerPlanes) bits.Number(plane);
        }
    }

    private static Structure ReadStructure(BitReader bits)
    {
        var structure = new Structure(0);
        var planes = bits.Count(MaxCount);
        for (var p = 0; p < planes; p++)
        {
            if (bits.Bit()) structure.AddConstantPlane(bits.Bit());
            else structure.AddPlane();
        }
        var count = bits.Count(MaxCount);
        if (count > 0) structure.Add(count);
        var live = bits.Bits(count);
        var all = Plane.Ones(count);
        for (var p = 0; p < planes; p++)
            if (!structure.IsConstantPlane(p)) structure.Write(p, bits.Bits(count), all);
        var ports = bits.Count(MaxCount);
        for (var i = 0; i < ports; i++)
        {
            var lanes = new int[bits.Count(MaxCount)];
            for (var lane = 0; lane < lanes.Length; lane++) lanes[lane] = bits.Count(count - 1);
            var exposed = new int[bits.Count(planes)];
            for (var k = 0; k < exposed.Length; k++) exposed[k] = bits.Count(planes - 1);
            structure.Publish(lanes, exposed);
        }
        foreach (var position in live.Not().Indexes()) structure.Retire(position);
        return structure;
    }

    private static void WriteLaw(BitWriter bits, Law law)
    {
        bits.Bit(law is TransitionLaw);
        bits.Number(law.Roles.Count);
        foreach (var role in law.Roles)
        {
            bits.Bit(role.IsUnary);
            bits.Number(role.Size);
        }
        var table = law.Bytes();
        for (var bit = 0; bit < law.BitCount; bit++) bits.Bit(((table[bit / 8] >> (bit % 8)) & 1) != 0);
        if (law is not TransitionLaw transition) return;
        bits.Bit(transition.Accounting.Functional);
        bits.Number(transition.Accounting.Invariants.Count);
        foreach (var invariant in transition.Accounting.Invariants)
            switch (invariant)
            {
                case PreservedSum sum:
                    bits.Bit(false);
                    bits.Bit(false);
                    bits.Number(sum.Roles.Count);
                    for (var i = 0; i < sum.Roles.Count; i++)
                    {
                        bits.Number(sum.Roles[i]);
                        bits.Signed(sum.Weights[i]);
                    }
                    break;
                case Unchanged unchanged:
                    bits.Bit(true);
                    bits.Bit(false);
                    bits.Number(unchanged.Role);
                    break;
                case NeverIncreases never:
                    bits.Bit(false);
                    bits.Bit(true);
                    bits.Number(never.Role);
                    break;
                case NeverDecreases never:
                    bits.Bit(true);
                    bits.Bit(true);
                    bits.Number(never.Role);
                    break;
                default:
                    throw new InvalidOperationException("Only the four invariants this project defines can be saved.");
            }
    }

    private static Law ReadLaw(BitReader bits)
    {
        var transition = bits.Bit();
        var roles = new Shape[bits.Count(64)];
        for (var r = 0; r < roles.Length; r++)
        {
            var unary = bits.Bit();
            var size = bits.Count(1 << 16);
            roles[r] = unary ? Shape.Unary(size - 1) : Shape.Binary(size);
        }
        long tuples = 1;
        foreach (var role in roles) tuples = checked(tuples * role.Size);
        var bitCount = transition ? checked(tuples * tuples) : tuples;
        if (bitCount > Law.MaxBits) throw new FormatException("A saved law table is too large.");
        var table = new byte[(bitCount + 7) / 8];
        for (var bit = 0; bit < bitCount; bit++)
            if (bits.Bit()) table[bit / 8] |= (byte)(1 << (int)(bit % 8));
        if (!transition) return new RelationLaw(roles, table);
        var functional = bits.Bit();
        var invariants = new Invariant[bits.Count(MaxCount)];
        for (var i = 0; i < invariants.Length; i++)
        {
            var (low, high) = (bits.Bit(), bits.Bit());
            invariants[i] = (low, high) switch
            {
                (false, false) => ReadSum(bits, roles.Length),
                (true, false) => new Unchanged(bits.Count(roles.Length - 1)),
                (false, true) => new NeverIncreases(bits.Count(roles.Length - 1)),
                _ => new NeverDecreases(bits.Count(roles.Length - 1)),
            };
        }
        return new TransitionLaw(roles, table, new Accounting(functional, invariants));
    }

    private static PreservedSum ReadSum(BitReader bits, int roleCount)
    {
        var count = bits.Count(roleCount);
        var roles = new int[count];
        var weights = new int[count];
        for (var i = 0; i < count; i++)
        {
            roles[i] = bits.Count(roleCount - 1);
            weights[i] = checked((int)bits.Signed());
        }
        return new PreservedSum(roles, weights);
    }

    private static void WriteWiring(BitWriter bits, Wiring wiring, Dictionary<Law, int> laws, Dictionary<Structure, int> structures)
    {
        bits.Number(laws[wiring.Law]);
        foreach (var binding in wiring.Bindings)
        {
            if (binding.Surface is Port port)
            {
                bits.Bit(true);
                bits.Number(structures[port.Owner]);
                bits.Number(IndexOf(port.Owner.Ports, port));
            }
            else
            {
                bits.Bit(false);
                bits.Number(structures[(Structure)binding.Surface]);
            }
            foreach (var plane in binding.Planes) bits.Number(plane);
        }
        bits.Number(wiring.Count);
        bits.Bits(wiring.Live, wiring.Count);
        for (var r = 0; r < wiring.Bindings.Count; r++)
        {
            var column = wiring.Column(r);
            bits.Number(column.Width);
            for (var row = 0; row < column.Count; row++)
                for (var k = 0; k < column.Width; k++) bits.Bit(((column[row] >> k) & 1) != 0);
        }
    }

    private static Wiring ReadWiring(BitReader bits, Law[] laws, Structure[] structures)
    {
        var law = laws[bits.Count(laws.Length - 1)];
        var bindings = new Binding[law.Roles.Count];
        for (var r = 0; r < bindings.Length; r++)
        {
            var throughPort = bits.Bit();
            var owner = structures[bits.Count(structures.Length - 1)];
            Surface surface = throughPort ? owner.Ports[bits.Count(owner.Ports.Count - 1)] : owner;
            var planes = new int[law.Roles[r].Bits];
            for (var b = 0; b < planes.Length; b++) planes[b] = bits.Count(surface.PlaneCount - 1);
            bindings[r] = new Binding(surface, planes);
        }
        var wiring = new Wiring(law, bindings);
        var rows = bits.Count(MaxCount);
        var live = bits.Bits(rows);
        var columns = new int[bindings.Length][];
        for (var r = 0; r < bindings.Length; r++)
        {
            var width = bits.Count(31);
            columns[r] = new int[rows];
            for (var row = 0; row < rows; row++)
                for (var k = 0; k < width; k++)
                    if (bits.Bit()) columns[r][row] |= 1 << k;
        }
        for (var row = 0; row < rows; row++) wiring.Restore(columns.Select(column => column[row]).ToArray(), live[row]);
        return wiring;
    }

    private static int IndexOf(IReadOnlyList<Port> ports, Port port)
    {
        for (var i = 0; i < ports.Count; i++)
            if (ReferenceEquals(ports[i], port)) return i;
        throw new InvalidOperationException("A port is not among its owner's ports.");
    }
}
