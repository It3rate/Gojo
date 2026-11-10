namespace Gojo.EncodingC;

/// <summary>Admitted (before, after) pairs over the whole state of its roles.
/// Bit <c>before + N·after</c> is set when that transition is admitted, where N
/// is the number of states. Read byte by byte for N = 8, each byte is an
/// after-state and each set bit in it a before-state that may land there. The
/// accounting is checked over every admitted pair when the law is made.</summary>
public sealed class TransitionLaw : Law
{
    private int[][]? afters;
    private bool[]? writtenRoles;

    public TransitionLaw(IReadOnlyList<Shape> roles, ReadOnlySpan<byte> bytes, Accounting accounting)
        : this(roles, bytes, accounting, out var violations)
    {
        if (violations.Count > 0)
            throw new ArgumentException($"The table breaks its accounting in {violations.Count} pairs, first {violations[0]}.", nameof(bytes));
    }

    private TransitionLaw(IReadOnlyList<Shape> roles, ReadOnlySpan<byte> bytes, Accounting accounting, out IReadOnlyList<Violation> violations)
        : base(roles, checked((long)Product(roles) * Product(roles)), bytes)
    {
        ArgumentNullException.ThrowIfNull(accounting);
        Accounting = accounting;
        violations = accounting.Check(this, StateCount, Admits);
    }

    public int StateCount => TupleCount;

    public Accounting Accounting { get; }

    /// <summary>Builds a law from a step function. Return null for a
    /// before-state that admits no transition.</summary>
    public static TransitionLaw FromFunction(IReadOnlyList<Shape> roles, Func<int[], int[]?> step, Accounting accounting) =>
        new(roles, PackBits(Pairs(roles, step), roles), accounting);

    public static TransitionLaw FromPairs(IReadOnlyList<Shape> roles, IEnumerable<(int Before, int After)> pairs, Accounting accounting) =>
        new(roles, PackBits(pairs, roles), accounting);

    /// <summary>Makes the law, or returns null with every pair that breaks the
    /// accounting. For tables that were learned rather than authored.</summary>
    public static TransitionLaw? TryFromPairs(
        IReadOnlyList<Shape> roles,
        IEnumerable<(int Before, int After)> pairs,
        Accounting accounting,
        out IReadOnlyList<Violation> violations)
    {
        var law = new TransitionLaw(roles, PackBits(pairs, roles), accounting, out violations);
        return violations.Count == 0 ? law : null;
    }

    public bool Admits(int before, int after)
    {
        if ((uint)before >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(before));
        if ((uint)after >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(after));
        return BitAt(before + StateCount * after);
    }

    public IReadOnlyList<int> Afters(int before)
    {
        afters ??= Enumerable.Range(0, StateCount)
            .Select(b => Enumerable.Range(0, StateCount).Where(a => BitAt(b + StateCount * a)).ToArray())
            .ToArray();
        return afters[before];
    }

    public bool IsFunctional => Enumerable.Range(0, StateCount).All(b => Afters(b).Count == 1);

    /// <summary>Whether some admitted transition changes the role. A role no
    /// transition changes is only read.</summary>
    public bool Writes(int role)
    {
        writtenRoles ??= Enumerable.Range(0, Roles.Count).Select(r => ChangingBits(r) != 0).ToArray();
        return writtenRoles[role];
    }

    /// <summary>The bits of a role's code that some admitted transition changes.</summary>
    public ulong ChangingBits(int role)
    {
        ulong changing = 0;
        for (var before = 0; before < StateCount; before++)
            foreach (var after in Afters(before))
                changing |= Roles[role].Encode(Tuple(before)[role]) ^ Roles[role].Encode(Tuple(after)[role]);
        return changing;
    }

    /// <summary>Prediction over joint support: <paramref name="joint"/>[t] marks
    /// the lanes that may be in whole state t. Each after-state collects every
    /// before-state that admits it. Nothing is executed.</summary>
    public Plane[] PredictJoint(Plane[] joint)
    {
        ArgumentNullException.ThrowIfNull(joint);
        if (joint.Length != StateCount) throw new ArgumentException($"Supply {StateCount} joint planes.", nameof(joint));
        var next = Enumerable.Range(0, StateCount).Select(_ => new Plane(joint[0].Length)).ToArray();
        for (var before = 0; before < StateCount; before++)
            foreach (var after in Afters(before))
                next[after] = next[after].Or(joint[before]);
        return next;
    }

    /// <summary>Runs the law across lanes as a sum of products.
    /// <paramref name="codes"/>[r][b] is a lane plane holding bit b of role r.
    /// For each before-state the lanes in it are the AND of matching code
    /// planes, and those lanes take the after-state's bits. Lanes whose bits are
    /// not codes, or whose before-state admits no transition or several, keep
    /// their bits and are reported.</summary>
    public TransitionStep Step(Plane[][] codes, Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentNullException.ThrowIfNull(lanes);
        if (codes.Length != Roles.Count) throw new ArgumentException("Supply code planes for every role.", nameof(codes));
        var next = new Plane[Roles.Count][];
        for (var r = 0; r < Roles.Count; r++)
        {
            if (codes[r].Length != Roles[r].Bits) throw new ArgumentException($"Role {r} needs {Roles[r].Bits} code planes.", nameof(codes));
            next[r] = new Plane[Roles[r].Bits];
            for (var b = 0; b < next[r].Length; b++) next[r][b] = new Plane(lanes.Length);
        }
        var covered = new Plane(lanes.Length);
        var ambiguous = new Plane(lanes.Length);
        var patterns = new ulong[Roles.Count];
        var afterPatterns = new ulong[Roles.Count];
        for (var before = 0; before < StateCount; before++)
        {
            var tuple = Tuple(before);
            for (var r = 0; r < Roles.Count; r++) patterns[r] = Roles[r].Encode(tuple[r]);
            var candidates = Afters(before);
            if (candidates.Count == 1)
            {
                var after = Tuple(candidates[0]);
                for (var r = 0; r < Roles.Count; r++) afterPatterns[r] = Roles[r].Encode(after[r]);
            }
            for (var w = 0; w < lanes.WordCount; w++)
            {
                var m = lanes.Word(w);
                for (var r = 0; r < Roles.Count && m != 0; r++)
                    for (var b = 0; b < codes[r].Length; b++)
                        m &= ((patterns[r] >> b) & 1) != 0 ? codes[r][b].Word(w) : ~codes[r][b].Word(w);
                if (m == 0) continue;
                if (candidates.Count != 1)
                {
                    if (candidates.Count > 1) ambiguous.OrWord(w, m);
                    continue;
                }
                covered.OrWord(w, m);
                for (var r = 0; r < Roles.Count; r++)
                    for (var b = 0; b < next[r].Length; b++)
                        if (((afterPatterns[r] >> b) & 1) != 0) next[r][b].OrWord(w, m);
            }
        }
        var unchanged = lanes.AndNot(covered);
        for (var r = 0; r < Roles.Count; r++)
            for (var b = 0; b < next[r].Length; b++)
                next[r][b] = next[r][b].Or(codes[r][b].And(unchanged));
        return new TransitionStep(next, unchanged.AndNot(ambiguous), ambiguous);
    }

    private static byte[] PackBits(IEnumerable<(int Before, int After)> pairs, IReadOnlyList<Shape> roles)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        var states = (long)Product(roles);
        return PackBits(states * states, pairs.Select(p => p.Before + states * p.After));
    }

    private static IEnumerable<(int Before, int After)> Pairs(IReadOnlyList<Shape> roles, Func<int[], int[]?> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        var probe = new RelationLaw(roles, new byte[(Product(roles) + 7) / 8]);
        for (var before = 0; before < probe.TupleCount; before++)
            if (step(probe.Tuple(before)) is { } after)
                yield return (before, probe.TupleIndex(after));
    }
}

/// <summary>One run of a transition law across lanes. <see cref="Unadmitted"/>
/// lanes had no admitted transition (or bits that are not codes);
/// <see cref="Ambiguous"/> lanes had several. Both keep their bits and stay
/// visible.</summary>
public sealed record TransitionStep(Plane[][] After, Plane Unadmitted, Plane Ambiguous);
