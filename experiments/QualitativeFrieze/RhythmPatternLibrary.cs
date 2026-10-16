using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation over one actual native port and its words in phase
/// domain order. Starting phase and occurrence order remain separate reads.</summary>
public sealed class RhythmPatternLayer
{
    public string Name { get; }
    public Relation Port { get; }
    public Relation Words { get; }
    public Relation Surface { get; }
    internal RhythmPatternLayer(string name, Relation port, Relation words)
    { Name = name; Port = port; Words = words; Surface = new Relation(port, words); }
}

public sealed class RhythmPatternEntry
{
    // entry = (fixture-surface -> Closed(port -> phase-ordered-words)) ->
    //         (authored-source -> optional-parent-edit)
    public string Name { get; }
    public FriezeFixture Fixture { get; }
    public int PeriodRounds { get; }
    public Relation Layers { get; }
    public IReadOnlyList<RhythmPatternLayer> Components { get; }
    public Relation Surface { get; }
    public Relation Onsets { get; }
    public Relation OnsetRoles { get; }
    public Relation OnsetPhases { get; }
    public Relation EmissionLaw { get; }
    public Relation Generation { get; }
    public Relation Anchors { get; }
    public Relation Unresolved { get; }
    public RhythmPatternEntry? Parent { get; }
    public Relation? EditSurface { get; }

    internal RhythmPatternEntry(string name, FriezeFixture fixture, RhythmPatternLayer[] layers,
        Relation authored, Relation? onsets = null, Relation? roles = null, Relation? onsetPhases = null,
        Relation? emissionLaw = null, Relation? generation = null, Relation? anchors = null,
        Relation? unresolved = null, RhythmPatternEntry? parent = null, Relation? edit = null)
    {
        Name = name; Fixture = fixture; Components = Array.AsReadOnly(layers);
        Layers = S(layers.Select(layer => (IElement)layer.Surface).ToArray());
        Onsets = onsets ?? S(); OnsetRoles = roles ?? S(); OnsetPhases = onsetPhases ?? S();
        EmissionLaw = emissionLaw ?? fixture.OutputLaw; Generation = generation ?? S();
        Anchors = anchors ?? new Relation(S(), S()); Unresolved = unresolved ?? S(); Parent = parent; EditSurface = edit;
        Surface = new Relation(new Relation(fixture.Surface, Layers), new Relation(authored, edit ?? S()));
        // This is a diagnostic of this builder's full direct fanout, not an
        // execution schedule or a general period algorithm for arbitrary routes.
        PeriodRounds = fixture.Components.Select(clock => E(clock.Domain).Length).Aggregate(1, Lcm);
    }

    private static int Lcm(int a, int b)
    {
        var x = a; var y = b;
        while (y != 0) { var next = x % y; x = y; y = next; }
        return checked(a / x * b);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

/// <summary>Authored phase rhythms acting additively on one sampled cursor.
/// Names and diagnostic round counts do not decide emissions or event routes.
/// Independent component plots are observations of these same actual ports,
/// not additional independently evolving material cursors.</summary>
public sealed class RhythmPatternLibrary
{
    // source-layer = actual-port -> ordered-word-cycle
    // onset = actual-slot -> actual-hit/rest participant
    // onset-phases = actual-slot -> Closed(actual-rise-phase, actual-return-phase)
    // generation = ((actual-slot -> pulse-leg) -> actual-phase) ->
    //              (finite-emission-read -> actual-output-law-row)
    // anchor premise = Closed(selected source slots) -> Closed(other source slots)
    // anchors = Closed(selected actual rise phases) -> Closed(other actual rise phases)
    // library = (frame -> shared-reaches) -> (entry-surfaces -> edits)
    public QualitativeMotionFrame Frame { get; }
    public IElement HorizontalReach { get; }
    public IElement VerticalReach { get; }
    public IReadOnlyList<RhythmPatternEntry> Entries { get; }
    public IReadOnlyList<RhythmPatternEntry> Patterns => Entries;
    public Relation Edits { get; }
    public Relation Surface { get; }

    public RhythmPatternLibrary(QualitativeMotionFrame? frame = null,
        IElement? horizontalReach = null, IElement? verticalReach = null)
    {
        Frame = frame ?? new QualitativeMotionFrame(); HorizontalReach = horizontalReach ?? new Placeholder();
        VerticalReach = verticalReach ?? new Placeholder();
        var r = QualitativeMotionFrame.Move(Frame.Right, HorizontalReach);
        var u = QualitativeMotionFrame.Move(Frame.Up, VerticalReach);
        var d = QualitativeMotionFrame.Move(Frame.Down, VerticalReach);
        var pause = S(); var root = S(S(r));
        var slow = S(S(u, u, u, u), pause, pause, pause, pause, pause, pause, pause,
            S(d, d, d, d), pause, pause, pause, pause, pause, pause, pause);
        var fast = S(S(u), S(d));
        var middle = S(S(u), S(u), S(u), S(u), S(d), S(d), S(d), S(d));
        var layered = Layered("Slow square excursion + fast triangle", root, S(slow, fast),
            ["Horizontal drift", "Slow square excursion", "Fast triangle"]);
        var fastLayer = layered.Components[^1];
        var fastClock = layered.Fixture.Components.Single(clock => ReferenceEquals(clock.Surface, fastLayer.Port.Right));
        var shiftedStarts = S(E(layered.Fixture.Starts).Cast<Relation>().Select(binding => ReferenceEquals(binding.Left, fastLayer.Port)
            ? (IElement)new Relation(binding.Left, S(E(fastClock.Domain)[0])) : binding).ToArray());
        var shiftedFixture = layered.Fixture.WithStarts(shiftedStarts, "Same layers — fast phase opposed");
        var phaseEdit = new Relation(new Relation(layered.Fixture.Surface, shiftedFixture.Surface),
            new Relation(layered.Fixture.Starts, shiftedStarts));
        var shifted = new RhythmPatternEntry(shiftedFixture.Name, shiftedFixture, layered.Components.ToArray(),
            new Relation(layered.Surface, phaseEdit), parent: layered, edit: phaseEdit);
        var three = Layered("Slow square + middle triangle + fast triangle", root, S(slow, middle, fast),
            ["Horizontal drift", "Slow square excursion", "Middle triangle", "Fast triangle"]);

        var hit = new Placeholder(); var rest = new Placeholder(); var roles = new Relation(hit, rest);
        var regular = AuthoredOnsets("Sixteen-slot regular pulses", roles,
            S(hit, rest, rest, rest, hit, rest, rest, rest, hit, rest, rest, rest, hit, rest, rest, rest),
            [0, 4, 8, 12]);
        var grouped = AuthoredOnsets("Grouped 3–3–2 pulses", roles,
            S(hit, rest, rest, hit, rest, rest, hit, rest), [0, 2, 4, 6]);
        Entries = Array.AsReadOnly(new[] { layered, shifted, three, regular, grouped });
        Edits = S(phaseEdit);
        Surface = new Relation(new Relation(Frame.Surface, S(HorizontalReach, VerticalReach)),
            new Relation(S(Entries.Select(entry => (IElement)entry.Surface).ToArray()), Edits));
    }

    private RhythmPatternEntry Layered(string name, Relation root, Relation cycles, string[] labels)
    {
        var fixture = FriezeFixture.LayeredCycles(Frame, root, cycles, name);
        var layers = ReadLayers(fixture, S(new IElement[] { root }.Concat(E(cycles)).ToArray()), labels);
        return new(name, fixture, layers, new Relation(S(layers.Select(layer => (IElement)layer.Words).ToArray()), fixture.OutputLaw));
    }

    private RhythmPatternEntry AuthoredOnsets(string name, Relation roles, Relation values, int[] selectedSlots)
    {
        var slots = E(values).Select(_ => (IElement)new Placeholder()).ToArray();
        var onsets = S(slots.Zip(E(values)).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        // These finite authored selections are discarded as indexes once their
        // actual source-slot relations are connected. No runtime clock uses them.
        var primary = S(selectedSlots.Select(index => slots[index]).ToArray());
        var selected = E(primary);
        var other = S(slots.Where(slot => !selected.Any(actual => ReferenceEquals(actual, slot))).ToArray());
        return FromOnsets(Frame, onsets, roles, new Relation(primary, other), HorizontalReach, VerticalReach, name);
    }

    /// <summary>Compiles an authored binary onset scope into two explicit phase
    /// legs per slot. A hit emits up then down; a rest has two explicit empty
    /// words. Open tails, malformed entries, foreign roles and invalid anchor
    /// premises remain connected uncertainty, rather than becoming rests.
    /// A closed attempted control cycle does not certify an open source's period.
    /// </summary>
    public static RhythmPatternEntry FromOnsets(QualitativeMotionFrame frame, Relation onsets,
        Relation roles, Relation anchorPremise, IElement horizontalReach, IElement verticalReach,
        string name = "Authored binary onset cycle")
    {
        var pending = new List<IElement>(); var sourceRows = new List<Relation>();
        var sourceClosed = StructuralConstraint.TryReadScope(onsets, out var entries, out _, out var tail) && Closed(onsets);
        if (entries.Length > 32) throw new ArgumentOutOfRangeException(nameof(onsets), "At most 32 source onset slots are supported.");
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var item in entries)
        {
            if (item is Relation row && seen.Add(row.Left)) sourceRows.Add(row);
            else { pending.Add(item); sourceRows.Add(new Relation(new Placeholder(), new Placeholder())); }
        }
        if (!sourceClosed || sourceRows.Count == 0)
        {
            pending.Add(onsets);
            sourceRows.Add(new Relation(tail ?? new Placeholder(), new Placeholder()));
        }
        if (sourceRows.Count > 32) throw new ArgumentOutOfRangeException(nameof(onsets), "The retained open-tail obligation needs one of the 32 slots.");
        var rise = new Placeholder(); var fall = new Placeholder(); var legs = S(rise, fall);
        var up = S(QualitativeMotionFrame.Move(frame.Up, verticalReach));
        var down = S(QualitativeMotionFrame.Move(frame.Down, verticalReach)); var pause = S();
        var law = FiniteLaw.Create(S(roles.Left, roles.Right), legs, S(up, down, pause),
            FiniteLaw.Row(roles.Left, rise, S(up)), FiniteLaw.Row(roles.Left, fall, S(down)),
            FiniteLaw.Row(roles.Right, rise, S(pause)), FiniteLaw.Row(roles.Right, fall, S(pause)));
        var reads = new List<(Relation Slot, IElement Leg, Relation Read, Relation Word)>();
        foreach (var row in sourceRows)
            foreach (var leg in E(legs))
            {
                var read = FiniteLaw.Evaluate(law, S(row.Right), S(leg));
                Relation word;
                if (!Same(roles.Left, roles.Right) && FiniteLaw.TryReadResult(read, out var support, out var residue, out _) &&
                    Closed(support) && E(support) is [Relation actual] && Closed(actual) && E(residue).Length == 0) word = actual;
                else { word = StructuralConstraint.Open(); pending.Add(read); }
                reads.Add((row, leg, read, word));
            }
        var root = S(S(QualitativeMotionFrame.Move(frame.Right, horizontalReach)));
        var words = S(reads.Select(read => (IElement)read.Word).ToArray());
        var fixture = FriezeFixture.LayeredCycles(frame, root, S(words), name);
        var layers = ReadLayers(fixture, S(root, words), ["Horizontal drift", "Binary pulse legs"]);
        var pulse = layers[1].Port;
        var clock = fixture.Components.Single(component => ReferenceEquals(component.Surface, pulse.Right));
        var phases = E(clock.Domain);
        FiniteLaw.TryRead(fixture.OutputLaw, out _, out _, out _, out var outputRows);
        var generation = new List<IElement>(); var correspondences = new List<IElement>();
        for (var i = 0; i < reads.Count; i++)
        {
            var source = reads[i]; var phase = phases[i];
            var outputRow = outputRows.Single(row => row.Left is Relation input && ReferenceEquals(input.Left, phase) && ReferenceEquals(input.Right, pulse));
            generation.Add(new Relation(new Relation(new Relation(source.Slot.Left, source.Leg), phase), new Relation(source.Read, outputRow)));
        }
        for (var i = 0; i < sourceRows.Count; i++)
            correspondences.Add(new Relation(sourceRows[i].Left, S(phases[2 * i], phases[2 * i + 1])));
        var selected = new List<IElement>(); var other = new List<IElement>(); var anchored = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        if (anchorPremise.Left is Relation first && anchorPremise.Right is Relation second && Closed(first) && Closed(second))
        {
            ReadAnchors(first, selected); ReadAnchors(second, other);
            if (anchored.Count != sourceRows.Count) pending.Add(anchorPremise);
        }
        else pending.Add(anchorPremise);
        var anchors = new Relation(S(selected.ToArray()), S(other.ToArray()));
        var onsetPhases = S(correspondences.ToArray()); var generated = S(generation.ToArray()); var unresolved = S(pending.ToArray());
        var authored = new Relation(new Relation(onsets, new Relation(roles, legs)),
            new Relation(law, new Relation(onsetPhases, new Relation(generated,
                new Relation(new Relation(anchorPremise, anchors), unresolved)))));
        return new(name, fixture, layers, authored, onsets, roles, onsetPhases, law, generated, anchors, unresolved);

        void ReadAnchors(Relation scope, List<IElement> target)
        {
            foreach (var source in E(scope))
            {
                var index = sourceRows.FindIndex(row => Same(row.Left, source));
                if (index < 0 || !anchored.Add(source)) { pending.Add(source); continue; }
                target.Add(phases[index * 2]);
            }
        }
    }

    private static RhythmPatternLayer[] ReadLayers(FriezeFixture fixture, Relation cycles, string[] names)
    {
        var ports = E(fixture.Network.Ports).Cast<Relation>().ToArray(); var words = E(cycles).Cast<Relation>().ToArray();
        return ports.Select((port, index) => new RhythmPatternLayer(names[index], port, words[index])).ToArray();
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
