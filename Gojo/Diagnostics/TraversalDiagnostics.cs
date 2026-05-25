using Gojo.Core;

namespace Gojo.Diagnostics;

/// <summary>
/// Factory for grouped traversal diagnostics. Diagnostics are read-only
/// projections over relation structure, kept separate from core settlement.
/// </summary>
public static class TraversalDiagnostics
{
    /// <summary>Creates a grouped traversal read over a travel relation and supports.</summary>
    public static TraversalRead Traverse(Carrier reader, Relation travel, params Relation[] relations) =>
        new(reader, travel, relations);
}
