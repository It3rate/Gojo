using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinPatternRefinementTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());
    public static IEnumerable<object[]> RecipeIds() => Library.Value.Recipes.Select(recipe => new object[] { recipe.Id });

    [Theory]
    [MemberData(nameof(RecipeIds))]
    public void FinerAuthoringWordsRetainEverySkeletonChordPortAndVisiblePosition(string id)
    {
        var source = Library.Value.Recipes.Single(recipe => recipe.Id == id);
        var refinement = new BourgoinPatternRefinement(source, 4); var result = refinement.Result;
        Assert.Same(source.Frame, result.Frame); Assert.Same(source.Program.Layout, result.Program.Layout);
        Assert.Same(source.Transforms, result.Transforms); Assert.Same(source.Window, result.Window);
        Assert.Equal(Position(source.Frame, source.ColumnPitch), Divide(Position(result.Frame, result.ColumnPitch), 4));
        Assert.Equal(Position(source.Frame, source.RowPitch), Divide(Position(result.Frame, result.RowPitch), 4));
        var coarse = Unit(source.Frame, Assert.IsType<Relation>(Assert.Single(E(source.Motifs))));
        var fine = Unit(result.Frame, Assert.IsType<Relation>(Assert.Single(E(result.Motifs))));
        var coarsePaths = E(coarse.Paths).Cast<Relation>().ToArray(); var finePaths = E(fine.Paths).Cast<Relation>().ToArray();
        Assert.Equal(coarsePaths.Length, finePaths.Length);
        Assert.Equal(E(coarse.ClosedPaths).Length, E(fine.ClosedPaths).Length);
        Assert.Equal(E(coarse.Ports).Length, E(fine.Ports).Length);
        for (var index = 0; index < coarsePaths.Length; index++)
        {
            var oldPath = coarsePaths[index]; var newPath = finePaths[index];
            EqualVisible((Relation)oldPath.Left, (Relation)newPath.Left);
            var oldWords = E((Relation)oldPath.Right).Cast<Relation>().ToArray();
            var newWords = E((Relation)newPath.Right).Cast<Relation>().ToArray();
            Assert.Equal(oldWords.Length, newWords.Length);
            for (var chord = 0; chord < oldWords.Length; chord++) EqualVisible(oldWords[chord], newWords[chord]);
        }
        var ports = E(fine.Ports).Cast<Relation>().ToArray();
        foreach (var oldPort in E(coarse.Ports).Cast<Relation>())
        {
            var port = Assert.Single(ports, row => ReferenceEquals(row.Left, oldPort.Left));
            var oldPick = (Relation)oldPort.Right; var pick = (Relation)port.Right;
            var pathIndex = Array.FindIndex(coarsePaths, path => ReferenceEquals(path, oldPick.Left));
            Assert.Same(finePaths[pathIndex], pick.Left);
            if (ReferenceEquals(oldPick.Left, oldPick.Right)) Assert.Same(pick.Left, pick.Right);
            else
            {
                var chordIndex = Array.FindIndex(E((Relation)coarsePaths[pathIndex].Right), word => ReferenceEquals(word, oldPick.Right));
                Assert.Same(E((Relation)finePaths[pathIndex].Right)[chordIndex], pick.Right);
            }
        }
        var before = Field(source, 1, 1); var after = Field(result, 1, 1);
        Assert.True(before.IsComplete); Assert.True(after.IsComplete);
        Assert.Equal(E(before.Graph.Strokes).Length, E(after.Graph.Strokes).Length);
        Assert.Equal(E(before.Accepted).Length, E(after.Accepted).Length);
        Assert.Empty(E(after.Blocked)); Assert.Empty(E(after.Unresolved));

        void EqualVisible(Relation oldWord, Relation newWord)
        {
            Assert.Equal(E(oldWord).Length * 4, E(newWord).Length);
            var a = Position(source.Frame, oldWord); var b = Position(result.Frame, newWord);
            Assert.Equal((a.X * 4, a.Y * 4), b);
            Assert.Equal(a.X * source.DisplayBasis.RX + a.Y * source.DisplayBasis.UX,
                b.X * result.DisplayBasis.RX + b.Y * result.DisplayBasis.UX, 10);
            Assert.Equal(a.X * source.DisplayBasis.RY + a.Y * source.DisplayBasis.UY,
                b.X * result.DisplayBasis.RY + b.Y * result.DisplayBasis.UY, 10);
        }
    }

    [Theory]
    [InlineData("B01")]
    [InlineData("B24")]
    [InlineData("B32")]
    [InlineData("B34")]
    public void ImportedFinerRecipeReplaysNeighbourSeamsWithoutNumericParticipants(string id)
    {
        var source = Library.Value.Recipes.Single(recipe => recipe.Id == id);
        var refined = new BourgoinPatternRefinement(source, 4).Result;
        var start = (Relation)source.Window.Left;
        var window = OrnamentProgram.Window(start.Left, start.Right, 2, 2);
        var json = OrnamentFieldPacket.Save(refined.Program, refined.Bindings, window, refined.SeamContract);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(0, document.RootElement.GetProperty("native").GetProperty("carriers").GetArrayLength());
        Assert.All(document.RootElement.GetProperty("native").GetProperty("nodes").EnumerateArray(),
            node => Assert.Contains(node.GetProperty("kind").GetString(), new[] { "relation", "placeholder" }));
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var packet, out var unresolved)); Assert.Empty(E(unresolved));
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        var original = Field(source, 2, 2);
        Assert.True(field.IsComplete); Assert.True(original.IsComplete);
        Assert.Equal(E(original.Graph.Strokes).Length, E(field.Graph.Strokes).Length);
        Assert.Equal(E(original.Accepted).Length, E(field.Accepted).Length);
        Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
    }

    [Fact]
    public void ActualSharedWordsPortsAndRepeatedOffsetStepsKeepTheirAliasesAndTheirSourceHistory()
    {
        var frame = new QualitativeMotionFrame(); IElement reach = new Placeholder();
        var zero = S(); var right = S(QualitativeMotionFrame.Move(frame.Right, reach));
        var up = S(QualitativeMotionFrame.Move(frame.Up, reach));
        var first = new Relation(zero, S(right)); var second = new Relation(zero, S(right));
        IElement a = new Placeholder(), b = new Placeholder();
        var unit = new UnitArrangementProgram(frame, "two shared words", S(first, second),
            S(new Relation(a, new Relation(first, right)), new Relation(b, new Relation(second, right))));
        var identity = new QualitativePatternTransform(frame, frame, S(new[] { frame.Right, frame.Left, frame.Up, frame.Down }.Select(direction =>
        {
            var move = QualitativeMotionFrame.Move(direction, reach); return (IElement)new Relation(move, S(move));
        }).ToArray()));
        IElement motifSlot = new Placeholder(), transformSlot = new Placeholder(), us = new Placeholder(), vs = new Placeholder();
        IElement u = new Placeholder(), v = new Placeholder();
        var uc = new Relation(S(u), S(new Relation(u, u))); var vc = new Relation(S(v), S(new Relation(v, v)));
        var sharedDomain = S(right);
        var parameters = S(new Relation(motifSlot, S(unit.Surface)), new Relation(transformSlot, S(identity.Surface)),
            new Relation(us, sharedDomain), new Relation(vs, sharedDomain));
        var layout = new Relation(new Relation(uc, vc), new Relation(new Relation(us, vs),
            S(new Relation(new Relation(u, v), new Relation(motifSlot, transformSlot)))));
        var program = new OrnamentProgram(frame, S(unit.Surface), S(identity.Surface), parameters, layout);
        var bindings = S(new Relation(motifSlot, unit.Surface), new Relation(transformSlot, identity.Surface),
            new Relation(us, right), new Relation(vs, right));
        var selector = new Relation(unit.Surface, identity.Surface);
        var offset = OrnamentProgramField.Offset(program, 2, 0);
        var rule = new Relation(new Relation(selector, selector), new Relation(offset, new Relation(a, b)));
        var source = new BourgoinPatternLibrary.Recipe("test", "native alias fixture", 0, 0, "", "", "", program, bindings,
            OrnamentProgram.Window(u, v, 1, 1), right, up, OrnamentProgramField.Contract(program, rule), new(1, 0, 0, 1));
        var refinement = new BourgoinPatternRefinement(source, 4); var result = refinement.Result;
        var mapped = Unit(frame, Assert.IsType<Relation>(Assert.Single(E(result.Motifs))));
        var paths = E(mapped.Paths).Cast<Relation>().ToArray();
        Assert.Same(paths[0].Left, paths[1].Left);
        Assert.Same(Assert.Single(E((Relation)paths[0].Right)), Assert.Single(E((Relation)paths[1].Right)));
        var parameterRows = E(result.Program.Parameters).Cast<Relation>().ToArray();
        Assert.Same(parameterRows[2].Right, parameterRows[3].Right);
        var bindRows = E(result.Bindings).Cast<Relation>().ToArray(); Assert.Same(bindRows[2].Right, bindRows[3].Right);
        var mappedRule = Assert.IsType<Relation>(Assert.Single(E((Relation)result.SeamContract.Right)));
        var picks = (Relation)mappedRule.Left; Assert.Same(picks.Left, picks.Right);
        var steps = E((Relation)((Relation)mappedRule.Right).Left); Assert.Same(steps[0], steps[1]);
        Assert.Same(result.Program.Surface, ((Relation)steps[0]).Left);
        var retained = Assert.IsType<Relation>(mapped.Evidence); Assert.Same(unit.Surface, retained.Left);
        Assert.Same(refinement.Transform.Surface, ((Relation)retained.Right).Left);
        var receipts = E((Relation)((Relation)((Relation)retained.Right).Right).Right).Cast<Relation>().ToArray();
        Assert.All(receipts, receipt => Assert.True(refinement.Transform.TryReadComplete(receipt, out _)));
        Assert.Same(program.Surface, ((Relation)refinement.Correspondence.Left).Left);
        Assert.Same(result.Program.Surface, ((Relation)refinement.Correspondence.Left).Right);
    }

    [Fact]
    public void UnitFactorPreservesTheActualRecipeRatherThanManufacturingAnEquivalentCopy()
    {
        var source = Library.Value.Recipes[0];
        Assert.Same(source, new BourgoinPatternRefinement(source, 1).Result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(17)]
    [InlineData(int.MaxValue)]
    public void InvalidOrUnboundedHostExpansionIsRejectedBeforeTheAtlasIsConstructed(int subdivisions) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BourgoinPatternLibrary(subdivisions));

    private static OrnamentProgramField Field(BourgoinPatternLibrary.Recipe recipe, int columns, int rows)
    {
        var origin = (Relation)recipe.Window.Left;
        var run = recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(origin.Left, origin.Right, columns, rows));
        Assert.True(run.IsComplete); return new(run, recipe.SeamContract);
    }
    private static UnitArrangementProgram Unit(QualitativeMotionFrame frame, Relation surface)
    {
        var shape = (Relation)surface.Right; var declarations = (Relation)shape.Left; var history = (Relation)shape.Right;
        return new(frame, "test read", (Relation)declarations.Left, (Relation)declarations.Right, (Relation)history.Left, history.Right);
    }
    private static (int X, int Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static (int X, int Y) Divide((int X, int Y) at, int by) => (at.X / by, at.Y / by);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
