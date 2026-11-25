namespace Gojo.EncodingC;

/// <summary>Optional evidence for an occupied pair. Bind the two occurrence
/// sites through their before and current bits, followed by three bits at the
/// pair site. The row records whether the left occurrence, the right occurrence,
/// or both arrived when the pair formed. It keeps the most recent formation
/// through quiet steps and removal. The current occupancy is a separate read.
/// </summary>
internal static class PairFormation
{
    internal static TransitionLaw Record { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        state =>
        {
            var wasLeftPresent = state[0];
            var wasRightPresent = state[1];
            if (state[2] == 0 || state[3] == 0 || (wasLeftPresent == 1 && wasRightPresent == 1))
                return state;

            return [
                .. state[..4],
                wasRightPresent,
                wasLeftPresent,
                (1 - wasLeftPresent) & (1 - wasRightPresent)
            ];
        },
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2), new Unchanged(3)));
}
