namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Authored construction examples. Captions choose examples for the
/// inspector; readers receive only the surface. Points are illustrative drawing
/// witnesses in an arbitrary upright frame, never inputs to structural proof.</summary>
public static class ConstructionAtlas
{
    public static IReadOnlyList<ConstructionExample> All { get; } = Array.AsReadOnly(Build().ToArray());
    public static IReadOnlyList<string> Patterns { get; } = Array.AsReadOnly(All.Select(e => e.Pattern).Distinct().ToArray());

    private static IEnumerable<ConstructionExample> Build()
    {
        foreach (var square in new[] { true, false })
        {
            var pattern = square ? "Square" : "Rectangle";
            PenPoint[] points = [new(0, 0), new(square ? 2 : 3, 0), new(square ? 2 : 3, 2), new(0, 2)];
            var links = Cycle(4, square ? [0, 0, 0, 0] : [0, 1, 0, 1]);
            yield return Make(pattern, "Repeated directed path",
                square ? "One shared reach is used on four explicitly bound quarter-turn lanes; the ordered walk returns."
                    : "Two shared reaches alternate on four explicitly bound quarter-turn lanes; opposite sides reuse their source.",
                points, links, [Forward(4)], (table, entry) => ConstructionBasis.Path(table, entry, [0, 1, 2, 3]));
            yield return Make(pattern, "Two ordered rails",
                square ? "Two perpendicular ordered rails bind their full gaps to the same field and define four sides. Separate boundary uses align each parallel pair instead of following the perimeter."
                    : "Two perpendicular ordered rails define four sides with unresolved aspect ratio. Separate boundary uses align each parallel pair instead of following the perimeter.",
                points, links, [[0], [2], [5], [7]], (table, entry) => ConstructionBasis.Rails(table, entry,
                    new OrdinalSketch(new LandmarkRun(1, [0, 1]), new LandmarkRun(1, [0, 1]),
                        [[0, 0], [1, 0], [1, 1], [0, 1]], true), [0, 1, 2, 3], [0, 1, 2, 3], square ? [0, 0] : null));
            yield return Make(pattern, "Radial parts and chords",
                square ? "Four equal shares of a closed radial traversal supply successive chord endpoints. The radius and traversal participate."
                    : "Four alternating shared arc parts supply two opposite point pairs on a closed radial traversal; successive chords form a rectangle.",
                points, links, [Forward(4)], (table, entry) => ConstructionBasis.Radial(table, entry,
                    RepetitionExamples.Contour(4, square, true), [1, 5, 6, 7], [0, 1, 2, 3]));
        }

        foreach (var (name, count) in new[] { ("Triangle", 3), ("Pentagon", 5), ("Hexagon", 6) })
        {
            var points = Polygon(count);
            yield return Make(name, "Radial equal divisions",
                "A positive closed radial traversal shares one arc-part source around the whole. Successive chords give a regular polygon under that selected basis.",
                points, Cycle(count, new int[count]), [Forward(count)], (table, entry) => ConstructionBasis.Radial(table, entry,
                    RepetitionExamples.Contour(count, true, true), ContourSites(count), Enumerable.Range(0, count).ToArray()));
            yield return Make(name, "Separate side walks",
                "Separate one-edge walks meet at shared endpoints. This establishes the boundary graph; the regular-looking picture supplies no equal-angle or equal-length proof.",
                points, Cycle(count, Enumerable.Range(0, count).ToArray()),
                Enumerable.Range(0, count).Select(i => new[] { i * 2 }).ToArray());
        }

        PenPoint[] meander = [new(0, 0), new(0, 1), new(1, 1), new(1, 0), new(2, 0),
            new(2, 1), new(3, 1), new(3, 0), new(4, 0)];
        var meanderLinks = Chain(9, [0, 1, 0, 1, 0, 1, 0, 1]);
        yield return Make("Meander", "One continuous walk", "One open walk reuses two reach sources. Turns and spacing in the picture remain illustrative without a geometric basis.",
            meander, meanderLinks, [Forward(8)]);
        yield return Make("Meander", "Two linked portions", "Two four-step portions meet at the same intermediate site and reuse the same two fields. Grouping preserves the original links.",
            meander, meanderLinks, [[0, 2, 4, 6], [8, 10, 12, 14]]);

        PenPoint[] fret = [new(0, 0), new(0, 3), new(4, 3), new(4, 0), new(1, 0),
            new(1, 2), new(3, 2), new(3, 1), new(2, 1)];
        var fretLinks = Chain(9, Enumerable.Range(0, 8).ToArray());
        yield return Make("Fret", "Inward walk", "Eight independent reaches form one open inward-looking walk. The outline does not yet prove equal whitespace or inward motion.",
            fret, fretLinks, [Forward(8)]);
        yield return Make("Fret", "Outer and inner portions", "An outer portion and an inner portion meet at their shared cut. No new edge or copied value is introduced at the cut.",
            fret, fretLinks, [[0, 2, 4, 6], [8, 10, 12, 14]]);

        PenPoint[] y = [new(0, 0), new(0, 1), new(-1, 2), new(1, 2)];
        FamilyLink[] yLinks = [new(0, 1, 0), new(1, 2, 1), new(1, 3, 2)];
        yield return Make("Y", "Branch reading", "Two possible walks begin on one shared stem and leave through different arms. These are directed reads, not a rule that duplicates material.",
            y, yLinks, [[0, 2], [0, 4]]);
        yield return Make("Y", "Merge reading", "The two arm-to-stem walks meet at the shared junction and reuse the same outgoing stem. A physical merge still needs its transfer law.",
            y, yLinks, [[3, 1], [5, 1]]);

        PenPoint[] t = [new(-1, 1), new(0, 1), new(1, 1), new(0, 0)];
        FamilyLink[] tLinks = [new(0, 1, 0), new(1, 2, 1), new(3, 1, 2)];
        yield return Make("T", "Across path and stem", "The top is one two-link walk; the stem terminates at its interior site. The selected decomposition preserves a through route.",
            t, tLinks, [[0, 2], [4]]);
        yield return Make("T", "Two corner walks", "A walk from each top end turns into the same stem. The common stem is referenced twice, and remains one source link.",
            t, tLinks, [[0, 5], [3, 5]]);

        PenPoint[] h = [new(0, 0), new(0, 1), new(0, 2), new(2, 0), new(2, 1), new(2, 2)];
        FamilyLink[] hLinks = [new(0, 1, 0), new(1, 2, 1), new(3, 4, 2), new(4, 5, 3), new(1, 4, 4)];
        yield return Make("H", "Uprights and bridge", "Two three-site upright walks share their interior landmarks with a bridge. Interior attachment is explicit in the use order.",
            h, hLinks, [[0, 2], [4, 6], [8]]);
        yield return Make("H", "Two crossing walks", "Two alternative endpoint-to-endpoint walks reuse the bridge while choosing different upright halves. They retain all five source links.",
            h, hLinks, [[0, 8, 6], [3, 8, 5]]);

        PenPoint[] a = [new(-1, 0), new(-0.5, 1), new(0, 2), new(0.5, 1), new(1, 0)];
        FamilyLink[] aLinks = [new(0, 1, 0), new(1, 2, 1), new(2, 3, 2), new(3, 4, 3), new(1, 3, 4)];
        yield return Make("A", "Legs and crossbar", "Two ordered legs meet at the apex. The crossbar joins interior sites on the legs, leaving both feet outside the resulting loop.",
            a, aLinks, [[0, 2], [4, 6], [8]]);
        yield return Make("A", "Loop and feet", "One closed walk uses the upper leg portions and the reversed crossbar. Two separate lower portions attach as feet to that same graph.",
            a, aLinks, [[2, 4, 9], [0], [6]]);

        PenPoint[] house = [new(0, 1), new(2, 1), new(1, 2), new(0, 0), new(2, 0)];
        FamilyLink[] houseLinks = [new(0, 1, 0), new(1, 2, 1), new(2, 0, 2), new(0, 3, 3), new(3, 4, 4), new(4, 1, 5)];
        yield return Make("House", "Roof and room share seam", "The roof and room close separately and traverse one common seam in opposite directions. Cancellation in a boundary read does not delete either face.",
            house, houseLinks, [[0, 2, 4], [1, 6, 8, 10]]);
        yield return Make("House", "Coincident independent seams", "The two faces have matching seam endpoints, but each owns a separate link and field. The coincident picture is not shared-edge identity.",
            house, [.. houseLinks, new(0, 1, 6)], [[0, 2, 4], [13, 6, 8, 10]]);
        yield return Make("House", "Outer boundary and seam", "The outer boundary is one closed walk. A separate use exposes the interior seam; the construction keeps the same six source links as the shared-face example.",
            house, houseLinks, [[2, 4, 6, 8, 10], [0]]);

        PenPoint[] church = [new(0, 0), new(3, 0), new(4, 0), new(4, 4), new(3.5, 5),
            new(3, 4), new(3, 2), new(1.5, 3), new(0, 2)];
        FamilyLink[] churchLinks = [new(0, 1, 0), new(1, 2, 1), new(2, 3, 2), new(3, 4, 3),
            new(4, 5, 4), new(5, 6, 5), new(6, 7, 6), new(7, 8, 7), new(8, 0, 8),
            new(1, 6, 9), new(8, 6, 10), new(5, 3, 11)];
        yield return Make("Church", "Body roof tower and spire", "Four closed walks share actual seams. This is an authored church schematic for composition tests, not an imported or recognized drawing.",
            church, churchLinks, [[0, 18, 21, 16], [20, 12, 14], [2, 4, 23, 10, 19], [6, 8, 22]]);
        yield return Make("Church", "Outer boundary and interiors", "One closed outer walk and three interior seam uses expose the same twelve links through a different grouping. Metric proportions remain uncommitted.",
            church, churchLinks, [Forward(9), [18], [20], [22]]);

        PenPoint[] cross = [new(-1, 0), new(0, 0), new(1, 0), new(0, -1), new(0, 1)];
        yield return Make("Cross", "Joined crossing", "Both paths pass through the very same interior site. Their four arms form one connected junction, independent of the screen placement.",
            cross, [new(0, 1, 0), new(1, 2, 1), new(3, 1, 2), new(1, 4, 3)], [[0, 2], [4, 6]]);
        yield return Make("Cross", "Separate projected crossing", "The two interior sites are distinct even though the drawing places them together. Projection coincidence does not connect the strands.",
            [new(-1, 0), new(0, 0), new(1, 0), new(0, -1), new(0, 0), new(0, 1)],
            [new(0, 1, 0), new(1, 2, 1), new(3, 4, 2), new(4, 5, 3)], [[0, 2], [4, 6]]);
    }

    private static ConstructionExample Make(string pattern, string method, string description, PenPoint[] points,
        FamilyLink[] links, int[][] groups, Func<FamilyTable, FamilyEntry, ConstructionBasis?>? basis = null)
    {
        var fieldCount = links.Max(link => link.Field) + 1;
        var template = new FamilyTemplate(Enumerable.Repeat(new FamilyField(0, 0), fieldCount).ToArray(), links);
        var table = new FamilyTable(template);
        var entry = table.Append(1, Enumerable.Repeat(1UL, fieldCount).ToArray());
        return new(pattern, method, description,
            new(table, entry, new FamilyUseTable(template, groups), basis?.Invoke(table, entry)), points.ToArray());
    }

    private static FamilyLink[] Cycle(int count, int[] fields) => Enumerable.Range(0, count)
        .Select(i => new FamilyLink(i, (i + 1) % count, fields[i])).ToArray();
    private static FamilyLink[] Chain(int sites, int[] fields) => Enumerable.Range(0, sites - 1)
        .Select(i => new FamilyLink(i, i + 1, fields[i])).ToArray();
    private static int[] Forward(int count) => Enumerable.Range(0, count).Select(i => i * 2).ToArray();
    private static int[] ContourSites(int count) => [ContourPattern.Start, .. Enumerable.Range(5, count - 1)];
    private static PenPoint[] Polygon(int count) => Enumerable.Range(0, count)
        .Select(i => new PenPoint(Math.Cos(Math.PI / 2 + 2 * Math.PI * i / count),
            Math.Sin(Math.PI / 2 + 2 * Math.PI * i / count))).ToArray();
}
