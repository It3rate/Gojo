using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// Finite two-dimensional operating definitions over a shared unmeasured basis.
/// Authored phase routes contain instructions, not output sites or finished
/// geometry. Every motif and passage is executed by the acknowledged machine.
/// These patches do not certify an infinite wallpaper symmetry group.
/// </summary>
public sealed class WallpaperMachineStudy
{
    // motif definition = actual machine -> actual reusable ordered actions
    // orientation plan = actual cell phase -> (actual action word -> its read)
    // passage = (actual current cell phase -> actual next cell phase) ->
    //           Closed(actual receiver actions in their execution order)
    // passage support = actual passage ->
    //                   (world displacement -> (old inverse read -> next action read))
    // The temporary inverse restores the world basis before navigation. It
    // does not transform existing ink or turn placement into motif-local motion.
    public WallpaperFlowStudy Basis { get; } = new();
    public WallpaperPatternMachine[] Machines { get; }
    public Case[] Cases { get; }
    public Relation MotifDefinitions { get; }
    public Relation PassageDefinitions { get; }
    public Relation Definitions { get; }
    public Relation Surface { get; }

    public sealed record Case(string Name, string Description,
        WallpaperPatternMachine Machine, Relation MotifActions,
        Relation Passages, Relation Cells, Relation ColumnPitch,
        Relation RowPitch, Relation OrientationPlan, Relation Support);

    public WallpaperMachineStudy()
    {
        var study = Basis.Basis; var frame = study.Frame;
        var actions = study.DirectionActions;
        var turn = study.DirectionTurn;
        var mirrorX = study.DirectionMirror;
        var mirrorY = study.OtherDirectionMirror;
        var cellX = study.Cell.XWord; var cellY = study.Cell.YWord;
        var superX = Join(cellX, Basis.HalfPeriod);
        var superY = Join(cellY, Word(frame.Up, 2));

        var pinwheel = Drawing(); var arms = new List<IElement>();
        // Three stroke instructions make one hooked arm. The four actual
        // reset/turn pairs form the orbit during execution, not by copying ink.
        for (var arm = 0; arm < 4; arm++)
            arms.AddRange([
                pinwheel.Draw(Word(frame.Right, 2)),
                pinwheel.Draw(Word(frame.Up)),
                pinwheel.Draw(Word(frame.Left)),
                pinwheel.Reset(), pinwheel.DirectionAction(turn)]);
        var pinwheels = Build("Four-arm turning pinwheels",
            "Four live hooked arms share an anchor. Each arm resets and turns; a completed orbit requests the next cell.",
            pinwheel, S(arms.ToArray()), superX, superY,
            (_, _) => S());

        var fan = Drawing(); var halves = new List<IElement>();
        for (var half = 0; half < 2; half++)
            halves.AddRange([
                fan.Draw(Word(frame.Right, 2)), fan.Draw(Word(frame.Up, 2)),
                fan.Draw(Word(frame.Left)), fan.Draw(Word(frame.Down)),
                fan.Reset(), fan.DirectionAction(mirrorX)]);
        var fans = Build("Mirrored stepped fans",
            "Each motif draws one stepped half and its reflected partner. Alternate rows use the native half-turn action.",
            fan, S(halves.ToArray()), superX, superY,
            (_, row) => row % 2 == 0 ? S() : S(turn, turn));

        var glide = Drawing();
        var staples = S(glide.Draw(Word(frame.Up)), glide.Draw(Word(frame.Right)),
            glide.Draw(Word(frame.Down)));
        var glides = Build("Glide staples in reflected rows",
            "An asymmetric staple advances by the exposed half-X path and flips vertically. Two horizontal passages restore orientation and span X; neighbouring rows also reflect X.",
            glide, staples, Basis.HalfPeriod, cellY,
            (column, row) => S([.. column % 2 == 0 ? Array.Empty<IElement>() : new IElement[] { mirrorY },
                .. row % 2 == 0 ? Array.Empty<IElement>() : new IElement[] { mirrorX }]));

        var key = Drawing();
        var keys = S(key.Draw(Word(frame.Right, 2)), key.Draw(Word(frame.Up, 2)),
            key.Draw(Word(frame.Left)), key.Draw(Word(frame.Down)));
        var rotatingKeys = Build("Turning keys with mirrored rows",
            "A single stepped key turns between cells. Alternate rows add a reflection; this orientation texture is not a claim of global quarter-turn symmetry.",
            key, keys, cellX, cellY,
            (column, row) => S([.. Enumerable.Repeat<IElement>(turn, (column + row) % 4),
                .. row % 2 == 0 ? Array.Empty<IElement>() : new IElement[] { mirrorX }]));

        Cases = [pinwheels, fans, glides, rotatingKeys];
        Machines = Cases.Select(example => example.Machine).ToArray();
        MotifDefinitions = S(Cases.Select(example => (IElement)new Relation(example.Machine.Surface, example.MotifActions)).ToArray());
        PassageDefinitions = S(Cases.Select(example => (IElement)new Relation(example.Machine.Surface, example.Passages)).ToArray());
        Definitions = S(Machines.Select(machine => (IElement)machine.Surface).ToArray());
        Surface = new Relation(Basis.Surface, new Relation(Definitions,
            new Relation(new Relation(MotifDefinitions, PassageDefinitions),
                S(Cases.Select(example => (IElement)example.Support).ToArray()))));

        QualitativeDrawingReceiver Drawing() => new(frame, initialSite: frame.Origin);
        Relation Word(IElement direction, int repetitions = 1) => S(Enumerable.Range(0, repetitions)
            .Select(_ => (IElement)QualitativeMotionFrame.Move(direction, study.Reach)).ToArray());

        Case Build(string name, string description, QualitativeDrawingReceiver drawing,
            Relation motif, Relation columnPitch, Relation rowPitch,
            Func<int, int, Relation> orientationWord)
        {
            const int columns = 4, rows = 3;
            var cells = Enumerable.Range(0, columns * rows).Select(_ => (IElement)new Placeholder()).ToArray();
            var cellRows = S(Enumerable.Range(0, rows)
                .Select(row => (IElement)S(cells.Skip(row * columns).Take(columns).ToArray())).ToArray());
            var orientationWords = cells.Select((_, index) => orientationWord(index % columns, index / columns)).ToArray();
            var orientationReads = orientationWords.Select(actions.Evaluate).ToArray();
            var orientations = orientationReads.Select(Result).ToArray();
            var orientationPlan = S(cells.Select((cell, index) => (IElement)new Relation(cell,
                new Relation(orientationWords[index], orientationReads[index]))).ToArray());
            var directionRows = E(motif).OfType<Relation>()
                .Where(action => drawing.TryReadDirectionAction(action, out _))
                .Select(action =>
                {
                    drawing.TryReadDirectionAction(action, out var map);
                    return new Relation(action, map);
                }).ToArray();
            var motifDirectionWord = S(directionRows.Select(row => row.Right).ToArray());
            var motifDirectionSupport = new Relation(motif,
                new Relation(S(directionRows), motifDirectionWord));
            var endOrientationReads = orientations.Select(orientation =>
                actions.Evaluate(S([orientation, .. E(motifDirectionWord)]))).ToArray();
            var passages = new List<IElement>(); var supportRows = new List<IElement>();
            for (var index = 0; index < cells.Length - 1; index++)
            {
                // A row change returns three column periods before advancing
                // one independent row period. These are native move words;
                // output positions and sites are produced only on acceptance.
                var word = index % columns < columns - 1 ? columnPitch :
                    Join(Repeat(study.Inverse(columnPitch), columns - 1), rowPitch);
                var inverse = actions.Inverse(Result(endOrientationReads[index]));
                var controls = S(drawing.Reset(), drawing.DirectionAction(Result(inverse)),
                    drawing.Jump(word), drawing.Anchor(), drawing.DirectionAction(orientations[index + 1]));
                var passage = new Relation(new Relation(cells[index], cells[index + 1]), controls);
                passages.Add(passage);
                supportRows.Add(new Relation(passage, new Relation(word,
                    new Relation(new Relation(endOrientationReads[index], inverse), orientationReads[index + 1]))));
            }
            var route = S(passages.ToArray());
            var support = new Relation(new Relation(study.CellRead, Basis.GlideSupport),
                new Relation(new Relation(columnPitch, rowPitch),
                    new Relation(cellRows, new Relation(orientationPlan,
                        new Relation(motifDirectionSupport, S(supportRows.ToArray()))))));
            var machine = new WallpaperPatternMachine(name, drawing, motif, route, cells[0], support);
            return new(name, description, machine, motif, route, cellRows,
                columnPitch, rowPitch, orientationPlan, support);
        }

        Relation Result(Relation read)
        {
            if (!actions.TryReadResult(read, out var result, out var blocked, out var pending, out _) ||
                E(blocked).Length != 0 || E(pending).Length != 0)
                throw new InvalidOperationException("An authored direction word must have a complete local action read.");
            return result;
        }
    }

    private static Relation Repeat(Relation word, int count) => S(Enumerable.Range(0, count).SelectMany(_ => E(word)).ToArray());
    private static Relation Join(params Relation[] words) => S(words.SelectMany(E).ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
