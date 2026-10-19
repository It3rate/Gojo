using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.LatticeLadderAuthor;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation captions over an actual retained native construction.
/// The ladder, unit, period words and seam contract carry its meaning.</summary>
public sealed record LatticeCatalogueStage(string Name, string Description,
    QualitativeLatticeLadder Ladder, BourgoinPatternLibrary.Recipe Recipe, Relation Evidence);

public sealed record LatticeCatalogueCase(string Id, string Name, string Family,
    string Description, IReadOnlyList<LatticeCatalogueStage> Stages)
{
    public LatticeCatalogueStage Current => Stages[^1];
}

/// <summary>A bounded construction catalogue, rather than an aesthetic truth
/// table. Twenty native path recipes each admit five declared substitutions.
/// Constraints may be withdrawn and the focus may be resegmented or replaced;
/// no edit asserts that the current graph still has its ancestor's topology.</summary>
public sealed partial class LatticeCatalogueRecipes
{
    public IReadOnlyList<LatticeCatalogueCase> Cases { get; }
    private readonly LatticeLadderRecipes seeds;
    private readonly LatticeLadderAuthor a;
    private readonly int columns;
    private readonly int rows;
    private readonly Dictionary<string, IReadOnlyList<LatticeCatalogueStage>> histories = [];

    public LatticeCatalogueRecipes(int columns = 3, int rows = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        this.columns = columns; this.rows = rows;
        seeds = new(columns, rows); a = seeds.Author;
        var cases = new List<LatticeCatalogueCase>();
        for (var family = 0; family < 20; family++)
            for (var variation = 1; variation <= 5; variation++)
                cases.Add(Build(family, variation, $"C{cases.Count + 1:000}"));
        Cases = cases.AsReadOnly();
    }

    private LatticeCatalogueCase Build(int family, int variation, string id)
    {
        var specification = Describe(family, variation);
        var seed = seeds.Cases.Single(item => item.Id == specification.Source);
        var stages = History(seed).ToList();
        var source = seed.Ladder;
        var paths = E(source.Current.Program.Paths).Cast<Relation>().ToArray();

        // An editable lattice is not a monotonic collection of requirements.
        // Remove actual source paths and their publication roles; carry one
        // real path so that the intermediate remains a legal path program.
        var withdrawn = paths.Skip(1).ToArray();
        if (withdrawn.Length != 0)
        {
            var retainedPorts = S(E(source.Current.Program.Ports).Cast<Relation>().Where(port =>
                ReferenceEquals(((Relation)port.Right).Left, paths[0])).Cast<IElement>().ToArray());
            var retainedClosed = S(E(source.Current.Program.ClosedPaths).Where(path => ReferenceEquals(path, paths[0])).ToArray());
            source = source.ReplacePaths("Release the old boundary constraints", S(withdrawn), S(), retainedPorts,
                retainedClosed, new Relation(seed.Unit.Paths, new Relation(S(paths[0]), S(withdrawn))));
            stages.Add(Stage(id + "-release", "Release the old boundary constraints",
                "The selected source paths and their endpoint publication roles are withdrawn. One actual source path remains carried. No delivered flow is retracted and no old rung is rewritten.",
                source, seed.Recipe.ColumnPitch, seed.Recipe.RowPitch, source.Current.Replacement));
        }
        else
        {
            // A one-contour seed cannot become an empty legal path program.
            // Open its closing span instead, keeping the surviving actual
            // chord identities. The explicit closed-contour obligation is
            // withdrawn along with the endpoint roles of the replaced path.
            var oldPath = paths.Single(); var chords = E((Relation)oldPath.Right).Cast<Relation>().ToArray();
            var openPath = Path((Relation)oldPath.Left, chords.Take(chords.Length - 1).ToArray());
            var releaseEvidence = new Relation(oldPath, new Relation(openPath, chords[^1]));
            source = source.ReplacePaths("Release the old boundary constraints", S(oldPath), S(openPath),
                Ports(openPath), S(), releaseEvidence);
            stages.Add(Stage(id + "-release", "Release the old boundary constraints",
                "The selected contour's closing chord and closure obligation are withdrawn. Its other actual chord identities remain in an open replacement path; the earlier closed contour remains available in history.",
                source, seed.Recipe.ColumnPitch, seed.Recipe.RowPitch, releaseEvidence));
        }

        var sketch = specification.Make();
        var column = a.Word(specification.Column.X, specification.Column.Y);
        var row = a.Word(specification.Row.X, specification.Row.Y);
        if (sketch.CoarsePaths.Count != 0)
        {
            var coarse = sketch.CoarsePaths.ToArray();
            var coarseSelected = E(source.Current.Program.Paths).Cast<Relation>().ToArray();
            var coarseEvidence = new Relation(S(coarseSelected), S(coarse));
            source = source.ReplacePaths("Establish the coarse compartment", S(coarseSelected), S(coarse), Ports(coarse),
                S(sketch.CoarseClosed.Cast<IElement>().ToArray()), coarseEvidence);
            stages.Add(Stage(id + "-envelope", "Establish the coarse compartment",
                "The earlier focus is reformulated as a closed coarse compartment. The following rung consumes this actual envelope, rather than remembering only its name.",
                source, column, row, coarseEvidence));
        }
        var introduced = sketch.Paths.ToArray();
        var sourcePaths = E(source.Current.Program.Paths).Cast<Relation>().ToArray();
        var selected = sourcePaths.Where(path => !introduced.Any(actual => ReferenceEquals(path, actual))).ToArray();
        var newPaths = introduced.Where(path => !sourcePaths.Any(actual => ReferenceEquals(path, actual))).ToArray();
        var carriedPorts = E(source.Current.Program.Ports).Cast<Relation>().Where(port =>
            !selected.Any(path => ReferenceEquals(path, ((Relation)port.Right).Left))).Cast<IElement>();
        var ports = S(carriedPorts.Concat(E(Ports(newPaths))).ToArray());
        var instruction = new Relation(S(selected), new Relation(S(introduced), sketch.Evidence));
        var result = source.ReplacePaths(specification.Name, S(selected), S(newPaths), ports,
            S(sketch.Closed.Cast<IElement>().ToArray()), instruction);
        if (!result.Current.IsComplete) throw new InvalidOperationException($"The native catalogue edit {id} did not settle.");
        stages.Add(Stage(id, specification.Name,
            specification.Description + (selected.Length == 0
                ? " The actual coarse envelope and its publication roles are carried; only the finer paths and their roles are introduced."
                : " The released focus is explicitly replaced; this is not an endpoint-preserving refinement claim."),
            result, column, row, instruction));
        return new(id, specification.Name, specification.Family, specification.Description, stages.AsReadOnly());
    }

    private IReadOnlyList<LatticeCatalogueStage> History(LatticeLadderCase seed)
    {
        if (histories.TryGetValue(seed.Id, out var previous)) return previous;
        var stages = seed.Ladder.Rungs.Select(rung =>
        {
            var actual = seeds.Cases.Single(item => ReferenceEquals(item.Ladder.Surface, rung.Surface));
            return new LatticeCatalogueStage(actual.Name, actual.Description, actual.Ladder, actual.Recipe, actual.PitchEvidence);
        }).ToArray();
        histories.Add(seed.Id, stages); return stages;
    }

    private LatticeCatalogueStage Stage(string id, string name, string description, QualitativeLatticeLadder ladder,
        Relation column, Relation row, Relation evidence) => new(name, description, ladder,
            a.Compile(id, name, description, "Native ladder catalogue; no exact historical plate identity is claimed.",
                ladder.Current.Program, column, row, columns, rows), evidence);

    private sealed record Specification(string Family, string Name, string Source, string Description,
        (int X, int Y) Column, (int X, int Y) Row, Func<Sketch> Make);

    /// <summary>Authoring coordinates choose repeated occurrences in one native
    /// shared reach. They do not survive as a second semantic graph: starts,
    /// chord words, contour claims and translated-copy receipts all become
    /// connected relations before the unit is compiled or executed.</summary>
    private sealed class Sketch(LatticeLadderAuthor author)
    {
        public List<Relation> Paths { get; } = [];
        public List<Relation> Closed { get; } = [];
        public List<Relation> CoarsePaths { get; } = [];
        public List<Relation> CoarseClosed { get; } = [];
        private readonly List<IElement> receipts = [];
        public Relation Evidence => S(receipts.ToArray());

        public Relation Line(params (int X, int Y)[] points)
        {
            if (points.Length < 2) throw new ArgumentException("A native path needs at least one chord.");
            var words = points.Zip(points.Skip(1), (from, to) =>
            {
                if (from == to) throw new ArgumentException("A path must not introduce a zero chord.");
                return author.Word(to.X - from.X, to.Y - from.Y);
            }).ToArray();
            var path = Path(author.Word(points[0].X, points[0].Y), words);
            Paths.Add(path); return path;
        }

        public Relation Polygon(params (int X, int Y)[] points)
        {
            var path = Line([.. points, points[0]]); Closed.Add(path); return path;
        }

        public void Coarse(Relation path, bool retainInFinal)
        {
            CoarsePaths.Add(path);
            if (Closed.Contains(path)) CoarseClosed.Add(path);
            if (!retainInFinal) { Paths.Remove(path); Closed.Remove(path); }
        }

        public Relation Copy(Relation path, (int X, int Y) shift, bool closed = false)
        {
            var copy = author.Translate(path, author.Word(shift.X, shift.Y), out var receipt);
            receipts.Add(receipt); Paths.Add(copy); if (closed) Closed.Add(copy); return copy;
        }

        public void Image(Relation source, QualitativePatternTransform transform, (int X, int Y) shift, bool closed)
        {
            var unit = new UnitArrangementProgram(author.Frame, "actual selected prototype", S(source), Ports(source), closed ? S(source) : S());
            var mapped = unit.Transform("actual native image", transform);
            var image = (Relation)E(mapped.Paths).Single();
            receipts.Add((Relation)mapped.Evidence); Copy(image, shift, closed);
        }
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
