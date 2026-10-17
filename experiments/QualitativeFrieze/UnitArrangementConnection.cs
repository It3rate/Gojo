using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A local seam over two published landmarks. It never traverses a
/// unit's recipe or actuator. A mismatch exposes a held demand; an authored
/// finite signal may request a replacement, after which the seam is reread.
/// Signals are observations/requests, not duplicated material packets.</summary>
public sealed class UnitArrangementConnection
{
    public QualitativeMotionFrame Frame { get; }
    public IUnitArrangementEndpoint Left { get; }
    public IUnitArrangementEndpoint Right { get; }
    public IElement LeftRole { get; }
    public IElement RightRole { get; }
    public Relation Surface { get; }
    public Relation Timeline { get; private set; } = S();
    public Relation Negotiations { get; private set; } = S();
    public Relation AcceptedJoins { get; private set; } = S();
    public Relation Blocked { get; private set; } = S();
    public Relation LastRead { get; private set; } = S();
    public bool IsAligned { get { Read(out var accepted, out _); return E(accepted).Length == 1; } }

    public UnitArrangementConnection(QualitativeMotionFrame frame, IUnitArrangementEndpoint left, IElement leftRole,
        IUnitArrangementEndpoint right, IElement rightRole)
    {
        Frame = frame; Left = left; Right = right; LeftRole = leftRole; RightRole = rightRole;
        Surface = new(frame.Surface, new Relation(new Relation(left.Surface, leftRole), new Relation(right.Surface, rightRole)));
    }

    // read = (connection -> (left advertisement -> right advertisement)) ->
    //        (accepted declarations -> blocked evidence)
    // declaration = (actual left site -> actual right site) ->
    //               ((left publication -> right publication) -> comparison)
    public Relation Observe()
    {
        LastRead = Read(out var accepted, out var blocked);
        AcceptedJoins = accepted; Blocked = blocked; Timeline = Append(Timeline, LastRead); return LastRead;
    }

    public Relation Negotiate(IUnitArrangementEndpoint target, IElement signal)
    {
        var before = Observe();
        if ((!ReferenceEquals(target, Left) && !ReferenceEquals(target, Right)) || E(AcceptedJoins).Length != 0)
        {
            var refused = new Relation(new Relation(before, signal), StructuralConstraint.Open(target.Surface));
            Negotiations = Append(Negotiations, refused); return refused;
        }
        // The target receives only the mismatch witness plus a finite signal.
        // Its native local policy, rather than this connection, picks a recipe.
        var proposal = target.Propose(signal, before); var response = target.Commit(proposal);
        var after = Observe();
        var negotiation = new Relation(new Relation(Surface, new Relation(target.Surface, signal)),
            new Relation(before, new Relation(new Relation(proposal, response), after)));
        Negotiations = Append(Negotiations, negotiation); return negotiation;
    }

    public bool IsCurrentAgreement(Relation declaration)
    {
        if (!E(Timeline).OfType<Relation>().Any(read => read.Right is Relation { Left: Relation admitted } &&
                E(admitted).Any(actual => ReferenceEquals(actual, declaration))) ||
            declaration.Left is not Relation sites || declaration.Right is not Relation { Left: Relation publications, Right: Relation comparison } ||
            publications.Left is not Relation left || publications.Right is not Relation right ||
            !TryReadPublication(Left, LeftRole, out var actualLeft, out var leftBinding) ||
            !TryReadPublication(Right, RightRole, out var actualRight, out var rightBinding) ||
            !ReferenceEquals(actualLeft, left) || !ReferenceEquals(actualRight, right) ||
            !ReferenceEquals(sites.Left, leftBinding.Left) || !ReferenceEquals(sites.Right, rightBinding.Left) ||
            !InFrame(Left.Advertisement()) || !InFrame(Right.Advertisement()) ||
            !KnownWord((Relation)leftBinding.Right) || !KnownWord((Relation)rightBinding.Right)) return false;
        return UnitArrangementRead.IsZero(Frame, comparison) && comparison.Left is Relation { Right: Relation sources } &&
            ReferenceEquals(sources.Left, leftBinding.Right) && sources.Right is Relation inverse &&
            ActualInverse(inverse, (Relation)rightBinding.Right) &&
            // Recompute against the live boundary words; a copied favourable
            // result cannot establish an agreement for an unrelated pair.
            UnitArrangementRead.IsZero(Frame, Frame.Reduce((Relation)leftBinding.Right,
                UnitArrangementRead.Inverse(Frame, (Relation)rightBinding.Right)));
    }

    public static bool TryReadPublication(IUnitArrangementEndpoint endpoint, IElement role,
        out Relation publication, out Relation binding)
    {
        publication = binding = null!;
        var advertisement = endpoint.Advertisement();
        if (advertisement.Left is not Relation context || !ReferenceEquals(context.Left, endpoint.Surface) ||
            !ReferenceEquals(context.Right, endpoint.Head) || advertisement.Right is not Relation { Right: Relation ports } ||
            !UnitArrangementRead.ClosedEntries(ports, out var rows)) return false;
        var matches = rows.OfType<Relation>().Where(row => ReferenceEquals(row.Left, role)).ToArray();
        if (matches is not [var actual] || actual.Right is not Relation { Left: var head, Right: Relation { Left: Relation site } } ||
            !ReferenceEquals(head, endpoint.Head) || site.Right is not Relation word || !UnitArrangementRead.ClosedEntries(word, out _)) return false;
        publication = actual; binding = site; return true;
    }

    private Relation Read(out Relation accepted, out Relation blocked)
    {
        var leftAdvertisement = Left.Advertisement(); var rightAdvertisement = Right.Advertisement();
        accepted = S(); blocked = S();
        if (!TryReadPublication(Left, LeftRole, out var left, out var a) ||
            !TryReadPublication(Right, RightRole, out var right, out var b) ||
            !InFrame(leftAdvertisement) || !InFrame(rightAdvertisement) ||
            !KnownWord((Relation)a.Right) || !KnownWord((Relation)b.Right))
            blocked = S(new Relation(leftAdvertisement, rightAdvertisement));
        else
        {
            var comparison = Frame.Reduce((Relation)a.Right, UnitArrangementRead.Inverse(Frame, (Relation)b.Right));
            var declaration = new Relation(new Relation(a.Left, b.Left), new Relation(new Relation(left, right), comparison));
            if (UnitArrangementRead.IsZero(Frame, comparison)) accepted = S(declaration); else blocked = S(declaration);
        }
        return new(new Relation(Surface, new Relation(leftAdvertisement, rightAdvertisement)), new Relation(accepted, blocked));
    }

    private bool InFrame(Relation advertisement) => advertisement.Right is Relation frame && ReferenceEquals(frame.Left, Frame.Surface);
    private bool KnownWord(Relation word) => UnitArrangementRead.ClosedEntries(word, out var terms) &&
        terms.All(term => term is Relation move && Frame.Direction(move, out _, out _, out _));
    private bool ActualInverse(Relation inverse, Relation word)
    {
        if (!UnitArrangementRead.ClosedEntries(inverse, out var actual) || !UnitArrangementRead.ClosedEntries(word, out var source) || actual.Length != source.Length) return false;
        var expected = E(UnitArrangementRead.Inverse(Frame, word));
        return actual.Zip(expected).All(pair => pair.First is Relation a && pair.Second is Relation b &&
            ReferenceEquals(a.Left, b.Left) && (ReferenceEquals(a.Right, b.Right) || a.Right is Placeholder x && b.Right is Placeholder y && x.Equals(y)));
    }

    private static Relation Append(Relation scope, IElement item) => S([.. E(scope), item]);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
