using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class RhythmFriezeLibraryTests
{
    [Fact]
    public void CatalogSeparatesTheSameOnsetsFromDifferentAuthoredInterpretations()
    {
        var library = new RhythmFriezeLibrary();
        Assert.Equal(6, library.Entries.Count);
        Assert.Equal(new[] { 26, 35, 41, 26, 32, 34 }, library.Entries.Select(entry => entry.PhaseCount).ToArray());
        Assert.All(library.Entries.Take(4), entry => Assert.Same(library.Entries[0].Onsets, entry.Onsets));
        Assert.NotSame(library.Entries[0].PhraseLaw, library.Entries[1].PhraseLaw);
        Assert.Same(library.Entries[3].PhraseLaw, library.Entries[4].PhraseLaw);
        Assert.Same(library.Entries[3].States, library.Entries[4].States);
        Assert.NotSame(library.Entries[3].Onsets, library.Entries[4].Onsets);
        Assert.DoesNotContain(Traverse(library.Surface), element => element is Carrier or CarrierLocation);
        foreach (var entry in library.Entries)
        {
            Assert.True(Contains(library.Surface, entry.Surface)); Assert.Empty(E(entry.Unresolved));
            Assert.Same(library.Frame, entry.Fixture.Frame);
            Assert.All(entry.Fixture.Reaches, reach => Assert.True(ReferenceEquals(reach, library.HorizontalReach) || ReferenceEquals(reach, library.VerticalReach)));
        }
    }

    [Fact]
    public void EveryPhraseStrokeRetainsItsActualSourceReadPhaseAndOutputRow()
    {
        var library = new RhythmFriezeLibrary();
        foreach (var entry in library.Entries)
        {
            var source = E(entry.Onsets); var selections = E(entry.Selections).Cast<Relation>().ToArray();
            Assert.Equal(source.Length, selections.Length);
            var flattened = new List<IElement>();
            foreach (var pair in selections.Zip(source))
            {
                Assert.True(entry.TryReadSelection(pair.First, out var onset, out var state, out var read,
                    out var phrase, out var strokes, out var pending));
                Assert.Same(pair.Second, onset); Assert.Same(onset.Left, state.Left);
                Assert.Empty(E(pending)); Assert.Same(phrase, strokes);
                Assert.True(FiniteLaw.TryReadResult(read, out var support, out var residue, out var consumed));
                Assert.Same(phrase, Assert.Single(E(support))); Assert.Empty(E(residue)); Assert.NotEmpty(E(consumed));
                Assert.True(Contains(entry.PhraseLaw, phrase)); Assert.True(Contains(pair.First, read));
                flattened.AddRange(E(strokes));
            }
            Assert.Equal(entry.PhaseCount, flattened.Count);
            var phases = E(entry.Fixture.Components.Single().Domain);
            var generated = E(entry.Generation).Cast<Relation>().ToArray(); Assert.Equal(phases.Length, generated.Length);
            for (var i = 0; i < generated.Length; i++)
            {
                Assert.True(entry.TryReadGeneration(generated[i], out var selection, out var word, out var phase, out var outputRow));
                Assert.Contains(selections, actual => ReferenceEquals(actual, selection));
                Assert.Same(flattened[i], word); Assert.Same(E(entry.Words)[i], word); Assert.Same(phases[i], phase);
                var input = Assert.IsType<Relation>(outputRow.Left); Assert.Same(phase, input.Left); Assert.Same(entry.Fixture.RootPort, input.Right);
                Assert.Same(word, Assert.Single(E(Assert.IsType<Relation>(outputRow.Right))));
            }
            var cycle = entry.SourceCycle;
            Assert.Equal(source.Length, E(Assert.IsType<Relation>(cycle.Left)).Length);
            var edges = E(Assert.IsType<Relation>(cycle.Right)).Cast<Relation>().ToArray();
            Assert.Same(Assert.IsType<Relation>(source[^1]).Left, edges[^1].Left);
            Assert.Same(Assert.IsType<Relation>(source[0]).Left, edges[^1].Right);
        }
    }

    [Fact]
    public void ExpansionKeepsCornersAsSeparateSamplesAndActualMoveRepetition()
    {
        var entry = new RhythmFriezeLibrary().Entries[0]; var trace = entry.Fixture.Run(entry.PhaseCount);
        Assert.True(entry.Fixture.TryReadTrace(trace, out var initial, out var final, out _, out _, out var samples, out var unresolved));
        Assert.True(entry.Fixture.SamePhaseConfiguration(initial, final)); Assert.Empty(E(unresolved));
        Assert.Equal(entry.PhaseCount, E(samples).Length);
        var repeated = false;
        foreach (var pair in E(samples).Zip(E(entry.Words)))
        {
            Assert.True(FriezeFixture.TryReadSample(Assert.IsType<Relation>(pair.First), out _, out _, out _, out _, out var apps, out _));
            var moves = E(Assert.IsType<Relation>(pair.Second));
            Assert.Equal(moves.Length, E(apps).Length);
            foreach (var actual in E(apps).Cast<Relation>().Zip(moves)) Assert.Same(actual.Second, actual.First.Right);
            if (moves.Length > 1) { repeated = true; Assert.All(moves, move => Assert.Same(moves[0], move)); }
        }
        Assert.True(repeated);
    }

    [Fact]
    public void AuthoredStateChangesAreActualUniqueNativeTransitionsIncludingTheWrap()
    {
        var entry = new RhythmFriezeLibrary().Entries[3]; var selections = E(entry.Selections).Cast<Relation>().ToArray();
        var stateDomain = Assert.IsType<Relation>(Assert.IsType<Relation>(entry.States.Left).Right);
        var states = E(stateDomain); Assert.Equal(2, states.Length);
        var expected = new[] { states[0], states[1], states[0], states[1], states[0], states[1], states[0], states[1] };
        var phrases = new List<Relation>();
        for (var i = 0; i < selections.Length; i++)
        {
            Assert.True(entry.TryReadSelection(selections[i], out var onset, out var binding, out _, out var phrase, out _, out var pending));
            Assert.Same(expected[i], binding.Right); Assert.Empty(E(pending));
            var stateRead = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(selections[i].Left).Right).Left);
            Assert.True(StructuralConstraint.TryReadUnique(stateRead, out var transition, out _));
            var edge = Assert.IsType<Relation>(transition);
            Assert.Same(expected[i == 0 ? selections.Length - 1 : i - 1], edge.Left); Assert.Same(expected[i], edge.Right);
            if (ReferenceEquals(onset.Right, entry.Roles.Left)) phrases.Add(phrase);
        }
        Assert.Equal(3, phrases.Count); Assert.Same(phrases[0], phrases[2]); Assert.NotSame(phrases[0], phrases[1]);
    }

    [Fact]
    public void AllAuthoredProgramsPassLocalStructuralRulesWithoutNumericSource()
    {
        var library = new RhythmFriezeLibrary();
        foreach (var entry in library.Entries)
        {
            var rules = new FriezeStructuralRules(entry.Fixture, S(Enumerable.Range(0, 64).Select(_ => (IElement)new Placeholder()).ToArray()));
            var read = rules.Evaluate(entry.Fixture.Run(entry.PhaseCount));
            Assert.True(rules.TryReadResult(read, out var passed, out var violations, out var unresolved));
            Assert.NotEmpty(E(passed)); Assert.Empty(E(violations)); Assert.Empty(E(unresolved));
            Assert.True(rules.TryReadPeriod(read, out var periodSamples, out _)); Assert.Equal(entry.PhaseCount, E(periodSamples).Length);
            Assert.DoesNotContain(Traverse(read), element => element is Carrier or CarrierLocation);
        }
    }

    [Fact]
    public void OpenSourceRetainsItsTailAndEmitsUnknownInsteadOfInventingARest()
    {
        var input = Input(); var source = StructuralConstraint.Open(E(input.Onsets));
        var entry = Compile(input, onsets: source);
        Assert.Same(source, entry.Onsets); Assert.NotEmpty(E(entry.Unresolved));
        Assert.Equal(2, E(entry.Selections).Length); Assert.Equal(2, entry.PhaseCount);
        Assert.True(entry.TryReadSelection(Assert.IsType<Relation>(E(entry.Selections)[0]), out _, out _, out _, out var known, out _, out var firstPending));
        Assert.Same(input.Phrase, known); Assert.Empty(E(firstPending));
        AssertUnknown(entry, 1);
        Assert.False(Closed(Assert.IsType<Relation>(entry.SourceCycle.Left)));
        Assert.False(Closed(Assert.IsType<Relation>(entry.SourceCycle.Right)));
        Assert.True(entry.Fixture.TryReadTrace(entry.Fixture.Run(2), out _, out _, out _, out _, out var samples, out var pending));
        Assert.NotEmpty(E(pending)); Assert.Equal(2, E(samples).Length);
        Assert.True(FriezeFixture.TryReadSampleDelta(Assert.IsType<Relation>(E(samples)[^1]), out var delta));
        Assert.True(input.Frame.TryReadResult(delta, out var residual, out _, out var remainder));
        Assert.False(Closed(residual)); Assert.NotEmpty(E(remainder));
    }

    [Fact]
    public void ForeignRoleMissingScheduleAndUnknownPhraseRemainUnresolved()
    {
        var input = Input(); var sourceRow = Assert.IsType<Relation>(Assert.Single(E(input.Onsets)));
        AssertUnknown(Compile(input, onsets: S(new Relation(sourceRow.Left, new Placeholder()))), 0);
        AssertUnknown(Compile(input, schedule: S()), 0);
        AssertUnknown(Compile(input, schedule: StructuralConstraint.Open(E(input.Schedule))), 0);
        var unknownPhrase = StructuralConstraint.Open();
        var unknownLaw = FiniteLaw.Create(S(input.Roles.Left, input.Roles.Right), input.Clock.Domain, S(unknownPhrase),
            FiniteLaw.Row(input.Roles.Left, input.State, S(unknownPhrase)));
        AssertUnknown(Compile(input, law: unknownLaw), 0);
        var unknownWord = S(StructuralConstraint.Open());
        var wordLaw = FiniteLaw.Create(S(input.Roles.Left, input.Roles.Right), input.Clock.Domain, S(unknownWord),
            FiniteLaw.Row(input.Roles.Left, input.State, S(unknownWord)));
        AssertUnknown(Compile(input, law: wordLaw), 0);
        AssertUnknown(Compile(input, law: StructuralConstraint.Open()), 0);
    }

    [Fact]
    public void CompetingPhrasesDoNotChooseAndMissingOrCompetingStateEdgesDoNotToggle()
    {
        var input = Input(); var second = S(S(QualitativeMotionFrame.Move(input.Frame.Up, new Placeholder())));
        var competingLaw = FiniteLaw.Create(S(input.Roles.Left, input.Roles.Right), input.Clock.Domain, S(input.Phrase, second),
            FiniteLaw.Row(input.Roles.Left, input.State, S(input.Phrase, second)));
        AssertUnknown(Compile(input, law: competingLaw), 0);
        var missingClock = new PhaseRecurrence(input.Clock.Domain, S());
        AssertUnknown(Compile(input, states: missingClock.Surface), 0);
        var competingClock = new PhaseRecurrence(input.Clock.Domain,
            S(new Relation(input.State, input.State), new Relation(input.State, input.State)));
        AssertUnknown(Compile(input, states: competingClock.Surface), 0);
        var other = new Placeholder();
        var wrongClock = new PhaseRecurrence(S(input.State, other), S(new Relation(input.State, other), new Relation(other, input.State)));
        AssertUnknown(Compile(input, states: wrongClock.Surface), 0);
    }

    [Fact]
    public void DuplicateSlotsAndDuplicateScheduleBindingsCannotSupplyFalseOccurrences()
    {
        var input = Input(); var row = Assert.IsType<Relation>(Assert.Single(E(input.Onsets)));
        var duplicate = Compile(input, onsets: S(row, row));
        Assert.Equal(2, E(duplicate.Selections).Length); AssertUnknown(duplicate, 0); AssertUnknown(duplicate, 1);
        var binding = Assert.IsType<Relation>(Assert.Single(E(input.Schedule)));
        AssertUnknown(Compile(input, schedule: S(binding, new Relation(binding.Left, binding.Right))), 0);
    }

    [Fact]
    public void LookalikeCopiedReceiptsAndForeignCompilationMembersAreRejected()
    {
        var library = new RhythmFriezeLibrary(); var first = library.Entries[0]; var other = library.Entries[1];
        var selection = Assert.IsType<Relation>(E(first.Selections)[0]); var generated = Assert.IsType<Relation>(E(first.Generation)[0]);
        Assert.False(first.TryReadSelection(new Relation(selection.Left, selection.Right), out _, out _, out _, out _, out _, out _));
        Assert.False(other.TryReadSelection(selection, out _, out _, out _, out _, out _, out _));
        Assert.False(first.TryReadGeneration(new Relation(generated.Left, generated.Right), out _, out _, out _, out _));
        Assert.False(other.TryReadGeneration(generated, out _, out _, out _, out _));
    }

    private sealed record Inputs(QualitativeMotionFrame Frame, Relation Onsets, Relation Roles,
        IElement State, PhaseRecurrence Clock, Relation Schedule, Relation Phrase, Relation Law);

    private static Inputs Input()
    {
        var frame = new QualitativeMotionFrame(); var hit = new Placeholder(); var rest = new Placeholder();
        var state = new Placeholder(); var slot = new Placeholder(); var roles = new Relation(hit, rest);
        var clock = new PhaseRecurrence(S(state), S(new Relation(state, state)));
        var phrase = S(S(QualitativeMotionFrame.Move(frame.Right, new Placeholder())));
        var law = FiniteLaw.Create(S(hit, rest), clock.Domain, S(phrase),
            FiniteLaw.Row(hit, state, S(phrase)), FiniteLaw.Row(rest, state, S(phrase)));
        return new(frame, S(new Relation(slot, hit)), roles, state, clock, S(new Relation(slot, state)), phrase, law);
    }

    private static RhythmFriezeEntry Compile(Inputs input, Relation? onsets = null, Relation? states = null,
        Relation? schedule = null, Relation? law = null) =>
        RhythmFriezeLibrary.FromOnsets(input.Frame, onsets ?? input.Onsets, input.Roles,
            states ?? input.Clock.Surface, schedule ?? input.Schedule, law ?? input.Law);

    private static void AssertUnknown(RhythmFriezeEntry entry, int index)
    {
        var selection = Assert.IsType<Relation>(E(entry.Selections)[index]);
        Assert.True(entry.TryReadSelection(selection, out _, out _, out _, out var phrase, out var strokes, out var pending));
        Assert.False(Closed(phrase)); Assert.NotEmpty(E(pending)); Assert.NotEmpty(E(entry.Unresolved));
        var unknownWord = Assert.IsType<Relation>(Assert.Single(E(strokes))); Assert.False(Closed(unknownWord));
        Assert.NotSame(phrase, strokes);
    }

    private static IEnumerable<IElement> Traverse(IElement element)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(element);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Right); pending.Push(relation.Left); }
        }
    }
    private static bool Contains(IElement source, IElement target) => Traverse(source).Any(item => ReferenceEquals(item, target));
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
