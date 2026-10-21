using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using C = Gojo.Experiments.QualitativeFrieze.OrnamentParticipationCount;
using H = Gojo.Experiments.QualitativeFrieze.ProvisionalEquivalence.Hypothesis;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Bounded proposal selection followed by actual native execution.
/// Source publications determine admissibility; a separate default concerns
/// the proposal's parameter sharing. Output evidence never confirms the source.</summary>
public sealed class RevisableRuleGeneration
{
    // prior = exact-one participation question -> (ordinary -> (independent -> unused))
    // proposal projection = hypothesis.Bindings -> Closed(actual parameter offers)
    // stage = source assessment -> (default policy -> (Closed(proposal reads) ->
    //         (explicit preference -> (chosen -> (execution -> output conformance)))))
    public C.Question SharedParameterQuestion { get; }
    public D.Policy Ordinary { get; }
    public D.Policy Independent { get; }
    public D.Policy Unselected { get; }
    public Relation Surface { get; }

    public RevisableRuleGeneration()
    {
        var variable = new Placeholder();
        SharedParameterQuestion = new(variable, S(variable), S(new Placeholder()));
        var shared = new D.Rule("Prefer one shared proposal parameter", SharedParameterQuestion.Surface);
        var independent = new D.Rule("Avoid a single shared proposal parameter", SharedParameterQuestion.Surface, false);
        Ordinary = new(strongDefaults: [shared]); Independent = new(strongDefaults: [independent]); Unselected = new(unused: [shared]);
        Surface = new(SharedParameterQuestion.Surface, new Relation(Ordinary.Surface, new Relation(Independent.Surface, Unselected.Surface)));
    }

    private RevisableRuleGeneration(Relation surface, C.Question question, D.Policy ordinary, D.Policy independent, D.Policy unselected)
    { Surface = surface; SharedParameterQuestion = question; Ordinary = ordinary; Independent = independent; Unselected = unselected; }

    /// <summary>Restore the exact prior and its actual predicate aliases.
    /// This imports policy, not a learned probability or a default source fact.</summary>
    public static bool TryRead(Relation surface, out RevisableRuleGeneration engine, out Relation witness)
    {
        engine = null!; witness = new(surface, StructuralConstraint.Open());
        if (surface is not { Left: Relation predicate, Right: Relation { Left: Relation first,
            Right: Relation { Left: Relation second, Right: Relation third } } }) return false;
        var question = new C.Question(predicate); var ordinary = new D.Policy(first); var independent = new D.Policy(second); var unused = new D.Policy(third);
        if (!question.IsValid || question.Pattern is not Placeholder pattern || E(question.Variables) is not [var variable] ||
            !ReferenceEquals(pattern, variable) || E(question.ExpectedRoles).Length != 1 ||
            !Valid(ordinary, 1, true) || !Valid(independent, 1, false) || !Valid(unused, 3, true)) return false;
        engine = new(surface, question, ordinary, independent, unused);
        witness = new(surface, S(predicate, first, second, third)); return true;

        bool Valid(D.Policy policy, int tier, bool expected) => policy.IsValid && policy.Rules.Count == 1 &&
            ReferenceEquals(policy.Rules[0].Predicate, predicate) && policy.Tier(policy.Rules[0]) == tier && policy.Rules[0].Expected == expected;
    }

    public sealed class Proposal
    {
        public H Hypothesis { get; }
        public ProvisionalEquivalence.Assessment SourceAssessment { get; }
        public C.Receipt Sharing { get; }
        public D.Receipt DefaultRead { get; }
        public Relation Projection { get; }
        public Relation Surface { get; }
        public bool Eligible => SourceAssessment.IsEligible && DefaultRead.Eligible;
        public bool Frontier { get; internal set; }
        internal Proposal(RevisableRuleGeneration engine, ProvisionalEquivalence.Assessment source, D.Policy policy)
        {
            Hypothesis = source.Hypothesis; SourceAssessment = source;
            var bindings = E(Hypothesis.Bindings).OfType<Relation>().ToArray();
            var offers = S(bindings.Select(binding => binding.Right).ToArray());
            Projection = new(Hypothesis.Bindings, new Relation(offers,
                S(bindings.Select(binding => (IElement)new Relation(binding, binding.Right)).ToArray())));
            Sharing = new(engine.SharedParameterQuestion, offers);
            var witness = new Relation(Projection, Sharing.Surface);
            DefaultRead = new(policy, new D.Observation(Hypothesis.Surface,
                [D.Evidence.FromAnswer(engine.SharedParameterQuestion.Surface, Sharing.Answer, witness)]));
            Surface = new(source.Surface, new Relation(Projection, new Relation(Sharing.Surface, DefaultRead.Surface)));
        }
    }

    public sealed class Stage
    {
        public ProvisionalEquivalence.Selection SourceRead { get; }
        public IReadOnlyList<Proposal> Proposals { get; }
        public Proposal? Chosen { get; }
        public RevisablePatternTemplate.Realization? Execution { get; }
        public ProvisionalEquivalence.Assessment? OutputConformance { get; }
        public Relation Preference { get; }
        public Relation Surface { get; }
        public Relation Carried { get; }
        public Relation Withdrawn { get; }
        public Relation Step { get; }
        public string Answer => Chosen is null ? "Open" : Execution?.Answer != "Yes" ? Execution?.Answer ?? "Open" :
            OutputConformance is null ? "Open" : E(OutputConformance.Blocked).Length != 0 ? "Blocked" :
            E(OutputConformance.InScopeSampleRefuted).Length != 0 || E(OutputConformance.InScopeConflicts).Length != 0 ? "No" :
            E(OutputConformance.SampleOpen).Length != 0 || E(OutputConformance.SampleSupport).Length != E(Chosen.Hypothesis.Domain).Length ? "Open" : "Yes";

        internal Stage(RevisableRuleGeneration engine, RevisablePatternTemplate template, Relation positiveBasis,
            IReadOnlyList<H> candidates, IReadOnlyList<ProvisionalEquivalence.Publication> publications,
            ProvisionalEquivalence.Context context, D.Policy policy, Relation preference, Stage? previous, Carrier clock, int ordinal)
        {
            if (!policy.Rules.All(rule => ReferenceEquals(rule.Predicate, engine.SharedParameterQuestion.Surface)))
                throw new ArgumentException("A proposal-sharing policy must reference the actual advertised prior question.", nameof(policy));
            if (!Closed(preference) || E(preference).Any(item => !candidates.Any(candidate => ReferenceEquals(candidate.Surface, item))))
                throw new ArgumentException("Selection preference must expose actual candidate hypothesis surfaces.", nameof(preference));
            var assessor = new ProvisionalEquivalence(template.Parameters, positiveBasis);
            SourceRead = assessor.Assess(candidates, publications, context);
            var proposals = SourceRead.Candidates.Select(source => new Proposal(engine, source, policy)).ToArray();
            foreach (var proposal in proposals)
                proposal.Frontier = proposal.Eligible && !proposals.Any(other => other.Eligible &&
                    D.Receipt.ComparePreference(other.DefaultRead, proposal.DefaultRead) == -1);
            Proposals = proposals; Preference = preference;
            Chosen = E(preference).Select(item => proposals.FirstOrDefault(proposal => proposal.Frontier && ReferenceEquals(proposal.Hypothesis.Surface, item)))
                .FirstOrDefault(proposal => proposal is not null);
            if (Chosen is not null)
            {
                Execution = template.Realize(Chosen.Hypothesis);
                if (Execution.Sample is not null && Execution.Answer == "Yes")
                {
                    // This publication describes the NEW OUTPUT, never an
                    // additional observation of the unknown source.
                    var output = new ProvisionalEquivalence.Publication(Execution.Surface, new Placeholder(), Execution.RoleValues);
                    var outputContext = new ProvisionalEquivalence.Context(S(template.Surface), S(), S(), S(output.Source));
                    OutputConformance = assessor.Assess([Chosen.Hypothesis], [output], outputContext).Candidates.Single();
                }
            }
            var before = previous?.Chosen?.Hypothesis.Equalities ?? S(); var after = Chosen?.Hypothesis.Equalities ?? S();
            Carried = S(E(before).Where(edge => Present(edge, after)).ToArray());
            Withdrawn = S(E(before).Where(edge => !Present(edge, after)).ToArray());
            Surface = new(SourceRead.Surface, new Relation(policy.Surface, new Relation(S(proposals.Select(proposal => (IElement)proposal.Surface).ToArray()),
                new Relation(preference, new Relation(Chosen?.Hypothesis.Surface ?? StructuralConstraint.Open(),
                    new Relation(Execution?.Surface ?? StructuralConstraint.Open(), new Relation(OutputConformance?.Surface ?? StructuralConstraint.Open(),
                        new Relation(Carried, Withdrawn))))))));
            Step = TimeStepSequence.Step(clock, clock, ordinal, ordinal + 1, previous?.Surface ?? S(),
                new Relation(S(publications.Select(publication => (IElement)publication.Surface).ToArray()), new Relation(context.Surface, policy.Surface)),
                Surface, new Relation(Withdrawn, S(proposals.Where(proposal => !proposal.Eligible).Select(proposal => (IElement)proposal.Surface).ToArray())));
        }

        private static bool Present(IElement edge, Relation scope) => edge is Relation pair && E(scope).OfType<Relation>()
            .Any(actual => ReferenceEquals(actual.Left, pair.Left) && ReferenceEquals(actual.Right, pair.Right));
    }

    public Stage Evaluate(RevisablePatternTemplate template, Relation positiveBasis, IReadOnlyList<H> candidates,
        IReadOnlyList<ProvisionalEquivalence.Publication> publications, ProvisionalEquivalence.Context context,
        D.Policy policy, Relation preference, Carrier clock, int ordinal = 0, Stage? previous = null) =>
        new(this, template, positiveBasis, candidates, publications, context, policy, preference, previous, clock, ordinal);

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && ReferenceEquals(seam, tail);
}
