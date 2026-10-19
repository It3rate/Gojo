using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite ornament authoring over retained Bourgoin programs. The
/// layers, selected actual paths, reusable additions and landmark attachments
/// are native structure. Palette and visual emphasis belong to a renderer.</summary>
public static class BourgoinColourLibrary
{
    public sealed record Scene(string Id, string Title, string SourceId,
        BourgoinPatternLibrary.Recipe Source, OrnamentEnrichment Enrichment,
        IElement FieldLayer, IElement MotifLayer, IElement DetailLayer, string Description);

    public static IReadOnlyList<Scene> Create()
    {
        var recipes = new BourgoinPatternLibrary().Recipes;
        return [StarGarden(Find("B26")), OctagonMedallions(Find("B06.B")),
            FrettedGarden(Find("B31")), DiagonalBrocade(Find("B33"))];
        BourgoinPatternLibrary.Recipe Find(string id) => recipes.Single(recipe => recipe.Id == id);
    }

    private static Scene StarGarden(BourgoinPatternLibrary.Recipe source)
    {
        var author = new Author(source);
        var boundary = author.Closed.Single();
        var rays = author.Paths.Where(path => !author.IsClosed(path)).ToArray();
        author.Field.Select(boundary);
        author.Motif.Select(rays);
        // The fine view can use the same ray evidence without consuming it.
        author.Detail.Select(rays);
        var centre = author.Port(rays[0], 0);
        var petal = author.Prototype("shared sixfold petal", true,
            (0, 0), (1, -1), (2, -1), (1, 0), (0, 0));
        var horizontal = (X: 1, Y: 0); var vertical = (X: 0, Y: 1);
        for (var turn = 0; turn < 6; turn++)
        {
            author.Place(author.Detail, petal, centre, (0, 0), author.Map(horizontal, vertical));
            horizontal = Sixth(horizontal); vertical = Sixth(vertical);
        }
        return author.Finish("C01", "Flat-star flower garden",
            "The six-flat-tip field and its twelve centre rays are retained. One actual four-chord petal prototype has six witnessed substitution images attached to an actual ray-origin port. A field, radial motif or conventional flower can become the leading read without discarding its siblings. The small hexagon gaps remain emergent; the flowers do not claim new joins or occupied native faces.");

        static (int X, int Y) Sixth((int X, int Y) point) => (-point.Y, point.X + point.Y);
    }

    private static Scene OctagonMedallions(BourgoinPatternLibrary.Recipe source)
    {
        var author = new Author(source);
        var boundary = author.Closed.Single();
        author.Field.Select(boundary); author.Motif.Select(boundary); author.Detail.Select(boundary);
        var anchor = author.Port(boundary, 0);
        // The actual source starts at (2,2); (-2,4) selects the centre (0,6)
        // in its existing shared-reach basis. These finite counts only author
        // native offset words, not calibrated screen measurements.
        var centreOffset = (X: -2, Y: 4);
        var inset = author.Prototype("octagonal medallion", true,
            (1, -3), (3, -1), (3, 1), (1, 3), (-1, 3), (-3, 1), (-3, -1), (-1, -3), (1, -3));
        author.Place(author.Motif, inset, anchor, centreOffset);
        var petal = author.Prototype("shared fourfold petal", true,
            (0, 0), (-1, 1), (0, 2), (1, 1), (0, 0));
        foreach (var turn in author.Quarters()) author.Place(author.Detail, petal, anchor, centreOffset, turn);
        return author.Finish("C02", "Octagons and quartered medallions",
            "The actual octagon remains the large field. An eight-chord inset supplies an intermediate medallion and one four-chord petal has four actual quarter-related images at its centre. Every addition retains its published boundary-port anchor and offset witness. The upright square gaps continue to come from neighbouring octagons; no square glyph is added to them.");
    }

    private static Scene FrettedGarden(BourgoinPatternLibrary.Recipe source)
    {
        var author = new Author(source);
        var arms = author.Paths.Where(path => !author.IsClosed(path)).ToArray();
        var marker = author.Closed.Single();
        author.Field.Select(arms); author.Motif.Select(marker); author.Detail.Select(marker);
        var leaf = author.Prototype("shared wrist leaf", true,
            (0, 0), (1, 1), (3, 0), (1, -1), (0, 0));
        var quarters = author.Quarters();
        for (var index = 0; index < arms.Length; index++)
            author.Place(author.Motif, leaf, author.Port(arms[index], 1), (0, 0), quarters[index]);
        // The existing faceted round marker is retained. The flower enriches
        // its interior rather than replacing that marker with a diamond.
        var petal = author.Prototype("round-marker flower petal", true,
            (0, 0), (-1, 1), (0, 2), (1, 1), (0, 0));
        var markerAnchor = author.Port(marker, 0);
        foreach (var turn in quarters) author.Place(author.Detail, petal, markerAnchor, (-2, 0), turn);
        return author.Finish("C03", "Fret stems, wrist leaves and round flowers",
            "Four outward turning arms remain the dominant structural alternative. One leaf prototype has four actual quarter-related images attached to the arms' published first-turn ports. The original twelve-chord round marker is retained and receives a four-petal conventional flower. Equal drawing positions do not manufacture contact or over-under order; all contours remain finite chords, without analytic-circle or tangency claims.");
    }

    private static Scene DiagonalBrocade(BourgoinPatternLibrary.Recipe source)
    {
        var author = new Author(source);
        var courses = author.Paths.Where(path => author.Chords(path) > 2).ToArray();
        var returns = author.Paths.Where(path => author.Chords(path) == 2).ToArray();
        author.Field.Select(courses); author.Motif.Select(returns); author.Detail.Select(returns);
        var inset = author.Prototype("shared diamond-window inset", true,
            (0, 1), (1, 0), (0, -1), (-1, 0), (0, 1));
        foreach (var path in returns)
            author.Place(author.Detail, inset, author.Port(path, 0), (0, -2));
        return author.Finish("C04", "Diagonal brocade and small diamond seeds",
            "The long open courses, the short two-chord returns and their new small diamond seeds form three selectable levels. Both inset images reuse one actual four-chord prototype and attach through an existing return's actual start port with a visible offset. The underlying mirrored half-drop and open white passages are preserved. Colour may reveal a different diagonal chain, but it does not turn the open source courses into closed ribbons or establish filled-region connectivity.");
    }

    private sealed class LayerDraft
    {
        public Placeholder Identity { get; } = new();
        private readonly List<Relation> selected = [], additions = [], attachments = [];
        public void Select(params Relation[] paths) => selected.AddRange(paths);
        public void Add(UnitArrangementProgram program, Relation attachment)
        { additions.Add(program.Surface); attachments.Add(attachment); }
        public Relation Definition => OrnamentEnrichment.Layer(Identity, N.S(selected.ToArray()),
            N.S(additions.ToArray()), N.S(attachments.ToArray()));
    }

    private sealed class Author
    {
        private readonly BourgoinPatternLibrary.Recipe recipe;
        private readonly IElement reach;
        private readonly UnitArrangementProgram source;
        private QualitativeMotionFrame Frame => source.Frame;
        public Relation[] Paths { get; }
        public Relation[] Closed { get; }
        public LayerDraft Field { get; } = new();
        public LayerDraft Motif { get; } = new();
        public LayerDraft Detail { get; } = new();

        public Author(BourgoinPatternLibrary.Recipe recipe)
        {
            this.recipe = recipe;
            var actual = (Relation)N.E(recipe.Motifs).Single();
            if (!ReferenceEquals(actual.Left, recipe.Frame.Surface) ||
                actual.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                    Right: Relation { Left: Relation closed, Right: var evidence } })
                throw new ArgumentException("Colour authoring requires one complete native source motif.", nameof(recipe));
            source = new(recipe.Frame, "retained ornament source", paths, ports, closed, evidence);
            if (!source.Complete()) throw new ArgumentException("Colour authoring requires complete source paths and exposed ports.", nameof(recipe));
            Paths = N.E(paths).Cast<Relation>().ToArray(); Closed = N.E(closed).Cast<Relation>().ToArray();
            var moves = Paths.SelectMany(path => N.E((Relation)path.Left)
                .Concat(N.E((Relation)path.Right).Cast<Relation>().SelectMany(N.E))).Cast<Relation>().ToArray();
            var reaches = moves.Select(move => move.Right).Distinct(N.Participants.Instance).ToArray();
            if (reaches.Length != 1) throw new ArgumentException("This finite authoring study uses the source's one shared reach.", nameof(recipe));
            reach = reaches[0];
        }

        public bool IsClosed(Relation path) => Closed.Any(actual => ReferenceEquals(actual, path));
        public int Chords(Relation path) => N.E((Relation)path.Right).Length;

        public Relation Port(Relation actualPath, int chordIndex)
        {
            var words = N.E((Relation)actualPath.Right);
            var pick = chordIndex == words.Length ? actualPath : words[chordIndex];
            return N.E(source.Ports).Cast<Relation>().Single(port => port.Right is Relation selector &&
                ReferenceEquals(selector.Left, actualPath) && ReferenceEquals(selector.Right, pick));
        }

        public UnitArrangementProgram Prototype(string name, bool closes, params (int X, int Y)[] vertices)
        {
            var words = Enumerable.Range(0, vertices.Length - 1).Select(index => Word(
                (vertices[index + 1].X - vertices[index].X, vertices[index + 1].Y - vertices[index].Y))).ToArray();
            var path = new Relation(Word(vertices[0]), N.S(words));
            return new(Frame, name, N.S(path), Ports(path, words), closes ? N.S(path) : N.S());
        }

        public QualitativePatternTransform Map((int X, int Y) horizontal, (int X, int Y) vertical)
        {
            Relation Row(IElement direction, (int X, int Y) vector) =>
                new(QualitativeMotionFrame.Move(direction, reach), Word(vector));
            return new(Frame, Frame, N.S(Row(Frame.Right, horizontal), Row(Frame.Left, (-horizontal.X, -horizontal.Y)),
                Row(Frame.Up, vertical), Row(Frame.Down, (-vertical.X, -vertical.Y))));
        }

        public QualitativePatternTransform[] Quarters() =>
            [Map((1, 0), (0, 1)), Map((0, 1), (-1, 0)), Map((-1, 0), (0, -1)), Map((0, -1), (1, 0))];

        public void Place(LayerDraft layer, UnitArrangementProgram prototype, Relation actualAnchor,
            (int X, int Y) offset, QualitativePatternTransform? transform = null)
        {
            var mapped = prototype.Transform("witnessed ornament image", transform ?? Map((1, 0), (0, 1)));
            var mappedPath = (Relation)N.E(mapped.Paths).Single();
            if (!OrnamentEnrichment.TryAnchorWord(source, actualAnchor, out var anchor, out var anchorRead))
                throw new ArgumentException("An addition must attach through an actual source port.", nameof(actualAnchor));
            var offsetRead = Frame.Reduce(Word(offset), (Relation)mappedPath.Left);
            if (!Frame.TryReadResult(offsetRead, out var fromAnchor, out _, out var pending) || N.E(pending).Length != 0)
                throw new InvalidOperationException("A finite addition offset did not settle in the source frame.");
            var placement = Frame.Reduce(anchor, fromAnchor);
            if (!Frame.TryReadResult(placement, out var start, out _, out pending) || N.E(pending).Length != 0)
                throw new InvalidOperationException("A finite addition did not settle at its advertised landmark.");
            var words = N.E((Relation)mappedPath.Right).Cast<Relation>().ToArray();
            var path = new Relation(start, mappedPath.Right);
            var portRows = Ports(path, words);
            var correspondence = new Relation(mappedPath, path);
            var evidence = new Relation(mapped.Surface, new Relation(correspondence,
                new Relation(actualAnchor, new Relation(anchorRead, new Relation(offsetRead, placement)))));
            var added = new UnitArrangementProgram(Frame, "placed native ornament", N.S(path), portRows,
                N.E(mapped.ClosedPaths).Length == 0 ? N.S() : N.S(path), evidence);
            layer.Add(added, OrnamentEnrichment.Attach(actualAnchor, fromAnchor, path));
        }

        public Scene Finish(string id, string title, string description)
        {
            var actual = (Relation)N.E(recipe.Motifs).Single();
            var enrichment = new OrnamentEnrichment(Frame, actual, N.S(Field.Definition, Motif.Definition, Detail.Definition));
            if (!enrichment.IsComplete) throw new InvalidOperationException("Authored ornament layers did not pass their native attachment contracts.");
            return new(id, title, recipe.Id, recipe, enrichment, Field.Identity, Motif.Identity, Detail.Identity, description);
        }

        private static Relation Ports(Relation path, Relation[] words) =>
            N.S(Enumerable.Range(0, words.Length + 1).Select(index => (IElement)new Relation(new Placeholder(),
                new Relation(path, index == words.Length ? path : words[index]))).ToArray());

        private Relation Word((int X, int Y) vector) => N.S(
            Enumerable.Range(0, Math.Abs(vector.X)).Select(_ => (IElement)QualitativeMotionFrame.Move(vector.X < 0 ? Frame.Left : Frame.Right, reach))
                .Concat(Enumerable.Range(0, Math.Abs(vector.Y)).Select(_ => (IElement)QualitativeMotionFrame.Move(vector.Y < 0 ? Frame.Down : Frame.Up, reach))).ToArray());
    }
}
