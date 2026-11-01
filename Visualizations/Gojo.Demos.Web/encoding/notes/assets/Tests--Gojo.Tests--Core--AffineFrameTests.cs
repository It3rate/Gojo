using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public class AffineFrameTests
{
    [Theory]
    [InlineData("zero frame")]
    [InlineData("foreign sample")]
    [InlineData("foreign support")]
    [InlineData("reversed support")]
    [InlineData("outside support")]
    [InlineData("opposed coordinate")]
    [InlineData("foreign coordinate")]
    [InlineData("extra grade")]
    [InlineData("missing basis")]
    [InlineData("non-unit basis")]
    [InlineData("unknown basis")]
    public void InvalidPremisesStayAvailableAsUnresolvedSource(string defect)
    {
        var x = new Carrier(); var t = new Carrier(); var space = new Carrier();
        IElement basis = Relation.OrderedGroup(space, Relation.Segment(x, 0, 1));
        IElement sample = L(t, 1);
        IElement parameter = Relation.Segment(t, 0, 2);
        IElement support = Relation.Segment(t, 0, 2);
        IElement second = Group(L(x, 2));
        switch (defect)
        {
            case "zero frame": parameter = Relation.Segment(t, 1, 1); break;
            case "foreign sample": sample = L(new Carrier(), 1); break;
            case "foreign support": support = Relation.Segment(new Carrier(), 0, 2); break;
            case "reversed support": support = Relation.Segment(t, 2, 0); break;
            case "outside support": sample = L(t, 3); break;
            case "opposed coordinate": second = Group(L(x.Inverse(), 2)); break;
            case "foreign coordinate": second = Group(L(new Carrier(), 2)); break;
            case "missing basis": basis = new Placeholder(); break;
            case "non-unit basis": basis = Group(Relation.Segment(x, 0, 2)); break;
            case "unknown basis": basis = Group(Relation.Segment(Carrier.Unknown, 0, 1)); break;
        }
        var grade = defect == "extra grade" ? Group(sample, parameter, support, basis, new Placeholder())
            : Group(sample, parameter, support, basis);
        var request = TransformSurface.Interpolate(Group(L(x, 0)), second, grade, new Placeholder());
        Assert.False(AffineFrame.TryInterpolate(request, out var witness));
        Assert.Same(request, F(witness)[0]);
        Assert.IsType<Placeholder>(F(witness)[2]);
        Assert.Same(request, Assert.Single(F(F(witness)[3])));
    }

    [Fact]
    public void ZeroWeightDoesNotHideAnIncompatibleGroundedParticipant()
    {
        var x = new Carrier(); var t = new Carrier();
        var basis = Group(Relation.Segment(x, 0, 1));
        var grade = Group(L(t, 0), Relation.Segment(t, 0, 1), Relation.Segment(t, 0, 1), basis);
        var request = TransformSurface.Interpolate(Group(L(x, 4)), Group(L(new Carrier(), 6)), grade, new Placeholder());
        Assert.False(AffineFrame.TryInterpolate(request, out _));
        var open = TransformSurface.Interpolate(Group(L(x, 4)), new Placeholder(), grade, new Placeholder());
        Assert.True(AffineFrame.TryInterpolate(open, out var read));
        Scalar(F(F(read)[2])[0], 4, 1);
    }

    [Fact]
    public void SameOpenCoordinateCancelsButDifferentCoordinatesDoNot()
    {
        var x = new Carrier(); var y = new Carrier(); var shared = new Placeholder();
        var basis = Group(Relation.Segment(x, 0, 1), Relation.Segment(y, 0, 1));
        var first = Group(shared, new Placeholder());
        var second = Group(shared, new Placeholder());
        Assert.False(AffineFrame.TryDifference(Group(first, second, basis), out var read));
        var coordinates = F(F(read)[2]);
        Scalar(coordinates[0], 0, 1);
        Assert.IsType<Placeholder>(coordinates[1]);
        Assert.Single(F(F(read)[3]));
        var wholePoint = new Placeholder();
        Assert.True(AffineFrame.TryDifference(Group(wholePoint, wholePoint, basis), out var same));
        Assert.All(F(F(same)[2]), coordinate => Scalar(coordinate, 0, 1));
        Assert.False(AffineFrame.TryDifference(Group(wholePoint, new Placeholder(), basis), out _));
    }

    [Fact]
    public void ExplicitExposureCanSupplyTheBasisForAStillOpenPoint()
    {
        var x = new Carrier(); var openPoint = new Placeholder();
        var motif = new Relation(openPoint, openPoint);
        var maps = Group(new Relation(Relation.Segment(x, 0, 1), Relation.Segment(x, 2, 3)));
        var request = Group(motif, Group(openPoint), maps, Group(Relation.Segment(x, 0, 1)));
        Assert.False(AffineFrame.TryMapExposed(request, out var read));
        var mapped = Assert.IsType<Relation>(F(read)[2]);
        Assert.Same(mapped.Left, mapped.Right);
        Assert.IsType<Placeholder>(Assert.Single(F(mapped.Left)));
        Assert.Same(openPoint, Assert.IsType<Relation>(F(F(F(read)[4])[0])[0]).Left);
    }

    [Fact]
    public void AWholeMissingPointDoesNotAssertEqualCoordinateSlots()
    {
        var x = new Carrier(); var y = new Carrier(); var t = new Carrier();
        var basis = Group(Relation.Segment(x, 0, 1), Relation.Segment(y, 0, 1));
        var grade = Group(L(t, 1), Relation.Segment(t, 0, 2), Relation.Segment(t, 0, 2), basis);
        var request = TransformSurface.Interpolate(new Placeholder(), Group(L(x, 1), L(y, 2)), grade, new Placeholder());
        Assert.False(AffineFrame.TryInterpolate(request, out var read));
        var result = F(F(read)[2]);
        Assert.NotSame(result[0], result[1]);
        Assert.Equal(2, F(F(read)[3]).Length);
    }

    [Fact]
    public void IntermediateProductsUseWideIntegersAndUnencodableResultsStayOpen()
    {
        var x = new Carrier(); var t = new Carrier(); var basis = Group(Relation.Segment(x, 0, 1));
        var grade = Group(L(t, 0), Relation.Segment(t, long.MinValue, long.MaxValue),
            Relation.Segment(t, long.MinValue, long.MaxValue), basis);
        var request = TransformSurface.Interpolate(Group(L(x, long.MinValue)), Group(L(x, long.MaxValue)), grade, new Placeholder());
        Assert.True(AffineFrame.TryInterpolate(request, out var read));
        Scalar(F(F(read)[2])[0], 0, 1);
        var overflow = Group(Group(L(x, long.MinValue)), Group(L(x, long.MaxValue)), basis);
        Assert.False(AffineFrame.TryDifference(overflow, out var unresolved));
        Assert.Same(overflow, F(unresolved)[0]);
        Assert.IsType<Placeholder>(F(unresolved)[2]);
    }

    [Fact]
    public void ProjectionHonorsCalibrationBetweenDifferentAxesAndKeepsFractions()
    {
        var x = new Carrier(); var output = new Carrier();
        var basis = Group(Relation.Segment(output, 0, 1));
        var map = new Relation(Relation.Segment(x, 2, 5), Relation.Segment(output, 10, 12));
        var request = Group(Group(L(x, 3)), Group(map), basis);
        Assert.True(AffineFrame.TryProject(request, out var witness));
        var coordinate = F(F(witness)[2])[0];
        Scalar(coordinate, 32, 3);
        Assert.True(ProjectionResidue.TryRead(coordinate, out var actual, out _, out _));
        Assert.Same(output, actual);
        var reverse = new Relation(map.Right, map.Left);
        Assert.True(AffineFrame.TryProject(Group(F(witness)[2], Group(reverse), Group(Relation.Segment(x, 0, 1))), out var back));
        Scalar(F(F(back)[2])[0], 3, 1);
        Assert.Equal(0UL, x.Value); Assert.Equal(0UL, output.Value);
    }

    [Fact]
    public void OrderedAxesAndStrictArityPreventPlausibleFalseReads()
    {
        var x = new Carrier(); var y = new Carrier();
        var basis = Group(Relation.Segment(x, 0, 1), Relation.Segment(y, 0, 1));
        var a = Group(L(x, 2), L(y, 3)); var swapped = Group(L(y, 3), L(x, 2));
        Assert.False(AffineFrame.TryDifference(Group(a, swapped, basis), out _));
        var duplicated = Group(Relation.Segment(x, 0, 1), Relation.Segment(x, 0, 1));
        Assert.False(AffineFrame.TryDifference(Group(Group(L(x, 2), L(x, 3)), a, duplicated), out _));
        Assert.False(AffineFrame.TryDifference(Group(a, a, basis, new Placeholder()), out _));
        var maps = Group(new Relation(Relation.Segment(x, 0, 0), Relation.Segment(x, 0, 1)),
            new Relation(Relation.Segment(y, 0, 1), Relation.Segment(y, 0, 1)));
        Assert.False(AffineFrame.TryProject(Group(a, maps, basis), out _));
    }

    [Fact]
    public void OutputClaimIsRetainedAndNotSilentlyOverwrittenOrDeclaredCorrect()
    {
        var x = new Carrier(); var t = new Carrier(); var claim = Group(L(x, 999));
        var grade = Group(L(t, 1), Relation.Segment(t, 0, 2), Relation.Segment(t, 0, 2), Group(Relation.Segment(x, 0, 1)));
        var request = TransformSurface.Interpolate(Group(L(x, 0)), Group(L(x, 4)), grade, claim);
        Assert.True(AffineFrame.TryInterpolate(request, out var witness));
        Scalar(F(F(witness)[2])[0], 2, 1);
        Assert.Same(claim, Assert.IsType<Relation>(F(witness)[4]).Left);
        Assert.Same(F(witness)[2], Assert.IsType<Relation>(F(witness)[4]).Right);
        Scalar(F(claim)[0], 999, 1);
    }

    [Fact]
    public void RationalParameterWorksInThreeExplicitlyOrderedDimensions()
    {
        var x = new Carrier(); var y = new Carrier(); var z = new Carrier(); var t = new Carrier();
        var basis = Group(Relation.Segment(x, 0, 1), Relation.Segment(y, 0, 1), Relation.Segment(z, 0, 1));
        var grade = Group(ProjectionResidue.Create(t, 1, 3), Relation.Segment(t, 0, 1), Relation.Segment(t, 0, 1), basis);
        var first = Group(L(x, 0), L(y, -5), L(z, 4));
        var second = Group(L(x, 3), L(y, 4), L(z, 1));
        Assert.True(AffineFrame.TryInterpolate(TransformSurface.Interpolate(first, second, grade, new Placeholder()), out var read));
        var result = F(F(read)[2]);
        Scalar(result[0], 1, 1); Scalar(result[1], -2, 1); Scalar(result[2], 3, 1);
    }

    [Fact]
    public void ConstantProjectionRetainsItsSourceEvenWhenEveryOutputIsKnown()
    {
        var source = new Carrier(); var target = new Carrier();
        var coordinate = L(source, 4); var point = Group(coordinate); var other = Group(L(source, 8));
        var map = new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 5, 5));
        var basis = Group(Relation.Segment(target, 0, 1));
        Assert.True(AffineFrame.TryProject(Group(point, Group(map), basis), out var read));
        Scalar(F(F(read)[2])[0], 5, 1);
        Assert.Same(coordinate, Assert.IsType<Relation>(Assert.Single(F(F(read)[3]))).Left);
        Assert.True(AffineFrame.TryMapExposed(Group(new Relation(point, other), Group(point, other), Group(map), basis), out var mapping));
        Assert.Equal(2, F(F(mapping)[3]).Length);
        var result = Assert.IsType<Relation>(F(mapping)[2]);
        Scalar(F(result.Left)[0], 5, 1); Scalar(F(result.Right)[0], 5, 1);
        Assert.NotSame(result.Left, result.Right);
    }

    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static IElement L(Carrier carrier, long ticks) => new CarrierLocation(carrier, ticks);
    private static IElement[] F(IElement element)
    {
        Assert.True(Assert.IsType<Relation>(element).TryReadOrderedGroup(out var fields)); return fields;
    }
    private static void Scalar(IElement element, long n, long d)
    {
        Assert.True(ProjectionResidue.TryRead(element, out _, out var numerator, out var denominator));
        Assert.Equal(n, numerator); Assert.Equal(d, denominator);
    }
}
