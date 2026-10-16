using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An authored onset program interpreted through an explicit phrase
/// law. Expansion preserves slot and stroke order, not elapsed beat duration.
/// The generated control cycle authors repetition; a closed source scope alone
/// is not evidence of a periodically observed physical rhythm.</summary>
public sealed class RhythmFriezeEntry
{
    // onset = actual-slot -> role
    // state-schedule = Closed(actual-slot -> actual-state)
    // source-cycle = Closed(actual-slots) -> Closed(actual-slot -> next-slot)
    // selection = ((onset -> state-binding) -> (state-read -> phrase-law-read)) ->
    //             (selected-phrase -> (compiled-words -> unresolved))
    // generation = (actual-selection -> actual-word) -> (actual-phase -> actual-output-row)
    // A phrase is Closed(words), each word Closed(actual move occurrences).
    public string Name { get; }
    public FriezeFixture Fixture { get; }
    public Relation Words { get; }
    public int PhaseCount => E(Words).Length;
    public Relation Surface { get; }
    public Relation Onsets { get; }
    public Relation Roles { get; }
    public Relation States { get; }
    public Relation StateSchedule { get; }
    public Relation SourceCycle { get; }
    public Relation PhraseLaw { get; }
    public Relation Selections { get; }
    public Relation Generation { get; }
    public Relation Unresolved { get; }

    internal RhythmFriezeEntry(string name, FriezeFixture fixture, Relation words,
        Relation onsets, Relation roles, Relation states, Relation schedule,
        Relation sourceCycle, Relation law, Relation selections, Relation generation, Relation unresolved)
    {
        Name = name; Fixture = fixture; Words = words; Onsets = onsets; Roles = roles;
        States = states; StateSchedule = schedule; SourceCycle = sourceCycle;
        PhraseLaw = law; Selections = selections; Generation = generation; Unresolved = unresolved;
        Surface = new Relation(new Relation(new Relation(onsets, roles), new Relation(states, schedule)),
            new Relation(new Relation(sourceCycle, law),
                new Relation(new Relation(selections, generation), new Relation(fixture.Surface, new Relation(words, unresolved)))));
    }

    /// <summary>Reads an actual member of this compilation. A lookalike copied
    /// receipt is not a selected source occurrence.</summary>
    public bool TryReadSelection(Relation row, out Relation onsetBinding, out Relation stateBinding,
        out Relation lawRead, out Relation phrase, out Relation strokes, out Relation unresolved)
    {
        onsetBinding = stateBinding = lawRead = phrase = strokes = unresolved = null!;
        if (!E(Selections).Any(item => ReferenceEquals(item, row)) ||
            row.Left is not Relation { Left: Relation source, Right: Relation reads } ||
            source.Left is not Relation onset || source.Right is not Relation state ||
            !Same(onset.Left, state.Left) || reads.Left is not Relation || reads.Right is not Relation read ||
            read.Left is not Relation { Left: Relation request } ||
            !ReferenceEquals(request.Left, PhraseLaw) ||
            request.Right is not Relation { Left: Relation roles, Right: Relation states } ||
            !One(roles, onset.Right) || !One(states, state.Right) ||
            row.Right is not Relation { Left: Relation selected, Right: Relation result } ||
            result.Left is not Relation words || result.Right is not Relation pending ||
            !StructuralConstraint.TryReadScope(selected, out _, out _, out _) ||
            !Closed(words) || !Closed(pending) || !FiniteLaw.TryReadResult(read, out _, out _, out _)) return false;
        onsetBinding = onset; stateBinding = state; lawRead = read; phrase = selected;
        strokes = words; unresolved = pending; return true;
    }

    /// <summary>Reads one exact generated output row and its originating phrase
    /// selection. Equal repeated words keep distinct phase occurrences.</summary>
    public bool TryReadGeneration(Relation row, out Relation selection, out Relation word,
        out IElement phase, out Relation outputRow)
    {
        selection = word = outputRow = null!; phase = null!;
        if (!E(Generation).Any(item => ReferenceEquals(item, row)) ||
            row.Left is not Relation { Left: Relation selected, Right: Relation stroke } ||
            !TryReadSelection(selected, out _, out _, out _, out _, out var words, out _) ||
            !E(words).Any(item => ReferenceEquals(item, stroke)) ||
            row.Right is not Relation { Right: Relation output } phaseBinding ||
            !FiniteLaw.TryRead(Fixture.OutputLaw, out _, out _, out _, out var actualRows) ||
            !actualRows.Any(actual => ReferenceEquals(actual, output)) ||
            output.Left is not Relation pair || !Same(pair.Left, phaseBinding.Left) ||
            !ReferenceEquals(pair.Right, Fixture.RootPort) || output.Right is not Relation support ||
            !One(support, stroke)) return false;
        selection = selected; word = stroke; phase = phaseBinding.Left; outputRow = output; return true;
    }

    private static bool One(Relation scope, IElement value) => Closed(scope) && E(scope) is [var item] && Same(item, value);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}

/// <summary>Small deliberately authored rhythm-to-fret interpretations. The
/// onset grammar does not determine a unique picture: the finite phrase law is
/// a separate connected premise. No numeric source samples or learned weights
/// participate in these programs.</summary>
public sealed class RhythmFriezeLibrary
{
    public QualitativeMotionFrame Frame { get; }
    public IElement HorizontalReach { get; }
    public IElement VerticalReach { get; }
    public IReadOnlyList<RhythmFriezeEntry> Entries { get; }
    public IReadOnlyList<RhythmFriezeEntry> Patterns => Entries;
    public Relation Surface { get; }

    public RhythmFriezeLibrary(QualitativeMotionFrame? frame = null,
        IElement? horizontalReach = null, IElement? verticalReach = null)
    {
        Frame = frame ?? new QualitativeMotionFrame(); HorizontalReach = horizontalReach ?? new Placeholder();
        VerticalReach = verticalReach ?? new Placeholder();
        var shapes = new PatternShapingLibrary(Frame, HorizontalReach, VerticalReach);
        var square = shapes.Patterns[0].Words; var folded = shapes.Patterns[2].Words;
        var broad = shapes.Patterns[3].Words; var nested = shapes.Patterns[5].Words;
        var connector = S(S(QualitativeMotionFrame.Move(Frame.Right, HorizontalReach)));
        var hit = new Placeholder(); var rest = new Placeholder(); var roles = new Relation(hit, rest);
        var grouped = OnsetRows(hit, rest, rest, hit, rest, rest, hit, rest);
        var alternating = OnsetRows(hit, rest, hit, rest, hit, rest, hit, rest);
        var four = OnsetRows(hit, rest, hit, rest);
        var constant = new Placeholder(); var constantClock = new PhaseRecurrence(S(constant), S(new Relation(constant, constant)));
        var a = new Placeholder(); var b = new Placeholder();
        var alternatingClock = new PhaseRecurrence(S(a, b), S(new Relation(a, b), new Relation(b, a)));
        var constantGrouped = Schedule(grouped, constant, constant, constant, constant, constant, constant, constant, constant);
        var alternatingGrouped = Schedule(grouped, a, b, a, b, a, b, a, b);
        var alternatingOther = Schedule(alternating, a, b, a, b, a, b, a, b);
        Relation ConstantLaw(Relation on, Relation off) => FiniteLaw.Create(S(hit, rest), constantClock.Domain,
            S(on, off), FiniteLaw.Row(hit, constant, S(on)), FiniteLaw.Row(rest, constant, S(off)));
        var alternatingLaw = FiniteLaw.Create(S(hit, rest), alternatingClock.Domain, S(folded, broad, connector),
            FiniteLaw.Row(hit, a, S(folded)), FiniteLaw.Row(hit, b, S(broad)),
            FiniteLaw.Row(rest, a, S(connector)), FiniteLaw.Row(rest, b, S(connector)));
        Entries = Array.AsReadOnly(new[]
        {
            FromOnsets(Frame, grouped, roles, constantClock.Surface, constantGrouped, ConstantLaw(folded, connector), "3–3–2 folded keys and connectors"),
            FromOnsets(Frame, grouped, roles, constantClock.Surface, constantGrouped, ConstantLaw(nested, connector), "Same onsets — nested keys"),
            FromOnsets(Frame, grouped, roles, constantClock.Surface, constantGrouped, ConstantLaw(folded, square), "Same onsets — square rest phrases"),
            FromOnsets(Frame, grouped, roles, alternatingClock.Surface, alternatingGrouped, alternatingLaw, "3–3–2 with alternating phrase state"),
            FromOnsets(Frame, alternating, roles, alternatingClock.Surface, alternatingOther, alternatingLaw, "Alternating onsets through the same law"),
            FromOnsets(Frame, four, roles, constantClock.Surface, Schedule(four, constant, constant, constant, constant),
                ConstantLaw(nested, folded), "Four slots — nested and folded phrases")
        });
        Surface = new Relation(new Relation(Frame.Surface, S(HorizontalReach, VerticalReach)),
            S(Entries.Select(entry => (IElement)entry.Surface).ToArray()));
    }

    /// <summary>Interprets source slots in their supplied order. States is an
    /// actual PhaseRecurrence surface; schedule binds each actual slot to one
    /// state. The unique outgoing transition must reach the next scheduled
    /// state, including the authored wrap. Unknown selection emits an open word
    /// and keeps its obligation instead of substituting a rest. At most 64
    /// generated strokes are admitted by the finite fixture API.</summary>
    public static RhythmFriezeEntry FromOnsets(QualitativeMotionFrame frame, Relation onsets,
        Relation roles, Relation states, Relation stateSchedule, Relation phraseLaw,
        string name = "Rhythm-derived frieze")
    {
        var unresolved = new List<IElement>(); var rows = new List<Relation>();
        var readable = StructuralConstraint.TryReadScope(onsets, out var offered, out var seam, out var tail);
        var sourceClosed = readable && seam.Equals(tail) && offered.Length > 0;
        var repeated = new List<IElement>();
        foreach (var entry in offered)
        {
            if (entry is Relation row)
            {
                if (rows.Any(previous => Same(previous.Left, row.Left))) repeated.Add(row.Left);
                rows.Add(row);
            }
            else { unresolved.Add(entry); rows.Add(new Relation(new Placeholder(), entry)); }
        }
        if (!sourceClosed)
        {
            unresolved.Add(onsets);
            rows.Add(new Relation(tail ?? new Placeholder(), new Placeholder()));
        }
        var slotScope = sourceClosed ? S(rows.Select(row => row.Left).ToArray()) : O(rows.Select(row => row.Left).ToArray());
        var sourceEdges = new List<IElement>();
        for (var i = 1; i < rows.Count; i++) sourceEdges.Add(new Relation(rows[i - 1].Left, rows[i].Left));
        if (sourceClosed) sourceEdges.Add(new Relation(rows[^1].Left, rows[0].Left));
        var sourceCycle = new Relation(slotScope, sourceClosed ? S(sourceEdges.ToArray()) : O(sourceEdges.ToArray()));
        var stateShape = ReadStates(states, out var domain, out var transitions);
        var scheduleClosed = Closed(stateSchedule); var scheduleRows = E(stateSchedule);
        if (!scheduleClosed) unresolved.Add(stateSchedule);
        // Foreign or duplicate schedule rows remain visible even if all known
        // source rows have usable bindings; they do not disappear from a read.
        foreach (var item in scheduleRows)
            if (item is not Relation binding || !rows.Any(row => Same(row.Left, binding.Left))) unresolved.Add(item);
        var bindings = rows.Select(row =>
        {
            var matches = scheduleRows.OfType<Relation>().Where(binding => Same(binding.Left, row.Left)).ToArray();
            return matches.Length == 1 ? matches[0] : new Relation(row.Left, new Placeholder());
        }).ToArray();
        var selections = new List<Relation>(); var allWords = new List<IElement>();
        foreach (var pair in rows.Select((row, index) => (Row: row, Index: index)))
        {
            var row = pair.Row; var index = pair.Index; var binding = bindings[index];
            var pending = new List<IElement>();
            if (!Same(row.Right, roles.Left) && !Same(row.Right, roles.Right)) pending.Add(new Relation(row, roles));
            if (Same(roles.Left, roles.Right)) pending.Add(roles);
            if (repeated.Any(slot => Same(slot, row.Left))) pending.Add(new Relation(row, slotScope));
            var scheduleMatches = scheduleRows.OfType<Relation>().Where(actual => Same(actual.Left, row.Left)).ToArray();
            if (!scheduleClosed || scheduleMatches.Length != 1) pending.Add(new Relation(row, stateSchedule));
            Relation stateRead;
            if (!stateShape)
            { stateRead = new Relation(states, binding); pending.Add(stateRead); }
            else if (index == 0 && !sourceClosed)
            {
                var variable = new Placeholder();
                stateRead = StructuralConstraint.Match(variable, S(variable), domain, new Relation(variable, binding.Right));
                if (!StructuralConstraint.TryReadUnique(stateRead, out _, out _)) pending.Add(stateRead);
            }
            else
            {
                var previous = bindings[index == 0 ? bindings.Length - 1 : index - 1];
                var from = new Placeholder(); var to = new Placeholder();
                stateRead = StructuralConstraint.Match(new Relation(from, to), S(from, to), transitions,
                    new Relation(from, previous.Right));
                if (!StructuralConstraint.TryReadUnique(stateRead, out var instance, out _) || instance is not Relation edge ||
                    !Same(edge.Right, binding.Right) || !E(domain).Any(state => Same(state, binding.Right)) ||
                    !E(domain).Any(state => Same(state, previous.Right))) pending.Add(stateRead);
            }
            var lawRead = FiniteLaw.Evaluate(phraseLaw, S(row.Right), S(binding.Right));
            Relation selectedPhrase = O();
            if (FiniteLaw.TryReadResult(lawRead, out var support, out var residue, out _) && Closed(support) &&
                E(residue).Length == 0 && E(support) is [Relation candidate] &&
                ValidPhrase(frame, candidate)) selectedPhrase = candidate;
            else pending.Add(lawRead);
            if (pending.Count > 0) selectedPhrase = O();
            var strokeWords = Closed(selectedPhrase) ? selectedPhrase : S(O());
            var selection = new Relation(new Relation(new Relation(row, binding), new Relation(stateRead, lawRead)),
                new Relation(selectedPhrase, new Relation(strokeWords, S(pending.ToArray()))));
            selections.Add(selection); allWords.AddRange(E(strokeWords));
            unresolved.AddRange(pending.Select(evidence => (IElement)new Relation(selection, evidence)));
        }
        if (allWords.Count > 64)
            throw new ArgumentOutOfRangeException(nameof(onsets), "This finite interpretation expands beyond the fixture's 64 stroke phases.");
        var words = S(allWords.ToArray()); var fixture = FriezeFixture.Sequence(frame, words, name);
        FiniteLaw.TryRead(fixture.OutputLaw, out _, out _, out _, out var outputRows);
        var phases = E(fixture.Components.Single().Domain); var generation = new List<IElement>(); var cursor = 0;
        foreach (var selection in selections)
        {
            var strokes = (Relation)((Relation)selection.Right).Right;
            foreach (var word in E((Relation)strokes.Left))
            {
                var phase = phases[cursor++];
                var outputRow = outputRows.Single(actual => actual.Left is Relation pair && Same(pair.Left, phase) && ReferenceEquals(pair.Right, fixture.RootPort));
                generation.Add(new Relation(new Relation(selection, word), new Relation(phase, outputRow)));
            }
        }
        return new RhythmFriezeEntry(name, fixture, words, onsets, roles, states, stateSchedule, sourceCycle,
            phraseLaw, S(selections.ToArray()), S(generation.ToArray()), S(unresolved.ToArray()));
    }

    private static bool ValidPhrase(QualitativeMotionFrame frame, Relation phrase) => Closed(phrase) && E(phrase).Length > 0 &&
        E(phrase).All(item => item is Relation word && Closed(word) &&
            E(word).All(move => move is Relation motion && frame.Direction(motion, out _, out _, out _)));

    private static bool ReadStates(Relation states, out Relation domain, out Relation transitions)
    {
        domain = transitions = null!;
        if (states.Left is not Relation { Right: Relation offeredDomain } || states.Right is not Relation edges ||
            !Closed(offeredDomain) || !Closed(edges) || E(offeredDomain).Length == 0 ||
            E(offeredDomain).Select((state, index) => E(offeredDomain).Take(index).Any(previous => Same(previous, state))).Any(same => same) ||
            E(edges).Any(item => item is not Relation edge ||
                !E(offeredDomain).Any(state => Same(state, edge.Left)) || !E(offeredDomain).Any(state => Same(state, edge.Right)))) return false;
        domain = offeredDomain; transitions = edges; return true;
    }

    private static Relation OnsetRows(params IElement[] values) => S(values.Select(value => (IElement)new Relation(new Placeholder(), value)).ToArray());
    private static Relation Schedule(Relation onsets, params IElement[] states) =>
        S(E(onsets).Cast<Relation>().Zip(states).Select(pair => (IElement)new Relation(pair.First.Left, pair.Second)).ToArray());
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation O(params IElement[] entries) => StructuralConstraint.Open(entries);
}
