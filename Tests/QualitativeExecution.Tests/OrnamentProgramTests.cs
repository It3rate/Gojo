using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentProgramTests
{
    [Fact]
    public void SharedNativeDefinitionCreatesIndependentAcknowledgedOccurrences()
    {
        var f = new Fixture(); var run = f.Program.Generate(f.Bindings, f.Window(2, 2));
        Assert.True(run.IsComplete); Assert.Empty(E(run.Blocked)); Assert.Empty(E(run.Unresolved));
        Assert.Equal(4, run.Units.Count); Assert.Equal(4, E(run.Applications).Length);
        var first = run.Units[0];
        foreach (var unit in run.Units)
        {
            Assert.Same(first.Current.Program, unit.Current.Program);
            Assert.Same(f.Motif.Paths, unit.Current.Program.Paths);
            Assert.Equal(4, E(unit.Current.Graph.Strokes).Length);
            Assert.Single(E(unit.Current.Execution.CompletionEvents));
            Assert.True(unit.Current.Join.TryReadPassed(unit.Current.Join.Evaluate()));
        }
        Assert.Equal(4, run.Units.Select(unit => unit.Drawing).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(4, run.Units.Select(unit => unit.Head).Distinct(ReferenceEqualityComparer.Instance).Count());
        var siteSets = run.Units.Select(unit => E(unit.Current.Graph.Sites)).ToArray();
        for (var a = 0; a < siteSets.Length; a++) for (var b = a + 1; b < siteSets.Length; b++)
            Assert.DoesNotContain(siteSets[a], site => siteSets[b].Any(other => ReferenceEquals(site, other)));
        foreach (var application in E(run.Applications).Cast<Relation>())
        {
            var result = Assert.IsType<Relation>(application.Right);
            var selected = Assert.IsType<Relation>(result.Left);
            Assert.Same(f.Motif.Surface, selected.Left); Assert.Same(f.Identity.Surface, selected.Right);
        }
        var lowerings = Assert.IsType<Relation>(Assert.IsType<Relation>(run.Surface.Right).Left);
        Assert.Contains(E(lowerings), row => row is Relation binding && ReferenceEquals(binding.Left, f.Motif.Surface) &&
            ReferenceEquals(binding.Right, first.Current.Program.Surface));
        Assert.All(Reachable(f.Program.Surface), element => Assert.True(element is Relation or Placeholder));
    }

    [Fact]
    public void ABindingChangesTheActualSubstitutionWithoutChangingTheSharedRecipe()
    {
        var f = new Fixture(); var surface = f.Program.Surface;
        var plain = f.Program.Generate(f.Bindings, f.Window(1, 1));
        var mirror = f.Program.Generate(f.Bind(f.Mirror.Surface), f.Window(1, 1));
        Assert.True(plain.IsComplete); Assert.True(mirror.IsComplete); Assert.Same(surface, f.Program.Surface);
        Assert.Same(f.Motif.Paths, plain.Units[0].Current.Program.Paths);
        Assert.Same(f.Motif.Paths, mirror.Units[0].Current.Program.Paths);
        Assert.Equal("RUld", Directions(plain.Units[0].Current.ExecutedProgram));
        Assert.Equal("RdlU", Directions(mirror.Units[0].Current.ExecutedProgram));
        Assert.NotSame(plain.Units[0].Head, mirror.Units[0].Head);
    }

    [Fact]
    public void ArchiveRestoresAFreshSourceWorldAndPreservesWithinWorldAliases()
    {
        var f = new Fixture(); var json = f.Program.Save(f.Bindings, f.Window(2, 2));
        Assert.True(OrnamentProgram.TryLoad(json, out var sample, out var unresolved)); Assert.Empty(E(unresolved));
        Assert.NotSame(f.Program.Surface, sample.Source.Left); Assert.NotSame(f.Frame, sample.Program.Frame);
        Assert.All(Reachable(sample.Source).OfType<Placeholder>(), actual =>
            Assert.DoesNotContain(Reachable(f.Program.Surface).OfType<Placeholder>(), original => original.SameSlotAs(actual)));
        var motif = Assert.IsType<Relation>(Assert.Single(E(sample.Program.Motifs)));
        var parameter = E(sample.Program.Parameters).Cast<Relation>().Single(row =>
            row.Right is Relation domain && E(domain).Any(choice => ReferenceEquals(choice, motif)));
        var chosen = E(sample.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, parameter.Left));
        Assert.Same(motif, chosen.Right);
        Assert.Contains(Reachable(sample.Correspondence), element => ReferenceEquals(element, sample.Source));
        Assert.DoesNotContain("rectangle fixture", json, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadedNativeProgramExecutesWithoutAnAuthoringFactory()
    {
        var f = new Fixture(); var bindings = f.Bind(f.Mirror.Surface); var window = f.Window(2, 2);
        var before = f.Program.Generate(bindings, window);
        Assert.True(OrnamentProgram.TryLoad(f.Program.Save(bindings, window), out var sample, out _));
        var after = sample.Program.Generate(sample.Bindings, sample.Window);
        Assert.True(after.IsComplete); Assert.Equal(before.Units.Count, after.Units.Count);
        Assert.Equal(before.Units.Select(unit => Directions(unit.Current.ExecutedProgram)),
            after.Units.Select(unit => Directions(unit.Current.ExecutedProgram)));
        Assert.All(after.Units, unit => Assert.Same(after.Units[0].Current.Program, unit.Current.Program));
        Assert.All(after.Units, unit => Assert.True(unit.Current.Join.TryReadPassed(unit.Current.Join.Evaluate())));
        Assert.NotSame(before.Units[0].Head, after.Units[0].Head);
    }

    [Fact]
    public void ActualSuccessorLinksAndRuleReferencesControlAlternationAndPhaseOrigin()
    {
        var f = new Fixture(); var secondPhase = new Placeholder(); var mirrorSlot = new Placeholder();
        var cycle = new Relation(S(f.UPhase, secondPhase),
            S(new Relation(f.UPhase, secondPhase), new Relation(secondPhase, f.UPhase)));
        var plainRule = new Relation(new Relation(f.UPhase, f.VPhase), new Relation(f.MotifSlot, f.TransformSlot));
        var mirrorRule = new Relation(new Relation(secondPhase, f.VPhase), new Relation(f.MotifSlot, mirrorSlot));
        var parameters = S([.. E(f.Parameters), new Relation(mirrorSlot, S(f.Mirror.Surface))]);
        var layout = new Relation(new Relation(cycle, f.VCycle),
            new Relation(new Relation(f.StepUSlot, f.StepVSlot), S(plainRule, mirrorRule)));
        var program = new OrnamentProgram(f.Frame, f.Program.Motifs, f.Program.Transforms, parameters, layout);
        var bindings = S([.. E(f.Bindings), new Relation(mirrorSlot, f.Mirror.Surface)]);
        var first = program.Generate(bindings, OrnamentProgram.Window(f.UPhase, f.VPhase, 3, 1));
        var shifted = program.Generate(bindings, OrnamentProgram.Window(secondPhase, f.VPhase, 3, 1));
        Assert.True(first.IsComplete); Assert.True(shifted.IsComplete);
        Assert.Equal(new[] { "RUld", "RdlU", "RUld" }, first.Units.Select(unit => Directions(unit.Current.ExecutedProgram)));
        Assert.Equal(new[] { "RdlU", "RUld", "RdlU" }, shifted.Units.Select(unit => Directions(unit.Current.ExecutedProgram)));
        var firstRules = E(first.Applications).Cast<Relation>().Select(application => ((Relation)application.Left).Right).ToArray();
        Assert.Same(plainRule, firstRules[0]); Assert.Same(mirrorRule, firstRules[1]); Assert.Same(plainRule, firstRules[2]);
    }

    [Fact]
    public void MissingParameterIsAnOpenObligationAndProducesNoDefaultOccurrence()
    {
        var f = new Fixture(); var partial = S(E(f.Bindings).Where(row => row is Relation binding &&
            !ReferenceEquals(binding.Left, f.MotifSlot)).ToArray());
        var run = f.Program.Generate(partial, f.Window(2, 2));
        Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.Empty(E(run.Applications));
        Assert.NotEmpty(E(run.Unresolved)); Assert.Empty(E(run.Blocked));
        Assert.Contains(Reachable(run.Unresolved), element => ReferenceEquals(element, f.MotifSlot));
    }

    [Fact]
    public void ALookalikeDefinitionOutsideTheFiniteChoiceScopeIsBlocked()
    {
        var f = new Fixture(); var lookalike = new Relation(f.Motif.Surface.Left, f.Motif.Surface.Right);
        var choices = S(E(f.Bindings).Select(row => row is Relation binding && ReferenceEquals(binding.Left, f.MotifSlot)
            ? (IElement)new Relation(f.MotifSlot, lookalike) : row).ToArray());
        var run = f.Program.Generate(choices, f.Window(1, 1));
        Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Blocked));
        Assert.Contains(Reachable(run.Blocked), element => ReferenceEquals(element, lookalike));
    }

    [Fact]
    public void TwoBindingsToTheSameSlotDoNotUseFirstBindingWins()
    {
        var f = new Fixture(); var sameSlot = f.TransformSlot.Dual().Dual();
        var run = f.Program.Generate(S([.. E(f.Bindings), new Relation(sameSlot, f.Mirror.Surface)]), f.Window(1, 1));
        Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Blocked));
    }

    [Fact]
    public void OpenBindingsAndOpenChoiceInventoryRemainUnresolved()
    {
        var f = new Fixture();
        var openBindings = f.Program.Generate(StructuralConstraint.Open(E(f.Bindings)), f.Window(1, 1));
        var parameters = S(E(f.Parameters).Select(row => row is Relation parameter && ReferenceEquals(parameter.Left, f.MotifSlot)
            ? (IElement)new Relation(f.MotifSlot, StructuralConstraint.Open(f.Motif.Surface)) : row).ToArray());
        var openDomain = f.WithParameters(parameters).Generate(f.Bindings, f.Window(1, 1));
        foreach (var run in new[] { openBindings, openDomain })
        { Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Unresolved)); }
    }

    [Fact]
    public void CyclesMustHaveActualOrderedSuccessorLinksBeforeAnyDrawing()
    {
        var f = new Fixture(); var otherPhase = new Placeholder();
        var invalidCycle = new Relation(S(f.UPhase, otherPhase),
            S(new Relation(f.UPhase, f.UPhase), new Relation(otherPhase, otherPhase)));
        var run = f.WithCycles(invalidCycle, f.VCycle).Generate(f.Bindings, f.Window(2, 1));
        Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Blocked));
        Assert.Contains(Reachable(run.Blocked), element => ReferenceEquals(element, invalidCycle));
    }

    [Fact]
    public void MissingRulesStayOpenWhileConflictingRulesAreBlocked()
    {
        var f = new Fixture(); var rule = Assert.Single(E(f.Rules));
        var missing = f.WithRules(S()).Generate(f.Bindings, f.Window(1, 1));
        var conflicting = f.WithRules(S(rule, rule)).Generate(f.Bindings, f.Window(1, 1));
        Assert.Empty(missing.Units); Assert.NotEmpty(E(missing.Unresolved)); Assert.Empty(E(missing.Blocked));
        Assert.Empty(conflicting.Units); Assert.NotEmpty(E(conflicting.Blocked));
    }

    [Fact]
    public void AnOpenSamplingWindowDoesNotPretendToBeAFinitePatch()
    {
        var f = new Fixture(); var window = new Relation(new Relation(f.UPhase, f.VPhase),
            new Relation(StructuralConstraint.Open(new Placeholder()), S(new Placeholder())));
        var run = f.Program.Generate(f.Bindings, window);
        Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Unresolved));
        Assert.Contains(Reachable(run.Unresolved), element => ReferenceEquals(element, ((Relation)window.Right).Left));
    }

    [Fact]
    public void RepeatedSamplingIdentityAndForeignStartPhaseAreBlocked()
    {
        var f = new Fixture(); var shared = new Placeholder();
        var duplicate = new Relation(new Relation(f.UPhase, f.VPhase), new Relation(S(shared), S(shared)));
        var foreign = OrnamentProgram.Window(new Placeholder(), f.VPhase, 1, 1);
        foreach (var window in new[] { duplicate, foreign })
        {
            var run = f.Program.Generate(f.Bindings, window);
            Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Blocked));
        }
    }

    [Fact]
    public void SamplingSlotsCannotAliasPhasesParametersOrSharedDefinitionReaches()
    {
        var f = new Fixture();
        foreach (var alias in new[] { f.UPhase, f.MotifSlot, f.Reach, f.Reach.Dual() })
        {
            var window = new Relation(new Relation(f.UPhase, f.VPhase), new Relation(S(alias), S(new Placeholder())));
            var run = f.Program.Generate(f.Bindings, window);
            Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Blocked));
        }
        // A definition may expose only the opposed face of a slot. Its
        // forward face still cannot be reused as a fresh occurrence identity.
        var inverseOnly = new Placeholder();
        var motif = new UnitArrangementProgram(f.Frame, "opposed evidence slot", f.Motif.Paths, f.Motif.Ports,
            f.Motif.ClosedPaths, S(inverseOnly.Dual()));
        var parameters = S(E(f.Parameters).Select(row => row is Relation parameter && ReferenceEquals(parameter.Left, f.MotifSlot)
            ? (IElement)new Relation(f.MotifSlot, S(motif.Surface)) : row).ToArray());
        var bindings = S(E(f.Bindings).Select(row => row is Relation binding && ReferenceEquals(binding.Left, f.MotifSlot)
            ? (IElement)new Relation(f.MotifSlot, motif.Surface) : row).ToArray());
        var program = new OrnamentProgram(f.Frame, S(motif.Surface), f.Program.Transforms, parameters, f.Layout);
        var aliasedWindow = new Relation(new Relation(f.UPhase, f.VPhase), new Relation(S(inverseOnly), S(new Placeholder())));
        var inverseAlias = program.Generate(bindings, aliasedWindow);
        Assert.False(inverseAlias.IsComplete); Assert.Empty(inverseAlias.Units); Assert.NotEmpty(E(inverseAlias.Blocked));
    }

    [Fact]
    public void ASelectedIncompleteTransformBlocksTheWholePatchBeforeActuation()
    {
        var f = new Fixture(); var incomplete = new QualitativePatternTransform(f.Frame, f.Frame,
            S(new Relation(Assert.Single(E(f.R)), f.R)));
        var transforms = S(f.Identity.Surface, incomplete.Surface);
        var parameters = S(E(f.Parameters).Select(row => row is Relation parameter && ReferenceEquals(parameter.Left, f.TransformSlot)
            ? (IElement)new Relation(f.TransformSlot, transforms) : row).ToArray());
        var program = new OrnamentProgram(f.Frame, S(f.Motif.Surface), transforms, parameters, f.Layout);
        var run = program.Generate(f.Bind(incomplete.Surface), f.Window(2, 2));
        Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.NotEmpty(E(run.Blocked));
    }

    [Fact]
    public void CompleteMoveSubstitutionsCannotSilentlyInvalidateAClosureClaim()
    {
        var f = new Fixture();
        var opensContour = new QualitativePatternTransform(f.Frame, f.Frame, S(
            new Relation(Assert.Single(E(f.R)), f.R), new Relation(Assert.Single(E(f.L)), f.L),
            new Relation(Assert.Single(E(f.U)), f.U), new Relation(Assert.Single(E(f.D)), f.U)));
        var transforms = S(opensContour.Surface);
        var parameters = S(E(f.Parameters).Select(row => row is Relation parameter && ReferenceEquals(parameter.Left, f.TransformSlot)
            ? (IElement)new Relation(f.TransformSlot, transforms) : row).ToArray());
        var program = new OrnamentProgram(f.Frame, f.Program.Motifs, transforms, parameters, f.Layout);
        var run = program.Generate(f.Bind(opensContour.Surface), f.Window(2, 2));
        Assert.False(run.IsComplete); Assert.Empty(run.Units); Assert.Empty(E(run.Applications));
        Assert.NotEmpty(E(run.Blocked));
        var lowerings = Assert.IsType<Relation>(Assert.IsType<Relation>(run.Surface.Right).Left);
        var actualMapping = E(lowerings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, opensContour.Surface)).Right;
        Assert.Contains(Reachable(run.Blocked), element => ReferenceEquals(element, actualMapping));
    }

    [Fact]
    public void FiniteSamplingSlotsBoundExpansionAndAreNotPartOfTheReusableDefinition()
    {
        var f = new Fixture(); var surface = f.Program.Surface;
        var smallWindow = f.Window(1, 1); var largerWindow = f.Window(3, 2);
        var small = f.Program.Generate(f.Bindings, smallWindow); var larger = f.Program.Generate(f.Bindings, largerWindow);
        Assert.True(small.IsComplete); Assert.True(larger.IsComplete);
        Assert.Single(small.Units); Assert.Equal(6, larger.Units.Count); Assert.Same(surface, f.Program.Surface);
        Assert.DoesNotContain(Reachable(surface), element => ReferenceEquals(element, largerWindow));
    }

    [Fact]
    public void BrokenTransportAndUnsupportedFormatVersionRemainUnreadable()
    {
        var f = new Fixture(); var saved = f.Program.Save(f.Bindings, f.Window(1, 1));
        var document = JsonNode.Parse(saved)!; var missingVersion = JsonNode.Parse(saved)!;
        missingVersion.AsObject().Remove("version");
        document["version"] = 99;
        foreach (var input in new[] { "{ broken", document.ToJsonString(), missingVersion.ToJsonString() })
        {
            Assert.False(OrnamentProgram.TryLoad(input, out _, out var unresolved));
            Assert.True(StructuralConstraint.TryReadScope(unresolved, out _, out var seam, out var tail));
            Assert.False(seam.Equals(tail));
        }
    }

    [Fact]
    public void NullNativeTablesAndAbsentRowsRemainUnreadableRatherThanThrowing()
    {
        var f = new Fixture(); var saved = f.Program.Save(f.Bindings, f.Window(1, 1));
        Action<JsonNode>[] edits =
        [
            document => document["native"] = null,
            document => document["native"]!["nodes"] = null,
            document => document["native"]!["carriers"] = null,
            document => document["native"]!["nodes"]!.AsArray()[0] = null,
            document => document["native"]!["carriers"]!.AsArray().Add((JsonNode?)null)
        ];
        foreach (var edit in edits)
        {
            var document = JsonNode.Parse(saved)!; edit(document);
            AssertUnreadable(document.ToJsonString());
        }
    }

    [Fact]
    public void MissingAndIncompatibleArchiveReferencesCannotAcquireExecutableDefaults()
    {
        var f = new Fixture(); var saved = f.Program.Save(f.Bindings, f.Window(1, 1));
        Action<JsonNode>[] edits =
        [
            document => document["native"]!["root"] = null,
            document => document["native"]!["root"] = "absent-node",
            document => document["native"]!["nodes"] = new JsonObject(),
            document => document["native"]!["nodes"]!.AsArray()[0]!["id"] = null,
            document => document["native"]!["nodes"]!.AsArray()[1]!["id"] =
                document["native"]!["nodes"]!.AsArray()[0]!["id"]!.GetValue<string>(),
            document => document["native"]!["nodes"]!.AsArray()[0]!["kind"] = "unknown-storage-kind",
            document => document["native"]!["nodes"]!.AsArray().First(node =>
                node!["kind"]!.GetValue<string>() == "relation")!["left"] = "absent-node",
            document => document["native"]!["nodes"]!.AsArray().First(node =>
                node!["kind"]!.GetValue<string>() == "relation")!["right"] = null,
            document => document["native"]!["nodes"]!.AsArray().First(node =>
                node!["kind"]!.GetValue<string>() == "placeholder")!["ticks"] = null
        ];
        foreach (var edit in edits)
        {
            var document = JsonNode.Parse(saved)!; edit(document);
            AssertUnreadable(document.ToJsonString());
        }
    }

    private static void AssertUnreadable(string input)
    {
        Assert.False(OrnamentProgram.TryLoad(input, out var sample, out var unresolved)); Assert.Null(sample);
        Assert.True(StructuralConstraint.TryReadScope(unresolved, out _, out var seam, out var tail));
        Assert.False(seam.Equals(tail));
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public Placeholder Reach { get; } = new();
        public Placeholder MotifSlot { get; } = new();
        public Placeholder TransformSlot { get; } = new();
        public Placeholder StepUSlot { get; } = new();
        public Placeholder StepVSlot { get; } = new();
        public Placeholder UPhase { get; } = new();
        public Placeholder VPhase { get; } = new();
        public Relation R { get; }
        public Relation L { get; }
        public Relation U { get; }
        public Relation D { get; }
        public UnitArrangementProgram Motif { get; }
        public QualitativePatternTransform Identity { get; }
        public QualitativePatternTransform Mirror { get; }
        public Relation Parameters { get; }
        public Relation UCycle { get; }
        public Relation VCycle { get; }
        public Relation Rules { get; }
        public Relation Layout { get; }
        public OrnamentProgram Program { get; }
        public Relation Bindings => Bind(Identity.Surface);

        public Fixture()
        {
            R = S(QualitativeMotionFrame.Move(Frame.Right, Reach)); L = S(QualitativeMotionFrame.Move(Frame.Left, Reach));
            U = S(QualitativeMotionFrame.Move(Frame.Up, Reach)); D = S(QualitativeMotionFrame.Move(Frame.Down, Reach));
            var path = new Relation(S(), S(R, U, L, D));
            Motif = new(Frame, "rectangle fixture", S(path), S(new Relation(new Placeholder(), new Relation(path, path))), S(path));
            Identity = Transform(R, L, U, D); Mirror = Transform(R, L, D, U);
            Parameters = S(new Relation(MotifSlot, S(Motif.Surface)), new Relation(TransformSlot, S(Identity.Surface, Mirror.Surface)),
                new Relation(StepUSlot, S(Word(R, R, R))), new Relation(StepVSlot, S(Word(U, U, U))));
            UCycle = new(S(UPhase), S(new Relation(UPhase, UPhase))); VCycle = new(S(VPhase), S(new Relation(VPhase, VPhase)));
            Rules = S(new Relation(new Relation(UPhase, VPhase), new Relation(MotifSlot, TransformSlot)));
            Layout = new(new Relation(UCycle, VCycle), new Relation(new Relation(StepUSlot, StepVSlot), Rules));
            Program = new(Frame, S(Motif.Surface), S(Identity.Surface, Mirror.Surface), Parameters, Layout);
        }

        private QualitativePatternTransform Transform(Relation right, Relation left, Relation up, Relation down) => new(Frame, Frame, S(
            new Relation(Assert.Single(E(R)), right), new Relation(Assert.Single(E(L)), left),
            new Relation(Assert.Single(E(U)), up), new Relation(Assert.Single(E(D)), down)));
        public Relation Bind(IElement transform) => S(new Relation(MotifSlot, Motif.Surface), new Relation(TransformSlot, transform),
            new Relation(StepUSlot, Assert.Single(E((Relation)E(Parameters).Cast<Relation>().Single(row => ReferenceEquals(row.Left, StepUSlot)).Right))),
            new Relation(StepVSlot, Assert.Single(E((Relation)E(Parameters).Cast<Relation>().Single(row => ReferenceEquals(row.Left, StepVSlot)).Right))));
        public Relation Window(int columns, int rows) => OrnamentProgram.Window(UPhase, VPhase, columns, rows);
        public OrnamentProgram WithParameters(Relation parameters) => new(Frame, Program.Motifs, Program.Transforms, parameters, Layout);
        public OrnamentProgram WithRules(Relation rules) => new(Frame, Program.Motifs, Program.Transforms, Parameters,
            new(new Relation(UCycle, VCycle), new Relation(new Relation(StepUSlot, StepVSlot), rules)));
        public OrnamentProgram WithCycles(Relation u, Relation v) => new(Frame, Program.Motifs, Program.Transforms, Parameters,
            new(new Relation(u, v), new Relation(new Relation(StepUSlot, StepVSlot), Rules)));
    }

    private static string Directions(UnitArrangementProgram program) => string.Concat(E(program.Paths).Cast<Relation>()
        .SelectMany(path => E((Relation)path.Right)).Cast<Relation>().SelectMany(E).Cast<Relation>().Select(move =>
            ReferenceEquals(move.Left, program.Frame.Right) ? 'R' : ReferenceEquals(move.Left, program.Frame.Up) ? 'U' :
            ReferenceEquals(move.Left, program.Frame.Left) ? 'l' : ReferenceEquals(move.Left, program.Frame.Down) ? 'd' : '?'));
    private static Relation Word(params Relation[] words) => S(words.SelectMany(E).ToArray());
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var next))
        { if (!seen.Add(next)) continue; if (next is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
        return seen.ToArray();
    }
}
