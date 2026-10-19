using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    /// <summary>Plate 9 figures 4/5 are deformed diamond compartments, not
    /// rectangular cranks with diagonal decoration added afterward. A coarse
    /// diagonal is replaced by a pointed four-chord boundary while retaining
    /// its original displacement and landmark roles.</summary>
    private static SourceRevision DiamondField(bool returned)
    {
        var cell = new Cell();
        var courses = returned ? 2 : 1;
        for (var row = 0; row < courses; row++)
        {
            var y = row * 8;
            cell.RefinedDiamond(returned, [(0, y), (4, y + 4), (8, y), (4, y - 4)]);
            cell.RefinedDiamond(returned, [(0, y), (4, y + 4), (0, y + 8), (-4, y + 4)]);
        }
        return new(cell, (8, 0), (0, courses * 8),
            returned
                ? "A diamond scaffold supplies four coarse landmarks per compartment. Each diagonal boundary is replaced by its traced pointed detour. Successive root rows have opposite handedness; both rows are retained inside one native repeat, producing the distinct intervening closed compartments of Plate 9 figure 5."
                : "A diamond scaffold supplies four coarse landmarks per compartment. Each diagonal boundary is replaced by its traced pointed detour, with diagonal ends and two orthogonal middle chords. Two closed compartment orientations tile the repeat; shared landmark ports negotiate the boundary junctions.",
            "Relative boundary reconstruction checked by a trace overlay against the enlarged plate. The pointed diagonals, shared detours and compartment arrangement follow the source; exact ink thickness, scan irregularity and calibrated distances are not reproduced.",
            AlternateRows: false);
    }

    // One root-to-neighbour coarse diagonal, (0,0) -> (4,-4), has this
    // source-traced fine chain. Quarter turns give the other three directions.
    // Root rows reflected in figure 5 reverse its handedness. These integer
    // pairs author finite native words; neither execution nor seam admission
    // receives an authoring coordinate table.
    private static (int X, int Y)[] PointedDiamondEdge((int X, int Y) from,
        (int X, int Y) to, bool rootFirst, bool returned)
    {
        var root = rootFirst ? from : to;
        var other = rootFirst ? to : from;
        var reflect = returned && (root.Y / 8 & 1) != 0;
        var delta = (X: other.X - root.X, Y: other.Y - root.Y);
        if (reflect) delta.Y = -delta.Y;
        var turns = delta switch
        {
            (4, -4) => 0, (4, 4) => 1, (-4, 4) => 2, (-4, -4) => 3,
            _ => throw new ArgumentException("A source compartment edge must join adjacent diamond-grid roles.")
        };
        (int X, int Y)[] seed = [(0, 0), (1, 1), (3, 1), (3, -3), (4, -4)];
        var points = seed.Select(point =>
        {
            var turned = turns switch
            {
                0 => point, 1 => (-point.Y, point.X),
                2 => (-point.X, -point.Y), _ => (point.Y, -point.X)
            };
            return (X: root.X + turned.Item1, Y: root.Y + (reflect ? -turned.Item2 : turned.Item2));
        }).ToArray();
        if (!rootFirst) Array.Reverse(points);
        return points;
    }

    private sealed partial class Cell
    {
        /// <summary>Retains an actual coarse native unit, its selected chord
        /// replacements and the standard refinement witness. Only the fine
        /// path is executed. Original coarse landmark roles remain the actual
        /// fine ports, with additional roles at every new boundary corner.</summary>
        public void RefinedDiamond(bool returned, (int X, int Y)[] vertices)
        {
            if (vertices.Length != 4) throw new ArgumentException("The source scaffold has four coarse corner roles.", nameof(vertices));
            var coarseWords = Enumerable.Range(0, vertices.Length).Select(index =>
            {
                var next = vertices[(index + 1) % vertices.Length];
                return Word((next.X - vertices[index].X, next.Y - vertices[index].Y));
            }).ToArray();
            var coarsePath = new Relation(Word(vertices[0]), N.S(coarseWords));
            var coarsePorts = N.S(Enumerable.Range(0, vertices.Length + 1).Select(index =>
                (IElement)new Relation(new Placeholder(), new Relation(coarsePath,
                    index == vertices.Length ? coarsePath : coarseWords[index]))).ToArray());
            var coarse = new UnitArrangementProgram(frame, "coarse four-landmark scaffold", N.S(coarsePath), coarsePorts, N.S(coarsePath));
            var replacements = N.S(Enumerable.Range(0, vertices.Length).Select(index =>
            {
                var chain = PointedDiamondEdge(vertices[index], vertices[(index + 1) % vertices.Length], index % 2 == 0, returned);
                var words = Enumerable.Range(0, chain.Length - 1).Select(part => (IElement)Word(
                    (chain[part + 1].X - chain[part].X, chain[part + 1].Y - chain[part].Y))).ToArray();
                return (IElement)new Relation(coarseWords[index], N.S(words));
            }).ToArray());
            var refined = coarse.Refine("pointed four-chain boundary refinement", replacements);
            paths.AddRange(N.E(refined.Paths)); closed.AddRange(N.E(refined.ClosedPaths));
            ports.AddRange(N.E(refined.Ports));
            RetainConstruction(refined.Surface);

            foreach (var path in N.E(refined.Paths).Cast<Relation>())
            {
                var chords = N.E((Relation)path.Right).Cast<Relation>().ToArray();
                var prefix = new List<IElement>();
                for (var index = 0; index <= chords.Length; index++)
                {
                    IElement selector = index == chords.Length ? path : chords[index];
                    var port = N.E(refined.Ports).OfType<Relation>().SingleOrDefault(actual =>
                        actual.Right is Relation pick && ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, selector));
                    if (port is null)
                    {
                        port = new(new Placeholder(), new Relation(path, selector)); ports.Add(port);
                    }
                    var read = frame.Reduce((Relation)path.Left, N.S(prefix.ToArray()));
                    if (!frame.TryReadResult(read, out var position, out _, out var unknown) || !N.Read(position, out _) || N.E(unknown).Length != 0)
                        throw new InvalidOperationException("A refined boundary landmark needs an exact native cursor read.");
                    published.Add((port.Left, position));
                    // This read connects the actual new selector to its
                    // source prefix; it does not identify coincident sites.
                    RetainConstruction(new Relation(port, read));
                    if (index < chords.Length) prefix.AddRange(N.E(chords[index]));
                }
            }
        }
    }
}
