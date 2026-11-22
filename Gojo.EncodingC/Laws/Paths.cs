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
/// and no flow. A traversal is a use: each step goes forward, stays, or goes
/// back relative to the path's orientation, and a turn is a change of step
/// direction. Reading a use on the reoriented path is binding the same step
/// law with its two site roles swapped; the sites do not change.</para>
/// <para>Where order between sites is needed, it is worked out once from the
/// declared neighbours when the table is made, as any stock table is. The
/// running machine holds only the table.</para></summary>
public static class Paths
{
    /// <summary>Step directions, low to high: more forward reads as more.</summary>
    public const int Back = 0, Stay = 1, Forward = 2;

    /// <summary>A step's direction relative to the path's orientation.</summary>
    public static Shape Direction { get; } = Shape.Binary(3);

    /// <summary>A step from one site to the next moment's site, and its direction:
    /// forward along a declared neighbour, back along one, or staying. Roles:
    /// site, next site, direction. On a branching path a forward step from a
    /// fork admits each branch; nothing chooses the route.</summary>
    public static RelationLaw Step(int sites, IReadOnlyList<(int From, int To)> next)
    {
        var edge = Edges(sites, next);
        return RelationLaw.FromPredicate([Site(sites), Site(sites), Direction], t => t[2] switch
        {
            Forward => edge[t[0], t[1]],
            Back => edge[t[1], t[0]],
            _ => t[0] == t[1],
        });
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

    /// <summary>"Turned back": a forward step followed by a backward one.
    /// Roles: earlier direction, later direction.</summary>
    public static RelationLaw TurnedBack { get; } =
        RelationLaw.FromPredicate([Direction, Direction], t => t[0] == Forward && t[1] == Back);

    /// <summary>A change of step direction either way: what a turn is, whichever
    /// orientation the use is read on. Roles: earlier direction, later direction.</summary>
    public static RelationLaw Reversal { get; } =
        RelationLaw.FromPredicate([Direction, Direction], t => (t[0], t[1]) is (Forward, Back) or (Back, Forward));

    /// <summary>Moves a unit one site along the path when the use's direction
    /// is <paramref name="direction"/>: from a site holding it to a neighbour
    /// with room. Roles: from, direction, to. Each row is one neighbour pair
    /// read the way this law moves: a forward row from a site to its next, a
    /// back row from a site to its previous. The direction is only read, and
    /// the unit is conserved, so the law may take through a port.</summary>
    public static TransitionLaw Advance(int direction)
    {
        if (direction is not (Forward or Back)) throw new ArgumentOutOfRangeException(nameof(direction), "A unit advances forward or back.");
        return direction == Forward ? AdvanceForward : AdvanceBack;
    }

    private static readonly TransitionLaw AdvanceForward = Carry(Forward);
    private static readonly TransitionLaw AdvanceBack = Carry(Back);

    private static TransitionLaw Carry(int direction) => TransitionLaw.FromFunction(
        [Shape.Unary(1), Direction, Shape.Unary(1)],
        s => s[0] == 1 && s[1] == direction && s[2] == 0 ? [0, s[1], 1] : s,
        new Accounting(true, new PreservedSum([0, 2]), new Unchanged(1), new NeverIncreases(0), new NeverDecreases(2)));

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
