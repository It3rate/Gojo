using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class CommunicationLoopTests
{
    private static readonly Lazy<CommunicationLoopStudy> Study = new(CommunicationLoopStudy.Build);

    [Fact]
    public void Native_hint_repair_preserves_the_before_guess_and_recovers_each_held_out_topology()
    {
        var study = Study.Value;
        Assert.Equal(3, study.Cases.Count);
        Assert.All(study.Cases.SelectMany(item => item.Trials), trial =>
        {
            Assert.NotNull(trial.Before.Selected); Assert.NotNull(trial.Before.Assessment);
            Assert.False(trial.Before.Assessment!.IsSatisfied);
            Assert.True(trial.Before.Assessment.RefutedCount > 0);
            var repair = Assert.IsType<CommunicationLoopStudy.Repair>(trial.Repair);
            Assert.Same(trial.Before.Message, repair.BeforeMessage);
            Assert.DoesNotContain(Entries(repair.BeforeMessage.Required), clause => ReferenceEquals(clause, repair.AddedClause));
            Assert.Contains(Entries(repair.AfterMessage.Required), clause => ReferenceEquals(clause, repair.AddedClause));
            Assert.Equal(Entries(repair.BeforeMessage.Required).Length + 1, Entries(repair.AfterMessage.Required).Length);
            Assert.True(trial.After.Assessment!.IsSatisfied);
            Assert.Equal(2, trial.After.Admissible.Count);
            Assert.NotSame(trial.Before.Selected, trial.After.Selected);
            Assert.Same(trial.Before.Receipt.Selected, trial.Before.Selected);
        });
    }

    [Fact]
    public void Shared_receiver_library_preserves_two_unmeasured_realizations_after_the_same_repair()
    {
        var study = Study.Value;
        Assert.Equal(14, study.Library.Realizations.Count);
        foreach (var item in study.Cases)
        {
            var first = item.Trials[0].After; var second = item.Trials[1].After;
            Assert.NotSame(first.Selected, second.Selected);
            Assert.All(first.Admissible, candidate => Assert.True(candidate.Assessment.IsSatisfied));
            Assert.Equal(first.Admissible.Select(candidate => candidate.Realization.Id).Order(), second.Admissible.Select(candidate => candidate.Realization.Id).Order());
            Assert.NotEqual(WordParticipation(first.Selected!.Program), WordParticipation(second.Selected!.Program));
            Assert.All(item.Trials, trial => Assert.Equal(study.Library.Realizations.Count, trial.Before.Candidates.Count));
        }
    }

    [Fact]
    public void Critic_republishes_selected_output_without_recipe_or_cached_occurrence_identities()
    {
        foreach (var selection in Study.Value.Cases.SelectMany(item => item.Trials).SelectMany(trial => new[] { trial.Before, trial.After }))
        {
            var selected = selection.Selected!; var output = selection.Output!; var critic = selection.Critic!;
            Assert.NotSame(selected, output); Assert.Same(selected.Program, output.Program);
            Assert.NotSame(selected.Execution, output.Execution);
            Assert.NotSame(selected.Execution.Current, output.Execution.Current);
            Assert.NotSame(selected.Graph, output.Graph);
            Assert.NotSame(selected.Publisher.Observation, critic.Observation);
            Assert.NotSame(selected.Publisher.Observation.Frame, critic.Observation.Frame);
            Assert.Same(output.Publisher.Observation, critic.Observation);
            Assert.Single(Entries(output.Execution.Current.Execution.CompletionEvents));
            Assert.True(output.Execution.Current.Execution.IsAtBoundary);
            Assert.True(output.Join.TryReadPassed(output.Join.Evaluate()));
            Assert.Contains(Reachable(output.Generation), element => ReferenceEquals(element, selection.Message.Surface));
            Assert.Contains(Reachable(output.Generation), element => ReferenceEquals(element, output.Execution.Current.Surface));
            var exposed = Reachable(critic.Observation.Surface).ToArray();
            Assert.DoesNotContain(exposed, element => ReferenceEquals(element, selected.Program.Surface));
            Assert.DoesNotContain(exposed, element => ReferenceEquals(element, selected.Execution.Current.Surface));
            Assert.DoesNotContain(exposed, element => ReferenceEquals(element, selected.Bindings));
            Assert.DoesNotContain(exposed, element => ReferenceEquals(element, output.Execution.Current.Surface));
            Assert.DoesNotContain(exposed, element => ReferenceEquals(element, output.Bindings));
            Assert.All(critic.Families.SelectMany(family => family.Occurrences), occurrence =>
                Assert.DoesNotContain(selected.Induction.Families.SelectMany(family => family.Occurrences), cached => ReferenceEquals(cached.Read, occurrence.Read)));
            Assert.True(selection.Publisher!.IsComplete); Assert.True(critic.IsComplete);
        }
    }

    [Fact]
    public void Candidate_selection_uses_only_message_and_published_context_and_retains_all_ambiguity()
    {
        var study = Study.Value;
        foreach (var item in study.Cases)
        {
            var receipt = study.Library.Receive(item.InitialMessage, study.Contexts[0]);
            Assert.Same(receipt.Admissible[0].Realization, receipt.Selected);
            Assert.True(receipt.Admissible.Count > 2);
            Assert.All(receipt.Candidates, candidate => Assert.Equal(candidate.Assessment.IsSatisfied, candidate.IsAdmissible));
            Assert.DoesNotContain(Reachable(receipt.Surface), element => ReferenceEquals(element, item.Intent.Surface));
            Assert.Same(item.Trials[0].Before.Selected, receipt.Selected);
        }
    }

    [Fact]
    public void Construction_executes_acknowledged_actions_and_joins_only_declared_equal_sites()
    {
        foreach (var output in Study.Value.Library.Realizations)
        {
            Assert.Single(Entries(output.Execution.Current.Execution.CompletionEvents));
            Assert.True(output.Execution.Current.Execution.IsAtBoundary);
            Assert.True(output.Execution.Current.Join.TryReadPassed(output.Execution.Current.Join.Evaluate()));
            Assert.True(output.Join.TryReadPassed(output.Join.Evaluate()));
            Assert.True(output.Publisher.IsComplete); Assert.True(output.Induction.IsComplete);
        }
        var joined = Study.Value.Library.Realizations.Single(item => item.Id == "cross-joined-1");
        var cut = Study.Value.Library.Realizations.Single(item => item.Id == "cross-cut-1");
        Assert.Equal(3, Entries(joined.Join.Accepted).Length);
        Assert.Empty(Entries(cut.Join.Accepted));
        Assert.Contains(joined.Induction.Families, family => family.Kind == "junction" && family.SegmentCount == 4);
        Assert.DoesNotContain(cut.Induction.Families, family => family.SegmentCount > 1);
    }

    [Fact]
    public void Zero_critic_capacity_leaves_missing_promises_open_and_cannot_certify_a_repair()
    {
        var item = Study.Value.Cases[1];
        var closed = item.IntentExemplar;
        var publisher = new QualitativeRecognitionObservation(closed.Publisher.Observation.Frame,
            closed.Publisher.Observation.Sites, closed.Publisher.Observation.Segments);
        var empty = new QualitativeMotifInduction(publisher, maxCycleEdges: 4, proposalCapacity: 0, maxWalkEdges: 3, retainedCapacity: 0);
        var read = new CommunicationAssessment(empty, item.Intent);
        Assert.False(empty.IsComplete); Assert.False(read.IsSatisfied);
        Assert.Equal(0, read.RefutedCount); Assert.True(read.UnresolvedCount > 0);
    }

    [Fact]
    public void Native_experiment_contains_no_calibrated_numeric_participants()
    {
        var graph = Reachable(Study.Value.Surface).ToArray();
        Assert.DoesNotContain(graph, element => element is Carrier or CarrierLocation);
        Assert.Contains(graph, element => element is Placeholder);
        Assert.All(Study.Value.Cases.SelectMany(item => item.Trials), trial =>
            Assert.Contains(Reachable(trial.Repair!.Surface), element => ReferenceEquals(element, trial.Before.Critic!.Surface)));
    }

    private static int WordParticipation(UnitArrangementProgram program) => Entries(program.Paths).Cast<Relation>()
        .Sum(path => Entries((Relation)path.Right).Cast<Relation>().Sum(word => Entries(word).Length));
    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
    }
}
