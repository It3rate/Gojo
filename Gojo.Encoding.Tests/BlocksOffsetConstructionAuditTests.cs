using Gojo.Blocks;
using Gojo.Core;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Retained counterexamples from the experimental inventory. An offset-shaped
/// request and correspondence graph do not certify its supplied geometry.
/// These checks select the displayed coordinate chart's identity metric;
/// the specimen itself does not supply a calibration from its extent carrier.
/// </summary>
public class BlocksOffsetConstructionAuditTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OpenOffsetCandidateDoesNotEstablishParallelismOrANormalDisplacement(int restores)
    {
        var (source, candidate) = Sections("derived-path-open-offset", restores);
        var a = XY(R(source[0]).Left); var b = XY(R(source[0]).Right);
        var c = XY(R(candidate[0]).Left); var d = XY(R(candidate[0]).Right);
        var direction = Subtract(b, a); var otherDirection = Subtract(d, c);
        var displacement = Subtract(c, a);
        Assert.Equal(48, direction.X * otherDirection.Y - direction.Y * otherDirection.X);
        Assert.Equal(464, Dot(direction, displacement));
        Assert.Equal(260, Dot(displacement, displacement));
        // Even if distance 18 were calibrated to this chart, 260 != 18^2.
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CurvedOffsetCandidateDoesNotEstablishEvenItsInitialNormal(int restores)
    {
        var (source, candidate) = Sections("derived-path-curved-offset", restores);
        // Compact cubic: (start -> end) -> (first control -> second control).
        var start = XY(R(R(source[0]).Left).Left);
        var control = XY(R(R(source[0]).Right).Left);
        var candidateStart = XY(R(R(candidate[0]).Left).Left);
        var leg = Subtract(control, start);
        var tangent = (X: 3 * leg.X, Y: 3 * leg.Y);
        var displacement = Subtract(candidateStart, start);
        Assert.Equal(4080, Dot(tangent, displacement));
        Assert.Equal(340, Dot(displacement, displacement));
    }

    private static (IElement[] Source, IElement[] Candidate) Sections(string id, int restores)
    {
        IElement root = BlocksGlossaryExport.Structures().Single(s => s.Id == id).Structure;
        for (var i = 0; i < restores; i++) root = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        var sourcePath = R(F(R(root).Left)[0]);
        var candidatePath = R(R(root).Right);
        return (F(sourcePath.Left), F(candidatePath.Left));
    }
    private static (long X, long Y) XY(IElement point)
    {
        var coordinates = F(point).Select(e => Assert.IsType<CarrierLocation>(e)).ToArray();
        return (coordinates[0].Ticks, coordinates[1].Ticks);
    }
    private static (long X, long Y) Subtract((long X, long Y) a, (long X, long Y) b) => (a.X - b.X, a.Y - b.Y);
    private static long Dot((long X, long Y) a, (long X, long Y) b) => a.X * b.X + a.Y * b.Y;
    private static Relation R(IElement element) => Assert.IsType<Relation>(element);
    private static IElement[] F(IElement element)
    { Assert.True(R(element).TryReadOrderedGroup(out var fields)); return fields; }
}
