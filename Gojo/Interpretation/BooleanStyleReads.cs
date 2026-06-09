using Gojo.Core;
using Gojo.ConceptSurface;
using Gojo.Reads;

namespace Gojo.Interpretation;

/// <summary>
/// Boolean-style interpretations as lower projections over occupied
/// participation. The array-returning helpers remain aliases over relation
/// reads; the settlement helper exposes the richer participation witness.
/// </summary>
public static class BooleanStyleReads
{
    /// <summary>Reads overlapping support as AND-like tension.</summary>
    public static Relation[] AndOn(Carrier reader, params Relation[] relations) =>
        RelationReads.TensionOn(reader, relations);

    /// <summary>Reads any support as OR-like support.</summary>
    public static Relation[] OrOn(Carrier reader, params Relation[] relations) =>
        RelationReads.SupportOn(reader, relations);

    /// <summary>Reads non-overlapped support as XOR-like exclusivity.</summary>
    public static Relation[] XorOn(Carrier reader, params Relation[] relations) =>
        RelationReads.ExclusiveOn(reader, relations);

    /// <summary>Reads scope spans not covered by blocked relations.</summary>
    public static Relation[] NotOn(Carrier reader, Relation scope, params Relation[] blocked) =>
        RelationReads.UncoveredSpansOn(reader, scope, blocked);

    /// <summary>Reads scope spans covered by support relations.</summary>
    public static Relation[] PassOn(Carrier reader, Relation scope, params Relation[] support) =>
        RelationReads.CoveredSpansWithinOn(reader, scope, support);

    /// <summary>Reads scope spans not covered by support relations.</summary>
    public static Relation[] BlockOn(Carrier reader, Relation scope, params Relation[] support) =>
        RelationReads.UncoveredSpansOn(reader, scope, support);

    /// <summary>
    /// Builds the relation-shaped participation settlement behind the scoped
    /// segment boolean-style reads.
    /// </summary>
    public static Relation SettlementOn(Carrier reader, Relation scope, params Relation[] support) =>
        ParticipationSettlementSurface.CreateSegmentSupport(new Carrier(), reader, scope, support);
}
