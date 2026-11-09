namespace Gojo.EncodingB;

/// <summary>Energy read in tiers, compared lexicographically: a lower tier can
/// never buy its way out of a higher one.
/// <list type="bullet">
/// <item><b>Hard</b>: accounting violations and broken codes. Always 0; nonzero is a bug or contradiction.</item>
/// <item><b>Obligation</b>: violated rows of relation laws (claims, constructions).</item>
/// <item><b>Goal</b>: remaining tension, overflow and goal residuals such as gap spread.</item>
/// <item><b>Preference</b>: costs and preferences.</item>
/// </list>
/// Energy is a read for comparing states, not a probability or a confidence.</summary>
public readonly record struct Energy(long Hard, long Obligation, long Goal, long Preference) : IComparable<Energy>
{
    public int CompareTo(Energy other)
    {
        var c = Hard.CompareTo(other.Hard);
        if (c != 0) return c;
        c = Obligation.CompareTo(other.Obligation);
        if (c != 0) return c;
        c = Goal.CompareTo(other.Goal);
        return c != 0 ? c : Preference.CompareTo(other.Preference);
    }

    public static bool operator <(Energy a, Energy b) => a.CompareTo(b) < 0;
    public static bool operator >(Energy a, Energy b) => a.CompareTo(b) > 0;
    public static bool operator <=(Energy a, Energy b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Energy a, Energy b) => a.CompareTo(b) >= 0;

    public bool Quiet => Hard == 0 && Obligation == 0 && Goal == 0;

    public override string ToString() => $"hard {Hard}, obligation {Obligation}, goal {Goal}, preference {Preference}";
}
