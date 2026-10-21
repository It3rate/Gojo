using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using E = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;

namespace QualitativeExecution.Tests;

public sealed class NativeLineSupplierTests
{
    private static readonly Lazy<IReadOnlyList<NativeLatticeSources.SourceCase>> Sources = new(NativeLatticeSources.Cases);

    [Fact]
    public void Nine_native_support_pair_selections_expose_distinct_square_and_rectangle_extents()
    {
        var supplier = Supplier("N02");
        Assert.Equal(9, supplier.Candidates.Count); Assert.Empty(Items(supplier.Unsearched));
        Assert.All(supplier.Candidates, candidate => Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteBoundary).Answer));
        var squareAnswers = supplier.Candidates.Select(candidate => candidate.Read(candidate.Frame.FiniteSquare).Answer).ToArray();
        Assert.Equal(5, squareAnswers.Count(answer => answer == "Yes"));
        Assert.Equal(4, squareAnswers.Count(answer => answer == "No"));
    }

    [Fact]
    public void Native_crossing_reads_preserve_all_original_disconnected_endpoint_sites()
    {
        var source = Case("N02"); var originalGraph = source.Graph.Surface;
        var sites = Items(source.Graph.Sites); var strokes = Items(source.Graph.Strokes);
        var supplier = new NativeLineSupplier(source);
        Assert.NotNull(supplier.Publication);
        Assert.Same(source, supplier.Publication.Original); Assert.Same(supplier.Source, supplier.Publication.Published);
        Assert.Equal(12, sites.Length); Assert.Equal(6, strokes.Length);
        Assert.Equal(12, Items(supplier.Observation.Sites).Length);
        foreach (var site in sites)
        {
            Assert.True(supplier.Publication.TryMapSite(site, out var exposed));
            Assert.NotSame(site, exposed);
            Assert.Contains(Items(supplier.Observation.Sites), actual => ReferenceEquals(actual, exposed));
        }
        Assert.All(sites, site => Assert.Equal(1, Degree(source.Graph, site)));
        Assert.Equal(6, new QualitativeJunctionEvidence(source.Graph, source.Bindings).Describe().ActualComponents);
        foreach (var candidate in supplier.Candidates)
        {
            Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteBoundary).Answer);
            Assert.All(candidate.Supports, support => Assert.Contains(Items(supplier.Support.Graph.Strokes), actual => ReferenceEquals(actual, support)));
            var proof = OrnamentReconstruction.Participants(candidate.Connection);
            Assert.All(candidate.Supports, support => Assert.Contains(support, proof));
        }
        Assert.Same(originalGraph, source.Graph.Surface);
        Assert.Equal(strokes, Items(source.Graph.Strokes)); Assert.Equal(sites, Items(source.Graph.Sites));
        Assert.All(sites, site => Assert.Equal(1, Degree(source.Graph, site)));
    }

    [Fact]
    public void An_additive_axis_frame_does_not_supply_a_geometric_right_angle_without_the_selected_turn_model()
    {
        var plain = Supplier("N01"); var interpreted = Supplier("N02");
        Assert.Equal(plain.Candidates.Count, interpreted.Candidates.Count);
        Assert.All(plain.Candidates, candidate =>
        {
            Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteBoundary).Answer);
            Assert.Equal("Open", candidate.Read(candidate.Frame.FiniteRectangle).Answer);
        });
        Assert.All(interpreted.Candidates, candidate => Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteRectangle).Answer));
        Assert.Equal("Open", plain.Basis.ReadRightAngle(plain.Candidates[0].Frame.RightAngle).Answer);
    }

    [Fact]
    public void Unequal_native_family_intervals_are_compared_per_selected_pair_instead_of_by_fixture_name()
    {
        var supplier = Supplier("N03"); Assert.Equal(9, supplier.Candidates.Count);
        var answers = supplier.Candidates.Select(candidate => candidate.Read(candidate.Frame.FiniteSquare).Answer).ToArray();
        Assert.Equal(2, answers.Count(answer => answer == "Yes")); Assert.Equal(7, answers.Count(answer => answer == "No"));
        Assert.All(supplier.Candidates, candidate => Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteRectangle).Answer));
    }

    [Fact]
    public void Clipped_finite_supports_can_refute_coverage_without_refuting_their_extended_supporting_lines()
    {
        var supplier = Supplier("N04");
        Assert.All(supplier.Candidates, candidate => Assert.Equal("Yes", candidate.Read(candidate.Frame.PotentialBoundary).Answer));
        var answers = supplier.Candidates.Select(candidate => candidate.Read(candidate.Frame.FiniteBoundary).Answer).ToArray();
        Assert.Contains("No", answers); Assert.Contains("Yes", answers); Assert.DoesNotContain("Open", answers);
    }

    [Fact]
    public void Withheld_endpoint_words_stay_open_instead_of_being_recovered_from_private_execution_provenance()
    {
        var supplier = Supplier("N05");
        var answers = supplier.Candidates.Select(candidate => candidate.Read(candidate.Frame.FiniteBoundary).Answer).ToArray();
        Assert.Contains("Open", answers); Assert.DoesNotContain("No", answers);
        var unresolved = supplier.Candidates.First(candidate => candidate.Read(candidate.Frame.FiniteBoundary).Answer == "Open");
        var result = unresolved.Complete(unresolved.Frame.FiniteBoundary, AllowAll(unresolved.Frame));
        Assert.Equal("Open", result.SourceRead.Answer); Assert.NotEmpty(Items(result.SourceRead.ServiceOpen));
        Assert.Null(result.Completion);
    }

    [Fact]
    public void Coincident_transverse_positions_refute_strict_separation_without_merging_actual_passages()
    {
        var source = Case("N06"); var supplier = new NativeLineSupplier(source);
        var answers = supplier.Candidates.Select(candidate => candidate.Read(candidate.Frame.PotentialBoundary).Answer).ToArray();
        Assert.Contains("No", answers); Assert.Contains("Yes", answers);
        Assert.Equal(12, Items(source.Graph.Sites).Length); Assert.Equal(6, Items(source.Graph.Strokes).Length);
        Assert.All(Items(source.Graph.Sites), site => Assert.Equal(1, Degree(source.Graph, site)));
        Assert.Equal(6, new QualitativeJunctionEvidence(source.Graph, source.Bindings).Describe().ActualComponents);
    }

    [Fact]
    public void Withholding_straight_interpolation_prevents_endpoints_from_becoming_a_line_support_certificate()
    {
        var supplier = Supplier("N07"); Assert.NotEmpty(supplier.Candidates);
        Assert.All(supplier.Candidates, candidate =>
        {
            Assert.Equal("Open", candidate.Read(candidate.Frame.Straight).Answer);
            Assert.Equal("Open", candidate.Read(candidate.Frame.PotentialBoundary).Answer);
            var outcome = candidate.Complete(candidate.Frame.FiniteBoundary, AllowAll(candidate.Frame));
            Assert.Null(outcome.Completion);
        });
    }

    [Fact]
    public void A_negative_spacing_reply_short_circuits_all_other_geometric_services_and_rectangle_queries_omit_it()
    {
        var supplier = Supplier("N03");
        var nonSquare = supplier.Candidates.First(candidate => candidate.Read(candidate.Frame.EqualSpacing).Answer == "No");
        var rejected = nonSquare.Read(nonSquare.Frame.FiniteSquare);
        Assert.Equal("No", rejected.Answer);
        Assert.Same(nonSquare.Frame.EqualSpacing, Assert.Single(RequestedPredicates(rejected)));
        var rectangle = nonSquare.Read(nonSquare.Frame.FiniteRectangle);
        Assert.Equal("Yes", rectangle.Answer);
        Assert.DoesNotContain(RequestedPredicates(rectangle), predicate => ReferenceEquals(predicate, nonSquare.Frame.EqualSpacing));
        Assert.Equal(Items(rectangle.ServiceRequests).Length, Items(rectangle.ServiceAccepted).Length);
        Assert.Empty(Items(rectangle.ServiceBlocked)); Assert.Empty(Items(rectangle.ServiceOpen));
    }

    [Fact]
    public void Service_and_search_capacity_expose_unperformed_work_instead_of_assuming_it()
    {
        var bounded = new NativeLineSupplier(Case("N02"), candidateCapacity: 1);
        Assert.Single(bounded.Candidates); Assert.NotEmpty(Items(bounded.Unsearched));
        var candidate = bounded.Candidates[0];
        var read = candidate.Read(candidate.Frame.FiniteSquare, serviceCapacity: 0);
        Assert.Equal("Open", read.Answer); Assert.NotEmpty(Items(read.ServiceOpen));
        Assert.Empty(Items(read.ServiceConsumed));
        var outcome = candidate.Complete(candidate.Frame.FiniteSquare, AllowAll(candidate.Frame), serviceCapacity: 0);
        Assert.Equal("Open", outcome.SourceRead.Answer); Assert.Null(outcome.Completion);
    }

    [Fact]
    public void Independent_unmeasured_reach_atoms_do_not_become_an_inequality_between_family_gaps()
    {
        var original = Case("N02"); var replacement = new Placeholder(); var frame = original.Graph.Frame;
        Relation Word(Relation word) => Scope(Items(word).Cast<Relation>().Select(move =>
            (IElement)new Relation(move.Left, frame.Direction(move, out var axis, out _, out _) &&
                ReferenceEquals(axis, frame.Horizontal) ? replacement : move.Right)).ToArray());
        var strokes = Items(original.Graph.Strokes).Cast<Relation>().Select(stroke =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out var identity, out var from, out var to, out var word));
            return QualitativeStrokeGraph.Stroke(identity, from, to, Word(word));
        }).ToArray();
        var bindings = Scope(Items(original.Bindings).Cast<Relation>().Select(row => (IElement)new Relation(row.Left, Word((Relation)row.Right))).ToArray());
        var source = original with { Graph = new(frame, original.Graph.Sites, Scope(strokes)), Bindings = bindings, StraightPassages = Scope(strokes) };
        var supplier = new NativeLineSupplier(source);
        Assert.NotEmpty(supplier.Candidates);
        Assert.All(supplier.Candidates, candidate =>
        {
            Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteRectangle).Answer);
            Assert.Equal("Open", candidate.Read(candidate.Frame.EqualSpacing).Answer);
            Assert.Equal("Open", candidate.Read(candidate.Frame.FiniteSquare).Answer);
        });
    }

    [Fact]
    public void A_notched_route_cannot_be_flattened_into_a_straight_support_by_its_horizontal_net_displacement()
    {
        var supplier = Supplier("N12");
        var chains = Items(supplier.Support.Correspondence).Cast<Relation>().ToArray();
        Assert.NotEmpty(chains);
        foreach (var chain in chains)
        {
            var axes = Items((Relation)chain.Right).Cast<Relation>().Select(use =>
            {
                Assert.True(QualitativeStrokeGraph.TryReadStroke((Relation)use.Left, out _, out _, out _, out var word));
                var read = supplier.Source.Graph.Frame.Reduce(Scope(), word);
                Assert.True(supplier.Source.Graph.Frame.TryReadResult(read, out var rest, out _, out var unknown));
                Assert.Empty(Items(unknown));
                var first = Assert.IsType<Relation>(Items(rest)[0]);
                Assert.True(supplier.Source.Graph.Frame.Direction(first, out var axis, out _, out _));
                return axis;
            }).Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray();
            Assert.Single(axes);
        }
        Assert.All(OrnamentReconstruction.Participants(supplier.Surface), element =>
            Assert.True(element is Relation or Placeholder, "The public native supplier must not rely on measured carriers or render coordinates."));
    }

    [Fact]
    public void A_coarsened_support_cannot_hide_a_contradictory_source_occurrence_behind_its_first_representative()
    {
        var original = Case("N02"); var first = (Relation)Items(original.Graph.Strokes)[0];
        Assert.True(QualitativeStrokeGraph.TryReadStroke(first, out _, out var from, out var to, out var word));
        var contradictory = QualitativeStrokeGraph.Stroke(new Placeholder(), from, to,
            Scope(Items(word).Concat(Items(word)).ToArray()));
        var strokes = Scope(Items(original.Graph.Strokes).Append(contradictory).ToArray());
        var source = original with { Graph = new(original.Graph.Frame, original.Graph.Sites, strokes), StraightPassages = strokes };
        var supplier = new NativeLineSupplier(source);
        var dependent = supplier.Candidates.Where(candidate => Uses(supplier, candidate, contradictory)).ToArray();
        Assert.NotEmpty(dependent);
        foreach (var candidate in dependent)
        {
            Assert.Equal("Blocked", candidate.Read(candidate.Frame.Straight).Answer);
            Assert.Equal("Blocked", candidate.Read(candidate.Frame.FiniteBoundary).Answer);
        }
        Assert.Contains(supplier.Candidates, candidate => candidate.Read(candidate.Frame.FiniteBoundary).Answer == "Yes");
    }

    [Fact]
    public void An_open_binding_inventory_cannot_establish_unique_endpoint_positions_from_its_known_prefix()
    {
        var original = Case("N02");
        var source = original with { Bindings = StructuralConstraint.Open(Items(original.Bindings)) };
        var supplier = new NativeLineSupplier(source); Assert.NotEmpty(supplier.Candidates);
        foreach (var candidate in supplier.Candidates)
        {
            Assert.Equal("Open", candidate.Read(candidate.Frame.Straight).Answer);
            Assert.Equal("Open", candidate.Read(candidate.Frame.SeparateA).Answer);
            Assert.Equal("Open", candidate.Read(candidate.Frame.FiniteBoundary).Answer);
        }
    }

    [Fact]
    public void An_unresolved_signed_gap_cannot_refute_equal_span_magnitudes_after_other_terms_cancel()
    {
        var original = Case("N02"); var motion = original.Graph.Frame; var independentReach = new Placeholder();
        var strokes = Items(original.Graph.Strokes).Cast<Relation>().ToArray();
        Assert.True(QualitativeStrokeGraph.TryReadStroke(strokes[1], out _, out var from, out var to, out _));
        // This support's transverse position is c-b. The other selected
        // family gap is c; b=2c would make their absolute spans equal.
        var bindings = Scope(Items(original.Bindings).Cast<Relation>().Select(binding =>
            (IElement)(ReferenceEquals(binding.Left, from) || ReferenceEquals(binding.Left, to)
                ? new Relation(binding.Left, Scope(Items((Relation)binding.Right)
                    .Append(QualitativeMotionFrame.Move(motion.Down, independentReach)).ToArray()))
                : binding)).ToArray());
        var supplier = new NativeLineSupplier(original with { Bindings = bindings });
        var selected = supplier.Candidates.Where(candidate => new[] { strokes[0], strokes[1], strokes[3], strokes[4] }
            .All(source => Uses(supplier, candidate, source))).ToArray();
        var candidate = Assert.Single(selected);
        Assert.Equal("Yes", candidate.Read(candidate.Frame.Straight).Answer);
        Assert.Equal("Open", candidate.Read(candidate.Frame.SeparateA).Answer);
        Assert.Equal("Open", candidate.Read(candidate.Frame.EqualSpacing).Answer);
        Assert.Equal("Open", candidate.Read(candidate.Frame.PotentialSquare).Answer);
    }

    [Fact]
    public void Reversing_both_known_native_axis_directions_preserves_the_same_magnitude_comparisons()
    {
        var original = Case("N03"); var motion = original.Graph.Frame;
        Relation Reverse(Relation word) => Scope(Items(word).Cast<Relation>().Select(move =>
            (IElement)QualitativeMotionFrame.Move(((Relation)Items(motion.Opposites).Single(item =>
                item is Relation pair && ReferenceEquals(pair.Left, move.Left))).Right, move.Right)).ToArray());
        var strokes = Items(original.Graph.Strokes).Cast<Relation>().Select(stroke =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out var identity, out var from, out var to, out var word));
            return QualitativeStrokeGraph.Stroke(identity, from, to, Reverse(word));
        }).ToArray();
        var bindings = Scope(Items(original.Bindings).Cast<Relation>().Select(binding =>
            (IElement)new Relation(binding.Left, Reverse((Relation)binding.Right))).ToArray());
        var source = original with { Graph = new(motion, original.Graph.Sites, Scope(strokes)),
            Bindings = bindings, StraightPassages = Scope(strokes) };
        var supplier = new NativeLineSupplier(source); Assert.Equal(9, supplier.Candidates.Count);
        foreach (var candidate in supplier.Candidates)
            Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteRectangle).Answer);
        var answers = supplier.Candidates.Select(candidate => candidate.Read(candidate.Frame.FiniteSquare).Answer).ToArray();
        Assert.Equal(2, answers.Count(answer => answer == "Yes")); Assert.Equal(7, answers.Count(answer => answer == "No"));
    }

    [Fact]
    public void Independent_chord_and_endpoint_reaches_leave_consistency_open_instead_of_asserting_a_mismatch()
    {
        var original = Case("N02"); var independentReach = new Placeholder();
        var strokes = Items(original.Graph.Strokes).Cast<Relation>().ToArray();
        Assert.True(QualitativeStrokeGraph.TryReadStroke(strokes[0], out var identity, out var from, out var to, out var word));
        // The endpoint displacement remains expressed in r. Its chord now
        // offers repeated s: neither equality nor inequality is known.
        var offered = Scope(Items(word).Cast<Relation>().Select(move =>
            (IElement)QualitativeMotionFrame.Move(move.Left, independentReach)).ToArray());
        var changed = QualitativeStrokeGraph.Stroke(identity, from, to, offered); strokes[0] = changed;
        var source = original with { Graph = new(original.Graph.Frame, original.Graph.Sites, Scope(strokes)),
            StraightPassages = Scope(strokes) };
        var supplier = new NativeLineSupplier(source);
        var dependent = supplier.Candidates.Where(candidate => Uses(supplier, candidate, changed)).ToArray();
        Assert.NotEmpty(dependent);
        foreach (var candidate in dependent)
        {
            var straight = candidate.Read(candidate.Frame.Straight);
            Assert.Equal("Open", straight.Answer); Assert.Empty(Items(straight.Blocked));
            Assert.Single(Items(straight.ServiceAccepted));
            var actual = Assert.Single(straight.ObservedPublication.Evidence,
                fact => ReferenceEquals(fact.Predicate, candidate.Frame.Straight));
            Assert.Equal("Open", actual.Answer); Assert.NotEmpty(Items(actual.Open)); Assert.Empty(Items(actual.Blocked));
            Assert.Equal("Open", candidate.Read(candidate.Frame.FiniteBoundary).Answer);
        }
    }

    private static NativeLatticeSources.SourceCase Case(string id) => Sources.Value.Single(source => source.Id == id);
    private static NativeLineSupplier Supplier(string id) => new(Case(id));
    private static Relation AllowAll(StructuralPotential.Frame frame) => Scope(frame.Leaves.Select(leaf => (IElement)leaf.Predicate).ToArray());
    private static Relation[] RequestedPredicates(E.Receipt receipt) => Items(receipt.ServiceRequests).Cast<Relation>()
        .Select(request => (Relation)((Relation)((Relation)request.Right).Right).Right).ToArray();
    private static bool Uses(NativeLineSupplier supplier, NativeLineSupplier.Candidate candidate, Relation source)
    {
        Assert.NotNull(supplier.Publication);
        Assert.True(supplier.Publication.TryMapStroke(source, out var published));
        return candidate.Supports.Any(support => Items(supplier.Support.Correspondence).OfType<Relation>().Any(mapping =>
            ReferenceEquals(mapping.Left, support) && Items((Relation)mapping.Right).OfType<Relation>()
                .Any(use => ReferenceEquals(use.Left, published))));
    }
    private static int Degree(QualitativeStrokeGraph graph, IElement site) => Items(graph.Strokes).Cast<Relation>().Count(stroke =>
        QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _) &&
        (ReferenceEquals(site, from) || ReferenceEquals(site, to)));
    private static Relation Scope(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] Items(Relation scope) => FiniteLaw.Entries(scope);
}
