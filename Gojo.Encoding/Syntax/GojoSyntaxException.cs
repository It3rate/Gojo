namespace Gojo.Encoding.Syntax;

/// <summary>Reports a source location for an invalid first-pass Gojo syntax document.</summary>
public sealed class GojoSyntaxException : Exception
{
    public GojoSyntaxException(string message, int line, int column)
        : base($"{message} (line {line}, column {column})")
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }
}
