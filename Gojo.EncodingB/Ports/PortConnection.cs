using System.Numerics;

namespace Gojo.EncodingB;

/// <summary>What a connect or bind attempt did. A refused attempt connects
/// nothing and says why.</summary>
public sealed record ConnectResult(PortConnection? Connection, string Read)
{
    public bool Connected => Connection is not null;
}

/// <summary>A connection from a port to an inlet, lane i to lane i. It runs the
/// read/flow spine one stage at a time: publication, correspondence, the
/// receiver's basis and room, accepted and blocked lanes, and what remains.
/// <list type="bullet">
/// <item>Open (no correspondence): reads stay open and keep the publication as
/// residue; pending material stays at the source as remainder.</item>
/// <item>Read correspondence: <see cref="Read"/> projects every lane at once
/// through the relation law. The result can inform a workspace's support, or be
/// supplied to a non-material inlet field as one grouped commit.</item>
/// <item>Material correspondence: <see cref="Transfer"/> runs the transition law
/// across all lanes at once, offering from the source's committed state and
/// letting the receiver accept by its own room.</item>
/// </list></summary>
public sealed class PortConnection
{
    internal PortConnection(Port port, Inlet inlet, Correspondence? correspondence)
    {
        Port = port;
        Inlet = inlet;
        Correspondence = correspondence;
    }

    public Port Port { get; }
    public Inlet Inlet { get; }
    public Correspondence? Correspondence { get; private set; }

    /// <summary>No declared correspondence: an open slot.</summary>
    public bool IsOpen => Correspondence is null;

    public int LaneCount => Port.LaneCount;

    /// <summary>Declares the correspondence of an open connection. Refused, and the
    /// connection left open, when the law does not read from the port's basis into
    /// the inlet's or breaks the port's accounting class.</summary>
    public ConnectResult Bind(Correspondence correspondence)
    {
        ArgumentNullException.ThrowIfNull(correspondence);
        if (!IsOpen) return new ConnectResult(null, $"refused: already connected through {Correspondence!.Law.Name}");
        if (Refusal(Port, Inlet, correspondence) is { } refusal) return new ConnectResult(null, refusal);
        Correspondence = correspondence;
        return new ConnectResult(this, $"connected through {correspondence.Law.Name} ({correspondence.Law.ContentHash})");
    }

    /// <summary>Reads every lane through the read correspondence in one bit-sliced
    /// narrowing. Reading never changes the source.</summary>
    public PortRead Read()
    {
        if (Correspondence is { IsMaterial: true })
            throw new InvalidOperationException($"{Describe()} transfers material; observe through a read correspondence on a coupled port.");
        var publication = Port.Read();
        var lanes = Port.Lanes;
        var readable = publication.Readable;
        var supports = Enumerable.Repeat(Inlet.Basis.Domain.Full, LaneCount).ToArray();
        if (IsOpen) return new PortRead(this, publication, supports, 0, 0, 0, readable, lanes & ~readable);

        var law = (RelationLaw)Correspondence!.Law;
        var from = new ulong[law.Roles[0].Domain.Size];
        for (var value = 0; value < from.Length; value++) from[value] = publication.LanesReading(value);
        var to = Enumerable.Repeat(readable, law.Roles[1].Domain.Size).ToArray();
        var narrowed = law.Narrow([from, to], readable)[1];
        ulong seen = 0, twice = 0;
        foreach (var plane in narrowed)
        {
            twice |= seen & plane;
            seen |= plane;
        }
        foreach (var lane in Bits.Indexes(readable))
        {
            ulong support = 0;
            for (var value = 0; value < narrowed.Length; value++)
                if (Bits.Get(narrowed[value], lane)) support |= Bits.Of(value);
            supports[lane] = support;
        }
        return new PortRead(this, publication, supports, seen & ~twice, twice, readable & ~seen, 0, lanes & ~readable);
    }

    /// <summary>Supplies a read to the inlet's own field: a grouped commit. Every
    /// lane must settle, the field must be an observed (non-material) one, and the
    /// source must not have changed since the read. Otherwise no lane is supplied.</summary>
    public PortCommit Commit(PortRead read)
    {
        ArgumentNullException.ThrowIfNull(read);
        if (!ReferenceEquals(read.Connection, this)) throw new ArgumentException("That read came through another connection.", nameof(read));
        if (IsOpen) return PortCommit.Refused("open: no declared correspondence, so nothing is supplied", Port.Lanes);
        var grammar = Inlet.Owner.Grammar;
        var planes = grammar.PlanesOf(Inlet.Field);
        if (Inlet.Tension is not null || planes.Any(p => grammar.Plane(p).Material || grammar.Plane(p).BasisSupplied))
            return PortCommit.Refused($"'{Inlet.Field}' is material or basis-supplied; an observation can inform a workspace's support but cannot write it", 0);
        if (Port.Owner.Version != read.Publication.OwnerVersion)
            return PortCommit.Refused(
                $"stale: read at {Port.OwnerName} version {read.Publication.OwnerVersion}, which is now at {Port.Owner.Version}; nothing is supplied", 0);
        var unsettled = Port.Lanes & ~read.Settled;
        if (unsettled != 0)
            return PortCommit.Refused(
                $"a grouped port supplies every lane or none, and {string.Join("; ", Bits.Indexes(unsettled).Select(read.Describe))}",
                unsettled);
        var thermometer = grammar.TryField(Inlet.Field, out var field) && field is ThermometerField;
        for (var lane = 0; lane < LaneCount; lane++)
        {
            var value = BitOperations.TrailingZeroCount(read.Supports[lane]);
            var code = thermometer ? Thermometer.Encode(value, planes.Count) : (ulong)value;
            for (var bit = 0; bit < planes.Count; bit++)
                Inlet.Owner.Supply(planes[bit], Inlet.SiteOf(lane), Bits.Get(code, bit));
        }
        return new PortCommit(true, $"supplied {LaneCount} lane(s) of {Inlet.OwnerName}.{Inlet.Field}", 0);
    }

    /// <summary>One material transfer, one unit per lane, run as the material
    /// correspondence's law across every lane at once:
    /// <list type="number">
    /// <item>offer: lanes whose publication holds a unit, read from committed state;</item>
    /// <item>accept: the receiver opens the acceptance role where it has room;</item>
    /// <item>the law steps; a lane whose source gave and whose arrival filled is
    /// accepted, and an offered lane that did not move is blocked;</item>
    /// <item>return: the source releases accepted units and the receiver takes
    /// each as one unit of positive tension, to absorb on its own tick.</item>
    /// </list>
    /// <c>pending = offered + remainder</c> and <c>offered = accepted + blocked</c> are
    /// checked every time.</summary>
    public PortTransfer Transfer()
    {
        var publication = Port.Read();
        var pending = Port.Lanes & publication.Readable & ~publication.LanesReading(0);
        if (IsOpen)
            return new PortTransfer(pending, 0, 0, 0, pending, null, [],
                "open: no declared material correspondence, so pending units stay at the source as remainder");
        if (!Correspondence!.IsMaterial) throw new InvalidOperationException("A read correspondence observes; it never transfers.");

        var law = (TransitionLaw)Correspondence.Law;
        var receiver = Inlet.Tension!;
        ulong accept = 0;
        foreach (var lane in Bits.Indexes(Port.Lanes))
            if (receiver.Room(Inlet.SiteOf(lane)) > 0) accept |= Bits.Of(lane);

        var codes = new ulong[law.Roles.Count][];
        for (var r = 0; r < law.Roles.Count; r++)
        {
            codes[r] = new ulong[law.Roles[r].Domain.CodeBits];
            if (law.Roles[r].Name == Correspondence.SourceRole)
                for (var bit = 0; bit < codes[r].Length; bit++)
                    for (var lane = 0; lane < LaneCount; lane++)
                        if (publication.Values[lane] is int value && ((value >> bit) & 1) != 0) codes[r][bit] |= Bits.Of(lane);
            if (law.Roles[r].Name == Correspondence.AcceptRole) codes[r][0] = accept;
            // The arrival role starts empty on every lane: code 0.
        }
        var step = law.Step(codes, Port.Lanes & publication.Readable);
        var source = law.RoleIndex(Correspondence.SourceRole!);
        var arrival = law.RoleIndex(Correspondence.ArrivalRole!);
        var arrived = step.After[arrival].Aggregate(0UL, (lanes, plane) => lanes | plane);
        ulong gave = 0;
        for (var bit = 0; bit < codes[source].Length; bit++) gave |= step.After[source][bit] ^ codes[source][bit];

        var hard = new List<string>();
        if (gave != arrived) hard.Add($"lanes that gave ({Format(gave)}) differ from lanes that received ({Format(arrived)})");
        if ((arrived & ~pending) != 0) hard.Add($"lanes {Format(arrived & ~pending)} received a unit nobody offered");
        var accepted = arrived & pending;
        var blocked = pending & ~accepted;
        if ((accepted | blocked) != pending || (accepted & blocked) != 0) hard.Add("offered != accepted + blocked");

        if (Port.Tension!.Release(Port.SitesOf(accepted)) is var underflow and not 0)
            hard.Add($"source sites {Format(underflow)} had no unit to release");
        if (receiver.Receive(Inlet.SitesOf(accepted)) is var overflow and not 0)
            hard.Add($"receiver sites {Format(overflow)} overflowed");
        return new PortTransfer(pending, pending, accepted, blocked, 0, step, hard,
            $"{Bits.Count(accepted)} accepted, {Bits.Count(blocked)} blocked by the receiver's room");
    }

    public string Describe() =>
        $"{Port.OwnerName}.{Port.Name} -> {Inlet.OwnerName}.{Inlet.Name} ({(IsOpen ? "open" : Correspondence!.Describe())})";

    public override string ToString() => Describe();

    /// <summary>Why a correspondence cannot connect this port to this inlet, or null.</summary>
    internal static string? Refusal(Port port, Inlet inlet, Correspondence correspondence)
    {
        if (!correspondence.From.SameAs(port.Basis))
            return $"refused: {correspondence.Law.Name} reads from basis {correspondence.From.Fingerprint}, but port {port.Name} publishes basis {port.Basis.Fingerprint}";
        if (!correspondence.To.SameAs(inlet.Basis))
            return $"refused: {correspondence.Law.Name} reads into basis {correspondence.To.Fingerprint}, but inlet {inlet.Name} reads basis {inlet.Basis.Fingerprint}";
        if (!correspondence.IsMaterial)
            return port.Accounting == PortAccounting.Material
                ? $"refused: port {port.Name} is a material port; publish it as coupled to let readers observe it"
                : null;
        if (port.Accounting == PortAccounting.Observation)
            return $"refused: port {port.Name} is an observation port; a material correspondence would drain it";
        if (inlet.Tension is null)
            return $"refused: inlet {inlet.Name} is not a tension inlet; material arrives only as tension";
        var parts = port.Connections
            .Where(c => c.Correspondence is { IsMaterial: true })
            .Select(c => c.Correspondence!)
            .Append(correspondence)
            .ToArray();
        return parts.Length > 1 && JointRefusal(port, parts) is { } joint
            ? $"refused: a material port cannot fan out. {joint}"
            : null;
    }

    /// <summary>Composes every material connection of a port into one joint law,
    /// as they would run in one double-buffered phase: each reads the same
    /// committed lane. The port's material promise is that the sum of the lane and
    /// every arrival is preserved. If the joint table breaks it, the reason names
    /// the transition that would duplicate a unit.</summary>
    private static string? JointRefusal(Port port, IReadOnlyList<Correspondence> parts)
    {
        var roles = new List<LawRole> { new(port.Name, port.Basis.Domain) };
        var arrivals = new List<string>();
        var maps = new List<(TransitionLaw Law, int[] Joint)>();
        for (var i = 0; i < parts.Count; i++)
        {
            var law = (TransitionLaw)parts[i].Law;
            var joint = new int[law.Roles.Count];
            for (var r = 0; r < law.Roles.Count; r++)
            {
                var name = law.Roles[r].Name;
                if (name == parts[i].SourceRole) continue;
                joint[r] = roles.Count;
                roles.Add(new LawRole($"{name}{i + 1}", law.Roles[r].Domain));
                if (name == parts[i].ArrivalRole) arrivals.Add($"{name}{i + 1}");
            }
            maps.Add((law, joint));
        }
        var states = roles.Aggregate(1, (product, role) => checked(product * role.Domain.Size));
        var probe = new RelationLaw("joint", roles, new byte[(states + 7) / 8]);
        var pairs = new List<(int Before, int After)>();
        for (var before = 0; before < states; before++)
        {
            var values = probe.Tuple(before);
            var after = values.ToArray();
            int? laneWrite = null;
            var conflict = false;
            foreach (var (law, joint) in maps)
            {
                var partAfter = law.Tuple(law.Afters(law.TupleIndex(joint.Select(j => values[j]).ToArray()))[0]);
                for (var r = 0; r < joint.Length; r++)
                {
                    if (joint[r] != 0)
                    {
                        after[joint[r]] = partAfter[r];
                        continue;
                    }
                    if (partAfter[r] == values[0]) continue;
                    conflict |= laneWrite is int written && written != partAfter[r];
                    laneWrite = partAfter[r];
                }
            }
            if (conflict) continue;
            if (laneWrite is int lane) after[0] = lane;
            pairs.Add((before, probe.TupleIndex(after)));
        }
        try
        {
            TransitionLaw.FromPairs(
                $"{port.Name} read by {parts.Count} material connections at once",
                roles,
                pairs,
                new AccountingSignature(functional: true, new PreservedSum([port.Name, .. arrivals])),
                maps[0].Law.Confluence,
                "composed when connecting");
            return null;
        }
        catch (ArgumentException refusal)
        {
            return refusal.Message;
        }
    }

    private static string Format(ulong lanes) => "{" + string.Join(",", Bits.Indexes(lanes)) + "}";
}

/// <summary>A read through a connection, lane by lane, in the receiver's basis.
/// <list type="bullet">
/// <item><see cref="Settled"/>: exactly one receiver value.</item>
/// <item><see cref="Competing"/>: several candidates remain; not a probability.</item>
/// <item><see cref="Blocked"/>: the published value has no correspondent in the
/// receiver basis. It is kept visible, never moved to a nearby legal value.</item>
/// <item><see cref="Open"/>: no declared correspondence; the support stays open.</item>
/// <item><see cref="Undecodable"/>: the source bits did not decode.</item>
/// </list>
/// Blocked and open lanes keep their published value as residue.</summary>
public sealed record PortRead(
    PortConnection Connection,
    PortPublication Publication,
    IReadOnlyList<ulong> Supports,
    ulong Settled,
    ulong Competing,
    ulong Blocked,
    ulong Open,
    ulong Undecodable)
{
    /// <summary>Lanes whose publication is kept unread.</summary>
    public ulong Residue => Open | Blocked;

    public string Describe(int lane)
    {
        var head = $"{Connection.Port.Name}[{lane}] published {Publication.NameOf(lane)}";
        if (Bits.Get(Undecodable, lane)) return $"{head}: the source bits do not decode, so the lane stays open";
        if (Bits.Get(Open, lane)) return $"{head}: open, with no declared correspondence; kept as residue";
        if (Bits.Get(Blocked, lane)) return $"{head}: blocked, with no correspondent in the receiver basis; kept as residue";
        var domain = Connection.Inlet.Basis.Domain;
        return $"{head}: reads {domain.Describe(Supports[lane])}{(Bits.Get(Competing, lane) ? " (competing candidates)" : "")}";
    }

    /// <summary>Narrows a receiver workspace's support with this read. Settled and
    /// competing lanes narrow; open, blocked and undecodable lanes leave the
    /// support as it was. Unselected and introduced sites refuse the evidence.</summary>
    public PortDelivery DeliverTo(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var inlet = Connection.Inlet;
        if (!ReferenceEquals(workspace.Source, inlet.Owner))
            throw new ArgumentException($"Workspace {workspace.Name} reads {workspace.Source.Name}, not {inlet.OwnerName}.", nameof(workspace));
        ulong narrowed = 0, refused = 0;
        foreach (var lane in Bits.Indexes(Settled | Competing))
        {
            if (workspace.Narrow(inlet.Field, inlet.SiteOf(lane), Supports[lane])) narrowed |= Bits.Of(lane);
            else refused |= Bits.Of(lane);
        }
        return new PortDelivery(narrowed, refused, Open | Blocked | Undecodable);
    }
}

/// <summary>Lanes whose evidence narrowed a workspace's support, lanes whose site
/// the workspace refused (not selected, or introduced), and lanes left open.</summary>
public sealed record PortDelivery(ulong Narrowed, ulong Refused, ulong KeptOpen);

/// <summary>A grouped commit of a read to a non-material inlet field.</summary>
public sealed record PortCommit(bool Committed, string Read, ulong UnsettledLanes)
{
    public static PortCommit Refused(string why, ulong unsettled) => new(false, why, unsettled);
}

/// <summary>One material transfer, as lane planes. <c>Pending = Offered + Remainder</c>
/// and <c>Offered = Accepted + Blocked</c>. Blocked units stay with the source as
/// tension; a remainder had no declared route.</summary>
public sealed record PortTransfer(
    ulong Pending,
    ulong Offered,
    ulong Accepted,
    ulong Blocked,
    ulong Remainder,
    TransitionStep? Step,
    IReadOnlyList<string> HardViolations,
    string Read);
