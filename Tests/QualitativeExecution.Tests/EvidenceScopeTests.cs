using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace QualitativeExecution.Tests;

public sealed class EvidenceScopeTests
{
    [Fact]
    public void Complementation_requires_a_closed_binary_event_domain()
    {
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var valid = question.Observe(new Placeholder(), affirm: false);
        Assert.True(Stats.TryReadObservation(valid, out var parsed));
        var questionParts = (Relation)question.Surface.Left;
        var openQuestion = new Relation(new Relation(questionParts.Left,
            StructuralConstraint.Open(question.Order.Left, question.Order.Right)), question.Surface.Right);
        var malformed = new Relation(new Relation(parsed.Event, openQuestion), valid.Right);
        Assert.False(Stats.TryReadObservation(malformed, out _));
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var prior = frame.Prior(new(a, frame.Weight(1, 2)), new(b, frame.Weight(1, 2)));
        var update = Stats.Update(prior, Stats.Offer(malformed,
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), new Placeholder()));
        Assert.True(update.Open); Assert.False(update.Accepted); Assert.Same(prior, update.State);
    }

    [Fact]
    public void Three_alternatives_normalize_together_and_retain_the_exactly_excluded_member()
    {
        var models = Enumerable.Range(0, 3).Select(_ => (IElement)new Placeholder()).ToArray();
        var frame = new Stats.Frame(new Placeholder(), models);
        var prior = frame.Prior(new(models[0], frame.Weight(1, 2)), new(models[1], frame.Weight(1, 3)), new(models[2], frame.Weight(1, 6)));
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var result = Stats.Update(prior, Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(models[0], frame.Weight(2, 3)), new Relation(models[1], frame.Weight(1, 3)),
                new Relation(models[2], frame.Weight(0, 1))), S(), S(), new Placeholder()));
        Assert.True(result.Accepted);
        var read = Stats.Read(result.State);
        Assert.Equal(3, read.Rows.Count);
        Check(read, models[0], 3, 4); Check(read, models[1], 1, 4); Check(read, models[2], 0, 1);
        Assert.Equal(new Stats.Ratio(4, 9), read.EvidenceProbability);
    }

    [Fact]
    public void One_of_a_hundred_alternatives_is_not_a_hundred_acquired_observations()
    {
        var models = Enumerable.Range(0, 100).Select(_ => (IElement)new Placeholder()).ToArray();
        var frame = new Stats.Frame(new Placeholder(), models);
        var prior = frame.Prior(models.Select(model => new Relation(model, frame.Weight(1, 100))).ToArray());
        var before = Stats.Read(prior);
        Assert.True(before.Complete); Assert.Empty(before.Events);
        foreach (var model in models) Check(before, model, 1, 100);
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var result = Stats.Update(prior, Stats.Offer(question.Observe(new Placeholder()),
            S(models.Select((model, index) => (IElement)new Relation(model, frame.Weight(index == 0 ? 1000 : 1, 1000))).ToArray()),
            S(), S(), new Placeholder()));
        Assert.True(result.Accepted);
        var after = Stats.Read(result.State);
        Assert.Single(after.Events); Check(after, models[0], 1000, 1099);
        foreach (var model in models.Skip(1)) Check(after, model, 1, 1099);
    }

    [Fact]
    public void A_missing_third_likelihood_does_not_turn_a_three_model_scope_into_a_binary_choice()
    {
        var models = Enumerable.Range(0, 3).Select(_ => (IElement)new Placeholder()).ToArray();
        var frame = new Stats.Frame(new Placeholder(), models);
        var prior = frame.Prior(models.Select(model => new Relation(model, frame.Weight(1, 3))).ToArray());
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var offer = Stats.Offer(question.Observe(new Placeholder()), S(new Relation(models[0], frame.Weight(1, 1)),
            new Relation(models[1], frame.Weight(0, 1))), S(), S(), new Placeholder());
        var result = Stats.Update(prior, offer);
        Assert.True(result.Open); Assert.False(result.Accepted);
        Assert.Same(prior, result.State);
        foreach (var model in models) Check(Stats.Read(result.State), model, 1, 3);
    }

    private static void Check(Stats.Distribution read, IElement model, int n, int d)
    {
        Assert.True(read.Complete);
        var ratio = read.Rows.Single(row => ReferenceEquals(row.Model, model)).Probability!.Value;
        Assert.Equal((BigInteger)n * ratio.Denominator, (BigInteger)d * ratio.Numerator);
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
}
