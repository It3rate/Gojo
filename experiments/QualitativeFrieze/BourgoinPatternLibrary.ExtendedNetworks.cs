using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static IReadOnlyList<AdditionalSource> ExtendedNetworks() =>
    [
        new("B15", "Two-family oblique mesh", 271, "Fig. 177", ExtendedObliqueMesh()),
        new("B16", "Three-family triangular mesh", 271, "Fig. 178", ExtendedTriangleMesh(), Triangular: true),
        new("B17", "Rectangle mesh with diagonal ties", 272, "Fig. 181 (diagonal elaboration)", ExtendedCrossedMesh(true)),
        new("B18", "Square mesh with diagonal ties", 273, "Fig. 183 (diagonal elaboration)", ExtendedCrossedMesh(false)),
        new("B19", "Oblique running bond", 274, "Fig. 185 (first view)", ExtendedObliqueBond()),
        new("B20", "Three-lozenge centred network", 274, "Fig. 185 (second view)", ExtendedCentredLozenges(), Triangular: true),
        new("B21", "Opposed chevron lozenges", 274, "Fig. 185 (third view)", ExtendedChevronLozenges()),
        new("B22", "Alternating pointed lozenges", 275, "Fig. 187", ExtendedAlternatingLozenges()),
        new("B23", "Offset squares and small square gaps", 275, "Fig. 190", ExtendedOffsetSquares()),
        new("B24", "Sliding triangles around shorter hexagon gaps", 276, "Fig. 193", SlidTriangleGaps(), Triangular: true)
    ];

    private static SourceRevision ExtendedObliqueMesh()
    {
        var cell = new Cell();
        var square = cell.CorrectedSeed(true, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0));
        cell.CorrectedImage(square, cell.CorrectedMap((3, 0), (-1, 2)), (0, 0));
        return new(cell, (3, 0), (-1, 2),
            "One closed four-side prototype is read through R→3R and U→L+2U. Repeating its two actual side words supplies the two crossing line families, with endpoint seams at their common corners.",
            "Fig. 177's two-family parallelogram disposition. Its uniform incidence and direction families are retained; the slant, shared-reach proportions and finite crop are selected rather than calibrated from the engraving.");
    }

    private static SourceRevision ExtendedCrossedMesh(bool rectangle)
    {
        var cell = new Cell();
        var horizontal = rectangle ? 2 : 1;
        var positive = cell.CorrectedMap((horizontal, 0), (0, 1));
        var negative = cell.CorrectedMap((horizontal, 0), (0, -1));
        var boundary = cell.CorrectedSeed(true, (0, 0), (2, 0), (2, 2), (0, 2), (0, 0));
        var diagonal = cell.CorrectedSeed(false, (0, 0), (1, 1), (2, 2));
        cell.CorrectedImage(boundary, positive, (0, 0));
        cell.CorrectedImage(diagonal, positive, (0, 0));
        cell.CorrectedImage(diagonal, negative, (0, 2));
        return new(cell, (2 * horizontal, 0), (0, 2),
            "A closed cell boundary and two differently read images of the same diagonal prototype form a four-family network. Both diagonal paths expose their common middle landmark, so their crossing is an admitted shared site rather than a drawing coincidence.",
            rectangle
                ? "Fig. 181's rectangle grid and its illustrated diagonal elaboration. The diagonal subfield is extended uniformly here; the engraving's deliberately varying subdivision density is not reproduced. Rectangle aspect and the Cartesian display are selected."
                : "Fig. 183's square grid and its illustrated diagonal elaboration. The diagonal subfield is extended uniformly here; this does not reproduce the lower-left change of subdivision density. Equal side words support the selected square display without claiming a general geometric calibration.");
    }

    private static SourceRevision ExtendedObliqueBond()
    {
        var cell = new Cell();
        // The two long sides retain their half-side contact. No additional
        // collinear reach sites are needed by the selected running bond.
        var tile = cell.CorrectedSeed(true, (0, 0), (1, 0), (2, 0), (2, 2), (1, 2), (0, 2), (0, 0));
        cell.CorrectedImage(tile, cell.CorrectedMap((2, -1), (0, 2)), (0, 0));
        return new(cell, (4, -2), (2, 3),
            "One four-side tile is placed on an oblique bond. Its top and bottom carry explicit middle contacts; the second period moves one half-width and one whole course, creating the staggered T connections.",
            "The first Fig. 185 disposition: equally oriented oblique quadrilaterals on a staggered bond. The shared-side contact order follows that read; equal Euclidean side lengths, exact slant and printed proportions are not native promises.");
    }

    private static SourceRevision ExtendedCentredLozenges()
    {
        var cell = new Cell();
        var lozenge = cell.CorrectedSeed(true, (0, 0), (2, 0), (0, 2), (-2, 2), (0, 0));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        var next = cell.CorrectedMap((-1, 1), (-1, 0));
        var after = cell.CorrectedMap((0, -1), (1, -1));
        cell.CorrectedImage(lozenge, identity, (0, 0));
        cell.CorrectedImage(lozenge, next, (0, 0));
        cell.CorrectedImage(lozenge, after, (0, 0));
        cell.ExtendedReturn(lozenge, next, 3);
        return new(cell, (4, -2), (2, 2),
            "Three images share one lozenge prototype and one central landmark. Their explicit word correspondences close the three-phase substitution orbit. The enclosing six-corner outline repeats through its own two period words, and neighbouring outlines meet only through declared ports.",
            "The centred three-direction reading in Fig. 185's second view. The selected triangular screen basis makes the three lozenges read like a projected cube. The native claim is the shared prototype, three-phase word return and endpoint topology, not a three-dimensional solid or a calibrated rotation angle.");
    }

    private static SourceRevision ExtendedChevronLozenges()
    {
        var cell = new Cell();
        var lozenge = cell.CorrectedSeed(true, (0, 0), (1, 1), (1, 3), (0, 2), (0, 0));
        cell.CorrectedImage(lozenge, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        cell.CorrectedImage(lozenge, cell.CorrectedMap((-1, 0), (0, 1)), (2, 0));
        return new(cell, (2, 0), (0, 2),
            "Two opposed images of one lozenge share the central vertical side. Repeating that pair creates vertical strands crossed by regularly alternating chevrons; the alternating slope is retained in the actual chord words.",
            "Fig. 185's third view, with its two opposed lozenge directions and continuous vertical family. Heights and slants are selected shared-reach proportions; the source's exact aspect is not measured.");
    }

    private static SourceRevision ExtendedAlternatingLozenges()
    {
        var cell = new Cell();
        var lozenge = cell.CorrectedSeed(true, (-2, 0), (0, -1), (2, 0), (0, 1), (-2, 0));
        var identity = cell.CorrectedMap((1, 0), (0, 1));
        var turn = cell.CorrectedMap((0, 1), (-1, 0));
        cell.CorrectedImage(lozenge, identity, (0, 0));
        cell.CorrectedImage(lozenge, turn, (3, 0));
        cell.CorrectedImage(lozenge, identity, (3, 3));
        cell.CorrectedImage(lozenge, turn, (0, 3));
        return new(cell, (6, 0), (0, 6),
            "One elongated pointed lozenge supplies horizontal and turned images on a four-place repeat. Adjacent tips are explicitly joined. The unfilled four-edge circuits between them arise from the lozenge sides; no separate gap-square path is authored.",
            "Fig. 187's alternating lozenge disposition. The selected Cartesian realization gives square-looking interstices and alternating long-axis directions. These are contour and endpoint constructions, not native filled-face or equal-angle theorems.");
    }

    private static SourceRevision ExtendedOffsetSquares()
    {
        var cell = new Cell();
        var square = cell.CorrectedSeed(true,
            (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1));
        cell.CorrectedImage(square, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        return new(cell, (2, 1), (-1, 2),
            "One upright square outline exposes its four side-middle landmarks. Oblique period words offset neighbouring copies by a whole side and a half-side. Admitted half-side contacts leave smaller square circuits between the repeated outlines.",
            "Fig. 190's similarly oriented, displaced squares and smaller interstices. The finite sample has a slanted outer crop because the two periods are oblique. Gap area, occupied faces and exact engraving distances are not inferred by the native contour program.");
    }

    private static SourceRevision ExtendedTriangleMesh()
    {
        var cell = new Cell();
        var triangle = cell.CorrectedSeed(true, (0, 0), (2, 0), (0, 2), (0, 0));
        cell.CorrectedImage(triangle, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        cell.CorrectedImage(triangle, cell.CorrectedMap((-1, 0), (0, -1)), (2, 2));
        return new(cell, (2, 0), (0, 2),
                "Two opposed images of one triangle share their diagonal boundary. Repeating the pair supplies three direction families, and all intersections are exposed triangle-corner ports. Coincident shared-edge histories remain separate participations.",
                "Fig. 178's uniform three-family triangular network. The selected triangular screen basis makes the triangles equilateral-looking; the native word and seam construction does not claim angle calibration or deduplicate shared edges.");
    }

    private sealed partial class Cell
    {
        public void ExtendedReturn(CorrectedPathSeed seed, QualitativePatternTransform transform, int count)
        {
            var phases = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
            var cycle = new Relation(N.S(phases), N.S(phases.Select((phase, index) =>
                (IElement)new Relation(phase, phases[(index + 1) % phases.Length])).ToArray()));
            var orbit = new QualitativeWordOrbit(transform, N.S([seed.Start, .. seed.Chords]), cycle);
            if (!orbit.IsComplete) throw new InvalidOperationException("A retained source orbit must return every selected formal word.");
            RetainConstruction(orbit.Surface);
        }
    }
}
