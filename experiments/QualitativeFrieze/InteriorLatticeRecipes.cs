using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A rung whose meaning is the actual atomic cell complex, exclusive
/// claims, positions and connected transition, rather than its display name.</summary>
public sealed record InteriorLatticeStage(string Name, string Description,
    Relation Complex, Relation Groups, Relation Bindings, Relation Transition, Relation Surface);

public sealed record InteriorLatticeCase(string Id, string Name, string Family, string Description,
    QualitativeMotionFrame Frame, IElement Reach, Relation Column, Relation Row, Relation Periodicity,
    BourgoinPatternLibrary.DisplayBasis DisplayBasis, IReadOnlyList<InteriorLatticeStage> Stages)
{
    public InteriorLatticeStage Current => Stages[^1];
    /// <summary>Independent desired claims and the explicit contested choices;
    /// absent for the ordinary separator-withdrawal families.</summary>
    public Relation? SeamOffers { get; init; }
}

/// <summary>Compiles small periodic area partitions into native shared-edge
/// complexes. Host coordinates choose authoring occurrences; the output keeps
/// every site, ordered edge, oriented face, group claim, period and translation
/// correspondence connected. No carrier or floating-point geometry enters the
/// construction. DisplayBasis is only a presentation of the declared frame.</summary>
public sealed partial class InteriorLatticeRecipes
{
    public IReadOnlyList<InteriorLatticeCase> Cases { get; }
    private readonly int columns;
    private readonly int rows;

    public InteriorLatticeRecipes(int columns = 3, int rows = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        this.columns = columns; this.rows = rows;
        var output = new List<InteriorLatticeCase>();
        foreach (var spec in Specifications())
            output.Add(Compile($"I{output.Count + 1:000}", spec));
        Cases = output.AsReadOnly();
    }

    private readonly record struct P(int X, int Y)
    {
        public static P operator +(P a, P b) => new(a.X + b.X, a.Y + b.Y);
        public static P operator -(P a, P b) => new(a.X - b.X, a.Y - b.Y);
    }
    private readonly record struct Cell(int X, int Y, int Half = -1);
    private sealed record Tile(int Template, P Translation, Cell[] Cells);
    private sealed record Spec(string Name, string Family, string Description, int Width, int Height,
        Tile[] Tiles, bool Triangular = false, bool Polyhex = false,
        int ScaleX = 1, int ScaleY = 1, int Shear = 0, bool Negotiate = false);
    private sealed record ActualTile(int Template, P Translation, Cell[] Cells);

    private InteriorLatticeCase Compile(string id, Spec spec)
    {
        var author = new LatticeLadderAuthor();
        P Map(P point)
        {
            return new(spec.ScaleX * point.X + spec.Shear * point.Y, spec.ScaleY * point.Y);
        }
        P MapPeriod(P point) => Map(spec.Polyhex ? new(point.X - point.Y, point.X + 2 * point.Y) : point);
        var actual = new List<ActualTile>();
        // Whole occurrences are retained at the finite frontier. A tile that
        // crosses the chosen period is never sliced into a misleading fragment.
        for (var r = 0; r < rows; r++) for (var c = 0; c < columns; c++)
            foreach (var tile in spec.Tiles)
            {
                var shift = new P(c * spec.Width, r * spec.Height);
                var cells = tile.Cells.Select(cell => new Cell(cell.X + shift.X, cell.Y + shift.Y, cell.Half)).ToArray();
                if (spec.Polyhex) cells = cells.SelectMany(HexagonTriangles).ToArray();
                actual.Add(new(tile.Template, MapPeriod(tile.Translation + shift), cells));
            }

        var sites = new Dictionary<P, IElement>();
        var edges = new Dictionary<(P A, P B), Relation>();
        var atoms = new Dictionary<Cell, Relation>();
        var groups = new List<Relation>();
        var tileAtoms = new List<(ActualTile Tile, Relation Group)>();
        IElement Site(P point)
        {
            if (!sites.TryGetValue(point, out var site)) sites.Add(point, site = new Placeholder());
            return site;
        }
        Relation Face(Cell cell)
        {
            if (atoms.TryGetValue(cell, out var prior))
                throw new InvalidOperationException(id + ": two supplied tiles claim the same atomic face.");
            var corners = Vertices(cell).Select(Map).ToArray();
            var darts = new List<IElement>();
            for (var i = 0; i < corners.Length; i++)
            {
                var from = corners[i]; var to = corners[(i + 1) % corners.Length];
                var key = Compare(from, to) < 0 ? (from, to) : (to, from);
                if (!edges.TryGetValue(key, out var edge))
                {
                    edge = new(new Placeholder(), new Relation(Site(key.Item1), Site(key.Item2)));
                    edges.Add(key, edge);
                }
                darts.Add(new Relation(edge.Left, new Relation(Site(from), Site(to))));
            }
            var face = new Relation(new Placeholder(), S(darts.ToArray())); atoms.Add(cell, face); return face;
        }
        foreach (var tile in actual)
        {
            var group = new Relation(new Placeholder(), S(tile.Cells.Select(cell => (IElement)Face(cell)).ToArray()));
            groups.Add(group); tileAtoms.Add((tile, group));
        }
        var complex = new Relation(S(sites.Values.ToArray()), new Relation(S(edges.Values.Cast<IElement>().ToArray()), S(atoms.Values.Cast<IElement>().ToArray())));
        var bindings = S(sites.Select(entry => (IElement)new Relation(entry.Value, author.Word(entry.Key.X, entry.Key.Y))).ToArray());
        var final = S(groups.Cast<IElement>().ToArray());
        var atomic = S(atoms.Values.Select(face => (IElement)new Relation(new Placeholder(), S(face))).ToArray());
        var intermediate = S(groups.SelectMany(group => PartialClaims(group)).Cast<IElement>().ToArray());
        var stages = new List<InteriorLatticeStage>();
        Append("Atomic shared-edge field", "The small cells cover the supplied finite patch once each. Actual adjacent faces traverse one shared edge identity in opposite directions.", atomic, S());
        Append("Release selected repeated dividers", "A connected portion of every repeated cell is merged first. The other atomic faces remain separate; withdrawn internal edges remain in the transition witness.", intermediate,
            Transition(complex, atomic, intermediate));
        Append("Complete the repeated interior shapes", "Remaining internal separators are withdrawn. Each complete tile owns its actual atomic faces exclusively; neighbouring interiors share their common boundary rather than independently drawing overlapping outlines.", final,
            Transition(complex, intermediate, final));

        var periodX = MapPeriod(new(spec.Width, 0)); var periodY = MapPeriod(new(0, spec.Height));
        var column = author.Word(periodX.X, periodX.Y); var row = author.Word(periodY.X, periodY.Y);
        var representatives = new Dictionary<int, (ActualTile Tile, Relation Group)>();
        var instances = new List<IElement>();
        foreach (var instance in tileAtoms)
        {
            if (!representatives.TryGetValue(instance.Tile.Template, out var canonical)) representatives.Add(instance.Tile.Template, canonical = instance);
            var shift = instance.Tile.Translation - canonical.Tile.Translation;
            var word = author.Word(shift.X, shift.Y);
            var from = E(canonical.Group.Right).Cast<Relation>().ToArray();
            var to = E(instance.Group.Right).Cast<Relation>().ToArray();
            var correspondence = S(to.Zip(from, (current, basis) => (IElement)new Relation(current, basis)).ToArray());
            instances.Add(new Relation(instance.Group, new Relation(canonical.Group, new Relation(word, correspondence))));
        }
        var periodicity = new Relation(new Relation(column, row), S(instances.ToArray()));
        var display = spec.Triangular || spec.Polyhex ? new BourgoinPatternLibrary.DisplayBasis(1, 0, .5, Math.Sqrt(3) / 2)
            : new BourgoinPatternLibrary.DisplayBasis(1, 0, 0, 1);
        var result = new InteriorLatticeCase(id, spec.Name, spec.Family, spec.Description, author.Frame, author.Reach,
            column, row, periodicity, display, stages.AsReadOnly());
        return spec.Negotiate ? Negotiate(result, spec, actual, atoms, groups, author) : result;

        void Append(string name, string description, Relation claims, Relation transition)
        {
            var surface = new Relation(new Relation(complex, claims), new Relation(bindings, transition));
            stages.Add(new(name, description, complex, claims, bindings, transition, surface));
        }
    }

    private static IEnumerable<Relation> PartialClaims(Relation group)
    {
        var faces = E(group.Right).Cast<Relation>().ToArray();
        if (faces.Length <= 2) return faces.Select(face => new Relation(new Placeholder(), S(face)));
        // A breadth-first connected prefix is a legal intermediate room. The
        // complement deliberately stays atomic, so no disconnected remainder
        // is hidden inside one group wrapper.
        var chosen = new List<Relation> { faces[0] }; var remaining = faces.Skip(1).ToList();
        while (chosen.Count < Math.Max(2, faces.Length / 2))
        {
            var index = remaining.FindIndex(face => chosen.Any(prior => ShareEdge(prior, face)));
            if (index < 0) throw new InvalidOperationException("Authored tile atoms must be edge-connected.");
            chosen.Add(remaining[index]); remaining.RemoveAt(index);
        }
        return new[] { new Relation(new Placeholder(), S(chosen.Cast<IElement>().ToArray())) }
            .Concat(remaining.Select(face => new Relation(new Placeholder(), S(face))));
    }

    private static Relation Transition(Relation complex, Relation previous, Relation next)
    {
        var before = E(previous).Cast<Relation>().ToArray(); var after = E(next).Cast<Relation>().ToArray();
        var oldOwner = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var newOwner = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        foreach (var group in before) foreach (var face in E(group.Right)) oldOwner.Add(face, group);
        foreach (var group in after) foreach (var face in E(group.Right)) newOwner.Add(face, group);
        var edgeFaces = new Dictionary<IElement, List<IElement>>(ReferenceEqualityComparer.Instance);
        foreach (var face in E(((Relation)complex.Right).Right).Cast<Relation>())
            foreach (var dart in E(face.Right).Cast<Relation>())
            {
                if (!edgeFaces.TryGetValue(dart.Left, out var incident)) edgeFaces.Add(dart.Left, incident = []);
                incident.Add(face);
            }
        var removed = new List<IElement>(); var retained = new List<IElement>();
        foreach (var edge in E(((Relation)complex.Right).Left).Cast<Relation>())
        {
            var incident = edgeFaces[edge.Left];
            var nowInternal = incident.Count == 2 && ReferenceEquals(newOwner[incident[0]], newOwner[incident[1]]);
            var wasInternal = incident.Count == 2 && ReferenceEquals(oldOwner[incident[0]], oldOwner[incident[1]]);
            if (nowInternal && !wasInternal) removed.Add(edge); else retained.Add(edge);
        }
        var correspondences = S(after.SelectMany(group => E(group.Right).Select(face => (IElement)new Relation(new Relation(oldOwner[face], face), group))).ToArray());
        return new(new Relation(previous, S(removed.ToArray())), new Relation(next, new Relation(S(retained.ToArray()), correspondences)));
    }

    private static bool ShareEdge(Relation a, Relation b) => E(a.Right).Cast<Relation>().Any(x => E(b.Right).Cast<Relation>().Any(y => ReferenceEquals(x.Left, y.Left)));
    private static int Compare(P a, P b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
    private static P[] Vertices(Cell cell) => cell.Half switch
    {
        0 => [new(cell.X, cell.Y), new(cell.X, cell.Y + 1), new(cell.X + 1, cell.Y)],
        1 => [new(cell.X + 1, cell.Y + 1), new(cell.X + 1, cell.Y), new(cell.X, cell.Y + 1)],
        _ => [new(cell.X, cell.Y), new(cell.X, cell.Y + 1), new(cell.X + 1, cell.Y + 1), new(cell.X + 1, cell.Y)]
    };
    private static Cell[] HexagonTriangles(Cell center)
    {
        var point = new P(center.X - center.Y, center.X + 2 * center.Y);
        var directions = new P[] { new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1), new(1, -1) };
        return directions.Select((direction, i) => Triangle(point, point + direction, point + directions[(i + 1) % 6])).ToArray();
    }
    private static Cell Triangle(params P[] points)
    {
        var x = points.Min(point => point.X); var y = points.Min(point => point.Y);
        return new(x, y, points.Contains(new P(x, y)) ? 0 : 1);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(IElement scope) => FiniteLaw.Entries((Relation)scope);
}
