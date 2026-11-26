using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>Sparse witness and optional trajectory store for Gojo structural
/// learning and symbolic regression. Unlike <see cref="LearnedTable"/>, it
/// never pre-allocates dense <c>StateCount²</c> positions and supports
/// hypothesizing compact equations while most of the domain is still
/// <see cref="LearnedRead.Unknown"/>.</summary>
public sealed class WitnessStore
{
    private readonly Shape[] roles;
    private readonly List<WitnessEntry> admitted = [];
    private readonly List<(int[] Before, int[] After)> unsupported = [];
    private readonly List<TrajectoryPoint> trajectory = [];

    public WitnessStore(IReadOnlyList<Shape> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Count == 0) throw new ArgumentException("Need at least one role.", nameof(roles));
        this.roles = [.. roles];
        Roles = Array.AsReadOnly(this.roles);
    }

    public IReadOnlyList<Shape> Roles { get; }
    public IReadOnlyList<WitnessEntry> Admitted => admitted.AsReadOnly();
    public IReadOnlyList<(int[] Before, int[] After)> Unsupported => unsupported.AsReadOnly();
    public IReadOnlyList<TrajectoryPoint> Trajectory => trajectory.AsReadOnly();

    public void Witness(int[] before, int[] after)
    {
        ValidateTuple(before);
        ValidateTuple(after);
        for (var i = 0; i < admitted.Count; i++)
        {
            if (admitted[i].Before.AsSpan().SequenceEqual(before) && admitted[i].After.AsSpan().SequenceEqual(after))
            {
                admitted[i] = admitted[i] with { Count = admitted[i].Count + 1 };
                return;
            }
        }
        admitted.Add(new WitnessEntry([.. before], [.. after], 1));
    }

    /// <summary>Witnesses an input-output pair <c>(inputs → output)</c> where
    /// the last role holds the output and prior roles are unchanged inputs.</summary>
    public void WitnessObservation(int[] inputs, int output)
    {
        if (inputs.Length + 1 != roles.Length) throw new ArgumentException("Inputs plus output must match Roles.Count.");
        var before = new int[roles.Length];
        var after = new int[roles.Length];
        for (var i = 0; i < inputs.Length; i++) before[i] = after[i] = inputs[i];
        after[^1] = output;
        Witness(before, after);
    }

    public void MarkUnsupported(int[] before, int[] after)
    {
        ValidateTuple(before);
        ValidateTuple(after);
        unsupported.Add(([.. before], [.. after]));
    }

    public void WitnessTrajectory(int tick, int source, int gain, int output) =>
        trajectory.Add(new TrajectoryPoint(tick, source, gain, output));

    private void ValidateTuple(int[] tuple)
    {
        ArgumentNullException.ThrowIfNull(tuple);
        if (tuple.Length != roles.Length) throw new ArgumentException("Tuple length must match Roles.Count.");
        for (var r = 0; r < roles.Length; r++)
            if ((uint)tuple[r] >= (uint)roles[r].Size)
                throw new ArgumentOutOfRangeException(nameof(tuple), $"Value {tuple[r]} out of range for role {r}.");
    }
}

public sealed record WitnessEntry(int[] Before, int[] After, int Count);

public sealed record TrajectoryPoint(int Tick, int Source, int Gain, int Output);

/// <summary>Exact structural hints calculated strictly from a law's table bits
/// and checked <see cref="Accounting"/> — never from names or enums.</summary>
public sealed record LawHints(
    int[] RoleFate,
    int[] ReadRoles,
    int[] WrittenRoles,
    int[] ConservedRoles,
    bool IsFunctional,
    bool IsReversible,
    (int From, int To, int Take, int Give)[] Trades,
    bool? AdditiveSeparable,
    int? ConstantCorner,
    bool? BoundaryCornerOnly,
    bool? TranslationInvariant);

/// <summary>Pure-numeric compressed balance equation (<c>Σ Weights[r] · v_r = 0</c>)
/// recovered from a table, including any auto-discovered free residue role,
/// optional blocked bit, explicit exception pairs, and MDL bit counts.</summary>
public sealed record BalanceFit(
    int[] Weights,
    int[] Reads,
    int[] Writes,
    int FreeResidueCapacity,
    int FreeResidueWeight,
    int BlockedRole,
    (int Before, int After)[] Exceptions,
    int CanonicalBits,
    int CompressedBits,
    bool ExactTableMatch)
{
    public int BitsSaved => CanonicalBits - CompressedBits;
    public bool CompressesByMdl => CompressedBits < CanonicalBits;
}

/// <summary>Pure-numeric compressed product equation (<c>Scale · a · b = value [+ overflow]</c>)
/// with explicit exception pairs and MDL bit counts.</summary>
public sealed record ProductFit(
    int LeftRole,
    int RightRole,
    int ProductRole,
    int OverflowRole,
    int Scale,
    (int Before, int After)[] Exceptions,
    int CanonicalBits,
    int CompressedBits,
    bool ExactTableMatch)
{
    public int BitsSaved => CanonicalBits - CompressedBits;
    public bool CompressesByMdl => CompressedBits < CanonicalBits;
}

/// <summary>Pure-numeric compressed conserving packet-trade equation
/// (<c>Convert(Take, Give)</c>) recovered from table bits and accounting.</summary>
public sealed record PacketTradeFit(
    int SourceRole,
    int TargetRole,
    int Take,
    int Give,
    int CanonicalBits,
    int CompressedBits,
    bool ExactTableMatch)
{
    public int BitsSaved => CanonicalBits - CompressedBits;
    public bool CompressesByMdl => CompressedBits < CanonicalBits;
}

/// <summary>An executable sparse algebraic hypothesis over two inputs and one
/// output, represented strictly by numbers (never strings or opcode enums):
/// linear <c>W0·a + W1·b + Offset</c>, bilinear <c>Bilinear·a·b</c>, or
/// lattice fold (<c>FoldSign = -1</c> for min, <c>+1</c> for max).</summary>
public sealed record SparseHypothesis(
    int W0,
    int W1,
    int Offset,
    int Bilinear,
    int FoldSign,
    int DescriptionBits)
{
    public int Predict(int a, int b)
    {
        if (FoldSign < 0) return a < b ? a : b;
        if (FoldSign > 0) return a > b ? a : b;
        return W0 * a + W1 * b + Offset + Bilinear * a * b;
    }

    public int Predict(int[] inputs) => Predict(inputs[0], inputs[1]);

    /// <summary>Materializes this hypothesis as a genuine bidirectional
    /// <see cref="RelationLaw"/> bindable onto fresh sites and saveable through
    /// <see cref="Transport"/>.</summary>
    public RelationLaw ToRelation(Shape left, Shape right, Shape output) =>
        RelationLaw.FromPredicate([left, right, output], t => Predict(t[0], t[1]) == t[2]);

    /// <summary>Materializes this hypothesis as a directional functional
    /// <see cref="TransitionLaw"/> (holding where the predicted output exceeds
    /// the target shape).</summary>
    public TransitionLaw ToTransition(Shape left, Shape right, Shape output, bool withBlocked = false)
    {
        var rel = ToRelation(left, right, output);
        Shape[] roles = withBlocked ? [left, right, output, Shape.Bit] : [left, right, output];
        return Operations.Bind(rel, roles, [0, 1, -1], [-1, -1, 2], blocked: withBlocked ? 3 : -1);
    }
}

/// <summary>Pure-numeric trajectory signature distinguishing live stretch,
/// captured-sample repeat, and live-reading coasting repeat under a mid-flight
/// source edit (ops_sol §3 and ops_claude §3.3).</summary>
public sealed record RealizationSignature(
    int[] StepDeltas,
    int[] SecondDeltas,
    bool RespondedImmediatelyToEdit,
    bool IgnoredMidFlightEdit,
    bool TrackedLiveSourcePerStep);

/// <summary>Exact Gojo-native structural analyzer and discrete symbolic
/// regressor (the inverse of <see cref="Operations.Bind"/>). Computes hints
/// from table bits and accounting, recovers integer-weight balances (including
/// auto-discovered residue roles and exceptions), products, and packet trades,
/// proposes competing sparse hypotheses, selects separating discriminator
/// probes, and distinguishes equifinal realizations from execution traces.</summary>
public static class StructuralCompression
{
    /// <summary>Calculates structural hints strictly from <paramref name="law"/>'s
    /// table bits and checked accounting.</summary>
    public static LawHints ReadHints(TransitionLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);
        var roleCount = law.Roles.Count;
        var fate = new int[roleCount];
        var read = new List<int>();
        var written = new List<int>();
        var conserved = new List<int>();
        for (var r = 0; r < roleCount; r++)
        {
            var w = law.Writes(r);
            var c = law.Accounting.Conserves(r);
            fate[r] = (w ? 1 : 0) | (c ? 2 : 0);
            if (w) written.Add(r); else read.Add(r);
            if (c) conserved.Add(r);
        }

        // Collect conserved trades (loss, gain) between every ordered pair of written roles.
        var trades = new List<(int From, int To, int Take, int Give)>();
        if (written.Count >= 2)
        {
            var seen = new HashSet<(int, int, int, int)>();
            for (var before = 0; before < law.StateCount; before++)
            {
                var bt = law.Tuple(before);
                foreach (var after in law.Afters(before))
                {
                    if (after == before) continue;
                    var at = law.Tuple(after);
                    for (var i = 0; i < written.Count; i++)
                    {
                        var rFrom = written[i];
                        var loss = bt[rFrom] - at[rFrom];
                        if (loss <= 0) continue;
                        for (var j = 0; j < written.Count; j++)
                        {
                            if (i == j) continue;
                            var rTo = written[j];
                            var gain = at[rTo] - bt[rTo];
                            if (gain > 0 && seen.Add((rFrom, rTo, loss, gain)))
                                trades.Add((rFrom, rTo, loss, gain));
                        }
                    }
                }
            }
        }

        bool? additiveSep = null;
        int? constCorner = null;
        bool? boundaryOnly = null;
        bool? transInv = null;

        // For 2-read, 1-write functional laws where output does not depend on prior output:
        if (law.IsFunctional && read.Count == 2 && written.Count == 1)
        {
            var r0 = read[0];
            var r1 = read[1];
            var rOut = written[0];
            var s0 = law.Roles[r0].Size;
            var s1 = law.Roles[r1].Size;
            if (s0 >= 2 && s1 >= 2)
            {
                int Eval(int a, int b)
                {
                    var st = new int[roleCount];
                    st[r0] = a;
                    st[r1] = b;
                    return law.Tuple(law.Afters(law.TupleIndex(st))[0])[rOut];
                }

                var allZero = true;
                var allSame = true;
                var firstCorner = 0;
                var haveCorner = false;
                var nonZeroOnlyOnDiagOrWall = true;
                var allTrans = true;

                for (var a = 0; a + 1 < s0; a++)
                    for (var b = 0; b + 1 < s1; b++)
                    {
                        var f00 = Eval(a, b);
                        var f10 = Eval(a + 1, b);
                        var f01 = Eval(a, b + 1);
                        var f11 = Eval(a + 1, b + 1);
                        if (f11 != f00) allTrans = false;
                        var corner = f11 - f10 - f01 + f00;
                        if (!haveCorner) { firstCorner = corner; haveCorner = true; }
                        else if (corner != firstCorner) allSame = false;
                        if (corner != 0)
                        {
                            allZero = false;
                            // Boundary/diagonal check: either a == b (gap diagonal) or hits max/min output wall
                            var hitsWall = f00 == 0 || f11 == 0 || f11 == law.Roles[rOut].Size - 1 || f10 == law.Roles[rOut].Size - 1 || f01 == law.Roles[rOut].Size - 1;
                            if (a != b && !hitsWall) nonZeroOnlyOnDiagOrWall = false;
                        }
                    }

                additiveSep = allZero;
                if (haveCorner && allSame && firstCorner != 0) constCorner = firstCorner;
                boundaryOnly = !allZero && nonZeroOnlyOnDiagOrWall;
                transInv = allTrans;
            }
        }

        return new LawHints(
            fate,
            [.. read],
            [.. written],
            [.. conserved],
            law.IsFunctional,
            law.IsReversible,
            [.. trades],
            additiveSep,
            constCorner,
            boundaryOnly,
            transInv);
    }

    /// <summary>Exact integer-kernel balance recognizer (<see cref="Operations.Bind"/>'s
    /// inverse). Recovers linear balances, weighted sums, tallies, folds with
    /// auto-discovered free residue roles (shortfall, accumulate), radix-B
    /// combined and split digit adders, landmark shears (with or without
    /// blocked bit), and equations with a small set of explicit exceptions.</summary>
    public static BalanceFit? TryRecognizeBalance(TransitionLaw law, int maxWeight = 10, int maxExceptions = 4)
    {
        ArgumentNullException.ThrowIfNull(law);
        if (!law.IsFunctional) return null;

        var roleCount = law.Roles.Count;
        var written = Enumerable.Range(0, roleCount).Where(law.Writes).ToArray();
        if (written.Length == 0 || written.Length > 2) return null;

        // Detect optional trailing blocked bit (a Bit role written to 0 on changing transitions and 1 on held states).
        var blockedRole = -1;
        var mainWritten = written;
        if (written.Length == 2)
        {
            foreach (var cand in written)
            {
                if (law.Roles[cand] != Shape.Bit) continue;
                var other = written[0] == cand ? written[1] : written[0];
                if (LooksLikeBlockedBit(law, cand, other))
                {
                    blockedRole = cand;
                    mainWritten = [other];
                    break;
                }
            }
        }

        // Case 1: Two main written roles (e.g., HalfAdder / FullAdder / Radix-B DigitAddCombined: sum + B*cout).
        if (mainWritten.Length == 2)
            return TryRecognizeTwoOutputBalance(law, mainWritten, maxWeight, maxExceptions);

        // Case 2: Single main written role (covers Sum, Tally, Shortfall, Accumulate, Kick, Drift, Booking, DigitSum, DigitCarry, Doubled).
        var outRole = mainWritten[0];
        var activeInputs = FindActiveBeforeRoles(law, outRole, blockedRole);
        if (activeInputs.Length == 0) return null;

        // Estimate integer slope w_j along each active input role using mode of non-zero first differences.
        var weights = new int[activeInputs.Length];
        var anyZeroSlope = false;
        for (var i = 0; i < activeInputs.Length; i++)
        {
            var slope = EstimateModeSlope(law, activeInputs[i], outRole, blockedRole, maxWeight);
            if (slope == 0) { anyZeroSlope = true; break; }
            weights[i] = slope;
        }

        // First try direct balance without residue: Σ weights[i]*before[activeInputs[i]] - after[outRole] = 0
        if (!anyZeroSlope)
        {
            var direct = VerifySingleOutputBalance(law, activeInputs, weights, outRole, blockedRole, freeCap: 0, freeWeight: 0, maxExceptions);
            if (direct is not null && direct.ExactTableMatch) return direct;

            // Next try discovering ONE hidden free residue role from the residual!
            var residFit = TryWithDiscoveredResidue(law, activeInputs, weights, outRole, blockedRole, maxExceptions);
            if (residFit is not null && residFit.ExactTableMatch) return residFit;

            // Keep best near-exact fit with exceptions if within budget & MDL wins!
            if (direct is not null && direct.Exceptions.Length <= maxExceptions) return direct;
            if (residFit is not null && residFit.Exceptions.Length <= maxExceptions) return residFit;
        }

        // Special fallback for carry/borrow single-bit outputs where slopes are 0 except at radix threshold B:
        // a + b + cin = B*cout + freeDigit (free residue capacity = B - 1, weight = -1; cout weight = -B).
        if (law.Roles[outRole] == Shape.Bit && activeInputs.Length >= 2)
        {
            var radix = law.Roles[activeInputs[0]].Size;
            if (radix >= 2 && radix <= maxWeight)
            {
                var carryFit = TryRecognizeCarryOrBorrowBit(law, activeInputs, outRole, radix, maxExceptions);
                if (carryFit is not null) return carryFit;
            }
        }

        return null;
    }

    /// <summary>Recovers a bounded product equation (<c>Scale · a · b = value</c>
    /// or lossless <c>a · b = value + overflow</c>), tolerating up to
    /// <paramref name="maxExceptions"/> mutated entries and reporting exact MDL.</summary>
    public static ProductFit? TryRecognizeProduct(TransitionLaw law, int maxExceptions = 4)
    {
        ArgumentNullException.ThrowIfNull(law);
        if (!law.IsFunctional) return null;
        var roleCount = law.Roles.Count;
        var written = Enumerable.Range(0, roleCount).Where(law.Writes).ToArray();
        var read = Enumerable.Range(0, roleCount).Where(r => !law.Writes(r)).ToArray();

        // Case A: 4-role lossless bounded multiply (a [read], b [read], value [written], overflow [written])
        if (roleCount == 4 && read.Length == 2 && written.Length == 2)
        {
            var aR = read[0]; var bR = read[1];
            var maxProd = (ulong)(law.Roles[aR].Size - 1) * (ulong)(law.Roles[bR].Size - 1);
            foreach (var (vR, oR) in new[] { (written[0], written[1]), (written[1], written[0]) })
            {
                var vCap = (ulong)(law.Roles[vR].Size - 1);
                var oCap = (ulong)(law.Roles[oR].Size - 1);
                if (vCap + oCap < maxProd) continue;
                var exceptions = new List<(int, int)>();
                for (var before = 0; before < law.StateCount && exceptions.Count <= maxExceptions; before++)
                {
                    var bt = law.Tuple(before);
                    var prod = (ulong)bt[aR] * (ulong)bt[bR];
                    var expV = prod <= vCap ? (int)prod : (int)vCap;
                    var expO = prod > vCap ? (int)(prod - vCap) : 0;
                    var actualIdx = law.Afters(before)[0];
                    var at = law.Tuple(actualIdx);
                    if (at[aR] != bt[aR] || at[bR] != bt[bR] || at[vR] != expV || at[oR] != expO)
                        exceptions.Add((before, actualIdx));
                }
                if (exceptions.Count <= maxExceptions)
                {
                    var compBits = ProductDescriptionBits(4, hasOverflow: true, exceptions.Count, law.StateCount);
                    return new ProductFit(aR, bR, vR, oR, Scale: 1, [.. exceptions], law.BitCount, compBits, exceptions.Count == 0);
                }
            }
        }

        // Case B: 3-role (or 2-role squaring a->a^2) functional product with optional scale k.
        if (written.Length == 1 && read.Length is 1 or 2)
        {
            var aR = read[0];
            var bR = read.Length == 2 ? read[1] : read[0];
            var pR = written[0];
            var pCap = law.Roles[pR].Size - 1;
            var hints = ReadHints(law);
            var scale = hints.ConstantCorner ?? 1;
            if (scale < 1 || scale > 4) scale = 1;

            var exceptions = new List<(int, int)>();
            for (var before = 0; before < law.StateCount && exceptions.Count <= maxExceptions; before++)
            {
                var bt = law.Tuple(before);
                var prod = (long)scale * bt[aR] * bt[bR];
                var expP = prod <= pCap ? (int)prod : bt[pR]; // holds when out of range
                var actualIdx = law.Afters(before)[0];
                var at = law.Tuple(actualIdx);
                if (at[aR] != bt[aR] || at[bR] != bt[bR] || at[pR] != expP)
                    exceptions.Add((before, actualIdx));
            }
            if (exceptions.Count <= maxExceptions)
            {
                var compBits = ProductDescriptionBits(roleCount, hasOverflow: false, exceptions.Count, law.StateCount);
                return new ProductFit(aR, bR, pR, OverflowRole: -1, scale, [.. exceptions], law.BitCount, compBits, exceptions.Count == 0);
            }
        }

        return null;
    }

    /// <summary>Recovers a conserving packet trade (<c>Flows.Convert(take, give)</c>)
    /// strictly from <see cref="LawHints.Trades"/>, <see cref="PreservedSum"/>,
    /// and exhaustive table verification.</summary>
    public static PacketTradeFit? TryRecognizePacketTrade(TransitionLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);
        if (law.Roles.Count != 2 || !law.Roles[0].IsUnary || !law.Roles[1].IsUnary || !law.IsFunctional) return null;
        var hints = ReadHints(law);
        if (hints.Trades.Length != 1) return null;
        var (from, to, take, give) = hints.Trades[0];
        if (from != 0 || to != 1 || take < 1 || give < 1) return null;

        var srcCap = law.Roles[0].Size - 1;
        var dstCap = law.Roles[1].Size - 1;
        for (var s = 0; s <= srcCap; s++)
            for (var d = 0; d <= dstCap; d++)
            {
                var after = law.Tuple(law.Afters(law.TupleIndex([s, d]))[0]);
                var canFire = s >= take && d + give <= dstCap;
                if (after[0] != (canFire ? s - take : s) || after[1] != (canFire ? d + give : d)) return null;
            }
        var compBits = GammaBits(take) + GammaBits(give) + GammaBits(srcCap) + GammaBits(dstCap) + 4;
        return new PacketTradeFit(from, to, take, give, law.BitCount, compBits, ExactTableMatch: true);
    }

    /// <summary>Sparse symbolic regression over partial observations: returns
    /// every compact integer hypothesis consistent with all
    /// <see cref="WitnessStore.Admitted"/> pairs and all
    /// <see cref="WitnessStore.Unsupported"/> exclusions, ordered simplest
    /// first by description length.</summary>
    public static IReadOnlyList<SparseHypothesis> ProposeFromSparse(WitnessStore witnesses, int maxWeight = 3, int maxOffset = 8)
    {
        ArgumentNullException.ThrowIfNull(witnesses);
        if (witnesses.Roles.Count != 3) throw new ArgumentException("Sparse two-input discovery expects 3 roles (a, b, output).", nameof(witnesses));

        var candidates = new List<SparseHypothesis>();

        // 1. Pure product a*b and scaled k*a*b
        for (var k = 1; k <= 2; k++)
            candidates.Add(new SparseHypothesis(0, 0, 0, Bilinear: k, FoldSign: 0, DescriptionBits: 4 + GammaBits(k)));

        // 2. Min / Max lattice folds (explicitly tests tiny-domain identifiability aliasing!)
        candidates.Add(new SparseHypothesis(0, 0, 0, Bilinear: 0, FoldSign: -1, DescriptionBits: 5));
        candidates.Add(new SparseHypothesis(0, 0, 0, Bilinear: 0, FoldSign: +1, DescriptionBits: 5));

        // 3. Linear balances w0*a + w1*b + offset
        for (var w0 = 0; w0 <= maxWeight; w0++)
            for (var w1 = 0; w1 <= maxWeight; w1++)
                for (var off = 0; off <= maxOffset; off++)
                {
                    if (w0 == 0 && w1 == 0 && off == 0) continue;
                    var bits = 3 + GammaBits(w0 + 1) + GammaBits(w1 + 1) + (off == 0 ? 1 : 2 + GammaBits(off));
                    candidates.Add(new SparseHypothesis(w0, w1, off, Bilinear: 0, FoldSign: 0, DescriptionBits: bits));
                }

        var survivors = new List<SparseHypothesis>();
        foreach (var h in candidates)
        {
            var ok = true;
            for (var i = 0; i < witnesses.Admitted.Count && ok; i++)
            {
                var e = witnesses.Admitted[i];
                if (h.Predict(e.Before[0], e.Before[1]) != e.After[2]) ok = false;
            }
            for (var i = 0; i < witnesses.Unsupported.Count && ok; i++)
            {
                var (b, a) = witnesses.Unsupported[i];
                if (h.Predict(b[0], b[1]) == a[2]) ok = false;
            }
            if (ok) survivors.Add(h);
        }

        survivors.Sort((x, y) => x.DescriptionBits.CompareTo(y.DescriptionBits));
        return survivors;
    }

    /// <summary>Finds an unseen input probe <c>(a, b)</c> inside the role
    /// domains that maximally separates the surviving hypotheses (returning
    /// null if fewer than two candidates survive or all agree on the domain).</summary>
    public static int[]? FindDiscriminator(
        IReadOnlyList<SparseHypothesis> candidates,
        WitnessStore witnesses)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(witnesses);
        if (candidates.Count < 2) return null;

        var seen = new HashSet<(int, int)>();
        foreach (var e in witnesses.Admitted) seen.Add((e.Before[0], e.Before[1]));

        var maxA = witnesses.Roles[0].Size - 1;
        var maxB = witnesses.Roles[1].Size - 1;
        int[]? bestProbe = null;
        var bestDistinct = 1;
        var distinct = new HashSet<int>();

        for (var a = 0; a <= maxA; a++)
            for (var b = 0; b <= maxB; b++)
            {
                if (seen.Contains((a, b))) continue;
                distinct.Clear();
                for (var i = 0; i < candidates.Count; i++) distinct.Add(candidates[i].Predict(a, b));
                if (distinct.Count > bestDistinct)
                {
                    bestDistinct = distinct.Count;
                    bestProbe = [a, b];
                }
            }
        return bestProbe;
    }

    /// <summary>Analyzes an execution trace (e.g. changing source 4 → 5 mid-run
    /// while holding gain = 3) to separate live stretch, captured-sample
    /// repeat, and live-reading coasting repeat strictly from numeric deltas.</summary>
    public static RealizationSignature AnalyzeRealization(IReadOnlyList<TrajectoryPoint> trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (trace.Count < 2) throw new ArgumentException("Need at least 2 trace points.", nameof(trace));

        var deltas = new int[trace.Count - 1];
        for (var i = 0; i < deltas.Length; i++)
            deltas[i] = trace[i + 1].Output - trace[i].Output;

        var second = new int[deltas.Length > 0 ? deltas.Length - 1 : 0];
        for (var i = 0; i < second.Length; i++)
            second[i] = deltas[i + 1] - deltas[i];

        var editIdx = -1;
        for (var i = 1; i < trace.Count; i++)
            if (trace[i].Source != trace[0].Source) { editIdx = i; break; }

        var respondedImmediately = false;
        var ignoredEdit = false;
        var trackedLivePerStep = false;

        if (editIdx >= 1)
        {
            var pt = trace[editIdx];
            // Live stretch: output equals gain * newSource immediately upon edit!
            if (pt.Output == pt.Gain * pt.Source && trace[0].Output == trace[0].Gain * trace[0].Source)
                respondedImmediately = true;

            // Captured-sample repeat: every step delta stays equal to initial source0 despite the edit!
            if (deltas.All(d => d == trace[0].Source) && trace[0].Output == trace[0].Source)
                ignoredEdit = true;

            // Live-reading repeat (coasting): each step delta equals the live source active on that step!
            var matchesLive = trace[0].Output == trace[0].Source;
            for (var i = 0; i < deltas.Length && matchesLive; i++)
                if (deltas[i] != trace[i + 1].Source) matchesLive = false;
            if (matchesLive && !ignoredEdit) trackedLivePerStep = true;
        }

        return new RealizationSignature(deltas, second, respondedImmediately, ignoredEdit, trackedLivePerStep);
    }

    // ---- Private exact helpers ----

    private static bool LooksLikeBlockedBit(TransitionLaw law, int bitRole, int mainRole)
    {
        var sawZero = false;
        var sawOne = false;
        for (var before = 0; before < law.StateCount; before++)
        {
            var bt = law.Tuple(before);
            var at = law.Tuple(law.Afters(before)[0]);
            var changedMain = at[mainRole] != bt[mainRole];
            if (changedMain && at[bitRole] != 0) return false;
            if (at[bitRole] == 0) sawZero = true; else sawOne = true;
        }
        return sawZero && sawOne;
    }

    private static int[] FindActiveBeforeRoles(TransitionLaw law, int outRole, int blockedRole)
    {
        var roleCount = law.Roles.Count;
        var active = new List<int>();
        for (var r = 0; r < roleCount; r++)
        {
            if (r == blockedRole) continue;
            var depends = false;
            var size = law.Roles[r].Size;
            for (var before = 0; before < law.StateCount && !depends; before++)
            {
                var bt = law.Tuple(before);
                if (bt[r] + 1 >= size) continue;
                var bt2 = (int[])bt.Clone();
                bt2[r]++;
                var y1 = law.Tuple(law.Afters(before)[0])[outRole];
                var y2 = law.Tuple(law.Afters(law.TupleIndex(bt2))[0])[outRole];
                if (y1 != y2) depends = true;
            }
            if (depends) active.Add(r);
        }
        return [.. active];
    }

    private static int EstimateModeSlope(TransitionLaw law, int inRole, int outRole, int blockedRole, int maxWeight)
    {
        var size = law.Roles[inRole].Size;
        var counts = new Dictionary<int, int>();
        for (var before = 0; before < law.StateCount; before++)
        {
            var bt = law.Tuple(before);
            if (bt[inRole] + 1 >= size) continue;
            var at1 = law.Tuple(law.Afters(before)[0]);
            if (blockedRole >= 0 && at1[blockedRole] == 1) continue;
            var bt2 = (int[])bt.Clone();
            bt2[inRole]++;
            var at2 = law.Tuple(law.Afters(law.TupleIndex(bt2))[0]);
            if (blockedRole >= 0 && at2[blockedRole] == 1) continue;
            var diff = at2[outRole] - at1[outRole];
            if (diff == 0 || diff > maxWeight || diff < -maxWeight) continue;
            counts[diff] = counts.TryGetValue(diff, out var c) ? c + 1 : 1;
        }
        var bestSlope = 0;
        var bestCount = 0;
        foreach (var (slope, c) in counts)
            if (c > bestCount) { bestCount = c; bestSlope = slope; }
        return bestSlope;
    }

    private static BalanceFit? VerifySingleOutputBalance(
        TransitionLaw law,
        int[] activeInputs,
        int[] inWeights,
        int outRole,
        int blockedRole,
        int freeCap,
        int freeWeight,
        int maxExceptions)
    {
        var hasFree = freeCap > 0 && freeWeight != 0;
        var relCount = activeInputs.Length + 1 + (hasFree ? 1 : 0);
        var relShapes = new Shape[relCount];
        var relWeights = new int[relCount];
        var reads = new int[relCount];
        var writes = new int[relCount];
        for (var i = 0; i < activeInputs.Length; i++)
        {
            relShapes[i] = law.Roles[activeInputs[i]];
            relWeights[i] = inWeights[i];
            reads[i] = activeInputs[i];
            writes[i] = -1;
        }
        relShapes[activeInputs.Length] = law.Roles[outRole];
        relWeights[activeInputs.Length] = -1;
        reads[activeInputs.Length] = -1;
        writes[activeInputs.Length] = outRole;
        if (hasFree)
        {
            relShapes[^1] = freeCap == 1 ? Shape.Bit : Shape.Binary(freeCap + 1);
            relWeights[^1] = freeWeight;
            reads[^1] = -1;
            writes[^1] = -1;
        }

        var outCap = law.Roles[outRole].Size - 1;
        var exceptions = new List<(int, int)>();
        for (var before = 0; before < law.StateCount && exceptions.Count <= maxExceptions; before++)
        {
            var bt = law.Tuple(before);
            long target = 0;
            for (var i = 0; i < activeInputs.Length; i++) target += (long)inWeights[i] * bt[activeInputs[i]];

            int expOut;
            var expBlocked = 0;
            if (!hasFree)
            {
                if (target >= 0 && target <= outCap) expOut = (int)target;
                else { expOut = bt[outRole]; expBlocked = 1; }
            }
            else
            {
                // Find admitted output minimizing free residue r in [0..freeCap] with target - y + freeWeight * r == 0
                // => y = target + freeWeight * r
                var found = false;
                expOut = bt[outRole];
                for (var r = 0; r <= freeCap; r++)
                {
                    var candY = target + (long)freeWeight * r;
                    if (candY >= 0 && candY <= outCap) { expOut = (int)candY; found = true; break; }
                }
                if (!found) expBlocked = 1;
            }

            var actualIdx = law.Afters(before)[0];
            var at = law.Tuple(actualIdx);
            var match = at[outRole] == expOut && (blockedRole < 0 || at[blockedRole] == expBlocked);
            for (var r = 0; r < law.Roles.Count && match; r++)
                if (r != outRole && r != blockedRole && at[r] != bt[r]) match = false;
            if (!match) exceptions.Add((before, actualIdx));
        }

        if (exceptions.Count > maxExceptions) return null;

        // If 0 exceptions, confirm exact SameTable against Operations.Bind when table size allows!
        var exact = exceptions.Count == 0;
        if (exact)
        {
            long relBits = 1;
            foreach (var sh in relShapes) relBits *= sh.Size;
            if (relBits <= Law.MaxBits)
            {
                var candidate = Operations.Bind(Operations.Balance(relShapes, relWeights), law.Roles, reads, writes, blocked: blockedRole);
                exact = candidate.SameTable(law);
                if (!exact) return null;
            }
        }

        var compBits = BalanceDescriptionBits(relWeights, freeCap, blockedRole >= 0, exceptions.Count, law.StateCount);
        return new BalanceFit(relWeights, reads, writes, freeCap, freeWeight, blockedRole, [.. exceptions], law.BitCount, compBits, exact);
    }

    private static BalanceFit? TryWithDiscoveredResidue(
        TransitionLaw law,
        int[] activeInputs,
        int[] inWeights,
        int outRole,
        int blockedRole,
        int maxExceptions)
    {
        var outCap = law.Roles[outRole].Size - 1;
        // Inspect residuals (y_actual - target) across all states to detect residue sign & magnitude!
        int minPos = int.MaxValue, maxPos = 0;
        int minNeg = int.MaxValue, maxNeg = 0;
        var modStep = 0;
        for (var before = 0; before < law.StateCount; before++)
        {
            var bt = law.Tuple(before);
            long target = 0;
            for (var i = 0; i < activeInputs.Length; i++) target += (long)inWeights[i] * bt[activeInputs[i]];
            var y = law.Tuple(law.Afters(before)[0])[outRole];
            var diff = (int)(y - target); // we want freeWeight * r == diff
            if (diff > 0)
            {
                if (diff < minPos) minPos = diff;
                if (diff > maxPos) maxPos = diff;
                modStep = Gcd(modStep, diff);
            }
            else if (diff < 0)
            {
                var ad = -diff;
                if (ad < minNeg) minNeg = ad;
                if (ad > maxNeg) maxNeg = ad;
                modStep = Gcd(modStep, ad);
            }
        }

        var tries = new List<(int Cap, int Weight)>();
        if (maxPos > 0 && maxNeg == 0)
        {
            // e.g., Shortfall: y = 0 when target < 0 -> diff = -target > 0 -> freeWeight = +1, cap = maxPos;
            // OR SubDiff wrap: diff = +B -> freeWeight = +B, cap = maxPos / B.
            if (modStep > 1 && maxPos / modStep <= 3) tries.Add((maxPos / modStep, +modStep));
            tries.Add((maxPos, +1));
        }
        else if (maxNeg > 0 && maxPos == 0)
        {
            // e.g., Accumulate overflow: y = cap when target > cap -> diff = cap - target < 0 -> freeWeight = -1, cap = maxNeg;
            // OR DigitSum wrap: diff = -B -> freeWeight = -B, cap = maxNeg / B.
            if (modStep > 1 && maxNeg / modStep <= 3) tries.Add((maxNeg / modStep, -modStep));
            tries.Add((maxNeg, -1));
        }

        BalanceFit? best = null;
        foreach (var (cap, w) in tries)
        {
            if (cap < 1 || cap > 63) continue;
            var fit = VerifySingleOutputBalance(law, activeInputs, inWeights, outRole, blockedRole, cap, w, maxExceptions);
            if (fit is null) continue;
            if (fit.ExactTableMatch) return fit;
            if (best is null || fit.Exceptions.Length < best.Exceptions.Length) best = fit;
        }
        return best;
    }

    private static BalanceFit? TryRecognizeTwoOutputBalance(TransitionLaw law, int[] mainWritten, int maxWeight, int maxExceptions)
    {
        // Identify digit role (size B >= 2) and carry/bit role (size 2).
        int sumR, carryR;
        if (law.Roles[mainWritten[1]] == Shape.Bit && law.Roles[mainWritten[0]].Size >= 2)
        {
            sumR = mainWritten[0]; carryR = mainWritten[1];
        }
        else if (law.Roles[mainWritten[0]] == Shape.Bit && law.Roles[mainWritten[1]].Size >= 2)
        {
            sumR = mainWritten[1]; carryR = mainWritten[0];
        }
        else return null;

        var radix = law.Roles[sumR].Size;
        if (radix > maxWeight) return null;

        var inputs = Enumerable.Range(0, law.Roles.Count).Where(r => !law.Writes(r)).ToArray();
        if (inputs.Length < 2) return null;

        // Check linear balance Σ w_i * inputs[i] = sum + radix * carry with w_i = +1 (or ±1).
        var w = new int[inputs.Length + 2];
        var reads = new int[w.Length];
        var writes = new int[w.Length];
        var relShapes = new Shape[w.Length];
        for (var i = 0; i < inputs.Length; i++)
        {
            w[i] = 1; reads[i] = inputs[i]; writes[i] = -1; relShapes[i] = law.Roles[inputs[i]];
        }
        w[^2] = -1; reads[^2] = -1; writes[^2] = sumR; relShapes[^2] = law.Roles[sumR];
        w[^1] = -radix; reads[^1] = -1; writes[^1] = carryR; relShapes[^1] = law.Roles[carryR];

        var exceptions = new List<(int, int)>();
        for (var before = 0; before < law.StateCount && exceptions.Count <= maxExceptions; before++)
        {
            var bt = law.Tuple(before);
            var total = 0;
            for (var i = 0; i < inputs.Length; i++) total += bt[inputs[i]];
            var expSum = total % radix;
            var expCarry = total / radix;
            var actualIdx = law.Afters(before)[0];
            var at = law.Tuple(actualIdx);
            var ok = at[sumR] == expSum && at[carryR] == expCarry;
            for (var i = 0; i < inputs.Length && ok; i++) if (at[inputs[i]] != bt[inputs[i]]) ok = false;
            if (!ok) exceptions.Add((before, actualIdx));
        }
        if (exceptions.Count > maxExceptions) return null;

        var exact = exceptions.Count == 0;
        if (exact)
        {
            var cand = Operations.Bind(Operations.Balance(relShapes, w), law.Roles, reads, writes);
            exact = cand.SameTable(law);
            if (!exact) return null;
        }
        var compBits = BalanceDescriptionBits(w, 0, false, exceptions.Count, law.StateCount);
        return new BalanceFit(w, reads, writes, 0, 0, -1, [.. exceptions], law.BitCount, compBits, exact);
    }

    private static BalanceFit? TryRecognizeCarryOrBorrowBit(
        TransitionLaw law,
        int[] inputs,
        int bitOut,
        int radix,
        int maxExceptions)
    {
        // Try carry: sum(inputs) = freeDigit + radix * bitOut (all +1)
        // AND borrow: freeDigit + sub + bin = min + radix * bitOut -> weights (-1, +1, +1, -radix, +1) -> min - sub - bin + radix*bitOut = freeDigit
        foreach (var isBorrow in new[] { false, true })
        {
            if (isBorrow && inputs.Length != 3) continue;
            var exceptions = new List<(int, int)>();
            for (var before = 0; before < law.StateCount && exceptions.Count <= maxExceptions; before++)
            {
                var bt = law.Tuple(before);
                int expBit;
                if (!isBorrow)
                {
                    var total = 0;
                    for (var i = 0; i < inputs.Length; i++) total += bt[inputs[i]];
                    expBit = total / radix;
                }
                else
                {
                    var diff = bt[inputs[0]] - bt[inputs[1]] - bt[inputs[2]];
                    expBit = diff < 0 ? 1 : 0;
                }
                var actualIdx = law.Afters(before)[0];
                var at = law.Tuple(actualIdx);
                var ok = at[bitOut] == expBit;
                for (var i = 0; i < inputs.Length && ok; i++) if (at[inputs[i]] != bt[inputs[i]]) ok = false;
                if (!ok) exceptions.Add((before, actualIdx));
            }
            if (exceptions.Count <= maxExceptions)
            {
                int[] weights = !isBorrow
                    ? [.. Enumerable.Repeat(1, inputs.Length), -radix, -1]
                    : [-1, 1, 1, -radix, 1];
                int[] reads = [.. inputs, -1, -1];
                int[] writes = [.. Enumerable.Repeat(-1, inputs.Length), bitOut, -1];
                var compBits = BalanceDescriptionBits(weights, radix - 1, false, exceptions.Count, law.StateCount);
                return new BalanceFit(weights, reads, writes, radix - 1, isBorrow ? +1 : -1, -1, [.. exceptions], law.BitCount, compBits, exceptions.Count == 0);
            }
        }
        return null;
    }

    private static int BalanceDescriptionBits(int[] weights, int freeCap, bool hasBlocked, int exceptions, int stateCount)
    {
        var bits = 4 + GammaBits(weights.Length);
        foreach (var w in weights) bits += 1 + GammaBits(w < 0 ? -w : w);
        if (freeCap > 0) bits += 2 + GammaBits(freeCap);
        if (hasBlocked) bits += 2;
        if (exceptions > 0)
        {
            var idxBits = stateCount <= 1 ? 1 : 32 - BitOperations.LeadingZeroCount((uint)(stateCount - 1));
            bits += GammaBits(exceptions) + exceptions * (2 * idxBits);
        }
        return bits;
    }

    private static int ProductDescriptionBits(int roles, bool hasOverflow, int exceptions, int stateCount)
    {
        var bits = 6 + GammaBits(roles) + (hasOverflow ? 2 : 0);
        if (exceptions > 0)
        {
            var idxBits = stateCount <= 1 ? 1 : 32 - BitOperations.LeadingZeroCount((uint)(stateCount - 1));
            bits += GammaBits(exceptions) + exceptions * (2 * idxBits);
        }
        return bits;
    }

    private static int GammaBits(int n)
    {
        if (n < 1) return 1;
        var k = 31 - BitOperations.LeadingZeroCount((uint)n);
        return 2 * k + 1;
    }

    private static int Gcd(int a, int b)
    {
        if (a < 0) a = -a;
        if (b < 0) b = -b;
        while (b != 0) { var t = a % b; a = b; b = t; }
        return a;
    }
}
