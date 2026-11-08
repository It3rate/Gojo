using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Reads a response common to supported surviving motion candidates,
/// or admits one explicitly chosen continuation without declaring it true.
/// Candidate membership, captured evidence, response offers and choice remain
/// native relations. A proposal is an action scope, not a refined observation.
/// This reader does not allocate trials, spend allowances, or execute effects.</summary>
public sealed class CandidateResponseRead
{
    // surface = Closed(identity, motion-memory surface, sensor, owner,
    //   comparison basis, comparison domain, agreement-selector, candidate domain)
    // offers = Closed(candidate -> response publication, ...)
    // observation = (surface -> captured memory archive) -> Closed(
    //   offers, candidate-memory read, eligibility reads, agreement folds,
    //   candidate-choice proposal, common-response publication)
    // selection = (observation -> FiniteChoice receipt) ->
    //   Closed(current memory archive, rechecked observation, response, remainder)
    // All candidate identities remain in memory. Selecting one possible action
    // does not eliminate its alternatives or add a successful motion trial.
    private readonly IElement identity;
    private readonly Relation memorySurface;
    private readonly IElement sensor;
    private readonly IElement owner;
    private readonly Carrier basis;
    private readonly Relation domain;
    private readonly Relation candidates;
    private readonly IElement[] signs;
    private readonly ExtentResponseArbiter agreement;
    private readonly Curve.Frame frame;

    public Relation Surface { get; }
    public sealed record Observation(Relation Packet, byte Mask, bool Shared,
        Relation Proposal, int PossibleCount, int EligibleCount, Relation Witness);
    public sealed record Selection(Relation Packet, byte Mask, bool Resolved,
        bool Provisional, Relation? SelectedCandidate, Relation Witness);

    private CandidateResponseRead(Relation surface)
    {
        if (!Closed(surface, out var fields) || fields is not
            [Placeholder id, Relation memory, Placeholder inputSensor, Placeholder inputOwner,
             Carrier comparison, Relation comparisonDomain, Relation selectorSurface, Relation candidateDomain] ||
            !Closed(comparisonDomain, out var comparisons) || comparisons.Length != 3 ||
            comparisons.Where((s, i) => s is not CarrierLocation at ||
                !ReferenceEquals(at.Carrier, comparison) || at.Ticks != i - 1).Any() ||
            memory.Right is not Relation memoryBody || !Closed(memoryBody, out var memoryFields) ||
            memoryFields is not [Relation _, Relation _, Placeholder _, Relation words] ||
            !ReferenceEquals(words, candidateDomain) || !Closed(candidateDomain, out var members) ||
            members.Length is < 1 or > 64 || members.Any(c => c is not Relation) ||
            !Closed(selectorSurface, out var selector) || selector.Length != 8 ||
            !Same(selector[1], inputSensor) || !Same(selector[2], inputOwner) ||
            !ReferenceEquals(selector[3], comparison) || !ReferenceEquals(selector[4], comparisonDomain))
            throw new ArgumentException("Expected the exact motion-candidate response surface.", nameof(surface));
        if (memory.Left is not Relation frameSurface)
            throw new ArgumentException("Expected the connected motion frame.", nameof(surface));
        frame = Curve.Frame.FromSurface(frameSurface);
        agreement = ExtentResponseArbiter.FromSurface(selectorSurface);
        if (!AgreementLaw(agreement.Law))
            throw new ArgumentException("The shared-response route must contain the agreement law, not a preference law.", nameof(surface));
        Surface = surface; identity = id; memorySurface = memory; sensor = inputSensor;
        owner = inputOwner; basis = comparison; domain = comparisonDomain;
        candidates = candidateDomain; signs = comparisons;
    }

    public static CandidateResponseRead Create(CoupledExtentMachine receiver, ObservedMotionCandidates memory)
    {
        if (!ReferenceEquals(receiver.ExtentPort, memory.ExtentPort))
            throw new ArgumentException("The candidate memory must concern this receiver's exposed extent port.", nameof(memory));
        var words = (Relation)Entries((Relation)memory.Surface.Right)[3];
        return new(S(new Placeholder(), memory.Surface, receiver.Sensor, receiver.Owner,
            receiver.ComparisonBasis, receiver.ComparisonDomain,
            ExtentResponseArbiter.Create(receiver).Surface, words));
    }

    public static CandidateResponseRead FromSurface(Relation surface) => new(surface);

    /// <summary>Offers concern one supplied observation context. The caller
    /// constructs each candidate's response through its claim/read; this reader
    /// checks membership and response scope, not that the premises describe the
    /// wider world. Unknown or missing offers remain visible. A common response
    /// requires informative support for every surviving motion candidate.</summary>
    public Observation Observe(ObservedMotionCandidates memory, Relation offers,
        bool compiled = true, IElement? eventIdentity = null)
    {
        var occurrence = eventIdentity ?? new Placeholder();
        if (occurrence is not Placeholder) throw new ArgumentException("Expected an opaque response event.", nameof(eventIdentity));
        var archive = memory.ArchiveSurface(); var read = memory.Read();
        var scopeValid = ReferenceEquals(memory.Surface, memorySurface) && Closed(offers, out _);
        StructuralConstraint.TryReadScope(offers, out var entries, out _, out _);
        entries ??= [];
        var rows = entries.OfType<Relation>().ToArray();
        scopeValid &= rows.Length == entries.Length && rows.All(r => r.Right is Relation &&
            memory.Candidates.Any(c => ReferenceEquals(c, r.Left))) &&
            rows.Select(r => r.Left).Distinct(ReferenceEqualityComparer.Instance).Count() == rows.Length;
        var eligible = new List<IElement>(); var evidence = new List<IElement>(); var folds = new List<IElement>();
        var common = Packet(0, occurrence); var first = true;
        var complete = scopeValid && read.Possible.Length > 0;
        foreach (var candidate in read.Possible)
        {
            var matching = rows.Where(r => ReferenceEquals(r.Left, candidate)).ToArray();
            var offered = matching.Length == 1 && matching[0].Right is Relation p ? p : Packet(0, occurrence);
            var responseMask = scopeValid && frame.Vector(candidate, out _) ? ResponseMask(offered) : (byte)0;
            var singleton = responseMask is 1 or 2 or 4;
            if (singleton) eligible.Add(candidate);
            var support = read.Candidates.Single(r => ReferenceEquals(r.Candidate, candidate));
            var admissibility = singleton ? S(signs[Index(responseMask)]) : StructuralConstraint.Open(offered);
            evidence.Add(new Relation(candidate, S(support.Evidence, matching.Length == 1 ? matching[0] : StructuralConstraint.Open(candidate), admissibility)));
            complete &= singleton && support.Supported;
            var input = singleton && support.Supported ? offered : Packet(0, occurrence);
            var fold = agreement.Evaluate(first ? input : common, input, compiled, occurrence);
            folds.Add(fold.Witness); common = fold.Packet; first = false;
        }
        var mask = complete ? ResponseMask(common) : (byte)0;
        var shared = mask is 1 or 2 or 4;
        var packet = Packet(shared ? mask : (byte)0, occurrence);
        var choiceSupport = scopeValid ? S(eligible.ToArray()) : StructuralConstraint.Open(eligible.ToArray());
        var proposal = QualitativePublication.Create(identity, memorySurface, occurrence, memorySurface,
            candidates, choiceSupport, new Relation(archive, offers));
        var witness = new Relation(new Relation(Surface, archive),
            S(offers, read.Witness, S(evidence.ToArray()), S(folds.ToArray()), proposal, packet));
        return new(packet, shared ? mask : (byte)0, shared, proposal, read.Possible.Length, eligible.Count, witness);
    }

    /// <summary>Uses a caller-supplied FiniteChoice operation on this observation's
    /// proposal. The surrounding machine must supply and account for its trial or
    /// provisional-action allowance. Rechecking refuses changed candidate history,
    /// rejected candidates and altered proposals. Dispatch either the shared
    /// response or this selected response; both retain the observation event so
    /// delivery retries cannot become additional physical steps.</summary>
    public Selection Select(ObservedMotionCandidates memory, Relation observation,
        Relation choiceReceipt, bool compiled = true)
    {
        var occurrence = (IElement)new Placeholder();
        Relation? selectedCandidate = null; Relation? rechecked = null; var mask = (byte)0;
        var valid = false;
        if (TryObservation(observation, out var archive, out var offers, out var proposal, out var common) &&
            QualitativePublication.TryRead(common, out _, out _, out var observedEvent, out _, out _, out _, out _))
        {
            occurrence = observedEvent;
            var current = memory.ArchiveSurface();
            if (ReferenceEquals(memory.Surface, memorySurface) && ReferenceEquals(archive.Left, memorySurface) &&
                ReferenceEquals(archive.Right, current.Right))
            {
                var again = Observe(memory, offers, compiled, occurrence); rechecked = again.Witness;
                if (!again.Shared && SameProposal(proposal, again.Proposal, archive, offers) &&
                    FiniteChoice.TryReadSelected(choiceReceipt, out _) &&
                    FiniteChoice.TryReadSelection(choiceReceipt, out var chosenProposal, out _, out var candidate,
                        out _, out _, out _, out _, out _) && ReferenceEquals(chosenProposal, proposal) &&
                    candidate is Relation word && memory.Read().Possible.Any(c => ReferenceEquals(c, word)))
                {
                    var rows = Entries(offers).Cast<Relation>().Where(r => ReferenceEquals(r.Left, word)).ToArray();
                    if (rows is [Relation { Right: Relation packet }])
                    { mask = ResponseMask(packet); valid = mask is 1 or 2 or 4; if (valid) selectedCandidate = word; }
                }
            }
        }
        var response = Packet(valid ? mask : (byte)0, occurrence);
        var witness = new Relation(new Relation(observation, choiceReceipt), S(memory.ArchiveSurface(),
            rechecked ?? StructuralConstraint.Open(observation), response,
            valid ? S() : StructuralConstraint.Open(choiceReceipt)));
        return new(response, valid ? mask : (byte)0, valid, valid, selectedCandidate, witness);
    }

    private bool TryObservation(Relation read, out Relation archive, out Relation offers,
        out Relation proposal, out Relation common)
    {
        archive = offers = proposal = common = null!;
        if (read.Left is not Relation { Left: var source, Right: Relation captured } ||
            !ReferenceEquals(source, Surface) || read.Right is not Relation body || !Closed(body, out var fields) ||
            fields is not [Relation input, Relation _, Relation _, Relation _, Relation choice, Relation response]) return false;
        archive = captured; offers = input; proposal = choice; common = response; return true;
    }

    private bool SameProposal(Relation supplied, Relation expected, Relation archive, Relation offers) =>
        QualitativePublication.TryRead(supplied, out var source, out var subject, out var occurrence,
            out var suppliedBasis, out var suppliedDomain, out var support, out var proof) &&
        QualitativePublication.TryRead(expected, out _, out _, out var expectedEvent, out _, out _, out var expectedSupport, out _) &&
        Same(source, identity) && ReferenceEquals(subject, memorySurface) && Same(occurrence, expectedEvent) &&
        ReferenceEquals(suppliedBasis, memorySurface) && ReferenceEquals(suppliedDomain, candidates) &&
        Closed(support, out var actual) && Closed(expectedSupport, out var desired) &&
        actual.Length == desired.Length && actual.Select((item, i) => ReferenceEquals(item, desired[i])).All(x => x) &&
        proof is Relation evidence && ReferenceEquals(evidence.Left, archive) && ReferenceEquals(evidence.Right, offers);

    private byte ResponseMask(Relation packet)
    {
        if (!QualitativePublication.TryRead(packet, out var source, out var subject, out _, out var packetBasis,
                out var packetDomain, out var support, out _) || !Same(source, sensor) || !Same(subject, owner) ||
            !ReferenceEquals(packetBasis, basis) || !ReferenceEquals(packetDomain, domain) ||
            !Closed(support, out var values) || values.Length != 1) return 0;
        var index = Array.FindIndex(signs, sign => Same(sign, values[0]));
        return index < 0 ? (byte)0 : (byte)(1 << index);
    }
    private Relation Packet(byte mask, IElement occurrence) => QualitativePublication.Create(sensor, owner,
        occurrence, basis, domain, mask == 0 ? StructuralConstraint.Open() : S(signs[Index(mask)]), identity);
    private static bool AgreementLaw(Relation law)
    {
        if (!FiniteLaw.TryRead(law, out var left, out var right, out var output, out var rows) ||
            !ReferenceEquals(left, right) || !Closed(left, out var inputs) || inputs.Length != 4 ||
            !Closed(output, out var outputs) || outputs.Length != 4 || rows.Length != 16) return false;
        for (var a = 0; a < 4; a++)
            for (var b = 0; b < 4; b++)
            {
                var matching = rows.Where(r => r.Left is Relation pair && Same(pair.Left, inputs[a]) && Same(pair.Right, inputs[b])).ToArray();
                if (matching is not [Relation { Right: Relation support }] || !Closed(support, out var values) ||
                    values.Length != 1 || !Same(values[0], outputs[a == b && a != 3 ? a : 3])) return false;
            }
        return true;
    }
    private static int Index(byte singleton) => singleton == 1 ? 0 : singleton == 2 ? 1 : 2;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation u && b is CarrierLocation v && u.Carrier.SameIdentityAs(v.Carrier) && u.SameTickRefAs(v);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
