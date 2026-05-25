namespace Gojo.Core;

/// <summary>
/// Lowest-resolution structural site. A placeholder has identity and duality
/// but no carrier, basis, value, or unit shape until relation-shaped binding
/// structure gives it one.
/// </summary>
public sealed class Placeholder : IElement, IEquatable<Placeholder>
{
    private static ulong idCounter = 1;

    public Placeholder()
        : this(NextId(), isInverse: false)
    {
    }

    private Placeholder(ulong id, bool isInverse)
    {
        Id = id;
        IsInverse = isInverse;
    }

    public ulong Id { get; }

    public bool IsInverse { get; }

    /// <summary>Returns the opposed placeholder face while preserving slot identity.</summary>
    public Placeholder Dual() => new(Id, !IsInverse);

    /// <summary>Compares the abstract placeholder slot, ignoring its face.</summary>
    public bool SameSlotAs(Placeholder other) =>
        other is not null && Id == other.Id;

    public bool Equals(Placeholder? other) =>
        other is not null &&
        Id == other.Id &&
        IsInverse == other.IsInverse;

    public override bool Equals(object? obj) => obj is Placeholder other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, IsInverse);

    public override string ToString() => IsInverse ? $"?{Id}*" : $"?{Id}";

    /// <summary>Returns the structural dual of this placeholder.</summary>
    IElement IElement.Dual() => Dual();

    private static ulong NextId() => idCounter++;
}
