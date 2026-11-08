using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class ObservedMotionCandidatesTests
{
    [Fact]
    public void Actual_extent_change_filters_supplied_words_and_retains_displaced_evidence()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(2);
        var words = new[] { f.Word(0, -1), f.Word(0, -3), f.Word(0, -9), f.Word(0, 0), f.Word(1, 0) };
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, words);
        Assert.Equal(5, learner.Read().Possible.Length); Assert.Null(learner.Read().Unique);
        var before = Sample(machine); var first = Endpoint(f, 0, -6);
        var step = machine.Step(machine.Publication(4)); var after = Sample(machine); var second = Endpoint(f, 0, -9);
        var observation = learner.Observe(before, after, first, second, S(machine.ExtentPort), step.Surface);
        Assert.True(observation.Recorded); Assert.True(observation.Informative); Assert.Equal(1, observation.ActualDelta);
        Assert.Same(words[1], observation.Result.Unique); Assert.Equal(4, observation.Result.Rejected.Length);
        foreach (var part in new IElement[] { first, second, step.Surface, machine.ExtentPort, words[1] })
            Assert.Contains(Reachable(observation.Result.Witness), element => ReferenceEquals(part, element));
        Assert.Equal(3, machine.Snapshot().Value); Assert.Equal(0, machine.Snapshot().Tension);
    }

    [Fact]
    public void A_unique_unobserved_hypothesis_is_not_established_motion()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(2);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, -1));
        Assert.Single(learner.Read().Possible); Assert.Null(learner.Read().Unique);
        var sample = Sample(machine);
        var read = learner.Observe(sample, sample, Endpoint(f, 0, -2), Endpoint(f, 0, -2), S(machine.ExtentPort));
        Assert.True(read.Complete); Assert.False(read.Informative); Assert.Null(read.Result.Unique);
    }

    [Fact]
    public void A_blocked_request_does_not_turn_zero_actual_change_into_a_motion_sample()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(2, 2);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, 0), f.Word(0, -3));
        var before = Sample(machine); var step = machine.Step(machine.Publication(4));
        var read = learner.Observe(before, Sample(machine), Endpoint(f, 0, -6), Endpoint(f, 0, -6), S(machine.ExtentPort), step.Surface);
        Assert.Equal(1, step.RequestedDelta); Assert.Equal(0, step.AcceptedDelta);
        Assert.Equal(0, read.ActualDelta); Assert.False(read.Informative);
        Assert.Equal(2, read.Result.Possible.Length); Assert.Null(read.Result.Unique);
    }

    [Fact]
    public void Negative_and_multi_tick_actual_changes_use_the_same_positive_motion_word()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var down = f.Word(1, -3); var up = f.Word(-1, 3);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, down, up);
        var before = Sample(machine); var first = Endpoint(f, 4, -12);
        machine.Step(machine.Publication(1)); machine.Step(machine.Publication(1));
        var read = learner.Observe(before, Sample(machine), first, Endpoint(f, 2, -6), S(machine.ExtentPort));
        Assert.Equal(-2, read.ActualDelta); Assert.Same(down, read.Result.Unique);
    }

    [Theory]
    [InlineData("open")] [InlineData("multiple")] [InlineData("foreign")] [InlineData("none")]
    public void Unclosed_or_confounded_change_does_not_eliminate_candidates(string kind)
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(2); var other = CoupledExtentMachine.Create(2);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, -1), f.Word(0, -3));
        var changes = kind switch { "open" => StructuralConstraint.Open(machine.ExtentPort),
            "multiple" => S(machine.ExtentPort, other.ExtentPort), "foreign" => S(other.ExtentPort), _ => S() };
        var read = learner.Observe(Sample(machine, 2), Sample(machine, 3), Endpoint(f, 0, -6), Endpoint(f, 0, -9), changes);
        Assert.True(read.Complete); Assert.False(read.Isolated); Assert.False(read.Informative);
        Assert.Equal(2, read.Result.Possible.Length); Assert.Empty(read.Result.Rejected);
        Assert.Contains(Reachable(read.Witness), item => ReferenceEquals(item, changes));
    }

    [Fact]
    public void Missing_geometry_foreign_samples_and_live_aliases_remain_open()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(2); var other = CoupledExtentMachine.Create(2);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, -1), f.Word(0, -3));
        var cases = new[]
        {
            (Sample(machine, 2), Sample(machine, 3), new Relation(new Placeholder(), StructuralConstraint.Open())),
            (Sample(other, 2), Sample(other, 3), Endpoint(f, 0, -9)),
            ((CarrierLocation)machine.ExtentPort.Left, Sample(machine, 3), Endpoint(f, 0, -9)),
            (Sample(machine, 2), Sample(machine, 3), Endpoint(Frame(), 0, -9)),
        };
        foreach (var (before, after, port) in cases)
        {
            var read = learner.Observe(before, after, Endpoint(f, 0, -6), port, S(machine.ExtentPort));
            Assert.False(read.Complete); Assert.False(read.Informative); Assert.Null(read.Result.Unique);
            Assert.Equal(2, read.Result.Possible.Length);
        }
    }

    [Fact]
    public void Equal_displacements_do_not_choose_between_distinct_ordered_constructions()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(1);
        var rightDown = f.Word(1, -1);
        var downRight = S(QualitativeMotionFrame.Move(f.Motion.Down, f.Reach), QualitativeMotionFrame.Move(f.Motion.Right, f.Reach));
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, rightDown, downRight, f.Word(1, 1));
        var read = learner.Observe(Sample(machine, 1), Sample(machine, 2), Endpoint(f, 1, -1), Endpoint(f, 2, -2), S(machine.ExtentPort));
        Assert.Equal(2, read.Result.Possible.Length); Assert.Null(read.Result.Unique);
        Assert.All(read.Result.Candidates.Take(2), c => Assert.True(c.Supported));
    }

    [Fact]
    public void Later_incompatible_evidence_exposes_empty_support_instead_of_rewriting_history()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(1);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, -1), f.Word(0, -3));
        learner.Observe(Sample(machine, 1), Sample(machine, 2), Endpoint(f, 0, -1), Endpoint(f, 0, -2), S(machine.ExtentPort));
        Assert.NotNull(learner.Read().Unique);
        var read = learner.Observe(Sample(machine, 2), Sample(machine, 3), Endpoint(f, 0, -2), Endpoint(f, 0, -5), S(machine.ExtentPort));
        Assert.Empty(read.Result.Possible); Assert.Equal(2, read.Result.Rejected.Length); Assert.Null(read.Result.Unique);
        Assert.Equal(2, read.Result.InformativeObservations);
        Assert.All(read.Result.Candidates, c => { Assert.True(c.Supported); Assert.True(c.Contradicted); });
    }

    [Fact]
    public void Unknown_candidate_word_is_preserved_and_prevents_false_unique_selection()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(1);
        var unknown = StructuralConstraint.Open();
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, -3), unknown);
        var read = learner.Observe(Sample(machine, 1), Sample(machine, 2), Endpoint(f, 0, -3), Endpoint(f, 0, -6), S(machine.ExtentPort));
        Assert.Equal(2, read.Result.Possible.Length); Assert.Null(read.Result.Unique);
        Assert.False(read.Result.Candidates[1].Supported); Assert.False(read.Result.Candidates[1].Contradicted);
    }

    [Fact]
    public void Repeated_event_is_not_new_evidence_and_captured_samples_do_not_drift()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(1);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, -1), f.Word(0, -3));
        var occurrence = new Placeholder(); var before = Sample(machine, 1); var after = Sample(machine, 2);
        var a = Endpoint(f, 0, -1); var b = Endpoint(f, 0, -2);
        learner.Observe(before, after, a, b, S(machine.ExtentPort), eventIdentity: occurrence);
        var repeated = learner.Observe(before, after, a, b, S(machine.ExtentPort), eventIdentity: occurrence);
        Assert.False(repeated.Recorded); Assert.Equal(1, repeated.Result.InformativeObservations);
        after.Tick.Set(8);
        Assert.NotNull(learner.Read().Unique); Assert.Equal(1, learner.Read().InformativeObservations);
    }

    [Fact]
    public void Numeric_archive_restores_candidate_evidence_and_can_accept_another_trial()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(1);
        var learner = ObservedMotionCandidates.Create(f, machine.ExtentPort, f.Word(0, -1), f.Word(0, -3));
        learner.Observe(Sample(machine, 1), Sample(machine, 2), Endpoint(f, 0, -3), Endpoint(f, 0, -6), S(machine.ExtentPort));
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(f.Surface, learner.ArchiveSurface(),
            Sample(machine, 2), Sample(machine, 3), Endpoint(f, 0, -6), Endpoint(f, 0, -9)));
        var restoredFrame = Curve.Frame.FromSurface((Relation)roots[0]);
        var restored = ObservedMotionCandidates.Restore(restoredFrame, (Relation)roots[1]);
        Assert.NotNull(restored.Read().Unique); Assert.Equal(1, restored.Read().Rejected.Length);
        var next = restored.Observe((CarrierLocation)roots[2], (CarrierLocation)roots[3],
            (Relation)roots[4], (Relation)roots[5], S(restored.ExtentPort));
        Assert.Equal(2, next.Result.InformativeObservations); Assert.NotNull(next.Result.Unique);
        Assert.Equal(1, learner.Read().InformativeObservations);
        var isolated = ObservedMotionCandidates.Decode(learner.Encode());
        Assert.NotNull(isolated.Read().Unique); Assert.NotSame(learner.ExtentPort, isolated.ExtentPort);
        Assert.Throws<ArgumentException>(() => ObservedMotionCandidates.Restore(f, (Relation)roots[1]));
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static CarrierLocation Sample(CoupledExtentMachine machine, long? value = null) =>
        new((Carrier)machine.ExtentPort.Right, value ?? ((CarrierLocation)machine.ExtentPort.Left).Ticks);
    private static Relation Endpoint(Curve.Frame frame, int x, int y) => new(new Placeholder(), frame.Word(x, y));
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation r) { todo.Push(r.Left); todo.Push(r.Right); }
        }
    }
}
