namespace Gojo.EncodingC;

/// <summary>Numbers as rungs: a minimal place-value layer over pre-numeric
/// counts (ops_claude/GojoOperationsProposal-2026-11.md, §6). It adds range
/// and erases nothing: a count stays a count, readable and movable as it was.
/// <para><b>A digit is a count.</b> A rung holds a unary count of capacity
/// radix − 1, so larger is still more planes, and a rung's supports are its
/// open digits. A number is a ladder of rungs, finest first, each worth radix
/// of the one below.</para>
/// <para><b>Rungs are joined by balances with the radix as their weight.</b>
/// In arithmetic, a + b + carry in = radix · carry out + sum: the bit adder
/// is radix 2, and the same polynomial at radix 10 is a decimal column. It is
/// a relation, so a ladder of them narrows in any direction: sum forward,
/// difference backward, open digits left open. In material, grouping moves
/// radix units of one rung into one of the next and refining moves them back
/// (<see cref="Flows.Convert"/>), conserving the place-value sum. The radix
/// is a number in each balance, not a kind: the same law family is
/// recognizable at every base.</para></summary>
public static class Numbers
{
    /// <summary>A digit at a radix: a unary count of capacity radix − 1.</summary>
    public static Shape Digit(int radix) =>
        radix < 2 ? throw new ArgumentOutOfRangeException(nameof(radix), "A radix of at least 2 groups anything.") : Shape.Unary(radix - 1);

    /// <summary>A carry between rungs: a unary count of capacity 1.</summary>
    public static Shape Carry { get; } = Shape.Unary(1);

    /// <summary>a + b + carry in = radix · carry out + sum. Roles: a, b, carry
    /// in, carry out, sum.</summary>
    public static Polynomial DigitSum(int radix)
    {
        var digit = Digit(radix);
        return Polynomial.Balance([digit, digit, Carry, Carry, digit], 1, 1, 1, -radix, -1);
    }

    /// <summary>Radix units of a finer rung become one of the next: a
    /// conserving move. Roles: fine, coarse.</summary>
    public static TransitionLaw Group(int radix, int fineCapacity, int coarseCapacity) => Flows.Convert(radix, 1, fineCapacity, coarseCapacity);

    /// <summary>One unit of a coarser rung becomes radix of the next finer
    /// one. Roles: coarse, fine.</summary>
    public static TransitionLaw Refine(int radix, int coarseCapacity, int fineCapacity) => Flows.Convert(1, radix, coarseCapacity, fineCapacity);
}

/// <summary>A number held as rungs in a store: one digit field per rung,
/// finest first, and the digits each rung still allows. Values cross in and
/// out only at the edge, as observations a host restricts or reads; between
/// rungs everything is relations narrowing supports.</summary>
public sealed class Ladder
{
    private readonly Supports[] possible;

    /// <summary>A ladder of <paramref name="rungs"/> fresh digits, every digit open.</summary>
    public Ladder(Store store, int radix, int rungs)
        : this(store, radix, [.. Enumerable.Range(0, rungs).Select(_ =>
            new Field(Numbers.Digit(radix), [.. Enumerable.Range(0, radix - 1).Select(_ => store.AddPlane())]))])
    {
    }

    /// <summary>A ladder read over digit fields that already exist, such as
    /// the first radix − 1 planes of material counts: the digits are views of
    /// the counts, and their supports start open.</summary>
    public Ladder(Store store, int radix, IReadOnlyList<Field> digits)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Count == 0) throw new ArgumentException("A number has at least one rung.", nameof(digits));
        if (digits.Any(d => d.Shape != Numbers.Digit(radix))) throw new ArgumentException("Each rung is a digit of the radix.", nameof(digits));
        Store = store;
        Radix = radix;
        Digits = Array.AsReadOnly(digits.ToArray());
        possible = [.. digits.Select(d => Supports.On(store, d))];
    }

    public Store Store { get; }

    public int Radix { get; }

    public IReadOnlyList<Field> Digits { get; }

    /// <summary>The digits each rung still allows.</summary>
    public IReadOnlyList<Supports> Possible => Array.AsReadOnly(possible);

    /// <summary>The largest value the rungs hold: radix^rungs − 1.</summary>
    public long Capacity => Enumerable.Range(0, Digits.Count).Aggregate(1L, (p, _) => p * Radix) - 1;

    /// <summary>Narrows every rung to the digits of a value: an observation.</summary>
    public void Restrict(int position, long value)
    {
        if (value < 0 || value > Capacity) throw new ArgumentOutOfRangeException(nameof(value));
        for (var r = 0; r < possible.Length; r++, value /= Radix)
            possible[r].Restrict(position, 1UL << (int)(value % Radix));
    }

    /// <summary>Narrows one rung to some digits, leaving the others as they are.</summary>
    public void Restrict(int position, int rung, ulong digits) => possible[rung].Restrict(position, digits);

    /// <summary>Narrows every rung to the digit its field holds.</summary>
    public void Observe(int position)
    {
        foreach (var rung in possible) rung.Observe(position);
    }

    /// <summary>Per rung, finest first, the digits still possible.</summary>
    public ulong[] Open(int position) => [.. possible.Select(p => p.Values(position))];

    /// <summary>The value when every rung has one digit left, else null.</summary>
    public long? Read(int position)
    {
        long value = 0, weight = 1;
        foreach (var rung in possible)
        {
            var digits = rung.Values(position);
            if (digits == 0 || (digits & (digits - 1)) != 0) return null;
            value += weight * System.Numerics.BitOperations.TrailingZeroCount(digits);
            weight *= Radix;
        }
        return value;
    }

    /// <summary>Wires a + b = sum rung by rung at <paramref name="positions"/>:
    /// one digit sum per rung, joined by carry supports. The finest carry in is
    /// held at 0; the last carry out is the overflow, left open unless the
    /// caller narrows it. Returns the wirings and the carries, finest first.</summary>
    public static (IReadOnlyList<Wiring> Wirings, IReadOnlyList<Supports> Carries) Add(Ladder a, Ladder b, Ladder sum, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(sum);
        ArgumentNullException.ThrowIfNull(positions);
        if (a.Store != b.Store || a.Store != sum.Store) throw new ArgumentException("A sum is wired within one store.", nameof(sum));
        if (a.Radix != b.Radix || a.Radix != sum.Radix || a.Digits.Count != b.Digits.Count || a.Digits.Count != sum.Digits.Count)
            throw new ArgumentException("The ladders share a radix and a number of rungs.", nameof(sum));
        var store = a.Store;
        var carries = Enumerable.Range(0, a.Digits.Count + 1).Select(_ => Supports.On(store, new Field(Numbers.Carry, store.AddPlane()))).ToArray();
        foreach (var position in positions.Indexes()) carries[0].Restrict(position, 1UL);
        var column = Numbers.DigitSum(a.Radix).Relation;
        var wirings = Enumerable.Range(0, a.Digits.Count).Select(r =>
        {
            var wiring = new Wiring(column, a.possible[r].Variable, b.possible[r].Variable, carries[r].Variable, carries[r + 1].Variable, sum.possible[r].Variable);
            foreach (var position in positions.Indexes()) wiring.Add(position, position, position, position, position);
            return wiring;
        }).ToArray();
        return (Array.AsReadOnly(wirings), Array.AsReadOnly(carries));
    }
}
