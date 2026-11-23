namespace Gojo.EncodingC;

/// <summary>Where something is against a landmark, before numbers: which side
/// of it, and whether it is at it at this read's resolution. That is an ODV
/// word on one orientation: D says the side, V whether it is detectably away.
/// The orientation is the store's constant O, shared by every word in it, so
/// a side word is the two bits V and D, read low bit first.
/// <para><b>A stretch is words, not sites.</b> "Between home and the store" is
/// two words, after home and before the store, however long the stretch is.
/// Held open, the word's possibilities are a region: every position on that
/// side. Bound to a settled word, the same relation is a test. More landmarks
/// make a finer read; a coarse read leaves them out.</para>
/// <para>Each law is built once from the ODV codec's own arithmetic and its
/// detection contract, so nothing here knows a meaning the codec does not
/// already hold.</para></summary>
public static class Sides
{
    /// <summary>A side word on the store's orientation: the code V + 2D.</summary>
    public static Shape Word { get; } = Shape.Binary(4);

    /// <summary>A side word's planes in an ODV store: V, then D.</summary>
    public static Field Field { get; } = new(Word, Odv.V, Odv.D);

    /// <summary>The side word for a side and whether it is detectably away.</summary>
    public static int Of(bool negative, bool detectable) => OdvWord.Of(false, negative, detectable).Code & (OdvWord.DirectionBit | OdvWord.ValueBit);

    public static int After => Of(negative: false, detectable: true);

    public static int Before => Of(negative: true, detectable: true);

    /// <summary>A step word that moved detectably with the orientation.</summary>
    public static int With => After;

    /// <summary>A step word that moved detectably against the orientation.</summary>
    public static int Against => Before;

    /// <summary>At the landmark at this resolution, on its positive side.</summary>
    public static int AtFromAfter => Of(negative: false, detectable: false);

    /// <summary>At the landmark at this resolution, on its negative side.</summary>
    public static int AtFromBefore => Of(negative: true, detectable: false);

    /// <summary>The sides agree: B against C is what B against A, less C against
    /// A, can be. Roles: B against A, B against C, C against A. Under the
    /// detection contract two landmarks detectably apart can both be "at" at
    /// this resolution for something between them; under the exact-zero
    /// contract they cannot.</summary>
    public static RelationLaw Agree(int threshold = Odv.Detection) =>
        RelationLaw.FromPredicate([Word, Word, Word], t => Holds(Odv.Subtract(Of(t[0]), Of(t[2]), threshold), t[1]));

    /// <summary>Nothing jumps across a mark: two moments in a row cannot both be
    /// detectably away on opposite sides of the landmark, because something
    /// had to be at the mark in between and would have been seen there. If
    /// either moment is at the mark, the side may change. This is a promise
    /// about how densely the trail is sampled, not about how far a step can
    /// go. Roles: earlier, later.</summary>
    public static RelationLaw Continuous { get; } = RelationLaw.FromPredicate([Word, Word], t =>
    {
        var (earlier, later) = (Of(t[0]), Of(t[1]));
        return !(earlier.Detectable && later.Detectable && earlier.Negative != later.Negative);
    });

    /// <summary>A step moves the side by what the step is: the later side is
    /// the earlier side plus the step. Roles: earlier side, step, later side.
    /// The step is a word on the same orientation: D says with or against it,
    /// V whether it moved detectably.</summary>
    public static RelationLaw Moves(int threshold = Odv.Detection) =>
        RelationLaw.FromPredicate([Word, Word, Word], t => Holds(Odv.Add(Of(t[0]), Of(t[1]), threshold), t[2]));

    /// <summary>"Turned back": a detectable step with the orientation, then a
    /// detectable step against it. Roles: earlier step, later step.</summary>
    public static RelationLaw TurnedBack { get; } = RelationLaw.FromPredicate([Word, Word], t => t[0] == With && t[1] == Against);

    /// <summary>A change of step direction either way, both steps detectable:
    /// what a turn is on whichever orientation the use is read. Roles: earlier
    /// step, later step.</summary>
    public static RelationLaw Reversal { get; } = RelationLaw.FromPredicate([Word, Word], t =>
        Of(t[0]).Detectable && Of(t[1]).Detectable && Of(t[0]).Negative != Of(t[1]).Negative);

    // ---- Events: presence bits a trail of side and step words settles ----

    /// <summary>At the landmark at this resolution: the side word is not
    /// detectably away. Roles: side, at.</summary>
    public static RelationLaw At { get; } = RelationLaw.FromPredicate([Word, Shape.Bit], t => t[1] == (Of(t[0]).Detectable ? 0 : 1));

    /// <summary>A step toward the landmark: a detectable step whose direction
    /// is opposite to the side it starts on, so from the negative side with the
    /// orientation, or from the positive side against it. At the mark the
    /// side's D still says which edge it is on, so a step from the near edge
    /// through the mark is toward it. Roles: side before the step, step, toward.</summary>
    public static RelationLaw Toward { get; } = RelationLaw.FromPredicate([Word, Word, Shape.Bit], t => t[2] == (Approaches(t[0], t[1]) ? 1 : 0));

    /// <summary>A step away from the landmark: a detectable step in the same
    /// direction as the side it starts on, from the negative side against the
    /// orientation, or from the positive side with it, at the mark or not.
    /// Roles: side before, step, away.</summary>
    public static RelationLaw Away { get; } = RelationLaw.FromPredicate([Word, Word, Shape.Bit], t => t[2] == (Recedes(t[0], t[1]) ? 1 : 0));

    /// <summary>Passing the landmark: the side's D differs from one moment to
    /// the next, which continuity allows only at the mark. Roles: side before,
    /// side after, crossed.</summary>
    public static RelationLaw Crosses { get; } = RelationLaw.FromPredicate([Word, Word, Shape.Bit], t => t[2] == (Of(t[0]).Negative != Of(t[1]).Negative ? 1 : 0));

    /// <summary>Gave up on the landmark: having been heading toward it and not
    /// yet reached it, the next step goes away. Once abandoned, a use stays
    /// abandoned. Roles: abandoned before, reached before, toward before, away
    /// now, abandoned after.</summary>
    public static RelationLaw Abandons { get; } = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        t => t[4] == (t[0] == 1 || (t[1] == 0 && t[2] == 1 && t[3] == 1) ? 1 : 0));

    /// <summary>Under way: heading toward the landmark, not yet reached, not
    /// abandoned. Roles: toward, reached, abandoned, under way.</summary>
    public static RelationLaw UnderWay { get; } = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        t => t[3] == (t[0] == 1 && t[1] == 0 && t[2] == 0 ? 1 : 0));

    private static bool Approaches(int side, int step)
    {
        var (s, d) = (Of(side), Of(step));
        return d.Detectable && s.Negative != d.Negative;
    }

    private static bool Recedes(int side, int step)
    {
        var (s, d) = (Of(side), Of(step));
        return d.Detectable && s.Negative == d.Negative;
    }

    /// <summary>A side word as the full ODV word on the positive-right orientation.</summary>
    private static OdvWord Of(int code) => new((byte)(code | OdvWord.OrientationBit));

    private static bool Holds(ulong mask, int code) => ((mask >> Of(code).Code) & 1) != 0;
}
