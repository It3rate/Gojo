using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>One finite native ornament definition, sampled and rebound several
/// ways. Loaded variations select actual archived parameter choices; they do
/// not reconstruct a recipe from a case name or an external source factory.</summary>
public sealed class OrnamentProgramStudy
{
    public sealed record Variant(string Id, string Name, string Description,
        OrnamentProgram Program, Relation Bindings, Relation Window,
        OrnamentProgramRun Run, bool LoadedFromArchive);

    public OrnamentProgram Program { get; }
    public Relation BaseBindings { get; }
    public Relation BaseWindow { get; }
    public string Archive { get; }
    public OrnamentProgramSample LoadedSample { get; }
    public IReadOnlyList<Variant> Variants { get; }
    public Relation Surface { get; }

    public OrnamentProgramStudy()
    {
        var frame = new QualitativeMotionFrame();
        IElement reach = new Placeholder();
        Relation W(params IElement[] directions) => N.S(directions.Select(direction =>
            (IElement)QualitativeMotionFrame.Move(direction, reach)).ToArray());
        Relation Repeated(IElement direction, int occurrences) => W(Enumerable.Repeat(direction, occurrences).ToArray());

        var diamond = Path("opposed four-side diamond", W(frame.Down, frame.Down),
            [W(frame.Right, frame.Right, frame.Up, frame.Up), W(frame.Left, frame.Left, frame.Up, frame.Up),
             W(frame.Left, frame.Left, frame.Down, frame.Down), W(frame.Right, frame.Right, frame.Down, frame.Down)], true);
        var hook = Path("stepped hook", W(frame.Left, frame.Left, frame.Down),
            [W(frame.Up, frame.Up, frame.Up), W(frame.Right, frame.Right, frame.Right),
             W(frame.Down, frame.Down), W(frame.Left, frame.Left), W(frame.Up), W(frame.Right)], false);
        var zigzag = Path("four diagonal turns", W(frame.Left, frame.Left),
            [W(frame.Right, frame.Up), W(frame.Right, frame.Down), W(frame.Right, frame.Up), W(frame.Right, frame.Down)], false);

        var identity = Map(frame.Right, frame.Left, frame.Up, frame.Down);
        var mirror = Map(frame.Left, frame.Right, frame.Up, frame.Down);
        var turn = Map(frame.Up, frame.Down, frame.Left, frame.Right);
        var motifChoices = N.S(diamond.Surface, hook.Surface, zigzag.Surface);
        var transformChoices = N.S(identity.Surface, mirror.Surface, turn.Surface);
        // Repetition is actual participation in a shared reach. No measured
        // length is needed to distinguish these finite step words.
        var horizontalChoices = N.S(Repeated(frame.Right, 6), Repeated(frame.Right, 4),
            W(frame.Right, frame.Right, frame.Right, frame.Right, frame.Up, frame.Up));
        var verticalChoices = N.S(Repeated(frame.Up, 6), Repeated(frame.Up, 4),
            W(frame.Right, frame.Right, frame.Right, frame.Up, frame.Up, frame.Up, frame.Up, frame.Up, frame.Up));
        var slots = Enumerable.Range(0, 6).Select(_ => (IElement)new Placeholder()).ToArray();
        var parameters = N.S(new Relation(slots[0], motifChoices), new Relation(slots[1], motifChoices),
            new Relation(slots[2], transformChoices), new Relation(slots[3], transformChoices),
            new Relation(slots[4], horizontalChoices), new Relation(slots[5], verticalChoices));
        var u = new IElement[] { new Placeholder(), new Placeholder() };
        var v = new IElement[] { new Placeholder(), new Placeholder() };
        Relation Cycle(IElement[] phases) => new(N.S(phases), N.S(new Relation(phases[0], phases[1]), new Relation(phases[1], phases[0])));
        var rules = N.S(new Relation(new Relation(u[0], v[0]), new Relation(slots[0], slots[2])),
            new Relation(new Relation(u[1], v[0]), new Relation(slots[1], slots[3])),
            new Relation(new Relation(u[0], v[1]), new Relation(slots[1], slots[3])),
            new Relation(new Relation(u[1], v[1]), new Relation(slots[0], slots[2])));
        var layout = new Relation(new Relation(Cycle(u), Cycle(v)),
            new Relation(new Relation(slots[4], slots[5]), rules));
        Program = new(frame, motifChoices, transformChoices, parameters, layout);
        BaseBindings = Bind(Program, [0, 1, 0, 2, 0, 0]);
        BaseWindow = OrnamentProgram.Window(u[0], v[0], 6, 4);
        Archive = Program.Save(BaseBindings, BaseWindow);
        if (!OrnamentProgram.TryLoad(Archive, out var loaded, out var unknown))
            throw new InvalidOperationException($"The native ornament sample failed to reload ({N.E(unknown).Length} unresolved obligations).");
        LoadedSample = loaded;

        var variants = new List<Variant>();
        Add("P01", "One stored checker machine", "A diamond and a quarter-turned hook alternate across two native two-phase cycles. Twenty-four accepted unit executions come from one reusable definition.", Program, BaseBindings, BaseWindow, false);
        Add("P02", "A smaller observation window", "The same loaded definition, bindings and starting phases are sampled over three columns and three rows. The program itself is unchanged.", loaded.Program, loaded.Bindings, Window(loaded.Program, 0, 0, 3, 3), true);
        Add("P03", "A different phase origin", "Only the selected column starting phase changes. Alternation shifts without changing the motif vocabulary, rules, step words or template.", loaded.Program, loaded.Bindings, Window(loaded.Program, 1, 0, 6, 4), true);
        Add("P04", "Mirrored hooks and diagonal turns", "The archived slots are rebound to hooks and zigzags, using reflection and quarter-turn substitution. New choices remain inside the original advertised finite domains.", loaded.Program, Bind(loaded.Program, [1, 2, 1, 2, 0, 0]), Window(loaded.Program, 0, 0, 6, 4), true);
        Add("P05", "A denser diamond field", "Rebinding both motif slots to the diamond and selecting shorter step words makes a dense field. Coincident rendered endpoints remain distinct native unit sites; this example adds no seam joins.", loaded.Program, Bind(loaded.Program, [0, 0, 0, 1, 1, 1]), Window(loaded.Program, 0, 0, 6, 4), true);
        Add("P06", "An oblique repeating field", "A row step with both rightward and upward participation changes the disposition. Hook and zigzag programs, phase cycles and bindings still come entirely from the loaded archive.", loaded.Program, Bind(loaded.Program, [1, 2, 0, 1, 0, 2]), Window(loaded.Program, 0, 0, 6, 4), true);
        Variants = variants.AsReadOnly();
        Surface = new(Program.Surface, new Relation(loaded.Source, N.S(variants.Select(variant => (IElement)variant.Run.Surface).ToArray())));

        UnitArrangementProgram Path(string name, Relation start, Relation[] chords, bool closed)
        {
            var path = new Relation(start, N.S(chords));
            var ports = N.S(new Relation(new Placeholder(), new Relation(path, chords[0])),
                new Relation(new Placeholder(), new Relation(path, path)));
            return new(frame, name, N.S(path), ports, closed ? N.S(path) : N.S());
        }
        QualitativePatternTransform Map(IElement right, IElement left, IElement up, IElement down) => new(frame, frame,
            N.S(new Relation(QualitativeMotionFrame.Move(frame.Right, reach), W(right)),
                new Relation(QualitativeMotionFrame.Move(frame.Left, reach), W(left)),
                new Relation(QualitativeMotionFrame.Move(frame.Up, reach), W(up)),
                new Relation(QualitativeMotionFrame.Move(frame.Down, reach), W(down))));
        void Add(string id, string name, string description, OrnamentProgram program, Relation bindings, Relation window, bool archived)
        {
            var run = program.Generate(bindings, window);
            variants.Add(new(id, name, description, program, bindings, window, run, archived));
        }
    }

    /// <summary>Fixture selection by finite domain position. The resulting
    /// binding carries actual native slot/choice references, never their labels.</summary>
    public static Relation Bind(OrnamentProgram program, int[] choices)
    {
        var rows = N.E(program.Parameters).Cast<Relation>().ToArray();
        if (choices.Length != rows.Length) throw new ArgumentException("Every advertised parameter needs a choice.", nameof(choices));
        return N.S(rows.Select((row, index) => (IElement)new Relation(row.Left,
            N.E((Relation)row.Right)[choices[index]])).ToArray());
    }

    public static Relation Window(OrnamentProgram program, int columnPhase, int rowPhase, int columns, int rows)
    {
        var cycles = (Relation)program.Layout.Left;
        var u = N.E((Relation)((Relation)cycles.Left).Left);
        var v = N.E((Relation)((Relation)cycles.Right).Left);
        return OrnamentProgram.Window(u[columnPhase], v[rowPhase], columns, rows);
    }
}
