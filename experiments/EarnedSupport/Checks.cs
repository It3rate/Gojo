using System.Text.Json;
using Gojo.Core;
using M = NegotiatedPorts.MarketProbe;
using N = ConnectionAssembly.NativeMath;
using S = EarnedSupport.SupportNetwork;

namespace EarnedSupport;

/// <summary>Checks the supplied support law's causal and accounting contracts,
/// including unresolved observations. None requires an ecological winner.</summary>
internal static class Checks
{
    public static int Run()
    {
        LaterOutcomeAndDemand();
        HarmIsGradual();
        OnceOnlySettlement();
        ExactResourceAccounting();
        ZeroGainControl();
        NativeContinuation();
        FinishedWitnessRelease();
        FiniteResolutionRemainders();
        SuppliersReceivePurchases();
        return 9;
    }

    private static void LaterOutcomeAndDemand()
    {
        var root = S.Create();
        Observe(ref root, 100, true, false);
        Observe(ref root, 103, true, false);
        Require(Total(S.Issued(root)) == 0 && S.States(root).All(state => Total(S.Trace(state)) == 0),
            "Agreeing and paying for a service cannot earn support before its subsequent outcome.");
        var frame = Observe(ref root, 106, true, false);
        var outcome = Outcomes(frame)[0];
        Require(Number(outcome, 3) == 4 && Total(S.Trace(S.States(root)[0])) == 32,
            "A later useful demanded outcome supplies the connected support trace.");
        var supportFrame = (Relation)M.Parts(root)[4];
        var supportRow = (Relation)M.Parts((Relation)M.Parts(supportFrame)[1])[0];
        Require(ReferenceEquals(M.Parts(supportRow)[0], S.States(root)[0]) &&
            ReferenceEquals(M.Parts(supportRow)[1], outcome),
            "The support witness physically connects its interpreted state to the actual later outcome.");
        Require(Total(S.Trace(S.States(root)[1])) == 0,
            "An inactive consumer cannot receive evidence merely because another consumer benefits.");

        var before = SupportValues(root);
        frame = Observe(ref root, null, true, false);
        Require(Number(Outcomes(frame)[0], 8) == 0 && before.SequenceEqual(SupportValues(root)),
            "A missing immediate outcome neither pays retained support nor decays or clears any support remainder.");
        before = SupportValues(root);
        Observe(ref root, 112, true, false);
        Require(before.SequenceEqual(SupportValues(root)),
            "A later known input cannot substitute for the missing immediately promised observation.");

        // Withdrawal is prospective: the previous demanded service may still
        // earn support on this arrival. The following outcome has no demand.
        Observe(ref root, 115, false, false);
        before = SupportValues(root);
        Observe(ref root, 118, false, false);
        Require(before.SequenceEqual(SupportValues(root)),
            "A resolved but explicitly non-demanded outcome leaves support and issuance unchanged.");
    }

    private static void HarmIsGradual()
    {
        var root = S.Create();
        foreach (var input in new long[] { 100, 103, 106, 109, 112 }) Observe(ref root, input, true, false);
        var state = S.States(root)[0];
        var prior = Total(S.Trace(state));
        var frame = Observe(ref root, 109, true, false);
        Require(Number(Outcomes(frame)[0], 3) < 0 && Total(S.Trace(state)) < prior && Total(S.Trace(state)) > 0,
            "An opposite subsequent outcome lowers accumulated support without erasing several useful observations at once.");
        Require(M.Pass(S.Conservation(root)), "Harm changes future backing without an unaccounted currency destruction.");
    }

    private static void OnceOnlySettlement()
    {
        var root = S.Create();
        Observe(ref root, 100);
        Observe(ref root, 103);
        var stale = root;
        var frame = Observe(ref root, 106);
        var settled = Canonical(root);
        Require(!S.TrySettle(root, frame, out _, out _) && Canonical(root) == settled,
            "A second settlement of the identical frame cannot mint, smooth, or otherwise mutate current support.");
        Require(!S.TrySettle(root, N.Group(M.Parts(frame)), out _, out _) && Canonical(root) == settled,
            "A lookalike copied frame is not the attached outcome merely because its numeric clock agrees.");
        Require(!S.TryObserve(stale, 109, out _, out _, out _) && Canonical(root) == settled,
            "A stale world envelope cannot run another arrival against a moved operational clock.");
        Require(!S.TrySettle(stale, frame, out _, out _) && Canonical(root) == settled,
            "A newer frame cannot be applied to a stale world envelope.");
        Require(Total(S.LastSettledVersion(root)) == M.Version(S.Market(root)),
            "The native settlement cursor records the actual admitted market version.");
    }

    private static void ExactResourceAccounting()
    {
        var root = S.Create(3, 2);
        foreach (var input in Inputs())
        {
            Observe(ref root, input);
            var market = S.Market(root);
            var owners = new[] { M.Reservoir(market), M.Spent(market) }
                .Concat(M.Consumers(market).Select(M.Wallet))
                .Concat(M.Producers(market).Select(M.Reserve)).ToArray();
            Require(owners.Sum(Total) == 4096 + Total(S.Issued(root)) &&
                M.Pass(S.Conservation(root)) && M.Pass(M.Conservation(market)),
                "Every actual money owner balances against initial supply plus explicit cumulative issuance, including spent resources.");
        }
        Require(Total(S.Issued(root)) > 0, "The accounting fixture exercises actual earned issuance rather than a vacuous zero case.");
    }

    private static void ZeroGainControl()
    {
        var root = S.Create(0, 1);
        var control = M.Create();
        foreach (var input in Inputs())
        {
            Observe(ref root, input);
            Require(M.TryObserve(control, input, true, true, 5, 8, out control, out _),
                "The unmodified control admits the same external opportunity.");
            Require(Total(S.Issued(root)) == 0 &&
                Canonical(ReleaseMarket(S.Market(root))) == Canonical(ReleaseMarket(control)),
                "Zero issuance gain preserves the complete old market continuation, including topology, promises, money and attachments.");
        }
    }

    private static void NativeContinuation()
    {
        var root = S.Create(1, 3);
        foreach (var input in new long?[] { 100, 103, 106, 109, 112, null }) Observe(ref root, input);
        root = S.ReleaseFrame(root);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone),
            "Numeric transport preserves support, residue, settlement cursor and current unresolved market obligations.");
        foreach (var input in new long?[] { 118, 121, 124, 121, 118, null, 115, 115, 119, 125 })
        {
            Observe(ref root, input);
            Observe(ref clone, input);
            root = S.ReleaseFrame(root);
            clone = S.ReleaseFrame(clone);
            Require(Canonical(root) == Canonical(clone),
                "Restored structure produces the same subsequent native state after unknowns, reversals and returning paid service.");
        }
    }

    private static void FinishedWitnessRelease()
    {
        var retained = S.Create();
        var released = S.Create();
        var marketOperational = M.Operational(S.Market(released));
        var stateIdentities = S.States(released);
        foreach (var input in Inputs())
        {
            Observe(ref retained, input);
            Observe(ref released, input);
            var pending = M.Pending(S.Market(released)).Select(issue => issue.Id).ToArray();
            var oldWitness = M.Parts(released)[4] as Relation;
            released = S.ReleaseFrame(released);
            Require(ReferenceEquals(marketOperational, M.Operational(S.Market(released))) &&
                stateIdentities.Zip(S.States(released)).All(pair => ReferenceEquals(pair.First, pair.Second)),
                "Releasing finished evidence retains the actual mutable market and support structures.");
            var snapshot = GojoEncodedSnapshot.CaptureGraph(released);
            Require((oldWitness is null || !snapshot.Relations.Any(row => row.Id == oldWitness.Id)) &&
                pending.All(id => snapshot.Relations.Any(row => row.Id == id)),
                "Finished support evidence leaves the retained graph while obligations for future outcomes remain.");
            Require(Canonical(S.ReleaseFrame(retained)) == Canonical(released),
                "Discarding finished diagnostic witnesses cannot change future structural behavior.");
        }
    }

    private static void FiniteResolutionRemainders()
    {
        var root = S.Create(1, 3);
        foreach (var input in new long[] { 100, 103, 106, 109, 112 }) Observe(ref root, input, true, false);
        var state = S.States(root)[0];
        Require(Total(S.Trace(state)) == 84 && Total(S.TraceRemainder(state)) == 4 &&
            Total(S.PayoutRemainder(state)) == 176 && Total(S.Issued(root)) == 0,
            "Sub-tick smoother and payout participation survives across repeated coarse reads instead of being discarded.");
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(S.ReleaseFrame(root))).Single();
        Observe(ref root, 115, true, false);
        Observe(ref restored, 115, true, false);
        state = S.States(root)[0];
        Require(Total(S.Trace(state)) == 106 && Total(S.TraceRemainder(state)) == 0 &&
            Total(S.PayoutRemainder(state)) == 90 && Total(S.Issued(root)) == 1 &&
            Canonical(S.ReleaseFrame(root)) == Canonical(S.ReleaseFrame(restored)),
            "Retained native remainders contribute to the later whole payout and survive transport.");

        var negative = S.Create(0, 1);
        Observe(ref negative, 100, true, false);
        Observe(ref negative, 103, true, false);
        var negativeState = S.States(negative)[0];
        // Authored signed support perturbation makes the finite division edge
        // observable independently of which offer wins a larger experiment.
        S.Trace(negativeState).Set(-1);
        Observe(ref negative, 100, true, false);
        Require(Total(S.Trace(negativeState)) < 0 && Total(S.TraceRemainder(negativeState)) < 0 &&
            Total(S.Issued(negative)) == 0,
            "Signed harmful evidence retains negative division residue and cannot become positive spendable support.");
    }

    private static void SuppliersReceivePurchases()
    {
        var root = S.Create(4, 1);
        Observe(ref root, 100, true, false, 0, 0);
        Observe(ref root, 103, true, false, 0, 0);
        var market = S.Market(root);
        var consumer = M.Consumers(market)[0];
        var supplier = M.Producers(market)[0];
        Require(Total(M.Wallet(consumer)) == 4 && Total(M.Reserve(supplier)) == 6 &&
            Total(M.Spent(market)) == 2 && Total(S.Issued(root)) == 0,
            "A first purchase drains contact and price from its consumer, pays the supplier, and drains its operating cost.");
        var frame = Observe(ref root, 106, true, false, 0, 0);
        market = S.Market(root);
        Require(Total(S.Issued(root)) == 2 && Total(M.Reservoir(market)) == 4070 &&
            Total(M.Wallet(consumer)) == 1 && Total(M.Reserve(supplier)) == 8 &&
            Total(M.Spent(market)) == 3,
            "Earned support enters the reservoir once; suppliers receive only actual purchased fees, not a duplicate copy of the full downstream reward.");
        Require(M.Pass(S.Conservation(root)), "Supplier payment and external backing obey separate visible accounting paths.");
        Require(M.Pass((Relation)M.Parts(frame)[5]),
            "The captured market accounting claim remains true after later issuance moves the live expected-total site.");
        Observe(ref root, 109, true, false, 0, 0);
        Require(M.Pass((Relation)M.Parts(frame)[5]) && M.Pass(S.Conservation(root)),
            "Further outcomes and new issuance cannot rewrite the earlier pre-issuance accounting witness.");
    }

    private static long?[] Inputs() => [100, 103, 106, 109, 112, 115, null, 121, 124, 127, 124, 121, 121, 121, 125];
    private static Relation Observe(ref Relation root, long? input, bool demand0 = true, bool demand1 = true,
        long funding0 = 5, long funding1 = 8)
    {
        Require(S.TryObserve(root, input, demand0, demand1, funding0, funding1, out var next, out var frame, out _),
            "Current native structures admit the supplied next observation.");
        root = next;
        return frame;
    }
    private static Relation[] Outcomes(Relation frame) => M.Parts((Relation)M.Parts(frame)[2]).Cast<Relation>().ToArray();
    private static long Number(Relation row, int index) => ((CarrierLocation)M.Parts(row)[index]).Ticks;
    private static long Total(Carrier value) => S.SignedValue(value).Ticks;
    private static long[] SupportValues(Relation root) => S.States(root).SelectMany(state => new[]
        { Total(S.Trace(state)), Total(S.TraceRemainder(state)), Total(S.PayoutRemainder(state)) })
        .Append(Total(S.Issued(root))).ToArray();
    private static Relation ReleaseMarket(Relation root) => N.Group(M.Operational(root), M.Parts(root)[1], new Placeholder(), M.Parts(root)[3]);

    internal static string Canonical(Relation root)
    {
        var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
        return JsonSerializer.Serialize(snapshot with
        {
            Carriers = snapshot.Carriers.Select(row => row with { Id = 0 }).ToArray(),
            TickRefs = snapshot.TickRefs.Select(row => row with { Id = 0 }).ToArray(),
            Relations = snapshot.Relations.Select(row => row with { Id = 0 }).ToArray(),
            UnitShapes = snapshot.UnitShapes.Select(row => row with { Id = 0 }).ToArray()
        });
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Earned-support check failed: " + message);
    }
}
