namespace Gojo.EncodingB;

/// <summary>Generalized arc consistency over relation laws, bit-sliced across
/// rows: project each row's admitted tuples onto its sites, intersect, and repeat
/// until nothing changes. Narrowing only removes values, so any fair order
/// reaches the same fixed point (C1). A site left with no value becomes a
/// residual on the law that emptied it.</summary>
public static class Propagation
{
    public static PropagationResult Narrow(IReadOnlyList<LawRows> laws, Func<string, SupportPlanes> supportFor, int maxRounds = 64)
    {
        ArgumentNullException.ThrowIfNull(laws);
        ArgumentNullException.ThrowIfNull(supportFor);
        var contradictions = new List<Contradiction>();
        var rounds = 0;
        var changed = true;
        while (changed && rounds < maxRounds)
        {
            changed = false;
            rounds++;
            foreach (var rows in laws)
            {
                if (rows.Law is not RelationLaw relation)
                    throw new ArgumentException($"{rows.Name} is not a relation law; only relations narrow support.", nameof(laws));
                var lanes = rows.GatherSupports(supportFor, rows.AllRows);
                var next = relation.Narrow(lanes, rows.AllRows, rows.SameReference());
                for (var r = 0; r < relation.Roles.Count; r++)
                {
                    var support = supportFor(rows.RoleFields[r]);
                    var emptyBefore = support.Empty;
                    for (var v = 0; v < relation.Roles[r].Domain.Size; v++)
                    {
                        var removed = lanes[r][v] & ~next[r][v];
                        foreach (var row in Bits.Indexes(removed))
                            changed |= support.Restrict(rows.Rows[row][r], ~Bits.Of(v));
                    }
                    foreach (var site in Bits.Indexes(support.Empty & ~emptyBefore))
                    {
                        var touching = Enumerable.Range(0, rows.Count).Where(row => rows.Rows[row].Contains(site))
                            .Aggregate(0UL, (mask, row) => mask | Bits.Of(row));
                        contradictions.Add(new Contradiction(rows.Name, relation.ContentHash, support.Field, site, touching));
                    }
                }
            }
        }
        return new PropagationResult(rounds, !changed, contradictions);
    }
}

/// <summary>A site whose support emptied, and the rows of the law that emptied it.
/// A contradiction is not a measured zero.</summary>
public sealed record Contradiction(string Law, string LawHash, string Field, int Site, ulong Rows);

public sealed record PropagationResult(int Rounds, bool FixedPoint, IReadOnlyList<Contradiction> Contradictions);
