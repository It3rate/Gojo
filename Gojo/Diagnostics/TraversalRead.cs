using Gojo.Core;
using Gojo.Reads;

namespace Gojo.Diagnostics;

/// <summary>
/// Convenience grouped read for the natural traversal sequence: crossed
/// boundaries, split spans, support/tension/exclusive projections, pass/block.
/// </summary>
public readonly struct TraversalRead
{
    private readonly Relation[] relations;

    /// <summary>Captures traversal context without mutating relation structure.</summary>
    public TraversalRead(Carrier reader, Relation travel, params Relation[] relations)
    {
        Reader = reader;
        Travel = travel;
        this.relations = [.. relations];
    }

    public Carrier Reader { get; }

    public Relation Travel { get; }

    /// <summary>Boundary points crossed by the travel relation.</summary>
    public CarrierLocation[] BoundaryPoints =>
        RelationReads.TraversedBoundaryPointsOn(Reader, Travel, relations);

    /// <summary>Crossed boundary points paired with travel flow.</summary>
    public Relation[] BoundaryStates =>
        RelationReads.TraversedBoundaryStatesOn(Reader, Travel, relations);

    /// <summary>Travel spans split at crossed boundaries.</summary>
    public Relation[] Spans =>
        RelationReads.TraversedSpansOn(Reader, Travel, relations);

    /// <summary>Split spans paired with their flow.</summary>
    public Relation[] SpanStates =>
        RelationReads.TraversedSpanStatesOn(Reader, Travel, relations);

    /// <summary>Split spans paired with support state.</summary>
    public Relation[] SupportStates =>
        RelationReads.TraversedSupportStatesOn(Reader, Travel, relations);

    /// <summary>Visible support projections over traversed spans.</summary>
    public Relation[] Support =>
        RelationReads.TraversedSupportOn(Reader, Travel, relations);

    /// <summary>Visible tension projections over traversed spans.</summary>
    public Relation[] Tension =>
        RelationReads.TraversedTensionOn(Reader, Travel, relations);

    /// <summary>Visible exclusive projections over traversed spans.</summary>
    public Relation[] Exclusive =>
        RelationReads.TraversedExclusiveOn(Reader, Travel, relations);

    /// <summary>Traversed spans that pass through support.</summary>
    public Relation[] Pass =>
        RelationReads.TraversedPassOn(Reader, Travel, relations);

    /// <summary>Traversed spans blocked by missing support.</summary>
    public Relation[] Block =>
        RelationReads.TraversedBlockOn(Reader, Travel, relations);
}
