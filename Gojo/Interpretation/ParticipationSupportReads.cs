using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Reads;

namespace Gojo.Interpretation;

/// <summary>
/// Low-resolution support projections over occupied participation. The
/// array-returning helpers remain aliases over relation reads; the settlement
/// helper exposes the richer participation witness.
/// </summary>
public static class ParticipationSupportReads
{
    /// <summary>Reads any occupied support.</summary>
    public static Relation[] SupportOn(Carrier reader, params Relation[] relations) =>
        RelationReads.SupportOn(reader, relations);

    /// <summary>Reads overlapping support as unsettled tension.</summary>
    public static Relation[] TensionOn(Carrier reader, params Relation[] relations) =>
        RelationReads.TensionOn(reader, relations);

    /// <summary>Reads non-overlapped support as exclusive participation.</summary>
    public static Relation[] ExclusiveOn(Carrier reader, params Relation[] relations) =>
        RelationReads.ExclusiveOn(reader, relations);

    /// <summary>Reads scope spans not covered by blocked relations.</summary>
    public static Relation[] UncoveredOn(Carrier reader, Relation scope, params Relation[] blocked) =>
        RelationReads.UncoveredSpansOn(reader, scope, blocked);

    /// <summary>Reads scope spans covered by support relations.</summary>
    public static Relation[] CoveredWithinOn(Carrier reader, Relation scope, params Relation[] support) =>
        RelationReads.CoveredSpansWithinOn(reader, scope, support);

    /// <summary>Reads scope spans not covered by support relations.</summary>
    public static Relation[] UncoveredWithinOn(Carrier reader, Relation scope, params Relation[] support) =>
        RelationReads.UncoveredSpansOn(reader, scope, support);

    /// <summary>
    /// Builds the relation-shaped participation settlement behind the scoped
    /// segment support read.
    /// </summary>
    public static Relation SettlementOn(Carrier reader, Relation scope, params Relation[] support) =>
        ParticipationSettlementSurface.CreateSegmentSupport(new Carrier(), reader, scope, support);
}
