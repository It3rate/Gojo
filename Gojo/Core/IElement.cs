namespace Gojo.Core;

/// <summary>
/// Primitive Gojo element contract. Duality is structural inversion: locations
/// negate, carriers expose opposed faces, and relations dual their contents.
/// </summary>
public interface IElement
{
    /// <summary>Returns the structural dual of this element.</summary>
    IElement Dual();
}
