using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

/// <summary>Integration claims concern executed native participation, captured
/// dependencies and read contexts. Screen geometry and labels supply no premise.</summary>
public sealed class UnitArrangementStudyTests
{
    private static readonly Lazy<UnitArrangementStudy> Captured = new(() => new());
    private static UnitArrangementStudy Study => Captured.Value;

    [Fact]
    public void MirroredNeighbourRevisionsProduceSixActualSharedSeamsAndKeepTheFixedPairHeld()
    {
        var example = Study.Cases[1];
        Assert.Equal(7, example.Connections.Length);
        Assert.Equal(6, example.Connections.Count(connection => connection.IsAligned));
        var blocked = Assert.Single(example.Connections, connection => !connection.IsAligned);
        Assert.Single(E(blocked.Blocked)); Assert.Empty(E(blocked.AcceptedJoins)); Assert.Single(E(blocked.Negotiations));
        Assert.Equal(4, example.Before.Where((revision, index) => !ReferenceEquals(revision, example.Units[index].Current)).Count());
        Assert.Same(example.Before[^1], example.Units[^1].Current);
        var view = Assert.IsType<QualitativeGraphJoin>(example.SeamView);
        Assert.True(view.TryReadPassed(view.Evaluate())); Assert.Equal(6, E(view.Accepted).Length);
        Assert.Empty(E(view.Blocked)); Assert.Empty(E(view.Unresolved));
        foreach (var connection in example.Connections.Where(connection => connection.IsAligned))
        {
            var actual = Assert.IsType<Relation>(Assert.Single(E(connection.AcceptedJoins)));
            Assert.True(connection.IsCurrentAgreement(actual)); Assert.Contains(E(view.Accepted), item => ReferenceEquals(item, actual));
            var pair = Assert.IsType<Relation>(actual.Left);
            var map = E(view.SiteMap).Cast<Relation>().ToArray();
            Assert.Same(map.Single(row => ReferenceEquals(row.Left, pair.Left)).Right,
                map.Single(row => ReferenceEquals(row.Left, pair.Right)).Right);
        }
    }

    [Fact]
    public void FourCoarseCornerRolesRemainAtTheSameFormalSitesAfterEachSixteenEdgeRefinement()
    {
        var example = Study.Cases[3];
        Assert.Equal(6, example.Units.Length);
        for (var i = 0; i < example.Units.Length; i++)
        {
            var prior = example.Before[i]; var node = example.Units[i]; var current = node.Current;
            Assert.NotSame(prior, current); Assert.Equal(4, E(prior.Graph.Strokes).Length); Assert.Equal(16, E(current.Graph.Strokes).Length);
            Assert.Equal(2, E(node.Revisions).Length); Assert.Single(E(node.Timeline));
            Assert.True(prior.Join.TryReadPassed(prior.Join.Evaluate())); Assert.True(current.Join.TryReadPassed(current.Join.Evaluate()));
            var oldPorts = E(prior.Ports).Cast<Relation>().ToArray(); Assert.Equal(4, oldPorts.Length);
            foreach (var old in oldPorts)
            {
                Assert.True(UnitArrangementConnection.TryReadPublication(node, old.Left, out var publication, out var binding));
                var retainedBinding = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(old.Right).Right).Left);
                Assert.NotSame(retainedBinding.Left, binding.Left); Assert.NotSame(old, publication);
                AssertSamePosition(Study.Frame, (Relation)retainedBinding.Right, (Relation)binding.Right);
            }
            var refinement = Assert.IsType<Relation>(current.Program.Evidence);
            Assert.Same(prior.Program.Surface, refinement.Left);
            var replacement = Assert.IsType<Relation>(refinement.Right);
            Assert.Equal(4, E(Assert.IsType<Relation>(replacement.Left)).Length);
            foreach (var proof in E(Assert.IsType<Relation>(replacement.Right)).Cast<Relation>())
            {
                var chain = Assert.IsType<Relation>(proof.Right); Assert.Equal(4, E(Assert.IsType<Relation>(chain.Left)).Length);
                var reduction = Assert.IsType<Relation>(chain.Right);
                Assert.True(Study.Frame.TryReadResult(reduction, out var remaining, out _, out var pending));
                Assert.Empty(E(remaining)); Assert.Empty(E(pending));
            }
        }
    }

    [Fact]
    public void CapturedTemplateLandmarksBecomeStaleInsteadOfSilentlyMovingAfterSupplierRevision()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder(); var role = new Placeholder(); var signal = new Placeholder();
        Relation W(IElement direction) => S(QualitativeMotionFrame.Move(direction, reach));
        UnitArrangementProgram Program(string name, Relation start, Relation word)
        {
            var path = new Relation(start, S(word));
            return new(frame, name, S(path), S(new Relation(role, new Relation(path, word))));
        }
        var first = Program("one supplier", S(), W(frame.Right)); var next = Program("revised supplier", W(frame.Up), W(frame.Right));
        var domain = S(first.Surface, next.Surface);
        var node = new UnitArrangementNode("supplier", [first, next], first, S(),
            FiniteLaw.Create(domain, S(signal), domain, FiniteLaw.Row(first.Surface, signal, S(next.Surface))));
        var template = new UnitLandmarkTemplate([((IUnitArrangementEndpoint)node, role)], node.Surface);
        var landmark = Assert.Single(template.Landmarks); Assert.True(template.TryRead(landmark, out var capturedBinding));
        var retainedSurface = template.Surface;
        var dependentProgram = Program("dependent", S(), W(frame.Down));
        var dependant = new UnitArrangementNode("placed once", [dependentProgram], dependentProgram, (Relation)capturedBinding.Right);
        var dependantState = dependant.Drawing.State;
        node.Commit(node.Propose(signal, landmark.Surface));
        Assert.False(template.TryRead(landmark, out _)); Assert.Same(retainedSurface, template.Surface);
        Assert.Same(dependantState, dependant.Drawing.State); Assert.Same(capturedBinding.Right, dependant.PlacementWord);
        var validation = template.Validate(); var partition = Assert.IsType<Relation>(validation.Right);
        Assert.Empty(E(Assert.IsType<Relation>(partition.Left))); Assert.Same(landmark.Surface, Assert.Single(E(Assert.IsType<Relation>(partition.Right))));
        var fresh = new UnitLandmarkTemplate([((IUnitArrangementEndpoint)node, role)], template.Surface);
        Assert.True(fresh.TryRead(Assert.Single(fresh.Landmarks), out var currentBinding));
        Assert.NotSame(capturedBinding, currentBinding);
    }

    [Fact]
    public void DifferentQuestionsObserveTheSameScaffoldAndExplicitWithdrawalCreatesLargerChambers()
    {
        var quilt = Study.Quilt; var source = quilt.Scaffold;
        Assert.True(source.IsComplete); Assert.Equal(8, E(source.Faces).Length); Assert.Single(E(source.Exterior));
        Assert.Same(source, quilt.Triangles.Source); Assert.Same(source, quilt.Quadrilaterals.Source);
        Assert.NotSame(quilt.Triangles.Surface, quilt.Quadrilaterals.Surface);
        Assert.Equal(8, E(quilt.Triangles.Selected).Length); Assert.Empty(E(quilt.Triangles.Complement));
        Assert.Empty(E(quilt.Quadrilaterals.Selected)); Assert.Equal(8, E(quilt.Quadrilaterals.Complement).Length);
        foreach (var match in E(quilt.Triangles.Selected).Cast<Relation>())
        {
            Assert.True(quilt.Triangles.TryReadSelected(match, out var face, out var boundary, out var correspondence));
            Assert.Contains(E(source.Faces), candidate => ReferenceEquals(candidate, face));
            Assert.Equal(3, E(boundary).Length); Assert.Equal(3, E(correspondence).Length);
            Assert.All(E(correspondence).Cast<Relation>(), row => Assert.Contains(E(boundary), edge => ReferenceEquals(edge, row.Right)));
        }
        var graph = source.Graph; var originalStrokes = graph.Strokes; var heads = quilt.Units.Select(unit => unit.Head).ToArray();
        quilt.Triangles.Evaluate(); quilt.Quadrilaterals.Evaluate();
        Assert.Same(originalStrokes, graph.Strokes);
        Assert.True(heads.Zip(quilt.Units).All(pair => ReferenceEquals(pair.First, pair.Second.Head)));
        var withdrawal = quilt.Withdrawal;
        Assert.Same(source, withdrawal.Source); Assert.Equal(2, E(withdrawal.Removed).Length);
        Assert.Empty(E(withdrawal.Blocked)); Assert.Empty(E(withdrawal.Unresolved)); Assert.True(withdrawal.Result.IsComplete);
        Assert.Equal(6, E(withdrawal.Result.Faces).Length); Assert.Same(withdrawal.Result, quilt.Opened.Source);
        Assert.Equal(2, E(quilt.Opened.Selected).Length); Assert.Equal(4, E(quilt.Opened.Complement).Length);
        Assert.Equal(E(graph.Strokes).Length - 2, E(withdrawal.Result.Graph.Strokes).Length);
        foreach (var edge in E(withdrawal.Removed))
        {
            Assert.Contains(E(graph.Strokes), actual => ReferenceEquals(actual, edge));
            Assert.DoesNotContain(E(withdrawal.Result.Graph.Strokes), actual => ReferenceEquals(actual, edge));
        }
        Assert.Empty(E(quilt.Triangles.Unresolved)); Assert.Empty(E(quilt.Opened.Unresolved));
    }

    [Fact]
    public void CrossingProjectionRemainsIndependentUntilActualCentrePortsAreJoined()
    {
        var crossing = Study.Crossing;
        var separate = crossing.Independent.Current.Graph.Describe(crossing.Independent.Current.Graph.Evaluate());
        var joined = crossing.Joined.Graph.Describe(crossing.Joined.Graph.Evaluate());
        Assert.Equal(0, separate.X); Assert.Equal(4, separate.Endpoints); Assert.Equal(2, E(crossing.Independent.Current.Graph.Strokes).Length);
        Assert.Equal(1, joined.X); Assert.Equal(4, joined.Endpoints); Assert.Equal(4, E(crossing.Joined.Graph.Strokes).Length);
        Assert.Equal(3, E(crossing.Joined.Accepted).Length); Assert.True(crossing.Joined.TryReadPassed(crossing.Joined.Evaluate()));
        var shared = E(crossing.Joined.Classes).Cast<Relation>().Where(row => row.Left is Relation group && E((Relation)group.Left).Length > 1).ToArray();
        var centre = Assert.Single(shared);
        Assert.Equal(4, E(Assert.IsType<Relation>(Assert.IsType<Relation>(centre.Left).Left)).Length);
    }

    [Fact]
    public void EveryHistoricalRevisionAndPlacementAccountsForEachActualPacketWithoutNativeNumbers()
    {
        var units = Units().ToArray(); Assert.Equal(59, units.Length);
        var checkedAccounts = 0; var checkedRevisions = 0;
        AssertNoNumeric(Study.Surface);
        foreach (var node in units)
        {
            CheckAccount(node.PlacementExecution.Account()); checkedAccounts++;
            AssertNoNumeric(node.PlacementExecution.Capture());
            foreach (var revision in E(node.Revisions).Cast<Relation>())
            {
                // Exact revision record -> exact primitive-leaf capture -> its
                // retained material account. No arbitrary recursive shape match.
                var executed = R(revision.Right); var states = R(executed.Right); var captured = R(R(states.Right).Left);
                var control = R(captured.Right); var io = R(control.Right); var pending = R(io.Right);
                var completions = R(pending.Right); var unresolved = R(completions.Right); var account = R(unresolved.Right);
                Assert.Same(captured.Left, R(account.Left).Left);
                Assert.Empty(E(R(pending.Left))); Assert.Single(E(R(completions.Left))); Assert.Empty(E(R(unresolved.Left)));
                CheckAccount(account); checkedAccounts++; checkedRevisions++;
                AssertNoNumeric(revision);
            }
        }
        Assert.Equal(69, checkedRevisions); Assert.Equal(128, checkedAccounts);
    }

    private static void CheckAccount(Relation account)
    {
        var source = R(account.Left); var imported = E(R(source.Right)); var active = R(account.Right); var retained = R(active.Right); var released = R(retained.Right);
        var partition = E(R(active.Left)).Concat(E(R(retained.Left))).Concat(E(R(released.Left))).Cast<Relation>().ToArray();
        Assert.Empty(E(R(released.Right))); Assert.Empty(E(R(active.Left))); Assert.Empty(E(R(retained.Left)));
        Assert.Equal(imported.Length, partition.Length); Assert.Equal(imported.Length, imported.Distinct(ReferenceEqualityComparer.Instance).Count());
        foreach (var packet in imported) Assert.Single(partition, row => ReferenceEquals(row.Left, packet));
        foreach (var row in partition) Assert.Contains(imported, packet => ReferenceEquals(row.Left, packet));
    }
    private static IEnumerable<UnitArrangementNode> Units() => Study.Cases.SelectMany(example => example.Units.Concat(example.Construction))
        .Concat(Study.Quilt.Units).Concat(Study.Crossing.Arms).Append(Study.Crossing.Independent).Distinct();
    private static void AssertSamePosition(QualitativeMotionFrame frame, Relation a, Relation b)
    {
        var inverse = S(E(b).Cast<Relation>().Reverse().Select(move => (IElement)QualitativeMotionFrame.Move(
            E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
        Assert.True(frame.TryReadResult(frame.Reduce(a, inverse), out var remaining, out _, out var pending)); Assert.Empty(E(remaining)); Assert.Empty(E(pending));
    }
    private static void AssertNoNumeric(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item)) { if (!seen.Add(item)) continue; Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item); if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); } }
    }
    private static Relation R(IElement item) => Assert.IsType<Relation>(item);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
