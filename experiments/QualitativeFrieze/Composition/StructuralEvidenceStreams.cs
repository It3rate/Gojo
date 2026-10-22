using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Experimental, local evidence envelopes. The connected port, not the display
/// names of its four contexts, distinguishes observation, proposed explanation,
/// defeasible completion and desired result. No carrier tension or statistical
/// probability is introduced by this observational read.
/// </summary>
public static class StructuralEvidenceStreams
{
    // ladder = (least -> greatest) -> Closed(least -> next, ... -> greatest)
    // stream = ((cause -> claim) -> (support rung -> opposed rung)) ->
    //          (dependency scope -> provenance)
    // bundle = (ladder -> observations) ->
    //          (hypotheses -> (defaults -> desired results))
    // These are separate participation contexts. A desired condition does not
    // establish observed truth; a default has no observational premise.
    public sealed class Frame
    {
        public IElement None { get; }
        public IElement Weak { get; }
        public IElement Medium { get; }
        public IElement Strong { get; }
        public Relation Steps { get; }
        public Relation Surface { get; }

        public Frame() : this(new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder()) { }
        private Frame(IElement none, IElement weak, IElement medium, IElement strong)
        {
            None = none; Weak = weak; Medium = medium; Strong = strong;
            Steps = S(new Relation(none, weak), new Relation(weak, medium), new Relation(medium, strong));
            Surface = new(new Relation(none, strong), Steps);
        }
        private Frame(IElement none, IElement weak, IElement medium, IElement strong, Relation steps, Relation surface)
        { None = none; Weak = weak; Medium = medium; Strong = strong; Steps = steps; Surface = surface; }

        /// <summary>There are four ordinal promises, with no distances between them.</summary>
        public static bool TryRead(Relation surface, out Frame frame)
        {
            frame = null!;
            if (surface is not { Left: Relation ends, Right: Relation steps } || !Closed(steps)) return false;
            var rows = E(steps);
            if (rows.Length != 3 || rows.Any(row => row is not Relation)) return false;
            var links = rows.Cast<Relation>().ToArray();
            if (!ReferenceEquals(links[0].Left, ends.Left) || !ReferenceEquals(links[2].Right, ends.Right) ||
                !ReferenceEquals(links[0].Right, links[1].Left) || !ReferenceEquals(links[1].Right, links[2].Left)) return false;
            var rungs = new[] { ends.Left, links[0].Right, links[1].Right, ends.Right };
            if (rungs.Any(rung => rung is not Placeholder) || rungs.Distinct(ReferenceEqualityComparer.Instance).Count() != 4) return false;
            frame = new(rungs[0], rungs[1], rungs[2], rungs[3], steps, surface); return true;
        }

        public bool Contains(IElement rung) => ReferenceEquals(rung, None) || ReferenceEquals(rung, Weak) ||
            ReferenceEquals(rung, Medium) || ReferenceEquals(rung, Strong);

        /// <summary>Reads dominance by following the actual local ladder links.</summary>
        public Relation Route(IElement from, IElement to)
        {
            if (!Contains(from) || !Contains(to)) return StructuralConstraint.Open(new Relation(from, to));
            if (ReferenceEquals(from, to)) return S(new Relation(from, to));
            var selected = new List<IElement>(); var current = from;
            foreach (var row in E(Steps).Cast<Relation>())
            {
                if (!ReferenceEquals(row.Left, current)) continue;
                selected.Add(row); current = row.Right;
                if (ReferenceEquals(current, to)) return S(selected.ToArray());
            }
            return StructuralConstraint.Open(new Relation(from, to));
        }

        public bool AtLeast(IElement candidate, IElement other) => Closed(Route(other, candidate));
        public bool Greater(IElement candidate, IElement other) => !ReferenceEquals(candidate, other) && AtLeast(candidate, other);
        public string Name(IElement rung) => ReferenceEquals(rung, None) ? "No opinion" : ReferenceEquals(rung, Weak) ? "Weak" :
            ReferenceEquals(rung, Medium) ? "Medium" : ReferenceEquals(rung, Strong) ? "Strong" : "Unbound";
    }

    public sealed class Stream
    {
        public IElement Claim { get; }
        public IElement Support { get; }
        public IElement Opposition { get; }
        public IElement Cause { get; }
        public Relation Dependencies { get; }
        public IElement Provenance { get; }
        public Relation Surface { get; }

        public Stream(IElement claim, IElement support, IElement opposition, IElement cause,
            Relation dependencies, IElement provenance)
        {
            Claim = claim; Support = support; Opposition = opposition; Cause = cause;
            Dependencies = dependencies; Provenance = provenance;
            Surface = new(new Relation(new Relation(cause, claim), new Relation(support, opposition)),
                new Relation(dependencies, provenance));
        }
        private Stream(IElement claim, IElement support, IElement opposition, IElement cause,
            Relation dependencies, IElement provenance, Relation surface) : this(claim, support, opposition, cause, dependencies, provenance)
        { Surface = surface; }

        public static bool TryRead(Relation surface, Frame frame, out Stream stream)
        {
            stream = null!;
            if (surface is not { Left: Relation { Left: Relation premise, Right: Relation grades },
                Right: Relation { Left: Relation dependencies, Right: var provenance } } ||
                !frame.Contains(grades.Left) || !frame.Contains(grades.Right) || !Scope(dependencies)) return false;
            stream = new(premise.Right, grades.Left, grades.Right, premise.Left, dependencies, provenance, surface); return true;
        }
    }

    public sealed class Bundle
    {
        public Frame Ladder { get; }
        public Relation Observations { get; }
        public Relation Hypotheses { get; }
        public Relation Defaults { get; }
        public Relation Goals { get; }
        public Relation Surface { get; }

        public Bundle(Frame ladder, Relation observations, Relation hypotheses, Relation defaults, Relation goals)
        {
            Ladder = ladder; Observations = observations; Hypotheses = hypotheses; Defaults = defaults; Goals = goals;
            Surface = new(new Relation(ladder.Surface, observations), new Relation(hypotheses, new Relation(defaults, goals)));
        }
        private Bundle(Frame ladder, Relation observations, Relation hypotheses, Relation defaults, Relation goals, Relation surface)
            : this(ladder, observations, hypotheses, defaults, goals) { Surface = surface; }

        public static bool TryRead(Relation surface, out Bundle bundle)
        {
            bundle = null!;
            if (surface is not { Left: Relation { Left: Relation ladder, Right: Relation observed },
                Right: Relation { Left: Relation hypothetical, Right: Relation { Left: Relation defaults, Right: Relation goals } } } ||
                !Frame.TryRead(ladder, out var frame)) return false;
            foreach (var scope in new[] { observed, hypothetical, defaults, goals })
                if (!Scope(scope) || E(scope).Any(row => row is not Relation item || !Stream.TryRead(item, frame, out _))) return false;
            bundle = new(frame, observed, hypothetical, defaults, goals, surface); return true;
        }

        public IReadOnlyList<Stream> Read(Relation scope) => E(scope).Cast<Relation>().Select(row =>
            Stream.TryRead(row, Ladder, out var stream) ? stream : throw new ArgumentException("The stream does not belong to this ladder.")).ToArray();
    }

    public sealed class Assessment
    {
        public IElement Claim { get; }
        public IElement Support { get; }
        public IElement Opposition { get; }
        public IElement ObservedSupport { get; }
        public IElement ObservedOpposition { get; }
        public Relation SelectedStreams { get; }
        public Relation Dominance { get; }
        public Relation Surface { get; }
        public bool Supports { get; }
        public bool Opposes { get; }
        public bool Conflicted { get; }
        public bool CanUseDefault { get; }

        internal Assessment(Bundle bundle, IElement claim)
        {
            Claim = claim;
            var observed = bundle.Read(bundle.Observations).Where(row => ReferenceEquals(row.Claim, claim)).ToArray();
            var defaults = bundle.Read(bundle.Defaults).Where(row => ReferenceEquals(row.Claim, claim)).ToArray();
            // Repeated routes do not add certainty: only the strongest linked
            // ordinal witness is read, separately on each face.
            ObservedSupport = Strongest(observed.Select(row => row.Support));
            ObservedOpposition = Strongest(observed.Select(row => row.Opposition));
            var defaultSupport = Strongest(defaults.Select(row => row.Support));
            var defaultOpposition = Strongest(defaults.Select(row => row.Opposition));
            Support = Strongest([ObservedSupport, defaultSupport]); Opposition = Strongest([ObservedOpposition, defaultOpposition]);
            Supports = bundle.Ladder.Greater(Support, Opposition);
            Opposes = bundle.Ladder.Greater(Opposition, Support);
            Conflicted = !ReferenceEquals(Support, bundle.Ladder.None) && !ReferenceEquals(Opposition, bundle.Ladder.None);
            CanUseDefault = bundle.Ladder.Greater(defaultSupport, ObservedOpposition) &&
                !bundle.Ladder.Greater(ObservedSupport, defaultSupport) && !bundle.Ladder.Greater(defaultOpposition, defaultSupport);
            SelectedStreams = S(observed.Concat(defaults).Select(row => (IElement)row.Surface).ToArray());
            Dominance = new(new Relation(bundle.Ladder.Route(Opposition, Support), bundle.Ladder.Route(Support, Opposition)),
                new Relation(bundle.Ladder.Route(ObservedOpposition, defaultSupport), bundle.Ladder.Route(defaultSupport, ObservedOpposition)));
            Surface = new(new Relation(bundle.Surface, claim), new Relation(SelectedStreams,
                new Relation(new Relation(Support, Opposition), Dominance)));

            IElement Strongest(IEnumerable<IElement> grades)
            {
                var selected = bundle.Ladder.None;
                foreach (var grade in grades) if (bundle.Ladder.Greater(grade, selected)) selected = grade;
                return selected;
            }
        }
    }

    public static Assessment Assess(Bundle bundle, IElement claim) => new(bundle, claim);

    /// <summary>Publishes an independently identified closure default and the
    /// actual observed endpoint evidence. An endpoint observation opposes current
    /// closure, but does not decide whether the intended parent was closed.</summary>
    public static Bundle ClosureEvidence(JointShapeDescription.Message message)
    {
        var frame = new Frame(); var endpoints = Endpoints(message);
        var defaultStream = new Stream(message.ClosureDefault, frame.Weak, frame.None,
            message.ClosureDefault, S(), new Relation(message.Roles, message.ClosureDefault));
        var observations = E(endpoints).Length == 0 ? S() : S(new Stream(message.ClosureDefault, frame.None, frame.Strong,
            endpoints, S(message.Joints), new Relation(message.Roles, endpoints)).Surface);
        return new(frame, observations, S(), S(defaultStream.Surface), S());
    }

    public sealed class ClosureSelection
    {
        public Bundle Evidence { get; }
        public Assessment Read { get; }
        public Relation ObservedEnds { get; }
        public Relation ClosedParentWithDamage { get; }
        public Relation OpenPath { get; }
        public Relation CompletionProposal { get; }
        public Relation RetainedAlternatives { get; }
        public Relation Selected { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public bool ShouldComplete { get; }
        public bool ClosureRequired { get; }
        public bool PreserveObservedGap { get; }

        internal ClosureSelection(JointShapeDescription.Message message, Bundle evidence)
        {
            Evidence = evidence; Read = Assess(evidence, message.ClosureDefault); ObservedEnds = Endpoints(message);
            ClosedParentWithDamage = new(new Relation(message.Roles, message.ClosureDefault), ObservedEnds);
            OpenPath = new(message.Roles, ObservedEnds);
            CompletionProposal = new(new Relation(ObservedEnds, message.ClosureDefault), StructuralConstraint.Open());
            var goals = evidence.Read(evidence.Goals).Where(row => ReferenceEquals(row.Claim, message.ClosureDefault)).ToArray();
            var targetSupport = evidence.Ladder.None; var targetOpposition = evidence.Ladder.None;
            foreach (var goal in goals)
            {
                if (evidence.Ladder.Greater(goal.Support, targetSupport)) targetSupport = goal.Support;
                if (evidence.Ladder.Greater(goal.Opposition, targetOpposition)) targetOpposition = goal.Opposition;
            }
            // A target can request repair despite observed damage; that is an
            // action proposal, never a retrospective assertion of closure.
            ClosureRequired = evidence.Ladder.Greater(targetSupport, targetOpposition);
            PreserveObservedGap = E(ObservedEnds).Length != 0;
            var alternativeSupport = PreserveObservedGap ? evidence.Ladder.Weak : evidence.Ladder.None;
            RetainedAlternatives = S(
                new Stream(ClosedParentWithDamage, alternativeSupport, evidence.Ladder.None,
                    message.ClosureDefault, S(Read.Surface, ObservedEnds), evidence.Surface).Surface,
                new Stream(OpenPath, alternativeSupport, evidence.Ladder.None,
                    ObservedEnds, S(Read.Surface), evidence.Surface).Surface,
                new Stream(CompletionProposal, targetSupport, targetOpposition,
                    message.ClosureDefault, S(Read.Surface), S(goals.Select(row => (IElement)row.Surface).ToArray())).Surface);
            ShouldComplete = ClosureRequired || Read.Supports;
            Selected = ShouldComplete ? CompletionProposal : E(ObservedEnds).Length != 0 ? OpenPath : ClosedParentWithDamage;
            Blocked = ShouldComplete ? S() : S(new Relation(message.ClosureDefault, Read.Dominance));
            Surface = new(new Relation(message.Surface, evidence.Surface), new Relation(Read.Surface,
                new Relation(new Relation(Selected, RetainedAlternatives), new Relation(Blocked, S(goals.Select(row => (IElement)row.Surface).ToArray())))));
        }
    }

    public static ClosureSelection SelectClosure(JointShapeDescription.Message message, Bundle? evidence = null) =>
        new(message, evidence ?? ClosureEvidence(message));

    private static Relation Endpoints(JointShapeDescription.Message message) => S(message.JointReads
        .Where(joint => Closed(joint.Rays) && E(joint.Rays).Length == 1)
        .Select(joint => (IElement)new Relation(joint.Role, joint.Rays)).ToArray());
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Scope(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out _, out _);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) && start.Equals(end);
}
