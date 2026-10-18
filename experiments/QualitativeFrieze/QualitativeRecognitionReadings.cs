using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Connects locally induced motifs with every retained disposition.
/// A fragment can lie inside, across or outside a chosen channel read without
/// changing the original observation or assigning it to one winning concept.</summary>
public sealed class QualitativeRecognitionReadings
{
    // occurrence use = actual motif family -> actual occurrence
    // partition = actual occurrence use -> (inside segments -> outside segments)
    // reading = (actual disposition candidate -> actual motif capture) ->
    //           (contained -> (straddling -> (outside -> (partitions ->
    //           (unique covered participation -> unaccounted selected participation)))))
    // support extension = (actual lower reading -> actual richer reading) ->
    //    (paired actual selected segments -> (introduced actual segments ->
    //    ((common basis correspondence -> (old-only basis -> new-only basis)) ->
    //    (richer unsettled -> richer competing))))
    // This is refinement of explained selected support. Different basis roles
    // stay visible; the witness does not claim equivalent concepts, a unique
    // winner, or that one whole grammar logically entails the other.
    public QualitativeMotifInduction Motifs { get; }
    public QualitativeDispositionInduction Dispositions { get; }
    public IReadOnlyList<QualitativeRecognitionReading> Readings { get; }
    public Relation Refinements { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public QualitativeRecognitionReadings(QualitativeMotifInduction motifs, QualitativeDispositionInduction dispositions)
    {
        Motifs = motifs; Dispositions = dispositions;
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var readings = new List<QualitativeRecognitionReading>(); var refinements = new List<IElement>();
        if (!ReferenceEquals(motifs.Observation.Surface, dispositions.Observation.Surface))
            blocked.Add(new Relation(motifs.Observation.Surface, dispositions.Observation.Surface));
        if (!motifs.IsComplete) pending.Add(new Relation(motifs.Unsearched, motifs.Unresolved));
        if (!dispositions.IsComplete) pending.Add(new Relation(dispositions.Unsearched, dispositions.Unresolved));
        var occurrences = motifs.Families.SelectMany(family => family.Occurrences.Select(occurrence =>
            (Family: family, Occurrence: occurrence, Edges: N.E(occurrence.Segments)))).ToArray();
        if (blocked.Count == 0)
            foreach (var row in N.E(dispositions.Candidates))
            {
                if (row is not Relation actual || !dispositions.TryReadCandidate(actual, out var candidate))
                { blocked.Add(row); continue; }
                var contained = new List<IElement>(); var straddling = new List<IElement>(); var outside = new List<IElement>();
                var partitions = new List<IElement>(); var covered = new List<IElement>();
                var selected = N.E(candidate.SelectedSegments);
                var selection = selected.ToHashSet(N.Participants.Instance);
                foreach (var item in occurrences)
                    {
                        var inside = item.Edges.Where(selection.Contains).ToArray();
                        var rest = item.Edges.Where(edge => !selection.Contains(edge)).ToArray();
                        var use = new Relation(item.Family.Read, item.Occurrence.Read);
                        if (rest.Length == 0) { contained.Add(use); covered.AddRange(inside); }
                        else if (inside.Length == 0) outside.Add(use);
                        else straddling.Add(use);
                        partitions.Add(new Relation(use, new Relation(N.S(inside), N.S(rest))));
                    }
                var coverage = covered.ToHashSet(N.Participants.Instance);
                var cover = N.S(coverage.ToArray());
                var remainder = N.S(selected.Where(edge => !coverage.Contains(edge)).ToArray());
                var containedScope = N.S(contained.ToArray()); var straddlingScope = N.S(straddling.ToArray());
                var outsideScope = N.S(outside.ToArray()); var partitionScope = N.S(partitions.ToArray());
                var read = new Relation(new Relation(candidate.Read, motifs.Surface), new Relation(containedScope,
                    new Relation(straddlingScope, new Relation(outsideScope, new Relation(partitionScope,
                        new Relation(cover, remainder))))));
                readings.Add(new(candidate, read, containedScope, straddlingScope, outsideScope, partitionScope, cover, remainder));
            }
        Readings = readings;
        var selections = readings.ToDictionary(reading => reading, reading => N.E(reading.Candidate.SelectedSegments).ToHashSet(N.Participants.Instance));
        foreach (var lower in readings)
            foreach (var richer in readings)
            {
                var previous = N.E(lower.Candidate.SelectedSegments); var current = N.E(richer.Candidate.SelectedSegments);
                var oldSelection = selections[lower]; var newSelection = selections[richer];
                if (previous.Length >= current.Length || !oldSelection.IsSubsetOf(newSelection) ||
                    N.E(richer.Candidate.Unsettled).Length != 0 || N.E(richer.Candidate.Competing).Length != 0) continue;
                var correspondence = N.S(previous.Select(edge => (IElement)new Relation(edge,
                    current.Single(other => N.Same(edge, other)))).ToArray());
                var introduced = N.S(current.Where(edge => !oldSelection.Contains(edge)).ToArray());
                var oldBasis = N.E(lower.Candidate.Basis); var newBasis = N.E(richer.Candidate.Basis);
                var common = N.S(oldBasis.Where(basis => newBasis.Any(other => ReferenceEquals(basis, other)))
                    .Select(basis => (IElement)new Relation(basis, newBasis.Single(other => ReferenceEquals(basis, other)))).ToArray());
                var beforeOnly = N.S(oldBasis.Where(basis => !newBasis.Any(other => ReferenceEquals(basis, other))).ToArray());
                var afterOnly = N.S(newBasis.Where(basis => !oldBasis.Any(other => ReferenceEquals(basis, other))).ToArray());
                refinements.Add(new Relation(new Relation(lower.Surface, richer.Surface), new Relation(correspondence,
                    new Relation(introduced, new Relation(new Relation(common, new Relation(beforeOnly, afterOnly)),
                        new Relation(richer.Candidate.Unsettled, richer.Candidate.Competing))))));
            }
        Refinements = N.S(refinements.ToArray()); Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(motifs.Surface, dispositions.Surface), new Relation(N.S(readings.Select(reading => (IElement)reading.Surface).ToArray()),
            new Relation(Refinements, new Relation(Blocked, Unresolved))));
    }

    public Relation Evaluate() => Surface;
}

/// <summary>A decoded relationship between a disposition and local motifs.
/// Category rows are family-to-occurrence references; their exact segment
/// partitions and unique covered participation remain connected to Surface.</summary>
public sealed class QualitativeRecognitionReading(QualitativeDispositionCandidate candidate, Relation surface,
    Relation contained, Relation straddling, Relation outside, Relation partitions, Relation covered, Relation remainder)
{
    public QualitativeDispositionCandidate Candidate { get; } = candidate;
    public Relation Surface { get; } = surface;
    public Relation Contained { get; } = contained;
    public Relation Straddling { get; } = straddling;
    public Relation Outside { get; } = outside;
    public Relation Partitions { get; } = partitions;
    public Relation Covered { get; } = covered;
    public Relation Remainder { get; } = remainder;
}
