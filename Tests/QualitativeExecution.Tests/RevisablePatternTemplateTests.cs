using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using T = Gojo.Experiments.QualitativeFrieze.RevisablePatternTemplate;
using H = Gojo.Experiments.QualitativeFrieze.ProvisionalEquivalence.Hypothesis;

namespace QualitativeExecution.Tests;

public sealed class RevisablePatternTemplateTests
{
    [Fact]
    public void Shared_parameters_propose_equivalence_while_actual_execution_independently_supplies_role_values()
    {
        var (template, roles) = Contour(); var unit = new Placeholder(); var parameter = new Placeholder(); var word = S(unit, unit, unit);
        var hypothesis = template.Hypothesis(S(roles.Select(role => (IElement)new Relation(role, parameter)).ToArray()), S(new Relation(parameter, word)));
        Assert.Equal(6, E(hypothesis.Equalities).Length);
        var result = template.Realize(hypothesis);
        Assert.Equal("Yes", result.Answer); Assert.NotNull(result.Sample); Assert.Single(E(result.Accepted));
        Assert.Equal(4, E(result.RoleValues).Length);
        foreach (var row in E(result.RoleValues).Cast<Relation>())
        {
            var measured = Assert.IsType<Relation>(row.Right);
            Assert.NotSame(word, measured);
            Assert.Equal(new IElement[] { unit, unit, unit }, E(measured));
        }
        Assert.Equal(4, E(result.Sample.Observation.Segments).Length);
        Assert.Equal(E(template.Program.Ports).Length, E(result.PortCorrespondence).Length);
        Assert.Equal(E(template.Program.Ports).Length, E(result.ExecutionPortCorrespondence).Length);
        Assert.Equal(4, E(result.RoleEvidence).Length);
        var witnessWorld = OrnamentReconstruction.Participants(result.RoleEvidence);
        Assert.All(E(result.Sample.MechanicalEvidence), row => Assert.True(witnessWorld.Contains(row), "Actual emitted mechanical evidence must remain connected."));
        Assert.All(E(result.Sample.Bindings), binding => Assert.Contains(binding, witnessWorld));
        Assert.DoesNotContain(hypothesis.ParameterValues, OrnamentReconstruction.Participants(result.RoleValues));
    }

    [Fact]
    public void Splitting_shared_parameters_changes_actual_geometry_and_can_preserve_a_valid_closure()
    {
        var (template, roles) = Contour(); var unit = new Placeholder(); var width = new Placeholder(); var height = new Placeholder();
        var hypothesis = template.Hypothesis(S(new Relation(roles[0], width), new Relation(roles[1], height), new Relation(roles[2], width), new Relation(roles[3], height)),
            S(new Relation(width, S(unit, unit, unit, unit)), new Relation(height, S(unit, unit))));
        Assert.Equal(2, E(hypothesis.Equalities).Length);
        var result = template.Realize(hypothesis);
        Assert.Equal("Yes", result.Answer);
        Assert.Equal(new[] { 4, 2, 4, 2 }, E(result.RoleValues).Cast<Relation>().Select(row => E((Relation)row.Right).Length));
        Assert.Equal(4, E(result.Sample!.Observation.Segments).Length);
    }

    [Fact]
    public void Missing_binding_is_open_and_zero_or_invalid_participation_is_blocked()
    {
        var (template, roles) = Contour(); var unit = new Placeholder(); var parameter = new Placeholder();
        var bindings = S(roles.Select(role => (IElement)new Relation(role, parameter)).ToArray());
        var missing = template.Realize(template.Hypothesis(S(E(bindings)[..^1]), S(new Relation(parameter, S(unit)))));
        Assert.Equal("Open", missing.Answer); Assert.Null(missing.Sample); Assert.Empty(E(missing.Accepted));
        var zero = template.Realize(template.Hypothesis(bindings, S(new Relation(parameter, S()))));
        Assert.Equal("Blocked", zero.Answer); Assert.Null(zero.Sample);
        var opposed = template.Realize(template.Hypothesis(bindings, S(new Relation(parameter, S(unit.Dual())))));
        Assert.Equal("Blocked", opposed.Answer);
        var unknown = template.Realize(template.Hypothesis(bindings, S(new Relation(parameter, StructuralConstraint.Open(unit)))));
        Assert.Equal("Open", unknown.Answer); Assert.Null(unknown.Sample);
    }

    [Fact]
    public void Concrete_unequal_opposed_sides_cannot_fake_closure_and_independent_unknown_reaches_remain_open()
    {
        var (template, roles) = Contour(); var unit = new Placeholder(); var common = new Placeholder(); var changed = new Placeholder();
        var bindings = S(roles.Select((role, i) => (IElement)new Relation(role, i == 2 ? changed : common)).ToArray());
        var impossible = template.Realize(template.Hypothesis(bindings,
            S(new Relation(common, S(unit, unit)), new Relation(changed, S(unit, unit, unit)))));
        Assert.Equal("Blocked", impossible.Answer); Assert.Null(impossible.Sample); Assert.NotEmpty(E(impossible.Blocked));
        var unresolved = template.Realize(template.Hypothesis(bindings,
            S(new Relation(common, S(unit)), new Relation(changed, S(new Placeholder())))));
        Assert.Equal("Open", unresolved.Answer); Assert.Null(unresolved.Sample); Assert.Empty(E(unresolved.Blocked));
    }

    [Fact]
    public void Single_contour_and_four_joined_side_programs_share_a_visible_graph_but_keep_distinct_mechanics()
    {
        var (contour, contourRoles) = Contour(); var (sides, sideRoles) = Sides(); var unit = new Placeholder();
        T.Realization Run(T template, IElement[] roles)
        {
            var p = new Placeholder(); return template.Realize(template.Hypothesis(S(roles.Select(role => (IElement)new Relation(role, p)).ToArray()), S(new Relation(p, S(unit, unit)))));
        }
        var one = Run(contour, contourRoles); var four = Run(sides, sideRoles);
        Assert.Equal("Yes", one.Answer); Assert.Equal("Yes", four.Answer);
        Assert.Single(E(one.Sample!.MechanicalGroups)); Assert.Equal(4, E(four.Sample!.MechanicalGroups).Length);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(one.Sample.Observation, four.Sample.Observation, 100000).Answer);
        Assert.Equal(4, E(four.Sample.Join.Accepted).Length);
        var first = new Placeholder(); var second = new Placeholder();
        var cannotJoin = sides.Realize(sides.Hypothesis(S(sideRoles.Select((role, i) => (IElement)new Relation(role, i == 2 ? second : first)).ToArray()),
            S(new Relation(first, S(unit, unit)), new Relation(second, S(unit, unit, unit)))));
        Assert.Equal("Blocked", cannotJoin.Answer); Assert.Null(cannotJoin.Sample);
    }

    [Fact]
    public void Native_template_and_hypothesis_transport_reexecutes_without_original_factory_or_frame_identity()
    {
        var (template, roles) = Sides(); var parameter = new Placeholder(); var atom = new Placeholder();
        var h = template.Hypothesis(S(roles.Select(role => (IElement)new Relation(role, parameter)).ToArray()), S(new Relation(parameter, S(atom, atom, atom))));
        var before = template.Realize(h); Assert.Equal("Yes", before.Answer);
        var world = Restore(new Relation(template.Surface, h.Surface));
        Assert.True(T.TryRead((Relation)world.Left, out var imported, out var proof));
        var copied = ReadHypothesis((Relation)world.Right);
        var after = imported.Realize(copied);
        Assert.Equal("Yes", after.Answer); Assert.NotNull(after.Sample);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(before.Sample!.Observation, after.Sample.Observation, 100000).Answer);
        Assert.Equal(4, E(after.RoleValues).Length);
        Assert.DoesNotContain(template.Surface, OrnamentReconstruction.Participants(after.Surface));
        Assert.Contains(world.Left, OrnamentReconstruction.Participants(proof));
        Assert.True(OrnamentReconstructionCases.TryGenerate(Restore(after.Sample.Recipe), out var third, out _));
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(after.Sample.Observation, third!.Observation, 100000).Answer);
    }

    [Fact]
    public void Compound_only_parameter_role_is_not_falsely_certified_as_an_independent_output_read()
    {
        var frame = new QualitativeMotionFrame(); var role = new Placeholder(); var atom = new Placeholder(); var parameter = new Placeholder();
        var chord = S(QualitativeMotionFrame.Move(frame.Right, role), QualitativeMotionFrame.Move(frame.Up, role));
        var path = new Relation(S(), S(chord));
        var template = new T(new(frame, "two contributions on one chord", S(path), LatticeLadderAuthor.Ports(path)), S(role));
        var result = template.Realize(template.Hypothesis(S(new Relation(role, parameter)), S(new Relation(parameter, S(atom, atom)))));
        Assert.Equal("Open", result.Answer); Assert.NotNull(result.Sample);
        Assert.Empty(E(result.Accepted)); Assert.Empty(E(result.RoleValues));
    }

    [Fact]
    public void Static_moves_survive_and_shared_chord_identity_is_preserved_across_distinct_paths()
    {
        var frame = new QualitativeMotionFrame(); var role = new Placeholder(); var unit = new Placeholder(); var parameter = new Placeholder();
        var shared = S(QualitativeMotionFrame.Move(frame.Right, role));
        var first = new Relation(S(), S(shared));
        var second = new Relation(S(QualitativeMotionFrame.Move(frame.Up, unit)), S(shared));
        var template = new T(new(frame, "two parallel participants sharing a chord recipe", S(first, second), LatticeLadderAuthor.Ports(first, second)), S(role));
        var result = template.Realize(template.Hypothesis(S(new Relation(role, parameter)), S(new Relation(parameter, S(unit, unit, unit)))));
        Assert.Equal("Yes", result.Answer); Assert.NotNull(result.Sample);
        var paths = E(result.Sample.Program.Paths).Cast<Relation>().ToArray();
        Assert.Same(E((Relation)paths[0].Right)[0], E((Relation)paths[1].Right)[0]);
        var staticStart = Assert.IsType<Relation>(Assert.Single(E((Relation)paths[1].Left)));
        Assert.Same(unit, staticStart.Right);
        Assert.Single(E(result.RoleValues)); Assert.Equal(3, E((Relation)((Relation)E(result.RoleValues)[0]).Right).Length);
        Assert.Equal(2, E(result.RoleEvidence).Length); // both actual occurrences were independently read
    }

    [Fact]
    public void Crossing_selectors_retarget_actual_expanded_chords_and_survive_raw_template_transport()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder(); var unit = new Placeholder(); var p = new Placeholder();
        var xChord = S(QualitativeMotionFrame.Move(frame.Right, h)); var yChord = S(QualitativeMotionFrame.Move(frame.Up, v));
        var xPath = new Relation(S(QualitativeMotionFrame.Move(frame.Left, unit), QualitativeMotionFrame.Move(frame.Left, unit)), S(xChord));
        var yPath = new Relation(S(QualitativeMotionFrame.Move(frame.Down, unit), QualitativeMotionFrame.Move(frame.Down, unit)), S(yChord));
        var template = new T(new(frame, "explicit crossing participants", S(xPath, yPath), LatticeLadderAuthor.Ports(xPath, yPath)), S(h, v),
            crossings: S(new Relation(new Relation(xPath, xChord), new Relation(yPath, yChord))));
        var hypothesis = template.Hypothesis(S(new Relation(h, p), new Relation(v, p)), S(new Relation(p, S(unit, unit, unit, unit))));
        var world = Restore(new Relation(template.Surface, hypothesis.Surface));
        Assert.True(T.TryRead((Relation)world.Left, out var imported, out _));
        var output = imported.Realize(ReadHypothesis((Relation)world.Right));
        Assert.Equal("Yes", output.Answer); Assert.NotNull(output.Sample);
        var crossing = Assert.IsType<Relation>(Assert.Single(E(output.Sample.Observation.Crossings)));
        Assert.Contains(crossing.Left, E(output.Sample.Observation.Segments));
        Assert.Contains(crossing.Right, E(output.Sample.Observation.Segments));
        Assert.Equal(2, E(output.RoleValues).Length);
        Assert.Single(E(output.CrossingCorrespondence));
    }

    [Fact]
    public void Foreign_hypotheses_bad_assumption_edges_and_open_template_domains_cannot_certify_generation()
    {
        var (template, roles) = Contour(); var p = new Placeholder(); var q = new Placeholder(); var unit = new Placeholder();
        var bindings = S(roles.Select((role, i) => (IElement)new Relation(role, i == 0 ? p : q)).ToArray());
        var values = S(new Relation(p, S(unit)), new Relation(q, S(unit)));
        var incorrect = new H(template.Parameters, template.Surface, bindings, values, S(new Relation(roles[0], roles[1])));
        Assert.Equal("Blocked", template.Realize(incorrect).Answer);
        var foreign = new H(template.Parameters, S(template.Surface), bindings, values, S());
        Assert.Equal("Blocked", template.Realize(foreign).Answer);
        var incomplete = new Relation(new Relation(template.Program.Surface, StructuralConstraint.Open(roles)), new Relation(template.Connections, template.Crossings));
        Assert.False(T.TryRead(incomplete, out _, out _));
    }

    private static (T Template, IElement[] Roles) Contour()
    {
        var frame = new QualitativeMotionFrame(); var roles = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var words = new[] { frame.Right, frame.Up, frame.Left, frame.Down }.Select((direction, i) => S(QualitativeMotionFrame.Move(direction, roles[i]))).ToArray();
        var path = new Relation(S(), S(words.Cast<IElement>().ToArray()));
        return (new(new(frame, "symbolic closed course", S(path), LatticeLadderAuthor.Ports(path), S(path)), S(roles)), roles);
    }
    private static (T Template, IElement[] Roles) Sides()
    {
        var frame = new QualitativeMotionFrame(); var roles = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        Relation Move(IElement direction, IElement role) => QualitativeMotionFrame.Move(direction, role);
        var paths = new[] {
            new Relation(S(), S(S(Move(frame.Right, roles[0])))),
            new Relation(S(Move(frame.Right, roles[0])), S(S(Move(frame.Up, roles[1])))),
            new Relation(S(Move(frame.Right, roles[0]), Move(frame.Up, roles[1])), S(S(Move(frame.Left, roles[2])))),
            new Relation(S(Move(frame.Up, roles[1])), S(S(Move(frame.Down, roles[3])))) };
        var ports = LatticeLadderAuthor.Ports(paths);
        IElement Role(Relation path, bool end) => E(ports).Cast<Relation>().Single(port => port.Right is Relation pick && ReferenceEquals(pick.Left, path) &&
            ReferenceEquals(pick.Right, end ? path : E((Relation)path.Right)[0])).Left;
        var connections = S(paths.Select((path, i) => (IElement)new Relation(Role(path, true), Role(paths[(i + 1) % paths.Length], false))).ToArray());
        return (new(new(frame, "separate public-side ports", S(paths), ports), S(roles), connections), roles);
    }
    private static H ReadHypothesis(Relation surface)
    {
        Assert.True(surface is { Left: Relation, Right: Relation { Left: Relation, Right: Relation { Left: Relation, Right: Relation { Left: Relation, Right: Relation } } } });
        var body = (Relation)surface.Right; var b = (Relation)body.Right; var v = (Relation)b.Right; var e = (Relation)v.Right;
        return new((Relation)surface.Left, (Relation)body.Left, (Relation)b.Left, (Relation)v.Left, (Relation)e.Left, e.Right);
    }
    private static Relation Restore(Relation source)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("native symbolic generation", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var imported));
        return Assert.IsType<Relation>(imported.Root);
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
