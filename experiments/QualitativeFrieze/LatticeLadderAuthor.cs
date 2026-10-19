using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Small authoring compiler for native lattice words. Integers select
/// repeated participation in one shared reach. Execution receives the actual
/// words, path selectors, bindings and seam declarations, never this helper.</summary>
public sealed class LatticeLadderAuthor
{
    public QualitativeMotionFrame Frame { get; }
    public IElement Reach { get; }

    public LatticeLadderAuthor() : this(new QualitativeMotionFrame(), new Placeholder()) { }
    public LatticeLadderAuthor(QualitativeMotionFrame frame, IElement reach)
    { Frame = frame; Reach = reach; }

    public Relation Word(int horizontal, int vertical) => S(
        Enumerable.Repeat(horizontal < 0 ? Frame.Left : Frame.Right, Math.Abs(horizontal))
            .Concat(Enumerable.Repeat(vertical < 0 ? Frame.Down : Frame.Up, Math.Abs(vertical)))
            .Select(direction => (IElement)QualitativeMotionFrame.Move(direction, Reach)).ToArray());

    public static Relation JoinWords(params Relation[] words) => S(words.SelectMany(E).ToArray());
    public Relation Inverse(Relation word) => UnitArrangementRead.Inverse(Frame, word);

    public Relation Advance(Relation start, Relation step, out Relation witness)
    {
        witness = Frame.Reduce(start, step);
        if (!Frame.TryReadResult(witness, out var output, out _, out var pending) || E(pending).Length != 0)
            throw new ArgumentException("The selected native path prefix must have a complete formal read.");
        return output;
    }

    public QualitativePatternTransform Map(Relation horizontal, Relation vertical)
    {
        Relation Row(IElement direction, Relation word) => new(QualitativeMotionFrame.Move(direction, Reach), word);
        return new(Frame, Frame, S(Row(Frame.Right, horizontal), Row(Frame.Left, Inverse(horizontal)),
            Row(Frame.Up, vertical), Row(Frame.Down, Inverse(vertical))));
    }

    public Relation MapWord(QualitativePatternTransform transform, Relation word, out Relation witness)
    {
        witness = transform.Apply(word);
        if (!transform.TryReadComplete(witness, out var result))
            throw new ArgumentException("A lattice word requires a complete native substitution.");
        return result;
    }

    public static Relation Path(Relation start, params Relation[] words) => new(start, S(words));

    /// <summary>Ports select actual chord occurrences. A fresh role is an
    /// identity, never a coordinate-equivalence assertion.</summary>
    public static Relation Ports(params Relation[] paths) => S(paths.SelectMany(path =>
        E((Relation)path.Right).Select(chord => (IElement)new Relation(new Placeholder(), new Relation(path, chord)))
            .Append(new Relation(new Placeholder(), new Relation(path, path)))).ToArray());

    public UnitArrangementProgram Unit(string name, params Relation[] paths) =>
        new(Frame, name, S(paths), Ports(paths));

    /// <summary>Copies preserve actual chord participation and retain the
    /// prefix read that justifies each translated start.</summary>
    public Relation Translate(Relation path, Relation displacement, out Relation witness)
    {
        var start = Advance((Relation)path.Left, displacement, out var prefix);
        var copy = new Relation(start, path.Right);
        witness = new(new Relation(path, copy), new Relation(displacement, prefix));
        return copy;
    }

    public Relation ReversePath(Relation path, out Relation witness)
    {
        var words = E((Relation)path.Right).Cast<Relation>().ToArray();
        var start = Advance((Relation)path.Left, JoinWords(words), out var prefix);
        var reversed = Path(start, words.Reverse().Select(Inverse).ToArray());
        witness = new(new Relation(path, reversed), prefix);
        return reversed;
    }

    public BourgoinPatternLibrary.Recipe Compile(string id, string name, string description,
        string atlasReferences, UnitArrangementProgram unit, Relation column, Relation row,
        int columns = 3, int rows = 3)
    {
        if (!ReferenceEquals(unit.Frame, Frame) || !unit.Complete())
            throw new ArgumentException("A lattice recipe compiles its actual complete native unit.", nameof(unit));
        var identity = Map(Word(1, 0), Word(0, 1));
        var motifs = S(unit.Surface); var transforms = S(identity.Surface);
        IElement motif = new Placeholder(), mapping = new Placeholder(), uStep = new Placeholder(), vStep = new Placeholder();
        IElement uPhase = new Placeholder(), vPhase = new Placeholder();
        var uCycle = new Relation(S(uPhase), S(new Relation(uPhase, uPhase)));
        var vCycle = new Relation(S(vPhase), S(new Relation(vPhase, vPhase)));
        var parameters = S(new Relation(motif, motifs), new Relation(mapping, transforms),
            new Relation(uStep, S(column)), new Relation(vStep, S(row)));
        var bindings = S(new Relation(motif, unit.Surface), new Relation(mapping, identity.Surface),
            new Relation(uStep, column), new Relation(vStep, row));
        var rules = S(new Relation(new Relation(uPhase, vPhase), new Relation(motif, mapping)));
        var layout = new Relation(new Relation(uCycle, vCycle), new Relation(new Relation(uStep, vStep), rules));
        var program = new OrnamentProgram(Frame, motifs, transforms, parameters, layout);
        var contract = Seams(program, unit, identity, column, row);
        return new(id, name, 0, 0, atlasReferences, description,
            "Construction-ladder study inspired by the named Bourgoin family; no full atlas graph equivalence is claimed.",
            program, bindings, OrnamentProgram.Window(uPhase, vPhase, columns, rows), column, row,
            contract, new BourgoinPatternLibrary.DisplayBasis(1, 0, 0, 1));
    }

    /// <summary>Recompiles a crystallized unit's finite field in its independent
    /// frame. Old period words select direction/repetition only; no source
    /// identities or ancestry enter the returned program or seam contract.</summary>
    public static BourgoinPatternLibrary.Recipe Rebuild(BourgoinPatternLibrary.Recipe source,
        UnitArrangementProgram standaloneUnit, int columns = 3, int rows = 3)
    {
        var reaches = E(standaloneUnit.Paths).Cast<Relation>().SelectMany(path =>
                E((Relation)path.Left).Concat(E((Relation)path.Right).Cast<Relation>().SelectMany(E)))
            .Cast<Relation>().Select(move => move.Right).Distinct(N.Participants.Instance).ToArray();
        if (reaches is not [var reach])
            throw new ArgumentException("This authoring adapter requires one declared native reach.", nameof(standaloneUnit));
        var author = new LatticeLadderAuthor(standaloneUnit.Frame, reach);
        Relation Rebind(Relation word) => S(E(word).Cast<Relation>().Select(move =>
        {
            if (!source.Frame.Direction(move, out var axis, out var forward, out _))
                throw new ArgumentException("A source period needs complete native directions.", nameof(source));
            var horizontal = ReferenceEquals(axis, source.Frame.Horizontal);
            var direction = horizontal ? forward ? standaloneUnit.Frame.Right : standaloneUnit.Frame.Left
                : forward ? standaloneUnit.Frame.Up : standaloneUnit.Frame.Down;
            return (IElement)QualitativeMotionFrame.Move(direction, reach);
        }).ToArray());
        return author.Compile(source.Id, source.Name, source.Construction, source.Reference,
            standaloneUnit, Rebind(source.ColumnPitch), Rebind(source.RowPitch), columns, rows) with
            { DisplayBasis = source.DisplayBasis };
    }

    /// <summary>False public-role agreement for the blocked-seam example.
    /// Both selectors are real; their displacement deliberately disagrees.</summary>
    public static Relation BlockedContract(BourgoinPatternLibrary.Recipe recipe)
    {
        var motif = (Relation)E(recipe.Program.Motifs).Single();
        var transform = (Relation)E(recipe.Program.Transforms).Single();
        var ports = (Relation)((Relation)((Relation)motif.Right).Left).Right;
        var role = ((Relation)E(ports)[0]).Left;
        return OrnamentProgramField.Contract(recipe.Program, OrnamentProgramField.Rule(motif, transform, motif, transform,
            OrnamentProgramField.Offset(recipe.Program, 1, 0), role, role));
    }

    private Relation Seams(OrnamentProgram program, UnitArrangementProgram unit,
        QualitativePatternTransform identity, Relation column, Relation row)
    {
        var endpoints = E(unit.Ports).Cast<Relation>().Select(port =>
        {
            var selector = (Relation)port.Right; var path = (Relation)selector.Left;
            var words = E((Relation)path.Right).Cast<Relation>().ToArray();
            var prefix = ReferenceEquals(selector.Right, path) ? words.Length
                : Array.FindIndex(words, word => ReferenceEquals(word, selector.Right));
            var position = Advance((Relation)path.Left, JoinWords(words.Take(prefix).ToArray()), out _);
            return (Role: port.Left, Position: position, Index: Index(position));
        }).ToArray();
        var rules = new List<Relation>();
        foreach (var offset in new[] { (C: 0, R: 0), (C: 1, R: 0), (C: 0, R: 1), (C: 1, R: 1), (C: -1, R: 1) })
        {
            var shift = JoinWords(Enumerable.Repeat(offset.C < 0 ? Inverse(column) : column, Math.Abs(offset.C))
                .Concat(Enumerable.Repeat(row, offset.R)).ToArray());
            var at = Index(shift);
            for (var a = 0; a < endpoints.Length; a++) for (var b = 0; b < endpoints.Length; b++)
            {
                if (offset == (0, 0) && a >= b) continue;
                var from = endpoints[a]; var to = endpoints[b];
                // This transient index only chooses a native equality
                // question. The field repeats that question from public ports.
                if (from.Index != (to.Index.X + at.X, to.Index.Y + at.Y)) continue;
                var translated = Advance(to.Position, shift, out _);
                if (!UnitArrangementRead.IsZero(Frame, Frame.Reduce(translated, Inverse(from.Position)))) continue;
                rules.Add(OrnamentProgramField.Rule(unit.Surface, identity.Surface, unit.Surface, identity.Surface,
                    OrnamentProgramField.Offset(program, offset.C, offset.R), from.Role, to.Role));
            }
        }
        return OrnamentProgramField.Contract(program, rules.ToArray());
    }

    private (int X, int Y) Index(Relation word)
    {
        var x = 0; var y = 0;
        foreach (var term in E(word).Cast<Relation>())
        {
            if (!Frame.Direction(term, out var axis, out var forward, out var reach) || !N.Same(reach, Reach))
                throw new ArgumentException("The authoring index reads only this declared shared reach.");
            if (ReferenceEquals(axis, Frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
