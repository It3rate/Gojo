namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class InteriorLatticeRecipes
{
    private static readonly Cell[] Domino = Square((0, 0), (1, 0));
    private static readonly Cell[] Tromino = Square((0, 0), (1, 0), (2, 0));
    private static readonly Cell[] L3 = Square((0, 0), (1, 0), (0, 1));
    private static readonly Cell[] L4 = Square((0, 0), (1, 0), (2, 0), (0, 1));
    private static readonly Cell[] T4 = Square((0, 0), (1, 0), (2, 0), (1, 1));
    private static readonly Cell[] Z4 = Square((0, 0), (1, 0), (1, 1), (2, 1));
    private static readonly Cell[] O4 = Square((0, 0), (1, 0), (0, 1), (1, 1));
    private static readonly Cell[] L5 = Square((0, 0), (1, 0), (2, 0), (3, 0), (0, 1));
    private static readonly Cell[] T5 = Square((0, 0), (1, 0), (2, 0), (1, 1), (1, 2));
    private static readonly Cell[] P5 = Square((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));
    private static readonly Cell[] F5 = Square((1, 0), (2, 0), (0, 1), (1, 1), (1, 2));
    private static readonly Cell[] U5 = Square((0, 0), (0, 1), (1, 1), (2, 1), (2, 0));
    private static readonly Cell[] W5 = Square((0, 0), (1, 0), (1, 1), (2, 1), (2, 2));
    private static readonly Cell[] V5 = Square((0, 0), (1, 0), (2, 0), (0, 1), (0, 2));
    private static readonly Cell[] X5 = Square((1, 0), (0, 1), (1, 1), (2, 1), (1, 2));
    private static readonly Cell[] Y5 = Square((0, 0), (1, 0), (2, 0), (3, 0), (1, 1));

    private static IEnumerable<Spec> Specifications()
    {
        Cell[][][] orthogonal =
        [
            [Domino, Tromino], [Domino, O4], [Tromino, O4], [Domino, Tromino, O4],
            [O4, L4], [Tromino, L3], [Domino, L3], [O4, T4], [Tromino, Z4], [Domino, O4, L3]
        ];
        Cell[][][] elbows =
        [
            [L3], [L3, Domino], [L4], [L4, O4], [L4, L3], [L5, Domino], [V5, L3], [P5, L3], [L3, T4], [L4, Z4]
        ];
        Cell[][][] forks =
        [
            [T4], [T4, Domino], [T4, O4], [T4, L4], [T5, Domino], [T5, L3], [X5, L3], [Y5, L3], [T4, Z4], [T4, L3, P5]
        ];
        Cell[][][] stairs =
        [
            [Z4], [Z4, Domino], [Z4, L4], [Z4, T4], [W5, L3], [W5, Domino], [V5, Z4], [W5, T4], [Z4, L3], [W5, P5, Domino]
        ];
        Cell[][][] mixed =
        [
            [P5, Domino], [P5, L3], [F5, L3], [U5, L3], [V5, Domino], [Y5, Domino], [X5, Domino],
            [F5, P5, Domino], [U5, T4, Domino], [P5, T4, L3]
        ];
        string[] variants = ["paired", "returned", "crosswise", "alternating", "offset", "broad", "opposed", "clustered", "interlocked", "mixed"];
        for (var family = 0; family < 5; family++)
        {
            var palettes = new[] { orthogonal, elbows, forks, stairs, mixed }[family];
            var title = new[] { "Rectangular rooms", "L and elbow rooms", "T and fork rooms", "Stair and Z rooms", "Mixed broad polyomino rooms" }[family];
            for (var variant = 0; variant < 10; variant++)
            {
                var palette = palettes[variant];
                var width = family == 0 || variant % 3 == 0 ? 6 : 4;
                var height = family == 4 && variant >= 2 ? 5 : 4;
                if (family == 2 && variant == 9) height = 5;
                if (palette.All(shape => shape.Length == 3) && width * height % 3 != 0) width = 6;
                if (palette.All(shape => shape.Length == 4) && width * height % 4 != 0) height = 4;
                var seed = 817 + family * 137 + variant * 43;
                var tiles = Cover(width, height, palette, seed);
                yield return new($"{title}: {variants[variant]}", title,
                    "A small periodic exact cover of an ordinary tight grid is compiled into actual exclusive atomic ownership. Whole L/T/step/rectangular regions come from removing their internal shared edges, rather than independently placing possibly overlapping outlines.",
                    width, height, tiles, ScaleX: variant % 5 == 1 ? 2 : 1, ScaleY: variant % 5 == 3 ? 2 : 1);
            }
        }
        // Actual oblique native words are supplied here. The affine read is an
        // optional basis on a separately varied partition, never a substitute
        // for ten identical owner maps.
        for (var variant = 0; variant < 10; variant++)
        {
            var palettes = new[] { new[] { L3, Domino }, new[] { T4, O4 }, new[] { Z4, Domino }, new[] { L4, T4 },
                new[] { P5, L3 }, new[] { U5, L3 }, new[] { V5, Domino }, new[] { W5, L3 }, new[] { F5, Domino }, new[] { Y5, L3 } };
            var width = variant % 2 == 0 ? 6 : 4; var height = 4;
            yield return new($"Oblique fused rooms: {variants[variant]}", "Oblique and shifted room fields",
                "The underlying independent axis words are sheared before adjacent atoms are fused. A separate periodic owner assembly gives broad oblique elbows, forks and stepped interiors; this is not renderer-only slant.",
                width, height, Cover(width, height, palettes[variant], 3001 + variant * 61), ScaleX: 2, ScaleY: 2, Shear: variant % 3 + 1);
        }
        // Hexagon centres form their own two-generator lattice. Each centre
        // initially owns six actual triangular faces; merging centre cells then
        // removes those interior triangle and hexagon separators together.
        var hexShapes = new Cell[][][]
        {
            [Square((0, 0))], [Square((0, 0), (1, 0))], [Square((0, 0), (1, 0), (2, 0))],
            [Square((0, 0), (1, 0), (0, 1))], [Square((0, 0), (1, 0)), Square((0, 0), (0, 1), (1, 1))],
            [Square((0, 0), (1, 0), (1, 1)), Square((0, 0), (1, 0))],
            [Square((0, 0), (1, 0), (0, 1)), Square((0, 0))],
            [Square((0, 0), (1, 0), (2, 0)), Square((0, 0), (1, 0))],
            [Square((0, 0), (1, 0), (1, -1)), Square((0, 0))],
            [Square((0, 0), (1, 0), (0, 1)), Square((0, 0), (1, 0), (1, 1)), Square((0, 0), (1, 0))]
        };
        for (var variant = 0; variant < 10; variant++)
        {
            // An eight-cell period forces the later mixed palettes to use
            // their companion rooms. A six-cell period could silently choose
            // two primary trimers and duplicate the earlier pure assembly.
            var width = variant is 6 or 7 or 9 ? 4 : 3;
            yield return new($"Six-triangle and polyhex rooms: {variants[variant]}", "Six-triangle hexagon fusion",
                "Six triangles first meet around one actual site. Selected adjacent six-triangle regions then share one larger exclusive interior. The two period generators and every repeated whole tile remain connected.",
                width, 2, Cover(width, 2, hexShapes[variant], 7001 + variant * 71, hexagonal: true), Triangular: true, Polyhex: true);
        }

        for (var variant = 0; variant < 10; variant++)
        {
            var palettes = new[] { new[] { L3 }, new[] { Z4 }, new[] { T4 }, new[] { L4, Z4 }, new[] { V5, Domino },
                new[] { W5, L3 }, new[] { U5, L3 }, new[] { P5, Domino }, new[] { F5, L3 }, new[] { T4, L3, O4 } };
            var width = variant % 2 == 0 ? 6 : 4; var height = 4;
            var squareTiles = Cover(width, height, palettes[variant], 8009 + variant * 53);
            var triangulated = squareTiles.Select(tile => tile with
            { Cells = tile.Cells.SelectMany(cell => new[] { new Cell(cell.X, cell.Y, 0), new Cell(cell.X, cell.Y, 1) }).ToArray() }).ToArray();
            yield return new($"Rhombic and chevron interiors: {variants[variant]}", "Rhombic and chevron fusions",
                "Opposed triangles first make rhombi, then an independently varied connected rhombus assembly makes elbows, zigzag rooms or broad chevrons. Diagonals disappear through actual face ownership rather than white paint.",
                width, height, triangulated, Triangular: true);
        }

        var rhombus = new[] { new Cell(0, 0, 0), new Cell(0, 0, 1) };
        var triangle = new[] { new Cell(0, 0, 0), new Cell(0, 0, 1), new Cell(1, 0, 0), new Cell(0, 1, 0) };
        var trapezoid = new[] { new Cell(0, 0, 0), new Cell(0, 0, 1), new Cell(1, 0, 0) };
        var hexagon = HexagonTriangles(new Cell(0, 0));
        var sixDirections = new P[] { new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1), new(1, -1) };
        var star = hexagon.Concat(sixDirections.Select((a, i) =>
        {
            var b = sixDirections[(i + 1) % 6]; return Triangle(a, b, a + b);
        })).ToArray();
        var largeTriangle = Enumerable.Range(0, 3).SelectMany(y => Enumerable.Range(0, 3 - y)
            .SelectMany(x => x + y < 2 ? new[] { new Cell(x, y, 0), new Cell(x, y, 1) } : new[] { new Cell(x, y, 0) })).ToArray();
        var trianglePalettes = new Cell[][][]
        {
            [triangle], [trapezoid, rhombus], [hexagon, triangle], [triangle, rhombus], [largeTriangle, trapezoid],
            [hexagon, trapezoid], [largeTriangle, rhombus], [triangle, trapezoid], [hexagon, rhombus], [star, rhombus, trapezoid]
        };
        for (var variant = 0; variant < 10; variant++)
            yield return new(variant == 9 ? "Six-point stars with shared companion rooms" : $"Triangular area mergers: {variants[variant]}", "Triangular and trapezoid rooms",
                "One conforming triangular atomic field supports larger triangles, trapezoids, rhombi and hexagons. Repeated exclusive unions remove different separators while their boundary remains a shared native seam.",
                6, 4, Cover(6, 4, trianglePalettes[variant], 9001 + variant * 89, triangular: true), Triangular: true);

        for (var variant = 0; variant < 10; variant++)
        {
            var width = 3 + variant % 3; var height = 3 + variant / 3;
            var profile = variant switch
            {
                0 => new[] { 0, 1, 0 }, 1 => new[] { 0, 1, 1 }, 2 => new[] { 0, 2, 0 },
                3 => new[] { 0, 1, 2, 1 }, 4 => new[] { 0, 2, 2, 0 }, 5 => new[] { 0, 1, 1, 0 },
                6 => new[] { 0, 0, 1, 2, 2 }, 7 => new[] { 0, 1, 2, 1, 0 }, 8 => new[] { 0, 2, 2, 1, 0 },
                _ => new[] { 0, 0, 1, 2, 1, 0 }
            };
            var tiles = new List<Tile>();
            // Two repeated rooms make one period. The proposed broad profile
            // changes both shared seams without changing the external period.
            for (var tile = 0; tile < 2; tile++)
            {
                var cells = Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(tile * width + profile[y], width).Select(x => new Cell(x, y))).ToArray();
                tiles.Add(new(tile, new(tile * width, 0), cells));
            }
            yield return new($"Negotiated broad seam: {variants[variant]}", "Independent seam compromise",
                "Neighbouring rectangular rooms independently request atomic faces along a broad step or shoulder. Their desired claims overlap, so an explicit actual-face decision is needed before the final shared seam can settle. Rejected claims remain inspectable.",
                2 * width, height, tiles.ToArray(), Negotiate: true);
        }
    }

    private static Cell[] Square(params (int X, int Y)[] cells) => cells.Select(cell => new Cell(cell.X, cell.Y)).ToArray();
}
