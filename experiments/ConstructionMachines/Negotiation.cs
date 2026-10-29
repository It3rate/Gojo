using System.Numerics;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// A local executor over a retained machine and scalar response routes. The
/// public side contains sites, bases and copied observations, never the private
/// machine or its actuators. This is a selected displacement-response experiment,
/// not a general force law, confidence model or global constraint solver.
/// </summary>
public sealed class NegotiatingMachine
{
    // Durable shape: G(definition, points, ports, routes, publication clock,
    // publications, timeline, receipts, initial sites, last application, parameter samples,
    // private-output/public-site correspondence).
    public Relation Definition { get; private set; }
    public IReadOnlyList<Relation> Points => points;
    public IReadOnlyList<Relation> Ports => ports;
    public IReadOnlyList<Relation> Responses => routes;
    public Relation CurrentPublication { get; private set; }
    public Relation Timeline { get; private set; }
    public Relation? LastReceipt => receipts.LastOrDefault();
    public Relation Surface => G(Definition, G(points.ToArray()), G(ports.ToArray()), G(routes.ToArray()),
        clock, CurrentPublication, Timeline, G(receipts.ToArray()), initial, lastApplication, parameterSamples, correspondence);

    private readonly List<Relation> points = [];
    private readonly List<Relation> ports = [];
    private readonly List<Relation> routes = [];
    private readonly List<Relation> receipts = [];
    private readonly List<Relation> steps = [];
    private readonly Carrier clock;
    private readonly IElement initial;
    private Relation lastApplication;
    private Relation parameterSamples;
    private Relation correspondence;

    public NegotiatingMachine(Relation definition)
    {
        Definition = definition;
        clock = new Carrier();
        lastApplication = MachineRunner.Run(definition);
        if (!WholeOutputs(lastApplication, out var values))
            throw new ArgumentException("The initial machine must expose resolved whole-coordinate points.", nameof(definition));
        foreach (var value in values)
        {
            var coordinates = F(value).Select(c =>
            {
                ProjectionResidue.TryRead(c, out var axis, out var n, out _);
                return (IElement)new CarrierLocation(axis, new TickRef(checked((long)n)));
            }).ToArray();
            var point = Relation.OrderedGroup((Carrier)value.Right, coordinates);
            points.Add(point);
            foreach (var coordinate in coordinates.Cast<CarrierLocation>())
                ports.Add(G(coordinate, Relation.Segment(coordinate.Carrier, 0, 1), clock, new Placeholder()));
        }
        initial = CapturePoints();
        correspondence = MakeCorrespondence();
        parameterSamples = G();
        CurrentPublication = MakePublications();
        Timeline = TimeStepSequence.Create(new Carrier(), clock, initial);
    }

    private NegotiatingMachine(Relation surface, bool restoring)
    {
        var f = F(surface);
        if (f.Length != 12) throw new ArgumentException("An exact local negotiation surface is required.", nameof(surface));
        Definition = (Relation)f[0];
        points.AddRange(F(f[1]).Cast<Relation>());
        ports.AddRange(F(f[2]).Cast<Relation>());
        routes.AddRange(F(f[3]).Cast<Relation>());
        clock = (Carrier)f[4];
        CurrentPublication = (Relation)f[5];
        Timeline = (Relation)f[6];
        receipts.AddRange(F(f[7]).Cast<Relation>());
        initial = f[8]; lastApplication = (Relation)f[9]; parameterSamples = (Relation)f[10];
        correspondence = (Relation)f[11];
        if (!TimeStepSequence.TryRead(Timeline, out _, out _, out var restoredSteps))
            throw new ArgumentException("The retained local timeline is unavailable.", nameof(surface));
        steps.AddRange(restoredSteps);
    }

    public static NegotiatingMachine Restore(Relation surface) => new(surface, true);
    public Relation Point(int outputIndex) => points[outputIndex];
    public Relation Port(int outputIndex, int coordinateIndex) => coordinateIndex is 0 or 1
        ? ports[checked(outputIndex * 2 + coordinateIndex)] : throw new ArgumentOutOfRangeException(nameof(coordinateIndex));

    /// <summary>
    /// An explicitly authored replacement under the SAME ordered public output
    /// contract. It must reproduce every current site in its actual basis.
    /// Successful replacement retires old response routes; their private actuator
    /// correspondence cannot be inferred from matching coordinates. Callers may
    /// then author new routes to the retained public ports.
    /// </summary>
    public bool TryReplaceDefinition(Relation replacement, out Relation witness)
    {
        var before = CurrentPublication;
        var candidate = MachineRunner.Run(replacement);
        var previous = G(Definition, G(routes.ToArray()), correspondence, parameterSamples, lastApplication);
        if (!CurrentStateMatches() || !WholeOutputs(candidate, out var values) || values.Length != points.Count ||
            values.Select((point, i) => SameCoordinates(point, points[i])).Any(matches => !matches))
        {
            witness = ReadWitness.Create(new Carrier(), previous, G(ports.ToArray()), new Placeholder(),
                G(candidate), G(replacement));
            return false;
        }
        Definition = replacement;
        lastApplication = candidate;
        routes.Clear();
        correspondence = MakeCorrespondence();
        parameterSamples = G();
        clock.Add(1);
        CurrentPublication = MakePublications();
        witness = ReadWitness.Create(new Carrier(), previous, G(ports.ToArray()),
            G(Definition, correspondence, CurrentPublication), G(), G(candidate, CapturePoints()));
        receipts.Add(witness);
        steps.Add(TimeStepSequence.Step(new Carrier(), clock, checked((long)clock.Value - 1), (long)clock.Value,
            before, witness, CurrentPublication, G()));
        Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
        return true;
    }

    /// <summary>
    /// Retains public displacement -> actuator displacement calibration, a
    /// dimensionless response fraction and one shared actuator capacity surface.
    /// The gain is authored response knowledge, not inferred from a shape name.
    /// </summary>
    public Relation AddResponse(Relation port, Carrier actuator,
        long gainN = 1, long gainD = 1, long responseN = 1, long responseD = 2,
        long min = 0, long max = 10000, long maxStep = 10000, long quantum = 1)
        => AddResponseCore(port,actuator,gainN,gainD,responseN,responseD,min,max,maxStep,quantum,false);

    private Relation AddResponseCore(Relation port,Carrier actuator,long gainN,long gainD,long responseN,long responseD,
        long min,long max,long maxStep,long quantum,bool family)
    {
        if (!ports.Any(p => ReferenceEquals(p, port)) || routes.Any(r => ReferenceEquals(F(r)[0], port)&&
                (!family||CapacityAxis((Relation)F(r)[3]).SameIdentityAs(actuator))) ||
            gainD <= 0 || responseN < 0 || responseD <= 0 || responseN > responseD || min < 0 || max < min ||
            maxStep < 0 || quantum <= 0 || actuator.IsUnknown || actuator.IsInverse ||
            actuator.Value < (ulong)min || actuator.Value > (ulong)max ||
            !Definition.Carriers().Any(c => c.SameIdentityAs(actuator)))
            throw new ArgumentException("The response must bind one owned port to a participating actuator and explicit valid limits.");
        var axis = PortAxis(port);
        var calibration = new Relation(Relation.Segment(axis, 0, gainD), Relation.Segment(actuator, 0, gainN));
        var capacity = routes.Select(r => (Relation)F(r)[3]).FirstOrDefault(c => CapacityAxis(c).SameIdentityAs(actuator));
        if (capacity is not null)
        {
            var bounds = Limits(capacity);
            if (bounds != (min, max, maxStep, quantum))
                throw new ArgumentException("Routes into one actuator must share the same declared capacity and quantum.");
        }
        else capacity = G(Relation.Segment(actuator, min, max), Relation.Segment(actuator, -maxStep, maxStep),
            Relation.Segment(actuator, 0, quantum));
        var fraction = ProjectionResidue.Create(new Carrier(), responseN, responseD);
        var route = G(port, calibration, fraction, capacity, G(Definition, actuator));
        routes.Add(route);
        parameterSamples = CaptureParameters();
        return route;
    }

    public sealed record ResponseTarget(Carrier Actuator,long GainN=1,long GainD=1,long Min=0,long Max=10000,long Step=10000,long Quantum=1);

    /// <summary>An explicitly coupled displacement response, not allocation of
    /// conserved input material. Each route retains its own calibration and
    /// receiver capacity; the family is validated atomically before attachment.</summary>
    public Relation AddCoupledResponses(Relation port,IReadOnlyList<ResponseTarget> targets,long responseN=1,long responseD=4)
    {
        if(targets.Count==0||routes.Any(r=>ReferenceEquals(F(r)[0],port)))
            throw new ArgumentException("Choose an unbound output port and a nonempty response family.");
        var preview=Restore(Surface);var start=routes.Count;
        foreach(var t in targets)preview.AddResponseCore(port,t.Actuator,t.GainN,t.GainD,responseN,responseD,
            t.Min,t.Max,t.Step,t.Quantum,true);
        var selected=preview.routes.Skip(start).ToArray();routes.AddRange(selected);parameterSamples=CaptureParameters();
        return G(port,G(selected));
    }

    /// <summary>A read of the currently published event; repeated reads do not republish.</summary>
    public Relation Publish(Relation port) => F(CurrentPublication).Cast<Relation>()
        .Single(observation => ReferenceEquals(F(observation)[0], port));

    /// <summary>
    /// Consumes one current batch. True includes anchored or quantum-blocked
    /// requests; false means the batch is stale, duplicated, foreign or invalid.
    /// All responses sharing an actuator are combined before its capacity is read.
    /// </summary>
    public bool Apply(IReadOnlyList<Relation> requests, out Relation receipt)
    {
        var before = CurrentPublication;
        if (requests.Count == 0 || RepeatedRequests(requests) ||
            !CurrentStateMatches() || requests.Any(r => !CurrentRequest(r)))
        {
            receipt = ReadWitness.Create(new Carrier(), G(requests.ToArray()), before, new Placeholder(),
                G(requests.ToArray()), G());
            receipts.Add(receipt);
            return false;
        }

        var contributions = new List<(Relation Request, Relation Route, Carrier Actuator, Fraction Value, Relation Witness)>();
        var unrouted = new List<IElement>();
        foreach (var request in requests)
        {
            var r = F(request);
            var selectedRoutes = routes.Where(candidate => ReferenceEquals(F(candidate)[0], r[0])).ToArray();
            if (selectedRoutes.Length==0) { unrouted.Add(request); continue; }
            foreach(var route in selectedRoutes)
            {
            var f = F(route); var map = (Relation)f[1];
            TimeStepSequence.TryReadSpan((Relation)map.Left, out var a, out var b);
            TimeStepSequence.TryReadSpan((Relation)map.Right, out var c, out var d);
            var delta = Fraction.Read(r[3]);
            var mapped = delta * new Fraction(d.Ticks - c.Ticks, b.Ticks - a.Ticks);
            var admitted = mapped * Fraction.Read(f[2]);
            var witness = ReadWitness.Create(new Carrier(), request, route, admitted.Encode(c.Carrier),
                (mapped - admitted).Encode(c.Carrier), G(map, f[2]));
            contributions.Add((request, route, c.Carrier, admitted, witness));
            }
        }

        var allocations = new List<Allocation>();
        foreach (var group in contributions.GroupBy(c => c.Actuator))
        {
            var capacity = (Relation)F(group.First().Route)[3];
            var limits = Limits(capacity); var actuator = group.Key;
            var wanted = group.Aggregate(Fraction.Zero, (sum, c) => sum + c.Value);
            var minimum = new Fraction((BigInteger)limits.Min - actuator.Value);
            var maximum = new Fraction((BigInteger)limits.Max - actuator.Value);
            var bounded = Fraction.Clamp(wanted, minimum, maximum);
            var stepLimited = Fraction.Clamp(bounded, new Fraction(-limits.Step), new Fraction(limits.Step));
            var whole = stepLimited.Truncate(limits.Quantum);
            var accepted = new Fraction(whole);
            allocations.Add(new Allocation(actuator, capacity, G(group.Select(c => (IElement)c.Witness).ToArray()),
                wanted, whole, wanted - bounded, bounded - stepLimited, stepLimited - accepted, (long)actuator.Value));
        }

        // A private native branch tests the declared action before the live
        // actuator is changed. It is a preview witness, not a rebuilt live world.
        var preview = Preview(allocations, out var admissible);
        Relation material = G();
        if (admissible)
        {
            var arrivals = allocations.Where(a => a.Accepted != 0)
                .Select(a => (IElement)new CarrierLocation(a.Actuator, a.Accepted)).ToArray();
            if (arrivals.Length > 0) material = Carrier.ReceiveTogether(G(arrivals));
            var application = MachineRunner.Run(Definition);
            if (!PublishOutputs(application))
                throw new InvalidOperationException("A validated local candidate changed during sequential application.");
            lastApplication = application;
        }

        var outcomes = new List<IElement>();
        foreach (var a in allocations)
        {
            var actual = new Fraction((BigInteger)a.Actuator.Value - a.Before);
            // Five values in actuator units: requested sum, actual movement,
            // rejected by bounds, deferred by step capacity, unrepresented quantum.
            // Candidate rejection has its own sixth remainder, never hidden loss.
            var candidateBlocked = admissible ? Fraction.Zero : new Fraction(a.Accepted);
            var result = G(a.Wanted.Encode(a.Actuator), actual.Encode(a.Actuator), a.Bounds.Encode(a.Actuator),
                a.Step.Encode(a.Actuator), a.Quantum.Encode(a.Actuator), candidateBlocked.Encode(a.Actuator));
            outcomes.Add(ReadWitness.Create(new Carrier(), a.Contributions, a.Capacity, result,
                new CarrierLocation(a.Actuator, a.Actuator.Tension),
                G(new CarrierLocation(a.Actuator, a.Before), new CarrierLocation(a.Actuator, (long)a.Actuator.Value), material)));
        }
        clock.Add(1);
        parameterSamples = CaptureParameters();
        CurrentPublication = MakePublications();
        receipt = ReadWitness.Create(new Carrier(), G(requests.ToArray()), before,
            G(CurrentPublication, G(outcomes.ToArray())), G(G(unrouted.ToArray()), admissible ? G() : preview),
            G(G(contributions.Select(c => (IElement)c.Witness).ToArray()), preview, material));
        receipts.Add(receipt);
        steps.Add(TimeStepSequence.Step(new Carrier(), clock, checked((long)clock.Value - 1), (long)clock.Value,
            before, receipt, CurrentPublication, F(receipt)[3]));
        Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
        return true;
    }

    // Result tuples are diagnostic reads, subordinate to retained native witnesses.
    public static IReadOnlyList<ActuatorOutcome> Outcomes(Relation receipt)
    {
        var fields = F(receipt);
        if (fields[2] is not Relation resultGroup || !resultGroup.TryReadOrderedGroup(out var result)) return [];
        if (result.Length != 2) return [];
        return F(result[1]).Cast<Relation>().Select(w =>
        {
            var f = F(w); var n = F(f[2]);
            return new ActuatorOutcome(CapacityAxis((Relation)f[1]), n[0], n[1], n[2], n[3], n[4], n[5]);
        }).ToArray();
    }
    public sealed record ActuatorOutcome(Carrier Actuator, IElement Requested, IElement Accepted,
        IElement BoundsBlocked, IElement CapacityDeferred, IElement QuantumRemainder, IElement CandidateBlocked);

    private Relation Preview(List<Allocation> allocations, out bool admissible)
    {
        if (allocations.All(a => a.Accepted == 0)) { admissible = true; return G(lastApplication); }
        var source = G(Definition, G(allocations.Select(a => (IElement)a.Actuator).ToArray()));
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(source))[0];
        var clone = F(restored); var actuators = F(clone[1]).Cast<Carrier>().ToArray();
        var arrivals = allocations.Select((a, i) => (IElement)new CarrierLocation(actuators[i], a.Accepted)).ToArray();
        var transfer = Carrier.ReceiveTogether(G(arrivals));
        var applied = MachineRunner.Run((Relation)clone[0]);
        var whole = WholeOutputs(applied, out var output) && output.Length == points.Count;
        var publication = whole ? PublicationProposal(output, out _) : G(applied);
        var publicationFits = whole && F(F(publication)[3]).Length == 0;
        admissible = whole && publicationFits &&
            actuators.All(a => a.Tension == 0) &&
            actuators.Select((a, i) => a.Value == (ulong)checked(allocations[i].Before + allocations[i].Accepted)).All(x => x);
        return ReadWitness.Create(new Carrier(), source, G(allocations.Select(a => (IElement)a.Capacity).ToArray()),
            applied, admissible ? G() : G(applied, publication), G(restored, transfer, publication));
    }

    private bool CurrentRequest(Relation request)
    {
        try
        {
            var r = F(request);
            if (r.Length != 5 || r[0] is not Relation port || !ports.Any(p => ReferenceEquals(p, port)) ||
                !ReferenceEquals(Publish(port), r[1]) || r[4] is not Relation linkSurface ||
                !ProjectionResidue.TryRead(r[3], out var requestBasis, out _, out _) ||
                !requestBasis.SameIdentityAs(PortAxis(port))) return false;
            var link = NegotiationLink.Restore(linkSurface);
            var left = ReferenceEquals(link.Left, port);
            if (!left && !ReferenceEquals(link.Right, port)) return false;
            var plan = left ? link.Plan((Relation)r[1], (Relation)r[2]) : link.Plan((Relation)r[2], (Relation)r[1]);
            return Fraction.Read(r[3]) == Fraction.Read(F(NegotiationLink.Requests(plan)[left ? 0 : 1])[3]);
        }
        catch (ArgumentException) { return false; }
        catch (InvalidCastException) { return false; }
        catch (IndexOutOfRangeException) { return false; }
        catch (DivideByZeroException) { return false; }
    }

    private static bool RepeatedRequests(IReadOnlyList<Relation> requests)
    {
        var seen = new List<IElement[]>();
        foreach (var request in requests)
        {
            if (!request.TryReadOrderedGroup(out var f) || f.Length != 5) return true;
            // A newly wrapped request is still the same participation if its
            // link and two observation events are the same actual participants.
            if (seen.Any(p => ReferenceEquals(p[0], f[0]) && ReferenceEquals(p[1], f[1]) &&
                ReferenceEquals(p[2], f[2]) && ReferenceEquals(p[4], f[4]))) return true;
            seen.Add(f);
        }
        return false;
    }

    private bool CurrentStateMatches()
    {
        if (!CorrespondenceMatches()) return false;
        if (routes.Select(r => CapacityAxis((Relation)F(r)[3])).Distinct().Any(a => a.Tension != 0)) return false;
        foreach (var sample in F(parameterSamples).Cast<Relation>())
        {
            var a = (Carrier)sample.Left; var values = F(sample.Right);
            if ((long)a.Value != ((CarrierLocation)values[0]).Ticks || a.Tension != ((CarrierLocation)values[1]).Ticks) return false;
        }
        var current = MachineRunner.Run(Definition);
        return WholeOutputs(current, out var valuesNow) && valuesNow.Length == points.Count &&
            valuesNow.Select((p, i) => SameCoordinates(p, points[i])).All(x => x);
    }

    private bool PublishOutputs(Relation application)
    {
        if (!CorrespondenceMatches() || !WholeOutputs(application, out var output) || output.Length != points.Count) return false;
        PublicationProposal(output, out var publicationFits);
        if (!publicationFits) return false;
        var old = points.SelectMany(F).Cast<CarrierLocation>().ToArray();
        var next = output.SelectMany(F).ToArray();
        var source = new List<IElement>(); var target = new List<IElement>(); var incoming = new List<IElement>();
        for (var i = 0; i < old.Length; i++)
        {
            ProjectionResidue.TryRead(next[i], out var axis, out var n, out _);
            if (!axis.SameIdentityAs(old[i].Carrier)) return false;
            var baseTick = checked(old[i].Ticks - 1);
            source.Add(Relation.Segment(axis, 0, 1));
            target.Add(new Relation(Relation.Point(axis, baseTick), new Relation(old[i], axis)));
            incoming.Add(new CarrierLocation(axis, checked((long)n - baseTick)));
        }
        return new Relation(Tree(source), Tree(target)).TrySetStructureFrom(Tree(incoming));
    }

    // Whole before/after coordinates do not guarantee that the selected one-tick
    // publication route has a representable incoming displacement. Check the
    // entire grouped route BEFORE live actuator admission; an unsupported jump
    // keeps both endpoints as residue instead of overflowing after mutation.
    private Relation PublicationProposal(Relation[] output, out bool supported)
    {
        var previous = points.SelectMany(F).Cast<CarrierLocation>().ToArray();
        var next = output.SelectMany(F).ToArray();
        var projected = new List<IElement>(); var blocked = new List<IElement>(); var frames = new List<IElement>();
        for (var i = 0; i < previous.Length; i++)
        {
            var old = previous[i];
            ProjectionResidue.TryRead(next[i], out _, out var n, out _);
            var low = (BigInteger)old.Ticks - 1;
            var incoming = n - low;
            var endpoints = new Relation(new CarrierLocation(old.Carrier, old.Ticks), next[i]);
            if (low < long.MinValue || low > long.MaxValue || incoming < long.MinValue || incoming > long.MaxValue)
            {
                projected.Add(new Placeholder()); blocked.Add(endpoints); frames.Add(endpoints);
                continue;
            }
            frames.Add(Relation.Segment(old.Carrier, (long)low, old.Ticks));
            projected.Add(new CarrierLocation(old.Carrier, (long)incoming));
        }
        supported = blocked.Count == 0;
        return ReadWitness.Create(new Carrier(), G(CapturePoints(), G(output)), G(frames.ToArray()),
            G(projected.ToArray()), G(blocked.ToArray()), G(ports.ToArray()));
    }

    private Relation MakePublications() => G(ports.Select(port =>
    {
        var f = F(port); var site = (CarrierLocation)f[0];
        var eventSite = new CarrierLocation(clock, (long)clock.Value);
        var sample = new CarrierLocation(site.Carrier, site.Ticks);
        // Public read provenance is deliberately opaque. The private application
        // remains in Surface; this witness does not advertise a concept or truth rank.
        var interpretation = ReadWitness.Create(new Carrier(), f[3], f[1], sample, G(), G(eventSite, f[3]));
        return (IElement)G(port, eventSite, sample, interpretation);
    }).ToArray());

    private Relation CapturePoints() => G(points.Select(p => (IElement)Relation.OrderedGroup((Carrier)p.Right,
        F(p).Cast<CarrierLocation>().Select(c => (IElement)new CarrierLocation(c.Carrier, c.Ticks)).ToArray())).ToArray());
    private Relation CaptureParameters() => G(routes.Select(r => CapacityAxis((Relation)F(r)[3])).Distinct()
        .Select(a => (IElement)new Relation(a, G(new CarrierLocation(a, (long)a.Value), new CarrierLocation(a, a.Tension)))).ToArray());
    private Relation MakeCorrespondence()
    {
        var producerPorts = F(F(Definition)[2]);
        return producerPorts.Length == 1 ? G(new Relation(producerPorts[0], G(points.ToArray())))
            : G(producerPorts.Zip(points).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
    }
    private bool CorrespondenceMatches()
    {
        var producerPorts = F(F(Definition)[2]); var bindings = F(correspondence);
        if (producerPorts.Length != bindings.Length) return false;
        for (var i = 0; i < bindings.Length; i++)
        {
            if (bindings[i] is not Relation binding || !ReferenceEquals(binding.Left, producerPorts[i])) return false;
            if (producerPorts.Length == 1)
            {
                var selected = F(binding.Right);
                if (selected.Length != points.Count || selected.Where((point, j) => !ReferenceEquals(point, points[j])).Any()) return false;
            }
            else if (bindings.Length != points.Count || !ReferenceEquals(binding.Right, points[i])) return false;
        }
        return true;
    }
    private static bool WholeOutputs(Relation application, out Relation[] values)
    {
        var f = F(application); values = [];
        if (f.Length != 6 || F(f[4]).Length != 0) return false;
        var outputs = F(f[2]);
        if (outputs.Length == 0 || outputs.Any(p => !MachineRunner.TryNumbers(p, out var coordinates) ||
                coordinates.Any(c => c.D != 1 || c.N < long.MinValue + 1 || c.N > long.MaxValue))) return false;
        values = outputs.Cast<Relation>().ToArray(); return true;
    }
    private static bool SameCoordinates(Relation a, Relation b) => F(a).Zip(F(b)).All(pair =>
        ProjectionResidue.TryRead(pair.First, out var ax, out var an, out var ad) &&
        ProjectionResidue.TryRead(pair.Second, out var bx, out var bn, out var bd) && ax.SameIdentityAs(bx) && an * bd == bn * ad);
    internal static Carrier PortAxis(Relation port) => ((CarrierLocation)F(port)[0]).Carrier;
    private static Carrier CapacityAxis(Relation capacity)
    { TimeStepSequence.TryReadSpan((Relation)F(capacity)[0], out var a, out _); return a.Carrier; }
    private static (long Min, long Max, long Step, long Quantum) Limits(Relation capacity)
    {
        var c = F(capacity);
        TimeStepSequence.TryReadSpan((Relation)c[0], out var lo, out var hi);
        TimeStepSequence.TryReadSpan((Relation)c[1], out _, out var step);
        TimeStepSequence.TryReadSpan((Relation)c[2], out _, out var quantum);
        return (lo.Ticks, hi.Ticks, step.Ticks, quantum.Ticks);
    }
    private static IElement Tree(IReadOnlyList<IElement> values) => values.Count == 1 ? values[0]
        : new Relation(values[0], Tree(values.Skip(1).ToArray()));
    private sealed record Allocation(Carrier Actuator, Relation Capacity, Relation Contributions, Fraction Wanted,
        long Accepted, Fraction Bounds, Fraction Step, Fraction Quantum, long Before);
}

/// <summary>A selected affine agreement between two neutral scalar ports.</summary>
public sealed class NegotiationLink
{
    // G(left port, right port, left-to-right calibration). Offset is in right ticks.
    public Relation Surface { get; }
    public Relation Left => (Relation)F(Surface)[0];
    public Relation Right => (Relation)F(Surface)[1];
    public NegotiationLink(Relation leftPort, Relation rightPort, long offset = 0, long mapN = 1, long mapD = 1)
    {
        if (mapD <= 0 || mapN == 0) throw new ArgumentException("A reversible nonzero affine correspondence is required.");
        Surface = G(leftPort, rightPort, new Relation(Relation.Segment(NegotiatingMachine.PortAxis(leftPort), 0, mapD),
            Relation.Segment(NegotiatingMachine.PortAxis(rightPort), offset, checked(offset + mapN))));
    }
    private NegotiationLink(Relation surface)
    {
        var f = F(surface);
        if (f.Length != 3 || f[0] is not Relation left || f[1] is not Relation right ||
            f[2] is not Relation map || map.Left is not Relation source || map.Right is not Relation target ||
            !TimeStepSequence.TryReadSpan(source, out var a, out var b) ||
            !TimeStepSequence.TryReadSpan(target, out var c, out var d) ||
            a.Ticks == b.Ticks || c.Ticks == d.Ticks ||
            !a.Carrier.SameIdentityAs(NegotiatingMachine.PortAxis(left)) ||
            !c.Carrier.SameIdentityAs(NegotiatingMachine.PortAxis(right)))
            throw new ArgumentException("The retained link needs actual port bases and reversible calibration.", nameof(surface));
        Surface = surface;
    }
    public static NegotiationLink Restore(Relation surface) => new(surface);

    // Plan = ReadWitness(G(left observation,right observation), link,
    // G(left request,right request), signed residual in right basis, calibration).
    // Request = G(recipient port, own observation, peer observation, delta, link).
    public Relation Plan(Relation leftObservation, Relation rightObservation)
    {
        var a = F(leftObservation); var b = F(rightObservation);
        if (!ValidObservation(a, Left) || !ValidObservation(b, Right))
            throw new ArgumentException("Observations must come from the selected actual ports.");
        var map = (Relation)F(Surface)[2];
        TimeStepSequence.TryReadSpan((Relation)map.Left, out var x0, out var x1);
        TimeStepSequence.TryReadSpan((Relation)map.Right, out var y0, out var y1);
        var scale = new Fraction(y1.Ticks - y0.Ticks, x1.Ticks - x0.Ticks);
        var projected = new Fraction(y0.Ticks) + (Fraction.Read(a[2]) - new Fraction(x0.Ticks)) * scale;
        var residual = Fraction.Read(b[2]) - projected;
        var left = G(Left, leftObservation, rightObservation, (residual / scale).Encode(x0.Carrier), Surface);
        var right = G(Right, rightObservation, leftObservation, (-residual).Encode(y0.Carrier), Surface);
        return ReadWitness.Create(new Carrier(), G(leftObservation, rightObservation), Surface, G(left, right),
            residual.Encode(y0.Carrier), map);
    }
    public static Relation[] Requests(Relation plan) => F(F(plan)[2]).Cast<Relation>().ToArray();
    public static IElement Residual(Relation plan) => F(plan)[3];
    private static bool ValidObservation(IElement[] observation, Relation port)
    {
        var p = F(port);
        return p.Length == 4 && observation.Length == 4 && ReferenceEquals(observation[0], port) &&
            observation[1] is CarrierLocation time && p[2] is Carrier clock &&
            time.Carrier.SameIdentityAs(clock) && time.Ticks >= 0 &&
            ProjectionResidue.TryRead(observation[2], out var axis, out _, out _) &&
            axis.SameIdentityAs(NegotiatingMachine.PortAxis(port));
    }
}

/// <summary>
/// A bounded caller-owned schedule: capture all publications, plan all links,
/// then let each local machine apply its batch. This chooses timing, never a
/// geometric answer. Saved surfaces are sufficient to resume another round.
/// </summary>
public sealed class NegotiationWorld
{
    public IReadOnlyList<NegotiatingMachine> Machines { get; }
    public IReadOnlyList<NegotiationLink> Links { get; }
    public Relation Timeline { get; private set; }
    public Relation Surface => G(G(Machines.Select(m => (IElement)m.Surface).ToArray()),
        G(Links.Select(l => (IElement)l.Surface).ToArray()), clock, Timeline);
    private readonly Carrier clock;
    private readonly IElement initial;
    private readonly List<Relation> steps = [];
    public NegotiationWorld(IReadOnlyList<NegotiatingMachine> machines, IReadOnlyList<NegotiationLink> links)
    {
        Machines = machines.ToArray(); Links = links.ToArray(); clock = new Carrier();
        foreach (var port in links.SelectMany(l => new[] { l.Left, l.Right })) _ = Owner(port);
        initial = G(Machines.Select(m => (IElement)m.CurrentPublication).ToArray());
        Timeline = TimeStepSequence.Create(new Carrier(), clock, initial);
    }
    private NegotiationWorld(Relation surface)
    {
        var f = F(surface);
        Machines = F(f[0]).Cast<Relation>().Select(NegotiatingMachine.Restore).ToArray();
        Links = F(f[1]).Cast<Relation>().Select(NegotiationLink.Restore).ToArray();
        clock = (Carrier)f[2]; Timeline = (Relation)f[3];
        if (!TimeStepSequence.TryRead(Timeline, out _, out var start, out var retained))
            throw new ArgumentException("The retained world timeline is unavailable.", nameof(surface));
        initial = start; steps.AddRange(retained);
    }
    public static NegotiationWorld Restore(Relation surface) => new(surface);
    public Relation[] Compare() => Links.Select(link => link.Plan(Owner(link.Left).Publish(link.Left), Owner(link.Right).Publish(link.Right))).ToArray();
    public Relation Step()
    {
        var before = G(Machines.Select(m => (IElement)m.CurrentPublication).ToArray());
        var plans = Compare(); var requests = plans.SelectMany(NegotiationLink.Requests).ToArray();
        var outcomes = new List<IElement>();
        foreach (var machine in Machines)
        {
            var local = requests.Where(r => machine.Ports.Any(p => ReferenceEquals(p, F(r)[0]))).ToArray();
            if (local.Length == 0) continue;
            machine.Apply(local, out var receipt); outcomes.Add(receipt);
        }
        var after = G(Machines.Select(m => (IElement)m.CurrentPublication).ToArray());
        var remaining = G(Compare().Select(p => NegotiationLink.Residual(p)).ToArray());
        var transition = G(G(plans), G(outcomes.ToArray()));
        var step = TimeStepSequence.Step(new Carrier(), clock, (long)clock.Value, checked((long)clock.Value + 1),
            before, transition, after, remaining);
        clock.Add(1); steps.Add(step);
        Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
        return step;
    }
    private NegotiatingMachine Owner(Relation port) => Machines.Single(m => m.Ports.Any(p => ReferenceEquals(p, port)));
}

// Exact derived arithmetic only. Every retained amount is encoded back onto its
// explicit carrier; the operational maps/fractions remain native relations.
internal readonly record struct Fraction
{
    public BigInteger N { get; }
    public BigInteger D { get; }
    public Fraction(BigInteger n) : this(n, 1) { }
    public Fraction(BigInteger n, BigInteger d)
    {
        if (d == 0) throw new DivideByZeroException();
        if (d < 0) { n = -n; d = -d; }
        var gcd = BigInteger.GreatestCommonDivisor(n, d); N = n / gcd; D = d / gcd;
    }
    public static Fraction Zero => new(0);
    public static Fraction Read(IElement value) => ProjectionResidue.TryRead(value, out _, out var n, out var d)
        ? new(n, d) : throw new ArgumentException("A grounded exact scalar is required.");
    public IElement Encode(Carrier axis) => ProjectionResidue.Create(axis, checked((long)N), checked((long)D));
    public long Truncate(long quantum) => checked((long)((N / (D * quantum)) * quantum));
    public static Fraction Clamp(Fraction x, Fraction lo, Fraction hi) => x < lo ? lo : x > hi ? hi : x;
    public static Fraction operator +(Fraction a, Fraction b) => new(a.N * b.D + b.N * a.D, a.D * b.D);
    public static Fraction operator -(Fraction a, Fraction b) => a + -b;
    public static Fraction operator -(Fraction a) => new(-a.N, a.D);
    public static Fraction operator *(Fraction a, Fraction b) => new(a.N * b.N, a.D * b.D);
    public static Fraction operator /(Fraction a, Fraction b) => new(a.N * b.D, a.D * b.N);
    public static bool operator <(Fraction a, Fraction b) => a.N * b.D < b.N * a.D;
    public static bool operator >(Fraction a, Fraction b) => a.N * b.D > b.N * a.D;
}
