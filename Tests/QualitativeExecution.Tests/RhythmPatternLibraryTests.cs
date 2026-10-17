using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class RhythmPatternLibraryTests
{
    [Fact]
    public void CatalogSourceRetainsActualPortsPhaseWordsAndSharedUnmeasuredReaches()
    {
        var library = new RhythmPatternLibrary(); Assert.Equal(5, library.Entries.Count);
        foreach (var entry in library.Entries)
        {
            Assert.InRange(entry.PeriodRounds, 2, 32); Assert.InRange(entry.Fixture.Components.Count, 2, 4);
            Assert.Equal(entry.Components.Count, E(entry.Layers).Length);
            var ports = E(entry.Fixture.Network.Ports);
            foreach (var pair in entry.Components.Zip(ports))
            {
                Assert.Same(pair.Second, pair.First.Port); Assert.Same(pair.First.Port, pair.First.Surface.Left);
                Assert.Same(pair.First.Words, pair.First.Surface.Right); Assert.Contains(pair.First.Surface, E(entry.Layers));
                var clock = entry.Fixture.Components.Single(component => ReferenceEquals(component.Surface, pair.First.Port.Right));
                Assert.Equal(E(clock.Domain).Length, E(pair.First.Words).Length);
                foreach (var phaseWord in E(clock.Domain).Zip(E(pair.First.Words)))
                {
                    var read = FiniteLaw.Evaluate(entry.Fixture.OutputLaw, S(phaseWord.First), S(pair.First.Port));
                    Assert.True(FiniteLaw.TryReadResult(read, out var support, out var pending, out _));
                    Assert.Same(phaseWord.Second, Assert.Single(E(support))); Assert.Empty(E(pending));
                    foreach (var move in E((Relation)phaseWord.Second).Cast<Relation>())
                    {
                        Assert.True(library.Frame.Direction(move, out var axis, out _, out var reach));
                        Assert.Same(ReferenceEquals(axis, library.Frame.Horizontal) ? library.HorizontalReach : library.VerticalReach, reach);
                    }
                }
            }
            Assert.True(Contains(library.Surface, entry.Surface)); Assert.Empty(E(entry.Unresolved));
        }
        AssertNoNumbers(library.Surface);
    }

    [Fact]
    public void DiagnosticPeriodAgreesWithActualNativeControlReturnAndQualitativePeriodLaw()
    {
        var library = new RhythmPatternLibrary();
        foreach (var entry in library.Entries)
        {
            var trace = entry.Fixture.Run(entry.PeriodRounds);
            Assert.True(entry.Fixture.TryReadTrace(trace, out var initial, out _, out var steps, out _, out _, out var pending));
            Assert.Empty(E(pending)); var firstReturn = 0; var round = 0;
            foreach (var value in E(steps).Cast<Relation>())
            {
                round++; Assert.True(entry.Fixture.Network.TryReadStep(value, out _, out _, out var after, out _, out _, out _));
                if (firstReturn == 0 && entry.Fixture.SamePhaseConfiguration(initial, after)) firstReturn = round;
            }
            Assert.Equal(entry.PeriodRounds, firstReturn);
            var rules = new FriezeStructuralRules(entry.Fixture, Slots(64)); var read = rules.Evaluate(trace);
            Assert.True(rules.TryReadResult(read, out var passed, out var violations, out var unresolved));
            Assert.NotEmpty(E(passed)); Assert.Empty(E(violations)); Assert.Empty(E(unresolved));
            Assert.True(rules.TryReadPeriod(read, out var samples, out var displacement)); Assert.Equal(entry.PeriodRounds, E(samples).Length);
            Assert.True(library.Frame.TryReadResult(displacement, out var residual, out _, out _));
            Assert.Equal(entry.PeriodRounds, E(residual).Length);
            Assert.All(E(residual).Cast<Relation>(), move => Assert.Same(library.Frame.Right, move.Left)); AssertNoNumbers(read);
        }
    }

    [Fact]
    public void SharedCursorAddsSlowAndFastDrivesAtOneSampleWhileActualExecutionRemainsOrdered()
    {
        var entry = new RhythmPatternLibrary().Entries[0]; var fixture = entry.Fixture; var trace = fixture.Run(2);
        var analysis = FriezeDriveAnalysis.Evaluate(fixture, trace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, analysis, out var samples, out var pending)); Assert.Empty(E(pending));
        Assert.Equal(2, E(samples).Length);
        Assert.True(FriezeDriveAnalysis.TryReadSample(fixture, (Relation)E(samples)[0], out var sample, out var route,
            out var apps, out var axes, out var delta, out _, out _));
        Assert.Same(fixture.Network.Surface, route.Left); Assert.Equal(6, E(apps).Length);
        Assert.True(FriezeFixture.TryReadSample(sample, out var cause, out _, out _, out _, out _, out _));
        Assert.True(fixture.Network.TryReadStep((Relation)cause, out _, out _, out _, out var accepted, out _, out _));
        Assert.Equal(3, E(accepted).Length);
        var phaseOrder = E(apps).Cast<Relation>().Select(app => ((Relation)app.Left).Left).ToArray();
        Assert.Same(E(accepted)[0], phaseOrder[0]); Assert.All(phaseOrder.Skip(1).Take(4), receipt => Assert.Same(E(accepted)[1], receipt));
        Assert.Same(E(accepted)[2], phaseOrder[^1]);
        Assert.True(FriezeDriveAnalysis.TryReadContribution(fixture, (Relation)E(axes)[1], out var axis, out var vertical, out var read));
        Assert.Same(fixture.Frame.Vertical, axis); Assert.Equal(5, E(vertical).Length);
        Assert.True(fixture.Frame.TryReadResult(read, out var retained, out _, out _)); Assert.Equal(5, E(retained).Length);
        Assert.True(fixture.Frame.TryReadResult(delta, out var combined, out _, out _)); Assert.Equal(6, E(combined).Length);
        AssertNoNumbers(analysis);
    }

    [Fact]
    public void IndependentTraceCursorsDoNotBecomeACombinedCursorBySharingWordsOrFrame()
    {
        var library = new RhythmPatternLibrary(); var entry = library.Entries[0];
        var separate = entry.Components.Select(layer => FriezeFixture.Sequence(library.Frame, layer.Words, layer.Name)).ToArray();
        var correspondence = new Relation(entry.Layers, S(separate.Select(fixture => (IElement)fixture.Surface).ToArray()));
        var finalParts = new List<IElement>();
        foreach (var cursor in separate)
        {
            Assert.NotSame(entry.Fixture.Network, cursor.Network); Assert.Single(E(cursor.Network.Ports)); Assert.Empty(E(cursor.Network.Routes));
            Assert.True(cursor.TryReadTrace(cursor.Run(entry.PeriodRounds), out _, out _, out _, out _, out var samples, out _));
            Assert.True(FriezeFixture.TryReadSample((Relation)E(samples)[^1], out _, out _, out _, out var after, out _, out _));
            finalParts.AddRange(E(after));
        }
        Assert.Equal(entry.PeriodRounds, finalParts.Count); // Only the separate drift cursor has a nonzero endpoint.
        var combinedTrace = entry.Fixture.Run(entry.PeriodRounds);
        Assert.True(entry.Fixture.TryReadTrace(combinedTrace, out _, out _, out _, out _, out var combinedSamples, out _));
        Assert.True(FriezeFixture.TryReadSample((Relation)E(combinedSamples)[^1], out _, out _, out _, out var combinedEnd, out _, out _));
        Assert.All(E(combinedEnd).Zip(finalParts), pair => Assert.Same(pair.First, pair.Second));
        // The optional sum is an observation connected to the correspondence;
        // it does not fuse sample ports, histories or the independent cursors.
        var sum = library.Frame.Reduce(S(), S(finalParts.ToArray())); AssertNoNumbers(new Relation(correspondence, sum));
    }

    [Fact]
    public void RelativeFastPhaseEditRetainsNetworkLawsWordsAndItsActualStartingBindings()
    {
        var library = new RhythmPatternLibrary(); var original = library.Entries[0]; var shifted = library.Entries[1];
        Assert.Same(original, shifted.Parent); Assert.Same(original.Fixture.Network, shifted.Fixture.Network);
        Assert.Same(original.Fixture.OutputLaw, shifted.Fixture.OutputLaw); Assert.Same(original.Fixture.Sampling, shifted.Fixture.Sampling);
        Assert.NotSame(original.Fixture.Starts, shifted.Fixture.Starts);
        Assert.All(original.Components.Zip(shifted.Components), pair => Assert.Same(pair.First.Surface, pair.Second.Surface));
        Assert.Same(shifted.EditSurface, Assert.Single(E(library.Edits)));
        var change = Assert.IsType<Relation>(Assert.IsType<Relation>(shifted.EditSurface).Right);
        Assert.Same(original.Fixture.Starts, change.Left); Assert.Same(shifted.Fixture.Starts, change.Right);
        Assert.Equal(5, FirstVerticalTerms(original)); Assert.Equal(3, FirstVerticalTerms(shifted));
        AssertNoNumbers(library.Surface);
    }

    [Fact]
    public void OnsetsRetainTwoActualPhaseLegsNativeEmissionReadsAndDeclaredAnchorMembership()
    {
        var library = new RhythmPatternLibrary();
        foreach (var entry in library.Entries.Skip(3))
        {
            Assert.Equal(E(entry.Onsets).Length * 2, entry.PeriodRounds);
            Assert.Equal(entry.PeriodRounds, E(entry.Generation).Length); Assert.Equal(E(entry.Onsets).Length, E(entry.OnsetPhases).Length);
            var pulsePort = entry.Components[1].Port;
            Assert.True(entry.Anchors.Left is Relation); Assert.True(entry.Anchors.Right is Relation);
            var primary = E((Relation)entry.Anchors.Left); var secondary = E((Relation)entry.Anchors.Right);
            Assert.Equal(4, primary.Length); Assert.Equal(E(entry.Onsets).Length, primary.Length + secondary.Length);
            var offAnchorHits = 0;
            foreach (var pair in E(entry.Onsets).Cast<Relation>().Zip(E(entry.OnsetPhases).Cast<Relation>()))
            {
                Assert.Same(pair.First.Left, pair.Second.Left); var phases = E((Relation)pair.Second.Right); Assert.Equal(2, phases.Length);
                Assert.Contains(phases[0], primary.Concat(secondary)); Assert.DoesNotContain(phases[1], primary.Concat(secondary));
                if (ReferenceEquals(pair.First.Right, entry.OnsetRoles.Left) && secondary.Any(phase => ReferenceEquals(phase, phases[0]))) offAnchorHits++;
            }
            Assert.Equal(entry.PeriodRounds == 32 ? 0 : 1, offAnchorHits);
            foreach (var item in E(entry.Generation).Cast<Relation>())
            {
                var source = Assert.IsType<Relation>(item.Left); var role = Assert.IsType<Relation>(source.Left);
                Assert.Contains(E(entry.Onsets).Cast<Relation>(), onset => ReferenceEquals(onset.Left, role.Left));
                var proof = Assert.IsType<Relation>(item.Right); var read = Assert.IsType<Relation>(proof.Left); var outputRow = Assert.IsType<Relation>(proof.Right);
                Assert.True(FiniteLaw.TryReadResult(read, out var support, out var residue, out _)); Assert.Empty(E(residue));
                var lawContext = Assert.IsType<Relation>(Assert.IsType<Relation>(read.Left).Left); Assert.Same(entry.EmissionLaw, lawContext.Left);
                var outputInput = Assert.IsType<Relation>(outputRow.Left); Assert.Same(source.Right, outputInput.Left); Assert.Same(pulsePort, outputInput.Right);
                Assert.Same(Assert.Single(E(support)), Assert.Single(E((Relation)outputRow.Right)));
            }
            Assert.True(entry.Fixture.TryReadTrace(entry.Fixture.Run(entry.PeriodRounds), out _, out _, out _, out var apps, out _, out _));
            var hits = E(entry.Onsets).Cast<Relation>().Count(onset => ReferenceEquals(onset.Right, entry.OnsetRoles.Left));
            Assert.Equal(entry.PeriodRounds + 2 * hits, E(apps).Length);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ForeignOnsetOrOpenTailCannotBeReplacedByAConcreteRestOrCertifiedPeriod(bool open)
    {
        var frame = new QualitativeMotionFrame(); var hit = new Placeholder(); var rest = new Placeholder(); var a = new Placeholder(); var b = new Placeholder();
        var onsets = open ? StructuralConstraint.Open(new Relation(a, hit)) : S(new Relation(a, hit), new Relation(b, new Placeholder()));
        var anchors = new Relation(S(a), open ? S() : S(b));
        var entry = RhythmPatternLibrary.FromOnsets(frame, onsets, new Relation(hit, rest), anchors, new Placeholder(), new Placeholder());
        Assert.Same(onsets, entry.Onsets); Assert.NotEmpty(E(entry.Unresolved)); Assert.True(Contains(entry.Surface, onsets));
        var trace = entry.Fixture.Run(entry.PeriodRounds);
        var rules = new FriezeStructuralRules(entry.Fixture, Slots(64)); var read = rules.Evaluate(trace);
        Assert.True(rules.TryReadResult(read, out _, out _, out var pending)); Assert.NotEmpty(E(pending));
        var analysis = FriezeDriveAnalysis.Evaluate(entry.Fixture, trace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(entry.Fixture, analysis, out var samples, out var unknown));
        Assert.NotEmpty(E(samples)); Assert.NotEmpty(E(unknown)); AssertNoNumbers(new Relation(entry.Surface, analysis));
        Assert.True(entry.Fixture.TryReadTrace(trace, out _, out _, out _, out _, out var commits, out _));
        Assert.True(FriezeFixture.TryReadSample((Relation)E(commits)[^1], out _, out _, out _, out var last, out _, out _)); Assert.False(Closed(last));
        if (open)
        {
            Assert.True(StructuralConstraint.TryReadScope(onsets, out _, out _, out var tail));
            Assert.Contains(E(entry.OnsetPhases).Cast<Relation>(), mapping => ReferenceEquals(mapping.Left, tail));
        }
    }

    [Fact]
    public void AnchorPremisesAndOpenWordsRetainTheirOwnDifferentUnresolvedEvidence()
    {
        var frame = new QualitativeMotionFrame(); var hit = new Placeholder(); var rest = new Placeholder(); var slot = new Placeholder(); var foreign = new Placeholder();
        var entry = RhythmPatternLibrary.FromOnsets(frame, S(new Relation(slot, hit)), new Relation(hit, rest),
            new Relation(S(foreign), S(slot)), new Placeholder(), new Placeholder());
        Assert.Contains(foreign, E(entry.Unresolved)); Assert.Empty(E((Relation)entry.Anchors.Left));
        var rules = new FriezeStructuralRules(entry.Fixture, Slots(8));
        Assert.True(rules.TryReadResult(rules.Evaluate(entry.Fixture.Run(2)), out _, out var violations, out var pending));
        Assert.Empty(E(violations)); Assert.Empty(E(pending)); // Geometry cannot repair an unrelated meter premise.

        var h = new Placeholder(); var v = new Placeholder();
        var root = S(S(QualitativeMotionFrame.Move(frame.Right, h)));
        var openWord = StructuralConstraint.Open(QualitativeMotionFrame.Move(frame.Up, v));
        var partial = FriezeFixture.LayeredCycles(frame, root, S(S(openWord)));
        Assert.True(partial.TryReadTrace(partial.Run(1), out _, out _, out _, out var apps, out _, out var missing));
        Assert.NotEmpty(E(missing)); Assert.Contains(E(apps).Cast<Relation>(), app => app.Right is Placeholder); AssertNoNumbers(partial.Surface);
    }

    private static int FirstVerticalTerms(RhythmPatternEntry entry)
    {
        Assert.True(entry.Fixture.TryReadTrace(entry.Fixture.Run(1), out _, out _, out _, out _, out var samples, out _));
        Assert.True(FriezeFixture.TryReadSampleDelta((Relation)Assert.Single(E(samples)), out var delta));
        Assert.True(entry.Fixture.Frame.TryReadResult(delta, out var residual, out _, out var pending)); Assert.Empty(E(pending));
        return E(residual).Cast<Relation>().Count(move => entry.Fixture.Frame.Direction(move, out var axis, out _, out _) && ReferenceEquals(axis, entry.Fixture.Frame.Vertical));
    }
    private static Relation Slots(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static void AssertNoNumbers(IElement source) => Assert.DoesNotContain(Traverse(source), item => item is Carrier or CarrierLocation);
    private static bool Contains(IElement source, IElement target) => Traverse(source).Any(item => ReferenceEquals(item, target));
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.Count != 0)
        {
            var item = pending.Pop(); if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
