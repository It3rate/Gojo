using System.Globalization;
using Gojo.Core;

namespace GojoCAD.GCode;

/// <summary>
/// Small regular G-code parser that imports a 2D tool path into GojoCAD
/// relation structure. It supports modal G0/G1/G2/G3 motion, G20/G21 units,
/// G90/G91 distance mode, F feed evidence, Z pen lift/drop, and T/M6 stroke
/// changes.
/// </summary>
public static class GCodeParser
{
    private const double InchToMillimeter = 25.4d;
    private const double Epsilon = 0.0000001d;

    public static Relation Parse(string source, GCodeParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var parseOptions = options ?? new GCodeParseOptions();
        var basis = ParserBasis.Create(parseOptions);
        var state = new ParserState(parseOptions);
        var elements = new List<IElement>();

        foreach (var line in ReadLines(source))
        {
            var words = ReadWords(StripComments(line));
            if (words.Count == 0)
            {
                continue;
            }

            elements.AddRange(ReadStateChanges(words, basis, state));

            var selectedMotion = ReadMotionCode(words, state.MotionCode);
            if (selectedMotion.HasValue)
            {
                state.MotionCode = selectedMotion.Value;
            }

            if (TryReadFeed(words, out var feed))
            {
                state.Feed = feed;
            }

            var before = state.Position;
            var next = ReadNextPosition(words, state);
            var hasXYChange = Math.Abs(next.X - before.X) > Epsilon ||
                Math.Abs(next.Y - before.Y) > Epsilon;
            var hasArcMotion = state.MotionCode is GCodeStructure.ClockwiseArc or GCodeStructure.CounterClockwiseArc &&
                words.Any(word => word.Letter is 'I' or 'J' or 'R');
            var hasZChange = Math.Abs(next.Z - before.Z) > Epsilon ||
                ReadMWord(words, 3) ||
                ReadMWord(words, 5);

            if (hasZChange)
            {
                var beforePen = GCodeStructure.PenState(basis.ZAxis, state.IsPenDown(before.Z));
                var afterPen = GCodeStructure.PenState(basis.ZAxis, state.IsPenDown(next.Z));
                if (!beforePen.Carrier.SameIdentityAs(afterPen.Carrier))
                {
                    elements.Add(GCodeStructure.PenChange(
                        new Carrier(),
                        ReadMWord(words, 5)
                            ? GCodeStructure.MWord(basis.MWordAxis, 5)
                            : GCodeStructure.GWord(basis.GWordAxis, state.MotionCode),
                        beforePen,
                        afterPen));
                }
            }

            if (hasXYChange || hasArcMotion)
            {
                elements.Add(ReadMotion(before, next, words, basis, state));
            }

            state.Position = next;
        }

        return GCodeStructure.Program(new Carrier(), basis.Structure, [.. elements]);
    }

    private static IEnumerable<IElement> ReadStateChanges(
        IReadOnlyList<GCodeWord> words,
        ParserBasis basis,
        ParserState state)
    {
        foreach (var word in words.Where(word => word.Letter == 'G'))
        {
            var code = Rounded(word.Number);
            switch (code)
            {
                case GCodeStructure.InchUnits:
                    state.UnitScale = InchToMillimeter;
                    yield return GCodeStructure.ModeChange(
                        new Carrier(),
                        GCodeStructure.GWord(basis.GWordAxis, GCodeStructure.InchUnits));
                    break;
                case GCodeStructure.MillimeterUnits:
                    state.UnitScale = 1d;
                    yield return GCodeStructure.ModeChange(
                        new Carrier(),
                        GCodeStructure.GWord(basis.GWordAxis, GCodeStructure.MillimeterUnits));
                    break;
                case GCodeStructure.AbsoluteDistance:
                    state.IsAbsolute = true;
                    yield return GCodeStructure.ModeChange(
                        new Carrier(),
                        GCodeStructure.GWord(basis.GWordAxis, GCodeStructure.AbsoluteDistance));
                    break;
                case GCodeStructure.IncrementalDistance:
                    state.IsAbsolute = false;
                    yield return GCodeStructure.ModeChange(
                        new Carrier(),
                        GCodeStructure.GWord(basis.GWordAxis, GCodeStructure.IncrementalDistance));
                    break;
            }
        }

        foreach (var tool in words.Where(word => word.Letter == 'T'))
        {
            var nextTool = Math.Max(0, Rounded(tool.Number));
            if (nextTool == state.Tool)
            {
                continue;
            }

            var before = GCodeStructure.ToolState(basis.ToolAxis, state.Tool);
            var after = GCodeStructure.ToolState(basis.ToolAxis, nextTool);
            state.Tool = nextTool;
            yield return GCodeStructure.StrokeChange(
                new Carrier(),
                GCodeStructure.MWord(basis.MWordAxis, GCodeStructure.ToolChange),
                before,
                after);
        }
    }

    private static IElement ReadMotion(
        MachinePosition before,
        MachinePosition next,
        IReadOnlyList<GCodeWord> words,
        ParserBasis basis,
        ParserState state)
    {
        var start = basis.FramePoint(before.X, before.Y);
        var end = basis.FramePoint(next.X, next.Y);
        var xFlow = GCodeStructure.AxisFlow(basis.XAxis, basis.CoordinateTicks(next.X - before.X));
        var yFlow = GCodeStructure.AxisFlow(basis.YAxis, basis.CoordinateTicks(next.Y - before.Y));
        var pen = GCodeStructure.PenState(basis.ZAxis, state.IsPenDown(next.Z));
        var tool = GCodeStructure.ToolState(basis.ToolAxis, state.Tool);
        var feed = GCodeStructure.FeedState(basis.FeedAxis, basis.FeedTicks(state.Feed));

        if (!state.IsPenDown(next.Z) ||
            state.MotionCode == GCodeStructure.RapidMove)
        {
            return GCodeStructure.TravelMotion(
                new Carrier(),
                GCodeStructure.GWord(basis.GWordAxis, state.MotionCode),
                pen,
                tool,
                start,
                end,
                xFlow,
                yFlow,
                feed);
        }

        if (state.MotionCode == GCodeStructure.ClockwiseArc ||
            state.MotionCode == GCodeStructure.CounterClockwiseArc)
        {
            return ReadArcMotion(before, next, words, basis, state, start, end, xFlow, yFlow, pen, tool, feed);
        }

        return GCodeStructure.LineMotion(
            new Carrier(),
            GCodeStructure.GWord(basis.GWordAxis, GCodeStructure.LinearMove),
            pen,
            tool,
            feed,
            start,
            end,
            xFlow,
            yFlow);
    }

    private static IElement ReadArcMotion(
        MachinePosition before,
        MachinePosition next,
        IReadOnlyList<GCodeWord> words,
        ParserBasis basis,
        ParserState state,
        Relation start,
        Relation end,
        Relation xFlow,
        Relation yFlow,
        CarrierLocation pen,
        CarrierLocation tool,
        CarrierLocation feed)
    {
        var isClockwise = state.MotionCode == GCodeStructure.ClockwiseArc;
        var center = TryReadArcCenter(words, before, next, state, out var resolvedCenter)
            ? resolvedCenter
            : new MachinePoint((before.X + next.X) * 0.5d, (before.Y + next.Y) * 0.5d);
        var centerPoint = basis.FramePoint(center.X, center.Y);
        var rendered = BuildRenderedArcPieces(
            basis,
            center,
            before,
            next,
            isClockwise);
        var radiusTicks = basis.CoordinateTicks(Distance(before.X, before.Y, center.X, center.Y));
        var sweepTicks = basis.ArcSweepTicks(center, before, next, isClockwise);
        var circumference = new Carrier(UnitShape.CircularFrom(Relation.Point(basis.ArcPhaseAxis, 0)));

        return GCodeStructure.ArcMotion(
            new Carrier(),
            GCodeStructure.GWord(basis.GWordAxis, state.MotionCode),
            pen,
            tool,
            feed,
            rendered,
            centerPoint,
            start,
            end,
            GCodeStructure.RadialElement(new Carrier(), centerPoint, start, new Carrier(), radiusTicks),
            GCodeStructure.RadialElement(new Carrier(), centerPoint, end, new Carrier(), radiusTicks),
            GCodeStructure.CircumferenceFlow(circumference, sweepTicks, isClockwise),
            xFlow,
            yFlow);
    }

    private static IElement BuildRenderedArcPieces(
        ParserBasis basis,
        MachinePoint center,
        MachinePosition before,
        MachinePosition next,
        bool isClockwise)
    {
        var centerPoint = basis.FramePoint(center.X, center.Y);
        var startAngle = Math.Atan2(before.Y - center.Y, before.X - center.X);
        var endAngle = Math.Atan2(next.Y - center.Y, next.X - center.X);
        var sweep = SignedSweep(startAngle, endAngle, isClockwise);
        if (Math.Abs(sweep) <= Epsilon &&
            Distance(before.X, before.Y, next.X, next.Y) <= Epsilon)
        {
            sweep = isClockwise ? -Math.Tau : Math.Tau;
        }

        var radius = Distance(before.X, before.Y, center.X, center.Y);
        var pieces = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 2d)));
        var arcs = new IElement[pieces];
        for (var index = 0; index < pieces; index++)
        {
            var a0 = startAngle + sweep * index / pieces;
            var a1 = startAngle + sweep * (index + 1) / pieces;
            var start = basis.FramePoint(
                center.X + Math.Cos(a0) * radius,
                center.Y + Math.Sin(a0) * radius);
            var end = basis.FramePoint(
                center.X + Math.Cos(a1) * radius,
                center.Y + Math.Sin(a1) * radius);
            arcs[index] = GraphicStructure.FrameArc(centerPoint, start, end);
        }

        return arcs.Length == 1
            ? arcs[0]
            : Relation.OrderedGroup(new Carrier(), arcs);
    }

    private static bool TryReadArcCenter(
        IReadOnlyList<GCodeWord> words,
        MachinePosition before,
        MachinePosition next,
        ParserState state,
        out MachinePoint center)
    {
        var hasI = TryReadWord(words, 'I', out var i);
        var hasJ = TryReadWord(words, 'J', out var j);
        if (hasI || hasJ)
        {
            center = new MachinePoint(before.X + (hasI ? i * state.UnitScale : 0d), before.Y + (hasJ ? j * state.UnitScale : 0d));
            return true;
        }

        if (TryReadWord(words, 'R', out var radiusWord))
        {
            return TryReadRadiusCenter(before, next, radiusWord * state.UnitScale, state.MotionCode == GCodeStructure.ClockwiseArc, out center);
        }

        center = default;
        return false;
    }

    private static bool TryReadRadiusCenter(
        MachinePosition before,
        MachinePosition next,
        double signedRadius,
        bool isClockwise,
        out MachinePoint center)
    {
        var radius = Math.Abs(signedRadius);
        var dx = next.X - before.X;
        var dy = next.Y - before.Y;
        var chord = Math.Sqrt(dx * dx + dy * dy);
        if (radius <= Epsilon || chord <= Epsilon || chord > radius * 2d + Epsilon)
        {
            center = default;
            return false;
        }

        var midX = (before.X + next.X) * 0.5d;
        var midY = (before.Y + next.Y) * 0.5d;
        var halfChord = chord * 0.5d;
        var height = Math.Sqrt(Math.Max(0d, radius * radius - halfChord * halfChord));
        var normalX = -dy / chord;
        var normalY = dx / chord;
        var candidates = new[]
        {
            new MachinePoint(midX + normalX * height, midY + normalY * height),
            new MachinePoint(midX - normalX * height, midY - normalY * height)
        };
        var wantsMajor = signedRadius < 0d;
        center = candidates
            .OrderBy(candidate =>
            {
                var startAngle = Math.Atan2(before.Y - candidate.Y, before.X - candidate.X);
                var endAngle = Math.Atan2(next.Y - candidate.Y, next.X - candidate.X);
                var sweep = Math.Abs(SignedSweep(startAngle, endAngle, isClockwise));
                return (sweep > Math.PI) == wantsMajor ? 0 : 1;
            })
            .First();
        return true;
    }

    private static MachinePosition ReadNextPosition(IReadOnlyList<GCodeWord> words, ParserState state)
    {
        var x = ReadCoordinate(words, 'X', state.Position.X, state);
        var y = ReadCoordinate(words, 'Y', state.Position.Y, state);
        var z = ReadCoordinate(words, 'Z', state.Position.Z, state);
        if (ReadMWord(words, 3))
        {
            z = Math.Min(z, state.Options.PenDownZ);
        }
        else if (ReadMWord(words, 5))
        {
            z = Math.Max(z, state.Options.PenDownZ + 1d);
        }

        return new MachinePosition(x, y, z);
    }

    private static double ReadCoordinate(
        IReadOnlyList<GCodeWord> words,
        char letter,
        double current,
        ParserState state)
    {
        if (!TryReadWord(words, letter, out var value))
        {
            return current;
        }

        var scaled = value * state.UnitScale;
        return state.IsAbsolute ? scaled : current + scaled;
    }

    private static long? ReadMotionCode(IReadOnlyList<GCodeWord> words, long current)
    {
        foreach (var word in words.Where(word => word.Letter == 'G'))
        {
            var code = Rounded(word.Number);
            if (code is GCodeStructure.RapidMove or
                GCodeStructure.LinearMove or
                GCodeStructure.ClockwiseArc or
                GCodeStructure.CounterClockwiseArc)
            {
                return code;
            }
        }

        return words.Any(word => word.Letter is 'X' or 'Y')
            ? current
            : null;
    }

    private static bool TryReadFeed(IReadOnlyList<GCodeWord> words, out double feed) =>
        TryReadWord(words, 'F', out feed);

    private static bool TryReadWord(IReadOnlyList<GCodeWord> words, char letter, out double value)
    {
        for (var index = words.Count - 1; index >= 0; index--)
        {
            if (words[index].Letter == letter)
            {
                value = words[index].Number;
                return true;
            }
        }

        value = 0d;
        return false;
    }

    private static bool ReadMWord(IReadOnlyList<GCodeWord> words, long code) =>
        words.Any(word => word.Letter == 'M' && Rounded(word.Number) == code);

    private static IReadOnlyList<string> ReadLines(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

    private static string StripComments(string line)
    {
        var text = line;
        var semicolon = text.IndexOf(';');
        if (semicolon >= 0)
        {
            text = text[..semicolon];
        }

        while (true)
        {
            var start = text.IndexOf('(');
            if (start < 0)
            {
                return text;
            }

            var end = text.IndexOf(')', start + 1);
            text = end < 0
                ? text[..start]
                : text.Remove(start, end - start + 1);
        }
    }

    private static List<GCodeWord> ReadWords(string line)
    {
        var words = new List<GCodeWord>();
        var index = 0;
        while (index < line.Length)
        {
            if (!char.IsLetter(line[index]))
            {
                index++;
                continue;
            }

            var letter = char.ToUpperInvariant(line[index]);
            index++;
            while (index < line.Length && char.IsWhiteSpace(line[index]))
            {
                index++;
            }

            var numberStart = index;
            if (index < line.Length && (line[index] == '+' || line[index] == '-'))
            {
                index++;
            }

            var hasDigit = false;
            while (index < line.Length && char.IsDigit(line[index]))
            {
                hasDigit = true;
                index++;
            }

            if (index < line.Length && line[index] == '.')
            {
                index++;
                while (index < line.Length && char.IsDigit(line[index]))
                {
                    hasDigit = true;
                    index++;
                }
            }

            if (!hasDigit)
            {
                continue;
            }

            if (double.TryParse(
                    line[numberStart..index],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                words.Add(new GCodeWord(letter, value));
            }
        }

        return words;
    }

    private static double SignedSweep(double startAngle, double endAngle, bool isClockwise)
    {
        var sweep = endAngle - startAngle;
        if (isClockwise)
        {
            while (sweep >= 0d)
            {
                sweep -= Math.Tau;
            }
        }
        else
        {
            while (sweep <= 0d)
            {
                sweep += Math.Tau;
            }
        }

        return sweep;
    }

    private static long Rounded(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        if (value >= long.MaxValue)
        {
            return long.MaxValue;
        }

        if (value <= long.MinValue)
        {
            return long.MinValue;
        }

        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private readonly record struct GCodeWord(char Letter, double Number);

    private readonly record struct MachinePoint(double X, double Y);

    private readonly record struct MachinePosition(double X, double Y, double Z);

    private sealed class ParserState
    {
        public ParserState(GCodeParseOptions options)
        {
            Options = options;
            Position = new MachinePosition(0d, 0d, options.PenDownZ);
        }

        public GCodeParseOptions Options { get; }

        public MachinePosition Position { get; set; }

        public long MotionCode { get; set; } = GCodeStructure.RapidMove;

        public bool IsAbsolute { get; set; } = true;

        public double UnitScale { get; set; } = 1d;

        public double Feed { get; set; }

        public long Tool { get; set; }

        public bool IsPenDown(double z) => z <= Options.PenDownZ;
    }

    private sealed class ParserBasis
    {
        private readonly GCodeParseOptions options;

        private ParserBasis(GCodeParseOptions options)
        {
            this.options = options;
            Frame = new Carrier();
            XAxis = new Carrier();
            YAxis = new Carrier();
            ZAxis = new Carrier();
            FeedAxis = new Carrier();
            ToolAxis = new Carrier();
            GWordAxis = new Carrier();
            MWordAxis = new Carrier();
            ArcPhaseAxis = new Carrier();
            Structure = GCodeStructure.Basis(
                new Carrier(),
                Frame,
                XAxis,
                YAxis,
                ZAxis,
                FeedAxis,
                ToolAxis,
                GWordAxis,
                MWordAxis,
                ArcPhaseAxis,
                options.SafeCoordinateTicksPerUnit,
                options.SafeArcTicksPerTurn);
        }

        public Carrier Frame { get; }

        public Carrier XAxis { get; }

        public Carrier YAxis { get; }

        public Carrier ZAxis { get; }

        public Carrier FeedAxis { get; }

        public Carrier ToolAxis { get; }

        public Carrier GWordAxis { get; }

        public Carrier MWordAxis { get; }

        public Carrier ArcPhaseAxis { get; }

        public Relation Structure { get; }

        public static ParserBasis Create(GCodeParseOptions options) => new(options);

        public Relation FramePoint(double x, double y) =>
            GCodeStructure.FramePoint(
                Frame,
                XAxis,
                YAxis,
                CoordinateTicks(x),
                CoordinateTicks(y));

        public long CoordinateTicks(double value) =>
            Rounded(value * options.SafeCoordinateTicksPerUnit);

        public long FeedTicks(double feed) =>
            Rounded(feed * options.SafeCoordinateTicksPerUnit);

        public long ArcSweepTicks(MachinePoint center, MachinePosition start, MachinePosition end, bool isClockwise)
        {
            var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
            var endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
            var sweep = SignedSweep(startAngle, endAngle, isClockwise);
            if (Math.Abs(sweep) <= Epsilon &&
                Distance(start.X, start.Y, end.X, end.Y) <= Epsilon)
            {
                sweep = isClockwise ? -Math.Tau : Math.Tau;
            }

            return Rounded(Math.Abs(sweep) / Math.Tau * options.SafeArcTicksPerTurn);
        }
    }
}
