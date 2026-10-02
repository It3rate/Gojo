using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Structures;

// The host supplies samples, construction, assertions, and reporting only.
// All signal arithmetic runs through attached Gojo relation structures.
var output = Path.GetFullPath("experiments/StreamingScratchpad/out");
Directory.CreateDirectory(output);
var evidence = new List<IElement>();
var report = new List<string>
{
    "# Streaming intercommunication probe",
    "This is a supplied local integer basis, not learned light/heat meaning. The remainder is unrepresented by this basis, not epistemic ignorance.",
    "The host supplies input samples and reports observations. Gojo performs projection, subtraction, reconstruction, publication, and settlement. This run exercises the new live pressure coordinate, coherent arrival, and bounded settlement foundations alongside existing structures.",
    "Recorded states below are historical observations. The separately archived splitter preserves a connected live circuit and its current input; replay must respond to new input rather than reuse those observations.",
    "Reads/publications and conserved transfers are different operations. One deliberately drained live publication is bounded to three passes to expose its continuing drive rather than misreport it as quiet."
};
var checks = 0;
var failures = 0;
Relation Group(params IElement[] parts) => Relation.OrderedGroup(new Carrier(), parts);
Relation Text(string value) => EncodedTextSurface.Create(new Carrier(), value);
Relation State(Carrier carrier) => Group(new CarrierLocation(carrier, (long)carrier.Value), carrier.TensionSlot);
void Check(bool condition, string description)
{
    checks++;
    if (!condition) failures++;
    report.Add($"- {(condition ? "PASS" : "UNEXPECTED")}: {description}");
}
void Observe(string description, params IElement[] parts)
{
    report.Add(description);
    evidence.Add(Group(Text(description), Group(parts)));
}

var source = new Carrier();
var quotient = new Carrier();
var remainder = new Carrier();
var nextReader = new Carrier();
var reconstructed = new Carrier();
var coarse = Equation.Affine(source, 0, 4, quotient, 0, 1);
var coarsePublication = source.AttachLiveOutput(source.ValueSlot, coarse);
var residualEquation = Equation.Sum(remainder,
    Equation.UnitPort(source), Equation.WeightedPort(quotient, 0, 1, 0, -4));
var residualPublication = source.AttachLiveOutput(new Relation(source.ValueSlot, quotient.ValueSlot), residualEquation);
var nextPort = Equation.Affine(remainder, 0, 1, nextReader, 0, 1);
var nextPublication = remainder.AttachLiveOutput(remainder.ValueSlot, nextPort);
var reconstruct = Equation.Sum(reconstructed,
    Equation.WeightedPort(quotient, 0, 1, 0, 4), Equation.UnitPort(remainder));
var reconstructPublication = quotient.AttachLiveOutput(new Relation(quotient.ValueSlot, remainder.ValueSlot), reconstruct);
var circuit = Group(source, quotient, remainder, nextReader, reconstructed,
    coarsePublication, residualPublication, nextPublication, reconstructPublication);
Observe("## Supplied circuit: q=floor(x/4), r=x-4q; another reader observes r, and 4q+r reconstructs x.", circuit);
foreach (var sample in new (long Input, ulong Quotient, ulong Remainder)[] { (11, 2, 3), (12, 3, 0), (5, 1, 1), (0, 0, 0) })
{
    source.Set(sample.Input);
    Observe($"Input {sample.Input}: q={quotient.Value}, r={remainder.Value}, next={nextReader.Value}, reconstructed={reconstructed.Value}; tensions {source.Tension}|{quotient.Tension}|{remainder.Tension}|{nextReader.Tension}|{reconstructed.Tension}.",
        State(source), State(quotient), State(remainder), State(nextReader), State(reconstructed));
    Check(source.Value == (ulong)sample.Input && quotient.Value == sample.Quotient && remainder.Value == sample.Remainder && nextReader.Value == sample.Remainder && reconstructed.Value == (ulong)sample.Input,
        $"Connected publications respond to input {sample.Input} and preserve the source.");
    nextReader.Add(0);
    source.Set(sample.Input);
    Check(quotient.Value == sample.Quotient && remainder.Value == sample.Remainder && nextReader.Value == sample.Remainder && source.Tension == 0 && quotient.Tension == 0 && remainder.Tension == 0 && nextReader.Tension == 0 && reconstructed.Tension == 0,
        "Unchanged publication and settlement with no new input do not accumulate tension.");
}
source.Set(11);
var splitterJson = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("streaming/supplied-splitter", circuit));
File.WriteAllText(Path.Combine(output, "splitter.gojo.json"), splitterJson);
var restoredComplete = GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(splitterJson), out var restored);
Check(restoredComplete && restored.ResidueOrTension.Equals(Relation.Point(Carrier.Unknown, 0)),
    "The archived live circuit reloads completely without unresolved import residue.");
if (restoredComplete && restored.Root is Relation restoredCircuit && restoredCircuit.Right is Carrier restoredHolder &&
    restoredCircuit.TryReadOrderedElementsOn(restoredHolder, out var restoredParts) && restoredParts.Length >= 5 &&
    restoredParts[0] is Carrier restoredSource && restoredParts[1] is Carrier restoredQuotient &&
    restoredParts[2] is Carrier restoredRemainder && restoredParts[3] is Carrier restoredReader && restoredParts[4] is Carrier restoredReconstruction)
{
    Observe("## Archive replay: local ordered participants recover the active circuit; saved numeric observations are not used as answers.", restoredCircuit);
    Check(restoredSource.Value == 11 && restoredQuotient.Value == 2 && restoredRemainder.Value == 3 && restoredReader.Value == 3 && restoredReconstruction.Value == 11 &&
        restoredSource.Tension == 0 && restoredQuotient.Tension == 0 && restoredRemainder.Tension == 0 && restoredReader.Tension == 0 && restoredReconstruction.Tension == 0,
        "Reload preserves the 11 -> quotient 2, remainder 3 state without introducing tension.");
    foreach (var sample in new (long Input, ulong Quotient, ulong Remainder)[] { (12, 3, 0), (5, 1, 1) })
    {
        restoredSource.Set(sample.Input);
        Observe($"Restored input {sample.Input}: q={restoredQuotient.Value}, r={restoredRemainder.Value}, next={restoredReader.Value}, reconstructed={restoredReconstruction.Value}.",
            State(restoredSource), State(restoredQuotient), State(restoredRemainder), State(restoredReader), State(restoredReconstruction));
        Check(restoredSource.Value == (ulong)sample.Input && restoredQuotient.Value == sample.Quotient && restoredRemainder.Value == sample.Remainder && restoredReader.Value == sample.Remainder && restoredReconstruction.Value == (ulong)sample.Input &&
            restoredSource.Tension == 0 && restoredQuotient.Tension == 0 && restoredRemainder.Tension == 0 && restoredReader.Tension == 0 && restoredReconstruction.Tension == 0,
            "Restored attached publications recalculate from the changed input without introducing tension.");
        Check(source.Value == 11 && quotient.Value == 2 && remainder.Value == 3 && nextReader.Value == 3 && reconstructed.Value == 11,
            "Changing the restored circuit does not mutate the original circuit.");
    }
}
else Check(false, "The archive did not expose the expected local ordered participants; replay could not proceed.");

var bareSource = new Carrier();
var bareTarget = new Carrier();
var barePort = Equation.Affine(bareSource, 0, 3, bareTarget, 0, 1);
var barePublication = bareSource.AttachLiveOutput(bareSource.ValueSlot, barePort);
bareSource.Set(5);
var bareRead = barePort.Project(bareSource.ValueSlot, bareTarget);
Observe($"## Bare live projection: source={bareSource.Value}, output={bareTarget.Value}, source tension={bareSource.Tension}.", barePublication, bareRead, State(bareSource), State(bareTarget));
Check(bareRead.TryGetCarrierLocations(out var bareValue, out var bareResidue) && bareValue.Ticks == 1 && bareResidue.Ticks == 2 && bareSource.Tension == 0,
    "A direct read exposes residue 2, but the bare live output does not forward that residue as tension.");

var importedSource = new Carrier();
var importedTarget = new Carrier();
var importPort = Equation.Affine(importedSource, 0, 3, importedTarget, 0, 1);
importedSource.Set(5);
var firstImport = importedTarget.TrySetFrom(importPort, importedSource);
var firstTension = importedSource.Tension;
var secondImport = importedTarget.TrySetFrom(importPort, importedSource);
Observe($"## Repeated explicit import: first source tension={firstTension}, second={importedSource.Tension}, source value={importedSource.Value}, target value={importedTarget.Value}.", importPort, State(importedSource), State(importedTarget));
Check(firstImport && secondImport && firstTension == 2 && importedSource.Tension == 4 && importedSource.Value == 5 && importedTarget.Value == 1,
    "Each import appends the same residue again; repeated observation needs an explicit transfer boundary.");

var slotted = new Carrier();
var keptValueSlot = slotted.ValueSlot;
var keptTensionSlot = slotted.TensionSlot;
var livePressure = slotted.LiveTensionSlot;
var pressureReader = new Carrier();
var stop = slotted.Attach(new Relation(Relation.Point(slotted, 3), Relation.Point(slotted, 3)));
var pressurePublication = slotted.AttachLiveOutput(livePressure, Equation.Affine(slotted, -10, 10, pressureReader, 0, 20));
slotted.Add(8);
Observe($"## Retained slots: value slot={keptValueSlot.Ticks}, old tension slot={keptTensionSlot.Ticks}, fresh tension slot={slotted.TensionSlot.Ticks}.", stop, keptValueSlot, keptTensionSlot, State(slotted));
Check(keptValueSlot.Ticks == 3 && keptTensionSlot.Ticks == 0 && slotted.TensionSlot.Ticks == 5,
    "ValueSlot follows its shared tick; a previously obtained TensionSlot is an observation of its earlier state.");
Check(livePressure.Ticks == 5 && pressureReader.Value == 15, "A connected LiveTensionSlot publishes current pressure through the supplied +10 read frame.");
slotted.Remove(10);
Check(livePressure.Ticks == -2 && pressureReader.Value == 8 && keptTensionSlot.Ticks == 0,
    "Live pressure and its reader follow negative pressure while the old observation stays fixed.");
slotted.Add(2);
Observe($"After pressure returns to zero: live={livePressure.Ticks}, historical={keptTensionSlot.Ticks}, reader={pressureReader.Value}.", pressurePublication, livePressure, keptTensionSlot, State(pressureReader));
Check(livePressure.Ticks == 0 && pressureReader.Value == 10 && keptTensionSlot.Ticks == 0,
    "The same connected pressure coordinate follows recovery to zero without reacquiring its slot.");

var pooled = new Carrier();
var receiver = new Carrier();
var boundary = pooled.Attach(new Relation(Relation.Point(pooled, 10), Relation.Point(pooled, 0)));
pooled.Add(23);
Observe($"## Existing pooled tension before route: value={pooled.Value}, tension={pooled.Tension}.", boundary, State(pooled));
Check(pooled.Value == 3 && pooled.Tension == 20, "Return boundary pools unhandled crossing participation.");
var laterRoute = receiver.Attach(new Relation(boundary, Relation.Point(receiver, 0)));
Observe($"After attaching a compatible route: source value={pooled.Value}, source tension={pooled.Tension}, receiver={receiver.Value}.", laterRoute, State(pooled), State(receiver));
Check(pooled.Value == 3 && pooled.Tension == 0 && receiver.Value == 2,
    "A later route consumes the old pool; three local ticks and two ten-tick groups preserve the supplied participation.");

var offered = new Carrier();
var limited = new Carrier();
var clamp = limited.Attach(new Relation(Relation.Point(limited, 3), Relation.Point(limited, 3)));
var capacityRoute = offered.Attach(new Relation(Relation.Point(offered, 1), Relation.Point(limited, 0)));
offered.Add(8);
Observe($"## Finite acceptance: source value={offered.Value}, retained tension={offered.Tension}, receiver={limited.Value}.", clamp, capacityRoute, State(offered), State(limited));
Check(offered.Value == 0 && offered.Tension == 5 && limited.Value == 3, "Unaccepted transfer stays on the source.");
limited.Remove(2);
Observe($"After receiver removes two ticks: source tension={offered.Tension}, receiver={limited.Value}.", State(offered), State(limited));
Check(offered.Tension == 3 && limited.Value == 3, "Existing tension fills newly available intake without resupplying input.");

var inputA = new Carrier();
var inputB = new Carrier();
var difference = new Carrier();
var differenceReceiver = new Carrier();
var compareInputs = inputA.AttachLiveOutput(new Relation(inputA.ValueSlot, inputB.ValueSlot),
    Equation.Sum(difference, Equation.UnitPort(inputA), Equation.WeightedPort(inputB, 0, 1, 0, -1)));
var differenceRoute = difference.Attach(new Relation(Relation.Point(difference, 1), Relation.Point(differenceReceiver, 0)));
Observe("## Coherent arrivals: two equal sensor increments enter before the difference circuit or its transfer route executes.", compareInputs, differenceRoute);
foreach (var delta in new long[] { 3, 7 })
{
    var together = Carrier.ReceiveTogether(Group(new CarrierLocation(inputA, delta), new CarrierLocation(inputB, delta)));
    Observe($"Equal increments {delta}: inputs {inputA.Value}|{inputB.Value}, difference={difference.Value}, transferred={differenceReceiver.Value}.", together, State(inputA), State(inputB), State(difference), State(differenceReceiver));
    Check(inputA.Value == inputB.Value && difference.Value == 0 && difference.Tension == 0 && differenceReceiver.Value == 0 && SettlementObservation.ObservedQuiet(together),
        "Coherent equal inputs create no transient difference transfer.");
}

var packetSource = new Carrier();
var packetReceiver = new Carrier();
var packetRoute = packetSource.Attach(new Relation(Relation.Point(packetSource, 1), Relation.Point(packetReceiver, 0)));
var identicalArrival = Group(new CarrierLocation(packetSource, 3));
var firstArrival = Carrier.ReceiveTogether(identicalArrival);
Check(packetReceiver.Value == 3 && packetSource.Value == 0 && SettlementObservation.ObservedQuiet(firstArrival),
    "One finite arrival transfers three ticks exactly once through an actual route.");
var secondArrival = Carrier.ReceiveTogether(identicalArrival);
Check(packetReceiver.Value == 6 && packetSource.Value == 0 && SettlementObservation.ObservedQuiet(secondArrival),
    "A second identical arrival is new participation and transfers three more ticks.");
var zeroArrival = Carrier.ReceiveTogether(Group(new CarrierLocation(packetSource, 0)));
Observe($"## Finite arrivals: identical 3,3 then explicit 0 leave receiver={packetReceiver.Value}, source={packetSource.Value}|{packetSource.Tension}.", packetRoute, firstArrival, secondArrival, zeroArrival, State(packetReceiver));
Check(packetReceiver.Value == 6 && packetSource.Tension == 0 && SettlementObservation.ObservedQuiet(zeroArrival),
    "An explicit zero arrival is observed without manufacturing another transfer.");

var driver = new Carrier();
var driven = new Carrier();
var drain = new Carrier();
var drivePublication = driver.AttachLiveOutput(driver.ValueSlot, Equation.Affine(driver, 0, 1, driven, 0, 1));
var drainRoute = driven.Attach(new Relation(Relation.Point(driven, 1), Relation.Point(drain, 0)));
var bounded = Carrier.ReceiveTogether(Group(new CarrierLocation(driver, 1)), maxPasses: 3);
Observe($"## Bounded driven-and-drained publication: driver={driver.Value}, outlet={driven.Value}, accumulated={drain.Value}.", bounded, drivePublication, drainRoute, State(driver), State(driven), State(drain));
Check(!SettlementObservation.ObservedQuiet(bounded) && SettlementObservation.TryRead(bounded, out _, out var used, out var activity, out var continuation) && used.Ticks == 3 && ReferenceEquals(activity, continuation) && drain.Value == 3,
    "Three active passes retain continuation rather than calling this repeatedly driven transfer quiet.");
var activeArchive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("streaming/active-continuation", Group(driver, driven, drain, bounded)));
File.WriteAllText(Path.Combine(output, "active-continuation.gojo.json"), activeArchive);
var activeRestored = GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(activeArchive), out var resumed);
Check(activeRestored && resumed.ResidueOrTension.Equals(Relation.Point(Carrier.Unknown, 0)), "A nonquiet execution witness and its attached world reload without guessed state.");
if (activeRestored && resumed.Root is Relation activeRoot && activeRoot.Right is Carrier activeHolder && activeRoot.TryReadOrderedElementsOn(activeHolder, out var activeParts) && activeParts.Length == 4 &&
    activeParts[0] is Carrier resumedDriver && activeParts[2] is Carrier resumedDrain && activeParts[3] is Relation priorObservation)
{
    Check(resumedDrain.Value == 3 && !SettlementObservation.ObservedQuiet(priorObservation), "Reload retains the prior nonquiet observation and does not replay its arrivals or passes.");
    var nextObservation = resumedDriver.Settle(maxPasses: 3);
    Observe($"Restored continuation after three more passes: accumulated={resumedDrain.Value}; original={drain.Value}.", priorObservation, nextObservation, State(resumedDrain));
    Check(resumedDrain.Value == 6 && !SettlementObservation.ObservedQuiet(nextObservation) && !ReferenceEquals(priorObservation, nextObservation),
        "Settling the restored driver creates a new execution observation and advances only the restored world.");
    Check(drain.Value == 3 && SettlementObservation.TryRead(priorObservation, out _, out var priorUsed, out _, out _) && priorUsed.Ticks == 3,
        "The archived pass count remains historical; the original world remains unchanged.");
}
else Check(false, "The active archive did not expose the expected local participants; continuation replay could not proceed.");

report.Add("## Limits\nThe splitter is a supplied positive integer basis and explicit subtractive reconstruction; it is not a generic automatic residual channel. Current live pressure, coherent input admission, finite arrivals, bounded settlement observations, and executable archive replay are exercised. This run does not learn a model, discover correlated detectors, grow or prune structure, or establish general signed/inverse streaming support.\nReceiveTogether admits a caller-supplied frame coherently; the host still supplies sampling times and does not infer them from the input. Residue is published as a live value in a separate carrier, not automatically pooled as unresolved transfer. Explicit imports still append residue on each call. Turning repeated observations into finite transfers needs an explicit sampling/consumption boundary. Continuation reports active structure, not a learned oscillator or proof of convergence.");
report.Add($"Checks: {checks}; unexpected observations: {failures}.");
File.WriteAllLines(Path.Combine(output, "observations.md"), report);
File.WriteAllText(Path.Combine(output, "evidence.gojo.json"),
    GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("streaming/readiness", Group(evidence.ToArray()))));
Console.WriteLine(string.Join(Environment.NewLine, report));
Environment.ExitCode = failures == 0 ? 0 : 1;
