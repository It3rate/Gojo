using Gojo.Core;
using Gojo.Patterns;
using SER = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored source premises for inspecting local implication and
/// conditional completion. Names are presentation only. Premises are supplied
/// explicitly through the native publication; none is discovered from pixels,
/// example identity, or the arbitrary diagram proportions.</summary>
public static class StructuralPotentialStudy
{
    public sealed record Case(string Id, string Name, string Description,
        StructuralPotential.Frame Frame, SER.Publication Source, Relation AllowedAssumptions);

    public static IReadOnlyList<Case> Cases()
    {
        var result = new List<Case>();
        Add("P01", "Separated line families", "Two distinct straight families are independent. Their finite coverage, relative angle and spacing are not supplied. Only coverage assumptions are permitted.",
            (f, facts) => { }, f => f.SpanCoverage);
        Add("P02", "Right-angle relation supplied", "The source additionally supplies a right-angle relation. Relative spacing remains uncommitted, so a rectangular completion does not establish a square.",
            (f, facts) => facts.Add((f.RightAngle, "Yes")), f => f.SpanCoverage);
        Add("P03", "Equal spacing supplied too", "Right-angle and cross-family equal-spacing promises are explicit source premises. Finite coverage still needs witnessed conditional assumptions.",
            (f, facts) => { facts.Add((f.RightAngle, "Yes")); facts.Add((f.EqualSpacing, "Yes")); }, f => f.SpanCoverage);
        Add("P04", "Unequal rectangular spacing", "Finite coverage and right angle are supplied; cross-family equal spacing is explicitly refuted. A square completion cannot override that evidence.",
            (f, facts) => { Cover(f, facts); facts.Add((f.RightAngle, "Yes")); facts.Add((f.EqualSpacing, "No")); }, _ => []);
        Add("P05", "One finite support stops short", "A supplied coverage refusal blocks a finite boundary even though its line-family potential survives. An assumption is not permission to reverse a known refusal.",
            (f, facts) => { Cover(f, facts, 0); facts.Add((f.RightAngle, "Yes")); facts.Add((f.EqualSpacing, "Yes")); }, f => f.Leaves.Select(p => p.Predicate));
        Add("P06", "Conflicting angle accounts", "Both positive and negative right-angle premises are retained. The boundary query need not consult this conflict; a more specific angle-dependent query exposes it.",
            (f, facts) => { Cover(f, facts); facts.Add((f.RightAngle, "Yes")); facts.Add((f.RightAngle, "No")); facts.Add((f.EqualSpacing, "Yes")); }, f => [f.RightAngle]);
        Add("P07", "Coincident sides in one family", "Separate transverse positions in the first family are explicitly refuted. Distinct support identities may still occupy the same position; that does not supply two separate sides for this boundary construction.",
            (f, facts) => { Set(facts, f.SeparateA, "No"); Cover(f, facts); facts.Add((f.RightAngle, "Yes")); }, f => f.Leaves.Select(p => p.Predicate));
        Add("P08", "Uniform repetitions within each family", "Each family has its own uniformity promise. Those two promises do not establish equality between the families. The square question must still ask for that distinction.",
            (f, facts) => { facts.Add((f.UniformA, "Yes")); facts.Add((f.UniformB, "Yes")); facts.Add((f.RightAngle, "Yes")); }, f => f.SpanCoverage);
        Add("P09", "Independence left open", "Straightness and within-family distinctness are supplied, but independence between the families is not. Coverage assumptions alone cannot fill that missing structural premise.",
            (f, facts) => { facts.RemoveAll(row => ReferenceEquals(row.Predicate, f.Independent)); facts.Add((f.RightAngle, "Yes")); }, f => f.SpanCoverage);
        Add("P10", "Shape choice deliberately delegated", "A finite boundary is supported. Its angle and spacing are open; this receiver may explicitly assume them when a more specific completion is requested.",
            (f, facts) => Cover(f, facts), f => [f.RightAngle, f.EqualSpacing]);
        Add("P11", "One assumption remains unauthorized", "Angle and equal spacing are supplied, but only three of four finite-coverage assumptions are permitted. The last obligation must remain unresolved.",
            (f, facts) => { facts.Add((f.RightAngle, "Yes")); facts.Add((f.EqualSpacing, "Yes")); }, f => f.SpanCoverage.Take(3));
        Add("P12", "Repetition without independent families", "Both families may be uniform while their independence is explicitly refuted. Uniform repetition does not supply another independent direction.",
            (f, facts) => { Set(facts, f.Independent, "No"); facts.Add((f.UniformA, "Yes")); facts.Add((f.UniformB, "Yes")); Cover(f, facts); }, f => f.Leaves.Select(p => p.Predicate));
        return result.AsReadOnly();

        void Add(string id, string name, string description,
            Action<StructuralPotential.Frame, List<(Relation Predicate, string Answer)>> modify,
            Func<StructuralPotential.Frame, IEnumerable<Relation>> allowed)
        {
            var frame = new StructuralPotential.Frame();
            var facts = new List<(Relation Predicate, string Answer)> {
                (frame.Straight, "Yes"), (frame.SeparateA, "Yes"), (frame.SeparateB, "Yes"), (frame.Independent, "Yes") };
            modify(frame, facts);
            result.Add(new(id, name, description, frame, frame.Publish(facts.ToArray()),
                StructuralConstraint.Closed(allowed(frame).Cast<IElement>().ToArray())));
        }
    }

    private static void Cover(StructuralPotential.Frame frame, List<(Relation Predicate, string Answer)> facts, int refused = -1)
    {
        for (var index = 0; index < frame.SpanCoverage.Count; index++) facts.Add((frame.SpanCoverage[index], index == refused ? "No" : "Yes"));
    }

    private static void Set(List<(Relation Predicate, string Answer)> facts, Relation predicate, string answer)
    { facts.RemoveAll(row => ReferenceEquals(row.Predicate, predicate)); facts.Add((predicate, answer)); }
}
