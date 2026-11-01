using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class ElementIdentityConcurrencyTests
{
    [Fact]
    public void IndependentConcurrentConstructionDoesNotReuseElementIds()
    {
        const int count = 32768;
        var carriers = new ulong[count];
        var relations = new ulong[count];
        var ticks = new ulong[count];
        var placeholders = new ulong[count];
        var units = new ulong[count];
        Parallel.For(0, count, i =>
        {
            var carrier = new Carrier();
            carriers[i] = carrier.Id;
            units[i] = carrier.UnitShape.Id;
            relations[i] = new Relation(carrier, carrier).Id;
            ticks[i] = new TickRef(i).Id;
            placeholders[i] = new Placeholder().Id;
        });

        foreach (var ids in new[] { carriers, relations, ticks, placeholders, units })
        {
            Assert.DoesNotContain(0UL, ids);
            Assert.Equal(count, ids.Distinct().Count());
        }
    }
}
