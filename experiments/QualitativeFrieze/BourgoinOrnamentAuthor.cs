using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite authoring tools. Host coordinates only select native words;
/// installed additions retain actual source ports, offsets, prototypes,
/// substitution receipts and equality witnesses.</summary>
internal sealed class BourgoinOrnamentAuthor
{
    internal sealed class Layer
    {
        public Placeholder Identity { get; } = new();
        private readonly List<Relation> selected = [], additions = [], attachments = [];
        public void Select(params Relation[] paths)
        {
            foreach (var path in paths)
                if (!selected.Any(actual => ReferenceEquals(actual, path))) selected.Add(path);
        }
        public void SelectOnly(params Relation[] paths)
        {
            selected.Clear();
            Select(paths);
        }
        public void Add(UnitArrangementProgram program, Relation attachment)
        { additions.Add(program.Surface); attachments.Add(attachment); }
        public Relation Definition => OrnamentEnrichment.Layer(Identity, N.S(selected.ToArray()),
            N.S(additions.ToArray()), N.S(attachments.ToArray()));
    }

    internal sealed record PathRead(Relation Path, bool Closed, (int X, int Y)[] Vertices)
    {
        public int MinX => Vertices.Min(point => point.X);
        public int MinY => Vertices.Min(point => point.Y);
        public int Width => Vertices.Max(point => point.X) - MinX;
        public int Height => Vertices.Max(point => point.Y) - MinY;
        public (int X, int Y) Centre => (MinX + Width / 2, MinY + Height / 2);
    }

    private readonly BourgoinPatternLibrary.Recipe recipe;
    private readonly IElement reach;
    private readonly UnitArrangementProgram source;
    private readonly Dictionary<string, UnitArrangementProgram> prototypes = new();
    private readonly Dictionary<Relation, (int X, int Y)> portPositions = new(ReferenceEqualityComparer.Instance);
    public QualitativeMotionFrame Frame => source.Frame;
    public PathRead[] Paths { get; }
    public Layer Field { get; } = new();
    public Layer Motif { get; } = new();
    public Layer Detail { get; } = new();

    public BourgoinOrnamentAuthor(BourgoinPatternLibrary.Recipe recipe)
    {
        this.recipe = recipe;
        var actual = (Relation)N.E(recipe.Motifs).Single();
        if (!ReferenceEquals(actual.Left, recipe.Frame.Surface) ||
            actual.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                Right: Relation { Left: Relation closed, Right: var evidence } })
            throw new ArgumentException("Ornament authoring requires one complete source motif.", nameof(recipe));
        source = new(recipe.Frame, "retained ornament source", paths, ports, closed, evidence);
        if (!source.Complete()) throw new ArgumentException("Ornament authoring requires complete source paths and public ports.", nameof(recipe));
        var actualPaths = N.E(paths).Cast<Relation>().ToArray();
        var moves = actualPaths.SelectMany(path => N.E((Relation)path.Left)
            .Concat(N.E((Relation)path.Right).Cast<Relation>().SelectMany(N.E))).Cast<Relation>().ToArray();
        var reaches = moves.Select(move => move.Right).Distinct(N.Participants.Instance).ToArray();
        if (reaches.Length != 1) throw new ArgumentException("This author uses one declared source reach.", nameof(recipe));
        reach = reaches[0];
        Paths = actualPaths.Select(path =>
        {
            var cursor = Index((Relation)path.Left); var points = new List<(int X, int Y)> { cursor };
            foreach (var word in N.E((Relation)path.Right).Cast<Relation>())
            { var delta = Index(word); cursor = (cursor.X + delta.X, cursor.Y + delta.Y); points.Add(cursor); }
            return new PathRead(path, N.E(closed).Any(item => ReferenceEquals(item, path)), points.ToArray());
        }).ToArray();
        foreach (var port in N.E(source.Ports).Cast<Relation>())
        {
            if (!OrnamentEnrichment.TryAnchorWord(source, port, out var position, out _))
                throw new InvalidOperationException("A source port lacks its actual prefix read.");
            portPositions.Add(port, Index(position));
        }
        Field.Select(actualPaths);
        Motif.Select(Paths.Where(path => path.Closed).Select(path => path.Path).ToArray());
        Detail.Select(Paths.Where(path => !path.Closed).Select(path => path.Path).ToArray());
    }

    public Relation NearestPort((int X, int Y) point, Relation? actualPath = null) => portPositions
        .Where(row => actualPath is null || row.Key.Right is Relation selector && ReferenceEquals(selector.Left, actualPath))
        .MinBy(row => Math.Abs((long)row.Value.X - point.X) + Math.Abs((long)row.Value.Y - point.Y)).Key;

    public Relation Port(PathRead path, int index)
    {
        var words = N.E((Relation)path.Path.Right);
        var selected = index == words.Length ? path.Path : words[index];
        return portPositions.Keys.Single(port => port.Right is Relation selector &&
            ReferenceEquals(selector.Left, path.Path) && ReferenceEquals(selector.Right, selected));
    }

    public (int X, int Y) Position(Relation port) => portPositions[port];

    public UnitArrangementProgram Prototype(bool closes, params (int X, int Y)[] vertices)
    {
        if (vertices.Length < 2 || vertices.Zip(vertices.Skip(1)).Any(pair => pair.First == pair.Second))
            throw new ArgumentException("A finite ornament path needs distinct successive sites.", nameof(vertices));
        var key = (closes ? "C:" : "O:") + string.Join(";", vertices.Select(point => $"{point.X},{point.Y}"));
        if (prototypes.TryGetValue(key, out var retained)) return retained;
        var words = Enumerable.Range(0, vertices.Length - 1).Select(index => Word(
            (vertices[index + 1].X - vertices[index].X, vertices[index + 1].Y - vertices[index].Y))).ToArray();
        var path = new Relation(Word(vertices[0]), N.S(words));
        var program = new UnitArrangementProgram(Frame, "finite shared ornament prototype", N.S(path), Ports(path, words), closes ? N.S(path) : N.S());
        prototypes.Add(key, program);
        return program;
    }

    public QualitativePatternTransform Map((int X, int Y) horizontal, (int X, int Y) vertical)
    {
        Relation Row(IElement direction, (int X, int Y) vector) =>
            new(QualitativeMotionFrame.Move(direction, reach), Word(vector));
        return new(Frame, Frame, N.S(Row(Frame.Right, horizontal), Row(Frame.Left, (-horizontal.X, -horizontal.Y)),
            Row(Frame.Up, vertical), Row(Frame.Down, (-vertical.X, -vertical.Y))));
    }

    public void Place(Layer layer, UnitArrangementProgram prototype, (int X, int Y) at,
        Relation? anchor = null, QualitativePatternTransform? transform = null)
    {
        anchor ??= NearestPort(at);
        var mapped = prototype.Transform("witnessed ornament image", transform ?? Map((1, 0), (0, 1)));
        var mappedPath = (Relation)N.E(mapped.Paths).Single();
        if (!OrnamentEnrichment.TryAnchorWord(source, anchor, out var anchorWord, out var anchorRead))
            throw new ArgumentException("An addition must use an actual source port.", nameof(anchor));
        var anchorPosition = portPositions[anchor];
        var offset = Word((at.X - anchorPosition.X, at.Y - anchorPosition.Y));
        var offsetRead = Frame.Reduce(offset, (Relation)mappedPath.Left);
        if (!Frame.TryReadResult(offsetRead, out var fromAnchor, out _, out var pending) || N.E(pending).Length != 0)
            throw new InvalidOperationException("A finite addition offset did not settle.");
        var placement = Frame.Reduce(anchorWord, fromAnchor);
        if (!Frame.TryReadResult(placement, out var start, out _, out pending) || N.E(pending).Length != 0)
            throw new InvalidOperationException("A finite addition lacks its source landmark placement.");
        var words = N.E((Relation)mappedPath.Right).Cast<Relation>().ToArray();
        var path = new Relation(start, mappedPath.Right);
        var evidence = new Relation(mapped.Surface, new Relation(new Relation(mappedPath, path),
            new Relation(anchor, new Relation(anchorRead, new Relation(offsetRead, placement)))));
        var added = new UnitArrangementProgram(Frame, "attached native ornament", N.S(path), Ports(path, words),
            N.E(mapped.ClosedPaths).Length == 0 ? N.S() : N.S(path), evidence);
        layer.Add(added, OrnamentEnrichment.Attach(anchor, fromAnchor, path));
    }

    public void Polygon(Layer layer, (int X, int Y) at, params (int X, int Y)[] vertices) =>
        Place(layer, Prototype(true, [.. vertices, vertices[0]]), at);

    public void Segment(Layer layer, (int X, int Y) first, (int X, int Y) last, Relation? anchor = null)
    {
        if (first != last) Place(layer, Prototype(false, (0, 0), (last.X - first.X, last.Y - first.Y)), first, anchor);
    }

    public OrnamentEnrichment Finish()
    {
        var enrichment = new OrnamentEnrichment(Frame, (Relation)N.E(recipe.Motifs).Single(),
            N.S(Field.Definition, Motif.Definition, Detail.Definition));
        if (!enrichment.IsComplete || Paths.Any(path => !N.E(enrichment.Result.Paths).Any(item => ReferenceEquals(item, path.Path))))
            throw new InvalidOperationException("The ornament must preserve every source path and pass all native attachment contracts.");
        return enrichment;
    }

    private (int X, int Y) Index(Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in N.E(word).Cast<Relation>())
        {
            if (!Frame.Direction(move, out var axis, out var forward, out var actualReach) || !ReferenceEquals(actualReach, reach))
                throw new InvalidOperationException("An authoring read left its declared common-reach frame.");
            if (ReferenceEquals(axis, Frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }

    private Relation Word((int X, int Y) vector) => N.S(
        Enumerable.Range(0, Math.Abs(vector.X)).Select(_ => (IElement)QualitativeMotionFrame.Move(vector.X < 0 ? Frame.Left : Frame.Right, reach))
            .Concat(Enumerable.Range(0, Math.Abs(vector.Y)).Select(_ => (IElement)QualitativeMotionFrame.Move(vector.Y < 0 ? Frame.Down : Frame.Up, reach))).ToArray());

    private static Relation Ports(Relation path, Relation[] words) =>
        N.S(Enumerable.Range(0, words.Length + 1).Select(index => (IElement)new Relation(new Placeholder(),
            new Relation(path, index == words.Length ? path : words[index]))).ToArray());
}
