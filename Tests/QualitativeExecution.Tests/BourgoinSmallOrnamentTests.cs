using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinSmallOrnamentTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Sources = new(() => new());

    [Theory]
    [InlineData("B15", 2)]
    [InlineData("B16", 3)]
    [InlineData("B17", 2)]
    [InlineData("B18", 2)]
    public void TinyMeshDetailHasSparseFourChordAccentsRatherThanGrowingCornerFans(string id, int addedCount)
    {
        var source = new BourgoinPatternRefinement(Sources.Value.Recipes.Single(recipe => recipe.Id == id), 4).Result;
        var design = Assert.Single(BourgoinOrnamentDesignLibrary.Create(source, 0, ["detail"]));
        Assert.Equal("detail", design.Mode); Assert.Same(source, design.Source);
        Assert.True(design.Enrichment.IsComplete);
        var sourcePaths = E(design.Enrichment.Source.Paths);
        var resultPaths = E(design.Enrichment.Result.Paths);
        foreach (var actual in sourcePaths) Assert.Contains(resultPaths, path => ReferenceEquals(path, actual));
        var added = resultPaths.Where(path => !sourcePaths.Any(actual => ReferenceEquals(actual, path))).Cast<Relation>().ToArray();
        Assert.Equal(addedCount, added.Length);
        Assert.All(added, path =>
        {
            Assert.Equal(4, E((Relation)path.Right).Length);
            Assert.Contains(E(design.Enrichment.Result.ClosedPaths), actual => ReferenceEquals(actual, path));
        });
        Assert.Equal(3, E(design.Enrichment.Layers).Length);
    }

    [Fact]
    public void RequestedModesKeepTheirOrderAndUnknownModesDoNotBecomeDetailRecipes()
    {
        var source = new BourgoinPatternRefinement(Sources.Value.Recipes.Single(recipe => recipe.Id == "B15"), 4).Result;
        var designs = BourgoinOrnamentDesignLibrary.Create(source, 0, ["detail", "field", "detail"]);
        Assert.Equal(new[] { "detail", "field" }, designs.Select(design => design.Mode));
        Assert.All(designs, design => Assert.Same(source, design.Source));
        Assert.Throws<ArgumentException>(() => BourgoinOrnamentDesignLibrary.Create(source, 0, ["motive"]));
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
