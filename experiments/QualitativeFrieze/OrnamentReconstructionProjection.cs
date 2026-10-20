using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A derived finite-library backend over actual native read receipts.
/// A bit denotes an admitted candidate, never source truth or a probability.
/// Missing capabilities and unfinished reads contribute no positive or negative
/// answer mask. Fresh output must be assessed through its own observation bank.</summary>
public sealed class OrnamentReconstructionProjection
{
    public R.Bank Bank { get; }
    public R.Context Context { get; }
    public IReadOnlyList<R.Input> Inputs => Context.Preference;
    public IReadOnlyList<R.Query> Queries { get; }
    public BigInteger Universe { get; }
    public Relation Surface { get; }
    private readonly R.Receipt[,] receipts;
    private readonly Dictionary<Relation, int> queryIndices = new(ReferenceEqualityComparer.Instance);
    private readonly BigInteger[] positive;
    private readonly BigInteger[] negative;
    private readonly BigInteger[] possiblePositive;
    private readonly BigInteger[] possibleNegative;

    public OrnamentReconstructionProjection(R.Bank bank, R.Context context, IReadOnlyList<R.Query> queries)
    {
        Bank = bank; Context = context; Queries = queries;
        receipts = new R.Receipt[Inputs.Count, queries.Count]; positive = new BigInteger[queries.Count]; negative = new BigInteger[queries.Count];
        possiblePositive = new BigInteger[queries.Count]; possibleNegative = new BigInteger[queries.Count];
        for (var index = 0; index < queries.Count; index++)
            if (!queryIndices.TryAdd(queries[index].Surface, index))
                throw new ArgumentException("A prepared query scope contains each actual native query once.", nameof(queries));
        var validContext = context.IsValid && context.Preference.All(input => bank.Inputs.Any(actual => ReferenceEquals(actual, input))) &&
            context.Available.All(query => bank.Queries.Any(actual => ReferenceEquals(actual.Surface, query.Surface)));
        Universe = validContext ? (BigInteger.One << Inputs.Count) - BigInteger.One : BigInteger.Zero;
        var columns = new List<IElement>();
        for (var queryIndex = 0; queryIndex < queries.Count; queryIndex++)
        {
            var query = queries[queryIndex]; var rows = new List<IElement>();
            var available = context.Available.Any(actual => ReferenceEquals(actual.Surface, query.Surface));
            for (var inputIndex = 0; inputIndex < Inputs.Count; inputIndex++)
            {
                var input = Inputs[inputIndex];
                var receipt = available ? bank.Read(input, query) : new R.Receipt(input, query, S(), S(),
                    S(new Relation(query.Surface, StructuralConstraint.Open(context.Surface))), S(), S(), 0);
                receipts[inputIndex, queryIndex] = receipt;
                var bit = BigInteger.One << inputIndex;
                if (validContext && receipt.Answer == "Yes") positive[queryIndex] |= bit;
                if (validContext && receipt.Answer == "No") negative[queryIndex] |= bit;
                // An unfinished read does not eliminate the candidate. Only
                // an actual opposite answer or a blocked publication can do so.
                if (validContext && receipt.Answer is "Yes" or "Open") possiblePositive[queryIndex] |= bit;
                if (validContext && receipt.Answer is "No" or "Open") possibleNegative[queryIndex] |= bit;
                rows.Add(new Relation(input.Surface, receipt.Surface));
            }
            columns.Add(new Relation(query.Surface, S(rows.ToArray())));
        }
        Surface = new(context.Surface, S(columns.ToArray()));
    }

    public R.Receipt ReadReceipt(int inputIndex, R.Query query)
    {
        if (!queryIndices.TryGetValue(query.Surface, out var index)) throw new ArgumentException("The actual native query is not prepared.", nameof(query));
        return receipts[inputIndex, index];
    }
    public string ReadAnswer(int inputIndex, R.Query query) => ReadReceipt(inputIndex, query).Answer;

    public BigInteger Mask(R.Claim claim) => claim.Query.IsValid && queryIndices.TryGetValue(claim.Query.Surface, out var index)
        ? claim.Expected ? positive[index] : negative[index] : BigInteger.Zero;

    public BigInteger PossibleMask(R.Claim claim) => QualitativeRecognitionObservation.Entries(claim.Query.Blocked).Length == 0 &&
        queryIndices.TryGetValue(claim.Query.Surface, out var index)
        ? claim.Expected ? possiblePositive[index] : possibleNegative[index] : BigInteger.Zero;

    public BigInteger Admitted(IReadOnlyList<R.Claim> claims)
        => Combine(claims, Mask);

    public BigInteger Possible(IReadOnlyList<R.Claim> claims)
        => Combine(claims, PossibleMask);

    private BigInteger Combine(IReadOnlyList<R.Claim> claims, Func<R.Claim, BigInteger> read)
    {
        // The native Message rejects repeated or contradictory actual queries.
        // Mirror that exact precondition rather than treating duplicate clauses
        // as a cheaper normalized message without a correspondence witness.
        var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var mask = Universe;
        foreach (var claim in claims)
        {
            if (!seen.Add(claim.Query.Surface)) return BigInteger.Zero;
            mask &= read(claim);
        }
        return mask;
    }

    public IReadOnlyList<int> Indices(BigInteger mask)
    {
        var selected = mask & Universe; var indices = new List<int>();
        for (var index = 0; index < Inputs.Count; index++)
            if ((selected & (BigInteger.One << index)) != BigInteger.Zero) indices.Add(index);
        return indices;
    }

    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
}
