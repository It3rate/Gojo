using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authoring and evaluation harness. All recognition receives only the
/// published observation. Fixture names, coordinate words, rendering positions
/// and source correspondence remain outside that observation.</summary>
public sealed class QualitativeRecognitionStudy
{
    public IReadOnlyList<QualitativeRecognitionCase> Cases { get; }
    public Relation Surface { get; }

    public QualitativeRecognitionStudy(Action<string>? progress = null)
    {
        var cases = new List<QualitativeRecognitionCase>();
        Add("R01", "Square grid", "Two independent straight heading families; short corners, repeated quadrilaterals and larger perimeter cycles coexist.",
            builder => builder.Grid(4, 3, (x, y) => (2 * x, 2 * y)));
        Add("R02", "Sheared grid", "The same incidence arrangement uses a rightward and an upward-rightward family. No angle or equal-length claim is published.",
            builder => builder.Grid(4, 3, (x, y) => (2 * x + y, 2 * y)));
        Add("R03", "Diamond lattice", "Two diagonal transport families make a grid while small closed diamond-like readings repeat.",
            builder => builder.Grid(4, 3, (x, y) => (2 * x + 2 * y, 2 * x - 2 * y)));
        Add("R04", "Triangular lattice", "A three-family network retains two-family grid readings with actual unused diagonals and triangle circuits, alongside the three-family interpretation.",
            builder => builder.Grid(4, 3, (x, y) => (2 * x + y, 2 * y), diagonals: true));
        Add("R05", "Alternating offset rows", "Transport between successive rows alternates direction. A single straight family leaves participation outside its basis; a two-phase channel may explain more.",
            builder => builder.Grid(4, 4, (x, y) => (2 * x + y % 2, 2 * y)));
        Add("R06", "Zigzag strips", "Three separate repeating walks have alternating diagonal headings, no supplied faces and no actual closed cycles.",
            builder =>
            {
                for (var row = 0; row < 3; row++)
                {
                    var sites = Enumerable.Range(0, 13).Select(index => builder.Site(index, row * 4 + index % 2)).ToArray();
                    for (var i = 1; i < sites.Length; i++) builder.Edge(sites[i - 1], sites[i]);
                }
            });
        Add("R07", "Hexagonal mesh", "Three local heading families form six-edge cycles and branching channels. A coarse three-family reading need not establish a triangular grid.",
            builder =>
            {
                var vertices = new (int X, int Y)[] { (2, 0), (1, 2), (-1, 2), (-2, 0), (-1, -2), (1, -2) };
                for (var column = 0; column < 3; column++) for (var row = 0; row < 3; row++)
                {
                    var cycle = vertices.Select(point => builder.SharedSite(3 * column + point.X,
                        4 * row + 2 * (column % 2) + point.Y)).ToArray();
                    for (var i = 0; i < cycle.Length; i++) builder.Edge(cycle[i], cycle[(i + 1) % cycle.Length]);
                }
            });
        Add("R08", "Grid and isolated diamonds", "A large grid and twelve small closed diamonds occupy the same field. Their native components remain distinct while several motif and disposition readings coexist.",
            builder =>
            {
                builder.Grid(4, 3, (x, y) => (4 * x, 4 * y));
                for (var x = 0; x < 4; x++) for (var y = 0; y < 3; y++)
                {
                    var cx = 4 * x + 2; var cy = 4 * y + 2;
                    var cycle = new[] { builder.Site(cx + 1, cy), builder.Site(cx, cy + 1),
                        builder.Site(cx - 1, cy), builder.Site(cx, cy - 1) };
                    for (var i = 0; i < cycle.Length; i++) builder.Edge(cycle[i], cycle[(i + 1) % cycle.Length]);
                }
            });
        Add("R09", "Two local organizations", "A square patch and a triangular patch share no sites. Recurring fragments can generalize across components while a single global arrangement leaves evidence outside its basis.",
            builder =>
            {
                builder.Grid(3, 3, (x, y) => (2 * x, 2 * y));
                builder.Grid(3, 3, (x, y) => (10 + 2 * x + y, 2 * y), diagonals: true);
            });
        Add("R10", "Dense bounded search", "A larger triangular field deliberately exceeds the selected motif search budget. Supported occurrences survive beside an explicit unsearched scope.",
            builder => builder.Grid(10, 9, (x, y) => (2 * x + y, 2 * y), diagonals: true), stress: true);
        Cases = cases;
        Surface = N.S(cases.Select(item => (IElement)item.Surface).ToArray());

        void Add(string id, string name, string description, Action<SourceBuilder> build, bool stress = false)
        {
            progress?.Invoke($"{id}: publishing {name}");
            var source = new SourceBuilder(); build(source);
            var topology = new Relation(N.S(source.Sites.ToArray()), new Relation(N.S(source.Edges.ToArray()), N.S()));
            var bindings = N.S(source.Sites.Select(site => (IElement)new Relation(site, source.Word(source.Coordinates[site]))).ToArray());
            var publisher = new QualitativeRecognitionPublisher(source.Frame, topology, bindings);
            var positions = new Dictionary<IElement, QualitativeRecognitionPoint>(N.Participants.Instance);
            foreach (var site in source.Sites)
                if (publisher.TryMapSite(site, out var observed))
                { var point = source.Coordinates[site]; positions.Add(observed, new(point.X, point.Y)); }
            progress?.Invoke($"{id}: inducing local motifs");
            var motifs = new QualitativeMotifInduction(publisher.Observation, maxCycleEdges: 6,
                proposalCapacity: stress ? 5000 : 20000, retainedCapacity: stress ? 500 : 3000);
            progress?.Invoke($"{id}: reading disposition alternatives");
            var disposition = new QualitativeDispositionInduction(publisher.Observation);
            cases.Add(new(id, name, description, source.Frame, topology, bindings, publisher, positions, motifs, disposition));
        }
    }

    private sealed class SourceBuilder
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public List<IElement> Sites { get; } = [];
        public List<Relation> Edges { get; } = [];
        public Dictionary<IElement, (int X, int Y)> Coordinates { get; } = new(N.Participants.Instance);
        private readonly Dictionary<(int X, int Y), IElement> shared = [];
        public IElement Site(int x, int y)
        { var site = new Placeholder(); Sites.Add(site); Coordinates.Add(site, (x, y)); return site; }
        public IElement SharedSite(int x, int y)
        {
            if (shared.TryGetValue((x, y), out var existing)) return existing;
            var site = Site(x, y); shared.Add((x, y), site); return site;
        }
        public void Edge(IElement from, IElement to)
        {
            if (N.Same(from, to)) return;
            if (Edges.Any(edge => edge.Right is Relation ends &&
                (N.Same(ends.Left, from) && N.Same(ends.Right, to) || N.Same(ends.Left, to) && N.Same(ends.Right, from)))) return;
            Edges.Add(new(new Placeholder(), new Relation(from, to)));
        }
        public void Grid(int columns, int rows, Func<int, int, (int X, int Y)> placement, bool diagonals = false)
        {
            var sites = new IElement[columns + 1, rows + 1];
            for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++)
            { var point = placement(x, y); sites[x, y] = Site(point.X, point.Y); }
            for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++)
            {
                if (x < columns) Edge(sites[x, y], sites[x + 1, y]);
                if (y < rows) Edge(sites[x, y], sites[x, y + 1]);
                if (diagonals && x < columns && y < rows) Edge(sites[x + 1, y], sites[x, y + 1]);
            }
        }
        public Relation Word((int X, int Y) point)
        {
            var moves = new List<IElement>();
            for (var i = 0; i < Math.Abs(point.X); i++) moves.Add(QualitativeMotionFrame.Move(point.X < 0 ? Frame.Left : Frame.Right, Reach));
            for (var i = 0; i < Math.Abs(point.Y); i++) moves.Add(QualitativeMotionFrame.Move(point.Y < 0 ? Frame.Down : Frame.Up, Reach));
            return N.S(moves.ToArray());
        }
    }
}

/// <summary>A presentation projection, intentionally outside the recognition
/// observation and its native read.</summary>
public sealed record QualitativeRecognitionPoint(double X, double Y);

public sealed class QualitativeRecognitionCase(string id, string name, string description,
    QualitativeMotionFrame sourceFrame, Relation sourceTopology, Relation sourceBindings,
    QualitativeRecognitionPublisher publisher, IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> positions,
    QualitativeMotifInduction motifs, QualitativeDispositionInduction disposition)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public QualitativeMotionFrame SourceFrame { get; } = sourceFrame;
    public Relation SourceTopology { get; } = sourceTopology;
    public Relation SourceBindings { get; } = sourceBindings;
    public QualitativeRecognitionPublisher Publisher { get; } = publisher;
    public QualitativeRecognitionObservation Observation => Publisher.Observation;
    public IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> Positions { get; } = positions;
    public QualitativeMotifInduction Motifs { get; } = motifs;
    public QualitativeDispositionInduction Disposition { get; } = disposition;
    public QualitativeRecognitionReadings Readings { get; } = new(motifs, disposition);
    private Relation? surface;
    public Relation Surface => surface ??= new(new Relation(Publisher.Surface, Motifs.Surface), new Relation(Disposition.Surface, Readings.Surface));
}
