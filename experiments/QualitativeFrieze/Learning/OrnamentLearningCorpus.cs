using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze.Learning;

/// <summary>A bounded, reproducible curriculum of executed native programs.
/// Descriptor names select authoring factories; they are never receiver clues
/// or concept answers. Each row is materialized independently and may be
/// discarded after training, rather than retaining a corpus-sized proof DAG.</summary>
public static class OrnamentLearningCorpus
{
    public const int FamilyCount = 12;
    public const int EditCount = 8;
    public const int GroupCount = FamilyCount * EditCount;
    public const string Version = "ornament-learning-corpus-v1";

    /// <summary>Experiment bookkeeping, deliberately outside native evidence.
    /// All scale, phase, orientation and reflection siblings of one recipe/edit
    /// construction belong to the same partition, independently of the seed.</summary>
    public sealed record Descriptor(int Index, string Group, string Split,
        int Family, int Edit, int Variation)
    { public int Variant => Variation; }

    public sealed record EvidenceStream(string Name, NativeLatticeSources.SourceCase Source,
        Relation Surface, Relation Omitted);

    public sealed class Sample
    {
        public Descriptor Descriptor { get; }
        /// <summary>Clean teacher observation of one actual executed unit.
        /// Never give this object or the complete audit Surface to a receiver.</summary>
        public NativeLatticeSources.SourceCase Target { get; }
        /// <summary>A one-to-four-unit arrangement executed by the same program.
        /// It is a teacher/demo view, not an extra receiver observation.</summary>
        public NativeLatticeSources.SourceCase Pattern { get; }
        public IReadOnlyList<EvidenceStream> Streams { get; }
        public OrnamentProgram Program { get; }
        public Relation Bindings { get; }
        public Relation Window { get; }
        public Relation Construction { get; }
        public Relation RecipePacket { get; }
        public Relation Surface { get; }
        internal Sample(Descriptor descriptor, NativeLatticeSources.SourceCase target,
            NativeLatticeSources.SourceCase pattern, IReadOnlyList<EvidenceStream> streams,
            OrnamentProgram program, Relation bindings, Relation window, Relation construction)
        {
            Descriptor = descriptor; Target = target; Pattern = pattern; Streams = streams;
            Program = program; Bindings = bindings; Window = window; Construction = construction;
            RecipePacket = new(program.Surface, new Relation(bindings, window));
            // audit = executable recipe -> (teacher observations -> public streams).
            // The receiver receives stream.Surface roots only. No backreference
            // from a stream to this audit root supplies the hidden teacher.
            Surface = new(RecipePacket,
                new Relation(new Relation(target.Graph.Surface, target.Bindings),
                    new Relation(new Relation(pattern.Graph.Surface, pattern.Bindings),
                        new Relation(S(streams.Select(stream => (IElement)stream.Surface).ToArray()), construction))));
        }
    }

    public static IReadOnlyList<Descriptor> Describe(int count, int seed = 1729, bool splitByFamily = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new Descriptor[count];
        var offset = (int)((uint)seed % 4096);
        for (var index = 0; index < count; index++)
        {
            var group = index % GroupCount;
            var family = group % FamilyCount; var edit = group / FamilyCount;
            var bucket = (family * 7 + edit * 3) % 10;
            // The stricter curriculum deliberately reserves two complete
            // construction families for development and two for testing.
            // This administrative partition never enters the receiver graph.
            var partition = splitByFamily ? family is 4 or 7 ? "validation" : family is 8 or 9 ? "test" : "train"
                : bucket < 7 ? "train" : bucket < 9 ? "validation" : "test";
            result[index] = new(index, splitByFamily ? $"recipe-{family:00}" : $"recipe-{family:00}-edit-{edit:00}",
                partition, family, edit, checked(index / GroupCount * 37 + offset));
        }
        return result;
    }

    /// <summary>Builds only the requested native sample. Host integers below
    /// select repeated uncalibrated participation while authoring. No scalar
    /// coordinates, shape labels, images or teacher features enter a stream.</summary>
    public static Sample Materialize(Descriptor descriptor, bool includePattern = true)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.Family is < 0 or >= FamilyCount || descriptor.Edit is < 0 or >= EditCount || descriptor.Variation < 0)
            throw new ArgumentException("A corpus descriptor needs an available native construction.", nameof(descriptor));
        var author = new LatticeLadderAuthor(); var variation = descriptor.Variation;
        var w = 4 + variation % 3; var h = 3 + variation / 24 % 3;
        var paths = BasePaths(author, descriptor.Family, w, h);
        var baseUnit = author.Unit("shared curriculum unit", paths);
        var unit = Edit(author, baseUnit, descriptor.Edit, out var editWitness);
        var rotation = variation / 3 % 4; var mirrored = variation / 12 % 2 == 1;
        var right = mirrored ? author.Word(-1, 0) : author.Word(1, 0);
        var up = author.Word(0, 1);
        for (var turn = 0; turn < rotation; turn++)
        {
            var quarterTurn = author.Map(author.Word(0, 1), author.Word(-1, 0));
            right = author.MapWord(quarterTurn, right, out _);
            up = author.MapWord(quarterTurn, up, out _);
        }
        var transform = author.Map(right, up);
        var oriented = unit.Transform("native orientation variant", transform);
        var displacement = author.Word(variation / 48 % 3 - 1, variation / 144 % 3 - 1);
        var translationWitnesses = new List<IElement>();
        var translated = N.E(oriented.Paths).Cast<Relation>().Select(path =>
        { var result = author.Translate(path, displacement, out var witness); translationWitnesses.Add(witness); return result; }).ToArray();
        // Translation is an explicit native path replacement. Selectors are
        // rebuilt against actual new path/chord identities; no coordinate edit
        // mutates an already published teacher observation.
        var closedPaths = translated.Where(path => UnitArrangementRead.IsZero(author.Frame,
            author.Frame.Reduce(S(), LatticeLadderAuthor.JoinWords(N.E((Relation)path.Right).Cast<Relation>().ToArray())))).ToArray();
        var finalUnit = new UnitArrangementProgram(author.Frame, "translated native unit", S(translated),
            LatticeLadderAuthor.Ports(translated), S(closedPaths));
        var columns = 1 + variation / 432 % 2; var rows = 1 + variation / 864 % 2;
        if (columns * rows * N.E(finalUnit.Paths).Cast<Relation>().Sum(path => N.E((Relation)path.Right).Length) > 64) rows = 1;
        if (!includePattern) { columns = 1; rows = 1; }
        var recipe = author.Compile("curriculum", "native curriculum sample", "Executed unmeasured paths",
            "Limited structural curriculum", finalUnit, author.Word(18, 0), author.Word(0, 18), columns, rows);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var field = includePattern ? new OrnamentProgramField(run, recipe.SeamContract) : null;
        if (!run.IsComplete || field is { IsComplete: false } || run.Units.Count == 0)
            throw new InvalidOperationException("A curriculum program must execute before its evidence is published.");
        var one = run.Units[0].Current.Join;
        var targetPublication = Publish(one.Graph, one.Bindings); var target = targetPublication.Published;
        var patternPublication = field is null ? targetPublication : Publish(field.Graph, field.Bindings); var pattern = patternPublication.Published;
        var streams = MakeStreams(target, variation);
        var construction = new Relation(new Relation(baseUnit.Surface, new Relation(unit.Surface, editWitness)),
            new Relation(new Relation(transform.Surface, oriented.Surface), new Relation(displacement,
                new Relation(S(translationWitnesses.ToArray()), new Relation(finalUnit.Surface,
                    new Relation(run.Applications, new Relation(targetPublication.Surface, patternPublication.Surface)))))));
        return new(descriptor, target, pattern, streams, recipe.Program, recipe.Bindings, recipe.Window, construction);
    }

    private static NativeLinePublication Publish(QualitativeStrokeGraph graph, Relation bindings)
    {
        var source = new NativeLatticeSources.SourceCase("sample", "Public native passages", "No intended compartments are supplied.",
            graph, bindings, graph.Strokes, S(), true);
        return new NativeLinePublication(source);
    }

    private static IReadOnlyList<EvidenceStream> MakeStreams(NativeLatticeSources.SourceCase target, int variation)
    {
        var strokes = N.E(target.Graph.Strokes); var bindings = N.E(target.Bindings);
        var streams = new List<EvidenceStream>();
        // Two independently masked evidence sources share actual site aliases.
        // Their union is deliberately incomplete except for full-cue controls.
        // Mask selection is decorrelated from the width/orientation schedules.
        var mask = Mix((uint)variation); var fullCue = (mask >> 8) % 10 == 0;
        var first = strokes.Select((_, index) => fullCue || Keep(index, 0x23a9, 2)).ToArray();
        var second = strokes.Select((_, index) => Keep(index, 0x81d7, 3)).ToArray();
        if (!fullCue && strokes.Length > 2 && first.Zip(second).All(pair => pair.First || pair.Second))
        {
            var omitted = (int)(mask % (uint)strokes.Length); first[omitted] = false; second[omitted] = false;
        }
        Add("passages", strokes.Where((_, index) => first[index]).ToArray(), bindings,
            strokes.Where((_, index) => !first[index]).ToArray());
        var selectedBindings = bindings.Where((_, index) => Keep(index, 0x7b21, 3)).ToArray();
        Add("landmarks", strokes.Where((_, index) => second[index]).ToArray(), selectedBindings,
            strokes.Where((_, index) => !second[index]).Concat(bindings.Where((_, index) => !Keep(index, 0x7b21, 3))).ToArray());
        // A different evidence source sometimes disagrees about one site's
        // word. The original reply remains in the same stream, so conflict is
        // explicit multiplicity, rather than an unmarked replacement or label.
        if ((mask >> 12) % 4 == 0 && bindings.OfType<Relation>().FirstOrDefault() is Relation firstBinding)
        {
            var frame = target.Graph.Frame;
            var reach = N.E((Relation)firstBinding.Right).OfType<Relation>().Select(move => move.Right).FirstOrDefault()
                ?? strokes.OfType<Relation>().SelectMany(stroke => QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word)
                    ? N.E(word).OfType<Relation>().Select(move => move.Right) : []).FirstOrDefault() ?? new Placeholder();
            var shift = S(QualitativeMotionFrame.Move(frame.Right, reach));
            var cancellation = frame.Reduce((Relation)firstBinding.Right, shift);
            if (!frame.TryReadResult(cancellation, out var shifted, out _, out _)) throw new InvalidOperationException("A native offset must read.");
            Add("competing landmark", [], [firstBinding, new Relation(firstBinding.Left, shifted)], []);
        }
        return streams;

        bool Keep(int index, uint salt, uint denominator) =>
            Mix(unchecked((uint)variation * 0x9e3779b1u + (uint)index * 0x85ebca6bu + salt)) % denominator == 0;

        void Add(string name, IElement[] passages, IElement[] facts, IElement[] omitted)
        {
            // Open scope means the supplier intentionally withholds other
            // passages/bindings; absence is not a complete negative inventory.
            var graph = new QualitativeStrokeGraph(target.Graph.Frame, target.Graph.Sites,
                passages.Length == strokes.Length ? S(passages) : StructuralConstraint.Open(passages), target.Graph.PeriodWord);
            var source = target with { Graph = graph, Bindings = facts.Length == bindings.Length ? S(facts) : StructuralConstraint.Open(facts),
                StraightPassages = S(passages), Provenance = S(), TurnModel = variation % 5 != 0 };
            var basis = new NativeLineBasis(graph.Frame, source.TurnModel);
            var surface = new Relation(new Relation(graph.Surface, source.Bindings), new Relation(source.StraightPassages, basis.Surface));
            // Omitted is an external teacher/audit comparison, never reachable
            // from Surface. Giving it to a learner would leak the answer.
            streams.Add(new(name, source, surface, S(omitted)));
        }
    }

    private static uint Mix(uint value)
    {
        value ^= value >> 16; value = unchecked(value * 0x7feb352du);
        value ^= value >> 15; value = unchecked(value * 0x846ca68bu);
        return value ^ value >> 16;
    }

    private static UnitArrangementProgram Edit(LatticeLadderAuthor a, UnitArrangementProgram original, int edit, out Relation witness)
    {
        var paths = N.E(original.Paths).Cast<Relation>().ToArray(); var first = paths[0];
        var words = N.E((Relation)first.Right).Cast<Relation>().ToArray();
        var chords = new List<Relation>(); var replacements = new List<IElement>();
        if (edit is 2 or 5 or 7)
        {
            var selected = edit == 5 ? words.Take(Math.Min(2, words.Length)).ToArray() : [words[0]];
            foreach (var word in selected)
            {
                var terms = N.E(word);
                // An excursion needs a before, an altered middle and an after.
                // Do not manufacture finer reaches just to notch a single
                // primitive participation, or retrace a neighbour at a corner.
                if (terms.Length < 3) continue;
                var cut = Math.Max(1, terms.Length / 3);
                var middleCount = Math.Max(1, terms.Length - cut * 2);
                var before = S(terms.Take(cut).ToArray());
                var middle = S(terms.Skip(cut).Take(middleCount).ToArray());
                var after = S(terms.Skip(cut + middleCount).ToArray());
                var excursion = a.Word(0, 1);
                if (terms.OfType<Relation>().All(move => a.Frame.Direction(move, out var axis, out _, out _) && ReferenceEquals(axis, a.Frame.Vertical)))
                    excursion = a.Word(1, 0);
                var chain = new List<IElement> { before, excursion, middle, a.Inverse(excursion), after };
                replacements.Add(new Relation(word, S(chain.ToArray())));
            }
            var refined = original.Refine("local native excursion", S(replacements.ToArray()));
            paths = N.E(refined.Paths).Cast<Relation>().ToArray();
            first = paths[0]; words = N.E((Relation)first.Right).Cast<Relation>().ToArray();
        }
        if (edit is 1 or 7)
        {
            if (words.Length > 1) paths[0] = new(first.Left, S(words.Take(words.Length - 1).Cast<IElement>().ToArray()));
        }
        if (edit == 3 && words.Length >= 3)
        {
            var middle = Math.Max(2, words.Length / 2);
            var end = a.Advance((Relation)first.Left, LatticeLadderAuthor.JoinWords(words.Take(middle).ToArray()), out _);
            var displacement = a.Advance(end, a.Inverse((Relation)first.Left), out _);
            chords.Add(LatticeLadderAuthor.Path((Relation)first.Left, displacement));
        }
        if (edit == 4)
            chords.Add(Route(a, (1, 1), (2, 1), (2, 2), (1, 2), (1, 1)));
        if (edit == 6)
            chords.Add(LatticeLadderAuthor.Path((Relation)first.Left, a.Word(-1, -1)));
        var unit = a.Unit("edited native recipe", paths.Concat(chords).ToArray());
        witness = new(original.Surface, new Relation(S(replacements.ToArray()), unit.Surface));
        return unit;
    }

    private static Relation[] BasePaths(LatticeLadderAuthor a, int family, int w, int h) => family switch
    {
        0 => [Route(a, (0, 0), (w, 0), (w, w), (0, w), (0, 0))],
        1 => [Route(a, (0, 0), (w + 2, 0), (w + 2, h), (0, h), (0, 0))],
        2 => [Route(a, (0, 0), (2 * h, 0), (h, h), (0, 0))],
        3 => [Route(a, (0, 0), (h, h), (0, 2 * h), (-h, h), (0, 0))],
        4 => [Route(a, (0, 0), (w, 0), (w + 1, h), (w, 2 * h), (0, 2 * h), (-1, h), (0, 0))],
        5 => [Route(a, (0, 0), (w, 0), (w, 1), (1, 1), (1, h), (0, h), (0, 0))],
        6 => [Route(a, (0, 0), (w, 0), (w, h), (w - 1, h), (w - 1, h - 1), (1, h - 1), (1, h), (0, h), (0, 0))],
        7 => [Route(a, (0, 0), (6, 0), (6, h), (5, h - 1), (4, h), (3, h - 1), (2, h), (1, h - 1), (0, h), (0, 0))],
        8 => [Route(a, (0, -h), (1, -1), (h, 0), (1, 1), (0, h), (-1, 1), (-h, 0), (-1, -1), (0, -h))],
        9 => [Route(a, (h, 0), (2 * h, 0), (2 * h, h), (3 * h, h), (3 * h, 2 * h), (2 * h, 2 * h),
            (2 * h, 3 * h), (h, 3 * h), (h, 2 * h), (0, 2 * h), (0, h), (h, h), (h, 0))],
        10 => [Route(a, (0, 0), (w, 0), (w, h), (0, h), (0, 0)),
               Route(a, (w / 2, h), (w + w / 2, h), (w + w / 2, 2 * h), (w / 2, 2 * h), (w / 2, h))],
        11 => [Route(a, (0, 0), (w, 0)), Route(a, (0, h), (w, h)), Route(a, (0, 2 * h), (w, 2 * h)),
               Route(a, (0, 0), (0, 2 * h)), Route(a, (w / 2, 0), (w / 2, 2 * h)), Route(a, (w, 0), (w, 2 * h))],
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    private static Relation Route(LatticeLadderAuthor author, params (int X, int Y)[] points) =>
        LatticeLadderAuthor.Path(author.Word(points[0].X, points[0].Y), Enumerable.Range(0, points.Length - 1)
            .Select(index => author.Word(points[index + 1].X - points[index].X, points[index + 1].Y - points[index].Y)).ToArray());
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
}
