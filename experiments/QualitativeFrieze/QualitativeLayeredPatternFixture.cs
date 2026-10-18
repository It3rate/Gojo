using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Stimulus authoring and display supplier. The recipe, source words
/// and positions stay outside all public pattern and query interfaces.</summary>
public sealed class QualitativeLayeredPatternFixture
{
    public QualitativeMotionFrame SourceFrame { get; } = new();
    public IElement Reach { get; } = new Placeholder();
    public Relation SourceTopology { get; }
    public Relation SourceBindings { get; }
    public Relation SourceEnvelopes { get; }
    public QualitativeRecognitionPublisher Publisher { get; }
    public QualitativeRecognitionObservation Observation => Publisher.Observation;
    public QualitativeMotifInduction Motifs { get; }
    public QualitativeRecognitionPlacementPublisher OrdinalPublisher { get; }
    public QualitativeRecognitionPlacement Placement => OrdinalPublisher.Placement;
    public IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> Positions { get; }
    public IReadOnlyDictionary<IElement, (int Column, int Row)> ChamberCoordinates { get; }
    public Relation ChamberFocus { get; }
    public Relation AlphabetFocus { get; }
    public int Columns { get; }
    public int Rows { get; }
    public Relation AuditSurface { get; }

    // Recipe numbers select authored stimuli only. Every drawn participant is
    // built below as native incidence and formal displacement; no recipe value
    // is published to the recognizers.
    public QualitativeLayeredPatternFixture(int columns, int rows, Func<int, int, int> recipe,
        (int Column, int Row)? overlappingUnit = null)
    {
        Columns = columns; Rows = rows;
        var sites = new List<IElement>(); var edges = new List<Relation>();
        var positions = new Dictionary<IElement, QualitativeRecognitionPoint>(N.Participants.Instance);
        var grid = new IElement[columns + 1, rows + 1];
        for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++) grid[x, y] = Site(8 * x, 8 * y);
        for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++)
        {
            if (x < columns) Edge(grid[x, y], grid[x + 1, y]);
            if (y < rows) Edge(grid[x, y], grid[x, y + 1]);
        }
        for (var y = 0; y < rows; y++) for (var x = 0; x < columns; x++)
        {
            Unit(8 * x + 4, 8 * y + 4, recipe(x, y));
            if (overlappingUnit is { } extra && extra.Column == x && extra.Row == y)
                Unit(8 * x + 4, 8 * y + 4, 2);
        }
        SourceTopology = new(S(sites), new Relation(S(edges), N.S()));
        SourceBindings = S(sites.Select(site => (IElement)new Relation(site, Word(positions[site]))));
        SourceEnvelopes = S(edges.Select(edge =>
        {
            var ends = (Relation)edge.Right;
            return (IElement)QualitativeRecognitionPlacementPublisher.Envelope(SourceFrame, edge.Left, ends.Left, ends.Right);
        }));
        Publisher = new(SourceFrame, SourceTopology, SourceBindings);
        var publishedPositions = new Dictionary<IElement, QualitativeRecognitionPoint>(N.Participants.Instance);
        foreach (var pair in positions)
            if (Publisher.TryMapSite(pair.Key, out var actual)) publishedPositions.Add(actual, pair.Value);
        Positions = publishedPositions;
        Motifs = new(Observation, maxCycleEdges: 4, maxWalkEdges: 2, proposalCapacity: 100000, retainedCapacity: 15000);
        OrdinalPublisher = new(SourceFrame, SourceBindings, Publisher, SourceEnvelopes);

        // Focus on the boundary-connected rectangle network. An inner square
        // has the same local role graph as a chamber but no shared boundary
        // with another candidate. This distinction is native incidence.
        var rectangles = Motifs.Families.Where(family => family.Kind == "cycle" && family.SegmentCount == 4)
            .SelectMany(family => family.Occurrences).Where(occurrence => E(occurrence.Segments).Cast<Relation>().All(row =>
            {
                Observation.TryReadSegment(row, out _, out _, out _, out var heading);
                return E(heading).Length == 1;
            })).ToArray();
        var boundaries = rectangles.ToDictionary(occurrence => occurrence, occurrence => E(occurrence.Segments).ToHashSet(N.Participants.Instance));
        var chambers = rectangles.Where(first => rectangles.Any(second => !ReferenceEquals(first, second) &&
            boundaries[first].Overlaps(boundaries[second]))).ToArray();
        ChamberFocus = S(chambers.Select(occurrence => (IElement)occurrence.Read));
        AlphabetFocus = S(Motifs.Families.Where(family => family.Kind == "cycle" && family.SegmentCount is 3 or 4 ||
                family.Kind == "junction" && family.SegmentCount == 8)
            .Select(family => (IElement)family.Read));
        var coordinateMap = new Dictionary<IElement, (int Column, int Row)>(N.Participants.Instance);
        foreach (var chamber in chambers)
        {
            var footprint = E(chamber.Sites).Select(site => publishedPositions[site]).ToArray();
            coordinateMap.Add(chamber.Read, ((int)footprint.Min(p => p.X) / 8, (int)footprint.Min(p => p.Y) / 8));
        }
        ChamberCoordinates = coordinateMap;
        AuditSurface = new(new Relation(SourceFrame.Surface, new Relation(SourceTopology, SourceBindings)),
            new Relation(Publisher.Surface, new Relation(Motifs.Surface, new Relation(OrdinalPublisher.Surface, new Relation(ChamberFocus, AlphabetFocus)))));

        IElement Site(int x, int y)
        { var site = new Placeholder(); sites.Add(site); positions.Add(site, new(x, y)); return site; }
        void Edge(IElement from, IElement to) => edges.Add(new(new Placeholder(), new Relation(from, to)));
        void Cycle(params (int X, int Y)[] points)
        {
            var loop = points.Select(point => Site(point.X, point.Y)).ToArray();
            for (var i = 0; i < loop.Length; i++) Edge(loop[i], loop[(i + 1) % loop.Length]);
        }
        void Unit(int x, int y, int kind)
        {
            switch (kind)
            {
                case 0: Cycle((x + 2, y), (x, y + 2), (x - 2, y), (x, y - 2)); break;
                case 1: Cycle((x - 2, y - 2), (x + 2, y - 2), (x + 2, y + 2), (x - 2, y + 2)); break;
                case 2: Cycle((x - 2, y - 2), (x + 2, y - 2), (x, y + 2)); break;
                case 3:
                    var centre = Site(x, y);
                    foreach (var point in new[] { (2, 0), (2, 2), (0, 2), (-2, 2), (-2, 0), (-2, -2), (0, -2), (2, -2) })
                        Edge(centre, Site(x + point.Item1, y + point.Item2));
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(recipe));
            }
        }
        Relation Word(QualitativeRecognitionPoint point) => S(
            Enumerable.Range(0, (int)Math.Abs(point.X)).Select(_ => (IElement)QualitativeMotionFrame.Move(point.X < 0 ? SourceFrame.Left : SourceFrame.Right, Reach))
                .Concat(Enumerable.Range(0, (int)Math.Abs(point.Y)).Select(_ => (IElement)QualitativeMotionFrame.Move(point.Y < 0 ? SourceFrame.Down : SourceFrame.Up, Reach))));
    }
    private static IElement[] E(Relation scope) => N.E(scope);
    private static Relation S(IEnumerable<IElement> entries) => N.S(entries.ToArray());
}
