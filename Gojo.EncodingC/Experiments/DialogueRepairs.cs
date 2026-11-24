namespace Gojo.EncodingC;

internal readonly record struct DialogueProbe(int ReplyForFirst, int ReplyForSecond, int ScopeCost, bool InScope);

internal static class DialogueRepairs
{
    /// <summary>Adds a read of two probes over the supported referent and
    /// answer pairs at site zero. Each result plane says whether its probe
    /// is in scope and leaves one answer for every possible reply, with at
    /// least one possible reply. Planes are ordered by increasing scope cost,
    /// retaining input order for ties.</summary>
    internal static Store Add(Engine engine, Store answer, Store referent, ReadOnlySpan<int> answerByReferent, ReadOnlySpan<DialogueProbe> probes)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(referent);
        if (answerByReferent.Length != 2 || (uint)answerByReferent[0] > 1 || (uint)answerByReferent[1] > 1)
            throw new ArgumentException("Supply one of two answers for each of the two referents.", nameof(answerByReferent));
        if (probes.Length != 2)
            throw new ArgumentException("Supply two probes.", nameof(probes));
        foreach (var probe in probes)
            if ((uint)probe.ReplyForFirst > 1 || (uint)probe.ReplyForSecond > 1 || probe.ScopeCost < 0)
                throw new ArgumentException("Give each probe binary replies and a nonnegative scope cost.", nameof(probes));

        var (firstProbe, secondProbe) = probes[0].ScopeCost <= probes[1].ScopeCost
            ? (probes[0], probes[1]) : (probes[1], probes[0]);
        var firstAnswer = 1 << answerByReferent[0];
        var secondAnswer = 1 << answerByReferent[1];

        var mask = Shape.Binary(4);
        var law = TransitionLaw.FromFunction([mask, mask, mask], state =>
        {
            var first = (state[1] & 1) != 0 ? state[0] & firstAnswer : 0;
            var second = (state[1] & 2) != 0 ? state[0] & secondAnswer : 0;
            var results = (Settles(firstProbe, first, second) ? 1 : 0) |
                (Settles(secondProbe, first, second) ? 2 : 0);
            return [state[0], state[1], results];
        }, new Accounting(true, new Unchanged(0), new Unchanged(1)));

        var repairs = new Store(2, 1);
        var comparison = new Wiring(law, new Binding(answer, 0, 1), new Binding(referent, 0, 1), new Binding(repairs, 0, 1));
        comparison.Add(0, 0, 0);
        engine.Add(comparison);
        return repairs;
    }

    private static bool Settles(DialogueProbe probe, int first, int second)
    {
        if (!probe.InScope) return false;
        var replyZero = (probe.ReplyForFirst == 0 ? first : 0) | (probe.ReplyForSecond == 0 ? second : 0);
        var replyOne = (probe.ReplyForFirst == 1 ? first : 0) | (probe.ReplyForSecond == 1 ? second : 0);
        return (replyZero | replyOne) != 0 && replyZero != 3 && replyOne != 3;
    }
}
