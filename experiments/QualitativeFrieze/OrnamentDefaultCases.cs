using Spec = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Spec;
using Sample = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Sample;
using PathSpec = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.PathSpec;
using Definition = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Definition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored alternatives for studying requirements and defeasible
/// defaults. Every drawing is executed by the native uncalibrated word/path,
/// public-port connection and publication machinery. Family, names and notes
/// describe the experiment; no classification or expected verdict is supplied
/// to a reader. A recipe can be transported and run without these factories.</summary>
public static class OrnamentDefaultCases
{
    public sealed record Case(string Id, string Family, string Name, string ConstructionNote, Spec Spec)
    {
        public Sample Create() => Spec.Create();
    }

    public sealed class Family
    {
        public string Id { get; }
        public string Name { get; }
        public string ReferenceId { get; }
        public string RepertoireNote { get; }
        private readonly Spec core;
        internal Family(string id, string name, string referenceId, string note, Spec core)
        { Id = id; Name = name; ReferenceId = referenceId; RepertoireNote = note; this.core = core; }
        /// <summary>Small connected native unit available for a deliberately
        /// broad partial query. This does not declare which whole designs fit.</summary>
        public Sample Core() => core.Create();
    }

    public static IReadOnlyList<Case> All { get; } = Build();
    public static IReadOnlyList<Family> Families { get; } = BuildFamilies();
    public static Sample Create(string id) => All.Single(item => item.Id == id).Create();

    private static IReadOnlyList<Family> BuildFamilies() => new[]
    {
        F("LOOP", "Closed compartment", "C01", "Square, diamond and notched compartment vocabulary in the Bourgoin atlas.",
            () => new([Ring((0, 0), (3, 0), (1, 2))])),
        F("LOBES", "Hub and lobes", "P01", "Radial lozenge and rosette scaffolds; petal multiplicity is an optional distinction.",
            () => new([Ring((0, 0), (2, 0), (1, 2)), Ring((0, 0), (-2, 0), (-1, -2))])),
        F("LADDER", "Rails and bridges", "L01", "Repeated rail/rung construction related to band and rectangular mesh ornaments.",
            () => Ladder(1, 3, 2)),
        F("THETA", "Two poles and routes", "T01", "Three-route bridge and enclosed counter-form; a theta-shaped connection grammar.",
            () => Theta(3, 2)),
        F("MEANDER", "Returning course", "M01", "Detached S-shaped frets and stepped Greek-key courses, as in Bourgoin B09.",
            () => new([Open((0, 0), (0, 2), (2, 2))])),
        F("STAR", "Pointed closed contour", "S01", "Four-, six- and eight-point contour vocabulary related to Bourgoin junctions and rosettes.",
            () => new([Ring((0, 0), (3, 0), (1, 2))]))
    };

    private static Family F(string id, string name, string reference, string note, Func<Definition> core) =>
        new(id, name, reference, note, Spec.Make("CORE-" + id, id, name + " connected core", note, 0, 0, 1, core));

    private static IReadOnlyList<Case> Build()
    {
        var result = new List<Case>();
        Add("C01", "LOOP", "Square compartment", "One closed turning path; no metric size is published.",
            () => new([Ring((0, 0), (4, 0), (4, 4), (0, 4))]));
        Add("C02", "LOOP", "Broad rectangle", "A changed reach keeps the enclosure and four corners.",
            () => new([Ring((0, 0), (8, 0), (8, 4), (0, 4))]));
        Add("C03", "LOOP", "Slanted compartment", "The closed course is sheared without exchanging its vertices.",
            () => new([Ring((0, 0), (5, 0), (7, 4), (2, 4))]));
        Add("C04", "LOOP", "Diamond compartment", "The same four-corner incidence is presented on diagonal courses.",
            () => new([Ring((0, 3), (3, 6), (6, 3), (3, 0))]));
        Add("C05", "LOOP", "Notched compartment", "An inward turn changes the boundary while keeping one closure.",
            () => new([Ring((0, 0), (6, 0), (6, 6), (4, 6), (4, 4), (2, 4), (2, 6), (0, 6))]));
        Add("C06", "LOOP", "Four separately joined sides", "The visible enclosure is retained; its mechanical groups become four independent sides.",
            () => Split(new([Ring((0, 0), (4, 0), (4, 4), (0, 4))])));
        Add("C07", "LOOP", "Unjoined coincident sides", "Matching presentation positions do not bind the independent endpoints.",
            () => Split(new([Ring((0, 0), (4, 0), (4, 4), (0, 4))])) with { JoinCoincident = false });
        Add("C08", "LOOP", "Overlapping compartments", "Two offset enclosures overlap in the embedding; neither interior is published as fill.",
            () => new([Ring((0, 0), (4, 0), (4, 4), (0, 4)), Ring((2, 2), (6, 2), (6, 6), (2, 6))]));
        Add("C09", "LOOP", "Crossed bow-tie contour", "One closed course crosses itself without an authored shared crossing site.",
            () => new([Ring((0, 0), (4, 4), (0, 4), (4, 0))]));
        Add("C10", "LOOP", "Very thin compartment", "A long enclosure still has two separated sides, but at coarse display scale it may read as a line.",
            () => new([Ring((0, 0), (24, 0), (24, 1), (0, 1))]));
        Add("C11", "LOOP", "Open compartment", "The returning side is absent; there is no closed-path claim.",
            () => new([Open((0, 0), (4, 0), (4, 4), (0, 4))]));
        Add("C12", "LOOP", "Enclosure with an outgoing branch", "A tail shares a real corner with the closed boundary.",
            () => new([Ring((0, 0), (4, 0), (4, 4), (0, 4)), Open((4, 4), (7, 6))]));

        Add("P01", "LOBES", "Four-lobe lozenge wheel", "Four closed siblings share a hub and attach to one closed outer contour.",
            () => Rosette(4));
        Add("P02", "LOBES", "Two opposed petals", "The shared-hub grammar remains but only two petal groups are present.",
            () => Rosette(2));
        Add("P03", "LOBES", "Six petals", "Six independent closed siblings meet the hub and outer contour.",
            () => SixLobes());
        Add("P04", "LOBES", "Four long spindles", "Equal rotated siblings use long narrow shoulders.",
            () => Rosette(4, Ring((0, 0), (1, 1), (1, 8), (0, 7)), 2));
        Add("P05", "LOBES", "Alternating petal reaches", "Opposed siblings agree while adjacent siblings use different reach words.",
            () => Rosette([Petal(), Ring((0, 0), (1, 1), (1, 5), (0, 4)), Petal(), Ring((0, 0), (1, 1), (1, 5), (0, 4))], [0, 1, 2, 3], 2));
        Add("P06", "LOBES", "Reflected lozenge wheel", "Reflection changes handedness without withdrawing the hub or rim joins.",
            () => Map(Rosette(4), point => (-point.X, point.Y)));
        Add("P07", "LOBES", "Four triangular petals", "Each sibling has fewer turns, but still has two hub-to-rim routes.",
            () => Rosette(4, Ring((0, 0), (3, 2), (1, 2)), 1));
        Add("P08", "LOBES", "Detached outer contour", "The rim has its own closure but misses all four intended attachments.",
            () => Rim(Rosette(4), path => Shift(path, 1, 1)));
        Add("P09", "LOBES", "Coincident unjoined petals", "Petals and rim share positions without declared public-port joins.",
            () => Rosette(4) with { JoinCoincident = false });
        Add("P10", "LOBES", "Unequal petal fan", "Four loops keep a common hub while their shoulders spread into the same half-plane.",
            Fan);
        Add("P11", "LOBES", "Open outer contour", "One rim side is removed while all four petal closures remain.",
            () => Rim(Rosette(4), path => new(path.Points[..^1])));
        Add("P12", "LOBES", "Overlapping petals", "Broad shoulders push neighboring lobes across each other without additional topology.",
            () => Rosette(4, Ring((0, 0), (5, -1), (3, 4), (-1, 3)), 2));

        Add("L01", "LADDER", "Three-bay ladder", "Four rungs join two parallel rails through explicit shared endpoints.",
            () => Ladder(3, 3, 4));
        Add("L02", "LADDER", "Two-bay ladder", "One repeated bay and one rung are absent; the rail/bridge grammar remains.",
            () => Ladder(2, 3, 4));
        Add("L03", "LADDER", "Four-bay ladder", "A further rung and bay extend the same arrangement.",
            () => Ladder(4, 3, 4));
        Add("L04", "LADDER", "Oblique ladder", "A sheared embedding retains its rail-to-rung incidence.",
            () => Map(Ladder(3, 3, 4), point => (point.X + point.Y, point.Y)));
        Add("L05", "LADDER", "Narrow ladder", "The rail spacing is a small word relative to each bay reach.",
            () => Ladder(3, 7, 1));
        Add("L06", "LADDER", "Wide short ladder", "The rail separation dominates the progression reach.",
            () => Ladder(3, 1, 8));
        Add("L07", "LADDER", "Missing middle rung", "One internal bridge is withdrawn without moving the rails.",
            () => Remove(Ladder(3, 3, 4), 3));
        Add("L08", "LADDER", "Unjoined rung endpoints", "The rungs touch rail landmarks only in the embedding; no cross-path joins exist.",
            () => Ladder(3, 3, 4) with { JoinCoincident = false });
        Add("L09", "LADDER", "Crossed internal braces", "Additional diagonal courses cross without a joined central site.",
            () => Append(Ladder(3, 3, 4), Open((3, 0), (6, 4)), Open((3, 4), (6, 0))) with { Crossings = [(6, 7)] });
        Add("L10", "LADDER", "Retraced rung", "A second mechanical chord repeats one rung in the opposed order.",
            () => Append(Ladder(3, 3, 4), Open((3, 4), (3, 0))));
        Add("L11", "LADDER", "Stepped rail lattice", "Both rails bend together, preserving the sequence of bridges.",
            () => new([Open((0, 0), (3, 0), (3, 2), (6, 2)), Open((0, 4), (3, 4), (3, 6), (6, 6)),
                Open((0, 0), (0, 4)), Open((3, 0), (3, 4)), Open((3, 2), (3, 6)), Open((6, 2), (6, 6))]));
        Add("L12", "LADDER", "Quarter-turned ladder", "The native basis rotates the complete joined construction.",
            () => Ladder(3, 3, 4), transform: 1);

        Add("T01", "THETA", "Three-route bridge", "Upper, middle and lower courses share both terminal sites.", () => Theta(6, 3));
        Add("T02", "THETA", "Angular lens bridge", "The upper and lower routes use diagonal shoulders.",
            () => new([Open((0, 0), (3, 3), (6, 0)), Open((0, 0), (6, 0)), Open((0, 0), (3, -3), (6, 0))]));
        Add("T03", "THETA", "Unequal bridge chambers", "Upper and lower reaches differ while the two terminal connections remain.",
            () => new([Open((0, 0), (0, 6), (6, 6), (6, 0)), Open((0, 0), (6, 0)), Open((0, 0), (0, -2), (6, -2), (6, 0))]));
        Add("T04", "THETA", "Three equal diagonal routes", "Intermediate waypoints move while the three routes keep their common ends.",
            () => new([Open((0, 0), (2, 4), (6, 2)), Open((0, 0), (3, 1), (6, 2)), Open((0, 0), (4, -2), (6, 2))]));
        Add("T05", "THETA", "Bridge without its central route", "The upper and lower courses alone form a single enclosed circuit.",
            () => Remove(Theta(6, 3), 1));
        Add("T06", "THETA", "Fourth terminal route", "An extra detour joins the same two terminal sites.",
            () => Append(Theta(6, 3), Open((0, 0), (1, 5), (5, 5), (6, 0))));
        Add("T07", "THETA", "Unjoined terminal contacts", "All three courses share terminal positions but retain distinct site identities.",
            () => Theta(6, 3) with { JoinCoincident = false });
        Add("T08", "THETA", "Bridge ending short", "The central course fails to reach the second pole.",
            () => new([Open((0, 0), (0, 3), (6, 3), (6, 0)), Open((0, 0), (5, 0)), Open((0, 0), (0, -3), (6, -3), (6, 0))]));
        Add("T09", "THETA", "Thin three-route bridge", "The chamber depths are small compared with the shared reach.", () => Theta(24, 1));
        Add("T10", "THETA", "Retraced middle bridge", "An opposed course repeats the central route exactly.",
            () => Append(Theta(6, 3), Open((6, 0), (0, 0))));
        Add("T11", "THETA", "Crossed detour bridge", "One route crosses the middle course before reaching the second pole.",
            () => new([Open((0, 0), (2, 3), (4, -3), (6, 0)), Open((0, 0), (6, 0)), Open((0, 0), (0, -4), (6, -4), (6, 0))]));
        Add("T12", "THETA", "Bridge with an outgoing stem", "The second pole acquires a free branch beyond the three connected routes.",
            () => Append(Theta(6, 3), Open((6, 0), (9, 0))));

        Add("M01", "MEANDER", "Returning S fret", "One ordered open path returns inside its earlier course and then advances.", Fret);
        Add("M02", "MEANDER", "Reflected S fret", "The opposite-handed path preserves a single connected course.", () => Map(Fret(), point => (-point.X, point.Y)));
        Add("M03", "MEANDER", "Long returning fret", "Longitudinal reach increases without adding turns.", () => Map(Fret(), point => (point.X * 3, point.Y)));
        Add("M04", "MEANDER", "Tall returning fret", "The returning course becomes taller while its order stays fixed.", () => Map(Fret(), point => (point.X, point.Y * 3)));
        Add("M05", "MEANDER", "Chamfered returning fret", "Two corners become short diagonal transitions.",
            () => new([Open((0, 0), (0, 3), (1, 4), (5, 4), (5, 2), (2, 2), (2, 1), (3, 0), (6, 0))]));
        Add("M06", "MEANDER", "Separated-axis square wave", "A single course alternates levels without the returning inner arm.",
            () => new([Open((0, 0), (1, 0), (1, 3), (3, 3), (3, 0), (5, 0), (5, 3), (6, 3))]));
        Add("M07", "MEANDER", "Diagonal zigzag course", "Horizontal and vertical advance occur together rather than as separate corners.",
            () => new([Open((0, 0), (2, 3), (4, 0), (6, 3), (8, 0))]));
        Add("M08", "MEANDER", "Dense notched course", "Repeated short returns add many turns to a still-readable advancing spine.",
            () => new([Open((0, 0), (0, 2), (1, 2), (1, 1), (2, 1), (2, 2), (3, 2), (3, 1), (4, 1), (4, 2), (5, 2), (5, 0), (6, 0))]));
        Add("M09", "MEANDER", "Retraced inner arm", "The path doubles back over an already emitted chord before advancing.",
            () => new([Open((0, 0), (0, 4), (5, 4), (5, 2), (2, 2), (5, 2), (5, 0), (6, 0))]));
        Add("M10", "MEANDER", "Disconnected fret halves", "A small gap splits what otherwise suggests a returning course.",
            () => new([Open((0, 0), (0, 4), (5, 4), (5, 2)), Open((4, 2), (2, 2), (2, 0), (6, 0))]));
        Add("M11", "MEANDER", "Closed returning fret", "The final endpoint connects back to the first and turns the course into a closure.",
            () => new([Ring((0, 0), (0, 4), (5, 4), (5, 2), (2, 2), (2, 1), (6, 1), (6, -1), (0, -1))]));
        Add("M12", "MEANDER", "Two joined returning frets", "A second reflected course begins at the actual endpoint of the first.",
            () => new([Open((0, 0), (0, 4), (5, 4), (5, 2), (2, 2), (2, 0), (6, 0)),
                Open((6, 0), (6, -4), (11, -4), (11, -2), (8, -2), (8, 0), (12, 0))]));

        Add("S01", "STAR", "Four-point star contour", "Four long tips alternate with four inward shoulders on one closed path.",
            () => new([Star()]));
        Add("S02", "STAR", "Wide four-point star", "An uncalibrated horizontal reach changes the proportions while preserving tip order.",
            () => Map(new([Star()]), point => (point.X * 2, point.Y)));
        Add("S03", "STAR", "Quarter-turned star", "The entire contour uses the next native basis orientation.",
            () => new([Star()]), transform: 1);
        Add("S04", "STAR", "Unequal four-tip star", "One tip reaches farther than the others; no symmetry is imposed by the path alone.",
            () => new([Ring((0, 8), (2, 2), (6, 0), (2, -2), (0, -6), (-2, -2), (-6, 0), (-2, 2))]));
        Add("S05", "STAR", "Six-point star contour", "Six outward tips alternate with six inward shoulders.",
            () => new([Ring((0, 8), (2, 3), (7, 4), (4, 0), (7, -4), (2, -3), (0, -8), (-2, -3), (-7, -4), (-4, 0), (-7, 4), (-2, 3))]));
        Add("S06", "STAR", "Eight-point star contour", "Eight outward tips alternate with eight inward shoulders.",
            () => new([Ring((0, 8), (2, 4), (6, 6), (4, 2), (8, 0), (4, -2), (6, -6), (2, -4),
                (0, -8), (-2, -4), (-6, -6), (-4, -2), (-8, 0), (-4, 2), (-6, 6), (-2, 4))]));
        Add("S07", "STAR", "Softly notched diamond", "Inward shoulders approach the tip-to-tip chords, making the star distinction weaker.",
            () => new([Ring((0, 6), (2, 3), (6, 0), (3, -2), (0, -6), (-2, -3), (-6, 0), (-3, 2))]));
        Add("S08", "STAR", "Star built from separate joined edges", "Every side becomes a separate mechanical group while all corner sites remain joined.",
            () => Split(new([Star()])));
        Add("S09", "STAR", "Open star contour", "The returning chord is absent and the two terminal sites remain exposed.",
            () => new([new PathSpec(Star().Points[..^1])]));
        Add("S10", "STAR", "Crossed star ordering", "A different traversal visits the same broad landmarks but crosses its own course.",
            () => new([Ring((0, 6), (2, -2), (-6, 0), (2, 2), (0, -6), (-2, 2), (6, 0), (-2, -2))]));
        Add("S11", "STAR", "Needle star", "The horizontal reach dominates while the vertical reach remains a short word.",
            () => Map(new([Star()]), point => (point.X * 4, point.Y)));
        Add("S12", "STAR", "Star with a detached inner diamond", "An additional closed motif sits inside the star without sharing a site.",
            () => new([Star(), Ring((0, 1), (1, 0), (0, -1), (-1, 0))]));
        return result.AsReadOnly();

        void Add(string id, string family, string name, string note, Func<Definition> build, int transform = 0)
        {
            var spec = Spec.Make(id, family, name, "Native default/counterfactual study; repertoire-inspired authored construction", transform, 0, 1, build);
            result.Add(new(id, family, name, note, spec));
        }
    }

    private static PathSpec Open(params (int X, int Y)[] points) => new(points);
    private static PathSpec Ring(params (int X, int Y)[] points) => new([.. points, points[0]], true);
    private static PathSpec Petal() => Ring((0, 0), (2, 1), (2, 4), (0, 3));
    private static PathSpec Star() => Ring((0, 6), (2, 2), (6, 0), (2, -2), (0, -6), (-2, -2), (-6, 0), (-2, 2));
    private static Definition Fret() => new([Open((0, 0), (0, 4), (5, 4), (5, 2), (2, 2), (2, 0), (6, 0))]);
    private static Definition Theta(int width, int height) => new([
        Open((0, 0), (0, height), (width, height), (width, 0)), Open((0, 0), (width, 0)),
        Open((0, 0), (0, -height), (width, -height), (width, 0))]);
    private static Definition Ladder(int bays, int reach, int depth)
    {
        var points = Enumerable.Range(0, bays + 1).Select(index => (X: index * reach, Y: 0)).ToArray();
        return new([new(points), new(points.Select(point => (point.X, depth)).ToArray()),
            .. points.Select(point => Open(point, (point.X, depth)))]);
    }

    private static Definition Rosette(int count, PathSpec? seed = null, int tip = 2) =>
        Rosette(Enumerable.Repeat(seed ?? Petal(), count).ToArray(), count == 2 ? [0, 2] : [0, 1, 2, 3], tip);
    private static Definition Rosette(PathSpec[] seeds, int[] turns, int tip)
    {
        var petals = seeds.Select((seed, index) => new PathSpec(seed.Points.Select(point => Quarter(point, turns[index])).ToArray(), true)).ToArray();
        var tips = petals.Select(petal => petal.Points[tip]).ToArray();
        // With only two petal tips, the surrounding course still uses an
        // actual nondegenerate loop rather than a doubled single chord.
        var rim = petals.Length == 2 ? Ring(tips[0], (6, -3), tips[1], (-6, 3)) : Ring(tips);
        return new([.. petals, rim]);
    }
    private static Definition SixLobes()
    {
        var vectors = new[] { (X: 0, Y: 8), (X: 8, Y: 4), (X: 8, Y: -4), (X: 0, Y: -8), (X: -8, Y: -4), (X: -8, Y: 4) };
        var petals = vectors.Select(v => Ring((0, 0), (v.X / 2 - Math.Sign(v.Y), v.Y / 2 + Math.Sign(v.X)),
            v, (v.X / 2 + Math.Sign(v.Y), v.Y / 2 - Math.Sign(v.X)))).ToArray();
        return new([.. petals, Ring(vectors)]);
    }
    private static Definition Fan()
    {
        var tips = new[] { -9, -3, 3, 9 };
        var petals = tips.Select(x => Ring((0, 0), (x / 3 - 1, 4), (x, 12), (x / 3 + 1, 4))).ToArray();
        return new([.. petals, Ring((-9, 12), (-3, 12), (3, 12), (9, 12), (12, -3), (-12, -3))]);
    }
    private static (int X, int Y) Quarter((int X, int Y) point, int turn) => turn switch
    { 0 => point, 1 => (-point.Y, point.X), 2 => (-point.X, -point.Y), _ => (point.Y, -point.X) };
    private static PathSpec Shift(PathSpec path, int x, int y) => new(path.Points.Select(p => (p.X + x, p.Y + y)).ToArray(), path.Closed);
    private static Definition Rim(Definition source, Func<PathSpec, PathSpec> change) => source with { Paths = [.. source.Paths[..^1], change(source.Paths[^1])] };
    private static Definition Map(Definition source, Func<(int X, int Y), (int X, int Y)> map) =>
        source with { Paths = source.Paths.Select(path => new PathSpec(path.Points.Select(map).ToArray(), path.Closed)).ToArray() };
    private static Definition Remove(Definition source, int path) => source with { Paths = source.Paths.Where((_, index) => index != path).ToArray() };
    private static Definition Append(Definition source, params PathSpec[] paths) => source with { Paths = [.. source.Paths, .. paths] };
    private static Definition Split(Definition source) => source with {
        Paths = source.Paths.SelectMany(path => Enumerable.Range(1, path.Points.Length - 1)
            .Select(index => Open(path.Points[index - 1], path.Points[index]))).ToArray() };
}
