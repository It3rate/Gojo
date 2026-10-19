using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinConstructionReadTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());

    [Fact]
    public void ImageReadGroupsActualSharedPrototypeAndRechecksActualWordAndPlacementReceipts()
    {
        var source = Images(); var read = new BourgoinConstructionRead(source.Frame, source.Program.Surface);
        Assert.True(read.IsReadable); Assert.Equal(2, read.PathCount); Assert.Equal(8, read.ChordCount);
        Assert.Equal(10, read.PortCount); Assert.Equal(2, read.ClosedPathCount);
        Assert.Equal(2, read.Images.Count); Assert.Single(E(read.Prototypes));
        Assert.Same(read.Images[0].Prototype, read.Images[1].Prototype);
        Assert.NotSame(read.Images[0].OutputPath, read.Images[1].OutputPath);
        Assert.All(read.Images, image => Assert.Equal(5, E(image.ApplyReads).Length));
        Assert.Empty(E(read.Blocked)); Assert.Empty(E(read.Unrecognized)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void ArbitrarilyNestedValidImageEvidenceIsNotMinedAsAConstructionContract()
    {
        var source = Images(); var hidden = new Relation(new Placeholder(), source.Evidence[0]);
        var program = WithEvidence(source.Program, S(hidden));
        var read = new BourgoinConstructionRead(source.Frame, program.Surface);
        Assert.True(read.IsReadable); Assert.Empty(read.Images); Assert.Same(hidden, Assert.Single(E(read.Unrecognized)));
    }

    [Fact]
    public void RetainedNativeUnitWithNoRefinementHistoryIsUnrecognizedRatherThanFaulted()
    {
        var source = Images();
        var retained = new UnitArrangementProgram(source.Frame, "retained child", source.Program.Paths, source.Program.Ports, source.Program.ClosedPaths);
        var selected = WithEvidence(source.Program, S(retained.Surface));
        var read = new BourgoinConstructionRead(source.Frame, selected.Surface);
        Assert.True(read.IsReadable); Assert.Empty(read.Refinements); Assert.Empty(E(read.Blocked));
        Assert.Same(retained.Surface, Assert.Single(E(read.Unrecognized)));
    }

    [Fact]
    public void ForgedSuccessfulSubstitutionResultIsRejectedRatherThanCountedAsAnImage()
    {
        var source = Images(); var original = source.Evidence[0];
        var details = (Relation)original.Right; var receiptAndShift = (Relation)details.Right;
        var receipts = E((Relation)receiptAndShift.Left).Cast<Relation>().ToArray();
        var result = (Relation)receipts[1].Right;
        receipts[1] = new(receipts[1].Left, new Relation(S(), result.Right));
        var forged = new Relation(original.Left, new Relation(details.Left, new Relation(S(receipts), receiptAndShift.Right)));
        var read = new BourgoinConstructionRead(source.Frame, WithEvidence(source.Program, S(forged)).Surface);
        Assert.Empty(read.Images); Assert.Same(forged, Assert.Single(E(read.Blocked)));
    }

    [Fact]
    public void UnrelatedValidZeroPlacementDoesNotAuthenticateAnImage()
    {
        var source = Images(); var original = source.Evidence[1]; var details = (Relation)original.Right;
        var receiptAndShift = (Relation)details.Right; var shiftAndPlacement = (Relation)receiptAndShift.Right;
        var unrelated = source.Frame.Reduce(S(), S());
        var forged = new Relation(original.Left, new Relation(details.Left,
            new Relation(receiptAndShift.Left, new Relation(shiftAndPlacement.Left, unrelated))));
        var read = new BourgoinConstructionRead(source.Frame, WithEvidence(source.Program, S(forged)).Surface);
        Assert.Empty(read.Images); Assert.Same(forged, Assert.Single(E(read.Blocked)));
    }

    [Fact]
    public void ExactRefinementReadPreservesActualCoarsePortsAndEndpointProofs()
    {
        var source = Images(); var path = (Relation)E(source.Program.Paths)[0];
        var coarse = new UnitArrangementProgram(source.Frame, "irrelevant authored name", S(path),
            S(E(source.Program.Ports).Cast<Relation>().Where(port => ReferenceEquals(((Relation)port.Right).Left, path)).ToArray()), S(path));
        var word = (Relation)E((Relation)path.Right)[0]; var reach = ((Relation)E(word)[0]).Right;
        var a = S(Move(source.Frame.Right), Move(source.Frame.Up)); var b = S(Move(source.Frame.Right), Move(source.Frame.Down));
        var fine = coarse.Refine("another irrelevant name", S(new Relation(word, S(a, b))));
        var selected = new UnitArrangementProgram(source.Frame, "selected", fine.Paths, fine.Ports, fine.ClosedPaths, S(fine.Surface));
        var read = new BourgoinConstructionRead(source.Frame, selected.Surface);
        var actual = Assert.Single(read.Refinements); Assert.Same(coarse.Surface, actual.Coarse); Assert.Same(fine.Surface, actual.Fine);
        Assert.Equal(5, read.ChordCount); Assert.Equal(4, E(actual.Proofs).Length); Assert.Empty(E(read.Blocked));
        Relation Move(IElement direction) => QualitativeMotionFrame.Move(direction, reach);
    }

    [Fact]
    public void EmptyZeroWitnessCannotReplaceTheActualRefinementDisplacementProof()
    {
        var source = Images(); var path = (Relation)E(source.Program.Paths)[0];
        var coarse = new UnitArrangementProgram(source.Frame, "coarse", S(path), S(), S(path));
        var fine = coarse.Refine("fine", S()); var history = (Relation)fine.Evidence;
        var replacementAndProofs = (Relation)history.Right; var proofs = E((Relation)replacementAndProofs.Right).Cast<Relation>().ToArray();
        var chainAndProof = (Relation)proofs[0].Right;
        proofs[0] = new(proofs[0].Left, new Relation(chainAndProof.Left, source.Frame.Reduce(S(), S())));
        var tampered = new UnitArrangementProgram(source.Frame, "tampered", fine.Paths, fine.Ports, fine.ClosedPaths,
            new Relation(history.Left, new Relation(replacementAndProofs.Left, S(proofs))));
        var selected = new UnitArrangementProgram(source.Frame, "selected", tampered.Paths, tampered.Ports, tampered.ClosedPaths, S(tampered.Surface));
        var read = new BourgoinConstructionRead(source.Frame, selected.Surface);
        Assert.True(read.IsReadable); Assert.Empty(read.Refinements); Assert.Same(tampered.Surface, Assert.Single(E(read.Blocked)));
    }

    [Fact]
    public void OpenEvidenceRetainsItsKnownValidPrefixAndItsUnsearchedTail()
    {
        var source = Images(); var evidence = StructuralConstraint.Open(source.Evidence[0]);
        var read = new BourgoinConstructionRead(source.Frame, WithEvidence(source.Program, evidence).Surface);
        Assert.Single(read.Images); Assert.Same(evidence, Assert.Single(E(read.Unresolved)));
    }

    [Fact]
    public void RetainedRefinementCanSupplyTheActualSourcePrototypeOfAdmittedImages()
    {
        var recipe = Library.Value.Recipes.Single(recipe => recipe.Id == "B30");
        var motif = (Relation)Assert.Single(E(recipe.Motifs));
        var read = new BourgoinConstructionRead(recipe.Frame, motif);
        var refinement = Assert.Single(read.Refinements); Assert.Equal(4, read.Images.Count);
        var fineBody = (Relation)refinement.Fine.Right; var finePath = Assert.Single(E((Relation)((Relation)fineBody.Left).Left));
        Assert.DoesNotContain(E(read.Paths), path => ReferenceEquals(path, finePath));
        Assert.All(read.Images, image => Assert.Same(finePath, image.SourcePath));
        var participation = Assert.IsType<Relation>(Assert.Single(E(refinement.Participation)));
        Assert.Same(finePath, participation.Left);
        var scope = Assert.IsType<Relation>(participation.Right);
        Assert.Empty(E((Relation)scope.Left));
        Assert.Equal(4, E((Relation)scope.Right).Length);
        Assert.All(E((Relation)scope.Right), evidence => Assert.Contains(read.Images, image => ReferenceEquals(image.Evidence, evidence)));
        Assert.Empty(E(read.Blocked));
        // The source refinement is connected by its actual prototype path;
        // none of its source ports or paths are claimed as direct execution.
    }

    [Theory]
    [InlineData("B02", 4, 1, 0, 0)]
    [InlineData("B10", 0, 0, 2, 0)]
    [InlineData("B12", 4, 2, 0, 4)]
    [InlineData("B13", 4, 1, 0, 4)]
    public void ArchiveImportPreservesReadablePrototypeImageRefinementAndCornerCorrespondences(string id, int images, int prototypes, int refinements, int corners)
    {
        var recipe = Library.Value.Recipes.Single(recipe => recipe.Id == id);
        var archive = recipe.Program.Save(recipe.Bindings, recipe.Window);
        Assert.DoesNotContain(recipe.Name, archive, StringComparison.Ordinal);
        Assert.True(OrnamentProgram.TryLoad(archive, out var sample, out var pending)); Assert.Empty(E(pending));
        var motif = (Relation)Assert.Single(E(sample.Program.Motifs));
        var read = new BourgoinConstructionRead(sample.Program.Frame, motif);
        Assert.True(read.IsReadable); Assert.Equal(images, read.Images.Count); Assert.Equal(prototypes, E(read.Prototypes).Length);
        Assert.Equal(refinements, read.Refinements.Count); Assert.Equal(corners, read.Corners.Count); Assert.Empty(E(read.Blocked));
        Assert.NotSame(Assert.Single(E(recipe.Motifs)), motif);
    }

    [Fact]
    public void ForeignFrameAndMalformedProgramHaveNoSuccessfulConstructionRead()
    {
        var source = Images(); var foreign = new BourgoinConstructionRead(new(), source.Program.Surface);
        Assert.False(foreign.IsReadable); Assert.Empty(foreign.Images); Assert.Single(E(foreign.Blocked));
        var malformed = new BourgoinConstructionRead(source.Frame, new Relation(source.Frame.Surface, S()));
        Assert.False(malformed.IsReadable); Assert.Equal(0, malformed.PathCount); Assert.Single(E(malformed.Blocked));
    }

    private sealed record Fixture(QualitativeMotionFrame Frame, UnitArrangementProgram Program, Relation[] Evidence);
    private static Fixture Images()
    {
        var frame = new QualitativeMotionFrame(); IElement reach = new Placeholder();
        Relation Word(params IElement[] directions) => S(directions.Select(direction => (IElement)QualitativeMotionFrame.Move(direction, reach)).ToArray());
        var start = S(); Relation[] words = [Word(frame.Right, frame.Right), Word(frame.Up), Word(frame.Left, frame.Left), Word(frame.Down)];
        var seedPath = new Relation(start, S(words)); var prototype = new Relation(seedPath, S(seedPath));
        var transform = new QualitativePatternTransform(frame, frame, S(new[] { frame.Right, frame.Left, frame.Up, frame.Down }
            .Select(direction => (IElement)new Relation(QualitativeMotionFrame.Move(direction, reach), Word(direction))).ToArray()));
        var paths = new List<IElement>(); var ports = new List<IElement>(); var evidence = new List<Relation>();
        for (var index = 0; index < 2; index++)
        {
            var reads = new[] { start }.Concat(words).Select(transform.Apply).ToArray();
            var results = reads.Select(read => { Assert.True(transform.TryReadComplete(read, out var result)); return result; }).ToArray();
            var shift = index == 0 ? S() : Word(frame.Right, frame.Right, frame.Right);
            var placement = frame.Reduce(results[0], shift); Assert.True(frame.TryReadResult(placement, out var actualStart, out _, out _));
            var path = new Relation(actualStart, S(results.Skip(1).Cast<IElement>().ToArray())); paths.Add(path);
            foreach (var selector in results.Skip(1).Cast<IElement>().Append(path))
                ports.Add(new Relation(new Placeholder(), new Relation(path, selector)));
            evidence.Add(new(new Relation(prototype, path), new Relation(transform.Surface, new Relation(S(reads), new Relation(shift, placement)))));
        }
        return new(frame, new(frame, "not a semantic opcode", S(paths.ToArray()), S(ports.ToArray()), S(paths.ToArray()), S(evidence.ToArray())), evidence.ToArray());
    }
    private static UnitArrangementProgram WithEvidence(UnitArrangementProgram source, IElement evidence) =>
        new(source.Frame, "changed label", source.Paths, source.Ports, source.ClosedPaths, evidence);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
