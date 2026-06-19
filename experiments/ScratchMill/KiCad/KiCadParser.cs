using System.Text.RegularExpressions;

namespace ScratchMill.KiCad;

public class KiCadParser
{
    public KiCadParser()
    {
    }
    public static KcPcb Parse(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var objList = SExpressionLexer.Parse(content);

        var result = KcPcb.Parse((List<object>)objList);
        return result;
    }
}

public class SExpressionLexer
{
    public static object Parse(string input)
    {
        var tokens = Tokenize(input);
        var tokenQueue = new Queue<string>(tokens);
        return ParseTokens(tokenQueue);
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var regex = new Regex(@"\(|\)|""[^""]*""|;[^\n]*|[^\s()]+");
        //var regex = new Regex(@"\(|\)|""[^""]*""|;[^\n]*|[^\s();]+");
        var matches = regex.Matches(input);

        foreach (Match match in matches)
        {
            var token = match.Value;

            if (token.StartsWith(";")) // Skip comments
                continue;

            tokens.Add(token);
        }

        return tokens;
    }

    private static object ParseTokens(Queue<string> tokens)
    {
        if (tokens.Count == 0)
            throw new InvalidOperationException("Unexpected end of tokens");

        var token = tokens.Dequeue();

        if (token == "(")
        {
            var list = new List<object>();
            while (tokens.Peek() != ")")
            {
                list.Add(ParseTokens(tokens));
            }
            tokens.Dequeue(); // Remove ")"
            return list;
        }

        if (token == ")")
        {
            throw new InvalidOperationException("Unexpected closing parenthesis");
        }
        return ConvertToken(token);
    }

    private static object ConvertToken(string token)
    {
        if (token == "yes")
        {
            return "true";
        }

        if (token == "no")
        {
            return "false";
        }

        if (token.StartsWith("\"") && token.EndsWith("\""))
        {
            return token.Substring(1, token.Length - 2);
        }

        return token;
    }
}








