using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A small, supplied policy for evaluating one claim over a complete
/// pass. Utility revises a cyclic preference, never the possible-motion set.
/// The last two pass witnesses are retained; older detail is deliberately lost.
/// The caller supplies the claim context and captured discrepancy observations.
/// Their physical truth and attribution to the selected route are premises.</summary>
public sealed class PassPolicyMemory
{
    // Surface = Closed(identity, claim-context, discrepancy-basis, options,
    //   phase-port, attached-wrap, Closed(position -> option),
    //   magnitude-basis, change-basis, change-domain, goal-basis, goal-domain,
    //   step-basis, step-domain, keep/advance-law, step-outlets, discarded-port,
    //   (retained-capacity -> open-forgotten-history))
    // archive = Closed(Surface, pending-choice scope, last-two-pass scope,
    //   optional import-witness scope). A transfer imports a preference, not
    //   a target observation or a motion fact.
    // choose evidence = Surface -> Closed(captured-phase, cyclic-membership-walk)
    // pass = (Surface -> event) -> Closed(context, choice receipt, captured
    //   before/after discrepancy, evaluator premise, magnitude claims,
    //   comparison claim, law read, cursor transition, resulting support)
    // A pending choice is an operation, not a newly established physical fact.

    private readonly Carrier phase;
    private readonly Carrier magnitude;
    private readonly Carrier changeBasis;
    private readonly Carrier goalBasis;
    private readonly Carrier stepBasis;
    private readonly Carrier discarded;
    private readonly Relation positions;
    private readonly Relation outlets;
    private readonly IElement[] changes;
    private readonly IElement[] goals;
    private readonly IElement[] steps;
    private readonly FiniteLawPlan plan;
    private Relation pending;
    private Relation recent;
    private Relation imported;

    public Relation Surface { get; }
    public IElement Identity { get; }
    public IElement Context { get; }
    public Carrier DiscrepancyBasis { get; }
    public Relation OptionDomain { get; }
    public IReadOnlyList<Relation> Options { get; }
    public Relation Law { get; }
    public long Phase => checked((long)phase.Value);
    public long PhaseTension => phase.Tension;
    public long DiscardedCount => checked((long)discarded.Value);
    public Relation LossSurface => new(new CarrierLocation(discarded, DiscardedCount), E(Surface)[17]);
    public Relation[] RecentPasses => E(recent).Cast<Relation>().ToArray();
    public Relation? PendingChoice => E(pending).SingleOrDefault() as Relation;
    public Relation? ImportedPreference => E(imported).SingleOrDefault() as Relation;

    public sealed record ChoiceRead(bool Selected, Relation? Candidate, Relation? Receipt,
        int SelectedIndex, long PhaseBefore, long PhaseAfter, Relation Witness);
    public sealed record PassRead(bool Recorded, bool Complete, bool Improved, bool Achieved,
        bool Advanced, long PhaseBefore, long PhaseAfter, Relation Witness, Relation LawRead);
    public sealed record PreferenceRead(bool Available, Relation Surface, Relation? Option, Relation Evidence);
    public sealed record ImportRead(bool Accepted, Relation? TargetOption, long PhaseBefore,
        long PhaseAfter, Relation Witness);

    private PassPolicyMemory(Relation surface, Relation pendingChoice, Relation recentPasses, Relation? importScope = null)
    {
        if (!Closed(surface, out var fields) || fields is not
            [Placeholder identity, var context, Carrier error, Relation options,
             Relation phasePort, Relation wrap, Relation bindings,
             Carrier abs, Carrier change, Relation changeDomain, Carrier goal, Relation goalDomain,
             Carrier step, Relation stepDomain, Relation law, Relation routes, Relation forgottenPort, Relation loss] ||
            error.IsUnknown || !LivePort(phasePort, out phase) || !LivePort(forgottenPort, out discarded) ||
            loss.Left is not CarrierLocation retainedCapacity || !ReferenceEquals(retainedCapacity.Carrier, discarded) || retainedCapacity.Ticks != 2 ||
            loss.Right is not Relation forgotten || !StructuralConstraint.TryReadScope(forgotten, out var lost, out var lossStart, out var lossEnd) || lost.Length != 0 || lossStart.Equals(lossEnd) ||
            !Closed(options, out var words) || words.Length is < 2 or > 64 || words.Any(x => x is not Relation) ||
            words.Distinct(ReferenceEqualityComparer.Instance).Count() != words.Length ||
            !Wrap(wrap, phase, words.Length) || !phase.IsAttached(wrap) || phase.Value >= (ulong)words.Length ||
            phase.Tension < 0 || phase.Tension % words.Length != 0 ||
            !Closed(bindings, out var bound) || bound.Length != words.Length ||
            bound.Where((b, i) => b is not Relation { Left: CarrierLocation at } pair ||
                !ReferenceEquals(at.Carrier, phase) || at.Ticks != i || !ReferenceEquals(pair.Right, words[i])).Any() ||
            !Coordinates(changeDomain, change, [-1, 0, 1], out changes) ||
            !Coordinates(goalDomain, goal, [0, 1], out goals) ||
            !Coordinates(stepDomain, step, [0, 1], out steps) || !step.UnitShape.Equals(phase.UnitShape) ||
            !FiniteLaw.TryRead(law, out var left, out var right, out var output, out _) ||
            !ReferenceEquals(left, changeDomain) || !ReferenceEquals(right, goalDomain) || !ReferenceEquals(output, stepDomain) ||
            !Closed(routes, out var effects) || effects.Length != 2 ||
            effects.Where((r, i) => r is not Relation { Right: Relation route } effect || !Same(effect.Left, steps[i]) ||
                !ReferenceEquals(route.Left, phasePort) || route.Right is not Relation calibration ||
                !UnitMap(calibration, step, phase)).Any() ||
            !Closed(pendingChoice, out var choices) || choices.Length > 1 || choices.Any(c => c is not Relation) ||
            !Closed(recentPasses, out var passes) || passes.Length > 2 || passes.Any(p => p is not Relation))
            throw new ArgumentException("Expected an exact bounded pass-policy memory.", nameof(surface));
        Surface = surface; Identity = identity; Context = context; DiscrepancyBasis = error;
        OptionDomain = options; Options = Array.AsReadOnly(words.Cast<Relation>().ToArray());
        positions = bindings; magnitude = abs; changeBasis = change; goalBasis = goal;
        stepBasis = step; outlets = routes; Law = law; pending = pendingChoice; recent = recentPasses;
        imported = importScope ?? S();
        plan = FiniteLawPlan.Capture(law) ?? throw new ArgumentException("Expected a closed finite pass law.", nameof(surface));
        if (choices is [Relation choice] && !ReadChoice(choice, out _, out _, out _) ||
            passes.Cast<Relation>().Any(p => !PassEvent(p, out _)) ||
            passes.Cast<Relation>().Select(p => { PassEvent(p, out var e); return e; }).Distinct(ReferenceEqualityComparer.Instance).Count() != passes.Length)
            throw new ArgumentException("Pass history or pending choice does not belong to this policy.", nameof(surface));
        if (!Closed(imported, out var imports) || imports.Length > 1 ||
            imports is [var import] && (import is not Relation witness || !ReadImportWitness(witness)))
            throw new ArgumentException("Preference import does not belong to this policy.", nameof(importScope));
    }

    public static PassPolicyMemory Create(IElement claimContext, Carrier discrepancyBasis, params Relation[] orderedOptions) =>
        CreateCore(claimContext, discrepancyBasis, orderedOptions, true);

    /// <summary>Comparison apparatus: the same choices and pass evidence, but
    /// every readable outcome retains its selected option. The difference is
    /// encoded in finite-law rows, not a runtime policy flag.</summary>
    public static PassPolicyMemory CreateFixed(IElement claimContext, Carrier discrepancyBasis, params Relation[] orderedOptions) =>
        CreateCore(claimContext, discrepancyBasis, orderedOptions, false);

    private static PassPolicyMemory CreateCore(IElement claimContext, Carrier discrepancyBasis, Relation[] orderedOptions, bool revise)
    {
        if (discrepancyBasis.IsUnknown || orderedOptions.Length is < 2 or > 64 ||
            orderedOptions.Distinct(ReferenceEqualityComparer.Instance).Count() != orderedOptions.Length)
            throw new ArgumentException("Supply a concrete discrepancy basis and two to sixty-four distinct options.");
        var phase = new Carrier(); var wrap = new Relation(Relation.Point(phase, orderedOptions.Length), Relation.Point(phase, 0));
        phase.Attach(wrap); var phasePort = Port(phase); var abs = new Carrier(discrepancyBasis.UnitShape);
        var change = new Carrier(); var goal = new Carrier(); var step = new Carrier(phase.UnitShape);
        var signs = new long[] { -1, 0, 1 }.Select(n => (IElement)new CarrierLocation(change, n)).ToArray();
        var reached = new long[] { 0, 1 }.Select(n => (IElement)new CarrierLocation(goal, n)).ToArray();
        var deltas = new long[] { 0, 1 }.Select(n => (IElement)new CarrierLocation(step, n)).ToArray();
        var a = S(signs); var b = S(reached); var c = S(deltas); var rows = new List<Relation>();
        for (var i = 0; i < signs.Length; i++) for (var j = 0; j < reached.Length; j++)
            rows.Add(FiniteLaw.Row(signs[i], reached[j], S(deltas[!revise || j == 1 || i == 0 ? 0 : 1])));
        var calibration = new Relation(Relation.Segment(step, 0, 1), Relation.Segment(phase, 0, 1));
        var routes = deltas.Select(d => (IElement)new Relation(d, new Relation(phasePort, calibration))).ToArray();
        var discarded = new Carrier();
        return new(S(new Placeholder(), claimContext, discrepancyBasis, S(orderedOptions), phasePort, wrap,
            S(orderedOptions.Select((word, i) => (IElement)new Relation(new CarrierLocation(phase, i), word)).ToArray()),
            abs, change, a, goal, b, step, c, FiniteLaw.Create(a, b, c, rows.ToArray()), S(routes), Port(discarded),
            new Relation(new CarrierLocation(discarded, 2), StructuralConstraint.Open())), S(), S());
    }

    /// <summary>Reads the first offered option in the declared cyclic order.
    /// Does not move the cursor or any actuator. Only one choice can be pending;
    /// repeated requests return that same selection until a pass accounts for it.</summary>
    public ChoiceRead Choose(Relation proposal)
    {
        if (PendingChoice is { } existing && ReadChoice(existing, out var held, out var heldIndex, out var start))
            return new(true, held, existing, heldIndex, start, Phase, new Relation(Surface, existing));
        var walk = new List<IElement>();
        if (QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out var domain, out var support, out _) &&
            Closed(domain, out var declared) && Closed(support, out var offered) && offered.Length != 0 &&
            declared.All(d => Options.Any(option => ReferenceEquals(option, d))) &&
            offered.All(d => declared.Any(x => ReferenceEquals(x, d))))
        {
            for (var offset = 0; offset < Options.Count; offset++)
            {
                var index = (int)((Phase + offset) % Options.Count); var word = Options[index];
                var membership = StructuralConstraint.Match(word, S(), support); walk.Add(membership);
                if (!StructuralConstraint.TryRead(membership, out _, out var admitted, out _, out var unresolved) ||
                    E(admitted).Length == 0 || E(unresolved).Length != 0) continue;
                var cause = new Relation(Surface, S(new CarrierLocation(phase, Phase), S(walk.ToArray())));
                var receipt = FiniteChoice.Select(proposal, Identity, word, cause);
                if (!FiniteChoice.TryReadSelected(receipt, out _)) continue;
                pending = S(receipt);
                return new(true, word, receipt, index, Phase, Phase, new Relation(cause, receipt));
            }
        }
        return new(false, null, null, -1, Phase, Phase, new Relation(new Relation(Surface, proposal), StructuralConstraint.Open(walk.ToArray())));
    }

    /// <summary>Consumes a pending selection with one whole-pass evaluation.
    /// Before/after must be captured signed discrepancies in this exact basis.
    /// The context includes the selected claim/goal; changed context needs its
    /// own memory. The evaluator evidence supplies the attribution premise.
    /// Duplicate exclusion covers only the retained last-two-event window.</summary>
    public PassRead CompletePass(IElement claimContext, IElement beforeDiscrepancy, IElement afterDiscrepancy,
        IElement evaluationEvidence, Relation? choiceReceipt = null, IElement? eventIdentity = null, bool compiled = true)
    {
        var occurrence = eventIdentity ?? new Placeholder();
        if (occurrence is not Placeholder) throw new ArgumentException("Use an opaque pass event identity.", nameof(eventIdentity));
        var beforePhase = Phase; var choice = choiceReceipt ?? PendingChoice;
        var refusal = new Relation(new Relation(Surface, occurrence), S(claimContext, beforeDiscrepancy, afterDiscrepancy, evaluationEvidence));
        var unresolvedLaw = compiled ? plan.Evaluate(StructuralConstraint.Open(), StructuralConstraint.Open())
            : FiniteLaw.Evaluate(Law, StructuralConstraint.Open(), StructuralConstraint.Open());
        if (!Same(claimContext, Context) || choice is null || !ReferenceEquals(choice, PendingChoice) ||
            !ReadChoice(choice, out _, out var selectedIndex, out var selectedPhase) || selectedPhase != beforePhase ||
            RecentPasses.Any(p => PassEvent(p, out var prior) && Same(prior, occurrence)))
            return new(false, false, false, false, false, beforePhase, Phase, refusal, unresolvedLaw);

        var before = Capture(beforeDiscrepancy); var after = Capture(afterDiscrepancy);
        var zero = new CarrierLocation(DiscrepancyBasis, 0);
        var beforeClaim = OperationClaim.Equality(new Carrier(), before, zero, magnitude);
        var afterClaim = OperationClaim.Equality(new Carrier(), after, zero, magnitude);
        Relation comparison = StructuralConstraint.Open(beforeClaim, afterClaim);
        var lawRead = unresolvedLaw; var complete = false; var improved = false; var achieved = false; var advance = false; var changeSign = 0;
        Relation transition = StructuralConstraint.Open();
        if (Frozen(before) && Frozen(after) &&
            OperationClaim.TryReadResolvedViolation(beforeClaim, out var beforeMagnitude) &&
            OperationClaim.TryReadResolvedViolation(afterClaim, out var afterMagnitude))
        {
            comparison = OperationClaim.Equality(new Carrier(), beforeMagnitude, afterMagnitude, changeBasis);
            if (OperationClaim.TryRead(comparison, out _, out var delta, out _, out _, out _, out var remainder) &&
                delta is CarrierLocation change && remainder is CarrierLocation residue && residue.Ticks == 0)
            {
                changeSign = Math.Sign(change.Ticks); improved = changeSign < 0; achieved = afterMagnitude.Ticks == 0;
                var left = S(changes[changeSign + 1]); var right = S(goals[achieved ? 1 : 0]);
                lawRead = compiled ? plan.Evaluate(left, right) : FiniteLaw.Evaluate(Law, left, right);
                if (FiniteLaw.TryReadResult(lawRead, out var answers, out var rest, out _) &&
                    Closed(answers, out var values) && values is [CarrierLocation step] &&
                    Closed(rest, out var pendingRead) && pendingRead.Length == 0 && ReferenceEquals(step.Carrier, stepBasis) && step.Ticks is 0 or 1)
                {
                    complete = true; advance = step.Ticks == 1;
                    var offset = (selectedIndex - (int)beforePhase + Options.Count) % Options.Count;
                    var selectedPosition = new CarrierLocation(phase, selectedIndex);
                    var cursorBefore = new CarrierLocation(phase, beforePhase);
                    var route = E(outlets)[checked((int)step.Ticks)];
                    // Cyclic scan displacement and the law's one step share the
                    // declared phase basis. The attached wrap accounts locally.
                    phase.Add(checked((ulong)(offset + step.Ticks)));
                    transition = S(cursorBefore, new Relation(positions, selectedPosition),
                        new CarrierLocation(phase, offset), new Relation(lawRead, route),
                        new CarrierLocation(phase, Phase), new CarrierLocation(phase, phase.Tension));
                }
            }
        }
        var outcome = complete ? S(goals[achieved ? 1 : 0], changes[changeSign + 1])
            : StructuralConstraint.Open();
        var witness = new Relation(new Relation(Surface, occurrence), S(claimContext, choice,
            new Relation(before, after), evaluationEvidence, S(beforeClaim, afterClaim), comparison, lawRead, transition, outcome));
        var retained = RecentPasses.Append(witness).ToArray();
        if (retained.Length > 2) { retained = retained[^2..]; discarded.Add(1); }
        recent = S(retained); pending = S();
        return new(true, complete, improved, achieved, advance, beforePhase, Phase, witness, lawRead);
    }

    /// <summary>Publishes a historical, locally useful preference. A bare
    /// default, a newly advanced but untried option, an unresolved last pass,
    /// or an outstanding choice cannot supply this witness. Its utility is
    /// evidence about the source context only, not a fact about a new world.</summary>
    public PreferenceRead ExportPreference()
    {
        var option = Options[(int)Phase]; var last = RecentPasses.LastOrDefault();
        if (PendingChoice is not null || last is null || !UsefulPass(last, option, Phase))
        {
            var open = StructuralConstraint.Open(Surface, Context, OptionDomain, recent);
            return new(false, open, null, open);
        }
        var capture = S(Surface, Context, OptionDomain, new CarrierLocation(phase, Phase), option, last);
        return new(true, capture, option, last);
    }

    /// <summary>An explicit analogy between options in two contexts. Rows are
    /// source option -> target option. Neither matching positions nor this
    /// supplied correspondence proves the target option physically correct.</summary>
    public Relation PreferenceMapping(Relation preference, Relation rows, IElement premise) =>
        new(new Relation(preference, Surface), S(Context, OptionDomain, rows, premise));

    /// <summary>One import into a fresh policy, through its exposed option and
    /// phase surfaces. No source sample, pass history, or candidate support is
    /// inserted into the target. Later ordinary pass evaluations may revise it.</summary>
    public ImportRead ImportPreference(Relation preference, Relation mapping)
    {
        var before = Phase;
        var refusal = new Relation(new Relation(preference, mapping),
            StructuralConstraint.Open(Surface, pending, recent, imported));
        if (PendingChoice is not null || RecentPasses.Length != 0 || DiscardedCount != 0 ||
            ImportedPreference is not null || Phase != 0 || PhaseTension != 0 ||
            !ReadMapping(preference, mapping, out var target, out var index))
            return new(false, null, before, Phase, refusal);

        var capturedBefore = new CarrierLocation(phase, before);
        // The correspondence selects a position in this local option domain.
        // Its positional displacement enters the actual phase carrier as
        // tension; the same attached wrap used by pass feedback accounts it.
        var destination = new CarrierLocation(phase, index);
        var route = new Relation(new Relation(preference, mapping),
            new Relation(E(Surface)[4], new Relation(positions, destination)));
        phase.Add(checked((ulong)index));
        var transition = S(capturedBefore, destination, route,
            new CarrierLocation(phase, Phase), new CarrierLocation(phase, phase.Tension));
        var witness = new Relation(new Relation(preference, mapping), S(Surface, Context,
            OptionDomain, target, transition, StructuralConstraint.Open()));
        imported = S(witness);
        return new(true, target, before, Phase, witness);
    }

    public Relation ArchiveSurface() => ImportedPreference is null ? S(Surface, pending, recent) : S(Surface, pending, recent, imported);
    public static PassPolicyMemory Restore(Relation archive)
    {
        if (Closed(archive, out var fields))
        {
            if (fields is [Relation surface, Relation pending, Relation recent]) return new(surface, pending, recent);
            if (fields is [Relation importedSurface, Relation importedPending, Relation importedRecent, Relation imports])
                return new(importedSurface, importedPending, importedRecent, imports);
        }
        throw new ArgumentException("Expected one bounded pass-memory archive.", nameof(archive));
    }
    public byte[] Encode() => GojoNumericFormat.Encode(ArchiveSurface());
    public static PassPolicyMemory Decode(ReadOnlySpan<byte> bytes) => GojoNumericFormat.Decode(bytes) is [Relation archive]
        ? Restore(archive) : throw new ArgumentException("Expected one pass-memory root.", nameof(bytes));

    private bool ReadMapping(Relation preference, Relation mapping, out Relation target, out int index)
    {
        target = null!; index = -1;
        if (!ReadPreference(preference, out var source, out var selected) ||
            mapping.Left is not Relation request || !ReferenceEquals(request.Left, preference) || !ReferenceEquals(request.Right, Surface) ||
            mapping.Right is not Relation payload || !Closed(payload, out var fields) ||
            fields is not [var context, Relation domain, Relation rows, _] || !Same(context, Context) ||
            !ReferenceEquals(domain, OptionDomain) || !Closed(rows, out var pairs) || pairs.Length == 0 ||
            pairs.Any(item => item is not Relation pair ||
                !source.Options.Any(option => ReferenceEquals(option, pair.Left)) ||
                !Options.Any(option => ReferenceEquals(option, pair.Right)))) return false;
        var mappings = pairs.Cast<Relation>().ToArray();
        // Every supplied source row must be unambiguous, including rows not
        // selected today. Partial maps are valid if the selected row exists.
        if (mappings.Select(row => row.Left).Distinct(ReferenceEqualityComparer.Instance).Count() != mappings.Length) return false;
        var selectedRow = mappings.SingleOrDefault(row => ReferenceEquals(row.Left, selected));
        if (selectedRow?.Right is not Relation selectedTarget) return false;
        target = selectedTarget; index = Options.ToList().FindIndex(item => ReferenceEquals(item, selectedTarget));
        return index >= 0;
    }

    private static bool ReadPreference(Relation preference, out PassPolicyMemory source, out Relation option)
    {
        source = null!; option = null!;
        if (!Closed(preference, out var fields) || fields is not
            [Relation surface, var context, Relation domain, CarrierLocation at, Relation selected, Relation witness]) return false;
        try { source = new(surface, S(), S()); }
        catch (ArgumentException) { return false; }
        if (!Same(source.Context, context) || !ReferenceEquals(source.OptionDomain, domain) ||
            !ReferenceEquals(at.Carrier, source.phase) || at.SameTickRefAs(source.phase.ValueSlot) ||
            at.SameTickRefAs(source.phase.LiveTensionSlot) || at.Ticks < 0 || at.Ticks >= source.Options.Count ||
            !ReferenceEquals(selected, source.Options[(int)at.Ticks]) || !source.UsefulPass(witness, selected, at.Ticks)) return false;
        option = selected; return true;
    }

    private bool UsefulPass(Relation witness, Relation option, long capturedPhase)
    {
        if (!PassEvent(witness, out _) || witness.Right is not Relation body || !Closed(body, out var fields) ||
            fields is not [_, Relation receipt, Relation samples, _, _, _, Relation lawRead, Relation transition, Relation outcome] ||
            !ReadChoice(receipt, out var selected, out var index, out _) || !ReferenceEquals(selected, option) || index != capturedPhase ||
            !Frozen(samples.Left) || !Frozen(samples.Right) ||
            !Closed(outcome, out var result) || result is not [CarrierLocation reached, CarrierLocation changed] ||
            !ReferenceEquals(reached.Carrier, goalBasis) || !ReferenceEquals(changed.Carrier, changeBasis) ||
            reached.Ticks is < 0 or > 1 || changed.Ticks is < -1 or > 1 ||
            reached.Ticks != 1 && changed.Ticks != -1 ||
            !FiniteLaw.TryReadResult(lawRead, out var answers, out var rest, out _) ||
            !Closed(answers, out var answer) || answer is not [CarrierLocation step] ||
            !ReferenceEquals(step.Carrier, stepBasis) || step.Ticks != 0 || !Closed(rest, out var residue) || residue.Length != 0 ||
            !Closed(transition, out var movement) || movement.Length != 6 ||
            movement[4] is not CarrierLocation after || !ReferenceEquals(after.Carrier, phase) || after.Ticks != capturedPhase) return false;
        // Re-read the retained discrepancies. A label-shaped "good outcome"
        // without a readable useful before/after pair cannot export a prior.
        var zero = new CarrierLocation(DiscrepancyBasis, 0);
        var before = OperationClaim.Equality(new Carrier(), samples.Left, zero, magnitude);
        var ending = OperationClaim.Equality(new Carrier(), samples.Right, zero, magnitude);
        return OperationClaim.TryReadResolvedViolation(before, out var a) &&
            OperationClaim.TryReadResolvedViolation(ending, out var b) &&
            reached.Ticks == (b.Ticks == 0 ? 1 : 0) && changed.Ticks == Math.Sign(b.Ticks - a.Ticks) &&
            (b.Ticks == 0 || b.Ticks < a.Ticks);
    }

    private bool ReadImportWitness(Relation witness)
    {
        if (witness.Left is not Relation { Left: Relation preference, Right: Relation mapping } ||
            !ReadMapping(preference, mapping, out var target, out var index) ||
            witness.Right is not Relation body || !Closed(body, out var fields) ||
            fields is not [var owner, var context, var domain, var option, Relation transition, Relation uncertainty] ||
            !ReferenceEquals(owner, Surface) || !Same(context, Context) || !ReferenceEquals(domain, OptionDomain) ||
            !ReferenceEquals(option, target) || !StructuralConstraint.TryReadScope(uncertainty, out var unknown, out var start, out var end) ||
            unknown.Length != 0 || start.Equals(end) || !Closed(transition, out var move) ||
            move is not [CarrierLocation before, CarrierLocation destination, Relation route, CarrierLocation after, CarrierLocation remainder]) return false;
        return ReferenceEquals(before.Carrier, phase) && before.Ticks == 0 &&
            ReferenceEquals(destination.Carrier, phase) && destination.Ticks == index &&
            ReferenceEquals(after.Carrier, phase) && after.Ticks == index &&
            ReferenceEquals(remainder.Carrier, phase) && remainder.Ticks == 0 &&
            route.Left is Relation origin && ReferenceEquals(origin.Left, preference) && ReferenceEquals(origin.Right, mapping) &&
            route.Right is Relation destinationRoute && ReferenceEquals(destinationRoute.Left, E(Surface)[4]) &&
            destinationRoute.Right is Relation binding && ReferenceEquals(binding.Left, positions) && Same(binding.Right, destination);
    }

    private bool ReadChoice(Relation receipt, out Relation candidate, out int index, out long capturedPhase)
    {
        candidate = null!; index = -1; capturedPhase = -1;
        if (!FiniteChoice.TryReadSelected(receipt, out _) ||
            !FiniteChoice.TryReadSelection(receipt, out var proposal, out var chooser, out var chosen, out var evidence, out _, out _, out _, out _) ||
            !Same(chooser, Identity) || chosen is not Relation word ||
            !QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out var domain, out var support, out _) ||
            !Closed(domain, out var declared) || declared.Any(d => !Options.Any(option => ReferenceEquals(option, d))) ||
            evidence is not Relation { Right: Relation values } reason || !ReferenceEquals(reason.Left, Surface) ||
            !Closed(values, out var fields) || fields is not [CarrierLocation at, Relation walk] ||
            !ReferenceEquals(at.Carrier, phase) || !Closed(walk, out var visited) || visited.Length == 0) return false;
        index = Options.ToList().FindIndex(option => ReferenceEquals(option, word));
        if (index < 0 || at.Ticks < 0 || at.Ticks >= Options.Count || visited.Length != (index - at.Ticks + Options.Count) % Options.Count + 1) return false;
        for (var offset = 0; offset < visited.Length; offset++)
        {
            var expected = Options[(int)((at.Ticks + offset) % Options.Count)];
            if (visited[offset] is not Relation membership || !MembershipRequest(membership, expected, support)) return false;
            // Recheck that every skipped option really is unavailable. A
            // copied membership-shaped witness cannot reorder the preference.
            var checkedRead = StructuralConstraint.Match(expected, S(), support);
            if (!StructuralConstraint.TryRead(checkedRead, out _, out var matches, out _, out var remainder) ||
                E(remainder).Length != 0 || (E(matches).Length != 0) != (offset == visited.Length - 1)) return false;
        }
        candidate = word; capturedPhase = at.Ticks; return true;
    }

    private static bool MembershipRequest(Relation read, IElement candidate, Relation support) =>
        StructuralConstraint.TryRead(read, out var request, out _, out _, out _) &&
        ReferenceEquals(request.Right, support) && request.Left is Relation { Left: Relation pattern, Right: Relation initial } &&
        Same(pattern.Left, candidate) && pattern.Right is Relation variables && Closed(variables, out var slots) && slots.Length == 0 &&
        Closed(initial, out var bindings) && bindings.Length == 0;

    private bool PassEvent(Relation witness, out IElement occurrence)
    {
        occurrence = null!;
        if (witness.Left is not Relation origin || !ReferenceEquals(origin.Left, Surface) || origin.Right is not Placeholder eventId ||
            witness.Right is not Relation body || !Closed(body, out var fields) || fields.Length != 9 || !Same(fields[0], Context)) return false;
        occurrence = eventId; return true;
    }
    private IElement Capture(IElement sample) => Frozen(sample) && sample is CarrierLocation at ? new CarrierLocation(DiscrepancyBasis, at.Ticks) : sample;
    private bool Frozen(IElement sample) => sample is CarrierLocation at && ReferenceEquals(at.Carrier, DiscrepancyBasis) &&
        !at.SameTickRefAs(DiscrepancyBasis.ValueSlot) && !at.SameTickRefAs(DiscrepancyBasis.LiveTensionSlot);
    private static bool LivePort(Relation port, out Carrier carrier)
    { carrier = null!; if (port is not { Left: CarrierLocation at, Right: Carrier owner } || owner.IsUnknown || !ReferenceEquals(at.Carrier, owner) || !at.SameTickRefAs(owner.ValueSlot)) return false; carrier = owner; return true; }
    private static bool Coordinates(Relation scope, Carrier basis, long[] ticks, out IElement[] entries) => Closed(scope, out entries) && entries.Length == ticks.Length &&
        entries.Select((entry, i) => entry is CarrierLocation at && ReferenceEquals(at.Carrier, basis) && at.Ticks == ticks[i]).All(valid => valid);
    private static bool Wrap(Relation relation, Carrier basis, long count) => relation is
        { Left: Relation { Left: CarrierLocation a, Right: Carrier ca }, Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } &&
        ReferenceEquals(ca, basis) && ReferenceEquals(cb, basis) && ReferenceEquals(a.Carrier, basis) && ReferenceEquals(b.Carrier, basis) && a.Ticks == count && b.Ticks == 0;
    private static bool UnitMap(Relation relation, Carrier source, Carrier target) =>
        relation.Left is Relation a && relation.Right is Relation b && Span(a, source) && Span(b, target);
    private static bool Span(Relation relation, Carrier basis) => relation is
        { Left: Relation { Left: CarrierLocation a, Right: Carrier ca }, Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } &&
        ReferenceEquals(ca, basis) && ReferenceEquals(cb, basis) && ReferenceEquals(a.Carrier, basis) && ReferenceEquals(b.Carrier, basis) && a.Ticks == 0 && b.Ticks == 1;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation u && b is CarrierLocation v && ReferenceEquals(u.Carrier, v.Carrier) && u.SameTickRefAs(v);
    private static Relation Port(Carrier carrier) => new(carrier.ValueSlot, carrier);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
