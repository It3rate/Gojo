using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace QualitativeExecution.Tests;

public sealed class CompatibilityEvidenceTests
{
    [Fact]
    public void Native_partitions_match_an_independent_exact_mixture_oracle()
    {
        var model = new Placeholder(); var source = new Placeholder();
        var frame = new Stats.Frame(new Placeholder(), model);
        foreach (var (hit, denominator) in new[] { (3, 4), (15, 16), (1, 2) })
        {
            var calibration = CompatibilityEvidence.Calibrate(frame, hit, denominator);
            for (var s = 0; s <= 3; s++) for (var c = 0; c <= 3; c++) for (var u = 0; u <= 3; u++)
            {
                if (s + c + u == 0) continue;
                var assessment = CompatibilityEvidence.Assess(source, model, Checks(s), Checks(c), Checks(u));
                Assert.True(CompatibilityEvidence.TryReadLikelihood(assessment, calibration, out var partition, out var receipt));
                Assert.True(Stats.TryReadPartition(partition, frame.Unit, out var actual));
                var expected = new Stats.Ratio(2 * hit * s + 2 * (denominator - hit) * c + denominator * u,
                    2 * denominator * (s + c + u));
                Assert.Equal(expected, actual);
                Assert.Same(assessment, ((Relation)receipt.Left).Left);
                Assert.Same(calibration, ((Relation)receipt.Left).Right);
            }
        }
    }

    [Fact]
    public void Repeated_routes_and_native_placeholder_aliases_count_once()
    {
        var model = new Placeholder(); var check = new Placeholder(); var contrary = new Placeholder();
        var frame = new Stats.Frame(new Placeholder(), model);
        var calibration = CompatibilityEvidence.Calibrate(frame, 3, 4);
        var ordinary = CompatibilityEvidence.Assess(new Placeholder(), model, S(check), S(contrary), S());
        var repeated = CompatibilityEvidence.Assess(ordinary.Left, model,
            S(check, check, check.Dual().Dual()), S(contrary, contrary), S());
        Assert.Equal(Read(frame, calibration, ordinary), Read(frame, calibration, repeated));
        Assert.Equal(new Stats.Ratio(1, 2), Read(frame, calibration, repeated));
    }

    [Fact]
    public void Opposed_faces_remain_distinct_checks()
    {
        var model = new Placeholder(); var check = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), model);
        var calibration = CompatibilityEvidence.Calibrate(frame, 3, 4);
        var assessment = CompatibilityEvidence.Assess(new Placeholder(), model, S(check, check.Dual()), Checks(1), S());
        Assert.Equal(new Stats.Ratio(7, 12), Read(frame, calibration, assessment));
    }

    [Fact]
    public void Conflicting_classifications_become_one_unknown_and_keep_original_evidence()
    {
        var model = new Placeholder(); var check = new Placeholder(); var other = new Placeholder();
        var frame = new Stats.Frame(new Placeholder(), model); var calibration = CompatibilityEvidence.Calibrate(frame, 3, 4);
        var assessment = CompatibilityEvidence.Assess(new Placeholder(), model, S(check, other), S(check.Dual().Dual()), S());
        Assert.True(CompatibilityEvidence.TryReadLikelihood(assessment, calibration, out var partition, out var receipt));
        Assert.True(Stats.TryReadPartition(partition, frame.Unit, out var actual));
        Assert.Equal(new Stats.Ratio(5, 8), actual);
        Assert.Same(assessment, ((Relation)receipt.Left).Left);
        var normalized = (Relation)((Relation)receipt.Right).Left;
        Assert.Single(Entries((Relation)normalized.Left));
        Assert.Empty(Entries((Relation)((Relation)normalized.Right).Left));
        Assert.Single(Entries((Relation)((Relation)normalized.Right).Right));
    }

    [Fact]
    public void A_report_marked_both_supported_and_unresolved_stays_unresolved()
    {
        var model = new Placeholder(); var check = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), model);
        var assessment = CompatibilityEvidence.Assess(new Placeholder(), model, S(check), S(), S(check));
        Assert.Equal(new Stats.Ratio(1, 2), Read(frame, CompatibilityEvidence.Calibrate(frame, 15, 16), assessment));
    }

    [Fact]
    public void Empty_and_open_groups_preserve_an_open_read_without_inventing_agreement()
    {
        var model = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), model);
        var calibration = CompatibilityEvidence.Calibrate(frame, 3, 4);
        foreach (var checks in new[] { S(), StructuralConstraint.Open(new Placeholder()) })
        {
            var assessment = CompatibilityEvidence.Assess(new Placeholder(), model, checks, S(), S());
            Assert.False(CompatibilityEvidence.TryReadLikelihood(assessment, calibration, out var partition, out var receipt));
            Assert.False(Stats.TryReadPartition(partition, frame.Unit, out _));
            Assert.Same(assessment, ((Relation)receipt.Left).Left);
        }
    }

    [Fact]
    public void Calibration_requires_one_native_unit_complement_and_neutral_half()
    {
        var model = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), model);
        var foreign = new Stats.Frame(new Placeholder(), model);
        var valid = CompatibilityEvidence.Calibrate(frame, 3, 4);
        var assessment = CompatibilityEvidence.Assess(new Placeholder(), model, Checks(1), S(), S());
        var invalid = new[] {
            new Relation(frame.Surface, new Relation(foreign.Weight(3, 4), new Relation(frame.Weight(1, 4), frame.Weight(1, 2)))),
            new Relation(frame.Surface, new Relation(frame.Weight(3, 4), new Relation(frame.Weight(1, 2), frame.Weight(1, 2)))),
            new Relation(frame.Surface, new Relation(frame.Weight(3, 4), new Relation(frame.Weight(1, 4), frame.Weight(1, 3))))
        };
        foreach (var calibration in invalid)
            Assert.False(CompatibilityEvidence.TryReadLikelihood(assessment, calibration, out _, out _));
        Assert.True(CompatibilityEvidence.TryReadLikelihood(assessment, valid, out _, out _));
    }

    [Fact]
    public void Foreign_models_cannot_use_a_calibrated_categorical_frame()
    {
        var frame = new Stats.Frame(new Placeholder(), new Placeholder());
        var assessment = CompatibilityEvidence.Assess(new Placeholder(), new Placeholder(), Checks(1), S(), S());
        Assert.False(CompatibilityEvidence.TryReadLikelihood(assessment,
            CompatibilityEvidence.Calibrate(frame, 3, 4), out _, out _));
    }

    [Fact]
    public void An_exact_mean_beyond_partition_capacity_stays_open()
    {
        var model = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), model);
        var assessment = CompatibilityEvidence.Assess(new Placeholder(), model, Checks(1), S(), Checks(72));
        Assert.False(CompatibilityEvidence.TryReadLikelihood(assessment,
            CompatibilityEvidence.Calibrate(frame, 1, 997), out var partition, out var receipt));
        Assert.False(Stats.TryReadPartition(partition, frame.Unit, out _));
        Assert.Same(assessment, ((Relation)receipt.Left).Left);
    }

    [Fact]
    public void Native_transport_preserves_calibration_and_alias_deduplication_without_helper_state()
    {
        var model = new Placeholder(); var check = new Placeholder();
        var frame = new Stats.Frame(new Placeholder(), model);
        var assessment = CompatibilityEvidence.Assess(new Placeholder(), model,
            S(check, check.Dual().Dual()), Checks(1), S());
        var packet = new Relation(CompatibilityEvidence.Calibrate(frame, 3, 4), assessment);
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("compatibility-test", packet));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var decoded));
        var restored = Assert.IsType<Relation>(decoded.Root);
        var calibration = Assert.IsType<Relation>(restored.Left);
        Assert.True(Stats.Frame.TryRead(Assert.IsType<Relation>(calibration.Left), out var restoredFrame));
        Assert.Equal(new Stats.Ratio(1, 2), Read(restoredFrame, calibration, Assert.IsType<Relation>(restored.Right)));
        Assert.NotSame(frame.Unit, restoredFrame.Unit);
    }

    private static Stats.Ratio Read(Stats.Frame frame, Relation calibration, Relation assessment)
    {
        Assert.True(CompatibilityEvidence.TryReadLikelihood(assessment, calibration, out var partition, out _));
        Assert.True(Stats.TryReadPartition(partition, frame.Unit, out var ratio)); return ratio;
    }
    private static Relation Checks(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] Entries(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var rows, out _, out _)); return rows; }
}
