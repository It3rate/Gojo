using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A selected noisy compatibility model over a frozen receiver pool.
/// The native assessments supply the evidence; no teacher graph, geometry or
/// family label enters this read. Estimates are authored, not calibrated.</summary>
public static class ShapeEvidenceWeighting
{
    // pool = exposed focus -> Closed(model snapshot -> repeated proposal snapshots)
    // assessment = (exposed evidence port -> model) -> (matched -> (contradicted -> open))
    // run = (pool -> channel/prior premises) -> (state -> Closed(update receipts))
    public sealed record Plan(string Id, string Name, int StrongHit, int NormalHit, int WeakHit,
        int Denominator, bool CompactPrior = false)
    {
        public string Premise => $"Estimated compatibility channel: strong {StrongHit}/{Denominator}, ordinary {NormalHit}/{Denominator}, " +
            $"coarse hypothesis {WeakHit}/{Denominator}; contradiction uses the complement and unknown uses 1/2. " +
            "One mixture report per family; approximate conditional independence between families is supplied, not discovered. " +
            (CompactPrior ? "Prior favours fewer repeated gap units and assumed passages." : "Equal prior mass per distinct normalized receiver sample.");
    }
    public static IReadOnlyList<Plan> Plans { get; } = [
        new("uniform-weighted", "Weighted evidence · equal prior", 15, 14, 9, 16),
        new("compact-weighted", "Weighted evidence · compactness prior", 15, 14, 9, 16, true),
        new("uniform-mild", "Milder evidence · equal prior", 12, 12, 9, 16),
        new("uniform-flat", "Neutral likelihood control", 8, 8, 8, 16)
    ];

    public sealed record Family(string Id, string Name, IElement Port, int Strength);
    public sealed record Alternative(string Id, RelationalShapeSearch.Trial Trial, Relation Model,
        IReadOnlyList<RelationalShapeSearch.Trial> Routes);
    public sealed record Group(Family Family, Relation Surface, int Supported, int Contradicted, int Unresolved);
    public sealed record Step(Family Family, IReadOnlyDictionary<string, Stats.Ratio> Likelihoods,
        Stats.Transition Transition);
    public sealed record Result(Plan Plan, IReadOnlyList<Alternative> Alternatives, Alternative Baseline,
        Alternative Selected, IReadOnlyList<Alternative> Tied, Stats.Distribution Prior,
        Stats.Distribution Posterior, Relation State, Relation Surface, IReadOnlyList<Step> Steps,
        IReadOnlyDictionary<string, IReadOnlyList<Group>> Assessments);
    public sealed record Pool(JointShapeDescription.Message Message, RelationalShapeSearch.Result Search,
        IReadOnlyList<Alternative> Alternatives, Alternative Baseline, IReadOnlyList<Family> Families,
        IReadOnlyDictionary<string, IReadOnlyList<Group>> Assessments, Relation Surface);

    public static Pool Freeze(JointShapeDescription.Message message, RelationalShapeSearch.Result search,
        IReadOnlyList<RelationalShapeSearch.Trial> trials)
    {
        if (trials.Count == 0 || !trials.Contains(search.Best)) throw new ArgumentException("The shared pool must contain the ordinal preference.");
        var groups = trials.GroupBy(trial => SampleKey(message, trial.Candidate), StringComparer.Ordinal).ToArray();
        var alternatives = groups.Select((group, index) => {
            var routes = group.ToArray();
            var representative = routes.Contains(search.Best) ? search.Best : routes.OrderBy(trial => trial.Rank).ThenBy(trial => trial.Index).First();
            return new Alternative($"C{index + 1:000}", representative, representative.Snapshot, routes);
        }).ToArray();
        var baseline = alternatives.Single(item => item.Routes.Contains(search.Best));
        var focus = search.Focus;
        // Explicit exposed source ports, rather than labels, anchor each lane.
        var jointPort = S(message.Joints, message.Components);
        var turnPort = message.InteriorTurns;
        Family[] families = [
            new("joints", "Joints and connected components", jointPort, 2),
            new("spatial", "Relative locations", focus.Spatial.Surface, 2),
            new("equivalence", "Length and angle equivalence", focus.Equivalence is null ? StructuralConstraint.Open() :
                new Relation(focus.Equivalence.ExactLengths, focus.Equivalence.ExactAngles), 1),
            new("symmetry", "Symmetry correspondences", message.Symmetries, 1),
            new("interiors", "Interior turn contexts", turnPort, 1),
            new("coarse", "Coarse angle hypotheses", focus.Equivalence?.SimilarAngles ?? StructuralConstraint.Open(), 0)
        ];
        var reads = alternatives.ToDictionary(item => item.Id, item => (IReadOnlyList<Group>)families.Select(family => Assess(item, family)).ToArray());
        var surface = new Relation(search.Focus.Surface, S(alternatives.Select(item => (IElement)new Relation(item.Model,
            S(item.Routes.Select(route => (IElement)route.Snapshot).ToArray()))).ToArray()));
        return new(message, search, alternatives, baseline, families, reads, surface);

        Group Assess(Alternative item, Family family)
        {
            var supported = new List<IElement>(); var contradicted = new List<IElement>(); var open = new List<IElement>();
            if (family.Id == "spatial")
            {
                supported.AddRange(E(item.Trial.Spatial.Supported)); contradicted.AddRange(E(item.Trial.Spatial.Contradicted));
                open.AddRange(E(item.Trial.Spatial.Open));
            }
            else if (family.Id is "equivalence" or "coarse")
            {
                if (item.Trial.Equivalence is not null)
                    foreach (var claim in item.Trial.Equivalence.Claims.Where(claim => family.Id == "coarse"
                        ? claim.Kind == "angleSimilarityHypothesis" : claim.Kind is "lengthEquivalence" or "angleEquivalence"))
                        Lane(claim.State).Add(claim.Witness);
            }
            else
                foreach (var claim in item.Trial.Core.Claims.Where(claim => family.Id switch {
                    "joints" => claim.Kind is "jointTypes" or "components",
                    "symmetry" => claim.Kind == "symmetry", "interiors" => claim.Kind == "refinement", _ => false }))
                    Lane(claim.State).Add(claim.Witness);
            // Existing typed claim descriptions are adapter reads of native
            // witnesses. The connected assessment, not Kind/State text, is the
            // durable statistical operand; defaults and goals are excluded.
            return new(family, CompatibilityEvidence.Assess(family.Port, item.Model, S(supported.ToArray()), S(contradicted.ToArray()), S(open.ToArray())),
                supported.Count, contradicted.Count, open.Count);
            List<IElement> Lane(string state) => state == "Supported" ? supported : state is "Violated" or "Contradicted" ? contradicted : open;
        }
    }

    public static Result Read(Pool pool, Plan plan)
    {
        var premise = new Relation(pool.Surface, S(pool.Families.Select(item => item.Port).ToArray()));
        var frame = new Stats.Frame(premise, pool.Alternatives.Select(item => (IElement)item.Model).ToArray());
        var priorRows = pool.Alternatives.Select(item => {
            var cost = plan.CompactPrior ? CompactCost(item.Trial) : 0;
            return new Relation(item.Model, frame.Weight(1, checked(1 + cost), item.Model));
        }).ToArray();
        var state = frame.Prior(priorRows); var prior = Stats.Read(state); var receipts = new List<IElement>(); var steps = new List<Step>();
        var previous = new List<IElement>();
        foreach (var family in pool.Families)
        {
            if (pool.Assessments.Values.All(groups => groups.Single(group => group.Family == family) is { Supported: 0, Contradicted: 0, Unresolved: 0 })) continue;
            var hit = family.Strength == 2 ? plan.StrongHit : family.Strength == 1 ? plan.NormalHit : plan.WeakHit;
            var calibration = CompatibilityEvidence.Calibrate(frame, hit, plan.Denominator);
            var rows = new List<IElement>(); var estimates = new Dictionary<string, Stats.Ratio>();
            foreach (var item in pool.Alternatives)
            {
                var group = pool.Assessments[item.Id].Single(group => group.Family == family);
                if (!CompatibilityEvidence.TryReadLikelihood(group.Surface, calibration, out var partition, out var receipt))
                    throw new InvalidOperationException("A nonempty advertised evidence family could not publish its declared channel: " + family.Id);
                receipts.Add(receipt); rows.Add(new Relation(item.Model, partition));
                if (!Stats.TryReadPartition(partition, frame.Unit, out var probability)) throw new InvalidOperationException("Channel produced a foreign partition.");
                estimates.Add(item.Id, probability);
            }
            var question = new Stats.Question(family.Port, new Relation(family.Port, StructuralConstraint.Open()), premise);
            // This is a selected representative-report channel, not an
            // assertion that all the witness checks were jointly observed.
            var observation = question.Observe(new Relation(pool.Surface, family.Port));
            var factor = Stats.Offer(observation, S(rows.ToArray()), S(), S(previous.ToArray()), calibration);
            var transition = Stats.Update(state, factor);
            if (!transition.Accepted) throw new InvalidOperationException("The declared compatibility law did not settle: " + family.Id);
            state = transition.State; receipts.Add(transition.Surface); steps.Add(new(family, estimates, transition));
            Stats.TryReadObservation(observation, out var read); previous.Add(read.Event);
        }
        var posterior = Stats.Read(state);
        if (!posterior.Complete) throw new InvalidOperationException("A finite weighted read remained open.");
        var ordered = pool.Alternatives.OrderByDescending(item => Weight(posterior, item), RatioComparer.Instance)
            .ThenBy(item => item.Trial.Rank).ThenBy(item => item.Trial.Index).ToArray();
        var selected = ordered[0]; var top = Weight(posterior, selected);
        var tied = ordered.Where(item => Weight(posterior, item) == top).ToArray();
        var surface = new Relation(new Relation(pool.Surface, premise), new Relation(state, S(receipts.ToArray())));
        return new(plan, pool.Alternatives, pool.Baseline, selected, tied, prior, posterior, state, surface, steps, pool.Assessments);
    }

    public static Stats.Ratio Weight(Stats.Distribution read, Alternative item) =>
        read.Rows.Single(row => ReferenceEquals(row.Model, item.Model)).Probability ?? throw new InvalidOperationException("Missing model weight.");
    public static int CompactCost(RelationalShapeSearch.Trial trial)
    {
        // Common scale is unmeasured: 2,2 and 1,1 have the same compactness
        // under this shape read. Extra assumed passages are separately priced.
        var gaps = trial.HorizontalGaps.Concat(trial.VerticalGaps).ToArray();
        var gcd = gaps.Aggregate(0, Gcd);
        return checked((gcd == 0 ? 0 : gaps.Sum(value => value / gcd)) + E(trial.Candidate.AssumedStrokes).Length);
    }
    public static string SampleKey(JointShapeDescription.Message message, JointShapeReconstruction.Candidate candidate)
    {
        var original = RelationalShapeSearch.Signature(message, candidate);
        var connectivity = original[..original.IndexOf(';')];
        var rows = E(message.Roles).Select(role => candidate.Positions.TryGetValue(role, out var word)
            ? E(word).Cast<Relation>().GroupBy(move => Sector(message.Frame, S(move.Left))).OrderBy(group => group.Key)
                .Select(group => (Direction: group.Key, Count: group.Count())).ToArray() : []).ToArray();
        var gcd = rows.SelectMany(row => row).Aggregate(0, (current, item) => Gcd(current, item.Count));
        return connectivity + ";" + string.Join(';', rows.Select(row => string.Join(',', row.Select(item => item.Direction + "x" + item.Count / Math.Max(1, gcd)))));
    }
    private static int Gcd(int a, int b) { while (b != 0) { var t = a % b; a = b; b = t; } return a; }
    public sealed class RatioComparer : IComparer<Stats.Ratio>
    {
        public static RatioComparer Instance { get; } = new();
        public int Compare(Stats.Ratio x, Stats.Ratio y) => (x.Numerator * y.Denominator).CompareTo(y.Numerator * x.Denominator);
    }
}
