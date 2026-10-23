using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Intent = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentColourIntent;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentColourIntentTests
{
    [Fact]
    public void DesireDoesNotSupplyObservedSupportAndRetainsActualSelection()
    {
        var fixture = new Fixture();
        Assert.True(Intent.TryRead(fixture.Frame, fixture.Intent, out var read));
        Assert.Same(fixture.Source, read.Source);
        Assert.Same(fixture.Primary, read.Subject);
        Assert.Same(fixture.Secondary, read.Reference);
        Assert.Same(fixture.Scope, read.Selected);
        Assert.Null(Intent.Assess(fixture.Frame, fixture.Intent, StructuralConstraint.Open()).Supported);
        Assert.Null(Intent.Assess(fixture.Frame, fixture.Intent,
            fixture.Observe(StructuralConstraint.Open(fixture.Frame.Higher))).Supported);
    }

    [Fact]
    public void DisjointSupportOpposesAndMixedSupportRemainsOpen()
    {
        var f = new Fixture();
        Assert.True(Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Higher))).Supported);
        Assert.False(Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Lower, f.Frame.Equal))).Supported);
        Assert.Null(Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Higher, f.Frame.Equal))).Supported);
        Assert.Null(Intent.Assess(f.Frame, f.Intent, f.Observe(S())).Supported);
        Assert.Null(Intent.Assess(f.Frame, f.Intent, f.Observe(S(new Placeholder()))).Supported);
    }

    [Fact]
    public void ForeignFrameSourceAndCopiedRoleCannotStandInForPublicIdentity()
    {
        var f = new Fixture(); var other = new OrdinalReferenceFrame();
        Assert.False(Intent.TryRead(other, f.Intent, out _));
        Assert.Null(Intent.Assess(f.Frame, f.Intent,
            other.Observe(f.Source, f.Primary, f.Secondary, new Placeholder(), S(other.Higher))).Supported);
        Assert.Null(Intent.Assess(f.Frame, f.Intent,
            f.Frame.Observe(new Placeholder(), f.Primary, f.Secondary, new Placeholder(), S(f.Frame.Higher))).Supported);
        Assert.Null(Intent.Assess(f.Frame, f.Intent,
            f.Frame.Observe(f.Source, new Relation(f.Primary.Left, f.Primary.Right), f.Secondary,
                new Placeholder(), S(f.Frame.Higher))).Supported);
        var missing = Intent.Declare(f.Source, S(f.Primary), f.Primary, f.Secondary, f.Frame, S(f.Frame.Higher));
        Assert.False(Intent.TryRead(f.Frame, missing, out _));
    }

    [Fact]
    public void QuestionOrientationAndResponseAreIndependent()
    {
        var f = new Fixture();
        var yes = Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Higher)));
        var forward = Intent.ObserveAnswer(f.Frame, yes, new Placeholder());
        var opposed = Intent.ObserveAnswer(f.Frame, yes, new Placeholder(), askOpposed: true);
        Assert.True(StatisticalEvidence.TryReadObservation(forward, out var first));
        Assert.True(StatisticalEvidence.TryReadObservation(opposed, out var second));
        Assert.True(first.Positive); Assert.True(second.Positive);
        Assert.Same(first.Orientation.Left, second.Orientation.Right);
        Assert.Same(first.Orientation.Right, second.Orientation.Left);
        Assert.Same(first.Response.Left, second.Response.Left);
        var no = Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Lower)));
        Assert.True(StatisticalEvidence.TryReadObservation(
            Intent.ObserveAnswer(f.Frame, no, new Placeholder(), askOpposed: true), out var negative));
        Assert.False(negative.Positive);
        Assert.Same(negative.Orientation.Left, negative.Response.Left);
        var unknown = Intent.Assess(f.Frame, f.Intent, f.Observe(f.Frame.Domain));
        Assert.False(StatisticalEvidence.TryReadObservation(Intent.ObserveAnswer(f.Frame, unknown, new Placeholder()), out _));
    }

    [Fact]
    public void DuplicateIntentDoesNotGainVotesAndContradictionStaysUnresolved()
    {
        var f = new Fixture(); IElement model = new Placeholder();
        var frame = new StatisticalEvidence.Frame(f.Source, model);
        var calibration = CompatibilityEvidence.Calibrate(frame, 7, 8);
        var yes = Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Higher)));
        var no = Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Lower)));
        var duplicate = Intent.Aggregate(f.Source, model, [yes, yes, yes]);
        Assert.Equal(1, duplicate.Supported); Assert.Equal(0, duplicate.Unresolved);
        Assert.True(Intent.TryLikelihood(duplicate, calibration, out var probability, out _));
        Assert.True(StatisticalEvidence.TryReadPartition(probability, frame.Unit, out var value));
        Assert.Equal(new StatisticalEvidence.Ratio(7, 8), value);
        var contradiction = Intent.Aggregate(f.Source, model, [yes, no]);
        Assert.Equal(0, contradiction.Supported); Assert.Equal(0, contradiction.Contradicted);
        Assert.Equal(1, contradiction.Unresolved);
        Assert.True(Intent.TryLikelihood(contradiction, calibration, out probability, out _));
        Assert.True(StatisticalEvidence.TryReadPartition(probability, frame.Unit, out value));
        Assert.Equal(new StatisticalEvidence.Ratio(1, 2), value);
    }

    [Fact]
    public void AggregateRereadsNativeResultInsteadOfEditedDiagnostic()
    {
        var f = new Fixture();
        var yes = Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Higher)));
        var edited = yes with { Supported = false };
        var aggregate = Intent.Aggregate(f.Source, new Placeholder(), [edited]);
        Assert.Equal(1, aggregate.Supported); Assert.Equal(0, aggregate.Contradicted);
        var wrongSurface = edited with { Surface = StructuralConstraint.Open(yes.Surface) };
        Assert.Equal(1, Intent.Aggregate(f.Source, new Placeholder(), [wrongSurface]).Unresolved);
    }

    [Fact]
    public void ForgedFavourableResultCannotOverrideContraryObservation()
    {
        var f = new Fixture();
        var contrary = Intent.Assess(f.Frame, f.Intent, f.Observe(S(f.Frame.Lower)));
        var falseSupport = new Relation(S(f.Intent), new Relation(S(), S()));
        var forged = contrary with { Supported = true,
            Surface = new Relation(f.Intent, new Relation(contrary.Observation, falseSupport)) };
        var aggregate = Intent.Aggregate(f.Source, new Placeholder(), [forged]);
        Assert.Equal(0, aggregate.Supported); Assert.Equal(0, aggregate.Contradicted);
        Assert.Equal(1, aggregate.Unresolved);
        Assert.False(StatisticalEvidence.TryReadObservation(
            Intent.ObserveAnswer(f.Frame, forged, new Placeholder()), out _));
        var wrongReader = contrary with { Frame = new OrdinalReferenceFrame() };
        Assert.Equal(1, Intent.Aggregate(f.Source, new Placeholder(), [wrongReader]).Unresolved);
    }

    [Fact]
    public void MultipleAllowedOrdersRemainDesireAndEmptyDesireIsNotTruth()
    {
        var f = new Fixture();
        var broad = Intent.Declare(f.Source, f.Scope, f.Primary, f.Secondary, f.Frame,
            S(f.Frame.Equal, f.Frame.Higher));
        Assert.True(Intent.Assess(f.Frame, broad, f.Observe(S(f.Frame.Equal, f.Frame.Higher))).Supported);
        Assert.False(Intent.Assess(f.Frame, broad, f.Observe(S(f.Frame.Lower))).Supported);
        var absent = Intent.Declare(f.Source, f.Scope, f.Primary, f.Secondary, f.Frame, S());
        Assert.False(Intent.TryRead(f.Frame, absent, out _));
        var foreign = Intent.Declare(f.Source, f.Scope, f.Primary, f.Secondary, f.Frame, S(new Placeholder()));
        Assert.False(Intent.TryRead(f.Frame, foreign, out _));
    }

    private sealed class Fixture
    {
        public IElement Source { get; } = new Placeholder();
        public Relation Primary { get; } = new(new Placeholder(), new Placeholder());
        public Relation Secondary { get; } = new(new Placeholder(), new Placeholder());
        public OrdinalReferenceFrame Frame { get; } = new();
        public Relation Scope { get; }
        public Relation Intent { get; }
        public Fixture()
        {
            Scope = S(Primary, Secondary);
            Intent = OrnamentColourIntent.Declare(Source, Scope, Primary, Secondary, Frame,
                S(Frame.Higher), new Relation(Primary, Secondary));
        }
        public Relation Observe(Relation support) => Frame.Observe(Source, Primary, Secondary,
            new Placeholder(), support, new Relation(Primary, Secondary));
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
