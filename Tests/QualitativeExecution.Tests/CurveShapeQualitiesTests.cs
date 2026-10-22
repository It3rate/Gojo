using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Qualities = Gojo.Experiments.QualitativeExecution.CurveShapeQualities;

namespace QualitativeExecution.Tests;

public sealed class CurveShapeQualitiesTests
{
    [Fact]
    public void Metric_dependent_dials_remain_open_without_the_selected_similarity_premise()
    {
        var frame = Frame(); var bow = Bow(frame);
        var read = Qualities.Read(bow);
        Assert.Null(read.Strength); Assert.Null(read.StartHeading.HorizontalShare); Assert.Null(read.StartHeading.Diagonal);
        Assert.Equal(3, read.Samples.Count); Assert.All(read.Samples, sample => Assert.Null(sample.SignedExcursion));
        Assert.Contains(bow.Section, Reachable(read.Surface));
    }

    [Theory]
    [InlineData(3, 3, true, 1, 2)]
    [InlineData(-3, 3, true, 1, 2)]
    [InlineData(3, 0, false, 1, 1)]
    [InlineData(0, 3, false, 0, 1)]
    [InlineData(3, 6, false, 1, 3)]
    public void Diagonal_means_equal_axis_participation_in_the_selected_basis(int x, int y, bool diagonal, int numerator, int denominator)
    {
        var frame = Frame();
        var line = frame.Read(frame.Line(new Placeholder(), new Placeholder(), frame.Word(0, 0), frame.Word(x, y)));
        var quality = Qualities.Read(line, Qualities.SimilarityBasis(frame));
        Assert.Equal(diagonal, quality.StartHeading.Diagonal);
        Ratio(quality.StartHeading.HorizontalShare, numerator, denominator);
    }

    [Fact]
    public void A_foreign_or_incomplete_similarity_basis_does_not_supply_metric_evidence()
    {
        var frame = Frame(); var bow = Bow(frame);
        var foreign = Qualities.SimilarityBasis(Frame());
        Assert.Null(Qualities.Read(bow, foreign).Strength);
        var complete = Qualities.SimilarityBasis(frame);
        var incomplete = new Relation(complete.Left, StructuralConstraint.Open(complete.Right));
        Assert.Null(Qualities.Read(bow, incomplete).Strength);
    }

    [Fact]
    public void Chord_relative_profile_preserves_three_signed_samples_and_the_finite_strength()
    {
        var frame = Frame(); var bow = Bow(frame); var basis = Qualities.SimilarityBasis(frame);
        var read = Qualities.Read(bow, basis);
        Ratio(read.Samples[0].SignedExcursion, 9, 32); Ratio(read.Samples[1].SignedExcursion, 3, 8);
        Ratio(read.Samples[2].SignedExcursion, 9, 32); Ratio(read.Strength, 3, 8);
        var root = Reachable(read.Surface);
        Assert.Contains(bow.ControlWords, root); Assert.Contains(basis, root);
        var scheme = (Relation)((Relation)bow.Section.Right).Right;
        var parameter = ((Relation)((Relation)((Relation)scheme.Left).Right).Right).Left;
        Assert.Contains(parameter, root);
    }

    [Fact]
    public void Profile_is_scale_free_but_reflection_and_traversal_retain_signed_sides()
    {
        var frame = Frame(); var basis = Qualities.SimilarityBasis(frame); var bow = Bow(frame);
        var scaled = Bow(frame, 2); var read = Qualities.Read(bow, basis);
        var enlarged = Qualities.Read(scaled, basis);
        var reflected = Qualities.Read(frame.Read(frame.Reflect(bow.Section)), basis);
        var reversed = Qualities.Read(frame.Read(frame.Reverse(bow.Section)), basis);
        Ratio(enlarged.Strength, 3, 8); Ratio(reflected.Strength, 3, 8); Ratio(reversed.Strength, 3, 8);
        for (var index = 0; index < 3; index++)
        {
            var original = read.Samples[index].SignedExcursion!;
            Assert.Equal(-original.Numerator, reflected.Samples[index].SignedExcursion!.Numerator);
            Assert.Equal(-original.Numerator, reversed.Samples[2 - index].SignedExcursion!.Numerator);
        }
    }

    [Fact]
    public void A_stronger_S_profile_can_be_read_without_mistaking_it_for_one_bend_side()
    {
        var frame = Frame(); var basis = Qualities.SimilarityBasis(frame);
        Curve.Read Shape(int height) => frame.Read(frame.Cubic(new Placeholder(), new Placeholder(),
            frame.Word(0, 0), frame.Word(2, height), frame.Word(4, -height), frame.Word(6, 0)));
        var weak = Qualities.Read(Shape(2), basis); var strong = Qualities.Read(Shape(4), basis);
        Ratio(weak.Strength, 3, 32); Ratio(strong.Strength, 3, 16);
        Assert.True(strong.Samples[0].SignedExcursion!.Numerator.Sign > 0);
        Ratio(strong.Samples[1].SignedExcursion, 0, 1);
        Assert.True(strong.Samples[2].SignedExcursion!.Numerator.Sign < 0);
    }

    [Fact]
    public void Zero_chord_and_open_curve_do_not_manufacture_normalized_profile_or_heading()
    {
        var frame = Frame(); var basis = Qualities.SimilarityBasis(frame); var point = frame.Word(0, 0);
        var loop = frame.Read(frame.Cubic(new Placeholder(), new Placeholder(), point, frame.Word(4, 5), frame.Word(-4, 5), point));
        Assert.Null(Qualities.Read(loop, basis).Strength);
        var open = frame.Read(frame.Cubic(new Placeholder(), new Placeholder(), point,
            StructuralConstraint.Open(), frame.Word(4, 4), frame.Word(8, 0)));
        var read = Qualities.Read(open, basis);
        Assert.Empty(read.Samples); Assert.Null(read.Strength); Assert.Null(read.StartHeading.Diagonal); Assert.Null(read.Retracing);
    }

    [Fact]
    public void Source_structure_governs_a_read_even_when_a_diagnostic_record_is_edited()
    {
        var frame = Frame(); var source = Bow(frame);
        var forged = source with { Controls = [new(0, 0), new(8, 0)], Complete = false };
        Ratio(Qualities.Read(forged, Qualities.SimilarityBasis(frame)).Strength, 3, 8);
    }

    [Fact]
    public void A_profile_beyond_the_selected_participation_capacity_preserves_an_open_read()
    {
        var frame = Frame();
        var huge = Cubic(frame, new Placeholder(), new Placeholder(), (0, 0), (0, 10000), (1, 10000), (1, 0));
        var read = Qualities.Read(huge, Qualities.SimilarityBasis(frame));
        Assert.Null(read.Strength); Assert.All(read.Samples, sample => Assert.Null(sample.SignedExcursion));
        Assert.Contains(huge.Section, Reachable(read.Surface));
    }

    [Fact]
    public void Derivative_sign_samples_certify_retracing_but_opposed_derivative_controls_alone_do_not()
    {
        var frame = Frame();
        Curve.Read Shape(int a, int b, int c, int d) => frame.Read(frame.Cubic(new Placeholder(), new Placeholder(),
            frame.Word(a, 0), frame.Word(b, 0), frame.Word(c, 0), frame.Word(d, 0)));
        Assert.True(Qualities.Read(Shape(0, 4, -4, 0)).Retracing);
        Assert.False(Qualities.Read(Shape(0, 2, 4, 8)).Retracing);
        // Derivative controls are 4,-1,4, but every sampled derivative is
        // positive. The sufficient test preserves unknown rather than claiming
        // that opposite control directions establish an actual reversal.
        Assert.Null(Qualities.Read(Shape(0, 4, 3, 7)).Retracing);
        Assert.Null(Qualities.Read(Bow(frame)).Retracing);
    }

    [Fact]
    public void Tangency_magnitude_balance_and_opposed_bending_are_distinct_local_reads()
    {
        var frame = Frame(); var site = new Placeholder(); var basis = Qualities.SimilarityBasis(frame);
        var incoming = Cubic(frame, new Placeholder(), site, (-4, 0), (-3, 0), (-1, 1), (0, 0));
        var opposite = Cubic(frame, site, new Placeholder(), (0, 0), (1, -1), (3, 0), (4, 0));
        var same = Cubic(frame, site, new Placeholder(), (0, 0), (1, -1), (1, -4), (4, -4));
        var opposed = Qualities.Join(incoming, opposite, basis); var continuing = Qualities.Join(incoming, same, basis);
        Assert.True(opposed.Connected); Assert.True(opposed.Smooth); Assert.True(opposed.EqualCurvature); Assert.True(opposed.OpposedBending);
        Assert.True(continuing.EqualCurvature); Assert.False(continuing.OpposedBending);
        Ratio(opposed.IncomingSquaredCurvature, 1, 2); Ratio(opposed.OutgoingSquaredCurvature, 1, 2);
        Assert.Contains(incoming.ControlWords, Reachable(opposed.Surface)); Assert.Contains(opposite.ControlWords, Reachable(opposed.Surface));
    }

    [Fact]
    public void A_forward_tangent_join_can_have_unequal_local_curvature()
    {
        var frame = Frame(); var site = new Placeholder(); var basis = Qualities.SimilarityBasis(frame);
        var incoming = frame.Read(frame.Line(new Placeholder(), site, frame.Word(-4, 0), frame.Word(0, 0)));
        var outgoing = Cubic(frame, site, new Placeholder(), (0, 0), (1, 0), (3, 2), (4, 4));
        var joined = Qualities.Join(incoming, outgoing, basis);
        Assert.True(joined.Smooth); Assert.False(joined.EqualCurvature); Assert.False(joined.OpposedBending);
        Ratio(joined.IncomingSquaredCurvature, 0, 1); Ratio(joined.OutgoingSquaredCurvature, 16, 9);
    }

    [Fact]
    public void Zero_endpoint_derivative_does_not_turn_a_tangent_limit_into_curvature()
    {
        var frame = Frame(); var site = new Placeholder(); var basis = Qualities.SimilarityBasis(frame);
        var incoming = Cubic(frame, new Placeholder(), site, (-4, 0), (-3, 0), (0, 0), (0, 0));
        var outgoing = Cubic(frame, site, new Placeholder(), (0, 0), (0, 0), (2, 0), (4, 2));
        var joined = Qualities.Join(incoming, outgoing, basis);
        Assert.True(joined.Smooth); Assert.Null(joined.EqualCurvature); Assert.Null(joined.OpposedBending);
        Assert.Null(joined.IncomingSquaredCurvature); Assert.Null(joined.OutgoingSquaredCurvature);
    }

    [Fact]
    public void Coincidence_foreign_bases_and_corners_do_not_authorize_tangent_balance()
    {
        var frame = Frame(); var basis = Qualities.SimilarityBasis(frame); var site = new Placeholder();
        var incoming = frame.Read(frame.Line(new Placeholder(), site, frame.Word(-4, 0), frame.Word(0, 0)));
        var coincident = frame.Read(frame.Line(new Placeholder(), new Placeholder(), frame.Word(0, 0), frame.Word(4, 0)));
        var corner = frame.Read(frame.Line(site, new Placeholder(), frame.Word(0, 0), frame.Word(0, 4)));
        var tangent = frame.Read(frame.Line(site, new Placeholder(), frame.Word(0, 0), frame.Word(4, 0)));
        Assert.False(Qualities.Join(incoming, coincident, basis).Connected);
        Assert.Null(Qualities.Join(incoming, coincident, basis).EqualCurvature);
        Assert.Null(Qualities.Join(incoming, corner, basis).EqualCurvature);
        Assert.Null(Qualities.Join(incoming, tangent).EqualCurvature);
        Assert.Null(Qualities.Join(incoming, tangent, Qualities.SimilarityBasis(Frame())).EqualCurvature);
    }

    [Fact]
    public void Native_profile_transport_retains_operands_partitions_and_shared_source_parameter()
    {
        var frame = Frame(); var quality = Qualities.Read(Bow(frame), Qualities.SimilarityBasis(frame));
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("curve-profile", quality.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var imported));
        var roots = Reachable(imported.Root);
        Assert.NotEmpty(roots.OfType<Relation>()); Assert.NotEmpty(roots.OfType<Placeholder>());
        Assert.All(roots.OfType<Carrier>(), carrier => { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); });
        var fraction = quality.Samples[1].SignedExcursion!;
        var body = Assert.IsType<Relation>(fraction.Surface.Right);
        var signed = Assert.IsType<Relation>(body.Left);
        Assert.Equal(3, Entries(Assert.IsType<Relation>(signed.Left)).Length);
        Assert.Empty(Entries(Assert.IsType<Relation>(signed.Right)));
        Assert.Equal(8, Entries(Assert.IsType<Relation>(body.Right)).Length);
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Curve.Read Bow(Curve.Frame frame, int scale = 1) => Cubic(frame, new Placeholder(), new Placeholder(),
        (0, 0), (2 * scale, 4 * scale), (6 * scale, 4 * scale), (8 * scale, 0));
    private static Curve.Read Cubic(Curve.Frame frame, IElement a, IElement b, params (int X, int Y)[] values) =>
        frame.Read(frame.Cubic(a, b, frame.Word(values[0].X, values[0].Y), frame.Word(values[1].X, values[1].Y),
            frame.Word(values[2].X, values[2].Y), frame.Word(values[3].X, values[3].Y)));
    private static void Ratio(Qualities.Fraction? value, long numerator, long denominator)
    { Assert.NotNull(value); Assert.Equal(new BigInteger(numerator), value.Numerator); Assert.Equal(new BigInteger(denominator), value.Denominator); }
    private static IElement[] Entries(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var items, out var seam, out var tail)); Assert.Equal(seam, tail); return items; }
    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        void Visit(IElement item) { if (!seen.Add(item)) return; if (item is Relation relation) { Visit(relation.Left); Visit(relation.Right); } }
        Visit(root); return seen.ToArray();
    }
}
