using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentProgramFieldTests
{
    [Fact]
    public void AnAbsentSeamRuleDoesNotWeldCoincidentPublishedEndpoints()
    {
        var f = new Fixture(); var run = f.Run(3, 1); var field = new OrnamentProgramField(run, OrnamentProgramField.Contract(f.Program));
        Assert.True(field.IsComplete); Assert.Empty(E(field.Accepted)); Assert.Empty(E(field.Frontier));
        Assert.Equal(6, E(field.Graph.Sites).Length); Assert.Equal(3, E(field.Graph.Strokes).Length);
        Assert.All(E(field.Join.SiteMap).Cast<Relation>(), map => Assert.Same(map.Left, map.Right));
        Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()));
    }

    [Fact]
    public void ADeclaredForwardSeamSharesActualQuotientSitesAndRetainsTheWindowFrontier()
    {
        var f = new Fixture(); var run = f.Run(3, 1); var contract = f.Contract(1, 0, f.End, f.Start);
        var field = new OrnamentProgramField(run, contract);
        Assert.True(field.IsComplete); Assert.Equal(2, E(field.Accepted).Length); Assert.Single(E(field.Frontier));
        Assert.Equal(4, E(field.Graph.Sites).Length); Assert.Equal(3, E(field.Graph.Strokes).Length);
        Assert.Equal(2, field.Graph.Describe(field.Graph.Evaluate()).Straight);
        foreach (var accepted in E(field.Accepted).Cast<Relation>())
        {
            var pair = Assert.IsType<Relation>(accepted.Left);
            Assert.Contains(E(field.SourceGraph.Sites), site => ReferenceEquals(site, pair.Left));
            Assert.Contains(E(field.SourceGraph.Sites), site => ReferenceEquals(site, pair.Right));
            Assert.Contains(E(field.Join.BindingChecks).Cast<Relation>(), check => ReferenceEquals(check.Left, accepted));
        }
    }

    [Fact]
    public void WrongPitchIsBlockedWithoutMovingTheUnits()
    {
        var f = new Fixture(pitch: 2); var run = f.Run(3, 1);
        var before = run.Units.Select(unit => (unit.Head, unit.Drawing.State, unit.Current.Graph)).ToArray();
        var field = new OrnamentProgramField(run, f.Contract(1, 0, f.End, f.Start));
        Assert.False(field.IsComplete); Assert.Empty(E(field.Accepted)); Assert.Equal(2, E(field.Blocked).Length);
        Assert.Empty(E(field.Unresolved)); Assert.Single(E(field.Frontier)); Assert.Equal(6, E(field.Graph.Sites).Length);
        for (var i = 0; i < run.Units.Count; i++)
        { Assert.Same(before[i].Head, run.Units[i].Head); Assert.Same(before[i].State, run.Units[i].Drawing.State); Assert.Same(before[i].Graph, run.Units[i].Current.Graph); }
    }

    [Fact]
    public void NegativeAndDiagonalOffsetsWalkActualWindowSlots()
    {
        var line = new Fixture(); var backwards = new OrnamentProgramField(line.Run(3, 1), line.Contract(-1, 0, line.Start, line.End));
        Assert.True(backwards.IsComplete); Assert.Equal(2, E(backwards.Accepted).Length); Assert.Single(E(backwards.Frontier));
        var f = new Fixture(diagonal: true); var diagonal = new OrnamentProgramField(f.Run(3, 3), f.Contract(1, 1, f.End, f.Start));
        Assert.True(diagonal.IsComplete); Assert.Equal(4, E(diagonal.Accepted).Length); Assert.Equal(5, E(diagonal.Frontier).Length);
        foreach (var walk in E(diagonal.Adjacencies).Cast<Relation>())
        {
            var steps = E((Relation)((Relation)walk.Right).Left).Cast<Relation>().ToArray(); Assert.Equal(2, steps.Length);
            Assert.All(steps, step => Assert.IsType<Relation>(step.Right));
        }
    }

    [Fact]
    public void MissingPublicRoleIsBlockedWhileOutsideTheFinitePatchIsFrontier()
    {
        var f = new Fixture(); var field = new OrnamentProgramField(f.Run(3, 1), f.Contract(1, 0, f.End, new Placeholder()));
        Assert.Empty(E(field.Accepted)); Assert.Equal(2, E(field.Blocked).Length); Assert.Single(E(field.Frontier));
        Assert.Empty(E(field.Unresolved));
    }

    [Fact]
    public void ClosurePublicationsResolveThroughEachUnitsExistingSiteMap()
    {
        var f = new Fixture(closed: true); var run = f.Run(3, 1);
        var field = new OrnamentProgramField(run, f.Contract(1, 0, f.Corner, f.End));
        Assert.True(field.IsComplete); Assert.Equal(2, E(field.Accepted).Length);
        foreach (var unit in run.Units)
        {
            Assert.True(UnitArrangementConnection.TryReadPublication(unit, f.End, out _, out var raw));
            Assert.DoesNotContain(E(unit.Current.Join.Graph.Sites), site => ReferenceEquals(site, raw.Left));
        }
        Assert.Equal(4, E(field.Graph.Sites).Length); Assert.Equal(6, E(field.Graph.Strokes).Length);
    }

    [Fact]
    public void ExplicitInternalSeamsJoinPathEndpointsButDoNotDeduplicateCoincidentStrokes()
    {
        var f = new Fixture(doublePath: true); var run = f.Run(1, 1);
        var field = new OrnamentProgramField(run, OrnamentProgramField.Contract(f.Program,
            f.Rule(0, 0, f.Start, f.OtherStart), f.Rule(0, 0, f.End, f.OtherEnd)));
        Assert.True(field.IsComplete); Assert.Equal(2, E(field.Accepted).Length); Assert.Empty(E(field.Frontier));
        Assert.Equal(2, E(field.Graph.Sites).Length); Assert.Equal(2, E(field.Graph.Strokes).Length);
        Assert.NotSame(E(field.Graph.Strokes)[0], E(field.Graph.Strokes)[1]);
    }

    [Fact]
    public void AnOpenOrForeignOffsetDoesNotInventACompletedWalk()
    {
        var f = new Fixture(); var foreign = new Fixture();
        var open = StructuralConstraint.Open(E(OrnamentProgramField.Offset(f.Program, 1, 0)));
        var a = new OrnamentProgramField(f.Run(2, 1), OrnamentProgramField.Contract(f.Program,
            OrnamentProgramField.Rule(f.Motif.Surface, f.Transform.Surface, f.Motif.Surface, f.Transform.Surface, open, f.End, f.Start)));
        var b = new OrnamentProgramField(f.Run(2, 1), OrnamentProgramField.Contract(f.Program,
            OrnamentProgramField.Rule(f.Motif.Surface, f.Transform.Surface, f.Motif.Surface, f.Transform.Surface,
                OrnamentProgramField.Offset(foreign.Program, 1, 0), f.End, f.Start)));
        Assert.All(new[] { a, b }, field =>
        { Assert.False(field.IsComplete); Assert.Empty(E(field.Accepted)); Assert.Empty(E(field.Frontier)); Assert.NotEmpty(E(field.Unresolved)); });
    }

    [Fact]
    public void OneNativeContractCanBeReusedAtDifferentSamplingWindows()
    {
        var f = new Fixture(); var contract = f.Contract(1, 0, f.End, f.Start);
        var a = new OrnamentProgramField(f.Run(2, 1), contract); var b = new OrnamentProgramField(f.Run(4, 1), contract);
        Assert.Same(a.SeamContract, b.SeamContract); Assert.True(a.IsComplete); Assert.True(b.IsComplete);
        Assert.Single(E(a.Accepted)); Assert.Equal(3, E(b.Accepted).Length); Assert.Single(E(a.Frontier)); Assert.Single(E(b.Frontier));
        Assert.NotSame(a.Run.Units[0].Head, b.Run.Units[0].Head);
    }

    [Fact]
    public void AFormerRunHeadCannotSupplyFreshSeamEvidenceAfterARevision()
    {
        var f = new Fixture(); var run = f.Run(2, 1); var application = (Relation)E(run.Applications)[0];
        var chosen = (Relation)((Relation)application.Right).Left; var position = (Relation)((Relation)((Relation)application.Right).Right).Left;
        var oldProgram = f.Motif; var replacement = new UnitArrangementProgram(f.Frame, "new local revision", oldProgram.Paths, oldProgram.Ports, oldProgram.ClosedPaths, S(new Placeholder()));
        var signal = new Placeholder(); var domain = S(oldProgram.Surface, replacement.Surface);
        var policy = FiniteLaw.Create(domain, S(signal), domain, FiniteLaw.Row(oldProgram.Surface, signal, S(replacement.Surface)));
        var unit = new UnitArrangementNode("mutable owner", [oldProgram, replacement], oldProgram, position, policy, f.Transform);
        var captured = unit.Current.Surface;
        var editedApplication = new Relation(application.Left, new Relation(chosen, new Relation(position, captured)));
        var changedRun = run with { Units = new[] { unit, run.Units[1] }, Applications = S(editedApplication, E(run.Applications)[1]) };
        unit.Commit(unit.Propose(signal, captured)); Assert.NotSame(captured, unit.Current.Surface);
        var field = new OrnamentProgramField(changedRun, f.Contract(1, 0, f.End, f.Start));
        Assert.False(field.IsComplete); Assert.Empty(E(field.Accepted)); Assert.NotEmpty(E(field.Unresolved));
    }

    [Theory]
    [InlineData("window")]
    [InlineData("bindings")]
    [InlineData("applications")]
    public void DetachedRecordChangesCannotOverrideTheActualNativeRun(string changed)
    {
        var f = new Fixture(); var run = f.Run(3, 1);
        var altered = changed switch
        {
            "window" => run with { Window = new Relation(run.Window.Left, run.Window.Right) },
            "bindings" => run with { Bindings = S(E(run.Bindings)) },
            "applications" => run with { Applications = S(E(run.Applications).Reverse().ToArray()) },
            _ => throw new ArgumentOutOfRangeException(nameof(changed))
        };
        var field = new OrnamentProgramField(altered, f.Contract(1, 0, f.End, f.Start));
        Assert.False(field.IsComplete); Assert.Empty(E(field.Accepted)); Assert.NotEmpty(E(field.Blocked));
    }

    [Fact]
    public void ReassignedNativeCellRowsStillNeedTheActualPhaseAndPlacementWitness()
    {
        var f = new Fixture(); var run = f.Run(2, 1); var apps = E(run.Applications).Cast<Relation>().ToArray();
        var a = (Relation)apps[0].Left; var b = (Relation)apps[1].Left;
        var changedApps = S(new Relation(new Relation(b.Left, a.Right), apps[0].Right),
            new Relation(new Relation(a.Left, b.Right), apps[1].Right));
        var root = (Relation)run.Surface.Right; var body = (Relation)root.Right;
        var surface = new Relation(run.Surface.Left, new Relation(root.Left, new Relation(changedApps, body.Right)));
        var altered = run with { Applications = changedApps, Surface = surface };
        var field = new OrnamentProgramField(altered, f.Contract(1, 0, f.End, f.Start));
        Assert.False(field.IsComplete); Assert.Empty(E(field.Accepted)); Assert.NotEmpty(E(field.Blocked));
    }

    private sealed class Fixture
    {
        internal QualitativeMotionFrame Frame { get; } = new();
        internal IElement Reach { get; } = new Placeholder();
        internal IElement Start { get; } = new Placeholder(); internal IElement End { get; } = new Placeholder();
        internal IElement Corner { get; } = new Placeholder(); internal IElement OtherStart { get; } = new Placeholder(); internal IElement OtherEnd { get; } = new Placeholder();
        internal UnitArrangementProgram Motif { get; }
        internal QualitativePatternTransform Transform { get; }
        internal OrnamentProgram Program { get; }
        private readonly Relation bindings; private readonly IElement u; private readonly IElement v;
        internal Fixture(int pitch = 1, bool diagonal = false, bool closed = false, bool doublePath = false)
        {
            Relation W(params IElement[] directions) => S(directions.Select(direction => (IElement)QualitativeMotionFrame.Move(direction, Reach)).ToArray());
            var right = W(Frame.Right); var chord = diagonal ? W(Frame.Right, Frame.Up) : right; var left = W(Frame.Left);
            var path = new Relation(S(), closed ? S(right, left) : S(chord)); var paths = new List<IElement> { path };
            var ports = new List<IElement> { new Relation(Start, new Relation(path, closed ? right : chord)), new Relation(End, new Relation(path, path)) };
            if (closed) ports.Add(new Relation(Corner, new Relation(path, left)));
            if (doublePath)
            {
                var other = new Relation(S(), S(chord)); paths.Add(other);
                ports.Add(new Relation(OtherStart, new Relation(other, chord))); ports.Add(new Relation(OtherEnd, new Relation(other, other)));
            }
            Motif = new(Frame, "native unit", S(paths.ToArray()), S(ports.ToArray()), closed ? S(path) : S());
            Transform = new(Frame, Frame, S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }
                .Select(direction => (IElement)new Relation(QualitativeMotionFrame.Move(direction, Reach), W(direction))).ToArray()));
            var m = new Placeholder(); var t = new Placeholder(); var x = new Placeholder(); var y = new Placeholder(); u = new Placeholder(); v = new Placeholder();
            var us = new Relation(S(u), S(new Relation(u, u))); var vs = new Relation(S(v), S(new Relation(v, v)));
            var stepX = W(Enumerable.Repeat<IElement>(Frame.Right, pitch).ToArray()); var stepY = W(Frame.Up);
            var parameters = S(new Relation(m, S(Motif.Surface)), new Relation(t, S(Transform.Surface)), new Relation(x, S(stepX)), new Relation(y, S(stepY)));
            var layout = new Relation(new Relation(us, vs), new Relation(new Relation(x, y), S(new Relation(new Relation(u, v), new Relation(m, t)))));
            Program = new(Frame, S(Motif.Surface), S(Transform.Surface), parameters, layout);
            bindings = S(new Relation(m, Motif.Surface), new Relation(t, Transform.Surface), new Relation(x, stepX), new Relation(y, stepY));
        }
        internal OrnamentProgramRun Run(int columns, int rows) => Program.Generate(bindings, OrnamentProgram.Window(u, v, columns, rows));
        internal Relation Rule(int columns, int rows, IElement from, IElement to) => OrnamentProgramField.Rule(Motif.Surface, Transform.Surface,
            Motif.Surface, Transform.Surface, OrnamentProgramField.Offset(Program, columns, rows), from, to);
        internal Relation Contract(int columns, int rows, IElement from, IElement to) => OrnamentProgramField.Contract(Program, Rule(columns, rows, from, to));
    }
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var elements, out _, out _) ? elements : [];
}
