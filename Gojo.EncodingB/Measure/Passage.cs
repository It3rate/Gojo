namespace Gojo.EncodingB;

/// <summary>The smallest material law: source S, control C (a gate) and target
/// T, each one presence bit. State code is S + 2C + 4T. The law admits holding,
/// or a whole forward handoff into an empty target; its table is the eight
/// bytes 01 02 04 00 10 20 48 80.</summary>
public static class Passage
{
    public static Domain Presence { get; } = Domain.Binary("presence", "empty", "occupied");
    public static Domain Gate { get; } = Domain.Binary("gate", "closed", "open");

    public static IReadOnlyList<LawRole> Roles { get; } =
        [new LawRole("S", Presence), new LawRole("C", Gate), new LawRole("T", Presence)];

    /// <summary>Total occupancy of S and T preserved; C never changes; S only
    /// holds or gives; T only holds or takes. Checked over all 64 pairs at load.</summary>
    public static AccountingSignature Signature { get; } = new(
        functional: true,
        new PreservedSum(["S", "T"]),
        new Unchanged("C"),
        new NeverIncreases("S"),
        new NeverDecreases("T"));

    /// <summary>Hands off when S is occupied, the gate reads open and T is empty.</summary>
    public static TransitionLaw Law { get; } = TransitionLaw.FromFunction(
        "passage",
        Roles,
        state => state is [1, 1, 0] ? [0, 1, 1] : state,
        Signature,
        ConfluenceClass.ScheduledDescent);

    /// <summary>One plane of material presence and one observed gate plane.</summary>
    public static SiteGrammar Grammar { get; } = SiteGrammar.Create("passage")
        .Plane("occupied", "value", "empty", "occupied", material: true)
        .Plane("open", "control", "gate reads closed", "gate reads open")
        .Build();

    /// <summary>Binds passage rows (source, gate, target) over a passage tile.</summary>
    public static LawRows Rows(string name, IReadOnlyList<int[]> rows) =>
        new(name, Law, Grammar, ["occupied", "open", "occupied"], rows);
}
