using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ScaffoldQuestionTests
{
    [Fact]
    public void TwoQuestionsReadOneUnchangedScaffoldWithBoundaryCorrespondenceAndComplement()
    {
        var f = new Fixture(); var source = f.Source();
        Assert.True(source.IsComplete); Assert.Equal(2, E(source.Faces).Length); Assert.Single(E(source.Exterior));
        Assert.Equal(4, E(source.Rotation).Length); Assert.Single(E(source.Euler));
        var triangles = new ScaffoldQuestion(source, Slots(3), new Placeholder());
        var quadrilaterals = new ScaffoldQuestion(source, Slots(4), new Placeholder());
        Assert.Equal(2, E(triangles.Selected).Length); Assert.Empty(E(triangles.Complement));
        Assert.Empty(E(quadrilaterals.Selected)); Assert.Equal(2, E(quadrilaterals.Complement).Length);
        Assert.Empty(E(triangles.Unresolved)); Assert.Empty(E(quadrilaterals.Unresolved));
        foreach (var match in E(triangles.Selected).Cast<Relation>())
        {
            Assert.True(triangles.TryReadSelected(match, out var face, out var boundary, out var correspondence));
            Assert.Contains(face, E(source.Faces)); Assert.Equal(3, E(boundary).Length); Assert.Equal(3, E(correspondence).Length);
            foreach (var (pair, slot, actual) in E(correspondence).Cast<Relation>().Zip(E(triangles.BoundarySlots), E(boundary)))
            { Assert.Same(slot, pair.Left); Assert.Same(actual, pair.Right); }
        }
        Assert.Same(source, triangles.Source); Assert.Same(source, quadrilaterals.Source);
        Assert.Same(f.Graph, source.Graph); Assert.Equal(5, E(f.Graph.Strokes).Length);
        Assert.Same(f.Inner, source.Boundaries); Assert.Same(f.Outer, source.ExteriorBoundaries);
        Assert.NotSame(triangles.Surface, quadrilaterals.Surface);
        AssertNoNumbers(new Relation(triangles.Evaluate(), quadrilaterals.Evaluate()));
    }

    [Fact]
    public void ExplicitSeamWithdrawalMergesTwoChambersAndPreservesOriginalRead()
    {
        var f = new Fixture(); var source = f.Source(); var before = source.Evaluate();
        var operation = new ScaffoldQuestionWithdrawal(source, S(f.AC), new Placeholder());
        Assert.Single(E(operation.Accepted)); Assert.Single(E(operation.Removed)); Assert.Same(f.AC, E(operation.Removed)[0]);
        Assert.Empty(E(operation.Blocked)); Assert.Empty(E(operation.Unresolved)); Assert.True(operation.Result.IsComplete);
        Assert.Equal(4, E(operation.Retained).Length); Assert.DoesNotContain(f.AC, E(operation.Result.Graph.Strokes));
        Assert.Single(E(operation.Result.Faces)); Assert.Equal(4, E((Relation)E(operation.Result.Boundaries)[0]).Length);
        var squareRead = new ScaffoldQuestion(operation.Result, Slots(4), new Placeholder());
        var triangleRead = new ScaffoldQuestion(operation.Result, Slots(3), new Placeholder());
        Assert.Single(E(squareRead.Selected)); Assert.Empty(E(triangleRead.Selected)); Assert.Single(E(triangleRead.Complement));
        Assert.Same(before, source.Evaluate()); Assert.Equal(5, E(source.Graph.Strokes).Length); Assert.Equal(2, E(source.Faces).Length);
        Assert.Contains(f.AC, E(source.Graph.Strokes)); AssertNoNumbers(operation.Evaluate());
    }

    [Fact]
    public void WithdrawalOfExteriorOrForeignConnectionIsBlockedWithoutChangingParticipation()
    {
        var f = new Fixture(); var source = f.Source();
        var foreign = QualitativeStrokeGraph.Stroke(new Placeholder(), f.A, f.C, S());
        var operation = new ScaffoldQuestionWithdrawal(source, S(f.AB, foreign), new Placeholder());
        Assert.Equal(2, E(operation.Blocked).Length); Assert.Empty(E(operation.Accepted)); Assert.Empty(E(operation.Removed));
        Assert.True(operation.Result.IsComplete); Assert.Equal(5, E(operation.Result.Graph.Strokes).Length);
        Assert.Equal(2, E(operation.Result.Faces).Length);
    }

    [Fact]
    public void DuplicateWithdrawalRequestRemainsUnresolvedAndDoesNotPartiallyApply()
    {
        var f = new Fixture(); var operation = new ScaffoldQuestionWithdrawal(f.Source(), S(f.AC, f.AC), new Placeholder());
        Assert.NotEmpty(E(operation.Unresolved)); Assert.Empty(E(operation.Removed)); Assert.Equal(5, E(operation.Retained).Length);
    }

    [Fact]
    public void MissingExteriorCannotEstablishBoundedRegionsFromClosedCyclesAlone()
    {
        var f = new Fixture(); var source = new ScaffoldQuestionScaffold(f.Graph, f.Inner, S(), new Placeholder());
        Assert.False(source.IsComplete); Assert.NotEmpty(E(source.Blocked));
        var read = new ScaffoldQuestion(source, Slots(3), new Placeholder());
        Assert.Empty(E(read.Selected)); Assert.Empty(E(read.Complement)); Assert.NotEmpty(E(read.Unresolved));
    }

    [Fact]
    public void ALookalikeEdgeWithTheSameEndsAndWordCannotStandInForActualParticipation()
    {
        var f = new Fixture(); QualitativeStrokeGraph.TryReadStroke(f.AB, out _, out _, out _, out var word);
        var clone = QualitativeStrokeGraph.Stroke(new Placeholder(), f.A, f.B, word);
        var fake = ScaffoldQuestionScaffold.Boundary(O(clone, f.A, f.B), O(f.BC, f.B, f.C), O(f.AC, f.C, f.A));
        var source = new ScaffoldQuestionScaffold(f.Graph, S(fake, f.Second), f.Outer, new Placeholder());
        Assert.False(source.IsComplete); Assert.NotEmpty(E(source.Blocked));
    }

    [Fact]
    public void DuplicateOrMisorderedFaceSidesCannotCertifyTheEmbedding()
    {
        var f = new Fixture();
        var duplicate = new ScaffoldQuestionScaffold(f.Graph, S(f.First, f.Second, f.First), f.Outer, new Placeholder());
        var wrongOrder = ScaffoldQuestionScaffold.Boundary(O(f.AB, f.A, f.B), O(f.AC, f.C, f.A), O(f.BC, f.B, f.C));
        var broken = new ScaffoldQuestionScaffold(f.Graph, S(wrongOrder, f.Second), f.Outer, new Placeholder());
        Assert.False(duplicate.IsComplete); Assert.NotEmpty(E(duplicate.Blocked));
        Assert.False(broken.IsComplete); Assert.NotEmpty(E(broken.Blocked));
    }

    [Fact]
    public void ADisconnectedFormalBoundaryOrZeroExtentEdgeCannotClaimAChamber()
    {
        var f = new Fixture();
        var empty = QualitativeStrokeGraph.Stroke(new Placeholder(), f.A, f.B, S());
        var graph = new QualitativeStrokeGraph(f.Frame, f.Graph.Sites, S(empty, f.BC, f.CD, f.DA, f.AC));
        var first = ScaffoldQuestionScaffold.Boundary(O(empty, f.A, f.B), O(f.BC, f.B, f.C), O(f.AC, f.C, f.A));
        var exterior = ScaffoldQuestionScaffold.Boundary(O(empty, f.B, f.A), O(f.DA, f.A, f.D), O(f.CD, f.D, f.C), O(f.BC, f.C, f.B));
        var source = new ScaffoldQuestionScaffold(graph, S(first, f.Second), S(exterior), new Placeholder());
        Assert.False(source.IsComplete); Assert.NotEmpty(E(source.Blocked));
    }

    [Fact]
    public void OpenBoundaryEvidenceAndOpenQuestionsRemainUnknown()
    {
        var f = new Fixture(); var open = new ScaffoldQuestionScaffold(f.Graph, StructuralConstraint.Open(f.First, f.Second), f.Outer, new Placeholder());
        Assert.False(open.IsComplete); Assert.NotEmpty(E(open.Unresolved));
        var source = f.Source(); var slot = new Placeholder();
        foreach (var slots in new[] { StructuralConstraint.Open(new Placeholder(), new Placeholder(), new Placeholder()), S(slot, slot, slot), S() })
        {
            var question = new ScaffoldQuestion(source, slots, new Placeholder());
            Assert.Empty(E(question.Selected)); Assert.Empty(E(question.Complement)); Assert.NotEmpty(E(question.Unresolved));
        }
    }

    [Fact]
    public void CopiedFaceOrMatchCannotForgeThisQuestionEvidence()
    {
        var f = new Fixture(); var source = f.Source(); var question = new ScaffoldQuestion(source, Slots(3), new Placeholder());
        var actual = (Relation)E(question.Selected)[0]; var copy = new Relation(actual.Left, actual.Right);
        Assert.False(question.TryReadSelected(copy, out _, out _, out _));
        var face = (Relation)actual.Left; Assert.False(source.TryReadFace(new Relation(face.Left, face.Right), out _, out _));
    }

    [Fact]
    public void RepeatedWithdrawalPreservesCompleteAcceptedPrefixAndBlocksPinchedRemainder()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var center = new Placeholder(); var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder(); var d = new Placeholder();
        var r = QualitativeMotionFrame.Move(frame.Right, reach); var l = QualitativeMotionFrame.Move(frame.Left, reach);
        var u = QualitativeMotionFrame.Move(frame.Up, reach); var down = QualitativeMotionFrame.Move(frame.Down, reach);
        Relation Edge(IElement from, IElement to, params IElement[] terms) => QualitativeStrokeGraph.Stroke(new Placeholder(), from, to, S(terms));
        var ab = Edge(a, b, r, r); var bc = Edge(b, c, u, u); var cd = Edge(c, d, l, l); var da = Edge(d, a, down, down);
        var oa = Edge(center, a, l, down); var ob = Edge(center, b, r, down); var oc = Edge(center, c, r, u); var od = Edge(center, d, l, u);
        var boundaries = S(ScaffoldQuestionScaffold.Boundary(O(oa, center, a), O(ab, a, b), O(ob, b, center)),
            ScaffoldQuestionScaffold.Boundary(O(ob, center, b), O(bc, b, c), O(oc, c, center)),
            ScaffoldQuestionScaffold.Boundary(O(oc, center, c), O(cd, c, d), O(od, d, center)),
            ScaffoldQuestionScaffold.Boundary(O(od, center, d), O(da, d, a), O(oa, a, center)));
        var exterior = S(ScaffoldQuestionScaffold.Boundary(O(ab, b, a), O(da, a, d), O(cd, d, c), O(bc, c, b)));
        var graph = new QualitativeStrokeGraph(frame, S(center, a, b, c, d), S(ab, bc, cd, da, oa, ob, oc, od));
        var source = new ScaffoldQuestionScaffold(graph, boundaries, exterior, new Placeholder());
        Assert.True(source.IsComplete);
        var operation = new ScaffoldQuestionWithdrawal(source, S(oa, ob, oc, od), new Placeholder());
        Assert.Equal(2, E(operation.Accepted).Length); Assert.Equal(2, E(operation.Removed).Length);
        Assert.Equal(2, E(operation.Checks).Length); Assert.Equal(2, E(operation.Blocked).Length);
        Assert.True(operation.Result.IsComplete); Assert.Equal(6, E(operation.Retained).Length);
        Assert.Contains(oc, E(operation.Retained)); Assert.Contains(od, E(operation.Retained));
        Assert.Equal(2, E(operation.Result.Faces).Length); Assert.Equal(8, E(source.Graph.Strokes).Length);
        AssertNoNumbers(operation.Evaluate());
    }

    [Fact]
    public void EmptyGraphDoesNotInventACompleteCellularEmbedding()
    {
        var graph = new QualitativeStrokeGraph(new QualitativeMotionFrame(), S(), S());
        var source = new ScaffoldQuestionScaffold(graph, S(), S(), new Placeholder());
        Assert.False(source.IsComplete); Assert.NotEmpty(E(source.Unresolved));
        var question = new ScaffoldQuestion(source, Slots(3), new Placeholder());
        Assert.Empty(E(question.Selected)); Assert.NotEmpty(E(question.Unresolved));
    }

    private sealed class Fixture
    {
        internal QualitativeMotionFrame Frame { get; } = new();
        internal IElement A { get; } = new Placeholder(); internal IElement B { get; } = new Placeholder();
        internal IElement C { get; } = new Placeholder(); internal IElement D { get; } = new Placeholder();
        internal Relation AB { get; } internal Relation BC { get; } internal Relation CD { get; } internal Relation DA { get; } internal Relation AC { get; }
        internal Relation First { get; } internal Relation Second { get; } internal Relation Inner { get; } internal Relation Outer { get; }
        internal QualitativeStrokeGraph Graph { get; }
        internal Fixture()
        {
            var h = new Placeholder(); var v = new Placeholder();
            var r = QualitativeMotionFrame.Move(Frame.Right, h); var l = QualitativeMotionFrame.Move(Frame.Left, h);
            var u = QualitativeMotionFrame.Move(Frame.Up, v); var d = QualitativeMotionFrame.Move(Frame.Down, v);
            AB = QualitativeStrokeGraph.Stroke(new Placeholder(), A, B, S(r)); BC = QualitativeStrokeGraph.Stroke(new Placeholder(), B, C, S(u));
            CD = QualitativeStrokeGraph.Stroke(new Placeholder(), C, D, S(l)); DA = QualitativeStrokeGraph.Stroke(new Placeholder(), D, A, S(d));
            AC = QualitativeStrokeGraph.Stroke(new Placeholder(), A, C, S(r, u));
            Graph = new QualitativeStrokeGraph(Frame, S(A, B, C, D), S(AB, BC, CD, DA, AC));
            First = ScaffoldQuestionScaffold.Boundary(O(AB, A, B), O(BC, B, C), O(AC, C, A));
            Second = ScaffoldQuestionScaffold.Boundary(O(AC, A, C), O(CD, C, D), O(DA, D, A));
            Inner = S(First, Second);
            Outer = S(ScaffoldQuestionScaffold.Boundary(O(AB, B, A), O(DA, A, D), O(CD, D, C), O(BC, C, B)));
        }
        internal ScaffoldQuestionScaffold Source() => new(Graph, Inner, Outer, new Placeholder());
    }
    private static Relation O(Relation edge, IElement from, IElement to) => ScaffoldQuestionScaffold.Oriented(edge, from, to);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation Slots(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static void AssertNoNumbers(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            Assert.False(item is Carrier or CarrierLocation);
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
