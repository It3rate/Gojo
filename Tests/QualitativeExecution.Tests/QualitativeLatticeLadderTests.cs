using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeLatticeLadderTests
{
    [Fact]
    public void TransformRetainsTheActualEarlierUnitAndNativeSubstitutionReceipts()
    {
        var f = new Fixture(); var square = f.Square();
        var original = QualitativeLatticeLadder.Begin(square);
        var shear = f.Shear(); var transformed = original.Transform("sheared", shear);
        Assert.Same(square, transformed.Current.Source);
        Assert.True(transformed.TryReadAncestor(square.Surface, out var ancestor, out var witness));
        Assert.Same(square, ancestor); Assert.Contains(square.Surface, Reachable(witness));
        Assert.Same(square.Surface, ((Relation)transformed.Current.Program.Evidence).Left);
        Assert.Contains(shear.Surface, Reachable(transformed.Surface));
        Assert.Contains(original.Surface, Reachable(transformed.Current.Provenance));
        Assert.Equal(2, transformed.Rungs.Count); Assert.True(transformed.Current.IsComplete);
        var oldPath = Assert.IsType<Relation>(Assert.Single(E(square.Paths)));
        var currentPath = Assert.IsType<Relation>(Assert.Single(E(transformed.Current.Program.Paths)));
        Assert.NotSame(oldPath, currentPath);
        Assert.Single(E(Assert.IsType<Relation>(E((Relation)oldPath.Right)[1])));
        Assert.Equal(2, E(Assert.IsType<Relation>(E((Relation)currentPath.Right)[1])).Length);
        // The older square is readable in its own complete native basis. Its
        // presence is history, and does not turn the sheared current word into
        // that old single-axis chord.
        var old = new UnitArrangementNode("old basis", [ancestor], ancestor, S());
        Assert.True(old.Current.Join.TryReadPassed(old.Current.Join.Evaluate()));
        AssertNoNumeric(transformed.Surface);
    }

    [Fact]
    public void RefinementRetainsActualCoarseCornersWithoutClaimingStraightFineChains()
    {
        var f = new Fixture(); var square = f.Square();
        var path = (Relation)E(square.Paths).Single(); var coarse = (Relation)E((Relation)path.Right)[0];
        var chain = S(f.U, f.R, f.D);
        var ladder = QualitativeLatticeLadder.Begin(square).Refine("bent first side", S(new Relation(coarse, chain)));
        Assert.True(ladder.TryReadAncestor(square.Surface, out var older, out _)); Assert.Same(square, older);
        var current = ladder.Current.Program;
        Assert.Equal(6, E((Relation)((Relation)E(current.Paths).Single()).Right).Length);
        var proof = Assert.IsType<Relation>(((Relation)current.Evidence).Right);
        var actual = Assert.IsType<Relation>(E((Relation)proof.Right)[0]);
        Assert.Same(coarse, actual.Left);
        Assert.Same(chain, ((Relation)actual.Right).Left);
        Assert.True(f.Frame.TryReadResult((Relation)((Relation)actual.Right).Right, out var residual, out _, out var unresolved));
        Assert.Empty(E(residual)); Assert.Empty(E(unresolved));
        Assert.Contains(coarse, Reachable(ladder.Current.Focus));
        var pathPair = Assert.IsType<Relation>(Assert.Single(E((Relation)ladder.Current.Correspondence.Left)));
        Assert.Same(path, pathPair.Left); Assert.Same(E(current.Paths).Single(), pathPair.Right);
        AssertNoNumeric(ladder.Surface);
    }

    [Fact]
    public void LocalEditCarriesActualPathsAndExposesRemovedPublicationRoles()
    {
        var f = new Fixture(); var a = new Relation(S(), S(f.R)); var b = new Relation(f.U, S(f.R));
        var oldPort = new Relation(f.Start, new Relation(a, a));
        var program = new UnitArrangementProgram(f.Frame, "two paths", S(a, b), S(oldPort));
        var introduced = new Relation(S(), S(f.U)); var assembly = new Relation(a, introduced);
        var ladder = QualitativeLatticeLadder.Begin(program).ReplacePaths("replace first", S(a), S(introduced), S(), S(), assembly);
        Assert.True(ladder.Current.IsComplete); Assert.Same(b, E(ladder.Current.Program.Paths)[0]);
        Assert.Same(introduced, E(ladder.Current.Program.Paths)[1]);
        Assert.Same(b, Assert.Single(E(ladder.Current.Carried)));
        var roleLoss = Assert.IsType<Relation>(Assert.Single(E((Relation)ladder.Current.Correspondence.Right)));
        Assert.Same(oldPort, roleLoss.Left); Assert.Empty(E((Relation)roleLoss.Right));
        Assert.Contains(assembly, Reachable(ladder.Current.Program.Evidence));
        Assert.True(ladder.TryReadAncestor(program.Surface, out var actual, out _)); Assert.Same(program, actual);
        AssertNoNumeric(ladder.Surface);
    }

    [Fact]
    public void CopiedForeignAndOpenSelectionsDoNotAcquireSourceMembership()
    {
        var f = new Fixture(); var program = f.Square(); var ladder = QualitativeLatticeLadder.Begin(program);
        var actual = (Relation)E(program.Paths).Single(); var copied = new Relation(actual.Left, actual.Right);
        var refused = ladder.ReplacePaths("foreign focus", S(copied), S(), S(), S());
        Assert.False(refused.Current.IsComplete); Assert.NotEmpty(E(refused.Current.Blocked));
        Assert.Same(program, refused.Current.Program); Assert.Same(actual, E(refused.Current.Carried).Single());
        var open = ladder.ReplacePaths("unfinished focus", StructuralConstraint.Open(actual), S(), S(), S());
        Assert.False(open.Current.IsComplete); Assert.NotEmpty(E(open.Current.Open)); Assert.Same(program, open.Current.Program);
        Assert.False(ladder.TryReadRung(new Relation(ladder.Surface.Left, ladder.Surface.Right), out _));
        var foreign = QualitativeLatticeLadder.Begin(f.Square());
        Assert.False(ladder.TryReadRung(foreign.Surface, out _));
        Assert.False(ladder.TryReadAncestor(new Relation(program.Surface.Left, program.Surface.Right), out _, out _));
        Assert.True(ladder.TryReadRung(ladder.Surface, out var rung)); Assert.Same(ladder.Current, rung);
    }

    [Fact]
    public void CrystallizationCreatesIndependentNativeWorldAndKeepsLossOutsideIt()
    {
        var f = new Fixture(); var source = f.Square();
        var history = QualitativeLatticeLadder.Begin(source).Transform("sheared", f.Shear());
        var capture = history.Crystallize("standalone"); var current = capture.Standalone.Current.Program;
        Assert.Single(capture.Standalone.Rungs); Assert.Null(capture.Standalone.Current.Source);
        Assert.Empty(E((Relation)current.Evidence));
        Assert.False(capture.Standalone.TryReadAncestor(source.Surface, out _, out _));
        var originalParticipants = Reachable(history.Surface); var standaloneParticipants = Reachable(capture.Standalone.Surface);
        Assert.DoesNotContain(source.Surface, standaloneParticipants);
        Assert.DoesNotContain(history.Current.Program.Surface, standaloneParticipants);
        Assert.DoesNotContain(capture.LossWitness, standaloneParticipants);
        Assert.DoesNotContain(originalParticipants, old => standaloneParticipants.Any(fresh => Same(old, fresh)));
        Assert.Contains(source.Surface, Reachable(capture.LossWitness)); Assert.NotEmpty(E(capture.Forgotten));
        Assert.NotSame(history.Current.Program.Frame, current.Frame);
        var path = (Relation)E(current.Paths).Single();
        foreach (var port in E(current.Ports).Cast<Relation>()) Assert.Same(path, ((Relation)port.Right).Left);
        Assert.Same(path, Assert.Single(E(current.ClosedPaths)));
        var node = new UnitArrangementNode("detached executable", [current], current, S());
        Assert.True(node.Current.Join.TryReadPassed(node.Current.Join.Evaluate()));
        var secondCapture = history.Crystallize("another standalone");
        Assert.DoesNotContain(Reachable(secondCapture.Standalone.Surface), item => standaloneParticipants.Any(other => Same(item, other)));
        AssertNoNumeric(capture.LossWitness);
    }

    [Fact]
    public void NumericReachAndAttachedNumericClaimsCannotEnterThePreNumericLadder()
    {
        var f = new Fixture(); var measured = S(QualitativeMotionFrame.Move(f.Frame.Right, new Carrier()));
        var path = new Relation(S(), S(measured));
        Assert.Throws<ArgumentException>(() => QualitativeLatticeLadder.Begin(new(f.Frame, "measured", S(path), S())));
        var valid = f.Square();
        Assert.Throws<ArgumentException>(() => QualitativeLatticeLadder.Begin(new(f.Frame, "attached claim", valid.Paths,
            valid.Ports, valid.ClosedPaths, new CarrierLocation(new Carrier(), 0))));
    }

    [Fact]
    public void ExactNativeReaderReconstructsTransportedRungsAndRejectsFabricatedSourceOrResult()
    {
        var f = new Fixture(); var square = f.Square();
        var path = (Relation)E(square.Paths).Single(); var word = (Relation)E((Relation)path.Right)[0];
        var refined = QualitativeLatticeLadder.Begin(square).Refine("bent side", S(new Relation(word, S(f.U, f.R, f.D))));
        Assert.True(QualitativeLatticeLadder.TryRead(f.Frame, refined.Rungs[0].Surface, out _), "base");
        Assert.True(QualitativeLatticeLadder.TryRead(f.Frame, refined.Surface, out _), "refine");
        var sheared = refined.Transform("sheared detail", f.Shear());
        Assert.True(QualitativeLatticeLadder.TryRead(f.Frame, sheared.Surface, out _), "transform");
        var added = new Relation(S(), S(f.R));
        var ladder = sheared.ReplacePaths("additional native path", S(), S(added), sheared.Current.Program.Ports,
            sheared.Current.Program.ClosedPaths, new Relation(added, added));
        Assert.True(QualitativeLatticeLadder.TryRead(f.Frame, ladder.Surface, out var liveRead));
        Assert.Same(ladder.Surface, liveRead.Surface);
        Assert.Same(ladder.Current.Program.Surface, liveRead.Current.Program.Surface);
        Assert.True(liveRead.TryReadAncestor(square.Surface, out var ancestor, out _)); Assert.Same(square.Surface, ancestor.Surface);
        var document = GojoStructureCompactJson.FromElement("lattice-ladder-test", ladder.Surface);
        Assert.True(GojoStructureCompactJson.TryToElement(document, out var transported));
        var transportedSurface = Assert.IsType<Relation>(transported.Root);
        var programSurface = CurrentProgramSurface(transportedSurface);
        Assert.True(QualitativeMotionFrameRead.TryReadStandard((Relation)programSurface.Left, out var frameRead));
        Assert.True(frameRead.TryRebind(transportedSurface, out var rebound, out _));
        Assert.True(QualitativeLatticeLadder.TryRead(frameRead.Target, (Relation)rebound, out var imported));
        Assert.Equal(4, imported.Rungs.Count);
        Assert.Same(imported.Rungs[2].Program, imported.Current.Source);
        Assert.Same(imported.Rungs[2].Program.Surface, ((Relation)imported.Current.Surface.Left).Left);
        Assert.True(imported.TryReadAncestor(imported.Rungs[0].Program.Surface, out var nativeBasis, out _));
        Assert.Same(imported.Rungs[0].Program, nativeBasis);
        var forgedSource = new Relation(new Relation(new Relation(square.Surface.Left, square.Surface.Right), ladder.Current.Focus), ladder.Surface.Right);
        Assert.False(QualitativeLatticeLadder.TryRead(f.Frame, forgedSource, out _));
        var body = (Relation)ladder.Surface.Right; var replacement = (Relation)body.Right;
        var output = (Relation)replacement.Right;
        var forgedOutput = new Relation(ladder.Surface.Left, new Relation(body.Left,
            new Relation(replacement.Left, new Relation(square.Surface, output.Right))));
        Assert.False(QualitativeLatticeLadder.TryRead(f.Frame, forgedOutput, out _));
        var refused = ladder.ReplacePaths("foreign path", S(new Relation(added.Left, added.Right)), S(), S(), S());
        Assert.True(QualitativeLatticeLadder.TryRead(f.Frame, refused.Surface, out var refusedRead));
        Assert.False(refusedRead.Current.IsComplete); Assert.NotEmpty(E(refusedRead.Current.Blocked));
    }

    private static Relation CurrentProgramSurface(Relation surface) =>
        (Relation)((Relation)((Relation)((Relation)surface.Right).Right).Right).Left;

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public IElement Start { get; } = new Placeholder();
        public IElement End { get; } = new Placeholder();
        public Relation R => S(QualitativeMotionFrame.Move(Frame.Right, Reach));
        public Relation L => S(QualitativeMotionFrame.Move(Frame.Left, Reach));
        public Relation U => S(QualitativeMotionFrame.Move(Frame.Up, Reach));
        public Relation D => S(QualitativeMotionFrame.Move(Frame.Down, Reach));
        public UnitArrangementProgram Square()
        {
            var words = new[] { R, U, L, D }; var path = new Relation(S(), S(words));
            return new(Frame, "native square", S(path), S(new Relation(Start, new Relation(path, words[0])),
                new Relation(End, new Relation(path, path))), S(path));
        }
        public QualitativePatternTransform Shear() => new(Frame, Frame, S(
            new Relation(E(R)[0], R), new Relation(E(L)[0], L),
            new Relation(E(U)[0], S([.. E(R), .. E(U)])), new Relation(E(D)[0], S([.. E(L), .. E(D)]))));
    }
    private static HashSet<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
            if (seen.Add(item) && item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static void AssertNoNumeric(IElement root) => Assert.All(Reachable(root), item => Assert.True(item is Relation or Placeholder));
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
