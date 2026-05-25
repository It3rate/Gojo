namespace Gojo.Core;

/// <summary>
/// Shared mutable tick coordinate. Several carrier locations can ground the
/// same TickRef so structural retargeting moves each grounded face together.
/// </summary>
public sealed class TickRef(long ticks) : IElement
{
    private static ulong idCounter = 1;

    /// <summary>Creates a tick reference from an unsigned tick count.</summary>
    public TickRef(ulong ticks)
        : this(checked((long)ticks))
    {
    }

    public ulong Id { get; } = NextId();

    public long Ticks { get; private set; } = ticks;

    /// <summary>Creates a separate tick reference with negated coordinate.</summary>
    public TickRef Dual() => new(checked(-Ticks));

    /// <summary>Retargets this coordinate to an absolute tick value.</summary>
    public void Set(long ticks) => Ticks = ticks;

    /// <summary>Moves this coordinate by a signed tick delta.</summary>
    public void Move(long delta) => Ticks = checked(Ticks + delta);

    /// <summary>Returns the structural dual of this tick reference.</summary>
    IElement IElement.Dual() => Dual();

    public override string ToString() => Ticks.ToString();

    private static ulong NextId() => idCounter++;
}
