using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Structures;

/// <summary>
/// A finite memory of opaque acquisitions and their ordinal comparisons.
/// Attention weights measure remembered-event participation, never brightness.
/// </summary>
public static class RelativeSignalSurface
{
    // Native ledger:
    // state = G(source, policy, fast bank, slow bank, latest witness)
    // policy = G(P(presence,1), S(order,-1,1), P(attention,initial),
    //            P(count,capacity), S(score,-16,16))
    // bank = G(S(attention,0,denominator)->S(attention,0,numerator),
    //          G(acquisition -> weight carrier))
    // bank read = witness(current, G(leak,vote equation,normalization),
    //   G(score,signed attention,total attention,count), residue,
    //   G(comparisons,captured weights,vote witness,normalization witness))
    // latest = witness(current,policy,G(fast read,slow read),losses,
    //                  G(update witnesses,loss witnesses))
    // Read before leak/admission. Retain only the latest diagnostic; it does
    // not link previous operational states into an unbounded history chain.

    public static Relation Create(Carrier root, IElement source, Carrier presenceAxis, Carrier orderAxis,
        int fastRetentionNumerator = 3, int fastRetentionDenominator = 4,
        int slowRetentionNumerator = 31, int slowRetentionDenominator = 32,
        int initialWeight = 64, int maxReferences = 64)
    {
        var attention = new Carrier(); var count = new Carrier(); var score = new Carrier();
        var policy = G(Relation.Point(presenceAxis, 1), Relation.Segment(orderAxis, -1, 1),
            Relation.Point(attention, initialWeight), Relation.Point(count, maxReferences), Relation.Segment(score, -16, 16));
        Relation Bank(int n, int d) => G(new Relation(Relation.Segment(attention, 0, d),
            Relation.Segment(attention, 0, n)), G());
        return State(root, source, policy, Bank(fastRetentionNumerator, fastRetentionDenominator),
            Bank(slowRetentionNumerator, slowRetentionDenominator), Open(source, policy));
    }

    public static Relation Observe(Carrier root, Relation state, Relation sample,
        Func<Relation, Relation, Relation> compare)
    {
        if (!TryReadState(state, out var source, out var policy, out var fast, out var slow, out var previous) ||
            !TryReadPolicy(policy, out var presenceAxis, out var orderAxis, out var attentionAxis, out var scoreAxis,
                out var initial, out var capacity)) return Open(state, sample);
        TryReadBank(fast, out var fastLaw, out var fastRefs); TryReadBank(slow, out var slowLaw, out var slowRefs);
        var countAxis = CountAxis(policy);
        var queries = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var updates = new List<IElement>(); var losses = new List<IElement>();

        bool valid = ValidSample(sample, source, presenceAxis, out var present);
        if (!valid) return Finish(fast, slow, Failed(fastLaw), Failed(slowLaw));
        if (TryReadLatest(previous, out var latestSample, out _, out _, out _, out _) && ReferenceEquals(sample, latestSample))
            return State(root, source, policy, fast, slow, previous);
        // Reusing any still-retained event is not fresh evidence. Forgotten
        // identities cannot be deduplicated without an additional history law.
        var duplicates = fastRefs.Concat(slowRefs).Where(reference => ReferenceEquals(reference.Left, sample)).ToArray();
        if (duplicates.Length != 0)
        {
            losses.Add(Open(sample, G(duplicates)));
            return Finish(fast, slow, Failed(fastLaw), Failed(slowLaw));
        }

        var fastRead = ReadBank(fastLaw, fastRefs);
        var slowRead = ReadBank(slowLaw, slowRefs);
        var nextFast = UpdateBank(fastLaw, fastRefs);
        var nextSlow = UpdateBank(slowLaw, slowRefs);
        return Finish(nextFast, nextSlow, fastRead, slowRead);

        Relation Failed(Relation law) => ReadWitness.Create(new Carrier(), sample, law,
            new Placeholder(), new Relation(sample, policy), G(G(), G(), new Placeholder(), new Placeholder()));
        Relation Finish(Relation a, Relation b, Relation aRead, Relation bRead)
        {
            var dropped = G(losses.ToArray());
            var latest = ReadWitness.Create(new Carrier(), sample, policy, G(aRead, bRead), dropped,
                G(G(updates.ToArray()), dropped));
            return State(root, source, policy, a, b, latest);
        }
        Relation Ask(Relation anchor)
        {
            if (!queries.TryGetValue(anchor, out var witness))
            {
                witness = compare(sample, anchor);
                queries.Add(anchor, witness);
            }
            return witness;
        }
        Relation ReadBank(Relation law, Relation[] references)
        {
            var comparisons = new List<IElement>(); var captured = new List<IElement>();
            var ports = new List<Relation>(); var inputs = new List<IElement>(); var bindings = new List<IElement>();
            var total = 0; var resolved = present && references.Length != 0;
            foreach (var reference in references)
            {
                TryReadReference(reference, out var anchor, out var weight);
                var amount = (int)weight.Value;
                var weightRead = new Relation(anchor, new CarrierLocation(weight, amount));
                captured.Add(weightRead);
                total += amount;
                if (!present) continue;
                var comparison = Ask(anchor); comparisons.Add(comparison);
                if (!SamplingSurface.TryReadComparison(comparison, sample, anchor, orderAxis, out var order))
                { resolved = false; continue; }
                var port = Equation.WeightedPort(orderAxis, 0, 1, 0, amount);
                var ordinal = new CarrierLocation(orderAxis, order);
                ports.Add(port); inputs.Add(ordinal);
                // Connect the supplier witness and captured weight to the
                // actual operand site consumed by the native weighted equation.
                bindings.Add(new Relation(comparison, new Relation(weightRead, new Relation(ordinal, port))));
            }
            var operands = G(comparisons.ToArray()); var weights = G(captured.ToArray());
            if (!resolved)
                return ReadWitness.Create(new Carrier(), sample, law, new Placeholder(), weights,
                    G(operands, weights, new Placeholder(), new Placeholder()));
            var equation = Equation.Sum(attentionAxis, ports.ToArray());
            var supplied = InputChain(inputs);
            var rawVote = Equation.PredictWitness(new Carrier(), equation, supplied, attentionAxis);
            var readableVote = ReadWitness.TryRead(rawVote, out _, out _, out var voted, out var voteResidue, out _);
            var vote = ReadWitness.Create(new Carrier(), supplied, equation, voted, voteResidue, G(rawVote, G(bindings.ToArray())));
            if (!readableVote || voted is not CarrierLocation signed ||
                !signed.Carrier.SameIdentityAs(attentionAxis) || signed.Ticks < -total || signed.Ticks > total)
                return ReadWitness.Create(new Carrier(), sample, equation, new Placeholder(), weights,
                    G(operands, weights, vote, new Placeholder()));
            var normalization = new Relation(Relation.Segment(attentionAxis, 0, total), Relation.Segment(scoreAxis, 0, 16));
            if (!normalization.TryProject(signed, scoreAxis, out var prediction) || prediction.Left is not CarrierLocation normalized)
                return ReadWitness.Create(new Carrier(), sample, normalization, new Placeholder(), prediction,
                    G(operands, weights, vote, new Placeholder()));
            var normWitness = ReadWitness.Create(new Carrier(), signed, normalization, normalized, prediction.Right, prediction);
            return ReadWitness.Create(new Carrier(), sample, G(law, equation, normalization),
                G(normalized, new CarrierLocation(attentionAxis, signed.Ticks), new CarrierLocation(attentionAxis, total),
                    new CarrierLocation(countAxis, references.Length)), prediction.Right, G(operands, weights, vote, normWitness));
        }
        Relation UpdateBank(Relation law, Relation[] references)
        {
            var retained = new List<IElement>();
            foreach (var reference in references)
            {
                TryReadReference(reference, out var anchor, out var weight);
                var before = new CarrierLocation(weight, (long)weight.Value);
                var supplied = new CarrierLocation(attentionAxis, before.Ticks);
                // Validated <=256 weights and coefficients keep this affine
                // read within int bounds even if the exact kernel uses wider scratch.
                law.TryProject(supplied, attentionAxis, out var prediction);
                var target = (CarrierLocation)prediction.Left;
                weight.Set(target.Ticks);
                var after = new CarrierLocation(weight, (long)weight.Value);
                var update = ReadWitness.Create(new Carrier(), new Relation(anchor, before), law, after, prediction.Right,
                    G(new Relation(before, supplied), prediction));
                updates.Add(update);
                if (weight.Value == 0) losses.Add(update);
                else retained.Add(reference);
            }
            if (present)
            {
                if (retained.Count == capacity)
                {
                    var dropped = (Relation)retained[0]; TryReadReference(dropped, out var anchor, out var weight);
                    var before = new CarrierLocation(weight, (long)weight.Value); weight.Set(0);
                    losses.Add(ReadWitness.Create(new Carrier(), anchor, Relation.Point(countAxis, capacity), new Placeholder(),
                        before, new Relation(before, new CarrierLocation(weight, 0))));
                    retained.RemoveAt(0);
                }
                var admitted = new Carrier(attentionAxis.UnitShape); admitted.Set(initial);
                retained.Add(new Relation(sample, admitted));
                updates.Add(ReadWitness.Create(new Carrier(), sample, Relation.Point(attentionAxis, initial),
                    new CarrierLocation(admitted, initial), G(), new Relation(Relation.Point(attentionAxis, initial), admitted)));
            }
            return G(law, G(retained.ToArray()));
        }
    }

    public static bool TryReadState(Relation state, out IElement source, out Relation policy,
        out Relation fastBank, out Relation slowBank, out Relation latest)
    {
        source = null!; policy = fastBank = slowBank = latest = null!;
        if (!state.TryReadOrderedGroup(out var fields) || fields.Length != 5 || fields[1] is not Relation p ||
            !TryReadPolicy(p, out var presence, out _, out var attention, out _, out var initial, out var capacity) ||
            fields[2] is not Relation fast || fields[3] is not Relation slow || fields[4] is not Relation last ||
            !ValidBank(fast) || !ValidBank(slow)) return false;
        source = fields[0]; policy = p; fastBank = fast; slowBank = slow; latest = last; return true;
        bool ValidBank(Relation bank)
        {
            if (!TryReadBank(bank, out var law, out var refs) || refs.Length > capacity ||
                !Span((Relation)law.Left, out var axis, out _, out _) || !axis.SameIdentityAs(attention)) return false;
            var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            foreach (var reference in refs)
                if (!TryReadReference(reference, out var sample, out var weight) || !seen.Add(sample) ||
                    weight.Value > (ulong)initial || !weight.UnitShape.CompatibleWith(attention.UnitShape) ||
                    !ValidSample(sample, fields[0], presence, out var exists) || !exists) return false;
            return true;
        }
    }

    public static bool TryReadPolicy(Relation policy, out Carrier presenceAxis, out Carrier orderAxis,
        out Carrier attentionAxis, out Carrier scoreAxis, out int initialWeight, out int maxReferences)
    {
        presenceAxis = orderAxis = attentionAxis = scoreAxis = null!; initialWeight = maxReferences = 0;
        if (!policy.TryReadOrderedGroup(out var fields) || fields.Length != 5 || !Point(fields[0], out var presence) ||
            presence.Carrier.IsUnknown || presence.Ticks != 1 || fields[1] is not Relation order ||
            !Span(order, out var orderBasis, out var low, out var high) || orderBasis.IsUnknown || low != -1 || high != 1 ||
            !Point(fields[2], out var initial) || initial.Carrier.IsUnknown || initial.Ticks is < 1 or > 256 ||
            !Point(fields[3], out var count) || count.Carrier.IsUnknown || count.Ticks is < 1 or > 128 ||
            fields[4] is not Relation score || !Span(score, out var scoreBasis, out low, out high) ||
            scoreBasis.IsUnknown || low != -16 || high != 16) return false;
        presenceAxis = presence.Carrier; orderAxis = orderBasis; attentionAxis = initial.Carrier; scoreAxis = scoreBasis;
        initialWeight = (int)initial.Ticks; maxReferences = (int)count.Ticks; return true;
    }

    public static bool TryReadBank(Relation bank, out Relation leakLaw, out Relation[] references)
    {
        leakLaw = null!; references = [];
        if (!bank.TryReadOrderedGroup(out var fields) || fields.Length != 2 || fields[0] is not Relation law ||
            law.Left is not Relation input || law.Right is not Relation output ||
            !Span(input, out var a, out var origin, out var denominator) || a.IsUnknown || origin != 0 || denominator is < 1 or > 256 ||
            !Span(output, out var b, out origin, out var numerator) || !a.SameIdentityAs(b) || origin != 0 ||
            numerator < 0 || numerator > denominator || fields[1] is not Relation group ||
            !group.TryReadOrderedGroup(out var refs) || refs.Length > 128 ||
            refs.Any(reference => reference is not Relation r || !TryReadReference(r, out _, out _))) return false;
        leakLaw = law; references = refs.Cast<Relation>().ToArray(); return true;
    }

    public static bool TryReadReference(Relation reference, out Relation sample, out Carrier weight)
    {
        if (reference.Left is Relation acquisition && SamplingSurface.TryReadSample(acquisition, out var presence, out _) &&
            presence.Ticks == 1 && reference.Right is Carrier attention && !attention.IsUnknown &&
            attention.Value is > 0 and <= 256 && attention.Tension == 0)
        { sample = acquisition; weight = attention; return true; }
        sample = null!; weight = null!; return false;
    }

    public static bool TryReadLatest(Relation latest, out Relation sample, out Relation fastRead, out Relation slowRead,
        out Relation updates, out Relation losses)
    {
        sample = fastRead = slowRead = updates = losses = null!;
        if (!ReadWitness.TryRead(latest, out var selected, out _, out var result, out _, out var provenance) ||
            selected is not Relation acquisition || result is not Relation group || !group.TryReadOrderedGroup(out var reads) || reads.Length != 2 ||
            reads[0] is not Relation fast || reads[1] is not Relation slow || provenance is not Relation evidence ||
            !evidence.TryReadOrderedGroup(out var parts) || parts.Length != 2 || parts[0] is not Relation changes || parts[1] is not Relation loss ||
            !changes.TryReadOrderedGroup(out _) || !loss.TryReadOrderedGroup(out _)) return false;
        sample = acquisition; fastRead = fast; slowRead = slow; updates = changes; losses = loss; return true;
    }

    public static bool TryReadBankRead(Relation read, out CarrierLocation score, out int signedEvidence,
        out int totalWeight, out int referenceCount)
    {
        score = default; signedEvidence = totalWeight = referenceCount = 0;
        if (!ReadWitness.TryRead(read, out _, out var surface, out var result, out _, out _) ||
            surface is not Relation grammar || !grammar.TryReadOrderedGroup(out var laws) || laws.Length != 3 ||
            laws[2] is not Relation normalization || normalization.Left is not Relation input || normalization.Right is not Relation output ||
            !Span(input, out var attention, out var origin, out var total) || origin != 0 || total is < 1 or > 32768 ||
            !Span(output, out var scoreAxis, out origin, out var extent) || origin != 0 || extent != 16 ||
            result is not Relation group || !group.TryReadOrderedGroup(out var fields) || fields.Length != 4 ||
            !Point(fields[0], out var value) || !value.Carrier.SameIdentityAs(scoreAxis) || value.Ticks is < -16 or > 16 ||
            !Point(fields[1], out var votes) || !votes.Carrier.SameIdentityAs(attention) || votes.Ticks < -total || votes.Ticks > total ||
            !Point(fields[2], out var weight) || !weight.Carrier.SameIdentityAs(attention) || weight.Ticks != total ||
            !Point(fields[3], out var count) || count.Carrier.IsUnknown || count.Ticks is < 1 or > 128 ||
            value.Ticks != votes.Ticks * 16 / total) return false;
        score = value; signedEvidence = (int)votes.Ticks; totalWeight = (int)total; referenceCount = (int)count.Ticks; return true;
    }

    public static bool TryReadBankComparisons(Relation read, out Relation[] comparisons)
    {
        comparisons = [];
        if (!ReadWitness.TryRead(read, out _, out _, out _, out _, out var provenance) || provenance is not Relation group ||
            !group.TryReadOrderedGroup(out var fields) || fields.Length != 4 || fields[0] is not Relation queries ||
            !queries.TryReadOrderedGroup(out var entries) || entries.Any(entry => entry is not Relation)) return false;
        comparisons = entries.Cast<Relation>().ToArray(); return true;
    }

    private static bool ValidSample(Relation sample, IElement source, Carrier presenceAxis, out bool exists)
    {
        exists = false;
        if (!SamplingSurface.TryReadSample(sample, out var presence, out _) || !presence.Carrier.SameIdentityAs(presenceAxis) ||
            !ReadWitness.TryRead(sample, out var selected, out _, out _, out _, out _) || !ReferenceEquals(source, selected)) return false;
        exists = presence.Ticks == 1; return true;
    }
    private static Carrier CountAxis(Relation policy)
    { policy.TryReadOrderedGroup(out var fields); Point(fields[3], out var count); return count.Carrier; }
    private static IElement InputChain(List<IElement> inputs)
    { var chain = inputs[0]; for (var i = 1; i < inputs.Count; i++) chain = new Relation(chain, inputs[i]); return chain; }
    private static bool Point(IElement element, out CarrierLocation point)
    {
        if (element is CarrierLocation value && value.Carrier is not null && value.Tick is not null) { point = value; return true; }
        if (element is Relation { Left: CarrierLocation location, Right: Carrier carrier } && location.Carrier is not null &&
            location.Tick is not null && location.Carrier.SameIdentityAs(carrier)) { point = location; return true; }
        point = default; return false;
    }
    private static bool Span(Relation span, out Carrier axis, out long start, out long end)
    {
        if (Point(span.Left, out var a) && Point(span.Right, out var b) && a.Carrier.SameIdentityAs(b.Carrier))
        { axis = a.Carrier; start = a.Ticks; end = b.Ticks; return true; }
        axis = null!; start = end = 0; return false;
    }
    private static Relation State(Carrier root, IElement source, Relation policy, Relation fast, Relation slow, Relation latest) =>
        Relation.OrderedGroup(root, source, policy, fast, slow, latest);
    private static Relation Open(IElement selected, IElement attempted) =>
        ReadWitness.Create(new Carrier(), selected, attempted, new Placeholder(), new Relation(selected, attempted), attempted);
    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
}
