using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static SourceRevision FlatStarFlorets()
    {
        var cell = new Cell();
        var boundary = cell.FlatStarBoundary();
        cell.CorrectedImage(boundary, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        // The two local stem definitions alternate around the centre. Their
        // twelve actual images remain distinct even where a coarse read would
        // group both stems into one sector of the six-part boundary.
        var first = cell.CorrectedSeed(false, (0, 0), (2, 0));
        var second = cell.CorrectedSeed(false, (0, 0), (1, 1));
        foreach (var turn in cell.ExtendedSixTurns())
        {
            cell.CorrectedImage(first, turn, (0, 0));
            cell.CorrectedImage(second, turn, (0, 0));
        }
        return new(cell, (12, -6), (6, 6),
            "A coarse six-chord hexagon fixes the six flat-tip midpoint roles. Each coarse chord is replaced by an endpoint-preserving four-chord chain that pulls the old corner inward and retains the two neighbouring half-flat tips. One actual refined boundary repeats on the hexagonal neighbour disposition. Small six-sided gaps emerge at three-cell meetings; no small hexagon motif is placed there. Two stem prototypes each have six actual word-substitution images, producing twelve centre rays.",
            "A revised skeleton of Plate 13 fig. 8: six flat tips, six inward corners, small hexagonal interstices and twelve radial floret participants replace the earlier plain-hexagon/six-spoke reading. The source's curved petal heads, coloured bands, exact proportions and filled regions remain outside this thin-line construction. The triangular screen basis is an optional interpretation, not a native angle calibration.");
    }

    private static SourceRevision OutwardArmWindows()
    {
        var cell = new Cell();
        // A three-chord arm has one short proximal stem, a transverse wrist,
        // and a long terminal hand. The hand ends farther from its own actual
        // junction than the wrist; it is not an inward spiral or thick filled
        // arm outline. Repetition puts the marker between four junctions.
        var arm = cell.CorrectedSeed(false, (0, 0), (0, 3), (6, 3), (6, -9));
        foreach (var turn in new[]
        {
            cell.CorrectedMap((1, 0), (0, 1)), cell.CorrectedMap((0, 1), (-1, 0)),
            cell.CorrectedMap((-1, 0), (0, -1)), cell.CorrectedMap((0, -1), (1, 0))
        }) cell.CorrectedImage(arm, turn, (-12, 0));

        // This small round approximation is actual closed chord structure.
        // Axis-midpoint subdivisions keep twelve chord participants; some
        // consecutive facets are collinear at this intentionally coarse scale.
        // It supplies neither an analytic circle nor occupied native area.
        var marker = cell.CorrectedSeed(true, (2, 0), (2, 1), (1, 2), (0, 2),
            (-1, 2), (-2, 1), (-2, 0), (-2, -1), (-1, -2), (0, -2), (1, -2), (2, -1), (2, 0));
        cell.CorrectedImage(marker, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
        return new(cell, (24, 0), (12, 12),
            "Four actual quarter-related images reuse one slender three-chord arm. Their proximal roles share a junction, their two successive turns retain one handedness, and the long terminal hands travel outward beyond the wrists. The staggered junction disposition surrounds a separate central round marker. That marker is an actual twelve-chord closed native contour, not an SVG circle substituted for a diamond.",
            "A simplified source-aligned reading of Plate 19 fig. 3's slender turning arms and round interstitial dots. It replaces the previous heavy closed key-window outline. Selected arm proportions and the source's short weave breaks are not traced exactly; no over/under order is claimed. The coarse round marker has flat facets and collinear subdivisions, not analytic-circle semantics or an inferred occupied face.");
    }

    private sealed partial class Cell
    {
        public CorrectedPathSeed FlatStarBoundary()
        {
            // Midpoints of the original coarse honeycomb sides become the
            // six actual landmarks of this smaller, rotated coarse hexagon.
            // The outer honeycomb corners need not survive as material sites.
            (int X, int Y)[] firstChain = [(3, 3), (2, 4), (0, 4), (-2, 6), (-3, 6)];
            var chains = new (int X, int Y)[6][];
            chains[0] = firstChain;
            for (var part = 1; part < chains.Length; part++)
                chains[part] = chains[part - 1].Select(point => (X: -point.Y, Y: point.X + point.Y)).ToArray();
            var coarseWords = chains.Select(chain => Word((chain[^1].X - chain[0].X, chain[^1].Y - chain[0].Y))).ToArray();
            var path = new Relation(Word(chains[0][0]), N.S(coarseWords));
            var ports = N.S(Enumerable.Range(0, 7).Select(index => (IElement)new Relation(new Placeholder(),
                new Relation(path, index == 6 ? path : coarseWords[index]))).ToArray());
            var coarse = new UnitArrangementProgram(frame, "coarse hexagonal flat-tip landmarks", N.S(path), ports, N.S(path));
            var replacements = N.S(chains.Select((chain, index) => (IElement)new Relation(coarseWords[index],
                N.S(Enumerable.Range(0, chain.Length - 1).Select(part => (IElement)Word(
                    (chain[part + 1].X - chain[part].X, chain[part + 1].Y - chain[part].Y))).ToArray()))).ToArray());
            var refined = coarse.Refine("six flat tips and inward corners", replacements);
            RetainConstruction(refined.Surface);
            var finePath = (Relation)N.E(refined.Paths).Single();
            return new(new Relation(finePath, N.S(finePath)), (Relation)finePath.Left,
                N.E((Relation)finePath.Right).Cast<Relation>().ToArray(), true);
        }
    }
}
