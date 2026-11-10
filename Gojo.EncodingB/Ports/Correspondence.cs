namespace Gojo.EncodingB;

/// <summary>A declared law from one port basis to another. Without one a
/// connection stays open: a published value is never silently reinterpreted.
/// <list type="bullet">
/// <item>A <b>read</b> correspondence is a relation law over (from, to). It says
/// which receiver values a published value may mean. Several admitted values
/// are competing candidates; none means the lane is blocked.</item>
/// <item>A <b>material</b> correspondence is a transition law. Its roles bind the
/// port lane (source), the receiver's acceptance (optional) and the arrival, and
/// its signature must preserve a sum spanning the source and the arrival.</item>
/// </list></summary>
public sealed class Correspondence
{
    private Correspondence(LawTable law, PortBasis from, PortBasis to, string? sourceRole, string? acceptRole, string? arrivalRole)
    {
        Law = law;
        From = from;
        To = to;
        SourceRole = sourceRole;
        AcceptRole = acceptRole;
        ArrivalRole = arrivalRole;
    }

    public LawTable Law { get; }
    public PortBasis From { get; }
    public PortBasis To { get; }

    public bool IsMaterial => Law is TransitionLaw;

    /// <summary>Material only: the role bound to the port lane.</summary>
    public string? SourceRole { get; }

    /// <summary>Material only: the role the receiver decides (open when it has room).</summary>
    public string? AcceptRole { get; }

    /// <summary>Material only: the role bound to the arrival at the inlet lane.</summary>
    public string? ArrivalRole { get; }

    /// <summary>A read through a relation law whose role 0 reads the port basis and
    /// role 1 the inlet basis.</summary>
    public static Correspondence Read(RelationLaw law, PortBasis from, PortBasis to)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (law.Roles.Count != 2) throw new ArgumentException("A read correspondence has two roles: from and to.", nameof(law));
        RequireValues(law.Roles[0], from, "from");
        RequireValues(law.Roles[1], to, "to");
        return new Correspondence(law, from, to, null, null, null);
    }

    /// <summary>The identity on one basis, declared rather than assumed.</summary>
    public static Correspondence Identity(PortBasis basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        var law = RelationLaw.FromPredicate(
            $"identity on basis {basis.Fingerprint}",
            [new LawRole("from", basis.Domain), new LawRole("to", basis.Domain)],
            t => t[0] == t[1],
            "declared identity");
        return new Correspondence(law, basis, basis, null, null, null);
    }

    /// <summary>A material transfer through a transition law. The units must be
    /// the same: a 1:1 transfer between different unit promises would need a
    /// calibration, which this layer does not have.</summary>
    public static Correspondence Material(
        TransitionLaw law,
        PortBasis from,
        PortBasis to,
        string sourceRole,
        string arrivalRole,
        string? acceptRole = null)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        var bound = new[] { sourceRole, arrivalRole, acceptRole }.OfType<string>().ToArray();
        if (bound.Distinct(StringComparer.Ordinal).Count() != bound.Length || law.Roles.Count != bound.Length ||
            bound.Any(role => law.Roles.All(r => r.Name != role)))
            throw new ArgumentException($"Bind every role of {law.Name} exactly once: source, arrival and optionally acceptance.", nameof(law));
        RequireValues(law.Roles[law.RoleIndex(sourceRole)], from, "source");
        RequireValues(law.Roles[law.RoleIndex(arrivalRole)], to, "arrival");
        if (acceptRole is not null && law.Roles[law.RoleIndex(acceptRole)].Domain.Size != 2)
            throw new ArgumentException("The acceptance role reads closed or open.", nameof(acceptRole));
        if (!law.Signature.Functional)
            throw new ArgumentException($"{law.Name} is not functional; a material transfer must execute.", nameof(law));
        if (!law.Signature.Invariants.OfType<PreservedSum>().Any(sum => sum.Roles.Contains(sourceRole) && sum.Roles.Contains(arrivalRole)))
            throw new ArgumentException($"{law.Name} does not preserve a sum spanning {sourceRole} and {arrivalRole}; it is not a material promise.", nameof(law));
        if (from.Unit != to.Unit)
            throw new ArgumentException($"Moving '{from.Unit}' into '{to.Unit}' needs a calibration, not a 1:1 transfer.", nameof(to));
        return new Correspondence(law, from, to, sourceRole, acceptRole, arrivalRole);
    }

    public string Describe() =>
        $"{(IsMaterial ? "material" : "read")} correspondence {Law.Name} ({Law.ContentHash}) from basis {From.Fingerprint} to {To.Fingerprint}";

    private static void RequireValues(LawRole role, PortBasis basis, string side)
    {
        if (!role.Domain.Values.SequenceEqual(basis.Domain.Values))
            throw new ArgumentException($"Role {role.Name} ({side}) reads {role.Domain.Describe(role.Domain.Full)}, but the basis reads {basis.Domain.Describe(basis.Domain.Full)}.");
    }
}
