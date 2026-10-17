using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class PlaceholderTests
{
    [Fact]
    public void PlaceholderCarriesIdentityWithoutCarrierOrBasis()
    {
        var placeholder = new Placeholder();
        var sameFace = placeholder;
        var other = new Placeholder();

        Assert.Equal(placeholder, sameFace);
        Assert.NotEqual(placeholder, other);
        Assert.False(placeholder.IsInverse);
        Assert.True(placeholder.SameSlotAs(sameFace));
        Assert.False(placeholder.SameSlotAs(other));
    }

    [Fact]
    public void PlaceholderDualPreservesSlotIdentityAndChangesFace()
    {
        var placeholder = new Placeholder();

        var dual = placeholder.Dual();

        Assert.NotEqual(placeholder, dual);
        Assert.True(placeholder.SameSlotAs(dual));
        Assert.True(dual.IsInverse);
        Assert.Equal(placeholder, dual.Dual());
    }
}
