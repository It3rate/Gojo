using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;
using A = Gojo.Experiments.QualitativeFrieze.OrnamentArrangementRead;
using C = Gojo.Experiments.QualitativeFrieze.OrnamentParticipationCount;

namespace QualitativeExecution.Tests;

public sealed class StructuralRulePackTests
{
    [Fact]
    public void A_pack_connects_all_four_native_services_to_one_explicit_target_role()
    {
        var source = Square(); var pack = AllServices(source); var input = Input(source);
        Assert.True(pack.IsValid); var application = new StructuralRulePack.Application(pack, input, G.Publication.FromSample(source));
        Assert.Same(pack.TargetRole, application.Connection.Left); Assert.Same(input.Surface, application.Connection.Right);
        Assert.Equal("Yes", application.Receipt.Answer); Assert.Equal((0, 0), application.Receipt.StrongRange);
        Assert.Equal(4, E(application.ProducerReads).Length);
        Assert.All(application.Observation.Facts, fact => Assert.Equal("Yes", fact.Answer));
        Assert.All(pack.Policy.Rules, rule => Assert.Single(application.Observation.Facts,
            fact => ReferenceEquals(fact.Predicate, rule.Predicate)));
        Assert.DoesNotContain(input.Surface, R.Participants(pack.Surface));
        Assert.DoesNotContain(source.MechanicalGroups, R.Participants(pack.Surface));
        Assert.Contains(input.Surface, R.Participants(application.Surface));
    }

    [Fact]
    public void Standalone_transport_reconstructs_the_pack_and_applies_it_to_an_independent_execution()
    {
        var source = Square(); var original = AllServices(source);
        Assert.True(StructuralRulePack.TryRead(Restore(original.Surface), out var imported, out var readWitness));
        Assert.NotSame(original.Surface, imported.Surface); Assert.NotSame(original.TargetRole, imported.TargetRole);
        Assert.NotEmpty(R.Participants(readWitness));
        var fresh = source.Replay(); var application = new StructuralRulePack.Application(imported, Input(fresh), G.Publication.FromSample(fresh));
        Assert.Equal("Yes", application.Receipt.Answer); Assert.All(application.Observation.Facts, fact => Assert.Equal("Yes", fact.Answer));
        Assert.All(imported.Policy.Rules, rule => Assert.Single(application.Observation.Facts,
            fact => ReferenceEquals(rule.Predicate, fact.Predicate)));
        Assert.DoesNotContain(source.Observation.Surface, R.Participants(imported.Surface));
        Assert.DoesNotContain(source.MechanicalGroups, R.Participants(imported.Surface));
    }

    [Fact]
    public void A_transported_policy_world_preserves_exact_exception_aliases()
    {
        var source = Square(); var pack = AllServices(source);
        var directive = new D.Directive(pack.Policy, pack.Policy.StrongDefaults);
        var world = Restore(new Relation(pack.Surface, directive.Surface));
        Assert.True(StructuralRulePack.TryRead((Relation)world.Left, out var imported, out _));
        var exception = new D.Directive((Relation)world.Right);
        Assert.Same(imported.Policy.Surface, exception.Policy.Surface);
        Assert.Same(imported.Policy.StrongDefaults[0].Surface, Assert.Single(exception.Waived));
        var thin = OrnamentDefaultCases.Create("C10");
        var ordinary = new StructuralRulePack.Application(imported, Input(thin), G.Publication.FromSample(thin));
        var waived = new StructuralRulePack.Application(imported, Input(thin), G.Publication.FromSample(thin), exception);
        Assert.Equal("Yes", ordinary.Receipt.Answer); Assert.Equal((1, 1), ordinary.Receipt.StrongRange);
        Assert.Equal("Yes", waived.Receipt.Answer); Assert.Equal((0, 0), waived.Receipt.StrongRange);
        Assert.Equal("No", waived.Observation.Facts.Single(fact => ReferenceEquals(fact.Predicate,
            imported.GeometryQuestionSurfaces[0])).Answer);
        Assert.Equal("Blocked", new StructuralRulePack.Application(imported, Input(thin),
            G.Publication.FromSample(thin), directive).Receipt.Answer);
    }

    [Fact]
    public void Missing_finer_publication_remains_open_without_consulting_hidden_source_words()
    {
        var sample = Square(); var standard = new G.Standard();
        var rule = new D.Rule("ordinary reach", standard.ModerateAspect.Surface);
        var required = new StructuralRulePack("requires an explicit supplier", new(required: [rule]), geometryQuestions: [standard.ModerateAspect]);
        var application = new StructuralRulePack.Application(required, Input(sample));
        Assert.Equal("Open", application.Receipt.Answer); Assert.True(application.Receipt.Eligible);
        Assert.Equal("Open", Assert.Single(application.Observation.Facts).Answer);
        Assert.Empty(E(application.GeometryAccepted)); Assert.NotEmpty(E(application.GeometryOpen));
        var optional = new StructuralRulePack("same supplier as a default", new(strongDefaults: [rule]), geometryQuestions: [standard.ModerateAspect]);
        var possible = new StructuralRulePack.Application(optional, Input(sample));
        Assert.Equal("Yes", possible.Receipt.Answer); Assert.Equal((0, 1), possible.Receipt.StrongRange);
        Assert.Equal("Open", Assert.Single(possible.Observation.Facts).Answer);
    }

    [Fact]
    public void A_complete_public_world_transports_policy_exceptions_and_explicit_publication_rebinding()
    {
        var sample = Square(); var input = Input(sample); var publication = G.Publication.FromSample(sample);
        var pack = AllServices(sample); var directive = new D.Directive(pack.Policy, pack.Policy.StrongDefaults);
        var world = Restore(new Relation(pack.Surface, new Relation(input.Surface, new Relation(publication.Port, directive.Surface))));
        Assert.True(StructuralRulePack.TryRead((Relation)world.Left, out var imported, out _));
        var body = (Relation)world.Right; var service = (Relation)body.Right;
        Assert.True(G.Publication.TryRead((Relation)service.Left, out var supplier, out var rebind));
        var exception = new D.Directive((Relation)service.Right);
        Assert.Same(imported.Policy.Surface, exception.Policy.Surface);
        // The supplier's importer changes frame identities explicitly. Bind
        // the target to that same public port, rather than connecting a second
        // independent import by coordinate or graph equality.
        var reboundInput = new R.Input("independently imported public target", supplier.Observation, supplier.MechanicalGroups);
        var application = new StructuralRulePack.Application(imported, reboundInput, supplier, exception);
        Assert.Equal("Yes", application.Receipt.Answer); Assert.NotEmpty(E(application.GeometryAccepted));
        Assert.Contains(rebind, R.Participants(application.Surface));
        Assert.Same(imported.Policy.StrongDefaults[0].Surface, Assert.Single(exception.Waived));
        Assert.DoesNotContain(input.Surface, R.Participants(application.Surface));
    }

    [Fact]
    public void A_geometrically_identical_but_independent_occurrence_cannot_supply_another_inputs_receipt()
    {
        var source = Square(); var independent = source.Replay(); var standard = new G.Standard();
        var rule = new D.Rule("explicit shape supplier", standard.ModerateAspect.Surface);
        var pack = new StructuralRulePack("same actual event", new(required: [rule]), geometryQuestions: [standard.ModerateAspect]);
        var input = Input(source);
        var wrong = new StructuralRulePack.Application(pack, input, G.Publication.FromSample(independent));
        Assert.Equal("Open", wrong.Receipt.Answer); Assert.NotEmpty(E(wrong.GeometryBlocked)); Assert.Empty(E(wrong.GeometryAccepted));
        Assert.Equal("Open", Assert.Single(wrong.Observation.Facts).Answer);
        var correct = new StructuralRulePack.Application(pack, input, G.Publication.FromSample(source));
        Assert.Equal("Yes", correct.Receipt.Answer); Assert.Empty(E(correct.GeometryBlocked)); Assert.NotEmpty(E(correct.GeometryAccepted));
    }

    [Fact]
    public void Geometry_binding_preserves_actual_public_inventories_and_cannot_replace_mechanical_groups()
    {
        var source = Square(); var standard = new G.Standard(); var input = Input(source);
        var rule = new D.Rule("closed contours have breadth", standard.LegibleClosedGroups.Surface);
        var pack = new StructuralRulePack("published group ownership", new(required: [rule]), geometryQuestions: [standard.LegibleClosedGroups]);
        var publication = G.Publication.FromSample(source);
        var missingGroups = new G.Publication(publication.Frame, publication.Observation, publication.Bindings, S());
        Assert.Equal("Open", new StructuralRulePack.Application(pack, input, missingGroups).Receipt.Answer);
        // A new wrapper over the same actual scopes does not manufacture a
        // different event; copied native identities do.
        var observation = new QualitativeRecognitionObservation(input.Observation.Frame,
            S(E(input.Observation.Sites)), S(E(input.Observation.Segments)),
            S(E(input.Observation.Continuations)), S(E(input.Observation.Crossings)));
        var wrapper = new G.Publication(publication.Frame, observation, publication.Bindings, S(E(source.MechanicalGroups)));
        var accepted = new StructuralRulePack.Application(pack, input, wrapper);
        Assert.Equal("Yes", accepted.Receipt.Answer); Assert.NotEmpty(E(accepted.GeometryAccepted));
    }

    [Fact]
    public void Every_policy_predicate_needs_one_actual_unambiguous_exposed_service()
    {
        var source = Square(); var input = Input(source);
        Assert.True(R.TryDescribeGroup(input, (Relation)E(input.MechanicalGroups)[0], out var pattern, out _));
        var query = R.Query.WholePath("one whole course", pattern); var rule = new D.Rule("course", query.Surface);
        Assert.False(new StructuralRulePack("unexposed", new(required: [rule])).IsValid);
        Assert.False(new StructuralRulePack("duplicate service", new(required: [rule]), [query, query]).IsValid);
        var lookalike = R.Query.WholePath("same shape, separate service identity", pattern);
        Assert.False(new StructuralRulePack("wrong actual alias", new(required: [rule]), [lookalike]).IsValid);
        Assert.True(new StructuralRulePack("actual service", new(required: [rule]), [query]).IsValid);
    }

    [Fact]
    public void Exact_pack_read_preserves_open_and_invalid_contracts_in_its_witness()
    {
        var pack = AllServices(Square());
        var body = (Relation)pack.Surface.Right; var scopes = (Relation)body.Right;
        var open = new Relation(pack.TargetRole, new Relation(body.Left,
            new Relation(StructuralConstraint.Open(E((Relation)scopes.Left)), scopes.Right)));
        Assert.False(StructuralRulePack.TryRead(open, out _, out var pending));
        Assert.NotEmpty(E((Relation)((Relation)((Relation)pending.Right).Right).Right));
        Assert.False(StructuralRulePack.TryRead(S(), out _, out var malformed));
        Assert.NotEmpty(R.Participants(malformed));
        var alias = R.Participants(pack.Policy.Surface).OfType<Placeholder>().First();
        Assert.False(StructuralRulePack.TryRead(new Relation(alias.Dual().Dual(), pack.Surface.Right), out _, out _));
    }

    [Fact]
    public void Arrangement_normalization_preserves_the_original_advertised_predicate_and_explicit_correspondence()
    {
        var sample = Square(); var question = A.Crystallize("a whole cycle", sample.Observation, out _);
        var pack = new StructuralRulePack("arrangement service", new(required: [new D.Rule("cycle", question.Surface)]), arrangementQuestions: [question]);
        Assert.Same(question.Surface, Assert.Single(pack.ArrangementQuestionSurfaces));
        Assert.NotSame(question.Surface, Assert.Single(pack.ArrangementQuestions).Surface);
        var application = new StructuralRulePack.Application(pack, Input(sample.Replay()));
        var fact = Assert.Single(application.Observation.Facts);
        Assert.Same(question.Surface, fact.Predicate); Assert.Equal("Yes", application.Receipt.Answer);
        Assert.Contains(pack.ArrangementQuestions[0].Surface, R.Participants(application.ProducerReads));
    }

    [Fact]
    public void One_imported_cycle_pack_applies_to_distinct_ornament_glyph_and_network_constructions()
    {
        var ornament = Cycle("ornamental compartment", 4); var glyph = Cycle("octagonal glyph outline", 8);
        var network = Cycle("three-node feedback network", 3);
        var arrangement = A.Crystallize("one closed route", ornament.Observation, out _);
        var count = C.ClosedGroups(ornament.MechanicalGroups, out _);
        var pack = new StructuralRulePack("closed participation", new(required: [
            new D.Rule("whole closed route", arrangement.Surface), new D.Rule("one closed mechanical unit", count.Surface)]),
            arrangementQuestions: [arrangement], countQuestions: [count]);
        Assert.True(StructuralRulePack.TryRead(Restore(pack.Surface), out var imported, out _));
        foreach (var input in new[] { ornament, glyph, network })
        {
            var application = new StructuralRulePack.Application(imported, input);
            Assert.Equal("Yes", application.Receipt.Answer); Assert.Equal(2, E(application.ProducerReads).Length);
            Assert.All(application.Observation.Facts, fact => Assert.Equal("Yes", fact.Answer));
        }
        Assert.NotEqual(E(ornament.Observation.Segments).Length, E(glyph.Observation.Segments).Length);
        Assert.NotEqual(E(glyph.Observation.Segments).Length, E(network.Observation.Segments).Length);
    }

    private static StructuralRulePack AllServices(OrnamentReconstructionCases.Sample sample)
    {
        var input = Input(sample);
        Assert.True(R.TryDescribeGroup(input, (Relation)E(input.MechanicalGroups)[0], out var pattern, out _));
        var visible = R.Query.WholePath("one advertised whole path", pattern); var standard = new G.Standard();
        var arrangement = A.Crystallize("complete branch arrangement", sample.Observation, out _);
        var count = C.ClosedGroups(sample.MechanicalGroups, out _);
        return new("portable cycle assessment", new(required: [new D.Rule("whole path", visible.Surface),
            new D.Rule("branch arrangement", arrangement.Surface), new D.Rule("closed groups", count.Surface)],
            strongDefaults: [new D.Rule("ordinary proportion", standard.ModerateAspect.Surface)]),
            [visible], [standard.ModerateAspect], [arrangement], [count]);
    }
    private static OrnamentReconstructionCases.Sample Square() => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == "SQ-PATH" && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static R.Input Input(OrnamentReconstructionCases.Sample sample) => new("uninterpreted publication", sample.Observation, sample.MechanicalGroups);
    private static R.Input Cycle(string name, int size)
    {
        var frame = new QualitativeMotionFrame(); var sites = Enumerable.Range(0, size).Select(_ => (IElement)new Placeholder()).ToArray();
        var directions = size == 4 ? new[] { S(frame.Right), S(frame.Up), S(frame.Left), S(frame.Down) } : size == 3 ?
            new[] { S(frame.Right), S(frame.Left, frame.Up), S(frame.Left, frame.Down) } :
            new[] { S(frame.Right), S(frame.Right, frame.Up), S(frame.Up), S(frame.Left, frame.Up),
                S(frame.Left), S(frame.Left, frame.Down), S(frame.Down), S(frame.Right, frame.Down) };
        var edges = sites.Select((site, i) => (IElement)new Relation(new Placeholder(),
            new Relation(new Relation(site, sites[(i + 1) % sites.Length]), directions[i]))).ToArray();
        var observation = new QualitativeRecognitionObservation(frame, S(sites), S(edges));
        var groups = S(new Relation(new Placeholder(), new Relation(S(edges), S(new Relation(sites[0], sites[0])))));
        return new(name, observation, groups);
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation Restore(Relation surface)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("portable-native-pack", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
}
