using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxMaterialRefinementTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredRefinementAndBorrowUseOneCalibrationAndPreserveMaterial(bool restore)
    {
        var roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "Syntax", "material-refinement.gojo"))).Roots.ToArray();
        if (restore) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        Assert.True(Assert.IsType<Relation>(roots[0]).TryReadOrderedGroup(out var participants));
        var coarse = Assert.IsType<Carrier>(participants[0]);
        var fine = Assert.IsType<Carrier>(participants[1]);
        coarse.Add(3);
        Assert.True(RefinementTransfer.TryApply(Assert.IsType<Relation>(roots[1]), out _));
        Assert.Equal(1UL, coarse.Value); Assert.Equal(20UL, fine.Value);
        fine.Remove(3);
        Assert.True(RefinementTransfer.TryApply(Assert.IsType<Relation>(roots[2]), out _));
        Assert.Equal(2UL, coarse.Value); Assert.Equal(7UL, fine.Value);
        fine.Remove(10);
        Assert.Equal(-3, fine.Tension);
        Assert.True(RefinementTransfer.TryBorrow(Assert.IsType<Relation>(roots[3]), out _));
        Assert.Equal(1UL, coarse.Value); Assert.Equal(7UL, fine.Value); Assert.Equal(0, fine.Tension);
        Assert.False(RefinementTransfer.TryApply(Assert.IsType<Relation>(roots[4]), out var unresolved));
        Assert.True(ReadWitness.TryRead(unresolved, out _, out _, out var result, out _, out _));
        Assert.IsType<Placeholder>(result);
    }

    [Theory]
    [InlineData("subdivision(unit_of($a), 0)")]
    [InlineData("subdivision(unit_of($a), -1)")]
    [InlineData("subdivision(subdivision(unit_of($a), 9223372036854775807), 2)")]
    public void InvalidSubdivisionPromisesReportLocatedSyntaxErrors(string unit)
    {
        Assert.Throws<GojoSyntaxException>(() => GojoSyntax.Parse($"gojo 1; let $a = carrier(); root carrier({unit});"));
    }
}
