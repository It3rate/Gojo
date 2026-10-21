using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Executed native sources for a local support-family inquiry.
/// Names select examples only while authoring; readers receive graph passages,
/// exposed native endpoint words and separately declared interpolation. The
/// optional turn-model choice belongs to the surrounding experiment, not to
/// a fact inferred from direction names or screen coordinates.</summary>
public static class NativeLatticeSources
{
    public sealed record SourceCase(string Id, string Name, string Note,
        QualitativeStrokeGraph Graph, Relation Bindings, Relation StraightPassages,
        Relation Provenance, bool TurnModel);

    public static IReadOnlyList<SourceCase> Cases()
    {
        var cases = new List<SourceCase>();
        var plain = LongSupports("N01", "Three by three support families",
            "Six separately executed passages expose twelve endpoints. Their nine crossings are not published as joints or faces. No geometric quarter-turn model is selected.",
            [0, 1, 2], [0, 1, 2], false);
        cases.Add(plain);
        cases.Add(plain with { Id = "N02", Name = "The same source with a selected turn model",
            Note = "The actual graph, words and straight-passage declarations are unchanged. The harness additionally selects its explicit native quarter-turn interpretation; the source direction tokens alone did not establish it.",
            TurnModel = true });
        cases.Add(LongSupports("N03", "Unequal intervals on the two families",
            "Horizontal supports have one-reach spacing; vertical supports have two-reach spacing. The common reach is uncalibrated. Different support-pair selections can expose different boundary proportions.",
            [0, 1, 2], [0, 2, 4], true));
        cases.Add(LongSupports("N04", "One finite horizontal passage is clipped",
            "The first horizontal support ends at the middle vertical support. Its extended supporting line remains available, but the finite stroke does not cover every possible encounter.",
            [0, 1, 2], [0, 1, 2], true, clip: true));

        var unknown = LongSupports("N05", "One endpoint word is withheld",
            "One exposed endpoint binding is an open word. The retained executed source does not authorize a reader to recover that private position through provenance.",
            [0, 1, 2], [0, 1, 2], true);
        var sourceRows = N.E(unknown.Bindings).Cast<Relation>().ToArray();
        var hidden = sourceRows[0];
        var offered = N.S(sourceRows.Select(row => (IElement)(ReferenceEquals(row, hidden)
            ? new Relation(row.Left, StructuralConstraint.Open()) : row)).ToArray());
        cases.Add(unknown with { Bindings = offered,
            Provenance = new(unknown.Provenance, new Relation(unknown.Bindings, offered)) });

        cases.Add(LongSupports("N06", "Distinct supports at coincident transverse positions",
            "The second and third vertical passages retain distinct stroke and endpoint identities at the same native position. Coincidence neither creates a join nor supplies a strictly separated pair of sides.",
            [0, 1, 2], [0, 1, 1], true));
        var curved = LongSupports("N07", "Interpolation intentionally unspecified",
            "Endpoint words and chord displacements are exposed, while straight interpolation is withheld. The reader must not silently replace an unspecified or curved passage by its straight endpoint chord.",
            [0, 1, 2], [0, 1, 2], true);
        cases.Add(curved with { StraightPassages = StructuralConstraint.Open() });

        var library = new BourgoinPatternLibrary();
        foreach (var (id, atlas) in new[] { ("N08", "B01"), ("N09", "B07"), ("N10", "B17"), ("N11", "B18") })
        {
            var recipe = library.Recipes.Single(item => item.Id == atlas);
            var start = (Relation)recipe.Window.Left;
            var window = OrnamentProgram.Window(start.Left, start.Right, 2, 2);
            var run = recipe.Program.Generate(recipe.Bindings, window);
            var field = new OrnamentProgramField(run, recipe.SeamContract);
            if (!run.IsComplete || !field.IsComplete)
                throw new InvalidOperationException("A source fixture must retain its actual completed field execution.");
            cases.Add(new(id, atlas + " · " + recipe.Name,
                "An actual two-by-two sample of the existing native ornament program, including its accepted endpoint seams. Its straight-chord interpretation is offered explicitly; no display basis supplies geometric perpendicularity or a filled interior.",
                field.Graph, field.Bindings, N.S(N.E(field.Graph.Strokes)),
                new Relation(new Relation(recipe.Program.Surface, new Relation(recipe.Bindings, window)),
                    new Relation(run.Surface, field.Surface)), false));
        }
        cases.Add(LongSupports("N12", "One passage is replaced by a notched route",
            "The lower horizontal route has explicit offset and return subchords. Those individual passages are straight, but the whole route is not a straight segment merely because its net displacement is horizontal.",
            [0, 1, 2], [0, 1, 2], true, notch: true));
        var bands = cases.Single(item => item.Id == "N09");
        cases.Add(bands with { Id = "N13", Name = "B07 with a selected native turn model",
            Note = "The graph and published words are exactly the same as N09. The harness selects an additional native geometric turn interpretation so rectangle questions can be asked under that explicit model, independently of the recipe name or its display basis.",
            TurnModel = true });
        return cases.AsReadOnly();
    }

    private static SourceCase LongSupports(string id, string name, string note,
        int[] horizontalOffsets, int[] verticalOffsets, bool turnModel, bool clip = false, bool notch = false)
    {
        var author = new LatticeLadderAuthor();
        var paths = new List<Relation>();
        var right = verticalOffsets.Max() + 1;
        var top = horizontalOffsets.Max() + 1;
        for (var index = 0; index < horizontalOffsets.Length; index++)
        {
            var end = clip && index == 0 ? verticalOffsets[1] : right;
            var start = author.Word(-1, horizontalOffsets[index]);
            paths.Add(notch && index == 0
                ? LatticeLadderAuthor.Path(start, author.Word(1, 0), author.Word(0, 1),
                    author.Word(end - 1, 0), author.Word(0, -1), author.Word(1, 0))
                : LatticeLadderAuthor.Path(start, author.Word(end + 1, 0)));
        }
        foreach (var offset in verticalOffsets)
            paths.Add(LatticeLadderAuthor.Path(author.Word(offset, -1), author.Word(0, top + 1)));
        var unit = author.Unit("native support passages", paths.ToArray());
        var recipe = author.Compile(id, name, note, "Native support inquiry",
            unit, author.Word(2 * (right + 1), 0), author.Word(0, 2 * (top + 1)), 1, 1);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        // Custom supports are deliberately independent. Even coincident
        // endpoint words do not activate the author's optional seam proposals.
        var contract = OrnamentProgramField.Contract(recipe.Program);
        var field = new OrnamentProgramField(run, contract);
        if (!run.IsComplete || !field.IsComplete)
            throw new InvalidOperationException("A native source passage fixture must execute before publication.");
        return new(id, name, note, field.Graph, field.Bindings, N.S(N.E(field.Graph.Strokes)),
            new Relation(new Relation(recipe.Program.Surface, new Relation(recipe.Bindings, recipe.Window)),
                new Relation(run.Surface, new Relation(contract, field.Surface))), turnModel);
    }
}
