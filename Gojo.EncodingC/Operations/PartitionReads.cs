namespace Gojo.EncodingC;

/// <summary>Small finite reads over explicitly selected count bases. The
/// count supports and their bindings carry the evidence for each operand.</summary>
internal static class PartitionReads
{
    /// <summary>Roles are the two occupied counts, their sum, and the sum of
    /// the partition sizes. Connect this read to disjoint scopes with one
    /// common unit per part. Scope agreement belongs to the binding.</summary>
    internal static RelationLaw PoolDisjoint(int leftParts, int rightParts) =>
        Sum(leftParts, rightParts, checked(leftParts + rightParts));

    /// <summary>Adds two quantities measured in one common whole. The result
    /// can exceed that whole. Roles are the two counts, the sum, and the
    /// unchanged partition size of the whole.</summary>
    internal static RelationLaw SumOnCommonWhole(int parts) => Sum(parts, parts, parts);

    /// <summary>Reads an equal subdivision of a common whole on another
    /// partition. Roles are source count, target count, and remainder. One
    /// remainder unit is 1/(sourceParts * targetParts) of the whole.</summary>
    internal static RelationLaw Repartition(int sourceParts, int targetParts) => RelationLaw.FromPredicate(
        [Count(sourceParts), Count(targetParts), sourceParts == 1 ? Shape.Bit : Shape.Binary(sourceParts)],
        values => values[2] < sourceParts &&
            (long)values[0] * targetParts == (long)values[1] * sourceParts + values[2]);

    private static RelationLaw Sum(int leftParts, int rightParts, int resultParts) => RelationLaw.FromPredicate(
        [Count(leftParts), Count(rightParts), Count(checked(leftParts + rightParts)), Count(resultParts)],
        values => values[2] == values[0] + values[1] && values[3] == resultParts);

    private static Shape Count(int parts) => Shape.Binary(checked(parts + 1));
}
