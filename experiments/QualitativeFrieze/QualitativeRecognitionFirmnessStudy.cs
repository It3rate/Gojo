using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Evidence-strength probes over published observations. Geometric
/// positions and source words belong only to this authoring/evaluation harness.</summary>
public sealed class QualitativeRecognitionFirmnessStudy
{
    public QualitativeRecognitionStudy Source { get; }
    public IReadOnlyList<QualitativeFirmnessField> Fields { get; }
    public QualitativeRecognitionCase PlacementSource { get; }
    public IReadOnlyList<QualitativeMotifOccurrence> Chambers { get; }
    public QualitativeMotifFamily Units { get; }
    public IReadOnlyList<QualitativePlacementProbe> Placements { get; }
    public IReadOnlyList<QualitativeRevalidationProbe> Revalidations { get; }
    public Relation Surface { get; }

    public QualitativeRecognitionFirmnessStudy(Action<string>? progress = null)
    {
        Source = new(progress);
        var square = Source.Cases.Single(field => field.Id == "R01");
        var staggered = Source.Cases.Single(field => field.Id == "R05");
        var fields = new List<QualitativeFirmnessField>();
        foreach (var id in new[] { "R01", "R04", "R05", "R06", "R07" })
        {
            var field = Source.Cases.Single(field => field.Id == id);
            fields.Add(new(id, field.Name, field.Description, field.Observation, field.Positions, field.Disposition));
        }
        // Same projected line arrangement, but every endpoint is a distinct
        // participant. Display coincidence never supplies a shared join.
        var splitSites = new List<IElement>(); var splitEdges = new List<IElement>();
        var splitPositions = new Dictionary<IElement, QualitativeRecognitionPoint>(N.Participants.Instance);
        foreach (var row in E(square.Observation.Segments).Cast<Relation>())
        {
            square.Observation.TryReadSegment(row, out _, out var a, out var b, out var heading);
            var from = new Placeholder(); var to = new Placeholder();
            splitSites.Add(from); splitSites.Add(to); splitPositions.Add(from, square.Positions[a]); splitPositions.Add(to, square.Positions[b]);
            splitEdges.Add(new Relation(new Placeholder(), new Relation(new Relation(from, to), heading)));
        }
        var split = new QualitativeRecognitionObservation(square.Observation.Frame, S(splitSites), S(splitEdges));
        fields.Add(new("F01", "Coincident ends without joins", "Its diagram resembles the square grid, but the endpoints of each stroke are separate identities. Direction admission must not become grid or flow evidence.", split, splitPositions, new(split)));
        // One isolated segment fits an existing heading but does not take part
        // in the observed alternating route or any commuting circuit.
        var spurFrom = new Placeholder(); var spurTo = new Placeholder();
        var spur = new Relation(new Placeholder(), new Relation(new Relation(spurFrom, spurTo),
            S(new IElement[] { staggered.Observation.Frame.Right, staggered.Observation.Frame.Up })));
        var spurPositions = new Dictionary<IElement, QualitativeRecognitionPoint>(staggered.Positions, N.Participants.Instance)
        { [spurFrom] = new(-3, -3), [spurTo] = new(-2, -1) };
        var withSpur = new QualitativeRecognitionObservation(staggered.Observation.Frame,
            S(E(staggered.Observation.Sites).Concat(new IElement[] { spurFrom, spurTo })),
            S(E(staggered.Observation.Segments).Append(spur)), staggered.Observation.Continuations);
        fields.Add(new("F02", "An admitted but unsupported segment", "The extra diagonal belongs to the chosen direction families. It is isolated from their longer routes and circuits, so admission and demonstrated participation differ.", withSpur, spurPositions, new(withSpur)));
        Fields = fields;

        PlacementSource = Source.Cases.Single(field => field.Id == "R08");
        var observation = PlacementSource.Observation;
        var fourCycles = PlacementSource.Motifs.Families.Where(family => family.Kind == "cycle" && family.SegmentCount == 4).ToArray();
        var chamberFamily = fourCycles.Single(family => E(family.Occurrences[0].Segments).Cast<Relation>().All(row => HeadingCount(row) == 1));
        Units = fourCycles.Single(family => E(family.Occurrences[0].Segments).Cast<Relation>().All(row => HeadingCount(row) == 2));
        Chambers = chamberFamily.Occurrences;
        var declarations = S(E((Relation)((Relation)PlacementSource.SourceTopology.Right).Left).Cast<Relation>().Select(row =>
        {
            var ends = (Relation)row.Right;
            return (IElement)QualitativeRecognitionPlacementPublisher.Envelope(PlacementSource.SourceFrame, row.Left, ends.Left, ends.Right);
        }));
        var placements = new List<QualitativePlacementProbe>();
        var baseline = new QualitativeRecognitionPlacementPublisher(PlacementSource.SourceFrame, PlacementSource.SourceBindings,
            PlacementSource.Publisher, declarations);
        AddPlacement("P01", "Order and passage constraints supplied", "Two ordinal projection orders locate the motifs in rectangular interval chambers. Explicit passage envelopes justify claims about whole line support, beyond the endpoints alone.", baseline.Placement, PlacementSource.Positions, baseline.Surface);
        Move("P02", "A motif shares its neighbour's chamber", "One complete diamond is translated to the next chamber. The original segment observation and local motif matches are unchanged; ordinal placement exposes one empty slot and two candidates for another slot.", 4);
        Move("P03", "A motif straddles two chambers", "The same diamond is moved halfway across a chamber boundary. Its local structure remains valid, while strict placement in either chamber fails.", 2);
        AddPlacement("P04", "One projection order withheld", "The displayed geometry is retained as a guide. Without the second ordinal observation, full chamber placement stays unresolved.",
            new(observation, baseline.Placement.HorizontalBuckets, StructuralConstraint.Open(), baseline.Placement.Envelopes), PlacementSource.Positions, baseline.Surface);
        AddPlacement("P05", "Endpoints without passage envelopes", "The site orders are known. The line may still leave its endpoint box: endpoint containment alone cannot establish whole-support containment.",
            new(observation, baseline.Placement.HorizontalBuckets, baseline.Placement.VerticalBuckets), PlacementSource.Positions, baseline.Surface);
        Placements = placements;

        var target = (Relation)E(Units.Occurrences[0].Segments)[0];
        var targetEnds = (Relation)((Relation)target.Right).Left;
        var validations = new List<QualitativeRevalidationProbe>();
        AddRevalidation("V01", "Original observation", "Reapply every preserved role graph at its original bindings. Recognition is unchanged.", observation, S());
        var changed = new Relation(target.Left, new Relation(targetEnds, S(new IElement[] { observation.Frame.Right })));
        AddRevalidation("V02", "One heading changes", "An explicit correspondence replaces one observed segment. Dependent heading matches fail; remote matches remain available. The diagram retains its old geometric guide.", Replace(changed, false), S(new IElement[] { new Relation(target, changed) }));
        var unknown = new Relation(target.Left, new Relation(targetEnds, StructuralConstraint.Open()));
        AddRevalidation("V03", "One heading is unavailable", "The replacement passage still exists, but its heading is open. Dependent obligations remain unresolved instead of being declared false.", Replace(unknown, false), S(new IElement[] { new Relation(target, unknown) }));
        AddRevalidation("V04", "One passage withdrawn from a closed inventory", "The inventory explicitly declares all currently observed passages. The retained occurrence's missing passage is a counterexample.", Replace(null, false), S());
        AddRevalidation("V05", "One passage absent from an open inventory", "Only a prefix is published. Failure to see the passage cannot establish its absence; dependent matches remain unresolved.", Replace(null, true), S());
        Revalidations = validations;
        Surface = new(S(fields.Select(field => (IElement)field.Evidence.Surface)), new Relation(
            S(placements.Select(probe => (IElement)probe.Surface)), S(validations.Select(probe => (IElement)probe.Read.Surface))));

        int HeadingCount(Relation row) { observation.TryReadSegment(row, out _, out _, out _, out var heading); return E(heading).Length; }
        void AddPlacement(string id, string name, string description, QualitativeRecognitionPlacement placement,
            IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> positions, Relation provenance) =>
            placements.Add(new(id, name, description, placement, Chambers, Units, positions, provenance));
        void Move(string id, string name, string description, int dx)
        {
            var movedSites = E(Units.Occurrences[0].Sites).ToHashSet(N.Participants.Instance);
            var sourceSites = E(PlacementSource.Publisher.SiteMap).Cast<Relation>().Where(row => movedSites.Contains(row.Right))
                .Select(row => row.Left).ToHashSet(N.Participants.Instance);
            var bindings = S(E(PlacementSource.SourceBindings).Cast<Relation>().Select(row =>
            {
                if (!sourceSites.Contains(row.Left)) return (IElement)row;
                var word = (Relation)row.Right;
                var reach = ((Relation)E(word)[0]).Right;
                var motion = S(Enumerable.Range(0, dx).Select(_ => (IElement)QualitativeMotionFrame.Move(PlacementSource.SourceFrame.Right, reach)));
                var read = PlacementSource.SourceFrame.Reduce(word, motion);
                PlacementSource.SourceFrame.TryReadResult(read, out var result, out _, out _);
                return new Relation(row.Left, result);
            }));
            var published = new QualitativeRecognitionPlacementPublisher(PlacementSource.SourceFrame, bindings, PlacementSource.Publisher, declarations);
            var positions = PlacementSource.Positions.ToDictionary(pair => pair.Key,
                pair => movedSites.Contains(pair.Key) ? pair.Value with { X = pair.Value.X + dx } : pair.Value, N.Participants.Instance);
            AddPlacement(id, name, description, published.Placement, positions, published.Surface);
        }
        QualitativeRecognitionObservation Replace(Relation? replacement, bool open)
        {
            var rows = E(observation.Segments).Where(row => !ReferenceEquals(row, target)).ToList();
            if (replacement is not null) rows.Add(replacement);
            // The original continuation observations involving the replaced
            // occurrence are withheld; they are not silently rebound.
            var continuations = E(observation.Continuations).Cast<Relation>().Where(row =>
                row.Right is Relation pair && !ReferenceEquals(pair.Left, target) && !ReferenceEquals(pair.Right, target));
            return new(observation.Frame, observation.Sites, open ? StructuralConstraint.Open(rows.ToArray()) : S(rows), S(continuations));
        }
        void AddRevalidation(string id, string name, string description, QualitativeRecognitionObservation current, Relation correspondence) =>
            validations.Add(new(id, name, description, current, new(PlacementSource.Motifs, current, correspondence), target));
    }
    private static IElement[] E(Relation scope) => N.E(scope);
    private static Relation S(IEnumerable<IElement> items) => N.S(items.ToArray());
    private static Relation S() => N.S();
}

public sealed class QualitativeFirmnessField(string id, string name, string description,
    QualitativeRecognitionObservation observation, IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> positions,
    QualitativeDispositionInduction induction)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public QualitativeRecognitionObservation Observation { get; } = observation;
    public IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> Positions { get; } = positions;
    public QualitativeDispositionInduction Induction { get; } = induction;
    public QualitativeDispositionEvidence Evidence { get; } = new(induction);
}

/// <summary>A single motif slot for each actual chamber. Assignment witnesses
/// connect the retained motif occurrence to its conditional containment read.</summary>
public sealed class QualitativePlacementProbe
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public QualitativeRecognitionPlacement Placement { get; }
    public IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> Positions { get; }
    public IReadOnlyList<QualitativeRecognitionContainment> Queries { get; }
    public Relation Requirements { get; }
    public Relation Accepted { get; }
    public Relation Empty { get; }
    public Relation Competing { get; }
    public Relation Unresolved { get; }
    public Relation Unplaced { get; }
    public Relation Considered { get; }
    public Relation ExternalProvenance { get; }
    public Relation Surface { get; }

    public QualitativePlacementProbe(string id, string name, string description, QualitativeRecognitionPlacement placement,
        IReadOnlyList<QualitativeMotifOccurrence> chambers, QualitativeMotifFamily units,
        IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> positions, Relation provenance)
    {
        Id = id; Name = name; Description = description; Placement = placement; Positions = positions; ExternalProvenance = provenance;
        var requirements = new List<IElement>(); var accepted = new List<IElement>(); var empty = new List<IElement>();
        var competing = new List<IElement>(); var pending = new List<IElement>(); var queries = new List<QualitativeRecognitionContainment>();
        var considered = new List<IElement>();
        var placed = new HashSet<IElement>(N.Participants.Instance);
        var uncertain = new HashSet<IElement>(N.Participants.Instance);
        foreach (var chamber in chambers)
        {
            var requirement = new Relation(chamber.Read, new Relation(new Placeholder(), units.Definition)); requirements.Add(requirement);
            var assignments = new List<IElement>(); var unknown = new List<IElement>();
            var localQueries = new List<IElement>();
            foreach (var unit in units.Occurrences)
            {
                var query = placement.ReadContainment(chamber.Segments, unit.Segments); queries.Add(query);
                localQueries.Add(new Relation(unit.Read, query.Surface));
                var binding = new Relation(requirement, new Relation(unit.Read, query.Surface));
                if (query.IsStrictlyInside) { assignments.Add(binding); placed.Add(unit.Read); }
                else if (N.E(query.Unresolved).Length != 0 || N.E(query.Blocked).Length != 0)
                { unknown.Add(binding); uncertain.Add(unit.Read); }
            }
            accepted.AddRange(assignments);
            considered.Add(new Relation(requirement, new Relation(units.Read, N.S(localQueries.ToArray()))));
            if (assignments.Count > 1) competing.Add(new Relation(requirement, N.S(assignments.ToArray())));
            if (unknown.Count != 0) pending.Add(new Relation(requirement, N.S(unknown.ToArray())));
            else if (assignments.Count == 0) empty.Add(new Relation(requirement, N.S()));
        }
        Queries = queries; Requirements = N.S(requirements.ToArray()); Accepted = N.S(accepted.ToArray());
        Empty = N.S(empty.ToArray()); Competing = N.S(competing.ToArray()); Unresolved = N.S(pending.ToArray());
        Unplaced = N.S(units.Occurrences.Where(unit => !placed.Contains(unit.Read) && !uncertain.Contains(unit.Read)).Select(unit => (IElement)unit.Read).ToArray());
        Considered = N.S(considered.ToArray());
        Surface = new(new Relation(placement.Surface, new Relation(Requirements, Considered)),
            new Relation(Accepted, new Relation(Empty, new Relation(Competing, new Relation(Unresolved, Unplaced)))));
    }
}

public sealed record QualitativeRevalidationProbe(string Id, string Name, string Description,
    QualitativeRecognitionObservation Observation, QualitativeRecognitionRevalidation Read, Relation Target);
