using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An exhaustive finite hint-selection baseline. It asks which
/// transmitted clauses suffice for every admitted library sample, rather than
/// rewarding only the receiver's preferred sample. This is not policy training
/// or an optimal description beyond the supplied clauses and library.</summary>
public sealed class CommunicationHintAblation
{
    public sealed record GoalRead(CommunicationLoopStudy.Realization Realization,
        CommunicationAssessment Assessment)
    {
        public Relation Surface => new(Realization.Surface, Assessment.Surface);
    }

    public sealed class Option
    {
        public CommunicationMessage Message { get; }
        public CommunicationMessage Omitted { get; }
        public IReadOnlyList<CommunicationLoopStudy.CandidateRead> Candidates { get; }
        public IReadOnlyList<CommunicationLoopStudy.CandidateRead> Admissible { get; }
        public IReadOnlyList<GoalRead> Goals { get; }
        public Relation Surface { get; }
        public int HintCount => E(Message.Required).Length + E(Message.Forbidden).Length;
        public int HintParticipation => Reachable(Message.Surface).Count;
        public bool IsRobust => Admissible.Count != 0 && Goals.All(goal => goal.Assessment.IsSatisfied);

        internal Option(CommunicationMessage pool, CommunicationMessage message, CommunicationMessage omitted,
            CommunicationLoopStudy study, CommunicationMessage intent)
        {
            Message = message; Omitted = omitted;
            // The filter receives only the selected message. Retained intent
            // enters the separate evaluation over the admitted outputs.
            Candidates = study.Library.Realizations.Select(realization => new CommunicationLoopStudy.CandidateRead(
                realization, new CommunicationAssessment(realization.Induction, message))).ToArray();
            Admissible = Candidates.Where(candidate => candidate.IsAdmissible).ToArray();
            Goals = Admissible.Select(candidate => new GoalRead(candidate.Realization,
                new CommunicationAssessment(candidate.Realization.Induction, intent))).ToArray();
            Surface = new(new Relation(pool.Surface, new Relation(message.Surface, omitted.Surface)),
                new Relation(S(Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()),
                    new Relation(S(Admissible.Select(candidate => (IElement)candidate.Realization.Surface).ToArray()),
                        new Relation(intent.Surface, S(Goals.Select(goal => (IElement)goal.Surface).ToArray())))));
        }
    }

    public sealed class Case
    {
        public CommunicationLoopStudy.Case Source { get; }
        public CommunicationMessage Pool { get; }
        public IReadOnlyList<Option> Options { get; }
        public IReadOnlyList<Option> Best { get; }
        public Relation Surface { get; }
        internal Case(CommunicationLoopStudy study, CommunicationLoopStudy.Case source)
        {
            Source = source;
            var messages = new[] { source.InitialMessage }.Concat(source.Trials.Select(trial => trial.After.Message)).ToArray();
            var required = messages.SelectMany(message => E(message.Required)).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
            var forbidden = messages.SelectMany(message => E(message.Forbidden)).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
            Pool = new(S(required), S(forbidden));
            var clauseCount = required.Length + forbidden.Length;
            if (clauseCount > 12) throw new ArgumentException("The exhaustive baseline accepts at most twelve supplied clauses.", nameof(source));
            var options = new List<Option>();
            for (var selection = 0; selection < 1 << clauseCount; selection++)
            {
                var selectedRequired = required.Where((_, index) => (selection & 1 << index) != 0).ToArray();
                var selectedForbidden = forbidden.Where((_, index) => (selection & 1 << (index + required.Length)) != 0).ToArray();
                var omittedRequired = required.Where((_, index) => (selection & 1 << index) == 0).ToArray();
                var omittedForbidden = forbidden.Where((_, index) => (selection & 1 << (index + required.Length)) == 0).ToArray();
                options.Add(new(Pool, new(S(selectedRequired), S(selectedForbidden)),
                    new(S(omittedRequired), S(omittedForbidden)), study, source.Intent));
            }
            Options = options;
            var minimum = options.Where(option => option.IsRobust).Select(option => option.HintCount).DefaultIfEmpty(int.MaxValue).Min();
            Best = options.Where(option => option.IsRobust && option.HintCount == minimum).ToArray();
            Surface = new(new Relation(Pool.Surface, source.Intent.Surface),
                new Relation(S(Options.Select(option => (IElement)option.Surface).ToArray()),
                    S(Best.Select(option => (IElement)option.Surface).ToArray())));
        }
    }

    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }
    private CommunicationHintAblation(CommunicationLoopStudy study)
    {
        Cases = study.Cases.Select(source => new Case(study, source)).ToArray();
        Surface = new(study.Library.Surface, S(Cases.Select(item => (IElement)item.Surface).ToArray()));
    }
    public static CommunicationHintAblation Build(CommunicationLoopStudy study) => new(study);
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => QualitativeRecognitionObservation.Entries(scope);
    private static HashSet<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
            if (seen.Add(element) && element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }
}
