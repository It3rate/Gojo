using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class CommunicationHintAblationTests
{
    [Fact]
    public void Every_supplied_subset_is_checked_and_one_topology_hint_suffices_in_each_case()
    {
        var study = CommunicationLoopStudy.Build(); var ablation = CommunicationHintAblation.Build(study);
        Assert.Equal(new[] { 8, 4, 4 }, ablation.Cases.Select(item => item.Options.Count).ToArray());
        foreach (var item in ablation.Cases)
        {
            var best = Assert.Single(item.Best);
            Assert.True(best.IsRobust); Assert.Equal(1, best.HintCount);
            Assert.Equal(2, best.Admissible.Count);
            Assert.All(best.Goals, goal => Assert.True(goal.Assessment.IsSatisfied));
            var repairedCount = Entries(item.Source.Trials[0].After.Message.Required).Length;
            Assert.True(best.HintCount < repairedCount);
            Assert.All(item.Options, option => Assert.Equal(study.Library.Realizations.Count, option.Candidates.Count));
        }
    }

    [Fact]
    public void Robustness_retains_all_admissible_samples_and_does_not_reward_one_lucky_selection()
    {
        var study = CommunicationLoopStudy.Build(); var ablation = CommunicationHintAblation.Build(study);
        foreach (var item in ablation.Cases)
        {
            var empty = Assert.Single(item.Options, option => option.HintCount == 0);
            Assert.Equal(14, empty.Admissible.Count); Assert.False(empty.IsRobust);
            Assert.Contains(empty.Goals, goal => goal.Assessment.IsSatisfied);
            Assert.Contains(empty.Goals, goal => !goal.Assessment.IsSatisfied);
            Assert.All(item.Options.Where(option => option.IsRobust), option =>
            {
                Assert.NotEmpty(option.Admissible);
                Assert.Equal(option.Admissible.Count, option.Goals.Count);
                Assert.All(option.Goals, goal => Assert.True(goal.Assessment.IsSatisfied));
            });
        }
    }

    [Fact]
    public void Selected_and_omitted_clauses_partition_the_actual_pool_without_copying_pattern_identity()
    {
        var ablation = CommunicationHintAblation.Build(CommunicationLoopStudy.Build());
        foreach (var item in ablation.Cases)
        foreach (var option in item.Options)
        {
            foreach (var pair in new[] { (item.Pool.Required, option.Message.Required, option.Omitted.Required),
                         (item.Pool.Forbidden, option.Message.Forbidden, option.Omitted.Forbidden) })
            {
                var pool = Entries(pair.Item1); var selected = Entries(pair.Item2); var omitted = Entries(pair.Item3);
                Assert.Equal(pool.Length, selected.Length + omitted.Length);
                Assert.DoesNotContain(selected, clause => omitted.Any(other => ReferenceEquals(clause, other)));
                Assert.All(selected.Concat(omitted), clause => Assert.Contains(pool, actual => ReferenceEquals(actual, clause)));
            }
            Assert.Same(option.Message, option.Candidates[0].Assessment.Message);
            Assert.All(option.Goals, goal => Assert.Same(item.Source.Intent, goal.Assessment.Message));
            Assert.True(option.HintParticipation > option.HintCount);
        }
    }

    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
