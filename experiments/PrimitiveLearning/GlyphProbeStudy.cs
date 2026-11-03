using Gojo.Core;
using Gojo.Experiments.PrimitiveMeasure;

namespace PrimitiveLearning;

/// <summary>The owner executes the walk. The query allocator only receives the
/// requested task and, after its decision, the benefit and cost bound to it.</summary>
public static class GlyphProbeStudy
{
    public sealed record Score(string Policy, int Trials, int Correct, int Open, int Queries, int Net);

    public static Score[] Run()
    {
        var learner = new QueryLearner(new Carrier(), new Carrier(), new Carrier());
        var scores = new List<Score>();
        void RunBlock(string policy, int[] cuts, bool train)
        {
            int trials = 0, correct = 0, open = 0, queries = 0, net = 0;
            foreach (int cut in cuts)
            foreach (bool retained in new[] { false, true })
            foreach (bool reverse in new[] { false, true })
            foreach (bool returnTask in new[] { false, true })
            {
                var glyph = new GlyphCut(cut, reverse, retained);
                var task = NativePublication.Point(learner.ContextAxis, returnTask ? 1 : 0);
                var ask = policy switch
                {
                    "Always query" => true,
                    "Never query" => false,
                    _ => NativePublication.Read(learner.Decide(task, train), learner.ChoiceAxis) == 1
                };
                var support = GlyphCut.Predict(returnTask, glyph.InkCloses, ask ? glyph.InspectReturn() : null);
                var actual = returnTask ? glyph.AttemptReturn().Arrived : glyph.AttemptInk().Arrived;
                var benefit = support == 3 ? 0 : (actual ? 2 : 1) == support ? 4 : -4;
                var cost = ask ? 1 : 0;
                if (train)
                    learner.Observe(task, learner.BindBenefit(NativePublication.Point(learner.BenefitAxis, benefit)),
                        learner.BindCost(NativePublication.Point(learner.CostAxis, cost)));
                trials++; queries += cost; net += benefit-cost;
                if (benefit > 0) correct++;
                if (support == 3) open++;
            }
            scores.Add(new(policy, trials, correct, open, queries, net));
        }
        RunBlock("Training", Enumerable.Range(0, 8).SelectMany(_ => new[] { 2, 4, 6 }).ToArray(), true);
        foreach (var policy in new[] { "Frozen", "Always query", "Never query" })
            RunBlock(policy, [1, 3, 5, 7], false);
        return scores.ToArray();
    }
}
