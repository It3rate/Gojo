using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// The website is an illustration, not a second execution engine. Check its
/// published numeric data against selected reads of the actual source fixtures.
/// Existing fixture tests retain the deeper identity, replay and non-read contracts.
/// </summary>
public sealed class GojoSyntaxGalleryTests
{
    private static JsonElement Demo(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery/catalog.json")));
        return document.RootElement.GetProperty("demos").EnumerateArray().Single(d => d.GetProperty("id").GetString() == id).Clone();
    }
    private static JsonElement Facts(string id) => Demo(id).GetProperty("facts");
    private static IElement[] Read(string id, int restores = 0)
    {
        var fixture = Demo(id).GetProperty("fixture").GetString()!;
        var canonical = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures/Syntax", fixture)).Replace("\r\n", "\n");
        var published = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery/fixtures", fixture)).Replace("\r\n", "\n");
        Assert.Equal(canonical, published);
        var roots = GojoSyntax.Parse(published).Roots.ToArray();
        for (var i = 0; i < restores; i++) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        return roots;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void SharedSiteAndParameterPicturesFollowTheActualReferencesAndFrames(int restores)
    {
        var roots = Read("shared-sites", restores); var f = F(roots[0]); var data = Facts("shared-sites");
        var joint = P(R(f[2]).Right); var next = P(R(f[5]).Left); var lookalike = P(R(f[6]).Left);
        Assert.True(joint.SameTickRefAs(next)); Assert.False(joint.SameTickRefAs(lookalike));
        Scalar(P(R(f[1]).Left), data.GetProperty("start")); Scalar(joint, data.GetProperty("joint"));
        Scalar(P(R(f[5]).Right), data.GetProperty("end"));
        joint.Tick!.Set(data.GetProperty("moved").GetInt64());
        Scalar(next, data.GetProperty("moved")); Scalar(lookalike, data.GetProperty("lookalike"));

        var normalized = F(F(Read("parameter-frames", restores)[1])[0]);
        data = Facts("parameter-frames");
        var target = P(normalized[4]).Carrier;
        for (var i = 0; i < 2; i++)
        {
            var sample = P(normalized[2 + i]); Scalar(sample, data.GetProperty("samples")[i]);
            var projection = R(normalized[7]).Project(sample, target);
            Assert.True(projection.TryReadProjection(out var result, out _));
            Scalar(result, data.GetProperty("positions")[i]);
        }
        var witness = F(normalized[6]);
        Scalar(witness[2], data.GetProperty("coarse")); Scalar(witness[3], data.GetProperty("residue"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void PartialAndExactSamplesKeepTheirDeclaredDomain(int restores)
    {
        var roots = Read("partial-construction", restores); var data = Facts("partial-construction");
        var inputs = F(R(roots[0]).Left);
        Vector(inputs[0], data.GetProperty("a")); Vector(inputs[1], data.GetProperty("b"));
        Assert.False(AffineFrame.TryInterpolate(R(roots[1]), out var partial));
        Assert.IsType<Placeholder>(F(Result(partial))[1]);
        Scalar(F(Result(partial))[0], data.GetProperty("exact")[0]);
        Assert.True(AffineFrame.TryInterpolate(R(roots[0]), out var exact));
        Vector(Result(exact), data.GetProperty("exact"));
        Assert.False(AffineFrame.TryInterpolate(R(roots[2]), out _));
        Assert.True(AffineFrame.TryInterpolate(R(roots[3]), out var extended));
        Vector(Result(extended), data.GetProperty("extended"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void EveryDisplayedBezierLayerComesFromTheSixExactReads(int restores)
    {
        var roots = Read("bezier-construction", restores); var data = Facts("bezier-construction");
        var controls = F(roots[0]).Skip(2).ToArray();
        for (var i = 0; i < 4; i++) Vector(controls[i], data.GetProperty("controls")[i]);
        var f = F(roots[1]); var bindings = F(f[2]).Select(R).ToList();
        var expected = data.GetProperty("levels").EnumerateArray().SelectMany(v => v.EnumerateArray()).ToArray();
        var steps = F(F(f[0])[1]);
        for (var i = 0; i < steps.Length; i++)
        {
            Assert.True(AffineFrame.TryInterpolate(R(StructuralPattern.Apply(steps[i], bindings)), out var read));
            Vector(Result(read), expected[i]);
            bindings.Add(new Relation(R(steps[i]).Right, Result(read)));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ReplayPictureHasTheCheckedOldNewAndUnaffectedParticipants(int restores)
    {
        var roots = Read("curve-replay", restores); var world = F(roots[0]); var data = Facts("curve-replay");
        var transforms = F(world[0]).Select(R).ToArray();
        var oldBindings = F(world[1]).Select(R).ToList();
        var edit = R(F(world[2])[0]);
        var newBindings = oldBindings.Where(b => !ReferenceEquals(b.Left, edit.Left)).Append(edit).ToList();
        Relation[] Observe(List<Relation> bindings)
        {
            var apps = new List<Relation>();
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i]; var fields = F(transform);
                var operation = R(StructuralPattern.Apply(fields[1], bindings));
                Relation witness;
                if (i == 0) witness = ReadWitness.Create(new Carrier(), operation.Left, transform, operation.Left, G(), operation);
                else Assert.True(AffineFrame.TryInterpolate(operation, out witness));
                var result = Result(witness); var source = StructuralPattern.Apply(fields[0], bindings);
                var focus = FocusSurface.Create(new Carrier(), source, fields[0], source, G(), result, G(), G(), witness);
                var output = F(fields[2])[0];
                var local = F(fields[0]).OfType<Placeholder>().Select(p => new Relation(p, StructuralPattern.Apply(p, bindings)))
                    .Append(new Relation(output, result)).Cast<IElement>().ToArray();
                Assert.True(TransformSurface.TryCreateApplication(new Carrier(), transform, focus, G(local), result, G(), witness, out var app));
                apps.Add(app); bindings.Add(new Relation(output, result));
            }
            return [.. apps];
        }
        var old = Observe(oldBindings); var fresh = Observe(newBindings);
        var selected = new[] { 1, 2, 4, 5, 6, 7 };
        for (var i = 0; i < selected.Length; i++)
        {
            Vector(F(old[selected[i]])[3], data.GetProperty("old")[i]);
            Vector(F(fresh[selected[i]])[3], data.GetProperty("fresh")[i]);
        }
        var controls = F(world[6]);
        for (var i = 0; i < controls.Length; i++)
        {
            Vector(controls[i], data.GetProperty("controls")[i]);
            Vector(i == 1 ? edit.Right : controls[i], data.GetProperty("edited")[i]);
        }
        var dependencies = new List<IElement>();
        foreach (var producer in old)
            foreach (var consumer in old)
                if (!ReferenceEquals(producer, consumer) && TransformSurface.TryCreateApplicationDependency(
                    new Carrier(), producer, consumer, G(), G(), out var edge)) dependencies.Add(edge);
        Assert.True(TransformSurface.TryCreateFocusChange(new Carrier(), R(F(old[0])[1]), R(F(fresh[0])[1]),
            world[3], G(), G(), out var change));
        Assert.True(TransformSurface.TryCreateDependencyAffectedSlice(new Carrier(), G(old), G([.. dependencies]),
            change, G(), G(), out var slice));
        Assert.Equal(data.GetProperty("affected").GetInt32(), selected.Count(i => F(F(slice)[4]).Contains(old[i])));
        Assert.Equal(data.GetProperty("carried").GetInt32(), selected.Count(i => F(F(slice)[3]).Contains(old[i])));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void JoinClaimsAndFriezeCoordinatesAreActualSelectedReads(int restores)
    {
        var roots = Read("curve-joins", restores); var f = F(roots[0]); var data = Facts("curve-joins");
        for (var i = 0; i < 4; i++)
        { Vector(F(f[0])[i], data.GetProperty("left")[i]); Vector(F(f[1])[i], data.GetProperty("right")[i]); }
        var bindings = new List<Relation>();
        IElement Step(IElement step, bool difference)
        {
            var request = R(StructuralPattern.Apply(R(step).Left, bindings));
            Assert.True(difference ? AffineFrame.TryDifference(request, out var read) : AffineFrame.TryProject(request, out read));
            var result = Result(read); bindings.Add(new Relation(R(step).Right, result)); return result;
        }
        var first = F(f[6]);
        for (var i = 0; i < first.Length; i++)
        { var result = Step(first[i], i < 2); if (i >= 2) Vector(result, data.GetProperty("tangents")[i - 2]); }
        var second = F(roots[3]);
        for (var i = 0; i < F(second[0]).Length; i++)
        { var result = Step(F(second[0])[i], i < 4); if (i >= 4) Vector(result, data.GetProperty("seconds")[i - 4]); }
        Assert.True(AffineFrame.TryDifference(R(StructuralPattern.Apply(R(second[1]).Left, bindings)), out var mismatch));
        Vector(Result(mismatch), data.GetProperty("mismatch"));

        f = F(Read("frieze-patterns", restores)[2]); var requestFields = F(f[0]); data = Facts("frieze-patterns");
        for (var i = 0; i < 3; i++) Vector(F(requestFields[1])[i], data.GetProperty("original")[i]);
        Assert.True(AffineFrame.TryMapExposed(R(f[0]), out var glide));
        var exposed = F(F(F(glide)[4])[0]).Select(c => R(c).Right).ToArray();
        for (var i = 0; i < 3; i++) Vector(exposed[i], data.GetProperty("glide")[i]);
        Assert.True(AffineFrame.TryMapExposed(G(Result(glide), G(exposed), requestFields[2], requestFields[3]), out var twice));
        var twicePoints = F(F(F(twice)[4])[0]).Select(c => R(c).Right).ToArray();
        Assert.True(AffineFrame.TryMapExposed(R(f[1]), out var translated));
        var translatedPoints = F(F(F(translated)[4])[0]).Select(c => R(c).Right).ToArray();
        for (var i = 0; i < 3; i++)
        { Vector(twicePoints[i], data.GetProperty("twice")[i]); Vector(translatedPoints[i], data.GetProperty("twice")[i]); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void UncertaintyPicturesUseConditionalBoundsAndDistanceToSpecification(int restores)
    {
        var roots = Read("uncertain-cups", restores); var data = Facts("uncertain-cups"); var f = F(roots[0]);
        var names = new[] { "bounds", "same", "different" };
        for (var i = 0; i < 3; i++)
        { Assert.True(BoundedQuantity.TryObserve(R(f[i]), out var read)); Bounds(read, data.GetProperty(names[i])); }
        Assert.True(BoundedQuantity.TryCombine(R(F(roots[1])[2]), out var events));
        Bounds(events, data.GetProperty("events"));
        f = F(Read("clearance", restores)[3]); data = Facts("clearance");
        Assert.True(BoundedQuantity.TryObserve(R(f[0]), out var clearance)); Bounds(clearance, data.GetProperty("clearance"));
        var terms = F(F(f[0])[1]);
        var hole = R(F(R(terms[0]).Left)[1]); var bolt = R(F(R(terms[1]).Left)[1]);
        Scalar(P(bolt.Left), data.GetProperty("bolt")[0]); Scalar(P(bolt.Right), data.GetProperty("bolt")[1]);
        Scalar(P(hole.Left), data.GetProperty("hole")[0]); Scalar(P(hole.Right), data.GetProperty("hole")[1]);
        Assert.True(BoundedQuantity.TryObserveConformance(G(clearance, f[1], new Placeholder()), out var broad));
        Assert.True(BoundedQuantity.TryReadBounds(broad, out _, out var lo, out var hi, out _));
        Assert.Equal("0", Fraction(lo)); Assert.Equal("0", Fraction(hi));
        Assert.True(BoundedQuantity.TryObserveConformance(G(clearance, f[2], new Placeholder()), out var strict));
        Bounds(strict, data.GetProperty("violation"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void DisplayedMaterialStatesAccountForStockWithdrawalsAndUnpaidDraw(int restores)
    {
        var roots = Read("material-refinement", restores); var states = Facts("material-refinement").GetProperty("states");
        var f = F(roots[0]); var coarse = (Carrier)f[0]; var fine = (Carrier)f[1];
        void State(int i)
        { Assert.Equal(states[i][0].GetUInt64(), coarse.Value); Assert.Equal(states[i][1].GetUInt64(), fine.Value); Assert.Equal(states[i][2].GetInt64(), fine.Tension); }
        coarse.Add(3); State(0);
        Assert.True(RefinementTransfer.TryApply(R(roots[1]), out _)); State(1);
        fine.Remove(3); State(2);
        Assert.True(RefinementTransfer.TryApply(R(roots[2]), out _)); State(3);
        fine.Remove(10); State(4);
        Assert.True(RefinementTransfer.TryBorrow(R(roots[3]), out _)); State(5);
        Assert.Equal(30UL - 3 - 10, coarse.Value * 10 + fine.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void BlendPredictionsAndResponseStallMatchThePublishedTrace(int restores)
    {
        var roots = Read("local-blend", restores); var data = Facts("local-blend");
        var components = F(roots[1]); var models = F(roots[2]); var control = F(roots[3]);
        var input = StructuralPattern.Apply(components[0], F(components[2]).Select(R).ToArray());
        var output = P(control[1]).Carrier; var gain = (Carrier)control[3]; var error = (Carrier)control[2];
        gain.Add((ulong)P(control[4]).Ticks);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(data.GetProperty("gains")[i].GetUInt64(), gain.Value);
            var model = R(StructuralPattern.Apply(models[0], new Relation(models[1], Relation.Point(output, checked((long)gain.Value)))));
            Assert.True(model.TryReadOperationFrom(input, output, out var predictionRead));
            Assert.True(Equation.TryReadPrediction(predictionRead, out var prediction, out _));
            Scalar(prediction, data.GetProperty("outputs")[i]);
            if (i < 3)
            {
                var mismatch = new CarrierLocation(error, P(control[1]).Ticks - prediction.Ticks);
                var response = R(control[i == 2 ? 7 : 6]).Project(mismatch, gain);
                Assert.True(response.TryReadProjection(out var delta, out var residue));
                if (i == 1) Scalar(residue, data.GetProperty("residue")[2]);
                gain.Add(checked((ulong)delta.Ticks));
            }
        }
        Assert.True(R(models[3]).TryReadOperationFrom(input, output, out var alternativeRead));
        Assert.True(Equation.TryReadPrediction(alternativeRead, out var alternativeResult, out _));
        Scalar(alternativeResult, data.GetProperty("alternative"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void UnitPicturesRetainAuthoredOrderAndTheHundredKilometreStandard(int restores)
    {
        var roots = Read("unit-standards", restores); var data = Facts("unit-standards");
        for (var i = 0; i < 4; i++)
        {
            Assert.True(UnitQuotient.TryCancel(R(roots[i + 1]), out var witness));
            Assert.Equal(data.GetProperty("pairs")[i].GetInt32(), F(F(witness)[1]).Length);
            Assert.True(UnitQuotient.TryRead(R(Result(witness)), out _, out _, out var standard));
            if (i >= 2) Scalar(P(R(standard).Right), data.GetProperty("standard"));
        }
        Assert.NotEqual(roots[1], roots[2]);
    }

    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static CarrierLocation P(IElement e) => Assert.IsType<CarrierLocation>(R(e).Left);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var f)); return f; }
    private static IElement Result(IElement witness) => F(witness)[2];
    private static string Fraction(IElement value)
    {
        Assert.True(ProjectionResidue.TryRead(value, out _, out var n, out var d));
        return d == 1 ? n.ToString(CultureInfo.InvariantCulture) : n + "/" + d;
    }
    private static void Scalar(IElement value, JsonElement expected)
    {
        if (expected.ValueKind == JsonValueKind.String) { Assert.Equal(expected.GetString(), Fraction(value)); return; }
        Assert.True(ProjectionResidue.TryRead(value, out _, out var n, out var d));
        // Website decimals are display data. Compare their finite exact value here.
        var parts = expected.GetRawText().Split('.');
        var scale = BigInteger.Pow(10, parts.Length == 2 ? parts[1].Length : 0);
        var numerator = BigInteger.Parse(string.Concat(parts), CultureInfo.InvariantCulture);
        Assert.Equal(numerator * d, (BigInteger)n * scale);
    }
    private static void Vector(IElement value, JsonElement expected)
    { var fields = F(value); Assert.Equal(expected.GetArrayLength(), fields.Length); for (var i = 0; i < fields.Length; i++) Scalar(fields[i], expected[i]); }
    private static void Bounds(Relation witness, JsonElement expected)
    { Assert.True(BoundedQuantity.TryReadBounds(witness, out _, out var lo, out var hi, out _)); Scalar(lo, expected[0]); Scalar(hi, expected[1]); }
}
