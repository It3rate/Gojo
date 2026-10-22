using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Streams = Gojo.Experiments.QualitativeFrieze.Composition.StructuralEvidenceStreams;

namespace QualitativeExecution.Tests;

public sealed class StructuralEvidenceStreamsTests
{
    private static readonly Lazy<IReadOnlyList<JointReconstructionSources.Case>> Sources = new(JointReconstructionSources.Cases);

    [Fact]
    public void Confidence_is_an_ordered_native_ladder_with_no_calibrated_spacing()
    {
        var frame = new Streams.Frame();
        Assert.True(Streams.Frame.TryRead(frame.Surface, out var read));
        Assert.True(read.Greater(read.Strong, read.Medium));
        Assert.True(read.Greater(read.Medium, read.Weak));
        Assert.True(read.Greater(read.Weak, read.None));
        Assert.False(read.Greater(read.Weak, read.Strong));
        Assert.True(read.AtLeast(read.Weak, read.Weak));
        Assert.Equal(3, E(read.Route(read.None, read.Strong)).Length);
        Assert.False(Closed(read.Route(read.Strong, read.None)));
        Assert.False(Reachable(frame.Surface).Any(item => item is Carrier or CarrierLocation));
    }

    [Fact]
    public void Support_and_doubt_remain_independent_faces_of_the_same_claim()
    {
        var frame = new Streams.Frame(); var claim = new Relation(new Placeholder(), new Placeholder());
        var cause = new Placeholder(); var dependency = new Placeholder();
        var observed = new Streams.Stream(claim, frame.Strong, frame.Medium, cause, S(dependency), new Relation(cause, dependency));
        var bundle = new Streams.Bundle(frame, S(observed.Surface), S(), S(), S());
        var read = Streams.Assess(bundle, claim);
        Assert.True(read.Supports); Assert.False(read.Opposes); Assert.True(read.Conflicted);
        Assert.True(ReferenceEquals(read.Support, frame.Strong));
        Assert.True(ReferenceEquals(read.Opposition, frame.Medium));
        Assert.True(Reachable(read.Surface).Any(item => ReferenceEquals(item, cause)));
        Assert.True(Reachable(read.Surface).Any(item => ReferenceEquals(item, dependency)));
    }

    [Fact]
    public void Defaults_goals_and_hypotheses_do_not_become_observed_truth()
    {
        var frame = new Streams.Frame(); var claim = new Placeholder(); var cause = new Placeholder();
        var goal = new Streams.Stream(claim, frame.Strong, frame.None, cause, S(), cause);
        var hypothesis = new Streams.Stream(claim, frame.Strong, frame.Weak, cause, S(), cause);
        var fallback = new Streams.Stream(claim, frame.Weak, frame.None, cause, S(), cause);
        var bundle = new Streams.Bundle(frame, S(), S(hypothesis.Surface), S(fallback.Surface), S(goal.Surface));
        var read = Streams.Assess(bundle, claim);
        Assert.True(ReferenceEquals(read.ObservedSupport, frame.None));
        Assert.True(ReferenceEquals(read.ObservedOpposition, frame.None));
        Assert.True(ReferenceEquals(read.Support, frame.Weak));
        Assert.True(read.CanUseDefault);
        Assert.True(Reachable(read.Surface).Any(item => ReferenceEquals(item, goal.Surface)));
        Assert.True(Reachable(read.Surface).Any(item => ReferenceEquals(item, hypothesis.Surface)));
    }

    [Fact]
    public void A_stronger_observation_defeats_a_default_without_deleting_it()
    {
        var frame = new Streams.Frame(); var claim = new Placeholder(); var source = new Placeholder();
        var defaultStream = new Streams.Stream(claim, frame.Weak, frame.None, source, S(), source);
        var contrary = new Streams.Stream(claim, frame.None, frame.Strong, source, S(), source);
        var before = new Streams.Bundle(frame, S(), S(), S(defaultStream.Surface), S());
        var after = new Streams.Bundle(frame, S(contrary.Surface), before.Hypotheses, before.Defaults, before.Goals);
        Assert.True(Streams.Assess(before, claim).CanUseDefault);
        var read = Streams.Assess(after, claim);
        Assert.False(read.CanUseDefault); Assert.True(read.Opposes); Assert.True(read.Conflicted);
        Assert.True(Reachable(read.Surface).Any(item => ReferenceEquals(item, defaultStream.Surface)));
        Assert.True(ReferenceEquals(before.Defaults, after.Defaults));
    }

    [Fact]
    public void Repeated_routes_do_not_accumulate_fake_independent_support()
    {
        var frame = new Streams.Frame(); var claim = new Placeholder(); var source = new Placeholder();
        var row = new Streams.Stream(claim, frame.Weak, frame.None, source, S(source), source);
        var copiedRoute = new Streams.Stream(claim, frame.Weak, frame.None, source, S(row.Surface), row.Surface);
        var bundle = new Streams.Bundle(frame, S(row.Surface, row.Surface, copiedRoute.Surface), S(), S(), S());
        var read = Streams.Assess(bundle, claim);
        Assert.True(ReferenceEquals(read.Support, frame.Weak));
        Assert.True(ReferenceEquals(read.ObservedSupport, frame.Weak));
        Assert.Equal(3, E(read.SelectedStreams).Length);
    }

    [Fact]
    public void Transport_retains_contexts_shared_claim_identity_and_qualitative_conflict()
    {
        var frame = new Streams.Frame(); var claim = new Placeholder(); var source = new Placeholder();
        var observation = new Streams.Stream(claim, frame.Medium, frame.Weak, source, S(source), source);
        var fallback = new Streams.Stream(claim, frame.Weak, frame.None, source, S(), source);
        var original = new Streams.Bundle(frame, S(observation.Surface), S(), S(fallback.Surface), S());
        var packet = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("evidence-test", original.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(packet), out var restored));
        Assert.True(restored.Root is Relation && Streams.Bundle.TryRead((Relation)restored.Root, out _));
        Assert.True(Streams.Bundle.TryRead((Relation)restored.Root, out var bundle));
        var importedClaim = bundle.Read(bundle.Observations)[0].Claim;
        Assert.True(ReferenceEquals(importedClaim, bundle.Read(bundle.Defaults)[0].Claim));
        var read = Streams.Assess(bundle, importedClaim);
        Assert.True(read.Conflicted); Assert.True(read.Supports);
        Assert.True(ReferenceEquals(read.Support, bundle.Ladder.Medium));
        Assert.False(ReferenceEquals(importedClaim, claim));
    }

    [Fact]
    public void An_observed_gap_defeats_default_completion_while_both_explanations_remain()
    {
        var message = Message("J08"); var originalJoints = message.Joints;
        var selected = Streams.SelectClosure(message);
        Assert.True(selected.PreserveObservedGap); Assert.False(selected.ShouldComplete); Assert.False(selected.ClosureRequired);
        Assert.True(ReferenceEquals(selected.Selected, selected.OpenPath));
        Assert.Equal(2, E(selected.ObservedEnds).Length);
        Assert.Equal(3, E(selected.RetainedAlternatives).Length);
        Assert.True(selected.Read.Opposes); Assert.True(selected.Read.Conflicted);
        Assert.True(Reachable(selected.Surface).Any(item => ReferenceEquals(item, selected.ClosedParentWithDamage)));
        Assert.True(Reachable(selected.Surface).Any(item => ReferenceEquals(item, message.ClosureDefault)));
        Assert.True(ReferenceEquals(message.Joints, originalJoints));
    }

    [Fact]
    public void A_repair_goal_requests_completion_without_rewriting_the_observed_gap()
    {
        var message = Message("J08"); var evidence = Streams.ClosureEvidence(message); var frame = evidence.Ladder;
        var request = new Streams.Stream(message.ClosureDefault, frame.Strong, frame.None,
            message.Roles, S(message.ClosureDefault), message.ClosureDefault);
        var withGoal = new Streams.Bundle(frame, evidence.Observations, evidence.Hypotheses, evidence.Defaults, S(request.Surface));
        var selected = Streams.SelectClosure(message, withGoal);
        Assert.True(selected.ShouldComplete); Assert.True(selected.ClosureRequired); Assert.True(selected.PreserveObservedGap);
        Assert.True(selected.Read.Opposes);
        Assert.True(ReferenceEquals(selected.Read.ObservedOpposition, frame.Strong));
        Assert.True(ReferenceEquals(selected.Selected, selected.CompletionProposal));
        Assert.True(Reachable(selected.Surface).Any(item => ReferenceEquals(item, request.Surface)));
    }

    [Fact]
    public void Opposed_goal_streams_remain_conflicted_instead_of_first_goal_winning()
    {
        var message = Message("J08"); var evidence = Streams.ClosureEvidence(message); var frame = evidence.Ladder;
        var desire = new Streams.Stream(message.ClosureDefault, frame.Weak, frame.None, message.Roles, S(), message.Roles);
        var refuse = new Streams.Stream(message.ClosureDefault, frame.None, frame.Strong, message.Roles, S(), message.Roles);
        var bundle = new Streams.Bundle(frame, evidence.Observations, evidence.Hypotheses, evidence.Defaults, S(desire.Surface, refuse.Surface));
        var selected = Streams.SelectClosure(message, bundle);
        Assert.False(selected.ClosureRequired); Assert.False(selected.ShouldComplete);
        Assert.True(Reachable(selected.Surface).Any(item => ReferenceEquals(item, desire.Surface)));
        Assert.True(Reachable(selected.Surface).Any(item => ReferenceEquals(item, refuse.Surface)));
    }

    [Fact]
    public void No_endpoint_evidence_leaves_default_weak_rather_than_claiming_observed_closure()
    {
        var message = Message("J02"); var selected = Streams.SelectClosure(message);
        Assert.True(selected.ShouldComplete); Assert.False(selected.PreserveObservedGap);
        Assert.True(ReferenceEquals(selected.Read.ObservedSupport, selected.Evidence.Ladder.None));
        Assert.True(ReferenceEquals(selected.Read.Support, selected.Evidence.Ladder.Weak));
        Assert.Empty(E(selected.ObservedEnds));
    }

    [Fact]
    public void A_rung_from_an_unrelated_ladder_cannot_supply_confidence_by_name_alone()
    {
        var frame = new Streams.Frame(); var other = new Streams.Frame(); var claim = new Placeholder();
        var bad = new Streams.Stream(claim, other.Strong, frame.None, claim, S(), claim);
        var bundle = new Streams.Bundle(frame, S(bad.Surface), S(), S(), S());
        Assert.False(Streams.Bundle.TryRead(bundle.Surface, out _));
        Assert.False(Closed(frame.Route(frame.None, other.Strong)));
    }

    [Fact]
    public void A_malformed_ladder_with_a_missing_link_is_not_a_valid_confidence_basis()
    {
        var frame = new Streams.Frame(); var links = E(frame.Steps).ToArray();
        var malformed = new Relation(frame.Surface.Left, S(links[0], new Relation(new Placeholder(), frame.Medium), links[2]));
        Assert.False(Streams.Frame.TryRead(malformed, out _));
    }

    private static JointShapeDescription.Message Message(string id) => JointShapeDescription.Analyze(Sources.Value.Single(item => item.Id == id).Source).Message;
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) && start.Equals(end);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!visited.Add(item)) continue;
            yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
