using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;
using PoetryScratchpad;

// A constructed reading experiment, not a language parser. The host supplies
// interpretations, schedules native reads, and formats observations. All domain
// arithmetic and binding substitution below execute through Gojo structure.
var output = Path.GetFullPath("experiments/PoetryScratchpad/out");
Directory.CreateDirectory(output);
var notebook = new List<IElement>();
var report = new List<string>
{
    "# Poetry scratchpad observations",
    "Constructed from the supplied PDFs and the older reader letter. Numbers except the stated depth and textual counts are experimental knobs, not measured poetic meaning or confidence.",
    "Descriptions are reporting adapters. The archived relations contain each source, supplied correspondence, calculation, and unresolved participant. No kernel changes were made for this experiment."
};
var checks = 0;
Relation Group(params IElement[] parts) => Relation.OrderedGroup(new Carrier(), parts);
Relation Words(string words) => EncodedTextSurface.Create(new Carrier(), words);
Relation Source(string document, long page, string excerpt) =>
    Group(Words(document), new CarrierLocation(new Carrier(), page), Words(excerpt));
void Check(bool condition, string description)
{
    checks++;
    if (!condition) throw new InvalidOperationException(description);
}
void Record(string description, IElement evidence)
{
    report.Add(description);
    notebook.Add(new Relation(evidence, Words(description)));
    Console.WriteLine(description);
}
string Read(Relation circuit)
{
    if (!QualifiedReadSurface.TryReadValue(circuit, out var value)) return "unresolved";
    return QualifiedReadSurface.TryReadBounds(circuit, out var low, out var high)
        ? $"{value.Ticks}/12 with alternatives [{low.Ticks}/12, {high.Ticks}/12]"
        : $"{value.Ticks}/12 with unresolved variation";
}
Relation Observation(Relation circuit)
{
    // An explicit observation of one read, never an input cache for the next.
    // Derived amounts and interval endpoints were calculated by the native read.
    if (!QualifiedReadSurface.TryRead(circuit, out var selected, out var basis, out var value,
        out var variation, out var coverage, out var scope, out var excluded, out var provenance))
        return ReadWitness.Create(new Carrier(), circuit, circuit, new Placeholder(), circuit, circuit);
    return QualifiedReadSurface.Create(new Carrier(), selected, basis, value, variation,
        coverage, scope, excluded, Group(provenance, circuit, Words("Historical local observation; execute the connected circuit to obtain a current answer.")));
}
long Knob(string name, long fallback)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? long.Parse(args[index + 1]) : fallback;
}
Relation Observe(IElement source, Carrier axis, IElement value, IElement variation, IElement scope) =>
    QualifiedReadSurface.Create(new Carrier(), source, Relation.Segment(axis, 0, 12), value,
        variation, new Placeholder(), scope, new Placeholder(), source);

var snagged = Source("Snagged, supplied PDF", 1,
    "it sinks 27 feet ... Probably having caught my own words ... Anchored to a fixed position deep down towards the dark mouth at the end of the line, I am snagged.");
var unsettling = Source("Upon leaving the clockworks, supplied PDF", 3,
    "Even the birds that appear ready-made quit clutching the branch, unsettling to fly.");
var hands = Source("Upon leaving the clockworks, supplied PDF", 4,
    "The body holds a universe: two-fisted hands, both planets, head, feet, all worlds held in place");
var sun = Source("Upon leaving the clockworks, supplied PDF", 4,
    "The sun rises, repeats, firing real bullets instead of blanks");
var sparrow = Source("Upon leaving the clockworks, supplied PDF", 2,
    "the sparrow even closer albeit biblical with less inclination to fly or in ways less visible");

// 1. A line can be under load without identifying what is at its other end.
var angler = new Carrier();
var line = new Carrier();
var target = new Placeholder();
var emergence = new Placeholder();
var encounter = Group(new Relation(angler, line), new Relation(line, target));
var requestedLanding = new Relation(encounter, emergence);
var fish = new Carrier();
var bottom = new Carrier();
var fishBinding = StructuralPattern.Bind(target, fish);
var bottomBinding = StructuralPattern.Bind(target, bottom);
var fishReading = StructuralPattern.Apply(requestedLanding, fishBinding);
var bottomReading = StructuralPattern.Apply(requestedLanding, bottomBinding);
Check(!StructuralPattern.IsComplete(requestedLanding), "The original text branch retains missing referents.");
Check(!StructuralPattern.IsComplete(requestedLanding, fishBinding), "Supplying fish identity does not supply a landed fish event.");
Check(StructuralPattern.ReadSlots(fishReading).Any(slot => Equals(slot, emergence)), "Emergence remains open in the fish reading.");
var feet = new Carrier();
var depth = Relation.Segment(feet, 0, 27);
var oppositeTraversal = Relation.Segment(feet, 27, 0);
Check(depth.FlowOn(feet).Ticks == 27 && oppositeTraversal.FlowOn(feet).Ticks == -27,
    "Reversing traversal changes directed displacement, not the identity of the target.");
Record($"1. Snagged: depth reads {depth.FlowOn(feet).Ticks} feet; reversed traversal reads {oppositeTraversal.FlowOn(feet).Ticks}. This constructed landing query has {StructuralPattern.ReadSlots(requestedLanding).Length} open slots. Supplying a fish leaves {StructuralPattern.ReadSlots(fishReading).Length} open landing-event slot. A bottom-object branch is retained separately. No surfaced or dying fish follows from these bindings.",
    Group(snagged, requestedLanding, depth, oppositeTraversal,
        ReadWitness.Create(new Carrier(), snagged, fishBinding, fishReading, emergence,
            Words("Fish identity supplied by the older reader letter; emergence is not asserted by the PDF.")),
        ReadWitness.Create(new Carrier(), snagged, bottomBinding, bottomReading, emergence,
            Words("Bottom-object identity is an analyst alternative, not a resolved referent."))));
StructuralPattern.TryReadBoundSlot(target, [fishBinding, bottomBinding], out var first);
StructuralPattern.TryReadBoundSlot(target, [bottomBinding, fishBinding], out var reversed);
Check(ReferenceEquals(first, fish) && ReferenceEquals(reversed, bottom), "Single-frame binding is first-wins, not ambiguity settlement.");
Record("Observed weakness: putting both identities into one binding frame silently selects the first. Reversing the frame changes the selected identity. The experiment therefore keeps alternatives in separate connected branches; a successful binding is not evidence that a reading is true.",
    Group(requestedLanding, Group(fishBinding, bottomBinding), Group(bottomBinding, fishBinding), first, reversed));

// 2. Coordinate sign, contextual face and lexical negation stay separate.
var grip = new Carrier();
var opposedGrip = grip.Inverse();
var positiveGrip = new CarrierLocation(grip, 4);
var negativeGrip = positiveGrip.Dual();
var positiveOpposed = new CarrierLocation(opposedGrip, 4);
var doubleNegative = negativeGrip.Dual();
Check(doubleNegative == positiveGrip, "Signed negation is involutive.");
Check(!positiveOpposed.ReadFrom(grip).IsVisible, "An opposed face is not silently a negative forward value.");
var wordScope = Group(unsettling, Words("Declared attachment-scale illustration. Neither lexical anxiety nor actual flight is quantified by these ticks."));
var held = Observe(unsettling, grip, new CarrierLocation(grip, 8), Relation.Segment(grip, 7, 9), wordScope);
var release = Observe(unsettling, grip, new CarrierLocation(grip, 6), Relation.Segment(grip, 5, 7), wordScope);
var remaining = QualifiedReadSurface.Subtract(new Carrier(), held, release);
var flightConditions = new Placeholder();
var takeoff = ReadWitness.Create(new Carrier(), unsettling, Group(remaining, flightConditions),
    new Placeholder(), flightConditions, wordScope);
Record($"2. Unsettling to fly: supplied grip 8/12 minus release 6/12 reads {Read(remaining)}. This is reduced attachment, not proof of airborne motion. Signed sequence is {positiveGrip.Ticks}, {negativeGrip.Ticks}, {doubleNegative.Ticks} on one face; positive {positiveOpposed.Ticks} on the inverse face remains distinct. The proposed four-stage emotional cycle is not generated by negation alone.",
    Group(unsettling, Group(positiveGrip, negativeGrip, doubleNegative, positiveOpposed), remaining, takeoff));

// 3 and 4: partial metaphor and native feedback (implemented separately).
PoetryPatterns.Run(hands, sun, Record);

// 5. An explicitly declared cross-level coupling, with live inputs and bounds.
// The physical and interpretive carriers are distinct until a correspondence
// is supplied. Neither the denominator nor repeated reads imply confidence.
var force = new Carrier();
var interpretation = new Carrier();
var pull = new CarrierLocation(force, new TickRef(8));
var lowPull = new CarrierLocation(force, new TickRef(6));
var highPull = new CarrierLocation(force, new TickRef(10));
var forceScope = Group(snagged, Words("Toy line-load model: 12 ticks is a declared reference load, not a measured rod capacity."));
var load = Observe(snagged, force, pull, new Relation(lowPull, highPull), forceScope);
var capacity = Observe(snagged, force, new CarrierLocation(force, 10), Relation.Segment(force, 10, 10), forceScope);
var reserve = QualifiedReadSurface.Subtract(new Carrier(), capacity, load);
var readerScope = Group(snagged, Words("Reader hypothesis: felt entanglement receives line-load and recalled-words contributions. Additive unit coupling is stipulated, not inferred from language."));
var remembered = Observe(snagged, interpretation, new CarrierLocation(interpretation, 4),
    Relation.Segment(interpretation, 2, 6), readerScope);
var unaligned = QualifiedReadSurface.Add(new Carrier(), load, remembered);
Check(!QualifiedReadSurface.TryReadValue(unaligned, out _), "Different domains require a supplied bridge.");
var bridge = Equation.Affine(force, 0, 12, interpretation, 0, 12);
var viewedLoad = QualifiedReadSurface.Reframe(new Carrier(), load, bridge);
var theme = QualifiedReadSurface.Add(new Carrier(), viewedLoad, remembered);
var restoredCapacity = QualifiedReadSurface.Add(new Carrier(), reserve, load);
var wordRung = StructuralLadderSurface.BaseRung(new Carrier(), snagged);
var selectedLoad = FocusSurface.Create(new Carrier(), wordRung, bridge, load, remembered,
    new Placeholder(), new Relation(load, viewedLoad), new Placeholder(), readerScope);
Check(StructuralLadderSurface.TryCreateRefinedRungFromFocus(new Carrier(), selectedLoad, theme,
    new Relation(load, viewedLoad), new Placeholder(), readerScope, out var themeRung), "Connected reading levels remain inspectable.");
Record($"5. Before a supplied bridge, force plus interpretation is unresolved. With the declared bridge, reserve = {Read(reserve)} and the constructed entanglement read = {Read(theme)}. Values above 12 are allowed: this is an amount on a reference scale, not a probability or bounded quality rating. Theme correspondence and contribution rule are analyst hypotheses.",
    Group(unaligned, bridge, themeRung, reserve, Observation(reserve), Observation(theme)));
Record($"Dependency weakness: (capacity minus load) plus that same load reads {Read(restoredCapacity)}. The nominal amount cancels, but interval propagation across the two paths does not recover their shared dependency. This is conservative overstatement of variation, not new poetic ambiguity or independent evidence.",
    Group(load, capacity, reserve, restoredCapacity, Observation(restoredCapacity)));

// Explicit historical evidence is a named archive of this observation stage.
// The live circuit in the final notebook will change; the old caption must not
// be mistaken for a claim about that circuit's later state.
var liveRoot = Group(pull, lowPull, highPull, reserve, theme);
File.WriteAllText(Path.Combine(output, "before-poke.gojo.json"),
    GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("poetry/before-poke", liveRoot)));
report.Add("The before-poke.gojo.json archive preserves the first source state. The final notebook contains live circuits at their later source state; earlier report captions are historical observations, not cached current answers.");

pull.Tick.Set(Knob("--pull", 11));
lowPull.Tick.Set(Knob("--low", 9));
highPull.Tick.Set(Knob("--high", 13));
Record($"5. After editing the same source coordinates: reserve = {Read(reserve)}; constructed entanglement = {Read(theme)}. No derived answer was installed or replaced. Overlapping support and coverage gaps remain attached; nominal readability does not certify them.",
    Group(load, reserve, theme, Observation(reserve), Observation(theme)));

var archive = GojoStructureCompactJson.FromElement("poetry/live-cross-level-circuit", liveRoot);
File.WriteAllText(Path.Combine(output, "live-circuit.gojo.json"), GojoStructureCompactJson.Serialize(archive));
Check(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(archive)), out var restored), "Executable circuit reload succeeds.");
var restoredRoot = (Relation)restored.Root;
Check(restoredRoot.TryReadOrderedElementsOn((Carrier)restoredRoot.Right, out var restoredParts), "Reloaded local circuit ports remain inspectable.");
((CarrierLocation)restoredParts[0]).Tick.Set(5);
((CarrierLocation)restoredParts[1]).Tick.Set(3);
((CarrierLocation)restoredParts[2]).Tick.Set(7);
Check(QualifiedReadSurface.TryReadValue((Relation)restoredParts[3], out var newReserve) && newReserve.Ticks == 5, "Reloaded reserve responds to its own changed source.");
Check(QualifiedReadSurface.TryReadValue((Relation)restoredParts[4], out var newTheme) && newTheme.Ticks == 9, "Reloaded theme follows the connected changed source.");
Record($"Preservation check: the reloaded graph, poked independently to load 5/12, reads reserve {Read((Relation)restoredParts[3])} and entanglement {Read((Relation)restoredParts[4])}. This checks circuit behavior, not literary validity.", restoredRoot);

// Deliberately distinguish actual carrier settlement tension from interpretive
// alternatives or supplied variation. This is a separate mechanical analogy.
var receiving = new Carrier();
var clamp = receiving.Attach(new Relation(Relation.Point(receiving, 10), Relation.Point(receiving, 10)));
receiving.Add(13);
Record($"Settlement contrast: a separate clamped carrier receiving 13 has value {receiving.Value} | tension {receiving.Tension}. The interval alternatives and open referents above are inspectable structure, but they do not automatically exert this kind of settlement pressure.",
    Group(snagged, clamp, receiving, receiving.ValueSlot, receiving.TensionSlot));

// 6. A textually signaled allusion still lacks a particular historical binding.
var historicalSource = new Placeholder();
var allusion = new Relation(sparrow, historicalSource);
var allusionFocus = FocusSurface.Create(new Carrier(), sparrow, allusion, allusion,
    Group(sparrow, Words("The adjective biblical licenses an allusive reading, not a unique verse, doctrine or historical event.")),
    new Placeholder(), new Relation(sparrow, historicalSource), historicalSource, sparrow);
Check(!StructuralPattern.IsComplete(allusion), "The allusion has no supplied historical-source witness.");
Record("6. Biblical sparrow: an explicit textual cue is linked to an open historical-source slot. No verse, theology, political allegory, or authorial intention is silently filled in. This is a represented request for correspondence; no historical inference has executed.", allusionFocus);

var full = GojoStructureCompactJson.FromElement("poetry/indicative-experiments", Group(notebook.ToArray()));
File.WriteAllText(Path.Combine(output, "notebook.gojo.json"), GojoStructureCompactJson.Serialize(full));
report.Add($"Harness checks passed: {checks}. These check representation and execution, not agreement with a correct poem interpretation.");
File.WriteAllText(Path.Combine(output, "observations.md"), string.Join("\n\n", report));
Console.WriteLine($"Passed {checks} main-harness checks. Evidence: {output}");
