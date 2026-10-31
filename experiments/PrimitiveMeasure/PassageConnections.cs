namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One owner-local transition table: a shared law and packed ordered
/// [source, control, target] port positions. No amount is stored on a passage.
/// Row coordinates are local, as in EndpointRows; there is no global row ID.
/// Calls select individual steps explicitly, with no automatic scheduling.</summary>
public sealed class PassageConnections
{
    private readonly PortStructure owner;
    private readonly PassageLaw law;
    private readonly EndpointRows rows = new();

    public PassageConnections(PortStructure owner, PassageLaw law)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(law);
        this.owner = owner;
        this.law = law;
    }

    public ConnectionRow Connect(int referenceBits, int sourcePort, int controlPort, int targetPort)
    {
        int[] positions = [sourcePort, controlPort, targetPort];
        owner.RequirePassagePorts(positions);
        return rows.Append(referenceBits, positions);
    }

    /// <summary>Result bit n admits complete state n=S+2*C+4*T. Unlike an ODV
    /// support, its three state bits refer to three sites in one fixed context.</summary>
    public byte Observe(ConnectionRow row, int visibleRoles = 7) =>
        (byte)owner.ObservePassageSites(Wiring(row), visibleRoles);

    public byte Predict(ConnectionRow row, byte beforeSupport) =>
        Observe(row, 0) == 0 ? (byte)0 : law.Predict(beforeSupport);

    public bool TryStep(ConnectionRow row, byte observedBefore, out byte afterState) =>
        owner.TryStepPassage(Wiring(row), law, observedBefore, out afterState);

    public int[] Wiring(ConnectionRow row)
    {
        if (row.Arity != 3) throw new ArgumentException("Expected [source, control, target].", nameof(row));
        return rows.Read(row);
    }

    public byte[] LawBytes() => law.PackedBytes();
    public byte[] PackedRows(int referenceBits) => rows.PackedBytes(3, referenceBits);

    /// <summary>A small explicitly selected scope of three to five distinct
    /// ports. Bit n admits the complete scope state n, so shared controls and
    /// intermediate sites retain their correspondence across predicted steps.</summary>
    public uint ObserveScope(int[] scopePorts, int visibleSites)
    {
        ArgumentNullException.ThrowIfNull(scopePorts);
        return owner.ObservePassageSites(scopePorts, visibleSites);
    }

    public uint PredictScope(ConnectionRow row, int[] scopePorts, uint beforeSupport)
    {
        var roles = ScopeRoles(row, scopePorts);
        return roles is null ? 0 : law.PredictScope(beforeSupport, roles, scopePorts.Length);
    }

    /// <summary>Project a joint scope observation onto this row. Projection can
    /// forget correlation with other sites; chain predictions keep the scope.</summary>
    public byte ProjectScope(ConnectionRow row, int[] scopePorts, uint support)
    {
        var roles = ScopeRoles(row, scopePorts);
        return roles is null ? (byte)0 : PassageLaw.ProjectScope(support, roles, scopePorts.Length);
    }

    private int[]? ScopeRoles(ConnectionRow row, int[] scopePorts)
    {
        ArgumentNullException.ThrowIfNull(scopePorts);
        var ends = Wiring(row);
        if (owner.ObservePassageSites(scopePorts, 0) == 0 || Observe(row, 0) == 0) return null;
        var roles = ends.Select(port => Array.IndexOf(scopePorts, port)).ToArray();
        return roles.Any(position => position < 0) ? null : roles;
    }
}
