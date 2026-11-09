namespace Gojo.EncodingB;

/// <summary>Admitted (before, after) pairs over a scope of roles. Bit
/// <c>before + N × after</c> is set when that whole transition is admitted, where
/// N is the number of role tuples. Read byte by byte, each byte (for N = 8) is
/// an after-state and each set bit in it a before-state allowed to land there.
/// The accounting signature is checked over every admitted pair at load.</summary>
public sealed class TransitionLaw : LawTable
{
    private int[][]? afters;
    private string[]? writtenRoles;

    public TransitionLaw(
        string name,
        IReadOnlyList<LawRole> roles,
        ReadOnlySpan<byte> bytes,
        AccountingSignature signature,
        ConfluenceClass confluence,
        string provenance = "authored")
        : base(name, roles, checked(Product(roles) * Product(roles)), bytes, provenance)
    {
        ArgumentNullException.ThrowIfNull(signature);
        if (confluence == ConfluenceClass.MonotoneNarrowing)
            throw new ArgumentException("A transition changes state; monotone narrowing belongs to relation reads.", nameof(confluence));
        Signature = signature;
        Confluence = confluence;
        var violations = signature.Check(this, StateCount, Admits);
        if (violations.Count > 0)
            throw new ArgumentException($"Law {name} breaks its accounting signature: " + string.Join("; ", violations.Take(4)));
    }

    protected override string Kind => "transition";

    /// <summary>Number of whole states of the scope (N).</summary>
    public int StateCount => TupleCount;

    public AccountingSignature Signature { get; }
    public ConfluenceClass Confluence { get; }

    /// <summary>Builds a law from a step function. Return null for a before-state
    /// that admits no transition.</summary>
    public static TransitionLaw FromFunction(
        string name,
        IReadOnlyList<LawRole> roles,
        Func<int[], int[]?> step,
        AccountingSignature signature,
        ConfluenceClass confluence,
        string provenance = "authored")
    {
        ArgumentNullException.ThrowIfNull(step);
        var states = Product(roles);
        var probe = new RelationLaw(name, roles, new byte[(states + 7) / 8]);
        var bits = new List<int>();
        for (var before = 0; before < states; before++)
            if (step(probe.Tuple(before)) is { } after)
                bits.Add(before + states * probe.TupleIndex(after));
        return new TransitionLaw(name, roles, PackBits(states * states, bits), signature, confluence, provenance);
    }

    public static TransitionLaw FromPairs(
        string name,
        IReadOnlyList<LawRole> roles,
        IEnumerable<(int Before, int After)> pairs,
        AccountingSignature signature,
        ConfluenceClass confluence,
        string provenance = "authored")
    {
        var states = Product(roles);
        return new TransitionLaw(name, roles, PackBits(states * states, pairs.Select(p => p.Before + states * p.After)),
            signature, confluence, provenance);
    }

    public bool Admits(int before, int after)
    {
        if (before < 0 || before >= StateCount) throw new ArgumentOutOfRangeException(nameof(before));
        if (after < 0 || after >= StateCount) throw new ArgumentOutOfRangeException(nameof(after));
        return BitAt(before + StateCount * after);
    }

    /// <summary>After-states a before-state may become.</summary>
    public IReadOnlyList<int> Afters(int before)
    {
        afters ??= Enumerable.Range(0, StateCount)
            .Select(b => Enumerable.Range(0, StateCount).Where(a => BitAt(b + StateCount * a)).ToArray())
            .ToArray();
        return afters[before];
    }

    public bool IsFunctional => Enumerable.Range(0, StateCount).All(b => Afters(b).Count == 1);

    /// <summary>Roles some admitted transition actually changes. Only these are
    /// written when the law runs.</summary>
    public IReadOnlyList<string> WrittenRoles =>
        writtenRoles ??= Roles.Where((_, r) =>
            Enumerable.Range(0, StateCount).Any(b => Afters(b).Any(a => Tuple(a)[r] != Tuple(b)[r])))
            .Select(role => role.Name).ToArray();

    /// <summary>Prediction over joint support. <paramref name="joint"/>[t] is a lane
    /// plane: bit i means "lane i may be in whole state t". Each after-state
    /// collects every before-state that admits it. For the passage law this
    /// reduces to <c>J[6] |= J[3]; J[3] = 0</c>. Prediction never executes.</summary>
    public ulong[] PredictJoint(ReadOnlySpan<ulong> joint)
    {
        if (joint.Length != StateCount) throw new ArgumentException($"Supply {StateCount} joint planes.", nameof(joint));
        var next = new ulong[StateCount];
        for (var before = 0; before < StateCount; before++)
        {
            if (joint[before] == 0) continue;
            foreach (var after in Afters(before)) next[after] |= joint[before];
        }
        return next;
    }

    /// <summary>Bit-sliced execution as a sum of products. <paramref name="codes"/>[r][b]
    /// is a lane plane holding bit b of role r's code. For each before-state, the
    /// lanes in it are the AND of matching code planes; those lanes receive the
    /// after-state's code bits. Lanes whose before-state admits no transition, or
    /// more than one, are left unchanged and reported.</summary>
    public TransitionStep Step(ulong[][] codes, ulong lanes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        if (codes.Length != Roles.Count) throw new ArgumentException("Supply code planes for every role.", nameof(codes));
        var next = new ulong[Roles.Count][];
        for (var r = 0; r < Roles.Count; r++)
        {
            if (codes[r].Length != Roles[r].Domain.CodeBits)
                throw new ArgumentException($"Role {Roles[r].Name} needs {Roles[r].Domain.CodeBits} code planes.", nameof(codes));
            next[r] = new ulong[codes[r].Length];
        }
        ulong covered = 0, ambiguous = 0;
        for (var before = 0; before < StateCount; before++)
        {
            var minterm = lanes;
            var tuple = Tuple(before);
            for (var r = 0; r < Roles.Count && minterm != 0; r++)
                for (var b = 0; b < codes[r].Length; b++)
                    minterm &= ((tuple[r] >> b) & 1) != 0 ? codes[r][b] : ~codes[r][b];
            if (minterm == 0) continue;
            var candidates = Afters(before);
            if (candidates.Count != 1)
            {
                if (candidates.Count > 1) ambiguous |= minterm;
                continue;
            }
            covered |= minterm;
            var after = Tuple(candidates[0]);
            for (var r = 0; r < Roles.Count; r++)
                for (var b = 0; b < next[r].Length; b++)
                    if (((after[r] >> b) & 1) != 0) next[r][b] |= minterm;
        }
        var unchanged = lanes & ~covered;
        for (var r = 0; r < Roles.Count; r++)
            for (var b = 0; b < next[r].Length; b++)
                next[r][b] |= codes[r][b] & unchanged;
        return new TransitionStep(next, unchanged & ~ambiguous, ambiguous);
    }

    /// <summary>"[S=occupied, C=open, T=empty] -> [S=empty, C=open, T=occupied]: admitted".</summary>
    public string DescribeBit(int bit)
    {
        if (bit < 0 || bit >= BitCount) throw new ArgumentOutOfRangeException(nameof(bit));
        var before = bit % StateCount;
        var after = bit / StateCount;
        return $"{DescribeTuple(before)} -> {DescribeTuple(after)}: {(BitAt(bit) ? "admitted" : "not admitted")}";
    }
}

/// <summary>Result of one bit-sliced step. <see cref="Unadmitted"/> lanes had no
/// admitted transition (for example an out-of-domain code); <see cref="Ambiguous"/>
/// lanes had several. Both are left unchanged and stay visible.</summary>
public sealed record TransitionStep(ulong[][] After, ulong Unadmitted, ulong Ambiguous);
