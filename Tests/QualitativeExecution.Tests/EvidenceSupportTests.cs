using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Support = Gojo.Experiments.QualitativeExecution.EvidenceSupport;

namespace QualitativeExecution.Tests;

public sealed class EvidenceSupportTests
{
    [Fact]
    public void Equal_means_preserve_unreduced_support_and_different_concentration()
    {
        var frame = new Support.Frame();
        var loose = Support.Read(Support.Create(frame, 1, 1));
        var concentrated = Support.Read(Support.Create(frame, 50, 50));
        Assert.Equal("1/2", loose.Mean.Reduced);
        Assert.Equal(loose.Mean.Reduced, concentrated.Mean.Reduced);
        Assert.Equal(2, loose.Concentration); Assert.Equal(100, concentrated.Concentration);
        Assert.Equal(1, Entries(loose.Positive).Length);
        Assert.Equal(50, Entries(concentrated.Positive).Length);
        Assert.All(Entries(concentrated.Positive), token => Assert.Same(frame.Token, token));
        Assert.Equal("1/12", loose.Variance.Reduced);
        Assert.Equal("1/404", concentrated.Variance.Reduced);
        Assert.Equal("1/4", loose.PredictiveVariance.Reduced);
        Assert.Equal(loose.PredictiveVariance.Reduced, concentrated.PredictiveVariance.Reduced);
        Assert.True(loose.Complete); Assert.True(concentrated.Complete);
        Assert.False(Reachable(concentrated.Surface).Any(item => item is Carrier or CarrierLocation));
    }

    [Fact]
    public void One_same_acquisition_changes_loose_and_concentrated_priors_differently()
    {
        var frame = new Support.Frame(); var sample = new Placeholder();
        var acquisition = frame.Acquisition(sample);
        var loose = Support.Read(Support.Create(frame, 1, 1, acquisition)
            .Observe(sample, false, true, acquisition));
        var firm = Support.Read(Support.Create(frame, 50, 50, acquisition)
            .Observe(sample, false, true, acquisition));
        Assert.Equal("2/3", loose.Mean.Reduced);
        Assert.Equal("51/101", firm.Mean.Reduced);
        Assert.Equal(1, loose.AcceptedEvents); Assert.Equal(1, firm.AcceptedEvents);
    }

    [Theory]
    [InlineData(false, true, 2, 1)]
    [InlineData(true, false, 2, 1)]
    [InlineData(false, false, 1, 2)]
    [InlineData(true, true, 1, 2)]
    public void Question_orientation_and_response_reversal_preserve_canonical_answer(
        bool askOpposed, bool affirm, int a, int b)
    {
        var frame = new Support.Frame(); var sample = new Placeholder(); var basis = frame.Acquisition(sample);
        var state = Support.Create(frame, 1, 1, basis).Observe(sample, askOpposed, affirm, basis);
        var read = Support.Read(state);
        Assert.Equal(a, read.A); Assert.Equal(b, read.B);
        Assert.Equal(0, read.UnresolvedReports);
        var native = Entries((Relation)state.Surface.Right).Single();
        var orientation = (Relation)((Relation)((Relation)native).Right).Left;
        Assert.Same(askOpposed ? frame.Contrary : frame.Claim, orientation.Left);
    }

    [Fact]
    public void Replayed_event_and_question_reversal_do_not_duplicate_support()
    {
        var frame = new Support.Frame(); var sample = new Placeholder(); var basis = frame.Acquisition(sample);
        var state = Support.Create(frame, 1, 1, basis)
            .Observe(sample, false, true, basis)
            .Observe(sample, true, false, basis);
        var read = Support.Read(state);
        Assert.Equal(2, read.A); Assert.Equal(1, read.B);
        Assert.Equal(1, read.AcceptedEvents); Assert.Equal(1, read.DuplicateReports);
        Assert.Equal(2, Entries((Relation)state.Surface.Right).Length);
    }

    [Fact]
    public void Same_observation_returned_over_two_routes_has_one_contribution_and_both_provenances()
    {
        var frame = new Support.Frame(); var sample = new Placeholder(); var basis = frame.Acquisition(sample);
        var routeA = new Relation(sample, new Placeholder()); var routeB = new Relation(sample, new Placeholder());
        var state = Support.Create(frame, 1, 1, basis)
            .Observe(sample, false, true, basis, routeA)
            .Observe(sample, false, true, basis, routeB);
        var read = Support.Read(state);
        Assert.Equal(3, read.Concentration); Assert.Equal(1, read.AcceptedEvents);
        Assert.Contains(Reachable(read.Accepted), item => ReferenceEquals(item, routeA));
        Assert.Contains(Reachable(read.Accepted), item => ReferenceEquals(item, routeB));
    }

    [Fact]
    public void Conflicting_replay_is_visible_without_counting_either_answer()
    {
        var frame = new Support.Frame(); var sample = new Placeholder(); var basis = frame.Acquisition(sample);
        var state = Support.Create(frame, 1, 1, basis)
            .Observe(sample, false, true, basis)
            .Observe(sample, false, false, basis);
        var read = Support.Read(state);
        Assert.Equal("1/2", read.Mean.Reduced); Assert.Equal(2, read.Concentration);
        Assert.Equal(0, read.AcceptedEvents); Assert.Equal(1, read.ConflictingEvents);
        Assert.False(read.Complete);
        Assert.Equal(2, Entries((Relation)((Relation)Entries(read.Conflicts).Single()).Right).Length);
    }

    [Fact]
    public void Unknown_response_is_open_and_does_not_supply_a_failure()
    {
        var frame = new Support.Frame(); var sample = new Placeholder(); var basis = frame.Acquisition(sample);
        var state = Support.Create(frame, 1, 1, basis).ObserveUnknown(sample, true, basis);
        var read = Support.Read(state);
        Assert.Equal(1, read.UnresolvedReports); Assert.Equal(0, read.AcceptedEvents);
        Assert.False(read.Complete);
        Assert.Equal(1, read.A); Assert.Equal(1, read.B);
        var report = (Relation)Entries((Relation)state.Surface.Right).Single();
        var answers = (Relation)((Relation)report.Right).Right;
        Assert.True(StructuralConstraint.TryReadScope(answers, out _, out var seam, out var tail));
        Assert.False(seam.Equals(tail));
    }

    [Fact]
    public void Distinct_event_ids_without_a_selected_joint_acquisition_contract_do_not_accumulate()
    {
        var frame = new Support.Frame(); var a = new Placeholder(); var b = new Placeholder();
        var state = Support.Create(frame, 1, 1)
            .Observe(a, false, true, new Placeholder())
            .Observe(b, false, true, new Placeholder());
        var read = Support.Read(state);
        Assert.Equal(2, read.Concentration); Assert.Equal(0, read.AcceptedEvents);
        Assert.Equal(2, read.UnresolvedReports);
    }

    [Fact]
    public void Separate_singleton_contracts_cannot_silently_be_pooled()
    {
        var frame = new Support.Frame(); var a = new Placeholder(); var b = new Placeholder();
        var onlyA = frame.Acquisition(a); var onlyB = frame.Acquisition(b);
        var state = Support.Create(frame, 1, 1, onlyA)
            .Observe(a, false, true, onlyA)
            .Observe(b, false, true, onlyB);
        var read = Support.Read(state);
        Assert.Equal(1, read.AcceptedEvents); Assert.Equal(1, read.UnresolvedReports);
        Assert.Equal(3, read.Concentration);
    }

    [Fact]
    public void Explicit_common_rate_product_contract_admits_distinct_events()
    {
        var frame = new Support.Frame(); var a = new Placeholder(); var b = new Placeholder();
        var basis = frame.Acquisition(a, b);
        Assert.True(Support.TryAcquisition(basis, frame, out var events));
        Assert.Equal(2, events.Length);
        var state = Support.Create(frame, 1, 1, basis)
            .Observe(a, false, true, basis)
            .Observe(b, false, false, basis);
        var read = Support.Read(state);
        Assert.Equal(2, read.A); Assert.Equal(2, read.B); Assert.Equal(2, read.AcceptedEvents);
        Assert.True(read.Complete);
        var nativeFactors = Entries((Relation)((Relation)basis.Right).Right);
        Assert.All(nativeFactors, item => Assert.Same(frame.Rate, ((Relation)((Relation)item).Right).Left));
    }

    [Fact]
    public void Display_partition_changes_neither_mean_nor_support()
    {
        var frame = new Support.Frame(); var state = Support.Create(frame, 1, 1);
        var coarse = Support.Read(state, 2); var fine = Support.Read(state, 100);
        Assert.Equal(coarse.Mean, fine.Mean); Assert.Equal(coarse.Variance, fine.Variance);
        Assert.Equal(coarse.Concentration, fine.Concentration);
        Assert.Equal(1, coarse.SelectedPartition); Assert.Equal(50, fine.SelectedPartition);
        Assert.Equal(2, coarse.Partitions); Assert.Equal(100, fine.Partitions);
        Assert.Same(state.Surface, ((Relation)coarse.Surface.Left).Left);
        Assert.Same(state.Surface, ((Relation)fine.Surface.Left).Left);
    }

    [Fact]
    public void Acquisition_sequence_is_exchangeable_under_the_selected_common_rate_premise()
    {
        var frame = new Support.Frame(); var a = new Placeholder(); var b = new Placeholder();
        var basis = frame.Acquisition(a, b);
        var left = Support.Read(Support.Create(frame, 2, 3, basis)
            .Observe(a, false, true, basis).Observe(b, false, false, basis));
        var right = Support.Read(Support.Create(frame, 2, 3, basis)
            .Observe(b, true, true, basis).Observe(a, true, false, basis));
        Assert.Equal(left.Mean, right.Mean); Assert.Equal(left.Variance, right.Variance);
        Assert.Equal(7, right.Concentration); Assert.Equal("3/7", right.Mean.Reduced);
    }

    [Fact]
    public void Native_transport_preserves_unreduced_participation_acquisition_aliases_and_replay()
    {
        var frame = new Support.Frame(); var a = new Placeholder(); var b = new Placeholder();
        var basis = frame.Acquisition(a, b);
        var state = Support.Create(frame, 3, 4, basis)
            .Observe(a, false, true, basis).Observe(a, true, false, basis)
            .ObserveUnknown(b, false, basis);
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("empirical-support", state.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var read = Support.Read(Assert.IsType<Relation>(restored.Root));
        Assert.Equal(4, read.A); Assert.Equal(4, read.B);
        Assert.Equal(1, read.AcceptedEvents); Assert.Equal(1, read.DuplicateReports);
        Assert.Equal(1, read.UnresolvedReports); Assert.Equal("1/2", read.Mean.Reduced);
        Assert.Equal(8, read.Concentration);
        Assert.False(read.Complete);
    }

    [Fact]
    public void Finite_mean_projection_keeps_its_unrepresented_remainder()
    {
        var frame = new Support.Frame();
        var read = Support.Read(Support.Create(frame, 2, 1), 2);
        Assert.Equal("2/3", read.Mean.Reduced); Assert.Equal(1, read.SelectedPartition);
        var resolution = (Relation)read.Resolution.Right;
        var selectedAndRemainder = (Relation)resolution.Right;
        Assert.Single(Entries((Relation)selectedAndRemainder.Right));
        Assert.Equal(2 * 2, read.SelectedPartition * read.Concentration +
            Entries((Relation)selectedAndRemainder.Right).Length);
    }

    [Fact]
    public void Acquisition_domain_is_not_observation_count_and_other_rate_is_not_accepted()
    {
        var frame = new Support.Frame(); var other = new Support.Frame(); var sample = new Placeholder();
        var domain = frame.Acquisition(sample, new Placeholder(), new Placeholder());
        var untouched = Support.Read(Support.Create(frame, 1, 1, domain));
        Assert.Equal(2, untouched.Concentration); Assert.Equal(0, untouched.AcceptedEvents);
        var foreign = other.Acquisition(sample);
        var read = Support.Read(Support.Create(frame, 1, 1, foreign)
            .Observe(sample, false, true, foreign));
        Assert.Equal(2, read.Concentration); Assert.Equal(1, read.UnresolvedReports);
    }

    [Fact]
    public void Negative_prior_or_repeated_acquisition_identity_violates_selected_contract()
    {
        var frame = new Support.Frame(); var sample = new Placeholder();
        Assert.Throws<ArgumentOutOfRangeException>(() => Support.Create(frame, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Support.Create(frame, 1, -1));
        Assert.Throws<ArgumentException>(() => frame.Acquisition(sample, sample));
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        void Visit(IElement item)
        { if (!seen.Add(item)) return; if (item is Relation relation) { Visit(relation.Left); Visit(relation.Right); } }
        Visit(root); return seen.ToArray();
    }
}
