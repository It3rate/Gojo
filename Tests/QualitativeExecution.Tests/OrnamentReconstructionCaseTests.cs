using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionCaseTests
{
    [Fact]
    public void Corpus_varies_basis_reach_recipe_and_occurrence_multiplicity_without_eager_recognition()
    {
        var specs = OrnamentReconstructionCases.Specs;
        Assert.Equal(416, specs.Count); Assert.Equal(26, specs.Select(spec => spec.Family).Distinct().Count());
        Assert.Equal(specs.Count, specs.Select(spec => spec.Id).Distinct().Count());
        Assert.All(specs.GroupBy(spec => spec.Family), group => Assert.Equal(16, group.Count()));
        foreach (var spec in specs.Where(spec => spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1))
        {
            var sample = spec.Create();
            Assert.True(sample.Publisher.IsComplete);
            Assert.True(sample.Join.TryReadPassed(sample.Join.Evaluate()));
            Assert.True(sample.Execution.Current.Execution.IsAtBoundary);
            Assert.InRange(E(sample.Observation.Segments).Length, 2, 32);
            Assert.DoesNotContain(Reachable(sample.Observation.Surface), item => item is Carrier or CarrierLocation);
            Assert.DoesNotContain(Reachable(sample.Observation.Surface), item => ReferenceEquals(item, sample.Program.Surface));
            var groups = E(sample.MechanicalGroups).Cast<Relation>().ToArray();
            var membership = groups.SelectMany(group => E((Relation)((Relation)group.Right).Left)).ToArray();
            Assert.Equal(E(sample.Observation.Segments).Length, membership.Length);
            Assert.All(E(sample.Observation.Segments), edge => Assert.Single(membership, item => ReferenceEquals(item, edge)));
            var privateProgram = Reachable(sample.Program.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
            Assert.DoesNotContain(Reachable(sample.MechanicalGroups), privateProgram.Contains);
            Assert.All(groups, group => Assert.IsType<Placeholder>(group.Left));
            Assert.All(groups, group => Assert.Single(E(sample.MechanicalEvidence).Cast<Relation>(), proof =>
                ReferenceEquals(proof.Left, group) && proof.Right is Relation origin &&
                E(sample.Execution.Current.ExecutedProgram.Paths).Any(path => ReferenceEquals(path, origin.Left))));
        }
    }

    [Fact]
    public void Same_square_outline_keeps_topology_and_mechanical_recipe_distinct()
    {
        var path = Base("SQ-PATH").Create(); var sides = Base("SQ-SIDES").Create(); var unjoined = Base("SQ-OPEN").Create();
        Assert.Equal(4, E(path.Observation.Sites).Length); Assert.Equal(4, E(sides.Observation.Sites).Length);
        Assert.Equal(8, E(unjoined.Observation.Sites).Length);
        Assert.Single(E(path.MechanicalGroups)); Assert.Equal(4, E(sides.MechanicalGroups).Length);
        var single = (Relation)E(path.MechanicalGroups).Single();
        Assert.Single(E((Relation)((Relation)single.Right).Right));
        Assert.All(E(sides.MechanicalGroups).Cast<Relation>(), group => Assert.Empty(E((Relation)((Relation)group.Right).Right)));
        var a = new QualitativeMotifInduction(path.Observation, maxCycleEdges: 4);
        var b = new QualitativeMotifInduction(sides.Observation, maxCycleEdges: 4);
        var c = new QualitativeMotifInduction(unjoined.Observation, maxCycleEdges: 4);
        Assert.True(StructuralEvidenceInquiry.CompareObservations(a, b).IsCompatibleAtVocabulary);
        Assert.False(StructuralEvidenceInquiry.CompareObservations(a, c).IsCompatibleAtVocabulary);
    }

    [Fact]
    public void A_crossing_declaration_does_not_create_a_shared_site_or_a_mechanical_merge()
    {
        var joined = Base("X-JOIN").Create(); var crossing = Base("X-CROSS").Create(); var unrelated = Base("X-UNRELATED").Create();
        Assert.Equal(5, E(joined.Observation.Sites).Length); Assert.Equal(4, E(joined.Observation.Segments).Length);
        Assert.Equal(4, E(crossing.Observation.Sites).Length); Assert.Equal(2, E(crossing.Observation.Segments).Length);
        Assert.Single(E(crossing.Observation.Crossings)); Assert.Empty(E(unrelated.Observation.Crossings));
        Assert.Equal(2, E(crossing.MechanicalGroups).Length); Assert.Equal(2, E(unrelated.MechanicalGroups).Length);
        Assert.All(E(crossing.Observation.Crossings).Cast<Relation>(), row => Assert.True(crossing.Observation.TryReadCrossing(row, out _, out _)));
    }

    [Fact]
    public void Fresh_transformed_and_repeated_samples_preserve_native_aliases_and_bound_panel_size()
    {
        foreach (var family in new[] { "GRID", "HONEYCOMB", "MEANDER", "BASKET", "MEANDER-PAIR", "DIAMOND-NODES", "ALTERNATING-CELLS", "MERGED-CELLS", "LOZENGE-ROSETTE" })
        {
            var baseline = Base(family).Create();
            foreach (var spec in OrnamentReconstructionCases.Specs.Where(spec => spec.Family == family && spec.Transform > 0 && spec.ReachRecipe == 1 && spec.Repeats == 2))
            {
                var sample = spec.Create();
                Assert.Equal(2 * E(baseline.Observation.Segments).Length, E(sample.Observation.Segments).Length);
                Assert.InRange(E(sample.Observation.Segments).Length, 2, 64);
                Assert.Equal(2 * E(baseline.MechanicalGroups).Length, E(sample.MechanicalGroups).Length);
                Assert.True(sample.Publisher.IsComplete);
                Assert.DoesNotContain(E(sample.Observation.Sites), site => E(baseline.Observation.Sites).Any(other => ReferenceEquals(site, other)));
            }
        }
    }

    [Fact]
    public void Replay_executes_the_retained_native_recipe_with_fresh_occurrence_identity()
    {
        var queries = OrnamentReconstructionVocabulary.All.Select(entry => entry.Query).ToArray();
        foreach (var family in new[] { "SQ-PATH", "SQ-SIDES", "SQ-OPEN", "X-JOIN", "X-CROSS", "X-UNRELATED", "GRID", "HONEYCOMB", "DIAMOND-NODES", "MERGED-CELLS" })
        {
            var source = Base(family).Create(); var replay = source.Replay();
            Assert.Same(source.Program, replay.Program); Assert.Same(source.Recipe, replay.Recipe);
            Assert.NotSame(source.Execution, replay.Execution); Assert.NotSame(source.Observation.Surface, replay.Observation.Surface);
            Assert.DoesNotContain(E(source.Observation.Sites), site => E(replay.Observation.Sites).Any(other => ReferenceEquals(site, other)));
            Assert.Equal(E(source.Observation.Segments).Length, E(replay.Observation.Segments).Length);
            Assert.Equal(E(source.Observation.Crossings).Length, E(replay.Observation.Crossings).Length);
            Assert.Equal(E(source.Join.Accepted).Length, E(replay.Join.Accepted).Length);
            Assert.Same(source.Recipe, replay.ReplayEvidence.Left);
            var a = new OrnamentReconstruction.Input("source", source.Observation, source.MechanicalGroups);
            var b = new OrnamentReconstruction.Input("replay", replay.Observation, replay.MechanicalGroups);
            var bank = new OrnamentReconstruction.Bank([a, b], queries);
            Assert.All(queries, question => Assert.Equal(bank.Read(a, question).Answer, bank.Read(b, question).Answer));
        }
    }

    private static OrnamentReconstructionCases.Spec Base(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
