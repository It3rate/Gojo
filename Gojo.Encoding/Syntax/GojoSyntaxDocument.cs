using Gojo.Core;

namespace Gojo.Encoding.Syntax;

/// <summary>
/// Parsed roots from a Gojo syntax document. Local aliases are deliberately
/// absent: they are lexical conveniences, not crystallizations or stored
/// meaning.
/// </summary>
public sealed class GojoSyntaxDocument
{
    internal GojoSyntaxDocument(int syntaxVersion, IReadOnlyList<IElement> roots)
    {
        SyntaxVersion = syntaxVersion;
        Roots = [.. roots];
    }

    public int SyntaxVersion { get; }

    public IReadOnlyList<IElement> Roots { get; }
}
