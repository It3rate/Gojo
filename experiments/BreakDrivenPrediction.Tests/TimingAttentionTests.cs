using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction.Tests;

public sealed class TimingAttentionTests
{
    [Fact]
    public void OrdinaryIssueConstructsOnlyRequestedRungWhileLegacyRetainsAllRungs()
    {
        var source = S.Create(0, new Carrier(), 1);
        var attentive = T.CreateAttentive(S.Port(source));
        var eager = T.Create(S.Port(source));
        var packet = Packet(attentive, 0, 12, 5, 10);
        var selective = Observe(ref attentive, packet);
        var legacy = Observe(ref eager, packet);

        Assert.True(T.IsAttentive(attentive));
        Assert.False(T.IsAttentive(eager));
        Assert.Single(T.IssuedRows(selective));
        Assert.Equal(3, T.IssuedRows(legacy).Length);
        Assert.Equal(3, Width(T.SelectedRung(T.Pending(attentive))!));
        Assert.Equal(Predicted(eager), Predicted(attentive));
        Assert.Empty(T.DiagnosticRows(T.Pending(attentive)));
        Assert.Equal(1, T.Phase(attentive).Ticks);
        Assert.Empty(T.AssessedRows(selective));
    }

    [Fact]
    public void FourthEligibleIssueSharesOneForecastWithFinerProbeAndOneCredit()
    {
        var root = Create();
        var first = Observe(ref root, Packet(root, 0, 10, 5, 10));
        Assert.Single(T.IssuedRows(first));
        for (var eventAt = 15L; eventAt <= 25; eventAt += 5)
        {
            var assessment = Observe(ref root, Packet(root, eventAt - 5, eventAt, eventAt));
            Assert.Equal(eventAt == 25 ? 2 : 1, T.IssuedRows(assessment).Length);
        }
        Assert.Equal(0, T.Phase(root).Ticks);
        var probe = Assert.Single(T.DiagnosticRows(T.Pending(root)));
        var selected = T.SelectedRung(T.Pending(root))!;
        Assert.Equal(3, Width(selected));
        Assert.Equal(1, Width(probe));
        Assert.Same(Source(selected), Source(probe));
        var before = T.Support(root).Value;
        var graded = Observe(ref root, Packet(root, 25, 30, 30));
        Assert.True(T.Successful(graded));
        Assert.Equal(2, T.AssessedRows(graded).Length);
        Assert.Equal(before + 1, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
        Assert.Single(T.IssuedRows(graded));
    }

    [Fact]
    public void ResolvedTimingMissRequestsNextFinerProbeWithoutWaitingForPeriod()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        var missed = Observe(ref root, Packet(root, 10, 19, 19));
        Assert.True(T.Settled(missed));
        Assert.False(T.Successful(missed));
        Assert.False(T.IsDormant(root));
        Assert.Equal(2, T.Phase(root).Ticks);
        Assert.Equal(2, T.IssuedRows(missed).Length);
        Assert.Single(T.DiagnosticRows(T.Pending(root)));

        var due = Predicted(root);
        var hit = Observe(ref root, Packet(root, 19, due, due));
        Assert.True(T.Successful(hit));
        Assert.Single(T.IssuedRows(hit));
        Assert.Empty(T.DiagnosticRows(T.Pending(root)));
    }

    [Fact]
    public void ZeroDemandPreservesFrozenPromiseThenSuppressesNewReadConstruction()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 12, 5, 10));
        var frozen = T.Pending(root);
        T.Demand(root).Set(0);
        var graded = Observe(ref root, Packet(root, 12, 16, 16));
        Assert.Same(frozen, C.Parts(graded)[0]);
        Assert.True(T.Successful(graded));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.Single(T.AssessedRows(graded));
        Assert.Empty(T.IssuedRows(graded));
        Assert.IsType<Placeholder>(T.Pending(root));
        Assert.Equal(1, T.Phase(root).Ticks);

        foreach (var eventAt in new long[] { 21, 26 })
        {
            var withheld = Observe(ref root, Packet(root, T.Available(root).Ticks, eventAt, eventAt));
            Assert.Empty(T.IssuedRows(withheld));
            Assert.Empty(T.AssessedRows(withheld));
            Assert.False(T.Settled(withheld));
            Assert.Equal(1, T.Phase(root).Ticks);
        }
        Assert.Equal(new long[] { 21, 26 }, Events(root));
        T.Demand(root).Set(12);
        var resumed = Observe(ref root, Packet(root, 26, 31, 31));
        Assert.Single(T.IssuedRows(resumed));
        Assert.Equal(12, Width(T.SelectedRung(T.Pending(root))!));
        Assert.Equal(2, T.Phase(root).Ticks);
    }

    [Fact]
    public void InitiallyWithdrawnDemandDoesNotSpendIssuePhaseOrInventAZeroWidthPromise()
    {
        var root = Create(demand: 0);
        var withheld = Observe(ref root, Packet(root, 0, 12, 5, 10));
        Assert.Empty(T.IssuedRows(withheld));
        Assert.Equal(0, T.Phase(root).Ticks);
        Assert.IsType<Placeholder>(T.Pending(root));
        Assert.Equal(new long[] { 5, 10 }, Events(root));
        T.Demand(root).Set(3);
        var resumed = Observe(ref root, Packet(root, 12, 24, 15, 20));
        Assert.Single(T.IssuedRows(resumed));
        Assert.False(T.Settled(resumed));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.Equal(25, Predicted(root));
    }

    [Fact]
    public void UnpaidDemandDrawIsExposedAsUnresolvedInsteadOfResolvedWithdrawal()
    {
        var root = Create(demand: 0);
        T.Demand(root).Remove(1);
        var blocked = Observe(ref root, Packet(root, 0, 12, 5, 10));
        Assert.Equal(0UL, T.Demand(root).Value);
        Assert.Equal(-1, T.Demand(root).Tension);
        Assert.IsType<Relation>(T.Pending(root));
        Assert.Null(T.SelectedRung(T.Pending(root)));
        Assert.Empty(T.IssuedRows(blocked));
        Assert.Equal(0, T.Phase(root).Ticks);
        T.Demand(root).Add(1);
        var quiet = Observe(ref root, Packet(root, 12, 24, 15, 20));
        Assert.Equal(0, T.Demand(root).Tension);
        Assert.IsType<Placeholder>(T.Pending(root));
        Assert.Empty(T.IssuedRows(quiet));
    }

    [Fact]
    public void AbsenceResponseReachesZeroBeforeGatingAndDormancyRetainsCadence()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 12, 5, 10));
        Observe(ref root, Packet(root, 12, 16, 16));
        Assert.Equal(1UL, T.Support(root).Value);
        var trace = Events(root);
        var phase = T.Phase(root).Ticks;
        var exhausted = Observe(ref root, Packet(root, 16, 24));
        Assert.True(T.Settled(exhausted));
        Assert.False(T.Successful(exhausted));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.True(T.IsDormant(root));
        Assert.Empty(T.IssuedRows(exhausted));
        Assert.Empty(T.AssessedRows(exhausted));
        Assert.Equal(trace, Events(root));

        var later = Observe(ref root, Packet(root, 24, 36));
        Assert.False(T.Settled(later));
        Assert.True(T.IsDormant(root));
        Assert.Empty(T.IssuedRows(later));
        Assert.Equal(trace, Events(root));
        Assert.Equal(phase, T.Phase(root).Ticks);
    }

    [Fact]
    public void InspectionWithoutAReportDoesNotAdvancePhaseOrEraseFrozenReads()
    {
        var root = Create(probeEvery: 1);
        var assessment = Observe(ref root, Packet(root, 0, 12, 5, 10));
        var before = GojoNumericFormat.Encode(root);
        var frozen = T.Pending(root);
        for (var i = 0; i < 4; i++)
        {
            Assert.True(T.IsAttentive(root));
            Assert.False(T.IsDormant(root));
            T.Phase(root); T.ProbeSpan(root); T.SelectedRung(frozen);
            T.DiagnosticRows(frozen); T.IssuedRows(assessment); T.AssessedRows(assessment);
        }
        Assert.Equal(before, GojoNumericFormat.Encode(root));
        Assert.Same(frozen, T.Pending(root));
    }

    [Fact]
    public void SelectiveEvidenceRetainsBoundedStructureAcrossRepeatedProbeCycles()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        var sizes = new List<long>();
        for (var issue = 2; issue <= 28; issue++)
        {
            var due = Predicted(root);
            Observe(ref root, Packet(root, T.Available(root).Ticks, due, due));
            Assert.Equal(2, T.Events(root).Length);
            Assert.InRange(T.Phase(root).Ticks, 0, 3);
            // Compare the same phase and selected/probed shape, not a tiny
            // ordinary row with a deliberately larger two-row probe issue.
            if (issue is 12 or 20 or 28) sizes.Add(GraphSize(root));
        }
        Assert.All(sizes.Skip(1), size => Assert.InRange(size, 1, sizes[0] + 128));
    }

    private static Relation Create(long demand = 3, long probeEvery = 4) =>
        T.CreateAttentive(S.Port(S.Create(0, new Carrier(), 1)), demand, probeEvery);

    private static Relation Packet(Relation root, long start, long end, params long[] events)
    {
        var port = T.Port(root);
        var fields = C.Parts(port);
        var clock = (Carrier)fields[0];
        return C.G(port, Relation.Segment(clock, start, end), new CarrierLocation(clock, end),
            C.G(events.Select(at => (IElement)Relation.Segment(clock, at, at)).ToArray()),
            new CarrierLocation((Carrier)fields[1], 0), C.G());
    }

    private static Relation Observe(ref Relation root, Relation packet)
    {
        Assert.True(T.TryObserve(root, packet, out var next, out var assessment));
        root = next;
        return assessment;
    }

    private static long Predicted(Relation root) =>
        ((CarrierLocation)C.Parts((Relation)C.Parts((Relation)T.Pending(root))[0])[1]).Ticks;
    private static long Width(Relation row) => C.Length((Relation)C.Parts(row)[1]).Ticks;
    private static long[] Events(Relation root) => T.Events(root).Cast<CarrierLocation>().Select(at => at.Ticks).ToArray();
    private static IElement Source(Relation row)
    {
        Assert.True(StructuralLadderSurface.TryReadRung((Relation)C.Parts(row)[3], out var source,
            out _, out _, out _, out _, out _, out _, out _));
        return source;
    }
    private static long GraphSize(Relation root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        return (long)graph.Carriers.Length + graph.Relations.Length + graph.TickRefs.Length +
            graph.CarrierLocations.Length + graph.UnitShapes.Length + graph.Placeholders.Length;
    }
}
