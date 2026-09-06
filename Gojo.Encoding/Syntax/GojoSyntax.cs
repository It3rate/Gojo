using System.Globalization;
using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Structures;

namespace Gojo.Encoding.Syntax;

/// <summary>
/// Parser for the deliberately small first Gojo surface syntax. It expands
/// every accepted construction into ordinary Gojo elements and retains no
/// semantic syntax tree after parsing.
/// </summary>
public static class GojoSyntax
{
    public static GojoSyntaxDocument Parse(string source) =>
        new Parser(source ?? throw new ArgumentNullException(nameof(source))).ParseDocument();

    private sealed class Parser
    {
        private readonly Lexer lexer;
        private readonly Dictionary<string, SyntaxValue> aliases = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Placeholder> slots = new(StringComparer.Ordinal);
        private Token current;

        public Parser(string source)
        {
            lexer = new Lexer(source);
            current = lexer.Next();
        }

        public GojoSyntaxDocument ParseDocument()
        {
            ExpectIdentifier("gojo");
            var versionValue = ExpectInteger();
            Expect(TokenKind.Semicolon);
            if (versionValue != 1)
            {
                Error($"Unsupported Gojo syntax version {versionValue}; this parser accepts version 1");
            }

            const int version = 1;

            var roots = new List<IElement>();
            while (current.Kind != TokenKind.End)
            {
                if (IsIdentifier("let"))
                {
                    Advance();
                    var alias = Expect(TokenKind.Alias).Text;
                    Expect(TokenKind.Equals);
                    var value = ParseExpression();
                    Expect(TokenKind.Semicolon);
                    if (!aliases.TryAdd(alias, value))
                    {
                        Error($"Alias '${alias}' is already defined");
                    }

                    continue;
                }

                if (IsIdentifier("root"))
                {
                    Advance();
                    var value = ParseExpression();
                    Expect(TokenKind.Semicolon);
                    roots.Add(RequireElement(value, "A root must be a Gojo element"));
                    continue;
                }

                Error("Expected 'let' or 'root'");
            }

            return new GojoSyntaxDocument(version, roots);
        }

        private SyntaxValue ParseExpression()
        {
            var value = ParsePrimary();
            while (current.Kind == TokenKind.Arrow)
            {
                Advance();
                var right = ParsePrimary();
                value = SyntaxValue.FromElement(new Relation(
                    RequireElement(value, "The left side of '->' must be a Gojo element"),
                    RequireElement(right, "The right side of '->' must be a Gojo element")));
            }

            return value;
        }

        private SyntaxValue ParsePrimary()
        {
            if (current.Kind == TokenKind.LeftParen)
            {
                Advance();
                var value = ParseExpression();
                Expect(TokenKind.RightParen);
                return value;
            }

            if (current.Kind == TokenKind.Integer)
            {
                return SyntaxValue.FromInteger(ExpectInteger());
            }

            if (current.Kind == TokenKind.Alias)
            {
                var token = current;
                Advance();
                if (aliases.TryGetValue(token.Text, out var value))
                {
                    return value;
                }

                Error($"Alias '${token.Text}' has not been defined", token);
            }

            if (current.Kind == TokenKind.Slot)
            {
                var name = current.Text;
                Advance();
                if (!slots.TryGetValue(name, out var slot))
                {
                    slot = new Placeholder();
                    slots.Add(name, slot);
                }

                return SyntaxValue.FromElement(slot);
            }

            if (current.Kind == TokenKind.Identifier)
            {
                var name = current.Text;
                Advance();
                if (current.Kind != TokenKind.LeftParen)
                {
                    return SyntaxValue.FromSymbol(name);
                }

                Advance();
                var arguments = new List<SyntaxValue>();
                if (current.Kind != TokenKind.RightParen)
                {
                    while (true)
                    {
                        arguments.Add(ParseExpression());
                        if (current.Kind != TokenKind.Comma)
                        {
                            break;
                        }

                        Advance();
                    }
                }

                Expect(TokenKind.RightParen);
                return Evaluate(name, arguments);
            }

            Error("Expected an alias, slot, integer, call, or parenthesized expression");
            return default;
        }

        private SyntaxValue Evaluate(string name, IReadOnlyList<SyntaxValue> arguments)
        {
            switch (name)
            {
                case "carrier":
                    RequireCount(name, arguments, 0, 1);
                    return SyntaxValue.FromElement(arguments.Count == 0
                        ? new Carrier()
                        : new Carrier(RequireUnit(arguments[0], "carrier(unit) requires a unit shape")));

                case "tick":
                    RequireCount(name, arguments, 1);
                    return SyntaxValue.FromElement(new TickRef(
                        RequireInteger(arguments[0], "tick(value) requires an integer")));

                case "at":
                    RequireCount(name, arguments, 2);
                    return SyntaxValue.FromElement(Location(arguments[0], arguments[1], name));

                case "point":
                    RequireCount(name, arguments, 2);
                    var pointCarrier = RequireCarrier(arguments[0], "point(carrier,tick) requires a carrier");
                    return SyntaxValue.FromElement(new Relation(
                        Location(arguments[0], arguments[1], name),
                        pointCarrier));

                case "span":
                    RequireCount(name, arguments, 3);
                    var spanCarrier = RequireCarrier(arguments[0], "span(carrier,start,end) requires a carrier");
                    return SyntaxValue.FromElement(new Relation(
                        Point(spanCarrier, arguments[1], name),
                        Point(spanCarrier, arguments[2], name)));

                case "dual":
                    RequireCount(name, arguments, 1);
                    return SyntaxValue.FromElement(
                        RequireElement(arguments[0], "dual(element) requires a Gojo element").Dual());

                case "reverse":
                    RequireCount(name, arguments, 1);
                    return SyntaxValue.FromElement(
                        RequireRelation(arguments[0], "reverse(relation) requires a relation").Reverse());

                case "orthogonal":
                    RequireCount(name, arguments, 1);
                    return SyntaxValue.FromElement(Relation.OrthogonalOutlet(
                        RequireElement(arguments[0], "orthogonal(element) requires a Gojo element")));

                case "group":
                case "ports":
                case "frame":
                    return SyntaxValue.FromElement(Relation.OrderedGroup(
                        new Carrier(),
                        [.. arguments.Select(argument => RequireElement(argument, $"{name}(...) accepts Gojo elements"))]));

                case "bind":
                    RequireCount(name, arguments, 2);
                    return SyntaxValue.FromElement(new Relation(
                        RequireElement(arguments[0], "bind(slot,element) requires a Gojo slot"),
                        RequireElement(arguments[1], "bind(slot,element) requires a Gojo element")));

                case "unit_of":
                    RequireCount(name, arguments, 1);
                    return SyntaxValue.FromUnit(
                        RequireCarrier(arguments[0], "unit_of(carrier) requires a carrier").UnitShape);

                case "resolution":
                    RequireCount(name, arguments, 2);
                    var resolutionUnit = RequireUnit(arguments[0], "resolution(unit,ticks) requires a unit shape");
                    var resolutionTicks = RequireInteger(arguments[1], "resolution(unit,ticks) requires an integer tick ratio");
                    if (resolutionTicks <= 0)
                    {
                        Error("resolution(unit,ticks) requires a positive tick ratio");
                    }

                    try
                    {
                        return SyntaxValue.FromUnit(UnitShape.ResolutionOf(resolutionUnit, resolutionTicks));
                    }
                    catch (OverflowException)
                    {
                        throw SyntaxError("The combined resolution exceeds the supported tick range");
                    }

                case "product_units":
                    RequireAtLeast(name, arguments, 1);
                    return SyntaxValue.FromUnit(UnitShape.Product(
                        [.. arguments.Select(argument => RequireUnit(argument, "product_units(...) accepts unit shapes"))]));

                case "subdivision":
                    RequireCount(name, arguments, 2);
                    var subdivisionUnit = RequireUnit(arguments[0], "subdivision(unit,parts) requires a unit shape");
                    var subdivisionParts = RequireInteger(arguments[1], "subdivision(unit,parts) requires an integer");
                    if (subdivisionParts <= 0) Error("subdivision(unit,parts) requires positive parts");
                    try { return SyntaxValue.FromUnit(UnitShape.SubdivisionOf(subdivisionUnit, subdivisionParts)); }
                    catch (OverflowException) { throw SyntaxError("The combined subdivision exceeds the supported tick range"); }

                case "quotient_unit":
                    RequireCount(name, arguments, 1);
                    var quotientExpression = RequireRelation(arguments[0], "quotient_unit(expression) requires a unit quotient relation");
                    if (!UnitQuotient.TryReadUnit(quotientExpression, out var quotientUnit))
                    {
                        Error("quotient_unit(expression) requires complete one-tick factors and a complete standard");
                    }
                    return SyntaxValue.FromUnit(quotientUnit);

                case "additive_units":
                    RequireAtLeast(name, arguments, 1);
                    return SyntaxValue.FromUnit(UnitShape.Additive(
                        [.. arguments.Select(argument => RequireUnit(argument, "additive_units(...) accepts unit shapes"))]));

                case "crystal":
                    RequireCount(name, arguments, 1);
                    var crystalSource = RequireRelation(arguments[0], "crystal(relation) requires a readable relation");
                    if (!crystalSource.TryCrystallize(out var crystal))
                    {
                        Error("The relation does not currently expose a crystallizable unit shape");
                    }

                    return SyntaxValue.FromElement(crystal);

                case "basis":
                    RequireCount(name, arguments, 3);
                    return SyntaxValue.FromElement(BoundaryFlowLaneBasis.Create(
                        new Carrier(),
                        Boundary(arguments[1]),
                        Flow(arguments[2]),
                        RequireCarrier(arguments[0], "basis(carrier,boundary,flow) requires a carrier")));

                case "lane":
                    RequireCount(name, arguments, 1);
                    return SyntaxValue.FromElement(BoundaryFlowLaneSurface.Create(
                        new Carrier(),
                        Lane(arguments[0])));

                case "lane_action":
                    RequireCount(name, arguments, 4);
                    return SyntaxValue.FromElement(BoundaryFlowLaneTransitionSurface.Create(
                        new Carrier(),
                        ReadLane(arguments[0], "lane_action(source,target,operation,witness) requires lane(...) as its source"),
                        ReadLane(arguments[1], "lane_action(source,target,operation,witness) requires lane(...) as its target"),
                        RequireRelation(arguments[2], "lane_action(...) requires relation-shaped operation evidence"),
                        RequireElement(arguments[3], "lane_action(...) requires a witness or open slot")));

                case "value":
                    RequireCount(name, arguments, 4, 5);
                    var valueCarrier = RequireCarrier(arguments[0], "value(carrier,magnitude,lane,basis[,residue]) requires a carrier");
                    var magnitude = RequireInteger(arguments[1], "value(...) requires an integer magnitude");
                    if (magnitude < 0)
                    {
                        Error("value(...) magnitude must be nonnegative; choose a boundary/flow lane for orientation");
                    }

                    var laneSurface = RequireRelation(arguments[2], "value(...) requires lane(...) as its third argument");
                    if (!BoundaryFlowLaneSurface.TryRead(laneSurface, out var occupiedLane, out _, out _))
                    {
                        Error("The value lane is not a readable boundary/flow lane surface");
                    }

                    return SyntaxValue.FromElement(BoundaryFlowValueSurface.Create(
                        new Carrier(),
                        valueCarrier,
                        checked((ulong)magnitude),
                        occupiedLane,
                        RequireElement(arguments[3], "value(...) requires a basis element"),
                        arguments.Count == 5
                            ? RequireElement(arguments[4], "value(...) residue must be a Gojo element")
                            : new Placeholder()));

                case "act":
                    RequireCount(name, arguments, 5);
                    return SyntaxValue.FromElement(ReadWitness.Create(
                        new Carrier(),
                        RequireElement(arguments[0], "act(selected,operation,result,residue,provenance) requires a selected element"),
                        RequireRelation(arguments[1], "act(...) requires relation-shaped operation evidence"),
                        RequireElement(arguments[2], "act(...) requires a result element"),
                        RequireElement(arguments[3], "act(...) requires residue or an open slot"),
                        RequireElement(arguments[4], "act(...) requires provenance or an open slot")));

                case "action":
                    RequireCount(name, arguments, 5);
                    return SyntaxValue.FromElement(TransformSurface.Create(
                        new Carrier(),
                        RequireElement(arguments[0], "action(inputs,operation,outputs,residue,provenance) requires input ports"),
                        RequireRelation(arguments[1], "action(...) requires relation-shaped operation structure"),
                        RequireElement(arguments[2], "action(...) requires output ports"),
                        RequireElement(arguments[3], "action(...) requires a residue policy or open slot"),
                        RequireElement(arguments[4], "action(...) requires provenance or an open slot")));

                case "access":
                    RequireCount(name, arguments, 6);
                    return SyntaxValue.FromElement(AccessCapabilitySurface.Create(
                        new Carrier(),
                        RequireElement(arguments[0], "access(ambient,exposed,capabilities,blocked,residue,provenance) requires an ambient basis"),
                        RequirePorts(arguments[1], "access(...) requires exposed ports"),
                        RequirePorts(arguments[2], "access(...) requires action capability ports"),
                        RequirePorts(arguments[3], "access(...) requires blocked-attempt ports"),
                        RequireElement(arguments[4], "access(...) requires residue/tension or an open slot"),
                        RequireElement(arguments[5], "access(...) requires provenance or an open slot")));

                case "count":
                    RequireCount(name, arguments, 2);
                    return SyntaxValue.FromElement(RepeatSurface.Count(
                        RequireCarrier(arguments[0], "count(carrier,ticks) requires a controller carrier"),
                        RequireInteger(arguments[1], "count(carrier,ticks) requires an integer")));

                case "repeat":
                    RequireCount(name, arguments, 5, 6);
                    return SyntaxValue.FromElement(RepeatSurface.Create(
                        new Carrier(),
                        RequireElement(arguments[0], "repeat(...) requires selected structure"),
                        RequireElement(arguments[1], "repeat(...) requires controller structure"),
                        RequireElement(arguments[2], "repeat(...) requires transition/action structure"),
                        RequireElement(arguments[3], "repeat(...) requires feedback structure"),
                        RequireElement(arguments[4], "repeat(...) requires residue or stop structure"),
                        arguments.Count == 6
                            ? RequireElement(arguments[5], "repeat(...) timeline must be a Gojo element")
                            : new Placeholder()));

                case "step":
                    RequireCount(name, arguments, 7);
                    return SyntaxValue.FromElement(TimeStepSequence.Step(
                        new Carrier(),
                        RequireCarrier(arguments[0], "step(time,start,end,before,transition,after,residue) requires a time carrier"),
                        RequireInteger(arguments[1], "step(...) start requires an integer tick"),
                        RequireInteger(arguments[2], "step(...) end requires an integer tick"),
                        RequireElement(arguments[3], "step(...) requires before evidence"),
                        RequireElement(arguments[4], "step(...) requires transition evidence"),
                        RequireElement(arguments[5], "step(...) requires after evidence"),
                        RequireElement(arguments[6], "step(...) requires residue or an open slot")));

                case "timeline":
                    RequireAtLeast(name, arguments, 2);
                    return SyntaxValue.FromElement(TimeStepSequence.Create(
                        new Carrier(),
                        RequireCarrier(arguments[0], "timeline(time,initial[,steps...]) requires a time carrier"),
                        RequireElement(arguments[1], "timeline(...) requires initial evidence"),
                        [.. arguments.Skip(2).Select(argument =>
                            RequireRelation(argument, "timeline(...) accepts relation-shaped steps"))]));

                default:
                    Error($"Unknown syntax constructor '{name}'");
                    return default;
            }
        }

        private CarrierLocation Location(SyntaxValue carrierValue, SyntaxValue tickValue, string constructor)
        {
            var carrier = RequireCarrier(carrierValue, $"{constructor}(carrier,tick) requires a carrier");
            return tickValue.Kind switch
            {
                SyntaxValueKind.Integer => new CarrierLocation(carrier, tickValue.Integer),
                SyntaxValueKind.Element when tickValue.Element is TickRef tick => new CarrierLocation(carrier, tick),
                _ => throw SyntaxError($"{constructor}(carrier,tick) requires an integer or tick(...) reference")
            };
        }

        private Relation Point(Carrier carrier, SyntaxValue tickValue, string constructor) =>
            new(Location(SyntaxValue.FromElement(carrier), tickValue, constructor), carrier);

        private BoundaryExposure Boundary(SyntaxValue value) => RequireSymbol(value, "basis boundary") switch
        {
            "P" => BoundaryExposure.Positive,
            "N" => BoundaryExposure.Negative,
            "PN" => BoundaryExposure.FullSigned,
            "S" => BoundaryExposure.BoundedWhole,
            var symbol => throw SyntaxError($"Unknown boundary exposure '{symbol}'; expected P, N, PN, or S")
        };

        private FlowExposure Flow(SyntaxValue value) => RequireSymbol(value, "basis flow") switch
        {
            "F" => FlowExposure.Forward,
            "I" => FlowExposure.Inverse,
            "FI" => FlowExposure.Both,
            "None" => FlowExposure.None,
            var symbol => throw SyntaxError($"Unknown flow exposure '{symbol}'; expected F, I, FI, or None")
        };

        private BoundaryFlowLane Lane(SyntaxValue value) => RequireSymbol(value, "lane") switch
        {
            "PF" => BoundaryFlowLane.PositiveForward,
            "PI" => BoundaryFlowLane.PositiveInverse,
            "NF" => BoundaryFlowLane.NegativeForward,
            "NI" => BoundaryFlowLane.NegativeInverse,
            "PNone" => BoundaryFlowLane.PositiveNone,
            "NNone" => BoundaryFlowLane.NegativeNone,
            "SF" => BoundaryFlowLane.WholeForward,
            "SI" => BoundaryFlowLane.WholeInverse,
            "SNone" => BoundaryFlowLane.WholeNone,
            var symbol => throw SyntaxError($"Unknown occupied lane '{symbol}'")
        };

        private BoundaryFlowLane ReadLane(SyntaxValue value, string message)
        {
            var surface = RequireRelation(value, message);
            if (BoundaryFlowLaneSurface.TryRead(surface, out var lane, out _, out _))
            {
                return lane;
            }

            throw SyntaxError(message);
        }

        private IElement RequireElement(SyntaxValue value, string message) =>
            value.Kind == SyntaxValueKind.Element && value.Element is not null
                ? value.Element
                : throw SyntaxError(message);

        private Carrier RequireCarrier(SyntaxValue value, string message) =>
            RequireElement(value, message) is Carrier carrier
                ? carrier
                : throw SyntaxError(message);

        private Relation RequireRelation(SyntaxValue value, string message) =>
            RequireElement(value, message) is Relation relation
                ? relation
                : throw SyntaxError(message);

        private IElement RequirePorts(SyntaxValue value, string message)
        {
            var ports = RequireElement(value, message);
            if (TransformSurface.TryReadPorts(ports, out _))
            {
                return ports;
            }

            throw SyntaxError(message);
        }

        private UnitShape RequireUnit(SyntaxValue value, string message) =>
            value.Kind == SyntaxValueKind.Unit && value.Unit is not null
                ? value.Unit
                : throw SyntaxError(message);

        private long RequireInteger(SyntaxValue value, string message) =>
            value.Kind == SyntaxValueKind.Integer
                ? value.Integer
                : throw SyntaxError(message);

        private string RequireSymbol(SyntaxValue value, string context) =>
            value.Kind == SyntaxValueKind.Symbol && value.Symbol is not null
                ? value.Symbol
                : throw SyntaxError($"{context} requires a symbolic basis mark");

        private void RequireCount(string name, IReadOnlyList<SyntaxValue> arguments, int count)
        {
            if (arguments.Count != count)
            {
                Error($"{name}(...) expects {count} argument{(count == 1 ? string.Empty : "s")}");
            }
        }

        private void RequireCount(string name, IReadOnlyList<SyntaxValue> arguments, int minimum, int maximum)
        {
            if (arguments.Count < minimum || arguments.Count > maximum)
            {
                Error($"{name}(...) expects between {minimum} and {maximum} arguments");
            }
        }

        private void RequireAtLeast(string name, IReadOnlyList<SyntaxValue> arguments, int minimum)
        {
            if (arguments.Count < minimum)
            {
                Error($"{name}(...) expects at least {minimum} argument{(minimum == 1 ? string.Empty : "s")}");
            }
        }

        private bool IsIdentifier(string text) =>
            current.Kind == TokenKind.Identifier && current.Text == text;

        private void ExpectIdentifier(string text)
        {
            if (!IsIdentifier(text))
            {
                Error($"Expected '{text}'");
            }

            Advance();
        }

        private long ExpectInteger()
        {
            var token = Expect(TokenKind.Integer);
            if (!long.TryParse(token.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            {
                Error($"Integer '{token.Text}' is outside the supported signed 64-bit range", token);
            }

            return value;
        }

        private Token Expect(TokenKind kind)
        {
            if (current.Kind != kind)
            {
                Error($"Expected {Display(kind)}, found {Display(current.Kind)}");
            }

            var token = current;
            Advance();
            return token;
        }

        private void Advance() => current = lexer.Next();

        private void Error(string message) => throw SyntaxError(message);

        private void Error(string message, Token token) =>
            throw new GojoSyntaxException(message, token.Line, token.Column);

        private GojoSyntaxException SyntaxError(string message) =>
            new(message, current.Line, current.Column);

        private static string Display(TokenKind kind) => kind switch
        {
            TokenKind.End => "end of document",
            TokenKind.Identifier => "an identifier",
            TokenKind.Alias => "an alias",
            TokenKind.Slot => "a slot",
            TokenKind.Integer => "an integer",
            TokenKind.LeftParen => "'('",
            TokenKind.RightParen => "')'",
            TokenKind.Comma => "','",
            TokenKind.Semicolon => "';'",
            TokenKind.Equals => "'='",
            TokenKind.Arrow => "'->'",
            _ => kind.ToString()
        };
    }

    private readonly record struct SyntaxValue(
        SyntaxValueKind Kind,
        IElement? Element,
        UnitShape? Unit,
        long Integer,
        string? Symbol)
    {
        public static SyntaxValue FromElement(IElement element) =>
            new(SyntaxValueKind.Element, element, null, 0, null);

        public static SyntaxValue FromUnit(UnitShape unit) =>
            new(SyntaxValueKind.Unit, null, unit, 0, null);

        public static SyntaxValue FromInteger(long value) =>
            new(SyntaxValueKind.Integer, null, null, value, null);

        public static SyntaxValue FromSymbol(string symbol) =>
            new(SyntaxValueKind.Symbol, null, null, 0, symbol);
    }

    private enum SyntaxValueKind
    {
        None,
        Element,
        Unit,
        Integer,
        Symbol
    }

    private sealed class Lexer(string source)
    {
        private int index;
        private int line = 1;
        private int column = 1;

        public Token Next()
        {
            SkipTrivia();
            if (index >= source.Length)
            {
                return new Token(TokenKind.End, string.Empty, line, column);
            }

            var startLine = line;
            var startColumn = column;
            var character = source[index];
            switch (character)
            {
                case '(':
                    Advance();
                    return new Token(TokenKind.LeftParen, "(", startLine, startColumn);
                case ')':
                    Advance();
                    return new Token(TokenKind.RightParen, ")", startLine, startColumn);
                case ',':
                    Advance();
                    return new Token(TokenKind.Comma, ",", startLine, startColumn);
                case ';':
                    Advance();
                    return new Token(TokenKind.Semicolon, ";", startLine, startColumn);
                case '=':
                    Advance();
                    return new Token(TokenKind.Equals, "=", startLine, startColumn);
                case '$':
                    Advance();
                    return ReadNamed(TokenKind.Alias, startLine, startColumn);
                case '?':
                    Advance();
                    return ReadNamed(TokenKind.Slot, startLine, startColumn);
                case '-' when Peek(1) == '>':
                    Advance();
                    Advance();
                    return new Token(TokenKind.Arrow, "->", startLine, startColumn);
                case '-' when char.IsDigit(Peek(1)):
                    return ReadInteger(startLine, startColumn);
                default:
                    if (char.IsDigit(character))
                    {
                        return ReadInteger(startLine, startColumn);
                    }

                    if (IsNameStart(character))
                    {
                        return ReadIdentifier(startLine, startColumn);
                    }

                    throw new GojoSyntaxException($"Unexpected character '{character}'", startLine, startColumn);
            }
        }

        private void SkipTrivia()
        {
            while (index < source.Length)
            {
                if (char.IsWhiteSpace(source[index]))
                {
                    Advance();
                    continue;
                }

                if (source[index] == '/' && Peek(1) == '/')
                {
                    while (index < source.Length && source[index] != '\n')
                    {
                        Advance();
                    }

                    continue;
                }

                break;
            }
        }

        private Token ReadNamed(TokenKind kind, int startLine, int startColumn)
        {
            if (index >= source.Length || !IsNameStart(source[index]))
            {
                throw new GojoSyntaxException(
                    kind == TokenKind.Alias ? "Expected a name after '$'" : "Expected a name after '?'",
                    startLine,
                    startColumn);
            }

            var start = index;
            while (index < source.Length && IsNamePart(source[index]))
            {
                Advance();
            }

            return new Token(kind, source[start..index], startLine, startColumn);
        }

        private Token ReadIdentifier(int startLine, int startColumn)
        {
            var start = index;
            while (index < source.Length && IsNamePart(source[index]))
            {
                Advance();
            }

            return new Token(TokenKind.Identifier, source[start..index], startLine, startColumn);
        }

        private Token ReadInteger(int startLine, int startColumn)
        {
            var start = index;
            if (source[index] == '-')
            {
                Advance();
            }

            while (index < source.Length && char.IsDigit(source[index]))
            {
                Advance();
            }

            return new Token(TokenKind.Integer, source[start..index], startLine, startColumn);
        }

        private char Peek(int offset)
        {
            var target = index + offset;
            return target >= 0 && target < source.Length ? source[target] : '\0';
        }

        private void Advance()
        {
            if (index >= source.Length)
            {
                return;
            }

            if (source[index] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }

            index++;
        }

        private static bool IsNameStart(char character) =>
            char.IsLetter(character) || character == '_';

        private static bool IsNamePart(char character) =>
            char.IsLetterOrDigit(character) || character == '_';
    }

    private readonly record struct Token(TokenKind Kind, string Text, int Line, int Column);

    private enum TokenKind
    {
        End,
        Identifier,
        Alias,
        Slot,
        Integer,
        LeftParen,
        RightParen,
        Comma,
        Semicolon,
        Equals,
        Arrow
    }
}
