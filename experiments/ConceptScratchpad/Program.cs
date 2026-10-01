using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

if (args.FirstOrDefault() == "audit-information")
{
    InformationAudit.Run();
    return;
}
if (args.FirstOrDefault() == "audit-read-boundaries")
{
    ReadBoundaryAudit.Run();
    return;
}

// A hand-authored experiment, not a language parser or a general theorem prover.
// C# constructs, enumerates, inspects, checks, and reports. All domain arithmetic,
// comparisons, extrema, projection, and settlement run through existing Gojo APIs.
// No Person/Fact/TruthState objects or symbolic operation dispatch are introduced.
var notebook = new List<IElement>();
var observations = new List<string>();
var checks = 0;

Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);

void Check(bool condition, string message)
{
    checks++;
    if (!condition)
        throw new InvalidOperationException($"Experiment check failed: {message}");
}

CarrierLocation Result(Relation witness)
{
    Check(Equation.TryReadPredictionWitness(witness, out _, out _, out var result, out _, out _),
        "prediction witness remains locally readable");
    return result;
}

Relation Read(Relation equation, IElement input, Carrier output)
{
    Check(Equation.TryPredict(equation, input, output, out var prediction), "equation accepts input");
    Check(Equation.TryCreatePredictionWitness(new Carrier(), equation, input, prediction, out var witness),
        "prediction retains input, surface, result, residue and provenance");
    return witness;
}

Relation Difference(CarrierLocation subtrahend, CarrierLocation minuend, Carrier output) =>
    Read(Equation.Difference(subtrahend.Carrier, minuend.Carrier, output),
        new Relation(subtrahend, minuend), output);

Relation Sum(CarrierLocation left, CarrierLocation right, Carrier output) =>
    Read(Equation.Sum(output, Equation.UnitPort(left.Carrier), Equation.UnitPort(right.Carrier)),
        new Relation(left, right), output);

void CheckSettledClaim(Relation claim, long expectedViolation)
{
    Check(OperationClaim.TryRead(claim, out _, out var residual, out _, out _, out var violation, out var residue),
        "claim is readable");
    Check(residual is CarrierLocation, "claim has a scalar residual, not unresolved structure");
    Check(residue is CarrierLocation { Ticks: 0 }, "claim has no unaccounted read residue");
    Check(violation.Ticks == expectedViolation, "claim violation matches experiment expectation");
}

void Record(string text, IElement structure)
{
    notebook.Add(structure);
    observations.Add(text);
    Console.WriteLine(text);
}

// Numeric accounting: the carry relation, not host division/modulo, finds digits.
var ones = new Carrier();
var tens = new Carrier(ones.UnitShape);
var carry = ones.Attach(new Relation(Relation.Point(ones, 10), Relation.Point(tens, 0)));
ones.Add(234);
Check(ones.Value == 4 && tens.Value == 23 && ones.Tension == 0, "carry accounts for 234");
Record($"Carry: 234 input ticks -> ones {ones.Value}, tens {tens.Value}, tension {ones.Tension}.",
    Group(carry, ones.ValueSlot, tens.ValueSlot, ones.TensionSlot));

// One common height axis, with named participant anchors attached to their spans.
// The origin is Mary's height: a coordinate choice, not an invented absolute height.
// Positive gaps are premises. John and Sue occupy negative positions relative to Mary.
Relation HeightWorld(CarrierLocation maryJohnGap, CarrierLocation marySueGap, Carrier axis,
    out Relation answer, long origin = 0)
{
    var mary = Relation.Point(axis, origin);
    var johnRead = Difference(maryJohnGap, new CarrierLocation(axis, origin), axis);
    var sueRead = Difference(marySueGap, new CarrierLocation(axis, origin), axis);
    var john = Result(johnRead);
    var sue = Result(sueRead);
    answer = Difference(sue, john, axis);
    // Re-read both premises from the resulting physical endpoints.
    var firstPremise = Difference(john, new CarrierLocation(axis, origin), axis);
    var secondPremise = Difference(sue, new CarrierLocation(axis, origin), axis);
    Check(Result(firstPremise).Ticks == maryJohnGap.Ticks, "Mary-John premise reconstructs");
    Check(Result(secondPremise).Ticks == marySueGap.Ticks, "Mary-Sue premise reconstructs");
    return ReadWitness.Create(new Carrier(),
        Group(
            new Relation(EncodedTextSurface.Create(new Carrier(), "Mary"), new Relation(mary, mary)),
            new Relation(EncodedTextSurface.Create(new Carrier(), "John"),
                new Relation(mary, new Relation(john, axis))),
            new Relation(EncodedTextSurface.Create(new Carrier(), "Sue"),
                new Relation(mary, new Relation(sue, axis))),
            firstPremise, secondPremise),
        Group(johnRead, sueRead), Result(answer),
        Relation.Point(Carrier.Unknown, 0), answer);
}

var height = new Carrier(UnitShape.LinearAt(Relation.Point(new Carrier(), 0)));
var exactWorld = HeightWorld(new CarrierLocation(height, 2), new CarrierLocation(height, 10),
    height, out var exactAnswer);
var exactGap = Result(exactAnswer);
var exactClaim = OperationClaim.AtLeast(new Carrier(), exactGap, new CarrierLocation(height, 1), new Carrier(height.UnitShape));
Check(exactGap.Ticks == 8, "exact gap is eight ticks");
CheckSettledClaim(exactClaim, 0);
Record($"Exact illustration: Mary-John 2, Mary-Sue 10 -> John-Sue {exactGap.Ticks} ticks.",
    Group(exactWorld, exactClaim));

var shiftedWorld = HeightWorld(new CarrierLocation(height, 2), new CarrierLocation(height, 10),
    height, out var shiftedAnswer, origin: 137);
Check(Result(shiftedAnswer).Ticks == exactGap.Ticks, "answer is independent of the arbitrary height origin");
Record($"Change of origin: moving Mary's coordinate from 0 to 137 leaves John-Sue at {Result(shiftedAnswer).Ticks}.", shiftedWorld);

// Closed segments are finite, discrete uncertainty domains for this experiment.
// They are NOT built-in meanings of 'a bit' or 'much'. Every candidate is read;
// Gojo tournaments compute extrema, retaining their winning witnesses.
void ReadGapDomains(string name, Relation smallDomain, Relation largeDomain,
    long expectedMinimum, long expectedMaximum)
{
    var smallStart = ((Relation)smallDomain.Left).LeftLocation;
    var smallEnd = ((Relation)smallDomain.Right).LeftLocation;
    var largeStart = ((Relation)largeDomain.Left).LeftLocation;
    var largeEnd = ((Relation)largeDomain.Right).LeftLocation;
    var axis = smallStart.Carrier;
    Check(axis.SameIdentityAs(largeStart.Carrier), "both qualifiers have an explicit shared scale");
    var entries = new List<Relation>();
    var reads = new List<IElement>();
    for (var small = smallStart.Ticks; small <= smallEnd.Ticks; small++)
    for (var large = largeStart.Ticks; large <= largeEnd.Ticks; large++)
    {
        var first = new CarrierLocation(axis, small);
        var second = new CarrierLocation(axis, large);
        var firstAdmission = OperationClaim.Between(new Carrier(), first, smallStart, smallEnd, new Carrier(axis.UnitShape));
        var secondAdmission = OperationClaim.Between(new Carrier(), second, largeStart, largeEnd, new Carrier(axis.UnitShape));
        Check(OperationClaim.TryReadBetween(firstAdmission, out _, out _, out _, out _, out _, out var firstViolation, out _)
            && firstViolation.Ticks == 0, "first candidate is admitted by its domain");
        Check(OperationClaim.TryReadBetween(secondAdmission, out _, out _, out _, out _, out _, out var secondViolation, out _)
            && secondViolation.Ticks == 0, "second candidate is admitted by its domain");
        var world = HeightWorld(first, second, axis, out var answer);
        var admitted = Group(firstAdmission, secondAdmission, world);
        reads.Add(admitted);
        entries.Add(ExtremaTournament.Entry(admitted, Result(answer)));
    }
    var minimum = ExtremaTournament.Minimum(new Carrier(), entries, out _);
    var maximum = ExtremaTournament.Maximum(new Carrier(), entries, out _);
    Check(ExtremaTournament.TryReadWinner(minimum, out var minEntry), "minimum has a witness");
    Check(ExtremaTournament.TryReadWinner(maximum, out var maxEntry), "maximum has a witness");
    Check(ExtremaTournament.TryReadEntry(minEntry, out _, out var min), "minimum value is readable");
    Check(ExtremaTournament.TryReadEntry(maxEntry, out _, out var max), "maximum value is readable");
    Check(min.Ticks == expectedMinimum && max.Ticks == expectedMaximum, "domain extrema match expectation");
    var positiveFloor = OperationClaim.AtLeast(new Carrier(), min, new CarrierLocation(axis, 1), new Carrier(axis.UnitShape));
    // Retain the claim even when it is violated. Its failure is part of the result.
    Check(OperationClaim.TryRead(positiveFloor, out _, out _, out _, out _, out var violation, out _), "floor claim is readable");
    Record($"{name}: {entries.Count} worlds; John-Sue range [{min.Ticks}, {max.Ticks}]; positive-floor violation {violation.Ticks}.",
        ReadWitness.Create(new Carrier(), Group(smallDomain, largeDomain), Group(reads.ToArray()),
            Group(minimum, maximum), positiveFloor, Group(entries.ToArray())));
}

ReadGapDomains("Shared non-overlapping qualifier bands", Relation.Segment(height, 1, 3), Relation.Segment(height, 8, 12), 5, 11);
ReadGapDomains("Overlapping qualifier bands", Relation.Segment(height, 1, 6), Relation.Segment(height, 4, 9), -2, 8);
ReadGapDomains("Only Mary is taller than both", Relation.Segment(height, 1, 6), Relation.Segment(height, 1, 6), -5, 5);

// Remove arbitrary lexical thresholds: construct the larger gap from a positive
// smaller gap plus a positive excess. These are finite witnesses, not a theorem
// about all unbounded gaps. The language-to-gap-order binding is still supplied.
var orderedGapWitnesses = new List<IElement>();
for (long small = 1; small <= 4; small++)
for (long excess = 1; excess <= 4; excess++)
{
    var first = new CarrierLocation(height, small);
    var extra = new CarrierLocation(height, excess);
    var largerRead = Sum(first, extra, height);
    var world = HeightWorld(first, Result(largerRead), height, out var answer);
    var cancellation = OperationClaim.Equality(new Carrier(), Result(answer), extra, new Carrier(height.UnitShape));
    CheckSettledClaim(cancellation, 0);
    orderedGapWitnesses.Add(Group(largerRead, world, cancellation));
}
Record($"Ordered gaps: {orderedGapWitnesses.Count} witnesses; if the Mary-Sue gap is the Mary-John gap plus a positive excess, John-Sue recovers that excess.",
    Group(orderedGapWitnesses.ToArray()));

// A low-resolution zero must not be silently promoted to exact equality.
var coarse = new Carrier(UnitShape.ResolutionOf(height.UnitShape, 10));
var coarseEquation = Equation.Affine(height, 0, 10, coarse, 0, 1);
var coarseRead = Read(coarseEquation, new CarrierLocation(height, 1), coarse);
Check(Equation.TryReadPredictionWitness(coarseRead, out _, out _, out var coarseValue, out var coarseResidue, out _), "coarse read is inspectable");
Check(coarseValue.Ticks == 0 && coarseResidue is CarrierLocation { Ticks: 1 }, "coarse zero preserves the fine difference");
Record($"Resolution: a 1-tick positive gap reads as {coarseValue.Ticks} coarse ticks with 1 fine tick of residue.", coarseRead);

// Same unit shape is not the same comparison frame. Quiet violation alone is
// insufficient evidence for truth; unresolved residue must also be inspected.
var otherFrame = new Carrier(height.UnitShape);
var unmatched = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(height, 10),
    new CarrierLocation(otherFrame, 2), new Carrier(height.UnitShape));
Check(OperationClaim.TryRead(unmatched, out _, out var unmatchedResidual, out _, out _, out var unmatchedViolation, out var unmatchedResidue), "unmatched claim is readable");
Check(unmatchedResidual is Relation && unmatchedResidue is Relation && unmatchedViolation.Ticks == 0,
    "unmatched frames retain unresolved structure despite zero violation");
var alignment = Read(Equation.Affine(otherFrame, 0, 1, height, 0, 1), new CarrierLocation(otherFrame, 2), height);
var aligned = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(height, 10), Result(alignment), new Carrier(height.UnitShape));
CheckSettledClaim(aligned, 0);
Record("Basis: same-unit carriers without alignment leave an unresolved claim (despite zero violation); an explicit alignment enables the comparison.",
    Group(unmatched, alignment, aligned));

// Missing premise: preserve the open slot without displaying a guessed number.
var firstSlot = UnitShape.DescriptorSlot(0);
var secondSlot = UnitShape.DescriptorSlot(1);
var openInput = new Relation(firstSlot, secondSlot);
var openEquation = Equation.Difference(height, height, height);
var firstBinding = StructuralPattern.BindDescriptor(0, new CarrierLocation(height, 2));
var complete = EquationPattern.TryPredict(openEquation, openInput, height, out var partial,
    out var unbound, firstBinding);
Check(!complete && unbound.Length == 1, "missing premise remains an open slot");
Check(!Equation.TryReadPrediction(partial, out _, out _), "partial prediction remains nonnumeric");
Check(StructuralPattern.ReadSlots(partial).Length > 0, "missing input remains traversable");
Record($"Missing premise: complete={complete}; {unbound.Length} unbound slot; no numeric answer is established.",
    ReadWitness.Create(new Carrier(), Group(openInput, firstBinding), openEquation, partial,
        Group(partial, Group(unbound)), Group(unbound)));
var boundComplete = EquationPattern.TryPredict(openEquation, openInput, height, out var boundPrediction,
    out var boundUnbound, firstBinding, StructuralPattern.BindDescriptor(1, new CarrierLocation(height, 10)));
Check(boundComplete && boundUnbound.Length == 0, "supplying the missing premise resolves the slots");
Check(Equation.TryReadPrediction(boundPrediction, out var boundValue, out _) && boundValue.Ticks == 8,
    "complete binding reads the expected difference");
Record($"Resolved premise: supplying the second gap 10 gives complete={boundComplete}, difference {boundValue.Ticks}.",
    Group(openInput, firstBinding, StructuralPattern.BindDescriptor(1, new CarrierLocation(height, 10)), boundPrediction));

// A contradictory directed cycle: A-B=2, B-C=3, C-A=1. Following the closed
// route must return to its origin. Gojo sums the displacements and reads closure.
var cycleAxis = new Carrier(height.UnitShape);
var firstEdge = new CarrierLocation(cycleAxis, 2);
var secondEdge = new CarrierLocation(cycleAxis, 3);
var thirdEdge = new CarrierLocation(cycleAxis, 1);
var pairRead = Sum(firstEdge, secondEdge, cycleAxis);
var cycleRead = Sum(Result(pairRead), thirdEdge, cycleAxis);
var a = Relation.Point(new Carrier(), 0);
var b = Relation.Point(new Carrier(), 0);
var c = Relation.Point(new Carrier(), 0);
var cycle = Group(new Relation(new Relation(b, a), firstEdge),
    new Relation(new Relation(c, b), secondEdge), new Relation(new Relation(a, c), thirdEdge));
var closure = OperationClaim.Equality(new Carrier(), Result(cycleRead), new CarrierLocation(cycleAxis, 0), new Carrier(height.UnitShape));
CheckSettledClaim(closure, 6);
Record($"Contradictory cycle: closure displacement {Result(cycleRead).Ticks}; equality-to-origin violation 6, retained as claim structure.",
    ReadWitness.Create(new Carrier(), cycle, Group(pairRead, cycleRead), closure, closure, cycle));

// A different idea: two requests each fit a shared resource, but together do not.
var capacity = new Carrier();
var clamp = capacity.Attach(new Relation(Relation.Point(capacity, 10), Relation.Point(capacity, 10)));
var firstRequest = new CarrierLocation(capacity, 7);
var secondRequest = new CarrierLocation(capacity, 6);
var firstFit = OperationClaim.AtMost(new Carrier(), firstRequest, new CarrierLocation(capacity, 10), new Carrier(capacity.UnitShape));
var secondFit = OperationClaim.AtMost(new Carrier(), secondRequest, new CarrierLocation(capacity, 10), new Carrier(capacity.UnitShape));
CheckSettledClaim(firstFit, 0);
CheckSettledClaim(secondFit, 0);
var totalRequest = Sum(firstRequest, secondRequest, capacity);
var combinedFit = OperationClaim.AtMost(new Carrier(), Result(totalRequest), new CarrierLocation(capacity, 10), new Carrier(capacity.UnitShape));
CheckSettledClaim(combinedFit, 3);
capacity.Add(7);
capacity.Add(6);
Check(capacity.Value == 10 && capacity.Tension == 3, "unaccommodated requests remain real carrier tension");
Record($"Shared capacity: each request fits; combined request {Result(totalRequest).Ticks}, settled {capacity.Value}, unresolved tension {capacity.Tension}.",
    Group(clamp, firstFit, secondFit, totalRequest, combinedFit, capacity.ValueSlot, capacity.TensionSlot));

// Compare a cached witness with an actual replay, including negative weights.
var compact = GojoStructureCompactJson.FromElement("concept-scratchpad/exact-height", exactWorld);
var compactJson = GojoStructureCompactJson.Serialize(compact);
var reloaded = GojoStructureCompactJson.ToElement(GojoStructureCompactJson.Deserialize(compactJson));
Check(ReadWitness.TryRead((Relation)reloaded.Root, out _, out _, out var reloadedResult, out _, out var reloadedProvenance), "height witness reloads");
Check(reloadedResult is CarrierLocation { Ticks: 8 }, "reloaded height result remains eight");
Check(Equation.TryReadPredictionWitness((Relation)reloadedProvenance, out var replayInput, out var replayEquation, out var replayResult, out _, out _), "reloaded calculation can be discovered");
var replayed = Result(Read(replayEquation, replayInput, replayResult.Carrier));
Check(replayed.Ticks == 8, "compact serialization preserves executable direction");
Record($"Persistence: compact exact-height witness retains cached answer {replayResult.Ticks}; re-executing the final equation returns {replayed.Ticks}.", reloaded.Root);

var directed = Relation.Segment(height, 0, -1);
var reloadedDirected = (Relation)GojoStructureCompactJson.ToElement(
    GojoStructureCompactJson.FromElement("concept-scratchpad/direction-probe", directed)).Root;
var reloadedAxis = ((Relation)reloadedDirected.Left).LeftLocation.Carrier;
Check(directed.FlowOn(height).Ticks == -1 && reloadedDirected.FlowOn(reloadedAxis).Ticks == -1,
    "compact segment round-trip retains negative flow");
Record($"Persistence direction probe: segment 0 -> -1 has flow {directed.FlowOn(height).Ticks}; reloaded segment has flow {reloadedDirected.FlowOn(reloadedAxis).Ticks}.",
    Group(directed, reloadedDirected));

// Recorded accounting and attached settlement structure survive together.
var compactCapacity = GojoStructureCompactJson.FromElement("concept-scratchpad/capacity-probe", capacity);
var reloadedCapacity = (Carrier)GojoStructureCompactJson.ToElement(compactCapacity).Root;
Check(reloadedCapacity.Value == capacity.Value && reloadedCapacity.Tension == capacity.Tension,
    "compact loader restores carrier accounting state");
Record($"Persistence: compact capacity retains value={capacity.Value}, tension={capacity.Tension}; restored value={reloadedCapacity.Value}, tension={reloadedCapacity.Tension}.",
    Group(capacity, reloadedCapacity));

var outputDirectory = Path.GetFullPath(args.Length == 0 ? "experiments/ConceptScratchpad/out" : args[0]);
Directory.CreateDirectory(outputDirectory);
File.WriteAllText(Path.Combine(outputDirectory, "exact-height.compact.gojo.json"), compactJson);
File.WriteAllText(Path.Combine(outputDirectory, "notebook.snapshot.gojo.json"),
    JsonSerializer.Serialize(GojoEncodedSnapshot.Capture(Group(notebook.ToArray())), new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(outputDirectory, "results.md"),
    "# Gojo conceptual scratchpad: observed results\n\n" + string.Join("\n\n", observations) +
    $"\n\nCompleted {checks} harness checks. These are finite experiments, not an unbounded logical proof.\n");
Console.WriteLine($"Completed {checks} checks. Evidence: {outputDirectory}");
