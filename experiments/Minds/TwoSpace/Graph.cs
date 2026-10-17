using Gojo.Core;

namespace Minds.TwoSpace;

// Narrow views over the authored fixture and the existing reader contracts.
// These helpers neither choose a response nor implement its arithmetic.
internal static class Graph
{
    internal static Relation R(IElement element) => element as Relation
        ?? throw new InvalidDataException("Expected an authored relation surface.");

    internal static IElement[] F(IElement element) => R(element).TryReadOrderedGroup(out var fields)
        ? fields : throw new InvalidDataException("Expected an authored ordered group.");

    internal static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);

    internal static void Require(bool condition, string operation)
    {
        if (!condition) throw new InvalidDataException($"Cannot read the selected fixture: {operation}.");
    }
}
