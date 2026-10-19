using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.LatticeLadderAuthor;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Captions for a native construction, not a taxonomy used by its
/// execution. Unit and rung surfaces retain the actual causal participants.</summary>
public sealed record LatticeLadderCase(string Id, string Name, string? ParentId,
    string Description, string AtlasReferences, QualitativeLatticeLadder Ladder,
    BourgoinPatternLibrary.Recipe Recipe, Relation PitchEvidence)
{
    public UnitArrangementProgram Unit => Ladder.Current.Program;
    public Relation RungSurface => Ladder.Current.Surface;
}

/// <summary>Nineteen branch examples built from a shared square. Topology edits,
/// native substitutions, chord refinements and repeated assemblies stay
/// distinct. These are construction experiments inspired by atlas families,
/// not assertions that all of the atlas drawings share one hidden grid.</summary>
public sealed class LatticeLadderRecipes
{
    public IReadOnlyList<LatticeLadderCase> Cases { get; }
    public LatticeLadderAuthor Author { get; } = new();
    public Relation BlockedSeamContract { get; }

    public LatticeLadderRecipes(int columns = 3, int rows = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(columns);
        ArgumentOutOfRangeException.ThrowIfNegative(rows);
        var cases = new List<LatticeLadderCase>();
        var a = Author;
        var squarePaths = new[] {
            Path(a.Word(0, 0), a.Word(2, 0), a.Word(2, 0)),
            Path(a.Word(4, 0), a.Word(0, 2), a.Word(0, 2)),
            Path(a.Word(4, 4), a.Word(-2, 0), a.Word(-2, 0)),
            Path(a.Word(0, 4), a.Word(0, -2), a.Word(0, -2)) };
        var seed = QualitativeLatticeLadder.Begin(a.Unit("split-sided square", squarePaths));
        var square = Add("L00", "Square starting grid", null,
            "Four ordered sides expose corners and side midpoints. Explicit seam declarations join repeated cells; matching coordinates alone do not join them.",
            "Shared starting construction", seed, a.Word(4, 0), a.Word(0, 4), S());

        var stretch = a.Map(a.Word(2, 0), a.Word(0, 1));
        var rectangle = Transform("L01", "Stretch into rectangles", square, stretch,
            "Every actual start, chord and pitch follows R→RR, U→U. The rectangular unit retains the square source and the native substitution receipts.",
            "Rectangular grid; precursor to B01 and B02");

        var rectanglePaths = Paths(rectangle.Unit);
        var course = a.Translate(rectanglePaths[0], Words(rectanglePaths[1])[0], out var courseRead);
        var courses = Edit(rectangle.Ladder, "insert the middle course", [], [course], courseRead);
        var split = Add("L02", "Insert a middle course", rectangle,
            "A translated copy of the actual bottom side inserts a course at the existing side midpoints. Existing four sides and their roles are carried unchanged.",
            "B01-family construction", courses, rectangle.Recipe.ColumnPitch, rectangle.Recipe.RowPitch, courseRead);

        // The two selected side paths are replaced by actual half-side
        // participation. One upper half is displaced by half the long span.
        var left = rectanglePaths[3]; var right = rectanglePaths[1];
        var lowerStart = a.Advance((Relation)left.Left, Words(left)[0], out var lowerPrefix);
        var lowerJoint = Path(lowerStart, Words(left)[1]);
        var upperStart = a.Advance((Relation)right.Left, Words(right)[0], out var upperPrefix);
        var upper = Path(upperStart, Words(right)[1]);
        var shiftedJoint = a.Translate(upper, a.Inverse(Words(rectanglePaths[0])[0]), out var jointShift);
        var brickEvidence = S(new Relation(left, new Relation(lowerJoint, lowerPrefix)),
            new Relation(right, new Relation(upper, upperPrefix)), jointShift);
        var brickLadder = Edit(split.Ladder, "alternate the selected side joints", [right, left], [lowerJoint, shiftedJoint], brickEvidence);
        var brick = Add("L03", "Shift alternate brick joints", split,
            "The side focus is replaced by a lower joint and a shifted upper joint. Horizontal courses remain actual carried paths. This changes adjacency; it is separate from the preceding stretch.",
            "B01-like running bond", brickLadder, split.Recipe.ColumnPitch, split.Recipe.RowPitch, brickEvidence);
        var shear = a.Map(a.Word(1, 0), a.Word(1, 1));
        Transform("L04", "Shear the running bond", brick, shear,
            "R→R and U→RU act on the existing brick paths and both period words. The alternating joint topology survives the oblique disposition.",
            "B19-like oblique running bond");

        var oblique = Transform("L05", "Shear the square grid", square, shear,
            "The same complete substitution shears the original square branch. No display-basis change supplies the native slant.",
            "B15-like oblique quadrilateral grid");
        var obliquePaths = Paths(oblique.Unit);
        var diagonalStep = a.Advance(Words(obliquePaths[1])[0], Words(obliquePaths[2])[0], out var diagonalRead);
        var triangularTie = Path((Relation)obliquePaths[1].Left,
            S(E(diagonalStep)), S(E(diagonalStep)));
        var triangleLadder = Edit(oblique.Ladder, "add the third tie family", [], [triangularTie],
            new Relation(new Relation(obliquePaths[1], obliquePaths[2]), diagonalRead));
        Add("L06", "Add a third tie family", oblique,
            "A new tie joins opposite advertised corners through the actual two-side displacement. The third direction creates triangular circuits. Merely shearing a four-sided grid did not create those ties.",
            "B16-like triangular network", triangleLadder, oblique.Recipe.ColumnPitch, oblique.Recipe.RowPitch, diagonalRead);

        var rising = JoinWords(Words(squarePaths[0])[0], Words(squarePaths[1])[0]);
        var falling = JoinWords(Words(squarePaths[2])[0], Words(squarePaths[1])[0]);
        var diagonalA = Path((Relation)squarePaths[0].Left, S(E(rising)), S(E(rising)));
        var diagonalB = Path((Relation)squarePaths[1].Left, S(E(falling)), S(E(falling)));
        var diagonalLadder = Edit(square.Ladder, "add two split diagonal ties", [], [diagonalA, diagonalB],
            S(new Relation(new Relation(squarePaths[0], squarePaths[1]), diagonalA),
                new Relation(new Relation(squarePaths[2], squarePaths[1]), diagonalB)));
        var diagonal = Add("L07", "Add crossing diagonal ties", square,
            "Two diagonal paths are composed from actual horizontal and vertical half-side words. Their centre is an explicit endpoint on both paths, so the local seam contract can join it.",
            "B18-like square grid with diagonals", diagonalLadder, square.Recipe.ColumnPitch, square.Recipe.RowPitch, S());
        Transform("L08", "Stretch the diagonal network", diagonal, stretch,
            "The previous diagonal network and its pitches follow the actual stretch. Its tied centre and diagonal histories remain; a second finished atlas endpoint is never substituted.",
            "B17-like stretched diagonal network");

        // One/three-reach subdivisions derive two paired line families. Their
        // crossing roles are vertices before any oblique map is applied.
        var vertical = a.ReversePath(squarePaths[3], out var reverseRead);
        var narrowHorizontal = SplitAfterFirst(squarePaths[0]);
        var narrowVertical = SplitAfterFirst(vertical);
        var otherHorizontal = a.Translate(narrowHorizontal, FirstMoveWord(narrowVertical), out var horizontalRead);
        var otherVertical = a.Translate(narrowVertical, FirstMoveWord(narrowHorizontal), out var verticalRead);
        var stripEvidence = S(reverseRead, new Relation(squarePaths[0], narrowHorizontal),
            new Relation(vertical, narrowVertical), horizontalRead, verticalRead);
        var stripLadder = Edit(square.Ladder, "assemble paired orthogonal strips", squarePaths,
            [narrowHorizontal, otherHorizontal, narrowVertical, otherVertical], stripEvidence);
        var strips = Add("L09", "Pair narrow orthogonal strips", square,
            "The bottom and reversed left side are subdivided one/three and each reused with a one-reach offset. All narrow-strip crossings have actual path endpoint roles.",
            "B07-like paired orthogonal bands", stripLadder, square.Recipe.ColumnPitch, square.Recipe.RowPitch, stripEvidence);
        var ribbonMap = a.Map(a.Word(2, 1), a.Word(-2, 1));
        Transform("L10", "Turn paired strips into oblique ribbons", strips, ribbonMap,
            "R→RRU and U→LLU carry the paired-strip incidence and both periods into an oblique ribbon arrangement. The narrow and wide intervals remain native repeated participation.",
            "B08-like intersecting ribbons");

        var insetRows = S(squarePaths.SelectMany(Words).Select(word => (IElement)new Relation(word,
            S(E(word).Select(move => (IElement)S(move)).ToArray()))).ToArray());
        var insetLadder = square.Ladder.Refine("expose one-reach corner insets", insetRows);
        var insets = Add("L11", "Expose corner inset sites", square,
            "Each two-reach chord is refined into its two actual move occurrences. The square outline is unchanged while the sites needed for a corner cut become explicit.",
            "B06-family preparation", insetLadder, square.Recipe.ColumnPitch, square.Recipe.RowPitch, insetRows);
        var insetPaths = Paths(insets.Unit);
        var chamferStart = a.Advance((Relation)insetPaths[0].Left, Words(insetPaths[0])[0], out var chamferPrefix);
        var chamferWords = new List<Relation>(); var cornerRows = new List<IElement>();
        for (var index = 0; index < insetPaths.Length; index++)
        {
            var current = Words(insetPaths[index]); var next = Words(insetPaths[(index + 1) % insetPaths.Length]);
            // The two middle unit chords remain actual old chord identities.
            chamferWords.AddRange(current.Skip(1).Take(current.Length - 2));
            var bridge = JoinWords(current[^1], next[0]); chamferWords.Add(bridge);
            cornerRows.Add(new Relation(new Relation(current[^1], next[0]), bridge));
        }
        var chamferPath = Path(chamferStart, chamferWords.ToArray());
        var chamferLadder = Edit(insets.Ladder, "replace corners by chamfer chords", insetPaths,
            [chamferPath], new Relation(chamferPrefix, S(cornerRows.ToArray())), [chamferPath]);
        Add("L12", "Chamfer the square corners", insets,
            "Corner participation is replaced by diagonal bridge chords between the inset sites. Middle side chords remain actual source words. This is a chord/topology edit, with an explicit closed contour, rather than an affine deformation of four corners.",
            "B06-like octagon and interstitial diamond field", chamferLadder,
            square.Recipe.ColumnPitch, square.Recipe.RowPitch, S(cornerRows.ToArray()));

        // A basket is an assembly of reused horizontal/vertical rectangles.
        // Refine long sides first so every intended side contact is a port.
        var basketRows = S(rectanglePaths.SelectMany(Words).Where(word => E(word).Length == 4)
            .Select(word => (IElement)new Relation(word,
                S(S(E(word).Take(2).ToArray()), S(E(word).Skip(2).ToArray())))).ToArray());
        var basketSeedLadder = rectangle.Ladder.Refine("expose rectangle side contacts", basketRows);
        var basketSeed = Add("L13", "Expose basket contact intervals", rectangle,
            "Only the long rectangle chords are subdivided. The outline remains rectangular, while future side contacts become actual path vertices. The next assembly publishes their new endpoint roles.",
            "B02-family assembly preparation", basketSeedLadder, rectangle.Recipe.ColumnPitch,
            rectangle.Recipe.RowPitch, basketRows);
        var quarter = a.Map(a.Word(0, 1), a.Word(-1, 0));
        var turned = basketSeed.Unit.Transform("actual quarter-oriented rectangle", quarter);
        var assembly = new List<Relation>(); var placements = new List<IElement>();
        // Centring and displacement use half-side participation from the
        // current rectangular unit. The four placements are an assembly law,
        // not a shifted-joint interpretation of the brick branch.
        var halfLong = Words(rectanglePaths[0])[0]; var halfShort = Words(rectanglePaths[1])[0];
        var centre = JoinWords(a.Inverse(halfLong), a.Inverse(halfShort));
        var centreTurn = a.MapWord(quarter, centre, out var centreTurnRead);
        var shortTurn = a.MapWord(quarter, halfShort, out var shortTurnRead);
        var spacing = JoinWords(halfLong, a.Inverse(shortTurn));
        var rise = a.MapWord(quarter, spacing, out var riseRead);
        foreach (var placement in new[] {
            (Unit: basketSeed.Unit, Shift: centre),
            (Unit: turned, Shift: JoinWords(centreTurn, spacing)),
            (Unit: basketSeed.Unit, Shift: JoinWords(centre, spacing, rise)),
            (Unit: turned, Shift: JoinWords(centreTurn, rise)) })
        {
            var copies = new List<IElement>();
            foreach (var sourcePath in Paths(placement.Unit))
            {
                var copy = a.Translate(sourcePath, placement.Shift, out var receipt);
                assembly.Add(copy); copies.Add(receipt);
            }
            placements.Add(new Relation(placement.Unit.Surface, new Relation(placement.Shift, S(copies.ToArray()))));
        }
        var assemblyEvidence = new Relation(turned.Evidence, new Relation(S(centreTurnRead, shortTurnRead, riseRead), S(placements.ToArray())));
        var basketLadder = Edit(basketSeed.Ladder, "assemble four alternating rectangles", Paths(basketSeed.Unit), assembly.ToArray(), assemblyEvidence);
        var basketPitch = JoinWords(spacing, spacing); var basketRise = JoinWords(rise, rise);
        var basket = Add("L14", "Assemble alternating basket rectangles", basketSeed,
            "The actual rectangle is reused twice directly and twice through its quarter substitution, with four retained translation receipts. Split side contacts leave square gaps. This assembly branch has different path topology from running brick.",
            "B02-like basket rectangles and square gaps", basketLadder, basketPitch, basketRise, assemblyEvidence);
        Transform("L15", "Shear the basket assembly", basket, shear,
            "The complete basket assembly is sheared with all contacts and periods. Its four-copy construction stays distinct from the sheared running-bond branch even where a coarse directional read agrees.",
            "Exploratory oblique basket branch");

        Transform("L16", "Try a wider shear", square, a.Map(a.Word(1, 0), a.Word(2, 1)),
            "A sibling generated from the same square substitutes U→RRU. The explicit binding and the native word receipts retain this larger offset separately from the first shear.",
            "New sibling of the oblique-grid family");
        Transform("L17", "Try an opposed brick shear", brick, a.Map(a.Word(1, 0), a.Word(-1, 1)),
            "The same running-bond parent receives U→LU. Its alternating joints remain, while the shear route differs from the earlier R→R, U→RU branch.",
            "New sibling of the oblique-brick family");
        Transform("L18", "Try a tighter ribbon disposition", strips, a.Map(a.Word(1, 1), a.Word(-1, 1)),
            "The paired-strip parent receives R→RU, U→LU. The one/three intervals and explicit crossing ports are reused with a different complete native substitution.",
            "New sibling of the ribbon family");

        Cases = cases.AsReadOnly();
        BlockedSeamContract = LatticeLadderAuthor.BlockedContract(square.Recipe);

        LatticeLadderCase Add(string id, string name, LatticeLadderCase? parent, string description,
            string atlas, QualitativeLatticeLadder ladder, Relation column, Relation row, Relation pitchEvidence)
        {
            if (!ladder.Current.IsComplete) throw new InvalidOperationException($"The authored rung {id} did not settle structurally.");
            var recipe = a.Compile(id, name, description, atlas, ladder.Current.Program, column, row, columns, rows);
            var result = new LatticeLadderCase(id, name, parent?.Id, description, atlas, ladder, recipe, pitchEvidence);
            cases.Add(result); return result;
        }

        LatticeLadderCase Transform(string id, string name, LatticeLadderCase source,
            QualitativePatternTransform transform, string description, string atlas)
        {
            var ladder = source.Ladder.Transform(name, transform);
            var column = a.MapWord(transform, source.Recipe.ColumnPitch, out var columnRead);
            var row = a.MapWord(transform, source.Recipe.RowPitch, out var rowRead);
            return Add(id, name, source, description, atlas, ladder, column, row,
                new Relation(transform.Surface, new Relation(columnRead, rowRead)));
        }
    }

    private static QualitativeLatticeLadder Edit(QualitativeLatticeLadder source, string name,
        Relation[] selected, Relation[] introduced, IElement evidence, Relation[]? closed = null)
    {
        var oldPorts = E(source.Current.Program.Ports).Cast<Relation>().Where(port =>
            !selected.Any(path => ReferenceEquals(path, ((Relation)port.Right).Left))).Cast<IElement>();
        var ports = S(oldPorts.Concat(E(Ports(introduced))).ToArray());
        var closedPaths = E(source.Current.Program.ClosedPaths)
            .Where(path => !selected.Any(actual => ReferenceEquals(actual, path))).Concat(closed ?? []).ToArray();
        return source.ReplacePaths(name, S(selected), S(introduced), ports, S(closedPaths), evidence);
    }

    private static Relation SplitAfterFirst(Relation path)
    {
        var moves = Words(path).SelectMany(E).ToArray();
        return Path((Relation)path.Left, S(moves[0]), S(moves.Skip(1).ToArray()));
    }
    private static Relation FirstMoveWord(Relation path) => S(E(Words(path)[0])[0]);
    private static Relation[] Paths(UnitArrangementProgram program) => E(program.Paths).Cast<Relation>().ToArray();
    private static Relation[] Words(Relation path) => E((Relation)path.Right).Cast<Relation>().ToArray();
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
