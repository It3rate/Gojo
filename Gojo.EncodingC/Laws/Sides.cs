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

    /// <summary>Nothing jumps across a mark: from one moment to the next, the
    /// side may change only while at the landmark. Roles: earlier, later.</summary>
    public static RelationLaw Continuous { get; } = RelationLaw.FromPredicate([Word, Word], t =>
    {
        var (earlier, later) = (Of(t[0]), Of(t[1]));
        return !(earlier.Detectable || later.Detectable) || earlier.Negative == later.Negative;
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

    /// <summary>A side word as the full ODV word on the positive-right orientation.</summary>
    private static OdvWord Of(int code) => new((byte)(code | OdvWord.OrientationBit));

    private static bool Holds(ulong mask, int code) => ((mask >> Of(code).Code) & 1) != 0;
}
