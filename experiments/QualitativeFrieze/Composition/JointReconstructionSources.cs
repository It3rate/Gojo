using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Learning;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Independent teacher constructions for a joint/position/symmetry
/// reconstruction experiment. The ordinary native program runs before the
/// public source exists. No discovered joint inventory, symmetry verdict,
/// polygon name or intended completion accompanies the exposed passages.</summary>
public static class JointReconstructionSources
{
    public sealed record Case(string Id, string Name, string Note, NativeLatticeSources.SourceCase Source);

    public static IReadOnlyList<Case> Cases()
    {
        var cases = new List<Case>
        {
            Build("J01", "Three-sided contour", "Three actual connected chords close through a shared site.",
                a => [Route(a, (0, 0), (6, 0), (3, 4), (0, 0))]),
            Build("J02", "Rectangular contour", "A wide closed path supplies its actual corners and endpoint words; no polygon class is published.",
                a => [Route(a, (0, 0), (6, 0), (6, 4), (0, 4), (0, 0))]),
            Build("J03", "Diamond contour", "The closed path has diagonal chords. Its orientation is expressed in native words rather than a shape label.",
                a => [Route(a, (0, -3), (3, 0), (0, 3), (-3, 0), (0, -3))]),
            Build("J04", "L-shaped contour", "A closed concave outline is authored directly; no parent rectangle is supplied to the receiver.",
                a => [L(a)]),
            Build("J05", "One inward return", "The upper side descends into the enclosure and returns. The receiver must discover this from the actual successive passages.",
                a => [Route(a, (0, 0), (6, 0), (6, 6), (4, 6), (4, 4), (2, 4), (2, 6), (0, 6), (0, 0))]),
            Build("J06", "Cross-shaped outline", "A single closed route turns around four arms; it is not a four-way physical junction.",
                a => [Route(a, (2, 0), (4, 0), (4, 2), (6, 2), (6, 4), (4, 4), (4, 6),
                    (2, 6), (2, 4), (0, 4), (0, 2), (2, 2), (2, 0))]),
            Build("J07", "Four-point outline", "Outward and inward turns are distributed around a closed contour rather than along one decorated side.",
                a => [Route(a, (0, -4), (1, -1), (4, 0), (1, 1), (0, 4), (-1, 1), (-4, 0), (-1, -1), (0, -4))]),
            Build("J08", "Contour with a missing passage", "Two cursor paths meet at a declared site, leaving a gap along the upper side. No assumed closing stroke is present in the source.",
                a => [Route(a, (0, 0), (6, 0), (6, 4), (4, 4)), Route(a, (2, 4), (0, 4), (0, 0))]),
            Build("J09", "Almost regular contour with a lower tab", "One small outward return sits toward the lower left. Any simpler symmetric context must retain this actual deviation.",
                a => [Route(a, (0, 0), (1, 0), (1, -1), (2, -1), (2, 0), (6, 0), (6, 6), (0, 6), (0, 0))]),
            Build("J10", "Translated disconnected contours", "A native translation places a second copy. No connection or recurrence claim is supplied as receiver evidence.",
                a => { var path = L(a); return [path, a.Translate(path, a.Word(7, 1), out _)]; }),
            Build("J11", "Reflected disconnected contours", "A direction substitution reflects the second native route before translating it. The published source contains the resulting passages, without a supplied mirror verdict.",
                a =>
                {
                    var path = L(a); var unit = a.Unit("source unit", path);
                    var mapping = a.Map(a.Word(-1, 0), a.Word(0, 1));
                    var reflected = unit.Transform("reflected source unit", mapping);
                    var other = (Relation)N.E(reflected.Paths).Single();
                    return [path, a.Translate(other, a.Word(10, 0), out _)];
                }),
            Build("J12", "Four turned polygon lobes", "Four applications of the same native quarter-turn substitution return to the original direction basis. Each lobe is closed; no radial or symmetry classification is published.",
                a =>
                {
                    var unit = a.Unit("radial source lobe", Route(a, (1, 0), (3, 0), (4, 1), (2, 2), (1, 0)));
                    var quarter = a.Map(a.Word(0, 1), a.Word(-1, 0)); var paths = new List<Relation>();
                    for (var i = 0; i < 4; i++)
                    {
                        paths.Add((Relation)N.E(unit.Paths).Single());
                        unit = unit.Transform("next native lobe", quarter);
                    }
                    return paths.ToArray();
                }),
            Build("J13", "Three-arm physical junction", "The transverse path and the stem share an actual declared site. This source intentionally has open ends rather than an enclosed area.",
                a => [Route(a, (-3, 0), (0, 0), (3, 0)), Route(a, (0, 0), (0, -3))]),
            Build("J14", "Four-arm physical junction", "Both diagonal paths explicitly stop and continue at the central role. Accepted native seams make that role one actual site; it is not inferred merely from an ink crossing.",
                a => [Route(a, (-3, -3), (0, 0), (3, 3)), Route(a, (-3, 3), (0, 0), (3, -3))])
        };
        var descriptors = OrnamentLearningCorpus.Describe(65);
        foreach (var selection in new[] { (Index: 16, Id: "J15"), (Index: 64, Id: "J16") })
        {
            var sample = OrnamentLearningCorpus.Materialize(descriptors[selection.Index], includePattern: false);
            cases.Add(new(selection.Id, $"Learning case L{selection.Index:00000}",
                "The exact complete teacher unit from the earlier learning curriculum, re-executed at the same descriptor and published independently. Its factory history and intended concept are withheld.",
                new NativeLinePublication(sample.Target with { Id = "public-source", Name = "Native source",
                    Note = "Exposed native geometry only." }).Published));
        }
        return cases.AsReadOnly();
    }

    private static Case Build(string id, string name, string note, Func<LatticeLadderAuthor, Relation[]> makePaths)
    {
        var author = new LatticeLadderAuthor(); var paths = makePaths(author);
        var closed = paths.Where(path => UnitArrangementRead.IsZero(author.Frame, author.Frame.Reduce(N.S(),
            LatticeLadderAuthor.JoinWords(N.E((Relation)path.Right).Cast<Relation>().ToArray())))).ToArray();
        var unit = new UnitArrangementProgram(author.Frame, "native teacher construction", N.S(paths),
            LatticeLadderAuthor.Ports(paths), N.S(closed));
        var recipe = author.Compile("source", "native teacher construction", "Executed passages", "Reconstruction controls",
            unit, author.Word(24, 0), author.Word(0, 24), 1, 1);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        if (!run.IsComplete || !field.IsComplete) throw new InvalidOperationException("A teacher source must execute before publication.");
        var source = new NativeLatticeSources.SourceCase("public-source", "Native source", "Exposed native geometry only.",
            field.Graph, field.Bindings, field.Graph.Strokes, N.S(), true);
        return new(id, name, note, new NativeLinePublication(source).Published);
    }

    private static Relation L(LatticeLadderAuthor author) => Route(author,
        (0, 0), (4, 0), (4, 1), (1, 1), (1, 5), (0, 5), (0, 0));
    private static Relation Route(LatticeLadderAuthor author, params (int X, int Y)[] points) =>
        LatticeLadderAuthor.Path(author.Word(points[0].X, points[0].Y), Enumerable.Range(0, points.Length - 1)
            .Select(index => author.Word(points[index + 1].X - points[index].X, points[index + 1].Y - points[index].Y)).ToArray());
}
