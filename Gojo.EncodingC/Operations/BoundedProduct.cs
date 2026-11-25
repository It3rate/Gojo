namespace Gojo.EncodingC;

/// <summary>Authors the exact count relation over three declared finite
/// domains. Binding identity and direction of inquiry come from the wiring.
/// An outcome outside the result domain has no admitted tuple.
/// </summary>
internal static class BoundedProduct
{
    internal static RelationLaw CreateRelation(Shape left, Shape right, Shape result) =>
        RelationLaw.FromPredicate([left, right, result],
            values => (long)values[0] * values[1] == values[2]);
}
