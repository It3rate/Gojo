using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;
using static Gojo.Experiments.QuickDraw.SequentialEvidence;

namespace Gojo.Experiments.QuickDraw;

/// <summary>Frozen learned coefficients as ordinary native weighted equations.
/// Training is external numerical estimation. Execution is a local read of the
/// transported equations, with the truncation remainder retained per output.</summary>
public static class NativePrior
{
    // prior = ((S(coefficient scale,0,Q) -> Closed(feature unit frames)) ->
    //          Closed(sum equation -> output scale projection)) ->
    //         (training event -> training count)
    // Each sum uses native weighted ports. Scaling follows the whole sum, so
    // small terms are not independently rounded away.
    public sealed record Row(Relation Sum, Relation Scale, Carrier Accumulator, Carrier Output);
    public sealed record Read(long Quantum, Carrier[] Features, Row[] Rows, Relation Surface);
    public sealed record Inference(CarrierLocation[] Outputs, Relation Surface);

    public static Relation Create(long coefficientQuantum, long[][] weights, long trainingCount)
    {
        if (coefficientQuantum <= 0 || trainingCount < 0 || weights.Length != 4 || weights.Any(row => row.Length != FeatureCount))
            throw new ArgumentException("The prior requires a positive coefficient quantum and four nine-term rows.");
        var unit = new Carrier(); var features = Enumerable.Range(0, FeatureCount).Select(_ => new Carrier(unit.UnitShape)).ToArray();
        var rows = weights.Select(row => {
            var accumulator = new Carrier(unit.UnitShape); var output = new Carrier(unit.UnitShape);
            return (IElement)new Relation(Equation.Sum(accumulator, row.Select((w, i) => Equation.WeightedPort(features[i], 0, 1, 0, w)).ToArray()),
                Equation.Affine(accumulator, 0, coefficientQuantum, output, 0, 1));
        }).ToArray();
        var definition = new Relation(new Relation(Relation.Segment(unit, 0, coefficientQuantum),
            S(features.Select(feature => (IElement)Relation.Segment(feature, 0, 1)).ToArray())), S(rows));
        return new(definition, new Relation(new Placeholder(), new CarrierLocation(new Carrier(), trainingCount)));
    }

    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (surface is not { Left: Relation { Left: Relation { Left: var quantumFrame, Right: Relation featureScope }, Right: Relation rowScope },
                Right: Relation { Left: Placeholder { IsInverse: false }, Right: CarrierLocation count } } || count.Ticks < 0 ||
            !Axis(quantumFrame, out var scaleBasis, out var quantum) || quantum <= 0 || !Entries(featureScope, out var featureRows) ||
            featureRows.Length != FeatureCount || !Entries(rowScope, out var rowSurfaces) || rowSurfaces.Length != 4) return false;
        var features = new Carrier[FeatureCount]; var axes = new HashSet<Carrier>(ReferenceEqualityComparer.Instance) { scaleBasis, count.Carrier };
        for (var i = 0; i < FeatureCount; i++)
            if (!Axis(featureRows[i], out features[i], out var one) || one != 1 || !axes.Add(features[i]) ||
                !features[i].UnitShape.CompatibleWith(scaleBasis.UnitShape)) return false;
        var rows = new List<Row>();
        foreach (var item in rowSurfaces)
        {
            if (item is not Relation { Left: Relation sum, Right: Relation projection } ||
                sum.Right is not Relation { Left: CarrierLocation outputPoint, Right: Carrier accumulator } ||
                !ReferenceEquals(outputPoint.Carrier, accumulator) || outputPoint.Ticks != 1 || !axes.Add(accumulator) ||
                !Axis(projection.Left, out var input, out var q) || !ReferenceEquals(input, accumulator) || q != quantum ||
                !Axis(projection.Right, out var output, out var one) || one != 1 || !axes.Add(output) ||
                !accumulator.UnitShape.CompatibleWith(scaleBasis.UnitShape) || !output.UnitShape.CompatibleWith(scaleBasis.UnitShape)) return false;
            var ports = new IElement[FeatureCount]; var group = sum.Left;
            for (var i = FeatureCount - 1; i > 0; i--)
            {
                if (group is not Relation pair) return false;
                ports[i] = pair.Right; group = pair.Left;
            }
            ports[0] = group;
            for (var i = 0; i < FeatureCount; i++)
                if (ports[i] is not Relation { Left: var inputFrame, Right: Relation {
                    Left: Relation { Left: CarrierLocation a, Right: Carrier ca },
                    Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } } ||
                    !Axis(inputFrame, out var feature, out var fq) || fq != 1 || !ReferenceEquals(feature, features[i]) ||
                    !ReferenceEquals(ca, Carrier.Unknown) || !ReferenceEquals(cb, Carrier.Unknown) ||
                    !ReferenceEquals(a.Carrier, Carrier.Unknown) || !ReferenceEquals(b.Carrier, Carrier.Unknown) || a.Ticks != 0) return false;
            rows.Add(new(sum, projection, accumulator, output));
        }
        read = new(quantum, features, rows.ToArray(), surface); return true;
    }

    public static Inference Predict(Read model, Section section)
    {
        var inputs = section.Features.Select((feature, i) => new CarrierLocation(model.Features[i], feature.Ticks)).ToArray();
        IElement input = inputs[0]; for (var i = 1; i < inputs.Length; i++) input = new Relation(input, inputs[i]);
        var outputs = new CarrierLocation[4]; var witnesses = new List<IElement>();
        for (var i = 0; i < model.Rows.Length; i++)
        {
            var row = model.Rows[i];
            if (!Equation.TryPredict(row.Sum, input, row.Accumulator, out var sum) || !Equation.TryReadPrediction(sum, out var accumulated, out _) ||
                !Equation.TryPredict(row.Scale, accumulated, row.Output, out var scaled) || !Equation.TryReadPrediction(scaled, out outputs[i], out _))
                throw new InvalidDataException("A transported learned equation did not accept its exposed features.");
            witnesses.Add(new Relation(new Relation(row.Sum, input), new Relation(sum, new Relation(row.Scale, scaled))));
        }
        var correspondence = S(inputs.Select((value, i) => (IElement)new Relation(section.Features[i], value)).ToArray());
        return new(outputs, new Relation(new Relation(section.Surface, model.Surface), new Relation(correspondence, S(witnesses.ToArray()))));
    }
}

/// <summary>Measured finite curve constructions using the existing shared
/// interpolation DAG. Controls are generated outputs. Public messages contain
/// only observations, and reconstruction runs entirely on restored relations.</summary>
public static class NativeReconstruction
{
    // curve = (from -> to) -> (Closed(controls) -> scheme)
    // interval = (before -> after) -> (parameter -> Closed(before->parameter, parameter->after))
    // scheme = (coordinate basis -> interval) -> (Closed(interpolations) -> output)
    // program = (basis -> Closed(Closed(stroke curves))) ->
    //           (packet -> (prior -> Closed(inference/translation witnesses)))
    public sealed record Curve(Placeholder From, Placeholder To, Point[] Controls, Relation Surface);
    public sealed record Read(Frame Frame, Curve[][] Strokes, Relation Surface);

    public static Relation Generate(Relation packet, Relation prior)
    {
        if (!SequentialEvidence.TryRead(packet, out var message) || !NativePrior.TryRead(prior, out var model))
            throw new InvalidDataException("Reconstruction requires readable native sequential evidence and prior equations.");
        var strokes = new List<IElement>(); var witnesses = new List<IElement>();
        foreach (var stroke in message.Strokes)
        {
            var curves = new List<IElement>();
            foreach (var section in stroke)
            {
                var inference = NativePrior.Predict(model, section); var translationWitnesses = new List<IElement>();
                CarrierLocation Translate(long origin, CarrierLocation offset, Carrier axis)
                {
                    var operation = Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(offset.Carrier));
                    var input = new Relation(new CarrierLocation(axis, origin), offset);
                    if (!Equation.TryPredict(operation, input, axis, out var prediction) || !Equation.TryReadPrediction(prediction, out var value, out _))
                        throw new InvalidDataException("A predicted control offset failed its explicit coordinate calibration.");
                    translationWitnesses.Add(new Relation(new Relation(operation, input), prediction)); return value;
                }
                var c1 = new Relation(Translate(section.Start.X, inference.Outputs[0], message.Frame.X),
                    Translate(section.Start.Y, inference.Outputs[1], message.Frame.Y));
                var c2 = new Relation(Translate(section.Start.X, inference.Outputs[2], message.Frame.X),
                    Translate(section.Start.Y, inference.Outputs[3], message.Frame.Y));
                var curve = BuildCurveWords(message.Frame, section.From, section.To,
                    [Position(message.Frame.X, message.Frame.Y, section.Start), c1, c2, Position(message.Frame.X, message.Frame.Y, section.End)]);
                curves.Add(curve); witnesses.Add(new Relation(inference.Surface, new Relation(S(translationWitnesses.ToArray()), curve)));
            }
            strokes.Add(S(curves.ToArray()));
        }
        return new(new Relation(message.Frame.Surface, S(strokes.ToArray())), new Relation(packet, new Relation(prior, S(witnesses.ToArray()))));
    }

    public static Relation BuildCurve(Frame frame, Placeholder from, Placeholder to, IReadOnlyList<Point> controls)
    {
        if (controls.Count is < 2 or > 4) throw new ArgumentException("A finite curve requires two to four controls.");
        return BuildCurveWords(frame, from, to, controls.Select(point => (IElement)Position(frame.X, frame.Y, point)).ToArray());
    }

    private static Relation BuildCurveWords(Frame frame, Placeholder from, Placeholder to, IElement[] words)
    {
        var parameter = new Placeholder(); var operations = new List<IElement>(); var level = words;
        while (level.Length > 1)
        {
            var next = new IElement[level.Length - 1];
            for (var i = 0; i < next.Length; i++)
            {
                next[i] = new Placeholder(); operations.Add(TransformSurface.Interpolate(level[i], level[i + 1], parameter, next[i]));
            }
            level = next;
        }
        var before = new Placeholder(); var after = new Placeholder();
        var interval = new Relation(new Relation(before, after), new Relation(parameter, S(new Relation(before, parameter), new Relation(parameter, after))));
        var scheme = new Relation(new Relation(frame.Surface, interval), new Relation(S(operations.ToArray()), level[0]));
        return new(new Relation(from, to), new Relation(S(words), scheme));
    }

    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (surface is not { Left: Relation { Left: Relation basis, Right: Relation strokeScope },
            Right: Relation { Left: Relation packet, Right: Relation { Left: Relation prior, Right: Relation witnesses } } } ||
            !SequentialEvidence.TryRead(packet, out var message) || !ReferenceEquals(basis, message.Frame.Surface) ||
            !NativePrior.TryRead(prior, out _) || !Entries(strokeScope, out var strokeRows) || strokeRows.Length != message.Strokes.Count ||
            !Entries(witnesses, out var witnessRows) || witnessRows.Length != message.Sections.Count()) return false;
        var strokes = new List<Curve[]>(); var sequence = 0;
        for (var s = 0; s < strokeRows.Length; s++)
        {
            if (strokeRows[s] is not Relation scope || !Entries(scope, out var rows) || rows.Length != message.Strokes[s].Length) return false;
            var curves = new List<Curve>();
            for (var i = 0; i < rows.Length; i++)
            {
                var source = message.Strokes[s][i];
                if (rows[i] is not Relation curve || !TryReadCurve(message.Frame, curve, out var curveRead) ||
                    !ReferenceEquals(source.From, curveRead.From) || !ReferenceEquals(source.To, curveRead.To) ||
                    source.Start != curveRead.Controls[0] || source.End != curveRead.Controls[^1] ||
                    witnessRows[sequence++] is not Relation { Left: Relation { Left: Relation { Left: var citedSection, Right: var citedPrior } },
                        Right: Relation { Left: Relation translations, Right: var citedCurve } } || !ReferenceEquals(citedSection, source.Surface) ||
                    !ReferenceEquals(citedPrior, prior) || !ReferenceEquals(citedCurve, curve) || !ControlDependencies(curve, translations)) return false;
                curves.Add(curveRead);
            }
            strokes.Add(curves.ToArray());
        }
        read = new(message.Frame, strokes.ToArray(), surface); return true;
    }

    // The translated native outputs are the actual downstream coordinate
    // locations. Equal ticks or a side-by-side provenance note are insufficient.
    private static bool ControlDependencies(Relation curve, Relation translations)
    {
        if (curve.Right is not Relation { Left: Relation scope } || !Entries(scope, out var controls) ||
            controls is not [_, Relation c1, Relation c2, _] || !Entries(translations, out var rows) || rows.Length != 4) return false;
        var coordinates = new[] { c1.Left, c1.Right, c2.Left, c2.Right };
        for (var i = 0; i < rows.Length; i++)
            if (rows[i] is not Relation { Right: Relation { Left: CarrierLocation output } } || coordinates[i] is not CarrierLocation coordinate ||
                !ReferenceEquals(output.Carrier, coordinate.Carrier) || !output.SameTickRefAs(coordinate)) return false;
        return true;
    }

    public static bool TryReadCurve(Frame frame, Relation section, out Curve read)
    {
        read = null!;
        if (section is not { Left: Relation { Left: Placeholder { IsInverse: false } from, Right: Placeholder { IsInverse: false } to },
            Right: Relation { Left: Relation controls, Right: Relation { Left: Relation { Left: var declaredFrame, Right: Relation interval },
                Right: Relation { Left: Relation operationScope, Right: var final } } } } || !ReferenceEquals(declaredFrame, frame.Surface) ||
            !Entries(controls, out var words) || words.Length is < 2 or > 4 || !Entries(operationScope, out var operations) ||
            interval is not { Left: Relation { Left: Placeholder { IsInverse: false } before, Right: Placeholder { IsInverse: false } after },
                Right: Relation { Left: Placeholder { IsInverse: false } parameter, Right: Relation order } } ||
            ReferenceEquals(before, after) || ReferenceEquals(before, parameter) || ReferenceEquals(after, parameter) ||
            !Entries(order, out var orders) || orders is not [Relation first, Relation second] ||
            !ReferenceEquals(first.Left, before) || !ReferenceEquals(first.Right, parameter) ||
            !ReferenceEquals(second.Left, parameter) || !ReferenceEquals(second.Right, after)) return false;
        var points = new Point[words.Length];
        for (var i = 0; i < words.Length; i++) if (!TryPosition(words[i], frame, out points[i])) return false;
        var level = words; var cursor = 0; var introduced = new HashSet<Placeholder> { parameter, before, after, from, to };
        while (level.Length > 1)
        {
            var next = new IElement[level.Length - 1];
            for (var i = 0; i < next.Length; i++)
            {
                if (cursor >= operations.Length || operations[cursor++] is not Relation operation ||
                    !TransformSurface.TryReadInterpolate(operation, out var a, out var b, out var t, out var output) ||
                    !ReferenceEquals(a, level[i]) || !ReferenceEquals(b, level[i + 1]) || !ReferenceEquals(t, parameter) ||
                    output is not Placeholder { IsInverse: false } result || !introduced.Add(result)) return false;
                next[i] = result;
            }
            level = next;
        }
        if (cursor != operations.Length || !ReferenceEquals(final, level[0])) return false;
        read = new(from, to, points, section); return true;
    }
}
