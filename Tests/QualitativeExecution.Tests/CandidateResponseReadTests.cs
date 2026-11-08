using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class CandidateResponseReadTests
{
    [Fact]
    public void Supported_distinct_constructions_can_share_one_response_without_becoming_one_candidate()
    {
        var f = Fixture(supported: true); var read = f.Reader.Observe(f.Memory, Offers(f, 4, 4));
        Assert.True(read.Shared); Assert.Equal(4, read.Mask); Assert.Equal(2, read.PossibleCount);
        Assert.Null(f.Memory.Read().Unique); Assert.Equal(2, f.Memory.Read().Possible.Length);
        Assert.True(f.Machine.Step(read.Packet).Changed); Assert.Equal(2, f.Machine.Snapshot().Value);
        Assert.Equal(1, f.Memory.Read().InformativeObservations);
        Assert.Contains(Reachable(read.Witness), x => ReferenceEquals(x, f.First));
        Assert.Contains(Reachable(read.Witness), x => ReferenceEquals(x, f.Second));
        Assert.DoesNotContain(Reachable(read.Packet), x => ReferenceEquals(x, f.Memory.Surface));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void Missing_conflicting_or_multiple_responses_do_not_become_shared_agreement(byte second)
    {
        var f = Fixture(supported: true); var read = f.Reader.Observe(f.Memory, Offers(f, 4, second));
        Assert.False(read.Shared); Assert.Equal(0, read.Mask);
        Assert.Equal(2, f.Memory.Read().Possible.Length);
    }

    [Fact]
    public void Supplied_hypotheses_can_be_chosen_provisionally_without_being_called_observed_motion()
    {
        var f = Fixture(supported: false); var read = f.Reader.Observe(f.Memory, Offers(f, 4, 4));
        Assert.False(read.Shared); Assert.Equal(2, read.EligibleCount);
        var chooser = new Placeholder(); var reason = new Relation(new Placeholder(), f.First);
        var choice = FiniteChoice.Select(read.Proposal, chooser, f.First, reason);
        var selected = f.Reader.Select(f.Memory, read.Witness, choice);
        Assert.True(selected.Resolved); Assert.True(selected.Provisional); Assert.Equal(4, selected.Mask);
        Assert.Same(f.First, selected.SelectedCandidate);
        Assert.Equal(0, f.Memory.Read().InformativeObservations); Assert.Null(f.Memory.Read().Unique);
        Assert.Equal(2, f.Memory.Read().Possible.Length);
        Assert.Contains(Reachable(selected.Witness), x => ReferenceEquals(x, choice));
        Assert.Contains(Reachable(selected.Witness), x => ReferenceEquals(x, reason));
        Assert.True(f.Machine.Step(selected.Packet).Changed);
    }

    [Fact]
    public void A_missing_prediction_keeps_the_common_read_open_but_does_not_forbid_an_explicit_known_action()
    {
        var f = Fixture(supported: true);
        var read = f.Reader.Observe(f.Memory, S(new Relation(f.First, f.Machine.Publication(1))));
        Assert.False(read.Shared); Assert.Equal(1, read.EligibleCount); Assert.Equal(2, read.PossibleCount);
        var choice = FiniteChoice.Select(read.Proposal, new Placeholder(), f.First, new Placeholder());
        var selected = f.Reader.Select(f.Memory, read.Witness, choice);
        Assert.True(selected.Provisional); Assert.Equal(1, selected.Mask);
        Assert.Equal(2, f.Memory.Read().Possible.Length);
    }

    [Fact]
    public void An_open_motion_word_cannot_borrow_a_supplied_singleton_response()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame()); var machine = CoupledExtentMachine.Create(1);
        var known = frame.Word(1, -1); var unknown = StructuralConstraint.Open();
        var memory = ObservedMotionCandidates.Create(frame, machine.ExtentPort, known, unknown);
        var reader = CandidateResponseRead.Create(machine, memory);
        var read = reader.Observe(memory, S(new Relation(known, machine.Publication(4)), new Relation(unknown, machine.Publication(4))));
        Assert.False(read.Shared); Assert.Equal(1, read.EligibleCount);
        var choice = FiniteChoice.Select(read.Proposal, new Placeholder(), unknown, new Placeholder());
        Assert.False(reader.Select(memory, read.Witness, choice).Resolved);
    }

    [Theory]
    [InlineData("open")] [InlineData("duplicate")] [InlineData("foreign")]
    public void An_incomplete_or_malformed_offer_scope_cannot_authorize_a_hidden_selection(string kind)
    {
        var f = Fixture(supported: true); var first = new Relation(f.First, f.Machine.Publication(4));
        var second = new Relation(f.Second, f.Machine.Publication(4));
        var offers = kind switch
        {
            "open" => StructuralConstraint.Open(first, second),
            "duplicate" => S(first, first, second),
            _ => S(first, second, new Relation(f.Frame.Word(2, 3), f.Machine.Publication(4)))
        };
        var read = f.Reader.Observe(f.Memory, offers); Assert.False(read.Shared);
        var choice = FiniteChoice.Select(read.Proposal, new Placeholder(), f.First, new Placeholder());
        Assert.False(f.Reader.Select(f.Memory, read.Witness, choice).Resolved);
    }

    [Fact]
    public void Contradicted_candidates_are_not_actions_and_empty_support_is_not_vacuous_agreement()
    {
        var f = Fixture(supported: true);
        f.Memory.Observe(Sample(f.Machine, 2), Sample(f.Machine, 3), Endpoint(f.Frame, 2, -2),
            Endpoint(f.Frame, 3, 1), S(f.Machine.ExtentPort));
        Assert.Empty(f.Memory.Read().Possible);
        var read = f.Reader.Observe(f.Memory, Offers(f, 4, 4));
        Assert.False(read.Shared); Assert.Equal(0, read.PossibleCount); Assert.Equal(0, read.EligibleCount);
        var choice = FiniteChoice.Select(read.Proposal, new Placeholder(), f.First, new Placeholder());
        Assert.False(f.Reader.Select(f.Memory, read.Witness, choice).Resolved);
    }

    [Fact]
    public void A_foreign_receivers_response_does_not_fill_this_receivers_offer()
    {
        var f = Fixture(supported: true); var other = CoupledExtentMachine.Create(1);
        var read = f.Reader.Observe(f.Memory, S(new Relation(f.First, f.Machine.Publication(4)), new Relation(f.Second, other.Publication(4))));
        Assert.False(read.Shared); Assert.Equal(1, read.EligibleCount);
    }

    [Fact]
    public void Later_evidence_invalidates_an_old_provisional_selection_without_erasing_the_old_witness()
    {
        var f = Fixture(supported: false); var read = f.Reader.Observe(f.Memory, Offers(f, 4, 1));
        var choice = FiniteChoice.Select(read.Proposal, new Placeholder(), f.First, new Placeholder());
        Assert.True(f.Reader.Select(f.Memory, read.Witness, choice).Resolved);
        Support(f);
        var stale = f.Reader.Select(f.Memory, read.Witness, choice);
        Assert.False(stale.Resolved); Assert.False(stale.Provisional); Assert.Equal(0, stale.Mask);
        Assert.Contains(Reachable(stale.Witness), x => ReferenceEquals(x, read.Witness));
        Assert.Equal(1, f.Memory.Read().InformativeObservations);
    }

    [Fact]
    public void A_choice_for_another_proposal_or_an_already_shared_read_is_not_a_provisional_response()
    {
        var f = Fixture(supported: false); var offers = Offers(f, 4, 1);
        var a = f.Reader.Observe(f.Memory, offers); var b = f.Reader.Observe(f.Memory, offers);
        var choice = FiniteChoice.Select(a.Proposal, new Placeholder(), f.First, new Placeholder());
        Assert.False(f.Reader.Select(f.Memory, b.Witness, choice).Resolved);
        Support(f); var shared = f.Reader.Observe(f.Memory, Offers(f, 4, 4));
        var needless = FiniteChoice.Select(shared.Proposal, new Placeholder(), f.First, new Placeholder());
        Assert.False(f.Reader.Select(f.Memory, shared.Witness, needless).Provisional);
        Assert.True(shared.Shared);
    }

    [Fact]
    public void Rechecking_or_retransmitting_a_selection_does_not_create_another_effect_event()
    {
        var f = Fixture(supported: false); var read = f.Reader.Observe(f.Memory, Offers(f, 4, 1));
        var choice = FiniteChoice.Select(read.Proposal, new Placeholder(), f.First, new Placeholder());
        var selected = f.Reader.Select(f.Memory, read.Witness, choice);
        var repeated = f.Reader.Select(f.Memory, read.Witness, choice);
        Assert.True(f.Machine.Step(selected.Packet).ConsumedEvent);
        Assert.False(f.Machine.Step(repeated.Packet).ConsumedEvent);
        Assert.Equal(2, f.Machine.Snapshot().Value); Assert.Equal(0, f.Machine.Snapshot().Tension);
    }

    [Fact]
    public void Every_finite_offer_pair_agrees_between_native_and_compiled_laws()
    {
        var f = Fixture(supported: true);
        for (byte a = 0; a < 8; a++) for (byte b = 0; b < 8; b++)
        {
            var offers = Offers(f, a, b); var native = f.Reader.Observe(f.Memory, offers, compiled: false);
            var compiled = f.Reader.Observe(f.Memory, offers, compiled: true);
            Assert.Equal(native.Mask, compiled.Mask); Assert.Equal(native.Shared, compiled.Shared);
            Assert.Equal(native.EligibleCount, compiled.EligibleCount);
        }
    }

    [Fact]
    public void Numeric_transport_preserves_the_choice_context_and_receiver_aliases()
    {
        var f = Fixture(supported: false); var read = f.Reader.Observe(f.Memory, Offers(f, 4, 1));
        var choice = FiniteChoice.Select(read.Proposal, new Placeholder(), f.First, new Placeholder());
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(f.Frame.Surface, f.Machine.ArchiveSurface(),
            f.Memory.ArchiveSurface(), f.Reader.Surface, read.Witness, choice));
        var frame = Curve.Frame.FromSurface((Relation)roots[0]); var machine = CoupledExtentMachine.Restore((Relation)roots[1]);
        var memory = ObservedMotionCandidates.Restore(frame, (Relation)roots[2]); var reader = CandidateResponseRead.FromSurface((Relation)roots[3]);
        var selected = reader.Select(memory, (Relation)roots[4], (Relation)roots[5]);
        Assert.True(selected.Provisional); Assert.Equal(4, selected.Mask); Assert.True(machine.Step(selected.Packet).Changed);
        Assert.Equal(2, machine.Snapshot().Value); Assert.Equal(1, f.Machine.Snapshot().Value);
        Assert.Equal(2, memory.Read().Possible.Length); Assert.Null(memory.Read().Unique);
        Assert.False(reader.Select(f.Memory, (Relation)roots[4], (Relation)roots[5]).Resolved);
    }

    [Fact]
    public void A_preference_law_cannot_be_relabelled_as_shared_agreement()
    {
        var f = Fixture(supported: true); var fields = FiniteLaw.Entries(f.Reader.Surface);
        fields[6] = ExtentResponseArbiter.Create(f.Machine, "first").Surface;
        Assert.Throws<ArgumentException>(() => CandidateResponseRead.FromSurface(S(fields)));
    }

    private sealed record Setup(Curve.Frame Frame, CoupledExtentMachine Machine,
        ObservedMotionCandidates Memory, CandidateResponseRead Reader, Relation First, Relation Second);
    private static Setup Fixture(bool supported)
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame()); var machine = CoupledExtentMachine.Create(1);
        var first = frame.Word(1, -1);
        var second = S(QualitativeMotionFrame.Move(frame.Motion.Down, frame.Reach), QualitativeMotionFrame.Move(frame.Motion.Right, frame.Reach));
        var memory = ObservedMotionCandidates.Create(frame, machine.ExtentPort, first, second);
        var result = new Setup(frame, machine, memory, CandidateResponseRead.Create(machine, memory), first, second);
        if (supported) Support(result); return result;
    }
    private static void Support(Setup f) => f.Memory.Observe(Sample(f.Machine, 1), Sample(f.Machine, 2),
        Endpoint(f.Frame, 1, -1), Endpoint(f.Frame, 2, -2), S(f.Machine.ExtentPort));
    private static Relation Offers(Setup f, byte a, byte b) => S(new Relation(f.First, f.Machine.Publication(a)), new Relation(f.Second, f.Machine.Publication(b)));
    private static CarrierLocation Sample(CoupledExtentMachine machine, long value) => new((Carrier)machine.ExtentPort.Right, value);
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
