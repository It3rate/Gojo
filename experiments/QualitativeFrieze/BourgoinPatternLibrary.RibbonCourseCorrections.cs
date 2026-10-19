namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static SourceRevision InterruptedDiagonalKeys()
    {
        var cell = new Cell();
        // The source's dark strip becomes rectilinear in this local grid.
        // Its two returns have separate central free ends: (2,3) and (3,3).
        // They must never be silently joined into one long spiral traversal.
        // The first side exposes the end of the preceding row's hook. That
        // extra landmark splits the same straight run; it creates no new bend.
        var backbone = cell.CorrectedSeed(false, RibbonAligned(
            (-5, 5), (-1, 5), (0, 5), (0, 1), (0, 0)));
        var hook = cell.CorrectedSeed(false, RibbonAligned(
            (0, 5), (4, 5), (4, 2), (2, 2), (2, 3)));
        var diagonal = cell.CorrectedMap((1, -1), (1, 1));
        var opposed = cell.CorrectedMap((-1, 1), (-1, -1));
        cell.CorrectedImage(backbone, diagonal, (0, 0));
        cell.CorrectedImage(hook, diagonal, (0, 0));
        // Half-turn in the source grid about (2.5,3), compiled entirely as
        // signed repeated-reach substitution plus an actual placement word.
        cell.CorrectedImage(backbone, opposed, (-1, -11));
        cell.CorrectedImage(hook, opposed, (-1, -11));
        return new(cell, (10, 0), (5, -5),
            "Two distinct open hooked returns attach to opposed staircase courses. Their inner ends remain one source-grid step apart instead of becoming one continuous spiral. The half-turned pair reuses two actual prototypes. Successive courses move diagonally by a half-drop, placing the next crest into the preceding course's valley. An exposed straight-side landmark accepts the neighbouring hook endpoint through an actual seam; coincident boundary participations remain separate stroke histories.",
            "Reconstructed from Plate 20 fig. 3's dark strip and checked against its lighter repeated field. The repeated field uses a diagonal half-drop rather than vertically stacking the strip. Separate central ends and explicit valley T contacts remain intact. Shared-reach proportions are selected from the source; scan breaks, printed ink width and filled ribbon faces are not inferred.");
    }

    private static SourceRevision ContinuousAngledChannels()
    {
        var cell = new Cell();
        // One open course repeats along the strip. The second path returns
        // between TWO exposed sites on that course, creating the compact
        // diamond compartment without enclosing an L-shaped ribbon unit.
        var course = cell.CorrectedSeed(false, RibbonAligned(
            (0, 0), (0, 1), (0, 3), (2, 3), (2, 4), (-4, 4)));
        var attachment = cell.CorrectedSeed(false, RibbonAligned(
            (0, 1), (2, 1), (2, 3)));
        var diagonal = cell.CorrectedMap((1, -1), (1, 1));
        var reflected = cell.CorrectedMap((-1, -1), (-1, 1));
        cell.CorrectedImage(course, reflected, (0, 0));
        cell.CorrectedImage(attachment, reflected, (0, 0));
        cell.CorrectedImage(course, diagonal, (6, -4));
        cell.CorrectedImage(attachment, diagonal, (6, -4));
        return new(cell, (8, 0), (0, -8),
            "An open angular course repeats continuously along each strip. A separate two-chord attachment meets two published course sites, closing the small diamond. The lower field alternates actual reflected and unreflected courses at a half-drop; the two courses form one repeat and interlock without enclosing an L-shaped ribbon unit. Both T contacts on each course require explicit endpoint seam declarations.",
            "Reconstructed from Plate 20 fig. 7's dark line construction and repeated lighter field. The source's lower-field handedness and half-drop are retained rather than simply stacking the top strip at a full-height interval. The small diamond compartments and surrounding angled-channel disposition replace the earlier isolated folded-strip contours. White-passage continuity is a chosen-basis geometric reading of boundaries, not native filled-face inference; source shading, ink width and over/under weaving remain outside the program.");
    }

    // Inspection coordinates have y down. This is only an authoring boundary;
    // actual source paths, images and placements are native move-word graphs.
    private static (int X, int Y)[] RibbonAligned(params (int X, int Y)[] vertices) =>
        vertices.Select(vertex => (vertex.X, -vertex.Y)).ToArray();
}
