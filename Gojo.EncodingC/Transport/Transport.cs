namespace Gojo.EncodingC;

/// <summary>A machine as bits: its stores, the ports they expose, the laws
/// with their accounting, the wirings, and the order the engine runs them in.
/// Loading needs nothing but these bits, so whatever code built the machine
/// can be thrown away. The restored machine runs the same and leaves the same
/// things unresolved: bits that are not codes, open values, several possible
/// next states, retired positions and rows, and rows whose roles meet at one
/// bit.
/// <para>Everything is a whole number or a run of bits, in a fixed order, with
/// no names, no type tags beyond a bit or two where a choice is made, and no
/// fixed widths: numbers are Elias gamma codes. What a store is identified
/// by is its place in the order. A wiring's rows are a store like any
/// other, so its topology and anything it recorded are saved as planes.</para>
/// <para>A law with exactly one next state for every state is saved as that
/// list of next states, which is what a table-driven device would hold. Any
/// other law is saved as one bit per tuple.</para>
/// <para>Rungs are not part of a machine. They are working views: commit or
/// drop them first.</para></summary>
public static class Transport
{
    private const int MaxCount = 1 << 24;

    /// <summary>Saves the engine's wirings in order, every store they reach,
    /// and any further stores given. Stores given come first, in the
    /// order given, then the ones the wirings reach, in the order reached.</summary>
    public static byte[] Save(Engine engine, params Store[] stores) => Write(engine, stores, out _).ToArray();

    /// <summary>How many bits a saved machine spends on its stores (state,
    /// and the rows of its wirings), its laws and its wirings' bindings.</summary>
    public static MachineSize Measure(Engine engine, params Store[] stores)
    {
        Write(engine, stores, out var size);
        return size;
    }

    /// <summary>Restores a machine: an engine with its wirings in order, and the
    /// stores in the order they were saved.</summary>
    public static (Engine Engine, IReadOnlyList<Store> Stores) Load(byte[] machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        try
        {
            var bits = new BitReader(machine);
            var stores = new Store[bits.Count(MaxCount)];
            for (var i = 0; i < stores.Length; i++) stores[i] = ReadStore(bits);
            var laws = new Law[bits.Count(MaxCount)];
            for (var i = 0; i < laws.Length; i++) laws[i] = ReadLaw(bits, laws, i);
            var engine = new Engine();
            var wirings = new Wiring[bits.Count(MaxCount)];
            for (var i = 0; i < wirings.Length; i++) wirings[i] = ReadWiring(bits, laws, stores);
            if (bits.Bit())
            {
                var steps = bits.Count(MaxCount);
                for (var i = 0; i < steps; i++) engine.Add(wirings[bits.Count(wirings.Length - 1)]);
            }
            else
            {
                foreach (var wiring in wirings) engine.Add(wiring);
            }
            bits.End();
            return (engine, stores);
        }
        catch (Exception refused) when (refused is ArgumentException or OverflowException)
        {
            // Bits that parse but describe something no builder would accept: a
            // law that breaks its accounting, a port naming a plane twice.
            throw new FormatException("The bits do not describe a machine.", refused);
        }
    }

    private static BitWriter Write(Engine engine, Store[] stores, out MachineSize size)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(stores);
        var order = new List<Store>();
        var place = new Dictionary<Store, int>(ReferenceEqualityComparer.Instance);
        void Reach(Store store)
        {
            if (place.TryAdd(store, order.Count)) order.Add(store);
        }
        foreach (var store in stores) Reach(store);
        var laws = new List<Law>();
        var lawPlace = new Dictionary<Law, int>(ReferenceEqualityComparer.Instance);
        foreach (var wiring in engine.Wirings)
        {
            // A law read as a relation is saved as a reading of that law, which therefore comes first.
            if (wiring.Law is RelationLaw { From: { } from } && lawPlace.TryAdd(from, laws.Count)) laws.Add(from);
            if (lawPlace.TryAdd(wiring.Law, laws.Count)) laws.Add(wiring.Law);
            foreach (var binding in wiring.Bindings)
                Reach(binding.Surface switch
                {
                    Store store => store,
                    Port port => port.Owner,
                    Life life => life.Of,
                    _ => throw new InvalidOperationException("A rung, or the gap between a rung and its source, is a working view and not part of a machine. Commit or drop it before saving."),
                });
            Reach(wiring.Rows);
        }

        var bits = new BitWriter();
        bits.Number(order.Count);
        foreach (var store in order) WriteStore(bits, store);
        var afterStores = bits.Length;
        bits.Number(laws.Count);
        foreach (var law in laws) WriteLaw(bits, law, lawPlace);
        var afterLaws = bits.Length;
        // Each wiring once, in the order first run. A wiring the engine runs more
        // than once in a step, such as a second pass of narrowing, costs only
        // its place in the order.
        var once = new List<Wiring>();
        var wiringPlace = new Dictionary<Wiring, int>(ReferenceEqualityComparer.Instance);
        foreach (var wiring in engine.Wirings)
            if (wiringPlace.TryAdd(wiring, once.Count)) once.Add(wiring);
        bits.Number(once.Count);
        foreach (var wiring in once) WriteWiring(bits, wiring, lawPlace, place);
        bits.Bit(once.Count != engine.Wirings.Count);
        if (once.Count != engine.Wirings.Count)
        {
            bits.Number(engine.Wirings.Count);
            foreach (var wiring in engine.Wirings) bits.Number(wiringPlace[wiring]);
        }
        size = new MachineSize(afterStores, afterLaws - afterStores, bits.Length - afterLaws);
        return bits;
    }

    private static void WriteStore(BitWriter bits, Store store)
    {
        bits.Number(store.PlaneCount);
        var constants = Enumerable.Range(0, store.PlaneCount).Where(store.IsConstantPlane).ToArray();
        bits.Number(constants.Length);
        foreach (var plane in constants)
        {
            bits.Number(plane);
            bits.Bit(store.ConstantValue(plane));
        }
        // 0: every position is in use. 1: some are retired. 2: some are spare.
        bits.Number(store.Count);
        var spare = store.Spare;
        var states = spare.Any ? 2 : store.Live.Count < store.Count ? 1 : 0;
        bits.Number(states);
        if (states > 0) bits.Bits(store.Live, store.Count);
        if (states > 1) bits.Bits(spare, store.Count);
        for (var p = 0; p < store.PlaneCount; p++)
            if (!store.IsConstantPlane(p)) bits.Bits(store.Read(p), store.Count);
        bits.Number(store.Ports.Count);
        foreach (var port in store.Ports)
        {
            bits.Number(port.Count);
            for (var lane = 0; lane < port.Count; lane++) bits.Number(port.Lane(lane));
            bits.Number(port.PlaneCount);
            foreach (var plane in port.OwnerPlanes) bits.Number(plane);
        }
    }

    private static Store ReadStore(BitReader bits)
    {
        var store = new Store(0);
        var planes = bits.Count(MaxCount);
        var constants = new Dictionary<int, bool>();
        var constantCount = bits.Count(planes);
        for (var i = 0; i < constantCount; i++)
            if (!constants.TryAdd(bits.Count(planes - 1), bits.Bit())) throw new FormatException("A constant plane is named twice.");
        for (var p = 0; p < planes; p++)
        {
            if (constants.TryGetValue(p, out var value)) store.AddConstantPlane(value);
            else store.AddPlane();
        }
        var count = bits.Count(MaxCount);
        var states = bits.Count(2);
        var live = states > 0 ? bits.Bits(count) : Plane.Ones(count);
        var spare = states > 1 ? bits.Bits(count) : new Plane(count);
        if (live.Overlaps(spare)) throw new FormatException("A position is saved as both spare and in use.");
        for (var position = 0; position < count; position++)
        {
            if (spare[position]) store.AddSpare();
            else store.Add();
        }
        for (var p = 0; p < planes; p++)
        {
            if (store.IsConstantPlane(p)) continue;
            var stored = bits.Bits(count);
            foreach (var position in stored.Indexes()) store.Write(p, position, true);
        }
        var ports = bits.Count(MaxCount);
        for (var i = 0; i < ports; i++)
        {
            var lanes = new int[bits.Count(MaxCount)];
            for (var lane = 0; lane < lanes.Length; lane++) lanes[lane] = bits.Count(count - 1);
            var exposed = new int[bits.Count(planes)];
            for (var k = 0; k < exposed.Length; k++) exposed[k] = bits.Count(planes - 1);
            store.Publish(lanes, exposed);
        }
        foreach (var position in live.Not().Indexes())
            if (!spare[position]) store.Retire(position);
        return store;
    }

    private static void WriteLaw(BitWriter bits, Law law, Dictionary<Law, int> laws)
    {
        bits.Bit(law is TransitionLaw);
        if (law is RelationLaw relation)
        {
            bits.Bit(relation.From is not null);
            if (relation.From is not null)
            {
                bits.Number(laws[relation.From]);
                return;
            }
        }
        bits.Number(law.Roles.Count);
        foreach (var role in law.Roles)
        {
            bits.Bit(role.IsUnary);
            bits.Number(role.Size);
        }
        if (law is not TransitionLaw transition)
        {
            WriteTable(bits, law);
            return;
        }

        // One next state per state: the list of next states, each at the width a state needs.
        bits.Bit(transition.IsFunctional);
        if (transition.IsFunctional)
        {
            var width = PackedInts.WidthFor(transition.StateCount - 1);
            for (var before = 0; before < transition.StateCount; before++)
                for (var k = 0; k < width; k++) bits.Bit(((transition.Afters(before)[0] >> k) & 1) != 0);
        }
        else WriteTable(bits, law);

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

    private static void WriteTable(BitWriter bits, Law law)
    {
        var table = law.Bytes();
        for (var bit = 0; bit < law.BitCount; bit++) bits.Bit(((table[bit / 8] >> (bit % 8)) & 1) != 0);
    }

    private static Law ReadLaw(BitReader bits, Law[] earlier, int place)
    {
        var transition = bits.Bit();
        if (!transition && bits.Bit())
            return earlier[bits.Count(place - 1)] is TransitionLaw from
                ? from.AsRelation()
                : throw new FormatException("A relation is saved as a reading of a law that is not a transition.");
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
        if (transition && bits.Bit())
        {
            var width = PackedInts.WidthFor((int)tuples - 1);
            for (long before = 0; before < tuples; before++)
            {
                long after = 0;
                for (var k = 0; k < width; k++)
                    if (bits.Bit()) after |= 1L << k;
                if (after >= tuples) throw new FormatException("A saved law names a state it does not have.");
                var bit = before + tuples * after;
                table[bit / 8] |= (byte)(1 << (int)(bit % 8));
            }
        }
        else
        {
            for (long bit = 0; bit < bitCount; bit++)
                if (bits.Bit()) table[bit / 8] |= (byte)(1 << (int)(bit % 8));
        }
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

    private static void WriteWiring(BitWriter bits, Wiring wiring, Dictionary<Law, int> laws, Dictionary<Store, int> stores)
    {
        bits.Number(laws[wiring.Law]);
        foreach (var binding in wiring.Bindings)
        {
            if (wiring.Law is RelationLaw) bits.Bit(binding.IsOpen);
            // 0: a store's own positions. 1: one of its ports. 2: whether its positions are in use.
            switch (binding.Surface)
            {
                case Port port:
                    bits.Number(1);
                    bits.Number(stores[port.Owner]);
                    bits.Number(IndexOf(port.Owner.Ports, port));
                    break;
                case Life life:
                    bits.Number(2);
                    bits.Number(stores[life.Of]);
                    break;
                default:
                    bits.Number(0);
                    bits.Number(stores[(Store)binding.Surface]);
                    break;
            }
            WritePlanes(bits, binding.Planes);
        }

        // The rows are a store like any other; here only where their
        // references sit in it. Usually that is role after role from plane 0,
        // and then the widths say it all.
        bits.Number(stores[wiring.Rows]);
        var next = 0;
        var inOrder = true;
        for (var r = 0; r < wiring.Bindings.Count; r++)
            foreach (var plane in wiring.ReferencePlanes(r)) inOrder &= plane == next++;
        bits.Bit(inOrder);
        for (var r = 0; r < wiring.Bindings.Count; r++)
        {
            var planes = wiring.ReferencePlanes(r);
            bits.Number(planes.Count);
            if (!inOrder) foreach (var plane in planes) bits.Number(plane);
        }
        bits.Bit(wiring.Recorded is not null);
        if (wiring.Recorded is { } evidence) bits.Number(evidence.Changed);
    }

    private static Wiring ReadWiring(BitReader bits, Law[] laws, Store[] stores)
    {
        var law = laws[bits.Count(laws.Length - 1)];
        var bindings = new Binding[law.Roles.Count];
        for (var r = 0; r < bindings.Length; r++)
        {
            var open = law is RelationLaw && bits.Bit();
            var kind = bits.Count(2);
            var owner = stores[bits.Count(stores.Length - 1)];
            Surface surface = kind switch
            {
                1 => owner.Ports[bits.Count(owner.Ports.Count - 1)],
                2 => owner.Life,
                _ => owner,
            };
            var planes = ReadPlanes(bits, open ? law.Roles[r].Size : law.Roles[r].Bits, surface.PlaneCount);
            bindings[r] = open ? Binding.Open(surface, planes) : new Binding(surface, planes);
        }
        var rows = stores[bits.Count(stores.Length - 1)];
        var inOrder = bits.Bit();
        var next = 0;
        var references = new int[bindings.Length][];
        for (var r = 0; r < bindings.Length; r++)
        {
            references[r] = new int[bits.Count(31)];
            for (var k = 0; k < references[r].Length; k++)
                references[r][k] = inOrder ? next++ : bits.Count(rows.PlaneCount - 1);
        }
        int? evidence = bits.Bit() ? bits.Count(rows.PlaneCount) : null;
        return new Wiring(law, bindings, rows, references, evidence);
    }

    /// <summary>A role's planes. Several planes one after another are written
    /// as the first alone.</summary>
    private static void WritePlanes(BitWriter bits, IReadOnlyList<int> planes)
    {
        var run = planes.Count > 1 && Enumerable.Range(1, planes.Count - 1).All(k => planes[k] == planes[k - 1] + 1);
        if (planes.Count > 1) bits.Bit(run);
        for (var k = 0; k < (run ? 1 : planes.Count); k++) bits.Number(planes[k]);
    }

    private static int[] ReadPlanes(BitReader bits, int count, int available)
    {
        var planes = new int[count];
        var run = count > 1 && bits.Bit();
        for (var k = 0; k < count; k++) planes[k] = run && k > 0 ? planes[k - 1] + 1 : bits.Count(available - 1);
        return planes;
    }

    private static int IndexOf(IReadOnlyList<Port> ports, Port port)
    {
        for (var i = 0; i < ports.Count; i++)
            if (ReferenceEquals(ports[i], port)) return i;
        throw new InvalidOperationException("A port is not among its owner's ports.");
    }
}

/// <summary>What a saved machine spends, in bits: on its stores (their
/// state, and the rows of its wirings), on its laws, and on binding laws to
/// stores.</summary>
public sealed record MachineSize(long StoreBits, long LawBits, long WiringBits)
{
    public long Bits => StoreBits + LawBits + WiringBits;

    public long Bytes => (Bits + 7) / 8;
}
