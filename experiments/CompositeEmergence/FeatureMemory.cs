using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace CompositeEmergence;

/// <summary>A bounded operational memory, separate from learned parameters.
/// The retained coarse feature and prior resource sign occupy native signed
/// TickRefs; availability and elapsed observations occupy physical carriers.
/// Publications expose only the selected coordinate and a boundary event.</summary>
public sealed class FeatureMemory
{
    public const int AgeCapacity = 4096;
    public Carrier FeatureAxis { get; }
    public Carrier AvailableAxis { get; }
    public Carrier AgeAxis { get; }
    public Carrier OutcomeAxis { get; }
    private readonly Carrier actualAge, available, outcomeKnown, featureRole, ageRole, outcomeRole;
    private readonly Relation featureSite, outcomeSite, featurePolicy, agePolicy, outcomePolicy, limits;
    private Relation lastRetain, lastAdvance, lastOutcome, lastReset;
    private readonly Relation[] lastReads;
    public Relation Root { get; private set; }
    public long ActualAge => checked((long)actualAge.Value);
    public long AgeOverflow => actualAge.Tension;
    public int LastOutcomeNow => checked((int)((CarrierLocation)outcomeSite.Left).Ticks);

    public FeatureMemory(Carrier featureAxis, Carrier availableAxis, Carrier ageAxis, Carrier outcomeAxis)
    {
        FeatureAxis = featureAxis; AvailableAxis = availableAxis; AgeAxis = ageAxis; OutcomeAxis = outcomeAxis;
        actualAge = new(); available = new(availableAxis.UnitShape); outcomeKnown = new();
        featureRole = new(); ageRole = new(); outcomeRole = new();
        actualAge.Attach(new Relation(Relation.Point(actualAge, AgeCapacity), Relation.Point(actualAge, AgeCapacity)));
        available.Attach(new Relation(Relation.Point(available, 1), Relation.Point(available, 1)));
        outcomeKnown.Attach(new Relation(Relation.Point(outcomeKnown, 1), Relation.Point(outcomeKnown, 1)));
        featureSite = Relation.Point(FeatureAxis, 0); outcomeSite = Relation.Point(OutcomeAxis, 0);
        featurePolicy = SupportDecisionSurface.Create(new Carrier(), SupportDecisionSurface.Rule(new Carrier(), featureSite.Left,
            SupportDecisionSurface.Condition(new Carrier(), featureRole, Relation.Segment(available, 1, 1))));
        agePolicy = SupportDecisionSurface.Create(new Carrier(),
            AgeRule(0, 0, 0), AgeRule(1, 3, 1), AgeRule(4, 7, 2), AgeRule(8, AgeCapacity, 3));
        // The initial public zero is a declared no-prior-outcome context. Its
        // unresolved source is retained by the fallback selection; it is not
        // inferred evidence that a resource event had zero value.
        outcomePolicy = SupportDecisionSurface.Create(new Carrier(), SupportDecisionSurface.Rule(new Carrier(), outcomeSite.Left,
            SupportDecisionSurface.Condition(new Carrier(), outcomeRole, Relation.Segment(outcomeKnown, 1, 1))),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(OutcomeAxis, 0)));
        limits = NativeGraph.G(Relation.Point(actualAge, AgeCapacity), Relation.Segment(FeatureAxis, -16, 16),
            Relation.Segment(AvailableAxis, 0, 1), Relation.Segment(AgeAxis, 0, 3), Relation.Segment(OutcomeAxis, -1, 1));
        lastRetain = lastAdvance = lastOutcome = lastReset = NativeGraph.Open(limits, new Placeholder());
        lastReads = Enumerable.Range(0, 4).Select(_ => NativeGraph.Open(limits, new Placeholder())).ToArray();
        Root = BuildRoot();
    }

    private FeatureMemory(Relation root)
    {
        var fields = NativeGraph.Parts(root, 14); var axes = NativeGraph.Parts((Relation)fields[0], 4).Cast<Carrier>().ToArray();
        FeatureAxis = axes[0]; AvailableAxis = axes[1]; AgeAxis = axes[2]; OutcomeAxis = axes[3];
        actualAge = (Carrier)fields[1]; available = (Carrier)fields[2]; featureSite = (Relation)fields[3]; outcomeSite = (Relation)fields[4]; outcomeKnown = (Carrier)fields[5];
        var roles = NativeGraph.Parts((Relation)fields[6], 3).Cast<Carrier>().ToArray(); featureRole = roles[0]; ageRole = roles[1]; outcomeRole = roles[2];
        var policies = NativeGraph.Parts((Relation)fields[7], 3).Cast<Relation>().ToArray(); featurePolicy = policies[0]; agePolicy = policies[1]; outcomePolicy = policies[2];
        limits = (Relation)fields[8]; lastRetain = (Relation)fields[9]; lastAdvance = (Relation)fields[10]; lastOutcome = (Relation)fields[11];
        lastReads = NativeGraph.Parts((Relation)fields[12], 4).Cast<Relation>().ToArray(); lastReset = (Relation)fields[13]; Root = root;
    }

    public static FeatureMemory FromRoot(Relation root) => new(root);

    public Relation ReadFeature()
    {
        var admitted = NativeGraph.Publication(available, checked((long)available.Value), new Carrier());
        var selection = SupportDecisionSurface.Select(new Carrier(), featurePolicy,
            SupportDecisionSurface.Binding(new Carrier(), featureRole, admitted));
        return PublishSelection(selection, FeatureAxis, 0);
    }

    public Relation Available()
    {
        var calibration = Equation.Affine(available, 0, 1, AvailableAxis, 0, 1);
        var prediction = Equation.Predict(calibration, new CarrierLocation(available, checked((long)available.Value)), AvailableAxis);
        lastReads[1] = prediction; Root = BuildRoot();
        return NativeGraph.TryValue(prediction, AvailableAxis, out var value)
            ? NativeGraph.Publication(AvailableAxis, value.Ticks, new Carrier(), ActualAge)
            : NativeGraph.Open(new Carrier(), Relation.Segment(AvailableAxis, 0, 1));
    }

    public Relation Age()
    {
        var elapsed = NativeGraph.Publication(actualAge, ActualAge, new Carrier(), ActualAge);
        var selection = SupportDecisionSurface.Select(new Carrier(), agePolicy,
            SupportDecisionSurface.Binding(new Carrier(), ageRole, elapsed));
        return PublishSelection(selection, AgeAxis, 2);
    }

    public Relation PreviousOutcome()
    {
        var known = NativeGraph.Publication(outcomeKnown, checked((long)outcomeKnown.Value), new Carrier());
        var selection = SupportDecisionSurface.Select(new Carrier(), outcomePolicy,
            SupportDecisionSurface.Binding(new Carrier(), outcomeRole, known));
        return PublishSelection(selection, OutcomeAxis, 3);
    }

    public Relation Retain(Relation feature)
    {
        var boundary = NativeGraph.CapturePublication(feature, FeatureAxis);
        if (!NativeGraph.TryValue(boundary, FeatureAxis, out var value) || value.Ticks is < -16 or > 16)
        {
            var before = checked((long)available.Value); available.Set(0);
            lastRetain = NativeGraph.Open(NativeGraph.G(boundary, Relation.Point(AvailableAxis, before)), limits);
            Root = BuildRoot(); return lastRetain;
        }
        var featurePoint = (CarrierLocation)featureSite.Left; var beforeFeature = new CarrierLocation(FeatureAxis, featurePoint.Ticks);
        var beforeAvailable = new CarrierLocation(AvailableAxis, checked((long)available.Value));
        var beforeAge = CaptureAge();
        featurePoint.Tick.Set(value.Ticks); available.Set(1); ForgetAge();
        lastRetain = ReadWitness.Create(new Carrier(), boundary, new Relation(beforeFeature, featureSite),
            new CarrierLocation(FeatureAxis, value.Ticks), NativeGraph.G(),
            NativeGraph.G(beforeFeature, new CarrierLocation(FeatureAxis, value.Ticks), beforeAvailable,
                Relation.Point(AvailableAxis, checked((long)available.Value)), beforeAge, CaptureAge()));
        Root = BuildRoot(); return lastRetain;
    }

    public Relation Advance()
    {
        var before = CaptureAge(); actualAge.Add(1);
        lastAdvance = ReadWitness.Create(new Carrier(), before, Relation.Point(actualAge, 1), CaptureAge(),
            new CarrierLocation(actualAge, actualAge.Tension), NativeGraph.G(before, CaptureAge(), limits));
        Root = BuildRoot(); return lastAdvance;
    }

    public Relation RememberOutcome(Relation grossResource)
    {
        var sign = OutcomeRetrodiction.ResourceSign(grossResource, OutcomeAxis);
        if (!NativeGraph.TryValue(sign, OutcomeAxis, out var value))
        { lastOutcome = NativeGraph.Open(sign, outcomePolicy); Root = BuildRoot(); return lastOutcome; }
        var point = (CarrierLocation)outcomeSite.Left; var before = new CarrierLocation(OutcomeAxis, point.Ticks);
        var oldKnown = new CarrierLocation(outcomeKnown, checked((long)outcomeKnown.Value));
        point.Tick.Set(value.Ticks); outcomeKnown.Set(1);
        lastOutcome = ReadWitness.Create(new Carrier(), sign, new Relation(before, outcomeSite), new CarrierLocation(OutcomeAxis, value.Ticks),
            NativeGraph.G(), NativeGraph.G(before, new CarrierLocation(OutcomeAxis, value.Ticks), oldKnown,
                new CarrierLocation(outcomeKnown, checked((long)outcomeKnown.Value))));
        Root = BuildRoot(); return lastOutcome;
    }

    public void Reset()
    {
        var before = NativeGraph.G(CaptureAge(), Relation.Point(AvailableAxis, checked((long)available.Value)),
            Relation.Point(FeatureAxis, ((CarrierLocation)featureSite.Left).Ticks), Relation.Point(OutcomeAxis, LastOutcomeNow));
        ForgetAge(); available.Set(0); outcomeKnown.Set(0);
        ((CarrierLocation)featureSite.Left).Tick.Set(0); ((CarrierLocation)outcomeSite.Left).Tick.Set(0);
        lastRetain = lastAdvance = lastOutcome = NativeGraph.Open(limits, new Placeholder());
        for (var index = 0; index < lastReads.Length; index++) lastReads[index] = NativeGraph.Open(limits, new Placeholder());
        lastReset = ReadWitness.Create(new Carrier(), before, limits,
            NativeGraph.G(CaptureAge(), Relation.Point(AvailableAxis, 0)), before, limits); Root = BuildRoot();
    }

    public static Relation InferFromOutcome(Relation chosenAction, Relation goal, Relation grossResource,
        Carrier actionAxis, Carrier goalAxis, Carrier featureAxis) =>
        OutcomeRetrodiction.InferFromOutcome(chosenAction, goal, grossResource, actionAxis, goalAxis, featureAxis);

    private Relation AgeRule(long lower, long upper, long bucket) => SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(AgeAxis, bucket),
        SupportDecisionSurface.Condition(new Carrier(), ageRole, Relation.Segment(actualAge, lower, upper)));
    private Relation PublishSelection(Relation selection, Carrier axis, int slot)
    {
        // Public coordinates are frozen: retargeting retained internal sites
        // later does not change a publication already delivered to a receiver.
        lastReads[slot] = ReadWitness.Create(new Carrier(), selection, limits,
            NativeGraph.TryValue(selection, axis, out var value) ? new CarrierLocation(axis, value.Ticks) : new Placeholder(),
            slot == 2 ? new CarrierLocation(actualAge, actualAge.Tension) : NativeGraph.G(), selection);
        Root = BuildRoot();
        return NativeGraph.TryValue(lastReads[slot], axis, out var exposed)
            ? NativeGraph.Publication(axis, exposed.Ticks, new Carrier(), ActualAge)
            : NativeGraph.Open(new Carrier(), Relation.Segment(axis, -16, 16));
    }
    private Relation CaptureAge() => NativeGraph.G(new CarrierLocation(actualAge, ActualAge), new CarrierLocation(actualAge, actualAge.Tension));
    private void ForgetAge()
    {
        // An explicit opposing packet cancels retained age and clipped overflow.
        // The caller captures both before the reset; no hidden clear operation.
        actualAge.Remove(checked((ulong)checked(ActualAge + actualAge.Tension)));
    }
    private Relation BuildRoot() => NativeGraph.G(NativeGraph.G(FeatureAxis, AvailableAxis, AgeAxis, OutcomeAxis), actualAge, available,
        featureSite, outcomeSite, outcomeKnown, NativeGraph.G(featureRole, ageRole, outcomeRole), NativeGraph.G(featurePolicy, agePolicy, outcomePolicy),
        limits, lastRetain, lastAdvance, lastOutcome, NativeGraph.G(lastReads), lastReset);
}

/// <summary>An authored outcome-only benchmark for source wiring +1.
/// A public action, goal and resource sign reveal the previous rule through
/// a native product. This is not a learned sensor service or a hidden-mode port.</summary>
public static class OutcomeRetrodiction
{
    public static Relation ResourceSign(Relation grossResource, Carrier signAxis)
    {
        if (!ReadWitness.TryRead(grossResource, out _, out _, out var result, out _, out _) ||
            !NativeGraph.TryPoint(result, out var exposed) || !NativeGraph.TryValue(grossResource, exposed.Carrier, out _))
            return NativeGraph.Open(NativeGraph.Identity(grossResource), Relation.Segment(signAxis, -1, 1));
        var axis = exposed.Carrier; var role = new Carrier();
        var rules = SupportDecisionSurface.Create(new Carrier(),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(signAxis, -1),
                SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(axis, long.MinValue, -1))),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(signAxis, 0),
                SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(axis, 0, 0))),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(signAxis, 1),
                SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(axis, 1, long.MaxValue))));
        var boundary = NativeGraph.CapturePublication(grossResource, axis);
        var selection = SupportDecisionSurface.Select(new Carrier(), rules, SupportDecisionSurface.Binding(new Carrier(), role, boundary));
        return ReadWitness.Create(new Carrier(), boundary, rules,
            NativeGraph.TryValue(selection, signAxis, out var sign) ? new CarrierLocation(signAxis, sign.Ticks) : new Placeholder(), selection, selection);
    }

    public static Relation InferFromOutcome(Relation chosenAction, Relation goal, Relation grossResource,
        Carrier actionAxis, Carrier goalAxis, Carrier featureAxis)
    {
        var signAxis = new Carrier(); var sign = ResourceSign(grossResource, signAxis);
        var product = Equation.Product(featureAxis, Equation.UnitPort(actionAxis), Equation.UnitPort(goalAxis), Equation.UnitPort(signAxis));
        if (!NativeGraph.TryValue(chosenAction, actionAxis, out var action) || action.Ticks is not (-1 or 1) ||
            !NativeGraph.TryValue(goal, goalAxis, out var desired) || desired.Ticks is not (-1 or 1) ||
            !NativeGraph.TryValue(sign, signAxis, out var signed) || signed.Ticks is not (-1 or 1))
            return NativeGraph.Open(NativeGraph.G(NativeGraph.Identity(chosenAction), NativeGraph.Identity(goal), sign), product);
        var input = new Relation(new Relation(new CarrierLocation(actionAxis, action.Ticks), new CarrierLocation(goalAxis, desired.Ticks)),
            new CarrierLocation(signAxis, signed.Ticks));
        var prediction = Equation.Predict(product, input, featureAxis);
        return ReadWitness.Create(new Carrier(), NativeGraph.G(NativeGraph.CapturePublication(chosenAction, actionAxis),
            NativeGraph.CapturePublication(goal, goalAxis), sign), product,
            NativeGraph.TryValue(prediction, featureAxis, out var value) ? new CarrierLocation(featureAxis, value.Ticks) : new Placeholder(), prediction, prediction);
    }
}
