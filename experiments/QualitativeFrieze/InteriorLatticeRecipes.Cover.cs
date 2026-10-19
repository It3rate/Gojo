namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class InteriorLatticeRecipes
{
    private sealed record Placement(int[] Covered, Cell[] Cells, P Anchor, int Kind, uint Priority);

    /// <summary>A bounded authoring search for exclusive whole tiles on a
    /// periodic atomic patch. The solver is not the Gojo execution law: its
    /// result is compiled to inspectable face ownership and separately read.</summary>
    private static Tile[] Cover(int width, int height, Cell[][] shapes, int seed, bool triangular = false, bool hexagonal = false)
    {
        var halves = triangular ? 2 : 1;
        var total = width * height * halves;
        int Index(Cell cell) => (Mod(cell.Y, height) * width + Mod(cell.X, width)) * halves + (triangular ? cell.Half : 0);
        var placements = new List<Placement>(); var keys = new HashSet<string>();
        for (var kind = 0; kind < shapes.Length; kind++)
            foreach (var shape in Orientations(shapes[kind], triangular, hexagonal))
                for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                {
                    var cells = shape.Select(cell => cell with { X = cell.X + x, Y = cell.Y + y }).ToArray();
                    var indices = cells.Select(Index).Order().ToArray();
                    if (indices.Distinct().Count() != cells.Length) continue;
                    var key = string.Join(',', indices);
                    if (!keys.Add(key)) continue;
                    var hash = Mix((uint)seed ^ (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(kind * 83492791));
                    foreach (var cell in shape) hash = Mix(hash ^ (uint)(cell.X * 31 + cell.Y * 131 + cell.Half * 503));
                    // Whole larger rooms are preferred, but their relative
                    // placement/orientation varies with the bounded seed.
                    placements.Add(new(indices, cells, new(x, y), kind, hash));
                }
        var byCell = Enumerable.Range(0, total).Select(_ => new List<int>()).ToArray();
        var order = Enumerable.Range(0, placements.Count).OrderBy(index => placements[index].Kind)
            .ThenBy(index => placements[index].Priority).ToArray();
        foreach (var index in order) foreach (var cell in placements[index].Covered) byCell[cell].Add(index);
        var occupied = new bool[total]; var chosen = new List<int>(); var calls = 0;
        if (!Search(0)) throw new InvalidOperationException($"No bounded periodic exact cover for {width}x{height}, seed {seed}, palette {string.Join('/', shapes.Select(shape => shape.Length))}; {calls} states.");
        return chosen.Select((index, template) => new Tile(template, placements[index].Anchor, placements[index].Cells)).ToArray();

        bool Compatible(int index) => placements[index].Covered.All(cell => !occupied[cell]);
        bool Search(int filled)
        {
            if (filled == total) return true;
            if (++calls > 600_000) return false;
            List<int>? possible = null;
            // Minimum remaining participation is a host compilation shortcut.
            // No index, cost or random seed survives as canonical cell meaning.
            for (var cell = 0; cell < total; cell++)
            {
                if (occupied[cell]) continue;
                var candidates = byCell[cell].Where(Compatible).ToList();
                if (candidates.Count == 0) return false;
                if (possible is null || candidates.Count < possible.Count) possible = candidates;
                if (possible.Count == 1) break;
            }
            foreach (var index in possible!)
            {
                var candidate = placements[index];
                foreach (var cell in candidate.Covered) occupied[cell] = true;
                chosen.Add(index);
                if (Search(filled + candidate.Covered.Length)) return true;
                chosen.RemoveAt(chosen.Count - 1);
                foreach (var cell in candidate.Covered) occupied[cell] = false;
            }
            return false;
        }
    }

    private static IEnumerable<Cell[]> Orientations(Cell[] source, bool triangular, bool hexagonal)
    {
        var seen = new HashSet<string>();
        var turns = triangular || hexagonal ? 6 : 4;
        foreach (var reflected in new[] { false, true }) for (var turn = 0; turn < turns; turn++)
        {
            P Transform(P original)
            {
                var point = reflected ? triangular || hexagonal ? new P(original.Y, original.X) : new P(-original.X, original.Y) : original;
                for (var i = 0; i < turn; i++) point = triangular || hexagonal ? new(-point.Y, point.X + point.Y) : new(-point.Y, point.X);
                return point;
            }
            Cell[] cells;
            if (triangular)
                cells = source.Select(cell => Triangle(Vertices(cell).Select(Transform).ToArray())).ToArray();
            else
                cells = source.Select(cell => { var point = Transform(new(cell.X, cell.Y)); return new Cell(point.X, point.Y); }).ToArray();
            var left = cells.Min(cell => cell.X); var bottom = cells.Min(cell => cell.Y);
            cells = cells.Select(cell => cell with { X = cell.X - left, Y = cell.Y - bottom }).OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ThenBy(cell => cell.Half).ToArray();
            var key = string.Join(';', cells.Select(cell => $"{cell.X},{cell.Y},{cell.Half}"));
            if (seen.Add(key)) yield return cells;
        }
    }

    private static uint Mix(uint value)
    {
        value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; return value ^ (value >> 16);
    }
    private static int Mod(int value, int period) => (value % period + period) % period;
}
