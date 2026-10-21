using Spec = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Spec;
using Sample = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Sample;
using PathSpec = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.PathSpec;
using Definition = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Definition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Executable alternatives and near misses for a four-lobe ornament.
/// These authoring recipes enter the same native word/path, exposed-port join,
/// execution and publication machinery as the reconstruction corpus. Labels
/// and notes are presentation only; no expected concept is supplied to a read.</summary>
public static class OrnamentArrangementCases
{
    public sealed record Case(string Id, string Name, string ConstructionNote, Spec Spec)
    {
        public Sample Create() => Spec.Create();
    }

    public static IReadOnlyList<Case> All { get; } = Build();
    public static IReadOnlyList<Spec> Specs { get; } = All.Select(item => item.Spec).ToArray();
    public static Sample Create(string id) => All.Single(item => item.Id == id).Create();

    private static IReadOnlyList<Case> Build()
    {
        var result = new List<Case>();
        Add("R01", "Lozenge rosette", "Reference: four closed lobes share a hub and meet a closed outer contour.",
            () => Rosette(Lozenge(2, 1, 2), 2));
        Add("R02", "Long narrow spindles", "The same turning words use a longer uncalibrated reach above each shoulder.",
            () => Rosette(Lozenge(1, 1, 5), 2));
        Add("R03", "Broad low shoulders", "Lobes keep their four heading classes while widening their lower arms.",
            () => Rosette(Lozenge(3, 1, 4), 2));
        Add("R04", "High swept shoulders", "The diagonal and axial portions exchange prominence without exchanging their order.",
            () => Rosette(Lozenge(1, 5, 1), 2));
        Add("R05", "Unequal four-lobe bloom", "Each sibling uses independent reach words; the hub and rim remain shared.",
            () => Rosette([Lozenge(2, 1, 8), Lozenge(1, 2, 3), Lozenge(4, 2, 4), Lozenge(2, 4, 3)], 2));
        Add("R06", "Alternating shoulders", "Opposed siblings agree but adjacent lobes use visibly different proportions.",
            () => Rosette([Lozenge(2, 1, 6), Lozenge(1, 5, 1), Lozenge(2, 1, 6), Lozenge(1, 5, 1)], 2));
        Add("R07", "Low broad and high narrow", "Four distinct shoulder positions preserve the same qualitative word sequence.",
            () => Rosette([Lozenge(3, 1, 5), Lozenge(1, 4, 1), Lozenge(3, 1, 7), Lozenge(1, 4, 3)], 2));
        Add("R08", "Flattened lozenge field", "A changed uncalibrated horizontal basis flattens the entire construction.",
            () => Rosette(Lozenge(2, 1, 2), 2), reach: 1);
        Add("R09", "Reflected swept rosette", "A reflected construction reverses handedness while retaining its native joins.",
            () => Map(Rosette(Lozenge(2, 1, 2), 2), point => (-point.X, point.Y)));
        Add("R10", "Quarter-turned unequal bloom", "The asymmetrical construction is expressed through a quarter-turn basis.",
            () => Rosette([Lozenge(2, 1, 8), Lozenge(1, 2, 3), Lozenge(4, 2, 4), Lozenge(2, 4, 3)], 2), transform: 1);
        Add("R11", "Triangular petal wheel", "Each lobe has fewer turning sites, but still supplies two distinct hub-to-rim routes.",
            () => Rosette(Ring((0, 0), (4, 2), (1, 2)), 1));
        Add("R12", "Diamond petal cross", "Four broad diagonal diamonds meet only at the common hub and rim tips.",
            () => Rosette(Ring((0, 0), (3, 1), (4, 4), (1, 3)), 2));
        Add("R13", "Cardinal kite wheel", "The lobes point along the observer's axes instead of its diagonals.",
            () => Rosette(Ring((0, 0), (1, 2), (0, 5), (-1, 2)), 2));
        Add("R14", "Elbow petal quartet", "Each lobe takes an orthogonal detour, yielding a squared fourfold figure.",
            () => Rosette(Ring((0, 0), (2, 1), (2, 2), (4, 2), (4, 4), (1, 4), (1, 2), (0, 1)), 4));
        Add("R15", "Notched petal wheel", "One side of each petal indents; the new turns are ordinary degree-two sites.",
            () => Rosette(Ring((0, 0), (3, 1), (2, 2), (4, 4), (1, 3), (1, 1)), 3));
        Add("R16", "Chamfered petal wheel", "Two additional turns articulate each lobe without adding a branch.",
            () => Rosette(Ring((0, 0), (2, 1), (3, 2), (3, 4), (1, 3), (0, 2)), 3));
        Add("R17", "Subdivided lozenge petals", "Every lobe edge contains a retained collinear landmark; no new branch is introduced.",
            () => Rosette(Subdivide(Lozenge(4, 2, 4)), 4));
        Add("R18", "Subdivided outer contour", "The rim is one closed path with extra between-sites on each course.",
            () => WithRim(Rosette(Lozenge(4, 2, 4), 2), path => Subdivide(path)));
        Add("R19", "Bowed outer contour", "The rim bends away between its attachment sites instead of taking four direct chords.",
            () => WithRim(Rosette(Lozenge(2, 1, 2), 2), _ => Ring((2, 3), (-1, 5), (-3, 2), (-5, -1), (-2, -3), (1, -5), (3, -2), (5, 1))));
        Add("R20", "Four different petal routes", "Triangle, lozenge, notched and chamfered lobes share one arrangement without equal local shape.",
            () => Rosette([Ring((0, 0), (4, 2), (1, 2)), Lozenge(2, 1, 3),
                Ring((0, 0), (3, 1), (2, 2), (4, 4), (1, 3), (1, 1)),
                Ring((0, 0), (2, 1), (3, 2), (3, 4), (1, 3), (0, 2))], [1, 2, 3, 3]));
        Add("R21", "Petals constructed from separate sides", "Visible incidence is retained while mechanical path grouping becomes independent sides.",
            () => SplitPaths(Rosette(Lozenge(2, 1, 2), 2), splitRim: false));
        Add("R22", "Rim constructed from four sides", "The closed visible rim is assembled from four separate drawing paths.",
            () => SplitPaths(Rosette(Lozenge(2, 1, 2), 2), splitRim: true));
        Add("R23", "Two independent rosette occurrences", "Multiplicity changes explicitly: two translated occurrences share no sites.",
            () => Rosette(Lozenge(2, 1, 2), 2), repeats: 2);
        Add("R24", "Open outer course", "The last rim chord is absent, leaving two distinct exposed ends.",
            () => WithRim(Rosette(Lozenge(2, 1, 2), 2), path => new(path.Points[..^1])));
        Add("R25", "One lobe missing", "Three hub-to-rim lobes remain while the fourth rim attachment has no petal.",
            () => RemoveLobe(Rosette(Lozenge(2, 1, 2), 2), 3));
        Add("R26", "Extra central lobe", "A fifth closed loop is attached only at the hub; four rim attachments remain.",
            () => Append(Rosette(Lozenge(2, 1, 2), 2), Ring((0, 0), (1, 0), (1, 1))));
        Add("R27", "Unjoined coincident petals", "The paths occupy matching presentation positions, but the shared hub and rim joins are not declared.",
            () => Rosette(Lozenge(2, 1, 2), 2) with { JoinCoincident = false });
        Add("R28", "Detached outer contour", "The rim is translated away from its intended attachment sites.",
            () => WithRim(Rosette(Lozenge(2, 1, 2), 2), path => new(path.Points.Select(point => (point.X + 1, point.Y + 1)).ToArray(), path.Closed)));
        Add("R29", "Broken petal closure", "One lobe loses its returning chord and its closed-path obligation.",
            () => ChangeLobe(Rosette(Lozenge(2, 1, 2), 2), 0, path => new(path.Points[..^1])));
        Add("R30", "Split central participation", "One lobe and its rim tip move together, withdrawing that lobe from the common hub.",
            () => SplitHub());
        Add("R31", "Rim diagonal tie", "An additional chord connects opposite rim tips and introduces new participation.",
            () => Append(Rosette(Lozenge(2, 1, 2), 2), Open((2, 3), (-2, -3))));
        Add("R32", "Petal wheel without a rim", "The common hub survives but all rim chords are absent.",
            () => new(Rosette(Lozenge(2, 1, 2), 2).Paths[..^1]));
        Add("R33", "Rim without petals", "One outer contour has neither radial lobes nor a shared central site.",
            () => new([Rosette(Lozenge(2, 1, 2), 2).Paths[^1]]));
        Add("R34", "Two-lobe bridge", "Two opposed closed lobes meet the hub and a closed connecting contour.",
            () => RemoveLobe(RemoveLobe(Rosette(Lozenge(2, 1, 2), 2), 3), 1));
        Add("R35", "Stem on the outer contour", "A new branch grows from one attachment site without closing another lobe.",
            () => Append(Rosette(Lozenge(2, 1, 2), 2), Open((2, 3), (2, 5))));
        Add("R36", "Four lozenges in a chain", "The same four-lobe local vocabulary is reorganized as a sequence rather than a common hub.",
            () => Chain());
        Add("R37", "Four-lobe fan in an enclosing field", "All four lobes face the same side. The hub, paired routes and cyclic rim survive; radial visual distribution is not asserted.",
            () => Fan());
        Add("R38", "Four-chamber branching window", "Stepped corridors spread from one hub to four ordered tips along a window side; the route arrangement survives a markedly different silhouette.",
            () => Window());
        return result.AsReadOnly();

        void Add(string id, string name, string note, Func<Definition> definition,
            int transform = 0, int reach = 0, int repeats = 1)
        {
            var spec = Spec.Make(id, "ARRANGEMENT-" + id, name, "Constructed counterfactual to LOZENGE-ROSETTE; native arrangement study",
                transform, reach, repeats, definition);
            result.Add(new(id, name, note, spec));
        }
    }

    private static PathSpec Lozenge(int a, int b, int c) => Ring((0, 0), (a, b), (a, b + c), (0, c));
    private static PathSpec Ring(params (int X, int Y)[] points) => new([.. points, points[0]], true);
    private static PathSpec Open(params (int X, int Y)[] points) => new(points);

    private static Definition Rosette(PathSpec seed, int tip) => Rosette([seed, seed, seed, seed], tip);
    private static Definition Rosette(PathSpec[] seeds, int tip) => Rosette(seeds, [tip, tip, tip, tip]);
    private static Definition Rosette(PathSpec[] seeds, int[] tips)
    {
        var petals = seeds.Select((seed, turn) => new PathSpec(seed.Points.Select(point => Quarter(point, turn)).ToArray(), seed.Closed)).ToArray();
        return new([.. petals, Ring(petals.Select((petal, index) => petal.Points[tips[index]]).ToArray())]);
    }

    private static (int X, int Y) Quarter((int X, int Y) point, int turn) => turn switch
    {
        0 => point, 1 => (-point.Y, point.X), 2 => (-point.X, -point.Y), _ => (point.Y, -point.X)
    };

    private static Definition WithRim(Definition source, Func<PathSpec, PathSpec> change) =>
        source with { Paths = [.. source.Paths[..^1], change(source.Paths[^1])] };
    private static Definition ChangeLobe(Definition source, int lobe, Func<PathSpec, PathSpec> change) =>
        source with { Paths = source.Paths.Select((path, index) => index == lobe ? change(path) : path).ToArray() };
    private static Definition RemoveLobe(Definition source, int lobe) =>
        source with { Paths = source.Paths.Where((_, index) => index != lobe).ToArray() };
    private static Definition Append(Definition source, PathSpec path) => source with { Paths = [.. source.Paths, path] };
    private static Definition Map(Definition source, Func<(int X, int Y), (int X, int Y)> map) =>
        source with { Paths = source.Paths.Select(path => new PathSpec(path.Points.Select(map).ToArray(), path.Closed)).ToArray() };

    private static PathSpec Subdivide(PathSpec path)
    {
        var points = new List<(int X, int Y)> { path.Points[0] };
        for (var index = 1; index < path.Points.Length; index++)
        {
            var a = path.Points[index - 1]; var b = path.Points[index];
            points.Add(((a.X + b.X) / 2, (a.Y + b.Y) / 2)); points.Add(b);
        }
        return new(points.ToArray(), path.Closed);
    }

    private static Definition SplitPaths(Definition source, bool splitRim)
    {
        var paths = new List<PathSpec>();
        for (var index = 0; index < source.Paths.Length; index++)
        {
            var path = source.Paths[index];
            if ((index == source.Paths.Length - 1) == splitRim)
                for (var edge = 1; edge < path.Points.Length; edge++) paths.Add(Open(path.Points[edge - 1], path.Points[edge]));
            else paths.Add(path);
        }
        return source with { Paths = paths.ToArray() };
    }

    private static Definition SplitHub()
    {
        var source = Rosette(Lozenge(2, 1, 2), 2);
        var moved = source.Paths[0].Points.Select(point => (point.X + 1, point.Y)).ToArray();
        var rim = source.Paths[^1].Points.ToArray(); rim[0] = moved[2]; rim[^1] = rim[0];
        return new([new(moved, true), .. source.Paths[1..^1], new(rim, true)]);
    }

    private static Definition Chain()
    {
        var seed = Lozenge(2, 1, 2);
        var petals = Enumerable.Range(0, 4).Select(index => new PathSpec(seed.Points.Select(point =>
            (point.X + index * 2, point.Y + index * 3)).ToArray(), true)).ToArray();
        return new(petals);
    }

    private static Definition Fan()
    {
        var petals = new[]
        {
            Ring((0, 0), (-4, 5), (-9, 12), (-3, 5)),
            Ring((0, 0), (-2, 5), (-3, 12), (-1, 5)),
            Ring((0, 0), (1, 5), (3, 12), (2, 5)),
            Ring((0, 0), (3, 5), (9, 12), (4, 5))
        };
        return new([.. petals, Ring((-9, 12), (-3, 12), (3, 12), (9, 12), (12, -3), (-12, -3))]);
    }

    private static Definition Window()
    {
        var petals = new[] { 18, 6, -6, -18 }.Select(y => Ring((0, 0), (6, y / 3 + 1),
            (12, y / 3 + 1), (18, y), (24, y), (18, y - 2), (12, y / 3 - 1), (6, y / 3 - 1))).ToArray();
        return new([.. petals, Ring((24, 18), (24, 6), (24, -6), (24, -18), (-6, -24), (-6, 24))]);
    }
}
