using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static IReadOnlyList<AdditionalSource> ExtendedMotifs() =>
    [
        new("B25", "Six-point star junctions", 412, "Plate 11, fig. 3", ExtendedStarJunctions(), Triangular: true),
        new("B26", "Hexagonal cells with radial florets", 416, "Plate 13, fig. 8", ExtendedHexagonalFlorets(), Triangular: true),
        new("B27", "Angled paired-band courses", 416, "Plate 13, fig. 4", ExtendedAngledBands()),
        new("B28", "Faceted shell-fan courses", 416, "Plate 13, fig. 6", ExtendedFans()),
        new("B29", "Hooked hexagon junctions", 424, "Plate 17, fig. 4", ExtendedHookedHexagons(), Triangular: true),
        new("B30", "Faceted cloud-scale arches", 424, "Plate 17, fig. 7", ExtendedCloudScales()),
        new("B31", "Four-turn key windows", 428, "Plate 19, fig. 3", ExtendedKeyWindows()),
        new("B32", "Diagonal spiral-key courses", 430, "Plate 20, fig. 3", ExtendedDiagonalKeys()),
        new("B33", "Folded diagonal ribbon units", 430, "Plate 20, fig. 7", ExtendedFoldedRibbons()),
        new("B34", "Crossed faceted loop lattice", 432, "Plate 21, fig. 2", ExtendedCrossedLoops())
    ];

    private static SourceRevision ExtendedStarJunctions()
    {
        var cell = new Cell();
        var star = cell.CorrectedSeed(true,
            (4, 0), (1, 1), (0, 4), (-1, 2), (-4, 4), (-2, 1),
            (-4, 0), (-1, -1), (0, -4), (1, -2), (4, -4), (2, -1), (4, 0));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        cell.CorrectedImage(star, identity, (0, 0));
        // Three actual neighbour passages supply the six incident directions
        // after repetition. They do not assert the source's surrounding
        // irregular pentagons have been reconstructed.
        foreach (var (from, to) in new ((int X, int Y), (int X, int Y))[]
        {
            ((4, 0), (8, 0)), ((0, 4), (0, 8)), ((-4, 4), (-8, 8))
        }) cell.CorrectedImage(cell.CorrectedSeed(false, from, to), identity, (0, 0));
        return new(cell, (12, 0), (0, 12),
            "One closed twelve-corner six-point star repeats on a triangular disposition. Three published neighbour passages connect each star to the next copies; all six contact directions follow from the native repeat and shared endpoint roles.",
            "A source-derived star-junction skeleton of Plate 11 fig. 3. The six long points and recurring disposition are retained; the source's intervening irregular polygon network is deliberately coarsened to passages. This is not a tracing of every source chamber.");
    }

    private static SourceRevision ExtendedHexagonalFlorets()
    {
        var cell = new Cell();
        var hexagon = cell.CorrectedSeed(true, (4, 0), (0, 4), (-4, 4), (-4, 0), (0, -4), (4, -4), (4, 0));
        cell.CorrectedImage(hexagon, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        var ray = cell.CorrectedSeed(false, (0, 0), (2, 0));
        foreach (var turn in cell.ExtendedSixTurns()) cell.CorrectedImage(ray, turn, (0, 0));
        return new(cell, (8, -4), (4, 4),
            "A closed hexagonal boundary supplies the repeating cell. Six actual word substitutions reuse one radial stem inside it. Neighbour cells share advertised boundary endpoints, while the six stems meet only through their separately declared central ports.",
            "A skeleton reading of Plate 13 fig. 8: hexagonal disposition and sixfold radial floret are retained. Petal curvature, the inner contour bands, colour and botanical filling are omitted. The optional triangular display supplies regular-looking hexagons rather than an angle calibration.");
    }

    private static SourceRevision ExtendedAngledBands()
    {
        var cell = new Cell();
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        var ridge = cell.CorrectedSeed(false, (0, 0), (2, 1), (4, 0));
        cell.CorrectedImage(ridge, identity, (0, 0));
        cell.CorrectedImage(ridge, identity, (0, -1));
        var inset = cell.CorrectedSeed(false, (0, -2), (1, -2), (1, -3), (2, -2), (3, -3), (3, -2), (4, -2));
        cell.CorrectedImage(inset, identity, (0, 0));
        var diamond = cell.CorrectedSeed(true, (0, 0), (1, 1), (2, 0), (1, -1), (0, 0));
        cell.CorrectedImage(diamond, identity, (1, -4));
        return new(cell, (4, 0), (0, -6),
            "Two translated copies of one angled ridge establish a paired band. A cranked inset follows its cadence and a four-edge diamond occupies the intervening course. Published end roles join each ridge and inset to the next column.",
            "A deliberately coarse construction-line reading of Plate 13 fig. 4. Paired angled courses, the central fret cadence and intervening diamond rhythm are retained; fine knot filling, exact fret proportions and the coloured ribbon interpretation are omitted.");
    }

    private static SourceRevision ExtendedFans()
    {
        var cell = new Cell();
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        var shell = cell.CorrectedSeed(true, (-4, 3), (-3, 1), (-2, -1), (0, -2), (2, -1), (3, 1),
            (4, 3), (2, 3), (0, 3), (-2, 3), (-4, 3));
        var ribs = new[] { cell.CorrectedSeed(false, (0, -2), (-2, 3)),
            cell.CorrectedSeed(false, (0, -2), (0, 3)), cell.CorrectedSeed(false, (0, -2), (2, 3)) };
        foreach (var offset in new[] { (0, 0), (4, -5) })
        {
            cell.CorrectedImage(shell, identity, offset);
            foreach (var rib in ribs) cell.CorrectedImage(rib, identity, offset);
        }
        return new(cell, (8, 0), (0, -10),
            "One faceted shell boundary and three radial rib prototypes repeat in two staggered courses. Ribs consume the same exposed shell-apex and top-edge landmarks; the second course's upper corners meet the preceding shell tips through explicit seam rules.",
            "A faceted shell-fan skeleton of Plate 13 fig. 6. Staggering, common fan apex and spreading ribs are retained. Scrolls, curved outlines, concentric inner arcs and ornament inside the fan are omitted; the finite chords do not claim smooth-curve semantics.");
    }

    private static SourceRevision ExtendedHookedHexagons()
    {
        var cell = new Cell();
        var hexagon = cell.ExtendedGridSeed(true, (3, 0), (0, 3), (-3, 3), (-3, 0), (0, -3), (3, -3), (3, 0));
        cell.CorrectedImage(hexagon, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        var hook = cell.CorrectedSeed(true, (3, 0), (4, 0), (3, -1), (3, 0));
        var turns = cell.ExtendedSixTurns();
        foreach (var index in new[] { 0, 2, 4 }) cell.CorrectedImage(hook, turns[index], (0, 0));
        return new(cell, (6, -3), (3, 3),
            "A six-edge compartment repeats on its hexagonal neighbour disposition. Three images of one small triangular hook select alternating boundary faces. Split native side landmarks expose the hook contacts before neighbouring copies negotiate their sites.",
            "A skeleton of Plate 17 fig. 4's hexagon framework and small directional junction hooks. The freehand flower is excluded. Hook faces and relative shared-reach sizes are selected, and no filled black triangle or botanical contour is inferred from the line graph.");
    }

    private static SourceRevision ExtendedCloudScales()
    {
        var cell = new Cell();
        // The source scallop is a finite refinement of a single coarse
        // chord: its intermediate profile is optional detail, while the
        // original two endpoint landmarks remain justified native roles.
        var arch = cell.ExtendedRefinedChord((0, 0), (24, 0),
            (0, 0), (0, 2), (1, 3), (2, 4), (4, 4), (5, 3), (6, 2),
            (6, 5), (7, 7), (9, 8), (12, 8), (15, 8), (17, 7), (18, 5),
            (18, 2), (19, 3), (20, 4), (22, 4), (23, 3), (24, 2), (24, 0));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        foreach (var offset in new[] { (0, 0), (24, 0), (12, 8), (36, 8) })
            cell.CorrectedImage(arch, identity, offset);
        return new(cell, (48, 0), (0, 16),
            "A straight coarse span is replaced by one twenty-chord cloud profile: two smaller lateral lobes flank a higher central lobe. Its original endpoints retain an exact zero-displacement refinement proof. Four actual images reuse the same fine prototype in staggered courses. The central top landmark is explicitly split so the next course's endpoints can meet a published port rather than a rendered chord interior.",
            "A faceted three-lobed cloud-scale skeleton of Plate 17 fig. 7. It retains the small/high/small lobe order and staggered courses; finite flat and angled chords approximate each curved silhouette. Smooth curvature, the source's short accent strokes and filled cloud-region inference are omitted.");
    }

    private static SourceRevision ExtendedKeyWindows()
    {
        var cell = new Cell();
        cell.QuarterContour([(4, 0), (4, 1), (1, 1), (1, 2), (2, 2), (2, 3), (0, 3), (0, 4)]);
        var marker = cell.CorrectedSeed(true, (0, 1), (1, 0), (0, -1), (-1, 0), (0, 1));
        cell.CorrectedImage(marker, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        return new(cell, (8, 0), (0, 8),
            "One stepped quarter boundary produces a closed four-part key window through actual quarter-turn substitution receipts and a finite word-return witness. A separate small four-edge central marker supplies the repeated dot's skeleton. Published outer tip roles connect adjacent copies.",
            "An authored four-turn key-window reading inspired by Plate 19 fig. 3's bent-cross units and centre dots. It retains the repeated turning-arm character, not every source boundary chamber; the dot is an outlined marker, and the source's continuous fret topology is not claimed to be identical.");
    }

    private static SourceRevision ExtendedDiagonalKeys()
    {
        var cell = new Cell();
        var key = cell.CorrectedSeed(false, Key());
        cell.CorrectedImage(key, cell.CorrectedMap((1, 1), (-1, 1)), (0, 0));
        return new(cell, (10, 10), (-9, 9),
            "A nested rectilinear key traversal is reused through the explicit word correspondence R→R+U and U→L+U. Both repeat directions use the same correspondence. Its actual terminal role meets the next start role along each diagonal course, while successive courses stay distinct.",
            "A diagonal spiral-key course adaptation of Plate 20 fig. 3. The turning traversal and oblique repeated disposition follow the source family; the authored nested key has additional turns, selected spacing and no shaded ribbon filling. The display's apparent 45-degree direction is an optional interpretation of the native word map.");
    }

    private static SourceRevision ExtendedFoldedRibbons()
    {
        var cell = new Cell();
        var corner = cell.ExtendedGridSeed(true, (0, 0), (0, 4), (4, 4), (4, 3), (1, 3), (1, 0), (0, 0));
        var diagonal = cell.CorrectedMap((1, 1), (-1, 1));
        var opposed = cell.CorrectedMap((-1, -1), (1, -1));
        cell.CorrectedImage(corner, diagonal, (0, 0));
        cell.CorrectedImage(corner, opposed, (0, 10));
        return new(cell, (5, 5), (-5, 5),
            "One closed folded-strip contour supplies two opposed images on an oblique disposition. Every unit side landmark is exposed before contact, so repeated copies can share the corner boundaries without merging unrelated intersecting stroke interiors.",
            "A folded-ribbon scaffold adaptation of Plate 20 fig. 7's interleaved diagonal lozenges. It preserves paired oblique folds and repeated lozenge spacing; over/under weaving, the source's exact central lozenge shapes and filled shading are not encoded.");
    }

    private static SourceRevision ExtendedCrossedLoops()
    {
        var cell = new Cell();
        var loop = cell.CorrectedSeed(true, (-4, -1), (-3, -2), (3, -2), (4, -1),
            (4, 1), (3, 2), (-3, 2), (-4, 1), (-4, -1));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        var turn = cell.CorrectedMap((0, 1), (-1, 0));
        cell.CorrectedImage(loop, identity, (0, 0));
        cell.CorrectedImage(loop, turn, (0, 0));
        foreach (var y in new[] { -1, 1 })
        {
            var passage = cell.CorrectedSeed(false, (4, y), (6, y));
            cell.CorrectedImage(passage, identity, (0, 0));
            cell.CorrectedImage(passage, turn, (0, 0));
        }
        return new(cell, (10, 0), (0, 10),
            "Two actual images of one faceted closed loop cross through their chord interiors. Four paired neighbour passages connect the advertised outer caps. No crossing site is manufactured: the crossing lines have no common endpoint selector there, whereas cap contacts carry actual seam declarations.",
            "A faceted crossed-loop lattice inspired by Plate 21 fig. 2's square-knot framework. The repeating loop and large-cell skeleton is retained; botanical filling, smooth turns, ribbon thickness and over/under knot order are omitted. This is an explicit nonjoining line crossing, not a proof of the source knot's topology.");
    }

    private sealed partial class Cell
    {
        public QualitativePatternTransform[] ExtendedSixTurns()
        {
            (int X, int Y)[] horizontal = [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)];
            (int X, int Y)[] vertical = [(0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1), (1, 0)];
            return Enumerable.Range(0, 6).Select(index => CorrectedMap(horizontal[index], vertical[index])).ToArray();
        }

        // Authored subdivisions only expose the common unit landmarks. They
        // create no geometric intersection and leave actual closure explicit.
        public CorrectedPathSeed ExtendedGridSeed(bool closes, params (int X, int Y)[] corners)
        {
            var vertices = new List<(int X, int Y)>();
            for (var index = 0; index < corners.Length - 1; index++)
            {
                var from = corners[index]; var to = corners[index + 1];
                var dx = to.X - from.X; var dy = to.Y - from.Y;
                var count = ExtendedGreatestCommonDivisor(Math.Abs(dx), Math.Abs(dy));
                if (count == 0) throw new ArgumentException("A subdivided source chord needs distinct endpoint roles.", nameof(corners));
                for (var step = 0; step < count; step++) vertices.Add((from.X + step * dx / count, from.Y + step * dy / count));
            }
            vertices.Add(corners[^1]);
            return CorrectedSeed(closes, vertices.ToArray());
        }

        private static int ExtendedGreatestCommonDivisor(int first, int second)
        {
            while (second != 0) { var remaining = first % second; first = second; second = remaining; }
            return first;
        }

        public CorrectedPathSeed ExtendedRefinedChord((int X, int Y) from, (int X, int Y) to,
            params (int X, int Y)[] vertices)
        {
            if (vertices.Length < 2 || vertices[0] != from || vertices[^1] != to)
                throw new ArgumentException("A finite profile retains both actual coarse endpoints.", nameof(vertices));
            var coarseWord = Word((to.X - from.X, to.Y - from.Y));
            var sourcePath = new Relation(Word(from), N.S(coarseWord));
            var ports = N.S(new Relation(new Placeholder(), new Relation(sourcePath, coarseWord)),
                new Relation(new Placeholder(), new Relation(sourcePath, sourcePath)));
            var source = new UnitArrangementProgram(frame, "coarse profile chord", N.S(sourcePath), ports);
            var fineWords = Enumerable.Range(0, vertices.Length - 1).Select(index => (IElement)Word(
                (vertices[index + 1].X - vertices[index].X, vertices[index + 1].Y - vertices[index].Y))).ToArray();
            var refined = source.Refine("finite faceted profile", N.S(new Relation(coarseWord, N.S(fineWords))));
            RetainConstruction(refined.Surface);
            var path = (Relation)N.E(refined.Paths).Single();
            return new(new Relation(path, N.S()), (Relation)path.Left, N.E((Relation)path.Right).Cast<Relation>().ToArray(), false);
        }
    }
}
