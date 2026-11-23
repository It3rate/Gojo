namespace Gojo.EncodingC;

/// <summary>Tables for a path: sites, and a declared order between
/// neighbours. Pre-numeric: no distance and no count, only whether one site
/// comes after another along the path. Each builder returns a table and
/// nothing else knows these laws by name.
/// <para><b>Definition and use.</b> A path is defined on one orientation: its
/// declared neighbours run from lower to higher. A segment of it is the set of
/// sites that can fill an open position role between two landmarks; the
/// landmarks may be written in either order, since "B is after A" and "C is
/// after B" are two facts without priority, and the lower landmark is the
/// segment's natural start. A range is the same set of sites with no start
/// and no flow. A traversal is a use: each step is an ODV step word on the
/// path's orientation (<see cref="Sides.Word"/>): <see cref="Sides.With"/> is
/// forward, <see cref="Sides.Against"/> back, and a zero word a stay that moved
/// nothing detectably, its heading left open. A turn is a change of step
/// direction (<see cref="Sides.TurnedBack"/>, <see cref="Sides.Reversal"/>).
/// Reading a use on the reoriented path is binding the same step law with its
/// two site roles swapped; the sites do not change.</para>
/// <para>Where order between sites is needed, it is worked out once from the
/// declared neighbours when the table is made, as any stock table is. The
/// running machine holds only the table.</para></summary>
public static class Paths
{
    /// <summary>A step from one site to the next moment's site, and its step
    /// word: forward along a declared neighbour, back along one, or staying,
    /// which admits either zero word. Roles: site, next site, step. On a
    /// branching path a forward step from a fork admits each branch; nothing
    /// chooses the route.</summary>
    public static RelationLaw Step(int sites, IReadOnlyList<(int From, int To)> next)
    {
        var edge = Edges(sites, next);
        return RelationLaw.FromPredicate([Site(sites), Site(sites), Sides.Word], t =>
            t[2] == Sides.With ? edge[t[0], t[1]]
            : t[2] == Sides.Against ? edge[t[1], t[0]]
            : t[0] == t[1]);
    }

    /// <summary>Whether the middle site lies between two landmarks along the
    /// path, the landmarks written in either order. Roles: landmark, site,
    /// landmark. Closed ends admit the landmarks themselves; open ends do not.
    /// Adjacent landmarks with open ends enclose nothing, coincident landmarks
    /// with closed ends enclose a point, and landmarks the path does not order
    /// enclose nothing.</summary>
    public static RelationLaw Within(int sites, IReadOnlyList<(int From, int To)> next, bool closed)
    {
        var reach = Reach(sites, next);
        bool Before(int a, int b) => closed ? reach[a, b] : reach[a, b] && a != b;
        return RelationLaw.FromPredicate([Site(sites), Site(sites), Site(sites)],
            t => (Before(t[0], t[1]) && Before(t[1], t[2])) || (Before(t[2], t[1]) && Before(t[1], t[0])));
    }

    /// <summary>A bound on one side: the site comes after the landmark along the
    /// path, or at it when the end is closed. Roles: landmark, site. A bound
    /// from above is the same law on the reoriented path. Several bounds on
    /// one site are several rows: a lower and an upper bound give a segment,
    /// two lower bounds give a ray after the greater of them, and the other
    /// bound then rules nothing out.</summary>
    public static RelationLaw After(int sites, IReadOnlyList<(int From, int To)> next, bool closed)
    {
        var reach = Reach(sites, next);
        return RelationLaw.FromPredicate([Site(sites), Site(sites)], t => reach[t[0], t[1]] && (closed || t[0] != t[1]));
    }

    /// <summary>A gap: the site lies before both landmarks or after both,
    /// written in either order. Roles: landmark, site, landmark. Closed ends
    /// admit the landmarks. It is two rays, so no stack of one-sided bounds
    /// gives it. Sites the path does not order against the landmarks are
    /// neither in the gap nor between them.</summary>
    public static RelationLaw Outside(int sites, IReadOnlyList<(int From, int To)> next, bool closed)
    {
        var reach = Reach(sites, next);
        bool Before(int a, int b) => closed ? reach[a, b] : reach[a, b] && a != b;
        return RelationLaw.FromPredicate([Site(sites), Site(sites), Site(sites)],
            t => (Before(t[1], t[0]) && Before(t[1], t[2])) || (Before(t[0], t[1]) && Before(t[2], t[1])));
    }

    /// <summary>A segment's natural start: the lower of its two landmarks,
    /// written in either order. Roles: landmark, start, landmark, the open
    /// role in the middle as in <see cref="Within"/>. Landmarks the path does
    /// not order have no start.</summary>
    public static RelationLaw Start(int sites, IReadOnlyList<(int From, int To)> next)
    {
        var reach = Reach(sites, next);
        return RelationLaw.FromPredicate([Site(sites), Site(sites), Site(sites)],
            t => (reach[t[0], t[2]] && t[1] == t[0]) || (reach[t[2], t[0]] && t[1] == t[2]));
    }

    /// <summary>Moves a unit one site along the path when the use's step word
    /// is <paramref name="step"/>, <see cref="Sides.With"/> or
    /// <see cref="Sides.Against"/>: from a site holding it to a neighbour with
    /// room. Roles: from, step, to. Each row is one neighbour pair read the way
    /// this law moves: a forward row from a site to its next, a back row from a
    /// site to its previous. A zero step word moves nothing. The step is only
    /// read, and the unit is conserved, so the law may take through a port.</summary>
    public static TransitionLaw Advance(int step)
    {
        if (step != Sides.With && step != Sides.Against) throw new ArgumentOutOfRangeException(nameof(step), "A unit advances with the path or against it.");
        return step == Sides.With ? AdvanceWith.Value : AdvanceAgainst.Value;
    }

    private static readonly Lazy<TransitionLaw> AdvanceWith = new(() => Carry(Sides.With));
    private static readonly Lazy<TransitionLaw> AdvanceAgainst = new(() => Carry(Sides.Against));

    private static TransitionLaw Carry(int direction) => TransitionLaw.FromFunction(
        [Shape.Unary(1), Sides.Word, Shape.Unary(1)],
        s => s[0] == 1 && s[1] == direction && s[2] == 0 ? [0, s[1], 1] : s,
        new Accounting(true, new PreservedSum([0, 2]), new Unchanged(1), new NeverIncreases(0), new NeverDecreases(2)));

    /// <summary>Moves a unit onto a site that is shared one at a time: as
    /// <see cref="Advance"/>, and only while the other face's lane there is
    /// clear and the active orientation is this face's. Which face may use the
    /// site is a bit, 1 for the forward face, so the choice is stated rather
    /// than left to row order. Roles: from, step, to, the other face at the
    /// target (read), the active orientation (read).</summary>
    public static TransitionLaw AdvanceInTurn(bool forwardFace) => forwardFace ? InTurnForward.Value : InTurnOpposed.Value;

    private static readonly Lazy<TransitionLaw> InTurnForward = new(() => Turned(1));
    private static readonly Lazy<TransitionLaw> InTurnOpposed = new(() => Turned(0));

    private static TransitionLaw Turned(int active) => TransitionLaw.FromFunction(
        [Shape.Unary(1), Sides.Word, Shape.Unary(1), Shape.Bit, Shape.Bit],
        s => s[0] == 1 && s[1] == Sides.With && s[2] == 0 && s[3] == 0 && s[4] == active ? [0, s[1], 1, s[3], s[4]] : s,
        new Accounting(true, new PreservedSum([0, 2]), new Unchanged(1), new Unchanged(3), new Unchanged(4), new NeverIncreases(0), new NeverDecreases(2)));

    /// <summary>A site read as a side of a landmark on the path: before it,
    /// at it, or after it, along the path's order. At the landmark either zero
    /// word is admitted; which edge is a read for continuity to settle. A site
    /// the path does not order against the landmark has no side. Roles: site,
    /// side word.</summary>
    public static RelationLaw SideOf(int sites, IReadOnlyList<(int From, int To)> next, int landmark)
    {
        var reach = Reach(sites, next);
        if ((uint)landmark >= (uint)sites) throw new ArgumentOutOfRangeException(nameof(landmark));
        return RelationLaw.FromPredicate([Site(sites), Sides.Word], t =>
            t[0] == landmark ? t[1] == Sides.AtFromBefore || t[1] == Sides.AtFromAfter
            : reach[t[0], landmark] ? t[1] == Sides.Before
            : reach[landmark, t[0]] && t[1] == Sides.After);
    }

    /// <summary>A moment of a trail, recorded from occupancy: while a clock
    /// reads the moment's stamp, the site held is the one whose lane shows the
    /// unit. At any other time the row rules nothing out. Roles: clock, stamp,
    /// one occupancy bit per site, site.</summary>
    public static RelationLaw Seen(int sites, int moments) =>
        RelationLaw.FromPredicate([Shape.Unary(moments), Shape.Unary(moments), .. Enumerable.Repeat(Shape.Bit, sites), Site(sites)],
            t => t[0] != t[1] || t[2 + t[^1]] == 1);

    /// <summary>The same path defined on the opposite orientation: the same sites
    /// and neighbours, the order read the other way.</summary>
    public static (int From, int To)[] Reorient(IReadOnlyList<(int From, int To)> next) => [.. next.Select(e => (e.To, e.From))];

    private static Shape Site(int sites) => Shape.Binary(sites);

    private static bool[,] Edges(int sites, IReadOnlyList<(int From, int To)> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        if (sites < 1) throw new ArgumentOutOfRangeException(nameof(sites));
        var edge = new bool[sites, sites];
        foreach (var (from, to) in next)
        {
            if ((uint)from >= (uint)sites || (uint)to >= (uint)sites || from == to)
                throw new ArgumentException("A neighbour joins two different sites of the path.", nameof(next));
            edge[from, to] = true;
        }
        return edge;
    }

    /// <summary>reach[a, b]: b is at a or after it along the declared neighbours.</summary>
    private static bool[,] Reach(int sites, IReadOnlyList<(int From, int To)> next)
    {
        var edge = Edges(sites, next);
        var reach = new bool[sites, sites];
        for (var s = 0; s < sites; s++) reach[s, s] = true;
        for (var changed = true; changed;)
        {
            changed = false;
            for (var a = 0; a < sites; a++)
                for (var from = 0; from < sites; from++)
                    if (reach[a, from])
                        for (var to = 0; to < sites; to++)
                            if (edge[from, to] && !reach[a, to]) changed = reach[a, to] = true;
        }
        for (var a = 0; a < sites; a++)
            for (var b = 0; b < sites; b++)
                if (a != b && reach[a, b] && reach[b, a])
                    throw new ArgumentException("The declared neighbours loop back; a path's order runs one way.", nameof(next));
        return reach;
    }
}
