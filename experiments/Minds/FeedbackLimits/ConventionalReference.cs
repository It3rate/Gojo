using System.Numerics;

namespace Minds.FeedbackLimits;

/// <summary>
/// Independent evaluation-boundary arithmetic. No Gojo readers or scalar helpers.
/// Fractions, message history and unused requests are retained just as explicitly
/// as in the compared Gojo contract; this is not a deliberately weakened baseline.
/// </summary>
public static class ConventionalReference
{
    private readonly record struct Fraction
    {
        public BigInteger N { get; }
        public BigInteger D { get; }
        public Fraction(BigInteger n, BigInteger d)
        {
            if (d.IsZero) throw new DivideByZeroException();
            if (d.Sign < 0) { n = -n; d = -d; }
            var gcd = BigInteger.GreatestCommonDivisor(n, d); N = n / gcd; D = d / gcd;
        }
        public static implicit operator Fraction(long n) => new(n, 1);
        public static Fraction operator +(Fraction a, Fraction b) => new(a.N * b.D + b.N * a.D, a.D * b.D);
        public static Fraction operator -(Fraction a, Fraction b) => new(a.N * b.D - b.N * a.D, a.D * b.D);
        public static Fraction operator *(Fraction a, Fraction b) => new(a.N * b.N, a.D * b.D);
        public static Fraction operator /(Fraction a, Fraction b) => new(a.N * b.D, a.D * b.N);
        public Fraction Abs() => new(BigInteger.Abs(N), D);
        public long Truncated => checked((long)(N / D));
        public override string ToString() => D.IsOne ? N.ToString() : $"{N}/{D}";
    }
    private sealed class Local(long initial, long ticks, long units)
    {
        public long Value = initial, Tension, Applications;
        public readonly long Ticks = ticks, Units = units;
        public Fraction Reference = 2 * units;
        public Fraction? Published, Received, Requested, Whole, Remainder;
        public int? Latest, Consumed;
        public Fraction Native => new(Value, Ticks);
        public Fraction Common => Native / Units;
    }
    private sealed record Message(int Source, int Sequence, Fraction Value);

    public static StudyTrace Run(StudyCase c, IReadOnlyList<TapeEvent> tape)
    {
        var parts = new[] { new Local(c.StartA * c.ATicks, c.ATicks, 1), new Local(c.StartB * c.BUnits * c.BTicks, c.BTicks, c.BUnits) };
        var messages = new Dictionary<int, Message>(); var pending = new HashSet<int>();
        var events = new List<StudyFrame>(); var rounds = new List<StudyFrame>();
        Fraction[] priorEvent = [parts[0].Common, parts[1].Common], priorRound = [.. priorEvent];
        var publications = 0; var deliveries = 0; var admissions = 0; var drops = 0; var calls = 0;
        Capture(null, null); Complete(0);
        for (var index = 0; index < tape.Count; index++)
        {
            var e = tape[index]; var p = parts[e.Side]; bool? accepted = null;
            switch (e.Action)
            {
                case TapeAction.Publish:
                    messages.Add(e.Message, new Message(e.Side, e.Message, p.Native));
                    p.Published = p.Native; pending.Add(e.Message); publications++; break;
                case TapeAction.Deliver:
                    var m = messages[e.Message]; deliveries++; pending.Remove(e.Message);
                    accepted = p.Latest is null || m.Sequence > p.Latest;
                    if (accepted.Value)
                    { p.Received = m.Value * p.Units / parts[m.Source].Units; p.Latest = m.Sequence; admissions++; }
                    break;
                case TapeAction.Drop: drops++; pending.Remove(e.Message); break;
                case TapeAction.Respond:
                    accepted = Respond(p, p.Received ?? throw new InvalidDataException("Missing reference receipt"), c.ResponseDenominator, true); break;
                case TapeAction.ObserveReference: p.Reference = e.Value; break;
                case TapeAction.RespondToReference: accepted = Respond(p, p.Reference, 1, false); break;
                case TapeAction.Skip: break;
                default: throw new InvalidDataException("Unknown host event");
            }
            Capture(e, accepted);
            if (index == tape.Count - 1 || tape[index + 1].Round != e.Round) Complete(e.Round);
        }
        return new StudyTrace(events, rounds);

        bool Respond(Local p, Fraction target, long denominator, bool consume)
        {
            calls++;
            p.Requested = (target - p.Native) * p.Ticks / denominator;
            var delta = p.Requested.Value.Truncated;
            p.Whole = delta; p.Remainder = p.Requested.Value - delta;
            if (p.Applications >= c.ApplicationLimit || p.Tension != 0 || consume && p.Consumed == p.Latest) return false;
            var total = checked(p.Value + p.Tension + delta);
            p.Value = Math.Max(0, total); p.Tension = Math.Min(0, total); p.Applications++;
            if (consume) p.Consumed = p.Latest;
            return true;
        }
        void Capture(TapeEvent? e, bool? accepted)
        { events.Add(Frame(e, accepted, priorEvent)); priorEvent = [parts[0].Common, parts[1].Common]; }
        void Complete(int round)
        { rounds.Add(Frame(null, null, priorRound) with { Index = events.Count - 1, Round = round, Action = "Round" }); priorRound = [parts[0].Common, parts[1].Common]; }
        StudyFrame Frame(TapeEvent? e, bool? accepted, Fraction[] previous)
        {
            var a = parts[0].Common; var b = parts[1].Common; var da = a - previous[0]; var db = b - previous[1];
            // Formulae are deliberately independent of PairObserver's composition reads.
            var values = new Dictionary<string, string> {
                ["a"] = a.ToString(), ["b"] = b.ToString(), ["mean"] = ((a + b) / 2).ToString(),
                ["gap"] = (b - a).ToString(), ["da"] = da.ToString(), ["db"] = db.ToString(),
                ["movement"] = (da.Abs() + db.Abs()).ToString(), ["meanChange"] = ((da + db) / 2).ToString(),
                ["aReferenceGap"] = (parts[0].Reference - a).ToString(),
                ["bReferenceGap"] = (parts[1].Reference / parts[1].Units - b).ToString()
            };
            for (var i = 0; i < 2; i++)
            {
                var p = parts[i]; var prefix = i == 0 ? "a." : "b.";
                values[prefix + "actuator"] = p.Value.ToString(); values[prefix + "tension"] = p.Tension.ToString();
                values[prefix + "applications"] = p.Applications.ToString(); values[prefix + "remaining"] = (c.ApplicationLimit - p.Applications).ToString();
                values[prefix + "reference"] = p.Reference.ToString(); values[prefix + "published"] = p.Published?.ToString() ?? "none";
                values[prefix + "received"] = p.Received?.ToString() ?? "none"; values[prefix + "latestSequence"] = p.Latest?.ToString() ?? "none";
                values[prefix + "consumedSequence"] = p.Consumed?.ToString() ?? "none"; values[prefix + "requested"] = p.Requested?.ToString() ?? "none";
                values[prefix + "whole"] = p.Whole?.ToString() ?? "none"; values[prefix + "fraction"] = p.Remainder?.ToString() ?? "none";
            }
            values["publications"] = publications.ToString(); values["deliveryCalls"] = deliveries.ToString();
            values["admissions"] = admissions.ToString(); values["drops"] = drops.ToString();
            values["applicationCalls"] = calls.ToString(); values["pending"] = pending.Count.ToString();
            return new StudyFrame(events.Count, e?.Round ?? 0, e?.Action.ToString() ?? "Initial", e is null ? "" : e.Side == 0 ? "A" : "B", e?.Message ?? 0, e?.Note ?? "", accepted, values);
        }
    }
}
