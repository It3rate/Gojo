using System.Runtime.CompilerServices;
using Gojo.Core;

namespace Gojo.Encoding.Core;

/// <summary>Shared participant identity for local encoding reads. Coordinate
/// equality never merges independently grounded sites. Hashing follows stable
/// carrier/tick identity, not a tick's mutable coordinate.</summary>
public static class ParticipantIdentity
{
    public static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation p && b is CarrierLocation q &&
        p.Carrier.SameIdentityAs(q.Carrier) && p.SameTickRefAs(q);

    public static IEqualityComparer<IElement> Comparer { get; } = new ParticipantComparer();

    private sealed class ParticipantComparer : IEqualityComparer<IElement>
    {
        public bool Equals(IElement? x, IElement? y) => ReferenceEquals(x, y) ||
            x is not null && y is not null && Same(x, y);

        public int GetHashCode(IElement value) => value switch
        {
            Placeholder slot => slot.GetHashCode(),
            CarrierLocation site => HashCode.Combine(site.Carrier.Id, RuntimeHelpers.GetHashCode(site.Tick)),
            _ => RuntimeHelpers.GetHashCode(value)
        };
    }
}
