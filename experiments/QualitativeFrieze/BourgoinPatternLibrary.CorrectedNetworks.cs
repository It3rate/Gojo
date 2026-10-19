using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static SourceRevision WovenRectangles()
    {
        var cell = new Cell();
        // One elongated rectangle supplies all four placements. The long
        // edges have explicit contacts at one reach either side of centre;
        // no extra square is drawn into any interstice.
        var rectangle = cell.CorrectedSeed(true,
            (-1, -2), (1, -2), (1, -1), (1, 1), (1, 2), (-1, 2), (-1, 1), (-1, -1), (-1, -2));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        var turn = cell.CorrectedMap((0, 1), (-1, 0));
        cell.CorrectedImage(rectangle, identity, (0, 0));
        cell.CorrectedImage(rectangle, turn, (3, 0));
        cell.CorrectedImage(rectangle, identity, (3, 3));
        cell.CorrectedImage(rectangle, turn, (0, 3));
        return new(cell, (6, 0), (0, 6),
            "One elongated rectangle is reused twice vertically and twice after a quarter-turn. Neighbouring rectangle centres are staggered, and explicit split side contacts leave small square interstices; no square motif is placed in those gaps.",
            "Rebuilt from the actual Fig. 188 arrangement: central side-to-side contacts, alternating horizontal/vertical elongated rectangles, and square gaps. Shared-reach proportions are selected; the book's drawn stroke widths are outside the native skeleton.");
    }

    private static SourceRevision CorrectedOctagonSquares()
    {
        var cell = new Cell();
        var octagon = cell.CorrectedSeed(true,
            (2, 0), (4, 0), (6, 2), (6, 4), (4, 6), (2, 6), (0, 4), (0, 2), (2, 0));
        // This word correspondence moves the old corner-diamond gaps into
        // upright square gaps. The previous Cartesian B06 stays a separate
        // interpretation; this construction does not overwrite its history.
        cell.CorrectedImage(octagon, cell.CorrectedMap((1, 1), (-1, 1)), (0, 0));
        return new(cell, (6, 6), (-6, 6),
            "The existing eight-side contour is reinterpreted by the native correspondence R→R+U and U→L+U, and both period words follow the same map. Horizontal and vertical octagon sides now bound upright square gaps, as in Fig. 201.",
            "A second, source-aligned orientation of the earlier octagon network. The selected Cartesian display reads this map as a scaled 45-degree turn; the pre-numeric program promises its actual word correspondence and endpoint joins, not regular octagons or an angle calibration.");
    }

    private static SourceRevision RibbonDiamonds()
    {
        var cell = new Cell();
        var diagonal = cell.CorrectedMap((2, 1), (-2, 1));
        // Alternating one/three-reach separations define two intersecting
        // ribbon families. Every intended intersection is already a split
        // source vertex before the oblique correspondence is applied.
        foreach (var line in new (int X, int Y)[][]
        {
            [(0, 0), (1, 0), (4, 0)], [(0, 1), (1, 1), (4, 1)],
            [(0, 0), (0, 1), (0, 4)], [(1, 0), (1, 1), (1, 4)]
        }) cell.CorrectedImage(cell.CorrectedSeed(false, line), diagonal, (0, 0));
        return new(cell, (8, 4), (-8, 4),
            "Two straight paired-line systems cross on an oblique disposition. Alternating narrow/wide separations create large diamond areas, narrow four-sided ribbon compartments, and small crossing compartments. All intended contacts are explicit endpoint roles; dotted construction guides and curves are omitted.",
            "A revised Fig. 203 ribbon skeleton replacing the cranked-wave reading. This preserves the paired diagonal disposition and its compartment hierarchy; slopes, narrow/wide proportions and the finite crop remain selected rather than traced from raster pixels. The source's steeper short junction transitions remain simplified to straight ribbon lines.");
    }

    private static SourceRevision DetachedFret()
    {
        var cell = new Cell();
        cell.Path(false, (1, 2), (0, 2), (0, 0), (3, 0), (3, 3), (6, 3), (6, 1), (5, 1));
        return new(cell, (4, 0), (0, 6),
            "A short sideways-S traversal has two shallow opposed hooks and two free ends. Translated units interleave their extents without sharing body endpoints, so the field contains detached units rather than one end-to-end meander.",
            "Rebuilt from Plate 1 fig. 1's shallow fret reading. The framing rails are omitted from this skeleton so detached body connectivity is explicit; repeated rows are a presentation sample added to the source strip.", Rows: 3);
    }

    private sealed partial class Cell
    {
        public sealed record CorrectedPathSeed(Relation Definition, Relation Start, Relation[] Chords, bool Closes);

        public CorrectedPathSeed CorrectedSeed(bool closes, params (int X, int Y)[] vertices)
        {
            var start = Word(vertices[0]);
            var chords = Enumerable.Range(0, vertices.Length - 1).Select(index => Word(
                (vertices[index + 1].X - vertices[index].X, vertices[index + 1].Y - vertices[index].Y))).ToArray();
            var path = new Relation(start, N.S(chords));
            return new(new Relation(path, closes ? N.S(path) : N.S()), start, chords, closes);
        }

        public QualitativePatternTransform CorrectedMap((int X, int Y) horizontal, (int X, int Y) vertical)
        {
            Relation Rule(IElement source, (int X, int Y) target) => new(
                Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame.Move(source, reach), Word(target));
            return new(frame, frame, N.S(Rule(frame.Right, horizontal), Rule(frame.Left, (-horizontal.X, -horizontal.Y)),
                Rule(frame.Up, vertical), Rule(frame.Down, (-vertical.X, -vertical.Y))));
        }

        public void CorrectedImage(CorrectedPathSeed seed, QualitativePatternTransform transform, (int X, int Y) offset)
        {
            var reads = new List<IElement>();
            Relation Apply(Relation word)
            {
                var read = transform.Apply(word); reads.Add(read);
                if (!transform.TryReadComplete(read, out var output))
                    throw new InvalidOperationException("A corrected source correspondence must account for every actual input word.");
                return output;
            }
            var mappedStart = Apply(seed.Start); var mappedChords = seed.Chords.Select(Apply).ToArray();
            var shift = Word(offset); var placement = frame.Reduce(mappedStart, shift);
            if (!frame.TryReadResult(placement, out var start, out _, out var pending) || !N.Read(start, out _) || N.E(pending).Length != 0)
                throw new InvalidOperationException("A corrected source placement must retain its formal displacement proof.");
            var output = NativePath(start, mappedChords, seed.Closes);
            RetainConstruction(new Relation(new Relation(seed.Definition, output.Path),
                new Relation(transform.Surface, new Relation(N.S(reads.ToArray()), new Relation(shift, placement)))));
        }
    }
}
