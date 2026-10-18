using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A finite authored cellular scaffold made by acknowledged edge
/// actuators. Shared sites come from accepted publication joins, never screen
/// proximity. Logical indexes construct repeated native words; only a renderer
/// may choose Euclidean lengths or an oblique interpretation of the two axes.</summary>
public sealed class OrnamentSourcePatch
{
    public sealed record FaceRecord(int CellX, int CellY, int Sector,
        Relation Face, Relation Boundary, (int X, int Y)[] LogicalVertices);

    public QualitativeMotionFrame Frame { get; } = new();
    public IElement Reach { get; } = new Placeholder();
    public int Columns { get; }
    public int Rows { get; }
    public int FineScale { get; }
    public ScaffoldQuestionScaffold Scaffold { get; }
    public QualitativeGraphJoin Join { get; }
    public Relation Bindings => Join.Bindings;
    public Relation SiteWords => Bindings;
    public UnitArrangementNode[] Units { get; }
    public FaceRecord[] FaceRecords { get; }
    public IReadOnlyDictionary<(int X, int Y), IElement> CoordinateSites { get; }
    public Relation Definition { get; }
    public Relation Surface => Definition;

    public static OrnamentSourcePatch TriangleGrid(int columns, int rows) => new(columns, rows, false);
    public static OrnamentSourcePatch SquareFans(int columns, int rows) => new(columns, rows, true);

    private OrnamentSourcePatch(int columns, int rows, bool squareFans)
    {
        if (columns < 1 || rows < 1) throw new ArgumentOutOfRangeException(nameof(columns), "A patch needs positive finite extents.");
        Columns = columns; Rows = rows; FineScale = 6;
        var plans = new List<(int X, int Y, int Sector, (int X, int Y)[] Vertices)>();
        for (var y = 0; y < rows; y++)
        for (var x = 0; x < columns; x++)
        {
            if (squareFans)
            {
                var a = (6 * x, 6 * y); var b = (6 * x + 6, 6 * y);
                var c = (6 * x + 6, 6 * y + 6); var d = (6 * x, 6 * y + 6);
                var centre = (6 * x + 3, 6 * y + 3);
                plans.Add((x, y, 0, [a, b, centre]));
                plans.Add((x, y, 1, [b, c, centre]));
                plans.Add((x, y, 2, [c, d, centre]));
                plans.Add((x, y, 3, [d, a, centre]));
            }
            else
            {
                plans.Add((x, y, 0, [(6 * x, 6 * y), (6 * x + 6, 6 * y), (6 * x, 6 * y + 6)]));
                plans.Add((x, y, 1, [(6 * x + 6, 6 * y), (6 * x + 6, 6 * y + 6), (6 * x, 6 * y + 6)]));
            }
        }

        var units = new List<UnitArrangementNode>();
        var rawStrokes = new List<IElement>(); var rawBindings = new List<IElement>();
        var declarations = new List<IElement>();
        var endpoints = new Dictionary<(int X, int Y), (IElement Site, Relation Publication)>();
        var edges = new Dictionary<((int X, int Y) A, (int X, int Y) B), Relation>();
        foreach (var plan in plans)
        for (var index = 0; index < plan.Vertices.Length; index++)
            Edge(plan.Vertices[index], plan.Vertices[(index + 1) % plan.Vertices.Length]);

        Units = units.ToArray();
        var source = new QualitativeStrokeGraph(Frame, S(rawBindings.Cast<Relation>().Select(row => row.Left).ToArray()), S(rawStrokes.ToArray()));
        Join = new(source, S(rawBindings.ToArray()), S(declarations.ToArray()));
        if (!Join.TryReadPassed(Join.Evaluate())) throw new InvalidOperationException("Declared endpoint publications must agree in the native motion frame.");
        var siteMap = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        foreach (var row in E(Join.SiteMap).Cast<Relation>()) siteMap.Add(row.Left, row.Right);
        var strokeMap = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        foreach (var row in E(Join.StrokeMap).Cast<Relation>()) strokeMap.Add(row.Left, (Relation)row.Right);
        CoordinateSites = endpoints.ToDictionary(row => row.Key, row => siteMap[row.Value.Site]);
        var boundaries = plans.Select(plan => Boundary(plan.Vertices)).ToArray();

        // Bounded faces use one orientation; the explicitly declared exterior
        // traverses their exposed boundary sides in the opposite orientation.
        var exteriorVertices = new List<(int X, int Y)>();
        for (var y = 0; y < rows; y++) exteriorVertices.Add((0, y * FineScale));
        for (var x = 0; x < columns; x++) exteriorVertices.Add((x * FineScale, rows * FineScale));
        for (var y = rows; y > 0; y--) exteriorVertices.Add((columns * FineScale, y * FineScale));
        for (var x = columns; x > 0; x--) exteriorVertices.Add((x * FineScale, 0));
        var premise = new Relation(S(Units.Select(unit => (IElement)unit.Current.Surface).ToArray()), Join.Surface);
        Scaffold = new(Join.Graph, S(boundaries), S(Boundary(exteriorVertices.ToArray())), premise);
        if (!Scaffold.IsComplete) throw new InvalidOperationException("The authored finite patch must satisfy its cellular embedding obligations.");
        var faces = E(Scaffold.Faces).Cast<Relation>().ToArray();
        FaceRecords = plans.Select((plan, index) => new FaceRecord(plan.X, plan.Y, plan.Sector,
            faces[index], boundaries[index], plan.Vertices.ToArray())).ToArray();
        Definition = new Relation(Frame.Surface, new Relation(Reach,
            new Relation(S(Units.Select(unit => (IElement)unit.Surface).ToArray()), new Relation(Join.Surface, Scaffold.Surface))));

        void Edge((int X, int Y) a, (int X, int Y) b)
        {
            var key = Key(a, b);
            if (edges.ContainsKey(key)) return;
            a = key.A; b = key.B;
            var chord = Word(b.X - a.X, b.Y - a.Y);
            var path = new Relation(S(), S(chord));
            var startRole = new Placeholder(); var endRole = new Placeholder();
            var program = new UnitArrangementProgram(Frame, "one published source edge", S(path),
                S(new Relation(startRole, new Relation(path, chord)), new Relation(endRole, new Relation(path, path))));
            var unit = new UnitArrangementNode("source edge actuator", [program], program, Word(a.X, a.Y));
            units.Add(unit);
            var stroke = E(unit.Current.Graph.Strokes).Cast<Relation>().Single();
            rawStrokes.Add(stroke); edges.Add(key, stroke);
            Register(a, startRole); Register(b, endRole);

            void Register((int X, int Y) coordinate, IElement role)
            {
                if (!UnitArrangementConnection.TryReadPublication(unit, role, out var publication, out var binding))
                    throw new InvalidOperationException("An executed source edge must expose its authenticated endpoint.");
                rawBindings.Add(binding);
                if (endpoints.TryGetValue(coordinate, out var old))
                    declarations.Add(new Relation(new Relation(binding.Left, old.Site), new Relation(publication, old.Publication)));
                else endpoints.Add(coordinate, (binding.Left, publication));
            }
        }

        Relation Boundary((int X, int Y)[] vertices) => ScaffoldQuestionScaffold.Boundary(vertices.Select((a, index) =>
        {
            var b = vertices[(index + 1) % vertices.Length];
            return ScaffoldQuestionScaffold.Oriented(strokeMap[edges[Key(a, b)]], CoordinateSites[a], CoordinateSites[b]);
        }).ToArray());
    }

    /// <summary>Repeated participation on a shared, uncalibrated reach. Signed
    /// logical indexes are authoring notation, not native measured values.</summary>
    public Relation Word(int x, int y) => S(
        Enumerable.Repeat(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, Reach), Math.Abs(x))
            .Concat(Enumerable.Repeat(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, Reach), Math.Abs(y)))
            .Cast<IElement>().ToArray());

    private static ((int X, int Y) A, (int X, int Y) B) Key((int X, int Y) a, (int X, int Y) b) =>
        a.X < b.X || a.X == b.X && a.Y < b.Y ? (a, b) : (b, a);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
