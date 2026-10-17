using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An order-six action on a declared two-generator additive lattice:
/// R(u)=v and R(v)=v-u. Its third direction is a word, not another primitive
/// axis. These formal laws do not certify equal Euclidean lengths or angles.
/// Native substitution and cancellation receipts remain available to a compiler
/// that selects ordinary acknowledged drawing instructions.</summary>
public sealed class QualitativeSixfoldWords
{
    // derivation = (actual transform.Surface -> actual input word) ->
    //              (actual substitution read -> actual formal reduction read)
    // orbit = Closed(u, R(u), ..., R^5(u))
    // inverse check = (actual input -> first derivation) ->
    //                 (second derivation -> cancellation against actual input)
    public QualitativeMotionFrame Frame { get; }
    public IElement Reach { get; }
    public Relation U { get; }
    public Relation V { get; }
    public QualitativePatternTransform Turn { get; }
    public QualitativePatternTransform InverseTurn { get; }
    public Relation Orbit { get; }
    public Relation Derivations { get; }
    public Relation HalfTurnRead { get; }
    public Relation FullTurnRead { get; }
    public Relation InverseReads { get; }
    public Relation Surface { get; }
    private readonly Relation zero = S();

    public QualitativeSixfoldWords(QualitativeMotionFrame frame, IElement reach)
    {
        Frame = frame; Reach = reach;
        var u = QualitativeMotionFrame.Move(frame.Right, reach);
        var v = QualitativeMotionFrame.Move(frame.Up, reach);
        var negativeU = QualitativeMotionFrame.Move(frame.Left, reach);
        var negativeV = QualitativeMotionFrame.Move(frame.Down, reach);
        U = S(u); V = S(v);
        Turn = new(frame, frame, S(new Relation(u, V), new Relation(v, S(v, negativeU)),
            new Relation(negativeU, S(negativeV)), new Relation(negativeV, S(u, negativeV))));
        InverseTurn = new(frame, frame, S(new Relation(u, S(u, negativeV)), new Relation(v, U),
            new Relation(negativeU, S(v, negativeU)), new Relation(negativeV, S(negativeU))));
        var orbit = new List<IElement>(); var derivations = new List<IElement>(); var current = U;
        Relation third = null!; Relation halfway = null!;
        for (var step = 0; step < 6; step++)
        {
            orbit.Add(current); var derivation = Apply(current); derivations.Add(derivation);
            if (!TryReadComplete(derivation, out current))
                throw new InvalidOperationException("The declared complete lattice action lost a local substitution law.");
            if (step == 1) third = current;
            if (step == 2) halfway = current;
        }
        Orbit = S(orbit.ToArray()); Derivations = S(derivations.ToArray());
        HalfTurnRead = Frame.Reduce(halfway, U);
        FullTurnRead = Frame.Reduce(current, S(negativeU));
        var inverses = new List<IElement>();
        foreach (var seed in new[] { U, V, third })
        {
            Check(seed, Turn, InverseTurn);
            Check(seed, InverseTurn, Turn);
        }
        InverseReads = S(inverses.ToArray());
        var certificates = new Relation(Derivations, new Relation(HalfTurnRead, new Relation(FullTurnRead, InverseReads)));
        Surface = new Relation(new Relation(Frame.Surface, Reach),
            new Relation(new Relation(Turn.Surface, InverseTurn.Surface), new Relation(new Relation(U, V), new Relation(Orbit, certificates))));

        void Check(Relation seed, QualitativePatternTransform first, QualitativePatternTransform second)
        {
            var outward = Derive(first, seed); TryReadComplete(outward, out var image);
            var back = Derive(second, image); TryReadComplete(back, out var result);
            var equality = Frame.Reduce(result, Inverse(seed));
            inverses.Add(new Relation(new Relation(seed, outward), new Relation(back, equality)));
        }
    }

    public Relation Apply(Relation word) => Derive(Turn, word);
    public Relation ApplyInverse(Relation word) => Derive(InverseTurn, word);

    /// <summary>Compiles the selected action to a reduced drawing word. Retain
    /// the returned derivation when recording how an instruction was compiled.
    /// Missing support yields an open word carrying that unresolved derivation.</summary>
    public Relation TurnWord(Relation word, out Relation derivation)
    {
        derivation = Apply(word);
        return TryReadComplete(derivation, out var result) ? result : StructuralConstraint.Open(derivation);
    }
    public Relation TurnWord(Relation word) => TurnWord(word, out _);

    public bool TryReadComplete(Relation derivation, out Relation result)
    {
        result = null!;
        if (derivation.Left is not Relation source || source.Right is not Relation input ||
            derivation.Right is not Relation { Left: Relation substitution, Right: Relation reduction }) return false;
        var transform = ReferenceEquals(source.Left, Turn.Surface) ? Turn :
            ReferenceEquals(source.Left, InverseTurn.Surface) ? InverseTurn : null;
        if (transform is null || substitution.Left is not Relation requested || !ReferenceEquals(requested.Right, input) ||
            !transform.TryReadComplete(substitution, out var mapped) ||
            reduction.Left is not Relation { Right: Relation operands } ||
            !ReferenceEquals(operands.Left, zero) || !ReferenceEquals(operands.Right, mapped) ||
            !Frame.TryReadResult(reduction, out var reduced, out _, out var pending) || !Closed(reduced) || E(pending).Length != 0) return false;
        result = reduced; return true;
    }

    private Relation Derive(QualitativePatternTransform transform, Relation word)
    {
        var read = transform.Apply(word);
        var hasRead = transform.TryReadResult(read, out var mapped, out _, out _, out _);
        var reduction = Frame.Reduce(zero, hasRead ? mapped : StructuralConstraint.Open(read));
        return new Relation(new Relation(transform.Surface, word), new Relation(read, reduction));
    }

    private Relation Inverse(Relation word) => S(E(word).Cast<Relation>().Reverse().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(Frame.Opposites).Cast<Relation>()
            .Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
