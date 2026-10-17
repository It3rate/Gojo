using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored, exploratory profiles over advertised graph and measured
/// observations. Ordinal points are a diagnostic interpretation, not a human
/// preference model. Missing evidence never contributes a favorable zero.</summary>
public static class ConnectedPatternScoring
{
    private static readonly IEqualityComparer<IElement> Identity = ReferenceEqualityComparer.Instance;
    public sealed record Feature(string Name, Relation Criterion, Relation Observation, int? Rank);
    public sealed record Evaluation(Relation Source, PatternAestheticProfile Profile, Relation Read,
        PatternAestheticProfile.Summary Summary, int? Score, IReadOnlyList<Feature> Features, Relation Surface);

    public static Evaluation Native(ConnectedPatternEntry entry)
    {
        var graph = entry.Graph; var read = graph.Evaluate(); var complete = Complete(entry, read);
        graph.TryReadResult(read, out var junctions, out _, out _, out _);
        var strokes = E(graph.Strokes).OfType<Relation>().ToArray();
        var vocabulary = S(strokes.Select(stroke => { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word); return word; })
            .Where(word => word is not null).Distinct(Identity).Select(word => (IElement)new Relation(word,
                S(strokes.Where(stroke => QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var actual) && ReferenceEquals(actual, word)).Cast<IElement>().ToArray()))).ToArray());
        var endpoints = new List<IElement>(); var signatures = new Dictionary<(int Rays, int Opposed, int Cooriented), List<Relation>>();
        foreach (var node in E(junctions).OfType<Relation>())
            if (graph.TryReadJunction(node, out _, out var rays, out var opposed, out _) &&
                node.Right is Relation { Right: Relation { Right: Relation { Left: Relation cooriented } } })
            {
                var count = E(rays).Length; if (count == 0) continue; // Unused basis landmarks are outside the ink focus.
                if (count == 1) endpoints.Add(node);
                var signature = (count, E(opposed).Length, E(cooriented).Length);
                if (!signatures.TryGetValue(signature, out var members)) signatures.Add(signature, members = []);
                members.Add(node);
            }
        var kinds = S(signatures.Values.Select(nodes => (IElement)new Relation(nodes[0], S(nodes.Cast<IElement>().ToArray()))).ToArray());
        var maps = S(entry.Correspondences.Where(map => graph.TryReadMapPassed(graph, map.Read) &&
            E(map.SiteMap).Concat(E(map.StrokeMap)).Concat(E(map.ChordMap)).OfType<Relation>().Any(row => !ReferenceEquals(row.Left, row.Right)))
            .Select(map => (IElement)map.Read).ToArray());
        return Build(entry.Surface, new[] {
            Cardinality("Shared chord vocabulary", read, vocabulary, 2, 8, 3, 6, complete),
            Cardinality("Endpoint junctions", read, S(endpoints.ToArray()), 0, 12, 0, 4, complete),
            Cardinality("Occupied junction signatures", read, kinds, 1, 6, 2, 5, complete),
            Cardinality("Nonidentity graph correspondences", read, maps, 0, 8, 1, 8, complete) });
    }

    public static Evaluation Measured(ConnectedPatternEntry entry, Relation geometry, FriezeCoverage.Evaluation coverage)
    {
        var complete = Complete(entry, entry.Graph.Evaluate()) &&
            FriezeMeasuredRules.TryReadResult(geometry, out _, out _, out _, out _, out _, out var geometryPending) && E(geometryPending).Length == 0;
        var based = GeometryBasis(entry, geometry, coverage);
        // Recalculate the native observer read. Projection is a presentation
        // record and cannot establish cell counts, clearance or completeness.
        var fresh = coverage.Coverage.Evaluate();
        var readable = coverage.Coverage.TryReadResult(fresh, out var empty, out _, out var multiple, out var layers, out var centers, out var pending);
        complete &= based && readable && E(pending).Length == 0 && Closed(coverage.Coverage.Cells);
        var cellCount = E(coverage.Coverage.Cells).Length; var emptyCount = E(empty).Length;
        var metric = new Carrier();
        var fraction = cellCount > 0 ? new Relation(new CarrierLocation(metric, emptyCount), Relation.Point(metric, cellCount)) : StructuralConstraint.Open();
        Relation Fraction(int numerator) => new(new CarrierLocation(metric, numerator), Relation.Point(metric, 100));
        var fractionBand = new Relation(new Relation(Fraction(20), Fraction(80)), new Relation(Fraction(30), Fraction(65)));
        int? fractionRank = complete && cellCount > 0 ?
            emptyCount * (BigInteger)100 < cellCount * (BigInteger)20 || emptyCount * (BigInteger)100 > cellCount * (BigInteger)80 ? 0 :
            emptyCount * (BigInteger)100 >= cellCount * (BigInteger)30 && emptyCount * (BigInteger)100 <= cellCount * (BigInteger)65 ? 2 : 1 : null;
        var indices = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
        foreach (var layer in E(layers).OfType<Relation>())
            if (coverage.Coverage.TryReadLayer(layer, out var previous, out _))
            {
                if (previous is Relation prior && indices.TryGetValue(prior, out var ordinal)) indices[layer] = ordinal + 1;
                else if (previous is Relation root && Closed(root) && E(root).Length == 0) indices[layer] = 0;
            }
        var ordinals = E(centers).OfType<Relation>().Select(center => coverage.Coverage.TryReadCenter(center, out var layer, out _) &&
            indices.TryGetValue(layer, out var ordinal) ? ordinal : -1).ToArray();
        var centerKnown = complete && ordinals.Length > 0 && ordinals.All(ordinal => ordinal >= 0);
        var spread = centerKnown ? ordinals.Max() - ordinals.Min() : 0;
        var clearance = new Relation(new Relation(centers, layers), new Relation(centerKnown ? Relation.Segment(metric, 0, spread) : StructuralConstraint.Open(),
            new Relation(Relation.Segment(metric, 0, 5), Relation.Segment(metric, 0, 1))));
        var crowdCount = E(multiple).Length;
        var crowdFraction = cellCount > 0 ? new Relation(new CarrierLocation(metric, crowdCount), Relation.Point(metric, cellCount)) : StructuralConstraint.Open();
        var crowdBand = new Relation(new Relation(Fraction(0), Fraction(35)), new Relation(Fraction(0), Fraction(20)));
        int? crowdRank = complete && cellCount > 0 ? crowdCount * (BigInteger)100 <= cellCount * (BigInteger)20 ? 2 :
            crowdCount * (BigInteger)100 <= cellCount * (BigInteger)35 ? 1 : 0 : null;
        var verified = new List<IElement>(); var beyond = false;
        if (FriezeMeasuredRules.TryReadResult(geometry, out _, out _, out _, out _, out var symmetries, out _))
            foreach (var value in E(symmetries).OfType<Relation>())
                if (FriezeMeasuredRules.TryReadSymmetry(value, out var operation, out var proof, out var missing) &&
                    E(missing).Length == 0 && E(proof).Length > 0 && operation.Left is Relation x && operation.Right is Relation y &&
                    Orientation(x, out var sx) && Orientation(y, out var sy))
                { verified.Add(value); beyond |= sx < 0 || sy < 0; }
        return Build(entry.Surface, new[] {
            new Datum("Empty probe fraction", coverage.Surface, new Relation(new Relation(fresh, fractionBand), fraction), fractionRank),
            new Datum("Clearance center spread", coverage.Surface, new Relation(fresh, clearance), centerKnown ? spread <= 1 ? 2 : spread <= 5 ? 1 : 0 : null),
            new Datum("Measured support isometries", geometry, S(verified.ToArray()), complete && verified.Count > 0 ? beyond ? 2 : 1 : null),
            new Datum("Probe crowding", coverage.Surface, new Relation(new Relation(fresh, crowdBand), crowdFraction), crowdRank) });
    }

    // Every threshold is a finite native slot scope. The observed subjects and
    // the four boundary scopes stay connected, without numeric source carriers.
    private static Datum Cardinality(string name, Relation source, Relation observed, int low, int high, int preferredLow, int preferredHigh, bool complete)
    {
        Relation Slots(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
        var band = new Relation(new Relation(Slots(low), Slots(high)), new Relation(Slots(preferredLow), Slots(preferredHigh)));
        var witness = new Relation(new Relation(source, observed), band); var count = E(observed).Length;
        int? rank = complete ? count < low || count > high ? 0 : count >= preferredLow && count <= preferredHigh ? 2 : 1 : null;
        return new(name, new Relation(source, observed), witness, rank);
    }
    private sealed record Datum(string Name, Relation Basis, Relation Witness, int? Rank);
    private static Evaluation Build(Relation source, IEnumerable<Datum> values)
    {
        var rejected = new Placeholder(); var admitted = new Placeholder(); var desired = new Placeholder();
        var domain = S(rejected, admitted, desired); var outcomes = new IElement[] { rejected, admitted, desired };
        var features = values.Select(value => {
            var criterion = PatternAestheticProfile.Criterion(value.Basis, domain, S(admitted, desired), S(desired));
            return new Feature(value.Name, criterion, PatternAestheticProfile.Observation(criterion, value.Witness,
                value.Rank is int rank ? S(outcomes[rank]) : StructuralConstraint.Open()), value.Rank);
        }).ToArray();
        var profile = new PatternAestheticProfile(S(features.Select(feature => (IElement)feature.Criterion).ToArray()));
        var observations = S(features.Select(feature => (IElement)feature.Observation).ToArray()); var read = profile.Evaluate(observations);
        var summary = profile.Describe(read); int? score = summary.Complete ? 2 * summary.Preferred + summary.Passed : null;
        return new(source, profile, read, summary, score, Array.AsReadOnly(features), new Relation(source, new Relation(profile.Surface, new Relation(observations, read))));
    }
    private static bool Complete(ConnectedPatternEntry entry, Relation read) => Closed(entry.Unresolved) && E(entry.Unresolved).Length == 0 &&
        entry.Graph.TryReadResult(read, out _, out _, out _, out var pending) && E(pending).Length == 0;
    private static bool GeometryBasis(ConnectedPatternEntry entry, Relation geometry, FriezeCoverage.Evaluation coverage) =>
        FriezeMeasuredRules.TryReadResult(geometry, out var strokes, out _, out _, out _, out _, out _) &&
        geometry.Left is Relation { Left: Relation { Left: Relation { Left: Relation provenance } } } &&
        ReferenceEquals(provenance.Left, entry.Graph.Surface) && provenance.Right is Relation context && ReferenceEquals(context.Left, entry.Surface) &&
        coverage.Surface.Left is Relation { Left: Relation supplier } && ReferenceEquals(supplier.Left, geometry) && ReferenceEquals(supplier.Right, strokes) &&
        coverage.Surface.Right is Relation { Right: Relation body } && ReferenceEquals(body.Left, coverage.Read) &&
        body.Right is Relation pending && Closed(pending) && E(pending).Length == 0 && ReferenceEquals(coverage.Read.Left, coverage.Coverage.Surface);
    private static bool Orientation(Relation correspondence, out int sign)
    {
        sign = 0;
        if (correspondence.Left is not Relation from || correspondence.Right is not Relation to ||
            from.Left is not Relation { Right: Carrier basis } || to.Left is not Relation { Right: Carrier target } || !ReferenceEquals(basis, target) ||
            !Delta(from, out var n, out var d) || n != d || !Delta(to, out n, out d) || BigInteger.Abs(n) != d) return false;
        sign = n.Sign; return true;
    }
    private static bool Delta(Relation span, out BigInteger n, out BigInteger d)
    {
        n = 0; d = 1;
        if (span.Left is not Relation { Right: Carrier a } first || span.Right is not Relation { Right: Carrier b } last ||
            !ReferenceEquals(a, b) || !Coordinate(first.Left, a, out var x, out var xd) || !Coordinate(last.Left, a, out var y, out var yd)) return false;
        n = y * xd - x * yd; d = xd * yd; return true;
    }
    private static bool Coordinate(IElement coordinate, Carrier carrier, out BigInteger n, out BigInteger d)
    {
        n = 0; d = 1;
        if (carrier.IsUnknown || carrier.IsInverse) return false;
        if (coordinate is CarrierLocation point && ReferenceEquals(point.Carrier, carrier)) { n = point.Ticks; return true; }
        if (coordinate is Relation { Left: CarrierLocation numerator, Right: Relation { Left: CarrierLocation denominator, Right: Carrier unit } } &&
            ReferenceEquals(numerator.Carrier, carrier) && ReferenceEquals(denominator.Carrier, carrier) && ReferenceEquals(unit, carrier) && denominator.Ticks > 0)
        { n = numerator.Ticks; d = denominator.Ticks; return true; }
        return false;
    }
    private static IElement[] E(Relation? scope) => scope is not null && StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation? scope) => scope is not null && StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
