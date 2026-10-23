using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Substrate = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrate;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Narrow, native source ports for attaching ornament to an existing
/// mesh path. Incidence, tangent direction and actual site aliasing are separate
/// reads. Exposing an interior occurrence never splits or welds the source.</summary>
public static class OrnamentMotifAttachment
{
    // port = (frame -> source envelope) ->
    //        (actual endpoint/landmark selection -> (position -> (tangent -> difference receipt)))
    // endpoint selection = Closed((section -> actual longitudinal endpoint marker) -> (site -> position))
    // interior selection = (lattice -> actual landmark) -> Closed(optional crossing event)
    // attachment = (mesh port -> actual motif-start port) ->
    //              (position difference -> ((mesh tangent -> motif tangent) -> motion basis))
    // Endpoint markers distinguish the two ends even when a closed curve has
    // the same physical site and control word at both ends. Start/end selection
    // never silently reverses the source traversal.

    public sealed record PortRead(Relation Envelope, Relation Section, IElement? Site,
        Relation Position, Relation Tangent, bool Complete, bool Interior, bool? AtStart, Relation Surface);
    public sealed record AttachmentRead(PortRead Port, Relation Motif, PortRead Root,
        bool? Incident, bool? Forward, bool? Opposed, bool SharedSite, Relation Surface)
    {
        public bool? Smooth => Incident == false || Forward == false ? false : Incident == true && Forward == true ? true : null;
    }

    /// <summary>start=true exposes the outgoing forward tangent. start=false
    /// exposes the incoming forward tangent. Width or cap uncertainty does not
    /// erase an independently readable source spine.</summary>
    public static PortRead Endpoint(Curve.Frame frame, Relation envelope, bool start = false)
    {
        var source = Band.ReadEnvelope(frame, envelope).Sweep.Spine;
        if (!source.Complete) return ReadPort(frame, new Relation(new Relation(frame.Surface, envelope), StructuralConstraint.Open(envelope)));
        var position = Word(source, start); var site = start ? source.Start : source.End;
        var marker = Marker(source.Section, start); var selection = S(new Relation(new Relation(source.Section, marker), new Relation(site, position)));
        var tangent = Derivative(frame, source, start);
        return ReadPort(frame, Port(frame, envelope, selection, position, tangent.Word, tangent.Proof));
    }

    /// <summary>A selected substrate occurrence strictly inside one straight
    /// span. Its tangent follows that actual span. An optional crossing event
    /// must bind this exact occurrence and envelope, even when its counterpart
    /// crossing remains unresolved. Curved interior queries stay open.</summary>
    public static PortRead Interior(Substrate.Lattice lattice, Substrate.Landmark landmark,
        Relation envelope, Relation? crossEvent = null)
    {
        var frame = lattice.Frame; var source = Band.ReadEnvelope(frame, envelope).Sweep.Spine;
        var selection = new Relation(new Relation(lattice.Surface, landmark.Surface), crossEvent is null ? S() : S(crossEvent));
        if (!source.Complete) return ReadPort(frame, new Relation(new Relation(frame.Surface, envelope), new Relation(selection, StructuralConstraint.Open(envelope))));
        var tangent = Derivative(frame, source, true);
        return ReadPort(frame, Port(frame, envelope, selection, landmark.Position, tangent.Word, tangent.Proof));
    }

    /// <summary>Reads only the exposed exact port grammar and rechecks its
    /// native source and derivative receipt. Copying equal coordinates or a
    /// tangent vector does not substitute for the actual selected operands.</summary>
    public static PortRead ReadPort(Curve.Frame frame, Relation surface)
    {
        PortRead Open(Relation? envelope = null, Relation? section = null, bool interior = false) => new(
            envelope ?? StructuralConstraint.Open(), section ?? StructuralConstraint.Open(), null,
            StructuralConstraint.Open(), StructuralConstraint.Open(), false, interior, null, surface);
        if (surface is not { Left: Relation { Left: var selectedFrame, Right: Relation envelope },
                Right: Relation { Left: Relation selection, Right: Relation { Left: Relation position,
                    Right: Relation { Left: Relation tangent, Right: Relation derivative } } } } || !Same(selectedFrame, frame.Surface)) return Open();
        var source = Band.ReadEnvelope(frame, envelope).Sweep.Spine;
        if (!source.Complete) return Open(envelope);
        bool start; IElement? site; bool interior;
        if (Entries(selection, out var entries))
        {
            if (entries is not [Relation { Left: Relation { Left: var section, Right: var marker }, Right: Relation { Left: var selectedSite, Right: var selectedPosition } }] ||
                !Same(section, source.Section) || !Same(selectedPosition, position)) return Open(envelope, source.Section);
            start = Same(marker, Marker(source.Section, true));
            if (!start && !Same(marker, Marker(source.Section, false))) return Open(envelope, source.Section);
            site = start ? source.Start : source.End;
            if (!Same(site, selectedSite) || !Same(position, Word(source, start))) return Open(envelope, source.Section);
            interior = false;
        }
        else
        {
            if (selection is not { Left: Relation { Left: Relation latticeSurface, Right: Relation nodeSurface }, Right: Relation events } ||
                !Entries(events, out var witnesses) || witnesses.Length > 1) return Open(envelope, source.Section, true);
            var lattice = Substrate.Read(frame, latticeSurface);
            var landmark = lattice.Landmarks.FirstOrDefault(n => Same(n.Surface, nodeSurface));
            if (!lattice.Complete || landmark is null || !Same(position, landmark.Position) ||
                source.Controls.Length != 2 || !frame.Vector(position, out var at) || !StrictInterior(at, source.Controls[0], source.Controls[1]) ||
                witnesses.Any(w => w is not Relation cross || !CrossSelects(frame, cross, latticeSurface, nodeSurface, envelope)))
                return Open(envelope, source.Section, true);
            start = true; site = null; interior = true;
        }
        if (!Derivative(frame, source, start, derivative, tangent)) return Open(envelope, source.Section, interior);
        return new(envelope, source.Section, site, position, tangent, true, interior, interior ? null : start, surface);
    }

    public static AttachmentRead Attach(Curve.Frame frame, Relation meshPort, Relation motifEnvelope)
    {
        var port = ReadPort(frame, meshPort); var root = Endpoint(frame, motifEnvelope, true);
        var difference = port.Complete && root.Complete ? Substrate.Difference(frame, port.Position, root.Position) : StructuralConstraint.Open(meshPort, root.Surface);
        var tangentComparison = new Relation(new Relation(port.Tangent, root.Tangent), frame.Motion.Surface);
        return ReadAttachment(frame, new Relation(new Relation(meshPort, root.Surface), new Relation(difference, tangentComparison)));
    }

    public static AttachmentRead ReadAttachment(Curve.Frame frame, Relation surface)
    {
        var unknown = ReadPort(frame, StructuralConstraint.Open());
        AttachmentRead Open(PortRead? port = null, PortRead? root = null) => new(port ?? unknown,
            root?.Envelope ?? StructuralConstraint.Open(), root ?? unknown, null, null, null, false, surface);
        if (surface is not { Left: Relation { Left: Relation sourcePort, Right: Relation rootPort },
                Right: Relation { Left: Relation positionDifference, Right: Relation { Left: Relation tangentPair, Right: var basis } } } ||
            !Same(basis, frame.Motion.Surface)) return Open();
        var port = ReadPort(frame, sourcePort); var root = ReadPort(frame, rootPort);
        if (!port.Complete || !root.Complete || root.Interior || root.AtStart != true ||
            !Same(tangentPair.Left, port.Tangent) || !Same(tangentPair.Right, root.Tangent)) return Open(port, root);
        var incident = Substrate.ReadDifference(frame, positionDifference, port.Position, root.Position);
        if (incident is null) return Open(port, root);
        bool? forward = null; bool? opposed = null;
        if (frame.Vector(port.Tangent, out var a) && frame.Vector(root.Tangent, out var b) && !a.Zero && !b.Zero)
        {
            var collinear = Curve.Vector.Cross(a, b).IsZero; var orientation = Curve.Vector.Dot(a, b).Sign;
            forward = collinear && orientation > 0; opposed = collinear && orientation < 0;
        }
        var shared = port.Site is not null && root.Site is not null && Same(port.Site, root.Site);
        return new(port, root.Envelope, root, incident, forward, opposed, shared, surface);
    }

    private static Relation Port(Curve.Frame frame, Relation envelope, Relation selection,
        Relation position, Relation tangent, Relation derivative) => new(new Relation(frame.Surface, envelope),
            new Relation(selection, new Relation(position, new Relation(tangent, derivative))));

    private static (Relation Word, Relation Proof) Derivative(Curve.Frame frame, Curve.Read source, bool start)
    {
        var controls = Items(source.ControlWords).Cast<Relation>().ToArray(); var other = Other(source, start);
        var from = start ? controls[0] : controls[other]; var to = start ? controls[other] : controls[^1];
        var proof = Substrate.Difference(frame, from, to); var receipt = (Relation)((Relation)proof.Right).Right;
        return (frame.Motion.TryReadResult(receipt, out var word, out _, out _) ? word : StructuralConstraint.Open(proof), proof);
    }
    private static bool Derivative(Curve.Frame frame, Curve.Read source, bool start, Relation proof, Relation word)
    {
        var controls = Items(source.ControlWords).Cast<Relation>().ToArray(); var other = Other(source, start);
        var from = start ? controls[0] : controls[other]; var to = start ? controls[other] : controls[^1];
        return Substrate.ReadDifference(frame, proof, from, to) is not null && proof.Right is Relation { Right: Relation receipt } &&
            frame.Motion.TryReadResult(receipt, out var result, out _, out _) && Same(result, word);
    }
    private static int Other(Curve.Read source, bool start)
    {
        if (start) { for (var i = 1; i < source.Controls.Length; i++) if (source.Controls[i] != source.Controls[0]) return i; return 0; }
        for (var i = source.Controls.Length - 2; i >= 0; i--) if (source.Controls[i] != source.Controls[^1]) return i;
        return source.Controls.Length - 1;
    }
    private static bool CrossSelects(Curve.Frame frame, Relation eventSurface, Relation lattice,
        Relation node, Relation envelope)
    {
        if (eventSurface is not { Left: Relation { Left: var actualLattice, Right: var actualNode },
                Right: Relation { Left: Relation { Left: Relation first, Right: Relation second }, Right: Relation crossing } } ||
            !Same(actualLattice, lattice) || !Same(actualNode, node) || !Same(first, envelope) && !Same(second, envelope)) return false;
        var a = Band.ReadEnvelope(frame, first).Sweep.Spine; var b = Band.ReadEnvelope(frame, second).Sweep.Spine;
        var marker = Substrate.Read(frame, lattice).Landmarks.First(n => Same(n.Surface, node));
        var read = Band.ReadCrossing(frame, crossing);
        return a.Complete && b.Complete && Same(read.First, a.Section) && Same(read.Second, b.Section) && Same(read.Locus, marker.Position);
    }
    private static bool StrictInterior(Curve.Vector at, Curve.Vector from, Curve.Vector to)
    {
        var direction = to - from; var delta = at - from; var progress = Curve.Vector.Dot(delta, direction);
        return !direction.Zero && Curve.Vector.Cross(delta, direction).IsZero && progress.Sign > 0 && progress < Curve.Vector.Dot(direction, direction);
    }
    private static Relation Word(Curve.Read source, bool start) => (Relation)Items(source.ControlWords)[start ? 0 : source.Controls.Length - 1];
    private static IElement Marker(Relation section, bool start)
    {
        var interval = (Relation)((Relation)((Relation)((Relation)section.Right).Right).Left).Right;
        var markers = (Relation)interval.Left; return start ? markers.Left : markers.Right;
    }
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] Items(Relation scope) => Entries(scope, out var entries) ? entries : [];
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
}
