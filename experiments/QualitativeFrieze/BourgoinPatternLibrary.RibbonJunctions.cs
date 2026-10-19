namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static SourceRevision AngledRibbonJunctions()
    {
        var cell = new Cell();
        // The short edge at a crossing has a different heading from the long
        // ribbon edge. Four images of this SAME native seed make both line
        // families and close the narrow crossing diamonds through port seams.
        var seed = cell.CorrectedSeed(false, (0, -2), (1, 0), (8, 4));
        var direct = cell.CorrectedMap((1, 0), (0, 1));
        var reflected = cell.CorrectedMap((-1, 0), (0, 1));
        cell.CorrectedImage(seed, direct, (0, 0));
        cell.CorrectedImage(seed, direct, (-1, 2));
        cell.CorrectedImage(seed, reflected, (0, 0));
        cell.CorrectedImage(seed, reflected, (1, 2));
        return new(cell, (8, 6), (-8, 6),
            "One short-steep/long-shallow two-chord ribbon seed supplies four translated or mirrored images. Exposed corners join into narrow upright crossing diamonds; the long edges bound the larger diamonds. Both period words belong to the same shared-reach frame.",
            "A second reading of Fig. 203's solid lines. Unlike B08's straight paired families, the short junction edges change heading. This is a finite angular skeleton with selected proportions, not a smooth-curve or exact raster tracing. Dotted guides are omitted.");
    }
}
