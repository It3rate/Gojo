namespace Gojo.EncodingB;

/// <summary>What a commit attempt did. A refused commit keeps the proposal and
/// says why; it never writes over newer source state.</summary>
public sealed record CommitResult(bool Committed, string Read, ulong MissingSites, TransitionWitness? Witness)
{
    public static CommitResult Refused(string why, ulong missing = 0) => new(false, why, missing, null);
}

/// <summary>A workspace that may write back. Writeback is validated:
/// <list type="bullet">
/// <item>A material commit must cover the law's whole accounting scope:
/// <c>(accountingScope &amp; ~Sel) == 0</c>. Partial attention can never lose or
/// duplicate material.</item>
/// <item>The source must still be at the version (for material commits) or the
/// epoch (for structural adoption) this workspace read. Otherwise the proposal
/// fails validation and stays a proposal: optimistic concurrency.</item>
/// <item>Introduced structure enters the source only by explicit adoption,
/// labelled with the rule that introduced it, never as an observation.</item>
/// </list></summary>
public sealed class TrialWorkspace(string name, Tile source, Selection focus) : Workspace(name, source, focus)
{
    /// <summary>Applies a transition law to the source under the commit rule.
    /// The owner runs the law on the source; this workspace only proposes it.</summary>
    public CommitResult CommitTransition(LawRows rows, ulong rowMask)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var accountingScope = rows.SitesOf(rowMask);
        var missing = Focus.Missing(accountingScope);
        if (missing != 0)
            return CommitResult.Refused(
                $"selection misses sites {{{string.Join(",", Bits.Indexes(missing))}}} of the accounting scope of {rows.Name}",
                missing);
        if (Source.Version != ReadVersion)
            return CommitResult.Refused(
                $"stale: read at source version {ReadVersion}, source is now at {Source.Version}; the proposal is kept as residue");
        var witness = rows.Apply(Source, rowMask);
        Relink(accountingScope, versionWasCurrent: true);
        return new CommitResult(true, $"applied {rows.Name} to {Source.Name}", 0, witness);
    }

    /// <summary>Adopts an introduced row into the source as an explicit structural
    /// edit. Its provenance names the rule and how often it was used; it is never
    /// recorded as observed.</summary>
    public CommitResult Adopt(IntroducedRow row, int uses)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (!IntroducedRows.Contains(row)) return CommitResult.Refused("this workspace did not introduce that row");
        if (row.Sites.Any(site => site >= Source.SiteCount))
            return CommitResult.Refused("the row uses introduced sites; the source has no counterpart to attach it to");
        if (Source.Epoch != ReadEpoch)
            return CommitResult.Refused(
                $"stale: read at source epoch {ReadEpoch}, source is now at {Source.Epoch}; the proposal is kept as residue");
        var versionWasCurrent = Source.Version == ReadVersion;
        Source.Rows(row.RowSet).Add(row.Sites, $"introduced by rule '{row.Rule}' in workspace {Name}, adopted after {uses} uses");
        Forget(row);
        Relink(0, versionWasCurrent);
        return new CommitResult(true, $"adopted into {Source.Name}.{row.RowSet}", 0, null);
    }
}
