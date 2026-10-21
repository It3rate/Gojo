using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Executed source passages for an interior-shape read. These cases
/// expose incidence, native endpoint words and straight-passage offers only;
/// no list of intended cells, shape classes or filled regions is supplied.
/// Names and atlas references select fixtures, never classification results.</summary>
public static class NativeInteriorSources
{
    public static IReadOnlyList<NativeLatticeSources.SourceCase> Cases()
    {
        var cases = new List<NativeLatticeSources.SourceCase>();
        cases.Add(LongGrid("I01", "Unjoined crossing grid", false));
        cases.Add(LongGrid("I02", "Unjoined crossing grid with wider columns", true));

        var library = new BourgoinPatternLibrary();
        var selections = new (string Atlas, int Columns, int Rows, bool TurnModel)[]
        {
            ("B01", 2, 2, true), ("B03", 2, 2, true),
            ("B04", 2, 2, false), ("B05", 2, 2, false),
            ("B06.B", 2, 2, true), ("B07", 2, 2, true),
            ("B10", 1, 1, true), ("B12", 1, 1, true),
            ("B13", 1, 1, true), ("B17", 2, 2, true),
            ("B18", 2, 2, true), ("B32", 2, 2, true)
        };
        foreach (var selection in selections)
        {
            var recipe = library.Recipes.Single(item => item.Id == selection.Atlas);
            var phase = (Relation)recipe.Window.Left;
            var window = OrnamentProgram.Window(phase.Left, phase.Right, selection.Columns, selection.Rows);
            var run = recipe.Program.Generate(recipe.Bindings, window);
            var field = new OrnamentProgramField(run, recipe.SeamContract);
            if (!run.IsComplete || !field.IsComplete)
                throw new InvalidOperationException("An interior source must retain its completed native execution.");
            var id = "I" + (cases.Count + 1).ToString("00");
            cases.Add(new(id, selection.Atlas + " · " + recipe.Name,
                $"A {selection.Columns} by {selection.Rows} patch of the actual Bourgoin program. Only its native passages, endpoint words and accepted seams are offered; the classifier receives no intended compartments. " +
                (selection.TurnModel ? "A native quarter-turn interpretation is explicitly selected for this experiment."
                    : "Independent word axes do not assert geometric right angles; no quarter-turn interpretation is selected."),
                field.Graph, field.Bindings, N.S(N.E(field.Graph.Strokes)),
                new Relation(new Relation(recipe.Program.Surface, new Relation(recipe.Bindings, window)),
                    new Relation(run.Surface, field.Surface)), selection.TurnModel));
        }

        cases.Add(Control("I15", "Open three-sided enclosure",
            "The last side is absent. Any proposed closing passage must remain a separate completion hypothesis, rather than becoming observed source evidence.",
            a => [Route(a, (0, 0), (4, 0), (4, 4), (0, 4))]));
        cases.Add(Control("I16", "A closed outline with an actual bisector",
            "The vertical passage meets the split lower and upper boundary at declared shared sites. The enclosing outline has an interior divider, so its two smaller compartments are the elementary candidates.",
            a => [Route(a, (0, 0), (2, 0), (4, 0), (4, 4), (2, 4), (0, 4), (0, 0)),
                Route(a, (2, 0), (2, 4))]));
        cases.Add(Control("I17", "A closed outline with one inward notch",
            "One side turns inward, traverses the inset and returns to the outer level. These are actual connected subchords; the original rectangle is not supplied as an intended face.",
            a => [Route(a, (0, 0), (4, 0), (4, 4), (3, 4), (3, 3), (1, 3), (1, 4), (0, 4), (0, 0))]));
        cases.Add(Control("I18", "A closed outline with a sawtooth side",
            "Three alternating diagonal excursions occupy one side of a connected enclosure. The repeated reversals may support a local zigzag read without claiming the entire shape is a star.",
            a => [Route(a, (0, 0), (6, 0), (6, 4), (5, 3), (4, 4), (3, 3), (2, 4), (1, 3), (0, 4), (0, 0))]));
        cases.Add(Control("I19", "Two closed contours with diagonal sides",
            "A three-edge contour and a four-edge contour are spatially separate. Their corners and closures are actual native incidence; no area inventory or polygon name accompanies the public source.",
            a => [Route(a, (0, 0), (4, 0), (2, 3), (0, 0)),
                Route(a, (8, 0), (10, 2), (8, 4), (6, 2), (8, 0))]));
        cases.Add(Control("I20", "A closed contour with distributed outward points",
            "The alternating excursions turn around the whole enclosure rather than decorating one roughly straight side. Its native corner sequence can be compared with the sawtooth-side case without an authored star label.",
            a => [Route(a, (0, -4), (1, -1), (4, 0), (1, 1), (0, 4), (-1, 1), (-4, 0), (-1, -1), (0, -4))]));
        return cases.AsReadOnly();
    }

    private static NativeLatticeSources.SourceCase LongGrid(string id, string name, bool wider)
    {
        var a = new LatticeLadderAuthor(); var paths = new List<Relation>();
        var offsets = wider ? new[] { 0, 2, 4 } : new[] { 0, 1, 2 };
        var right = wider ? 5 : 3;
        foreach (var y in new[] { 0, 1, 2 }) paths.Add(Route(a, (-1, y), (right, y)));
        foreach (var x in offsets) paths.Add(Route(a, (x, -1), (x, 3)));
        return Execute(a, id, name,
            "Six independent straight passages cross in their interiors. Those encounters are not mechanical joins; an observational subdivision must be discovered from the exposed words. A native quarter-turn model is selected separately.",
            paths.ToArray(), false);
    }

    private static NativeLatticeSources.SourceCase Control(string id, string name, string note,
        Func<LatticeLadderAuthor, Relation[]> paths)
    {
        var author = new LatticeLadderAuthor();
        return Execute(author, id, name, note, paths(author), true);
    }

    private static NativeLatticeSources.SourceCase Execute(LatticeLadderAuthor author,
        string id, string name, string note, Relation[] paths, bool activateDeclaredSeams)
    {
        var unit = author.Unit("interior inquiry source", paths);
        var recipe = author.Compile(id, name, note, "Native interior inquiry", unit,
            author.Word(30, 0), author.Word(0, 30), 1, 1);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var contract = activateDeclaredSeams ? recipe.SeamContract : OrnamentProgramField.Contract(recipe.Program);
        var field = new OrnamentProgramField(run, contract);
        if (!run.IsComplete || !field.IsComplete)
            throw new InvalidOperationException("An interior control must execute before publication.");
        return new(id, name, note, field.Graph, field.Bindings, N.S(N.E(field.Graph.Strokes)),
            new Relation(new Relation(recipe.Program.Surface, new Relation(recipe.Bindings, recipe.Window)),
                new Relation(run.Surface, new Relation(contract, field.Surface))), true);
    }

    private static Relation Route(LatticeLadderAuthor author, params (int X, int Y)[] vertices)
    {
        if (vertices.Length < 2) throw new ArgumentException("A source passage needs two endpoints.", nameof(vertices));
        return LatticeLadderAuthor.Path(author.Word(vertices[0].X, vertices[0].Y),
            Enumerable.Range(0, vertices.Length - 1).Select(index => author.Word(
                vertices[index + 1].X - vertices[index].X,
                vertices[index + 1].Y - vertices[index].Y)).ToArray());
    }
}
