using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace QualitativeExecution.Tests;

/// <summary>Adversarial identity checks for retaining an actual occurrence,
/// including aliases that have the same footprint but different site bindings.</summary>
public sealed class RetainedMatchAuditTests
{
    [Fact]
    public void Two_existential_successes_cannot_silently_substitute_different_paths()
    {
        var f = new Fixture();
        var a = f.Site(); var b = f.Site(); var c = f.Site();
        var d = f.Site(); var e = f.Site(); var g = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        f.Edge(d, e, 1); f.Edge(e, g, 1); f.Triangle(g);
        var output = f.Read(); var (path, loop) = Roles();
        var (start, _, end) = PathSites(path);
        var first = new C.Assessment(output, Contact(path, start, loop));
        Assert.Equal("Yes", first.Answer);
        var memory = RetainedMatch.Capture(first, Assert.Single(first.Witnesses), S(path.Slot));
        Assert.True(memory.IsValid);

        var next = Contact(path, end, loop);
        Assert.Equal("Yes", new C.Assessment(output, next).Answer);
        var retained = new C.Assessment(output, next, retained: memory);
        Assert.Equal("No", retained.Answer);
        Assert.Empty(retained.Witnesses);
        Assert.NotEmpty(E(retained.Refuted));
        Assert.Empty(E(retained.Open));
    }

    [Fact]
    public void Retaining_only_the_path_leaves_the_other_motif_role_free()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a); f.Triangle(c);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, end) = PathSites(path);
        var first = new C.Assessment(output, Contact(path, start, loop));
        var witness = Assert.Single(first.Witnesses);
        var pathOnly = RetainedMatch.Capture(first, witness, S(path.Slot));
        var both = RetainedMatch.Capture(first, witness, S(path.Slot, loop.Slot));
        var question = Contact(path, end, loop);

        Assert.Equal("Yes", new C.Assessment(output, question, retained: pathOnly).Answer);
        Assert.Equal("No", new C.Assessment(output, question, retained: both).Answer);
        Assert.Single(E(pathOnly.Pins));
        Assert.Equal(2, E(both.Pins).Length);
    }

    [Fact]
    public void Symmetric_leaf_aliases_cannot_swap_the_retained_pattern_site_mapping()
    {
        var source = new Fixture(); var origin = source.Site();
        source.Edge(origin, source.Site(), 1); source.Edge(origin, source.Site(), 1);
        var family = source.Read().Families.Single(item => item.Kind == "junction" && item.SegmentCount == 2);
        var pattern = new CommunicationPattern(source.Frame, family.Definition);
        var f = new Fixture(); var centre = f.Site(); var firstLeaf = f.Site(); var secondLeaf = f.Site();
        f.Edge(centre, firstLeaf, 1); f.Edge(centre, secondLeaf, 1); f.Triangle(firstLeaf);
        var output = f.Read(); var star = new C.Role(pattern); var loop = new C.Role(TrianglePattern());
        var shape = Shape(pattern.Definition);
        var leaves = shape.Sites.Where(site => !shape.Centres.Any(c => ReferenceEquals(c, site))).ToArray();
        Assert.Equal(2, leaves.Length);
        var first = new C.Assessment(output, Contact(star, leaves[0], loop));
        Assert.Equal("Yes", first.Answer);
        var memory = RetainedMatch.Capture(first, Assert.Single(first.Witnesses), S(star.Slot));
        var next = Contact(star, leaves[1], loop);

        Assert.Equal("Yes", new C.Assessment(output, next).Answer);
        var retained = new C.Assessment(output, next, retained: memory);
        Assert.Equal("No", retained.Answer);
        Assert.Empty(retained.Witnesses);
        Assert.NotEmpty(E(retained.Refuted));
    }

    [Fact]
    public void New_induction_wrappers_over_the_same_observation_preserve_actual_identity()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var question = Contact(path, start, loop);
        var initial = new C.Assessment(output, question);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(path.Slot));
        var rebuilt = Read(output.Observation);

        Assert.NotSame(output, rebuilt);
        Assert.Same(output.Observation, rebuilt.Observation);
        Assert.Equal("Yes", new C.Assessment(rebuilt, question, retained: memory).Answer);
    }

    [Fact]
    public void A_fresh_lookalike_event_cannot_inherit_a_previous_occurrence_by_shape()
    {
        var first = new Fixture(); var a = first.Site(); var b = first.Site(); var c = first.Site();
        first.Edge(a, b, 1); first.Edge(b, c, 1); first.Triangle(a);
        var output = first.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var question = Contact(path, start, loop); var initial = new C.Assessment(output, question);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(path.Slot));
        var second = new Fixture(); var d = second.Site(); var e = second.Site(); var g = second.Site();
        second.Edge(d, e, 1); second.Edge(e, g, 1); second.Triangle(d);
        var fresh = second.Read();

        Assert.Equal("Yes", new C.Assessment(fresh, question).Answer);
        var retained = new C.Assessment(fresh, question, retained: memory);
        Assert.Equal("Open", retained.Answer);
        Assert.Empty(E(retained.Refuted));
        Assert.NotEmpty(E(retained.Stale));
    }

    [Fact]
    public void A_new_event_with_reused_sites_and_edges_still_needs_an_event_correspondence()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var question = Contact(path, start, loop); var initial = new C.Assessment(output, question);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(path.Slot));
        var otherEvent = Read(new(output.Observation.Frame, output.Observation.Sites, output.Observation.Segments));

        Assert.NotSame(output.Observation.Surface, otherEvent.Observation.Surface);
        var assessment = new C.Assessment(otherEvent, question, retained: memory);
        Assert.Equal("Open", assessment.Answer);
        Assert.Empty(assessment.Witnesses);
        Assert.NotEmpty(E(assessment.Stale));
    }

    [Fact]
    public void A_role_from_another_question_is_not_a_retention_selection()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var initial = new C.Assessment(output, Contact(path, start, loop));
        var foreign = new C.Role(WalkPattern());
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(foreign.Slot));

        Assert.False(memory.IsValid);
        Assert.NotEmpty(E(memory.Blocked));
        Assert.Empty(E(memory.Pins));
    }

    [Fact]
    public void An_unfinished_retention_selection_does_not_produce_negative_followup_evidence()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, end) = PathSites(path);
        var initial = new C.Assessment(output, Contact(path, start, loop));
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), StructuralConstraint.Open(path.Slot));
        var read = new C.Assessment(output, Contact(path, end, loop), retained: memory);

        Assert.False(memory.IsValid);
        Assert.NotEmpty(E(memory.Open));
        Assert.Equal("Open", read.Answer);
        Assert.Empty(E(read.Refuted));
    }

    [Fact]
    public void A_valid_witness_from_another_assessment_is_not_the_selected_supported_witness()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var question = Contact(path, start, loop);
        var initial = new C.Assessment(output, question);
        var otherAssessment = new C.Assessment(output, question);
        var actual = Assert.Single(initial.Witnesses); var other = Assert.Single(otherAssessment.Witnesses);
        Assert.NotSame(actual.Surface, other.Surface);

        var memory = RetainedMatch.Capture(initial, other, S(path.Slot));
        Assert.False(memory.IsValid);
        Assert.NotEmpty(E(memory.Blocked));
        Assert.Empty(E(memory.Pins));
    }

    [Fact]
    public void A_native_pin_cannot_substitute_another_valid_mapping_of_the_same_symmetric_footprint()
    {
        var source = new Fixture(); var origin = source.Site();
        source.Edge(origin, source.Site(), 1); source.Edge(origin, source.Site(), 1);
        var family = source.Read().Families.Single(item => item.Kind == "junction" && item.SegmentCount == 2);
        var f = new Fixture(); var centre = f.Site(); var firstLeaf = f.Site(); var secondLeaf = f.Site();
        f.Edge(centre, firstLeaf, 1); f.Edge(centre, secondLeaf, 1); f.Triangle(firstLeaf);
        var star = new C.Role(new CommunicationPattern(source.Frame, family.Definition));
        var loop = new C.Role(TrianglePattern()); var shape = Shape(family.Definition);
        var leaves = shape.Sites.Where(site => !shape.Centres.Any(c => ReferenceEquals(c, site))).ToArray();
        var first = new C.Assessment(f.Read(), Contact(star, leaves[0], loop));
        var memory = RetainedMatch.Capture(first, Assert.Single(first.Witnesses), S(star.Slot));
        Assert.True(memory.IsValid);
        var pin = Assert.IsType<Relation>(Assert.Single(E(memory.Pins)));
        var body = Assert.IsType<Relation>(pin.Right); var physicalAndMap = Assert.IsType<Relation>(body.Right);
        var map = E(Assert.IsType<Relation>(physicalAndMap.Right)).Cast<Relation>().ToArray();
        var left = map.Single(row => ReferenceEquals(row.Left, leaves[0]));
        var right = map.Single(row => ReferenceEquals(row.Left, leaves[1]));
        var swapped = S(map.Select(row => (IElement)new Relation(row.Left,
            ReferenceEquals(row, left) ? right.Right : ReferenceEquals(row, right) ? left.Right : row.Right)).ToArray());
        var changedPin = new Relation(pin.Left, new Relation(body.Left, new Relation(physicalAndMap.Left, swapped)));
        var forged = new RetainedMatch(new(memory.Surface.Left,
            new Relation(memory.SelectedRoles, new Relation(memory.Observation, S(changedPin)))));

        Assert.False(forged.IsValid);
        Assert.NotEmpty(E(forged.Blocked));
        Assert.Empty(E(forged.Open));
    }

    [Fact]
    public void Exhausting_followup_assignment_capacity_does_not_refute_a_retained_match()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var question = Contact(path, start, loop); var initial = new C.Assessment(output, question);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(path.Slot));
        var cut = new C.Assessment(output, question, assignmentCapacity: 0, retained: memory);

        Assert.Equal("Open", cut.Answer);
        Assert.Empty(E(cut.Refuted));
        Assert.Empty(cut.Witnesses);
    }

    [Fact]
    public void A_successful_followup_can_become_the_basis_of_another_retained_read()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a); f.Triangle(c);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, end) = PathSites(path);
        var first = new C.Assessment(output, Contact(path, start, loop));
        var firstMemory = RetainedMatch.Capture(first, Assert.Single(first.Witnesses), S(path.Slot));
        var nextQuestion = Contact(path, end, loop);
        var followup = new C.Assessment(output, nextQuestion, retained: firstMemory);
        Assert.Equal("Yes", followup.Answer);
        var nextMemory = RetainedMatch.Capture(followup, Assert.Single(followup.Witnesses), S(path.Slot, loop.Slot));

        Assert.True(nextMemory.IsValid);
        Assert.Same(followup.Surface, nextMemory.SourceAssessment);
        Assert.Equal(2, E(nextMemory.Pins).Length);
        Assert.Equal("Yes", new C.Assessment(output, nextQuestion, retained: nextMemory).Answer);
        Assert.Equal("No", new C.Assessment(output, Contact(path, start, loop), retained: nextMemory).Answer);
    }

    [Fact]
    public void Native_supported_rows_cannot_certify_valid_roles_that_violate_the_source_joint_question()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, end) = PathSites(path);
        var actualQuestion = Contact(path, start, loop);
        var initial = new C.Assessment(output, actualQuestion);
        var witness = Assert.Single(initial.Witnesses);
        var memory = RetainedMatch.Capture(initial, witness, S(path.Slot));
        var falseQuestion = Contact(path, end, loop);
        Assert.Equal("No", new C.Assessment(output, falseQuestion).Answer);
        var fabricatedAssessment = Assert.IsType<Relation>(Replace(initial.Surface,
            actualQuestion.Surface, falseQuestion.Surface));
        // The role footprints, incidence and full site maps remain valid. Only
        // the claimed joint condition and its supported-row reference change.
        var forged = new RetainedMatch(new(new Relation(fabricatedAssessment, witness.Surface), memory.Surface.Right));

        Assert.False(forged.IsValid);
        Assert.NotEmpty(E(forged.Blocked));
        Assert.Empty(E(forged.Open));
    }

    [Fact]
    public void A_malformed_native_observation_frame_returns_blocked_without_throwing()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var initial = new C.Assessment(output, Contact(path, start, loop));
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(path.Slot));
        var oldFrame = output.Observation.Frame.Surface;
        var missingOpposites = new Relation(oldFrame.Left, new Relation(S(), ((Relation)oldFrame.Right).Right));
        var malformed = Assert.IsType<Relation>(Replace(memory.Surface, oldFrame, missingOpposites));

        var read = new RetainedMatch(malformed);
        Assert.False(read.IsValid);
        Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void A_valid_joint_assignment_cannot_keep_an_inconsistent_ordered_occurrence_route()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var initial = new C.Assessment(output, Contact(path, start, loop));
        var witness = Assert.Single(initial.Witnesses);
        var memory = RetainedMatch.Capture(initial, witness, S(path.Slot));
        var binding = E(witness.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, path.Slot));
        var occurrence = Assert.IsType<Relation>(binding.Right);
        var route = Assert.IsType<Relation>(((Relation)((Relation)occurrence.Right).Right).Right);
        Assert.Equal(2, E(route).Length);
        var changedRoute = S(E(route).Reverse().ToArray());
        var inconsistent = Assert.IsType<Relation>(Replace(memory.Surface, route, changedRoute));

        var read = new RetainedMatch(inconsistent);
        Assert.False(read.IsValid);
        Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void A_native_continuation_envelope_cannot_hide_switching_the_previous_pinned_path()
    {
        var f = new Fixture();
        var a = f.Site(); var b = f.Site(); var c = f.Site();
        var d = f.Site(); var e = f.Site(); var g = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        f.Edge(d, e, 1); f.Edge(e, g, 1); f.Triangle(g);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, end) = PathSites(path);
        var initial = new C.Assessment(output, Contact(path, start, loop));
        var previous = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(path.Slot));
        var laterQuestion = Contact(path, end, loop);
        Assert.Equal("No", new C.Assessment(output, laterQuestion, retained: previous).Answer);
        var independent = new C.Assessment(output, laterQuestion);
        Assert.Equal("Yes", independent.Answer);
        var independentWitness = Assert.Single(independent.Witnesses);
        var independentMemory = RetainedMatch.Capture(independent, independentWitness, S(path.Slot));
        var falseContinuation = new Relation(independent.Surface, new Relation(previous.Surface, S()));
        var forged = new RetainedMatch(new(new Relation(falseContinuation, independentWitness.Surface),
            independentMemory.Surface.Right));

        Assert.False(forged.IsValid);
        Assert.NotEmpty(E(forged.Blocked));
    }

    [Fact]
    public void Transport_preserves_native_pin_aliases_without_rebinding_them_to_the_original_world()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(a);
        var output = f.Read(); var (path, loop) = Roles(); var (start, _, _) = PathSites(path);
        var question = Contact(path, start, loop); var initial = new C.Assessment(output, question);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), S(path.Slot));
        var root = new Relation(memory.Surface, new Relation(output.Observation.Surface, question.Surface));
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("retained context", root));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var restoredRoot = Assert.IsType<Relation>(restored.Root);
        var restoredMemory = new RetainedMatch(Assert.IsType<Relation>(restoredRoot.Left));
        var context = Assert.IsType<Relation>(restoredRoot.Right);

        Assert.True(restoredMemory.IsValid);
        Assert.Same(context.Left, restoredMemory.Observation);
        Assert.NotSame(memory.Observation, restoredMemory.Observation);
        Assert.DoesNotContain(Reachable(restoredMemory.Surface), element => element is Carrier or CarrierLocation);
        Assert.DoesNotContain(Reachable(restoredMemory.Surface), element => ReferenceEquals(element, output.Observation.Surface));
        var restoredQuestion = new C.Question("restored follow-up", Assert.IsType<Relation>(context.Right));
        Assert.True(restoredQuestion.IsValid);
        var wrongWorld = new C.Assessment(output, restoredQuestion, retained: restoredMemory);
        Assert.Equal("Open", wrongWorld.Answer);
        Assert.Empty(E(wrongWorld.Refuted));
    }

    private static C.Question Contact(C.Role path, IElement selected, C.Role loop) => new("selected site meets loop",
        [path, loop], disjointEdges: S(C.Pair(path, loop)),
        sharedSelectedSites: S(C.Pair(C.Select(path, selected), C.Select(loop, Shape((Relation)loop.PatternSurface.Right).Sites))));

    private static (C.Role Path, C.Role Loop) Roles() => (new(WalkPattern()), new(TrianglePattern()));
    private static CommunicationPattern WalkPattern()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1);
        var family = f.Read().Families.First(item => item.Kind == "walk" && item.SegmentCount == 2);
        return new(f.Frame, family.Definition);
    }
    private static CommunicationPattern TrianglePattern()
    {
        var f = new Fixture(); f.Triangle(f.Site());
        var family = f.Read().Families.First(item => item.Kind == "cycle" && item.SegmentCount == 3);
        return new(f.Frame, family.Definition);
    }
    private static (IElement Start, IElement Between, IElement End) PathSites(C.Role role)
    {
        var shape = Shape((Relation)role.PatternSurface.Right);
        return (shape.Ends[0].Left, shape.Ends[0].Right, shape.Ends[^1].Right);
    }
    private static (IElement[] Sites, IElement[] Centres, Relation[] Ends) Shape(Relation definition)
    {
        var head = (Relation)definition.Left; var graph = (Relation)head.Right; var tail = (Relation)definition.Right;
        return (E((Relation)graph.Left), E((Relation)tail.Left), E((Relation)graph.Right).Cast<Relation>()
            .Select(row => (Relation)((Relation)row.Right).Left).ToArray());
    }
    private static QualitativeMotifInduction Read(QualitativeRecognitionObservation observation) => new(observation,
        maxCycleEdges: 3, maxWalkEdges: 3, proposalCapacity: 8192, retainedCapacity: 2048);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IElement Replace(IElement root, IElement before, IElement after)
    {
        var read = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(IElement Participant, bool Expanded)>();
        pending.Push((root, false));
        while (pending.TryPop(out var next))
        {
            var participant = next.Participant;
            if (read.ContainsKey(participant)) continue;
            if (ReferenceEquals(participant, before)) { read.Add(participant, after); continue; }
            if (participant is not Relation relation) { read.Add(participant, participant); continue; }
            if (!next.Expanded)
            {
                pending.Push((participant, true));
                pending.Push((relation.Right, false));
                pending.Push((relation.Left, false));
                continue;
            }
            var left = read[relation.Left]; var right = read[relation.Right];
            var result = ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right)
                ? participant : new Relation(left, right);
            read.Add(participant, result);
        }
        return read[root];
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private List<IElement> Sites { get; } = [];
        private List<IElement> Edges { get; } = [];
        public IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        public void Edge(IElement from, IElement to, int mask) => Edges.Add(new Relation(new Placeholder(),
            new Relation(new Relation(from, to), S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }
                .Where((_, index) => (mask & 1 << index) != 0).Cast<IElement>().ToArray()))));
        public void Triangle(IElement a)
        {
            var b = Site(); var c = Site(); Edge(a, b, 4); Edge(b, c, 9); Edge(c, a, 8);
        }
        public QualitativeMotifInduction Read() => RetainedMatchAuditTests.Read(new(Frame, S(Sites.ToArray()), S(Edges.ToArray())));
    }
}
