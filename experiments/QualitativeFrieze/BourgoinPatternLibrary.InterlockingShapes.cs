using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static SourceRevision ClosedArmField()
    {
        var cell = new Cell();
        // These are the two CLOSED white compartments in Plate 9 fig. 10,
        // not a collection of open spiral arms. Authoring y points down into
        // the source image; SourceContour reverses it at the frame boundary.
        var upright = cell.SourceContour(
            (-7, 0), (-4, -3), (2, -3), (2, -7), (0, -7), (0, -9),
            (4, -9), (4, -3), (7, 0), (4, 3), (4, 9), (0, 9),
            (0, 7), (2, 7), (2, 3), (-4, 3));
        var sideways = cell.SourceContour(
            (-9, 4), (-9, 0), (-7, 0), (-7, 2), (-3, 2), (-3, -4),
            (0, -7), (3, -4), (9, -4), (9, 0), (7, 0), (7, -2),
            (3, -2), (3, 4), (0, 7), (-3, 4));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        var mirror = cell.CorrectedMap((-1, 0), (0, 1));
        cell.SourceImage(upright, identity, (0, 0));
        cell.SourceImage(sideways, identity, (7, -7));
        cell.SourceImage(upright, mirror, (0, -14));
        cell.SourceImage(sideways, mirror, (7, -21));
        return new(cell, (14, 0), (0, -28),
            "Two closed stepped-arm compartment prototypes alternate diagonally. One has two same-side arms with hands bent together; the other has opposed arms whose hands turn toward the body. Actual mirrored images supply the next row. Every source corner is retained as a port correspondence, so the closed white chambers and their diagonal junctions determine the field rather than open spiral strokes.",
            "Reconstructed from the closed negative compartments of Plate 9 fig. 10. The two sixteen-corner contours, their opposed row handedness and their interlocking disposition follow the source. Finite shared-reach proportions are selected from the trace; printed ink width and exact raster coordinates are not native promises.");
    }

    private static SourceRevision InterlockingSerifs(bool dense)
    {
        var cell = new Cell();
        // Both source fields use the same interlocking disposition. Plate 14
        // has inset short serifs; Plate 19 attaches them at the cap ends.
        var contour = dense
            ? cell.SourceContour(
                (0, 0), (2, 0), (2, 1), (1, 1), (1, 3), (8, 3),
                (8, 1), (7, 1), (7, 0), (9, 0), (9, 7), (7, 7),
                (7, 6), (8, 6), (8, 4), (1, 4), (1, 6), (2, 6),
                (2, 7), (0, 7))
            : cell.SourceContour(
                (9, 0), (9, 7), (8, 7), (8, 6), (7, 6), (7, 5),
                (8, 5), (8, 4), (1, 4), (1, 5), (2, 5), (2, 6),
                (1, 6), (1, 7), (0, 7), (0, 0), (1, 0), (1, 1),
                (2, 1), (2, 2), (1, 2), (1, 3), (8, 3), (8, 2),
                (7, 2), (7, 1), (8, 1), (8, 0));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        // Source-image clockwise turn becomes R -> D and U -> R after the
        // source y/frame y correspondence. Placement words follow that map.
        var quarter = cell.CorrectedMap((0, -1), (1, 0));
        cell.SourceImage(contour, identity, (0, 0));
        cell.SourceImage(contour, quarter, (13, 1));
        cell.SourceImage(contour, quarter, (8, -4));
        cell.SourceImage(contour, identity, (5, -5));
        return new(cell, (10, 0), (0, -10),
            dense
                ? "A closed twenty-corner serif-I compartment repeats horizontally and after an actual quarter-turn vertically. Four staggered images form one macrocell; adjacent caps and inward serifs interlock. The spaces are closed compartments of the boundary network rather than decorations laid over a meander."
                : "A closed twenty-eight-corner serif-I compartment has short serifs inset from the cap ends. Horizontal and actual quarter-turned vertical images alternate on a staggered disposition. Four images form the macrocell; shared boundary landmark ports expose the interlocking chambers and articulated corner details.",
            dense
                ? "Reconstructed from the repeated horizontal/vertical negative I compartments of Plate 19 fig. 4. Their cap-end serifs and interlocking disposition replace the earlier open Greek-key approximation. Shared-reach proportions and a thin boundary interpretation are selected; this is not a tracing of the printed ink."
                : "Reconstructed from Plate 14 fig. 3's inset-serif negative compartments, not a four-armed Greek key. The actual contour, corner order and horizontal/vertical interlock are source-derived. Exact printed proportions, stroke widths and filled-region inference remain outside the native contour program.");
    }

    private sealed partial class Cell
    {
        public sealed record SourceContourSeed(CorrectedPathSeed Path, int[] Corners);

        /// <summary>Compile the traced corner order to unmeasured shared-reach
        /// words. The source arrangements put all chamber meetings at these
        /// explicit corners; a separate neighbourhood audit guards that
        /// premise rather than publishing artificial collinear tick sites.</summary>
        public SourceContourSeed SourceContour(params (int X, int Y)[] sourceVertices)
        {
            if (sourceVertices.Length < 3) throw new ArgumentException("A source compartment needs a closed contour.", nameof(sourceVertices));
            var vertices = new List<(int X, int Y)>();
            for (var index = 0; index < sourceVertices.Length; index++)
            {
                var from = sourceVertices[index]; var to = sourceVertices[(index + 1) % sourceVertices.Length];
                if (from == to) throw new ArgumentException("A source corner cannot repeat its next site.", nameof(sourceVertices));
                vertices.Add((from.X, -from.Y));
            }
            vertices.Add(vertices[0]);
            return new(CorrectedSeed(true, vertices.ToArray()), Enumerable.Range(0, sourceVertices.Length).ToArray());
        }

        /// <summary>The shared compiler retains actual word substitution and
        /// placement witnesses. This additional read binds the ordered traced
        /// corner selectors to the resulting executable path's actual ports.</summary>
        public void SourceImage(SourceContourSeed source, QualitativePatternTransform transform, (int X, int Y) offset)
        {
            var firstPort = ports.Count;
            CorrectedImage(source.Path, transform, offset);
            var actualPath = (Relation)paths[^1];
            var cornerMaps = source.Corners.Select(index =>
            {
                var sourceSelector = new Relation((Relation)source.Path.Definition.Left, source.Path.Chords[index]);
                return (IElement)new Relation(sourceSelector, (Relation)ports[firstPort + index]);
            }).ToArray();
            var orderedCorners = N.S(cornerMaps);
            var cycle = N.S(cornerMaps.Select((corner, index) =>
                (IElement)new Relation(corner, cornerMaps[(index + 1) % cornerMaps.Length])).ToArray());
            RetainConstruction(new Relation(new Relation(source.Path.Definition, actualPath), new Relation(orderedCorners, cycle)));
        }

    }
}
