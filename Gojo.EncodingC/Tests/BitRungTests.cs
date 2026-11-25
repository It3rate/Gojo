using System.Numerics;
using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Operations at the bit rung (ops_claude/GojoOperations-2026-11.md,
/// §6 and §5.3; experiments X3 and X13). One balance, a + b = 2·carry + sum,
/// is the half adder: its carry is AND and its sum is XOR, and the clamped sum,
/// with the overflow left as residue, is OR. Bound to a bit or to its flipped
/// view, those two laws and a wire make all 16 two-input functions, so
/// polarity is a binding and the 16 are four acts. The full adder chains
/// through carries: in declared order the sum settles in one step, taken
/// together it takes a step per carry, so propagation delay is step order.
/// And a product wider than a table can hold is built from small ones: the
/// lattice of bit products and adders gives every 2-bit product, while the
/// wide product's own transition is refused at 4 bits.</summary>
public sealed class BitRungTests
{
    private static readonly Shape Bit = Shape.Bit;

    /// <summary>a + b = 2·carry + sum.</summary>
    private static readonly RelationLaw HalfAdder = Operations.Balance([Bit, Bit, Bit, Bit], 1, 1, -2, -1);

    /// <summary>a + b + carry in = 2·carry out + sum.</summary>
    private static readonly TransitionLaw FullAdder = Operations.Bind(
        Operations.Balance([Bit, Bit, Bit, Bit, Bit], 1, 1, 1, -2, -1), [Bit, Bit, Bit, Bit, Bit], [0, 1, 2, -1, -1], [-1, -1, -1, 3, 4]);

    /// <summary>The half adder's carry alone (AND) and sum alone (XOR), the
    /// other left free.</summary>
    private static readonly TransitionLaw And = Operations.Bind(HalfAdder, [Bit, Bit, Bit], [0, 1, -1, -1], [-1, -1, 2, -1]);
    private static readonly TransitionLaw Xor = Operations.Bind(HalfAdder, [Bit, Bit, Bit], [0, 1, -1, -1], [-1, -1, -1, 2]);

    private static int[] Next(TransitionLaw law, params int[] state) =>
        law.Tuple(Assert.Single(law.Afters(law.TupleIndex(state))));

    /// <summary>A store with one position per lane and a bit plane per name.</summary>
    private static (Store Store, int[] Planes) Lanes(int lanes, int planes)
    {
        var store = new Store(0, lanes);
        return (store, [.. Enumerable.Range(0, planes).Select(_ => store.AddPlane())]);
    }

    private static void Write(Store store, int plane, int lane, int value) => Field.Bit(plane).Write(store, lane, value);

    private static int ReadBits(Store store, int lane, params int[] planes) =>
        planes.Select((p, i) => store.Read(p, lane) ? 1 << i : 0).Sum();

    private static Binding On(Store store, int plane) => new(store, plane);

    [Fact]
    public void OneBalanceIsTheHalfAdderAndTheBooleansAreItsReads()
    {
        var add = Operations.Bind(HalfAdder, [Bit, Bit, Bit, Bit], [0, 1, -1, -1], [-1, -1, 2, 3]);
        var or = Operations.Bind(Operations.Balance([Bit, Bit, Bit, Bit], 1, 1, -1, -1), [Bit, Bit, Bit], [0, 1, -1, -1], [-1, -1, 2, -1]);
        for (var a = 0; a <= 1; a++)
            for (var b = 0; b <= 1; b++)
            {
                Assert.Equal([a, b, a & b, a ^ b], Next(add, a, b, 0, 0));
                Assert.Equal(a & b, Next(And, a, b, 0)[2]);
                Assert.Equal(a ^ b, Next(Xor, a, b, 0)[2]);
                Assert.Equal(a | b, Next(or, a, b, 0)[2]);
            }
    }

    /// <summary>Four lanes hold the four input pairs. Each law runs once with
    /// each input and the output bound either to its bit or to its flipped
    /// view, and the output bits read across the lanes are a truth table. AND
    /// gives the eight tables with one or three ones, XOR the two others with
    /// two that are not a wire, a wire gives the four wires, and with the two
    /// constants that is all sixteen.</summary>
    [Fact]
    public void SixteenFunctionsAreFourActsUnderPolarity()
    {
        var wire = Operations.Bind(Operations.Balance([Bit, Bit], 1, -1), [Bit, Bit, Bit], [0, -1], [-1, 2]);
        var wireB = Operations.Bind(Operations.Balance([Bit, Bit], 1, -1), [Bit, Bit, Bit], [1, -1], [-1, 2]);
        HashSet<int> Tables(params TransitionLaw[] laws)
        {
            var tables = new HashSet<int>();
            foreach (var law in laws)
                for (var polarity = 0; polarity < 8; polarity++)
                {
                    var (store, p) = Lanes(4, 3);
                    for (var lane = 0; lane < 4; lane++)
                    {
                        Write(store, p[0], lane, lane & 1);
                        Write(store, p[1], lane, lane >> 1);
                    }
                    Binding Face(int role) => (polarity >> role & 1) == 1 ? new Binding(new Flipped(store, p[role]), p[role]) : On(store, p[role]);
                    new Engine().Add(Tension.Wire(law, [Face(0), Face(1), Face(2)], Plane.Ones(4))).Step();
                    tables.Add(ReadBits(store, 0, p[2]) | ReadBits(store, 1, p[2]) << 1 | ReadBits(store, 2, p[2]) << 2 | ReadBits(store, 3, p[2]) << 3);
                }
            return tables;
        }

        var overlap = Tables(And);
        var difference = Tables(Xor);
        var wires = Tables(wire, wireB);
        Assert.Equal(Enumerable.Range(0, 16).Where(t => BitOperations.PopCount((uint)t) % 2 == 1).ToHashSet(), overlap);
        Assert.Equal([0b0110, 0b1001], difference.Order());
        Assert.Equal([0b0011, 0b0101, 0b1010, 0b1100], wires.Order());
        Assert.Equal(16, overlap.Union(difference).Union(wires).Union([0b0000, 0b1111]).Count());
    }

    /// <summary>Three full adders over 64 lanes add every pair of 3-bit
    /// numbers. Declared from the low bit up, each sees the carry the one below
    /// just wrote, and one step settles everything; taken together, a carry
    /// moves one bit a step, and 7 + 1 needs all three. The wide adder's
    /// relation fits a table, but its transition is refused at 4 bits, while
    /// the full adder is 32 states reused at every bit.</summary>
    [Fact]
    public void ARippleAdderSettlesInTheStepsItsCarriesNeed()
    {
        foreach (var together in new[] { false, true })
        {
            var (store, p) = Lanes(64, 13);
            int[] a = p[0..3], b = p[3..6], c = p[6..10], s = p[10..13];
            for (var lane = 0; lane < 64; lane++)
                for (var bit = 0; bit < 3; bit++)
                {
                    Write(store, a[bit], lane, lane >> bit & 1);
                    Write(store, b[bit], lane, lane >> (3 + bit) & 1);
                }
            var engine = new Engine();
            for (var bit = 0; bit < 3; bit++)
                engine.Add(Tension.Wire(FullAdder, [On(store, a[bit]), On(store, b[bit]), On(store, c[bit]), On(store, c[bit + 1]), On(store, s[bit])], Plane.Ones(64)));

            Assert.Equal(together ? 4 : 2, engine.Settle(8, together));
            for (var lane = 0; lane < 64; lane++)
                Assert.Equal((lane & 7) + (lane >> 3), ReadBits(store, lane, [.. s, c[3]]));
        }

        Assert.Equal(32, FullAdder.StateCount);
        Shape four = Shape.Binary(16), five = Shape.Binary(32);
        var wide = Operations.Balance([four, four, five], 1, 1, -1);
        Assert.Equal(16 * 16 * 32, wide.BitCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => Operations.Bind(wide, wide.Roles, [0, 1, -1], [-1, -1, 2]));
    }

    /// <summary>The lattice: four bit products (make hold at rung 0, the
    /// product of two bits) and two half adders give every 2-bit product over
    /// sixteen lanes, the same as the wide product relation. Taken together it
    /// settles in three changing steps, products then each column's carry.
    /// The wide product's own transition is refused at 4 bits; the lattice
    /// reuses the same two small tables at any width.</summary>
    [Fact]
    public void ALatticeOfBitProductsIsTheWideProduct()
    {
        var one = Shape.Unary(1);
        var hold = Operations.Bind(Operations.Product(1, 1), [one, one, one], [0, 1, -1], [-1, -1, 2]);
        var halfAdd = Operations.Bind(HalfAdder, [Bit, Bit, Bit, Bit], [0, 1, -1, -1], [-1, -1, 2, 3]);

        var (store, p) = Lanes(16, 12);
        int a0 = p[0], a1 = p[1], b0 = p[2], b1 = p[3], q00 = p[4], q10 = p[5], q01 = p[6], q11 = p[7], c1 = p[8], r1 = p[9], r2 = p[10], r3 = p[11];
        for (var lane = 0; lane < 16; lane++)
        {
            Write(store, a0, lane, lane & 1);
            Write(store, a1, lane, lane >> 1 & 1);
            Write(store, b0, lane, lane >> 2 & 1);
            Write(store, b1, lane, lane >> 3 & 1);
        }
        var all = Plane.Ones(16);
        var engine = new Engine()
            .Add(Tension.Wire(hold, [On(store, a0), On(store, b0), On(store, q00)], all))
            .Add(Tension.Wire(hold, [On(store, a1), On(store, b0), On(store, q10)], all))
            .Add(Tension.Wire(hold, [On(store, a0), On(store, b1), On(store, q01)], all))
            .Add(Tension.Wire(hold, [On(store, a1), On(store, b1), On(store, q11)], all))
            .Add(Tension.Wire(halfAdd, [On(store, q10), On(store, q01), On(store, c1), On(store, r1)], all))
            .Add(Tension.Wire(halfAdd, [On(store, q11), On(store, c1), On(store, r3), On(store, r2)], all));

        Assert.Equal(4, engine.Settle(8, together: true));
        var times = Operations.Product(3, 3);
        for (var lane = 0; lane < 16; lane++)
        {
            int a = lane & 3, b = lane >> 2, product = ReadBits(store, lane, q00, r1, r2, r3);
            Assert.True(times.Admits(a, b, product));
        }

        Assert.Equal(8, hold.StateCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => Operations.Bind(Operations.Product(15, 15), Operations.Product(15, 15).Roles, [0, 1, -1], [-1, -1, 2]));
    }
}
