using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class BourgoinPatternLibrary
{
    private static SourceRevision SlidTriangleGaps() => TriangleSlideRevisions(3, [2])[0];
    private static SourceRevision SlidHexagonGaps() => HexagonSlideRevisions(4, [-1])[0];

    /// <summary>Finite authoring samples sharing one actual coarse triangle.
    /// Parameters count the chosen common unmeasured reach. Execution receives
    /// the native words, refinements, bindings and seam law, not these integers.</summary>
    public static IReadOnlyList<Recipe> CreateTriangleSlides(int side, params int[] gaps) =>
        TriangleSlideRevisions(side, gaps).Select(revision => SlidingRecipe(revision, "B24",
            "Sliding triangle-side contacts", 276, "Fig. 193")).ToArray();

    /// <summary>Finite authoring samples sharing one actual coarse hexagon.
    /// Opposite signed tangential shifts preserve the contour and reverse the
    /// handedness of its partial-side disposition.</summary>
    public static IReadOnlyList<Recipe> CreateHexagonSlides(int side, params int[] slides) =>
        HexagonSlideRevisions(side, slides).Select(revision => SlidingRecipe(revision, "B29",
            "Sliding hexagon-side contacts", 424, "Plate 17, fig. 4")).ToArray();

    private static SourceRevision[] TriangleSlideRevisions(int side, int[] gaps)
    {
        if (side <= 0 || gaps.Length == 0 || gaps.Any(gap => gap <= 0 || gap > side))
            throw new ArgumentException("Triangle samples require a positive side and a positive residual gap no longer than that side.");
        var source = new Cell();
        var prototype = source.SlidingCoarse(side, (0, 0), (side, 0), (0, side), (0, 0));
        return gaps.Select(gap =>
        {
            var cell = source.SlidingSibling();
            var seed = cell.SlidingRefinement(prototype, gap, contactFirst: false);
            cell.CorrectedImage(seed, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
            cell.CorrectedImage(seed, cell.CorrectedMap((-1, 0), (0, -1)), (side + gap, 0));
            return new SourceRevision(cell, (side + gap, gap - side), (side - gap, 2 * gap),
                "Two opposed images share one three-side prototype. Each whole side retains its exposed fragment and its contact subspan. Nonempty contact subspans give partial-side overlaps and shorter six-edge gap circuits; an empty contact gives the tip-only baseline. The gaps have no separately authored contour.",
                $"Revised Fig. 193 reading: long triangle sides share only a terminal subspan when the residual gap is shorter than the side. The selected sample uses {side} shared reaches per triangle side and {gap} per residual gap side. These are finite word proportions, not calibrated engraving distances or a filled-face theorem.");
        }).ToArray();
    }

    private static SourceRevision[] HexagonSlideRevisions(int side, int[] slides)
    {
        if (side <= 0 || slides.Length == 0 || slides.Any(slide => Math.Abs((long)slide) >= side))
            throw new ArgumentException("Hexagon samples require a positive side and a tangential shift strictly shorter than it.");
        var source = new Cell();
        var prototype = source.SlidingCoarse(side,
            (side, 0), (0, side), (-side, side), (-side, 0), (0, -side), (side, -side), (side, 0));
        return slides.Select(slide =>
        {
            var cell = source.SlidingSibling();
            var seed = cell.SlidingRefinement(prototype, Math.Abs(slide), contactFirst: slide <= 0);
            cell.CorrectedImage(seed, cell.CorrectedMap((1, 0), (0, 1)), (0, 0));
            return new SourceRevision(cell, (2 * side, -side + slide), (side - slide, side + slide),
                "One six-side prototype repeats with a tangential offset along each neighbouring side. Actual contact-end landmarks split each long side into a shared subspan and an exposed remainder. Nonzero offsets leave small triangular gap circuits; zero offset retains aligned whole-side contacts. No triangle or hook motif is added.",
                $"Revised Plate 17 fig. 4 reading: relatively slid hexagons, rather than hexagons decorated with triangular hooks. The selected sample uses {side} shared reaches per side and a signed {slide}-reach tangential offset. Botanical decoration, ink width, exact distances, coincident-edge folding and filled-region inference remain outside this contour construction.");
        }).ToArray();
    }

    private static Recipe SlidingRecipe(SourceRevision revision, string id, string name, int leaf, string reference)
    {
        var cell = revision.Cell;
        var program = cell.Program(name, revision.Column, revision.Row, false, out var bindings);
        var phases = (Relation)program.Layout.Left;
        var firstColumn = N.E((Relation)((Relation)phases.Left).Left)[0];
        var firstRow = N.E((Relation)((Relation)phases.Right).Left)[0];
        return new(id, name, leaf, 0, reference, revision.Construction, revision.Fidelity,
            program, bindings, OrnamentProgram.Window(firstColumn, firstRow, 3, 3),
            cell.Word(revision.Column), cell.Word(revision.Row), cell.Seams(program, bindings, revision.Column, revision.Row), Triangular);
    }

    private sealed partial class Cell
    {
        public Cell() { }
        private Cell(QualitativeMotionFrame sharedFrame, IElement sharedReach)
        { frame = sharedFrame; reach = sharedReach; }
        public Cell SlidingSibling() => new(frame, reach);

        public sealed record SlidingPrototype(UnitArrangementProgram Program, Relation Path,
            Relation[] Sides, (int X, int Y)[] Steps, int Side);

        public SlidingPrototype SlidingCoarse(int side, params (int X, int Y)[] corners)
        {
            var words = Enumerable.Range(0, corners.Length - 1).Select(index => Word(
                (corners[index + 1].X - corners[index].X, corners[index + 1].Y - corners[index].Y))).ToArray();
            var steps = Enumerable.Range(0, corners.Length - 1).Select(index =>
                ((corners[index + 1].X - corners[index].X) / side, (corners[index + 1].Y - corners[index].Y) / side)).ToArray();
            var path = new Relation(Word(corners[0]), N.S(words));
            var selectors = words.Select(word => (IElement)new Relation(new Placeholder(), new Relation(path, word)))
                .Append(new Relation(new Placeholder(), new Relation(path, path))).ToArray();
            var program = new UnitArrangementProgram(frame, "whole-side contour", N.S(path), N.S(selectors), N.S(path));
            return new(program, path, words, steps, side);
        }

        public CorrectedPathSeed SlidingRefinement(SlidingPrototype prototype, int exposedSteps, bool contactFirst)
        {
            var replacements = new List<IElement>();
            var partitions = new List<(Relation Whole, Relation Contact, Relation Exposed, Relation Pieces, Relation Sum, Relation Account)>();
            for (var index = 0; index < prototype.Sides.Length; index++)
            {
                var whole = prototype.Sides[index]; var step = prototype.Steps[index];
                var contact = Word((step.X * (prototype.Side - exposedSteps), step.Y * (prototype.Side - exposedSteps)));
                var exposed = Word((step.X * exposedSteps, step.Y * exposedSteps));
                var ordered = contactFirst ? new[] { contact, exposed } : new[] { exposed, contact };
                var pieces = N.S(ordered.Where(word => N.E(word).Length != 0).Cast<IElement>().ToArray());
                var flattened = N.S(N.E(pieces).Cast<Relation>().SelectMany(N.E).ToArray());
                var sum = frame.Reduce(N.S(), flattened);
                var accounting = frame.Reduce(whole, UnitArrangementRead.Inverse(frame, flattened));
                if (!UnitArrangementRead.IsZero(frame, accounting))
                    throw new InvalidOperationException("A selected side partition must retain its whole endpoint displacement.");
                replacements.Add(new Relation(whole, pieces)); partitions.Add((whole, contact, exposed, pieces, sum, accounting));
            }
            var refined = prototype.Program.Refine("side contact landmarks", N.S(replacements.ToArray()));
            RetainConstruction(refined.Surface);
            // row = (actual refined programme -> (actual coarse path -> whole side))
            //       -> ((contact subword -> exposed subword)
            //           -> (ordered actual fragments -> (sum read -> zero accounting read))).
            // The selected subspan is a local contact question. Actual joins are
            // still admitted separately through current endpoint publications.
            foreach (var part in partitions)
                RetainConstruction(new Relation(new Relation(refined.Surface, new Relation(prototype.Path, part.Whole)),
                    new Relation(new Relation(part.Contact, part.Exposed),
                        new Relation(part.Pieces, new Relation(part.Sum, part.Account)))));
            var path = (Relation)N.E(refined.Paths).Single();
            return new(new Relation(path, N.S(path)), (Relation)path.Left, N.E((Relation)path.Right).Cast<Relation>().ToArray(), true);
        }
    }
}
