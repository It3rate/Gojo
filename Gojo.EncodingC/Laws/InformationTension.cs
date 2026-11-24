namespace Gojo.EncodingC;

/// <summary>Small laws for reading what is still possible where an answer is demanded.</summary>
public static class InformationTension
{
    /// <summary>Summarizes two possibilities. Roles are possibility A,
    /// possibility B, seen one or more, and seen two or more. The possibility
    /// bits are read and the last two roles receive the live summary.</summary>
    public static TransitionLaw SupportPair { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        state => [state[0], state[1], state[0] | state[1], state[0] & state[1]],
        new Accounting(true, new Unchanged(0), new Unchanged(1)));

    /// <summary>Reads demand and the two support summary bits. The last two
    /// roles report an open answer under demand and a contradiction under
    /// demand, respectively. Both outputs are clear when demand is absent.</summary>
    public static TransitionLaw DemandedStatus { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        state => [state[0], state[1], state[2], state[0] & state[2], state[0] & (1 - state[1])],
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2)));
}
