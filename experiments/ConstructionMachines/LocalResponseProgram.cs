using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>A finite reader for disjoint-domain, layered affine programs.
/// Constant cells, regression and bounded neural layers use the same grammar.</summary>
public sealed class LocalResponseProgram
{
    public const long InputScale = 1024, OutputScale = 4096;
    public const int InputCount = 6;
    // Priority is explicit: equal predicted utility keeps the old twelve-tick law.
    public static readonly long[] Caps = [12, 4, 8, 24, 48, 96];
    public Relation Surface { get; }
    public Carrier[] Inputs => F(F(Surface)[0]).Cast<Carrier>().ToArray();
    public Carrier[] Outputs => F(F(Surface)[1]).Cast<Carrier>().ToArray();
    public Relation[] Cells => F(F(Surface)[2]).Cast<Relation>().ToArray();
    public Relation[] Actions => F(F(Surface)[3]).Cast<Relation>().ToArray();

    public LocalResponseProgram(Relation surface)
    {
        if (F(surface).Length != 5) throw new ArgumentException("An exact local response program is required.");
        Surface = surface;
    }

    public sealed record Cell(long[][] Domains, double[][][] Weights, int Support, IElement Source);

    public static LocalResponseProgram Build(Cell[] cells, IElement trainingReceipt)
    {
        var inputs = Enumerable.Range(0, InputCount).Select(_ => new Carrier()).ToArray();
        var outputs = Caps.Select(_ => new Carrier()).ToArray(); var capAxis = new Carrier();
        var nativeCells = cells.Select(cell =>
        {
            var previous = inputs; long previousScale = InputScale; var layers = new List<IElement>(); var imports = new List<IElement>();
            for (var layer = 0; layer < cell.Weights.Length; layer++)
            {
                var last = layer == cell.Weights.Length - 1;
                var axes = last ? outputs : cell.Weights[layer].Select(_ => new Carrier()).ToArray();
                var neurons = cell.Weights[layer].Select((weights, i) =>
                {
                    if (weights.Length != previous.Length + 1) throw new ArgumentException("A node needs one bias and its actual input frames.");
                    var fine = new Carrier(); var importMap = new Relation(Relation.Segment(fine, 0, 2), Relation.Segment(axes[i], 0, 1));
                    foreach (var weight in weights)
                    {
                        var q = Quantize(weight);
                        // A lossy import promises the rounded coefficient and a
                        // half-tick preimage, not recovery of the fitter's double.
                        imports.Add(G(new CarrierLocation(axes[i], q), importMap,
                            Relation.Segment(fine, checked(2 * q - 1), checked(2 * q + 1)), cell.Source));
                    }
                    var maps = previous.Select((axis, j) => (IElement)new Relation(Relation.Segment(axis, 0, previousScale),
                        Relation.Segment(axes[i], 0, Quantize(weights[j + 1])))).ToArray();
                    return (IElement)G(new CarrierLocation(axes[i], Quantize(weights[0])), G(maps),
                        Relation.Segment(axes[i], last ? -OutputScale : 0, last ? OutputScale : 4 * OutputScale),
                        Relation.Segment(axes[i], 0, 1));
                }).ToArray();
                layers.Add(G(neurons)); previous = axes; previousScale = OutputScale;
            }
            return (IElement)G(G(cell.Domains.Select((d, i) => (IElement)Relation.Segment(inputs[i], d[0], d[1])).ToArray()),
                G(layers.ToArray()), new CarrierLocation(new Carrier(), cell.Support), G(cell.Source, G(imports.ToArray())));
        }).ToArray();
        return new(G(G(inputs), G(outputs), G(nativeCells), G(Caps.Select(cap => (IElement)Relation.Segment(capAxis, -cap, cap)).ToArray()), trainingReceipt));
    }

    private static long Quantize(double value)
    {
        if (!double.IsFinite(value) || Math.Abs(value) > 1024) throw new ArgumentOutOfRangeException(nameof(value));
        return checked((long)Math.Round(value * OutputScale, MidpointRounding.ToEven));
    }

    /// <summary>Predicts relative one-step relief, not probabilities. Every
    /// selected node keeps exact pre-clipping participation and finite loss.</summary>
    public Relation Predict(Relation context)
    {
        if (F(context)[2] is not Relation vector || !vector.TryReadOrderedGroup(out var supplied) || supplied.Length != Inputs.Length)
            return Pending(context);
        var values = new Dictionary<Carrier, IElement>();
        for (var i = 0; i < supplied.Length; i++)
        {
            if (!ProjectionResidue.TryRead(supplied[i], out var axis, out _, out _) || !axis.SameIdentityAs(Inputs[i])) return Pending(context);
            values[Inputs[i]] = supplied[i];
        }
        var matches = Cells.Where(cell => F(F(cell)[0]).Select((domain, i) => Contains((Relation)domain, supplied[i])).All(x => x)).ToArray();
        if (matches.Length != 1) return Pending(G(context, G(matches)));
        var selected = matches[0]; var proofs = F(F(selected)[0]).Select((domain, i) =>
        {
            TimeStepSequence.TryReadSpan((Relation)domain, out var a, out var b);
            return (IElement)OperationClaim.Between(new Carrier(), supplied[i], a, b, new Carrier());
        }).ToArray();
        var traces = new List<IElement>();
        foreach (var layer in F(F(selected)[1]))
        {
            var next = new Dictionary<Carrier, IElement>();
            foreach (var neuron in F(layer))
            {
                var n = F(neuron); var bias = (CarrierLocation)n[0]; var raw = new Fraction(bias.Ticks);
                var participants = new List<IElement>();
                foreach (var map in F(n[1]).Cast<Relation>())
                {
                    if (!TimeStepSequence.TryReadSpan((Relation)map.Left, out var a, out var b) ||
                        !TimeStepSequence.TryReadSpan((Relation)map.Right, out var c, out var d) || a.Ticks == b.Ticks ||
                        !c.Carrier.SameIdentityAs(bias.Carrier) || !values.TryGetValue(a.Carrier, out var source)) return Pending(G(context, neuron));
                    var contribution = new Fraction(c.Ticks) + (Fraction.Read(source) - new Fraction(a.Ticks)) *
                        new Fraction(d.Ticks - c.Ticks, b.Ticks - a.Ticks);
                    raw += contribution; participants.Add(new Relation(source, map));
                }
                TimeStepSequence.TryReadSpan((Relation)n[2], out var lo, out var hi);
                TimeStepSequence.TryReadSpan((Relation)n[3], out _, out var quantum);
                if (!lo.Carrier.SameIdentityAs(bias.Carrier) || quantum.Ticks <= 0) return Pending(G(context, neuron));
                var clipped = Fraction.Clamp(raw, new Fraction(lo.Ticks), new Fraction(hi.Ticks));
                var whole = clipped.Truncate(quantum.Ticks); var result = new CarrierLocation(bias.Carrier, whole);
                traces.Add(ReadWitness.Create(new Carrier(), G(participants.ToArray()), (Relation)neuron, result,
                    G((raw - clipped).Encode(bias.Carrier), (clipped - new Fraction(whole)).Encode(bias.Carrier)), G(raw.Encode(bias.Carrier))));
                next[bias.Carrier] = result;
            }
            values = next;
        }
        if (Outputs.Any(axis => !values.ContainsKey(axis))) return Pending(context);
        return ReadWitness.Create(new Carrier(), context, Surface, G(Outputs.Select(axis => values[axis]).ToArray()),
            G(), G(selected, G(proofs), G(traces.ToArray())));

        Relation Pending(IElement why) => ReadWitness.Create(new Carrier(), context, Surface, new Placeholder(), G(why), G());
    }

    public Relation Select(Relation prediction)
    {
        if (!ReferenceEquals(F(prediction)[1], Surface) || F(prediction)[2] is not Relation result ||
            !result.TryReadOrderedGroup(out var scores) || scores.Length != Actions.Length ||
            scores.Where((score, i) => !ProjectionResidue.TryRead(score, out var basis, out _, out _) || !basis.SameIdentityAs(Outputs[i])).Any())
            return ReadWitness.Create(new Carrier(), prediction, G(Actions), new Placeholder(), G(prediction), G());
        var best = 0;
        for (var i = 1; i < scores.Length; i++) if (Fraction.Read(scores[best]) < Fraction.Read(scores[i])) best = i;
        // Each score has its own advertised output basis. Explicitly project
        // them to one comparison basis before claiming an ordering.
        var axis = new Carrier(); var candidates = scores.Select((score, i) =>
        {
            ProjectionResidue.TryRead(score, out var source, out _, out _);
            var map = new Relation(Relation.Segment(source, 0, OutputScale), Relation.Segment(axis, 0, OutputScale));
            return (IElement)G(Actions[i], score, map, Fraction.Read(score).Encode(axis));
        }).ToArray();
        var claims = candidates.Select(c => (IElement)OperationClaim.AtLeast(new Carrier(), F(candidates[best])[3], F(c)[3], new Carrier())).ToArray();
        return ReadWitness.Create(new Carrier(), prediction, G(Actions), Actions[best], G(), G(G(candidates), G(claims), new CarrierLocation(new Carrier(), best)));
    }

    private static bool Contains(Relation domain, IElement value) =>
        TimeStepSequence.TryReadSpan(domain, out var a, out var b) && ProjectionResidue.TryRead(value, out var axis, out _, out _) &&
        axis.SameIdentityAs(a.Carrier) && !(Fraction.Read(value) < new Fraction(a.Ticks)) && !(Fraction.Read(value) > new Fraction(b.Ticks));

    public static int Choice(Relation selection) => F(selection)[2] is Relation action && TimeStepSequence.TryReadSpan(action, out _, out var end)
        ? Array.IndexOf(Caps, end.Ticks) : -1;
}
