using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class ConstructionFixtureTests
{
    [Fact]
    public void ThreeRetainedRecipesExposeEquivalentGeometryThroughOneInterface()
    {
        var c = new ConstructionRecipes();
        var width = Value(8); var height = Value(4);
        var recipes = new[]
        {
            c.Path(c.Point(0, 0), c.Point(1, 0), [width, height], 2),
            c.Offsets(c.Point(0, 0), width, height),
            c.OppositePairs(c.Point(4, 2), Value(4), Value(2))
        };
        var boundary = new ShapeInterface();
        Relation[]? stableBindings = null;
        foreach (var recipe in recipes)
        {
            var application = MachineRunner.Run(recipe);
            Assert.Empty(F(F(application)[4]));
            Assert.True(boundary.Publish(application));
            Assert.Same(recipe, boundary.Selected);
            if (stableBindings is not null)
                foreach (var pair in stableBindings.Zip(boundary.CurrentBindings))
                {
                    Assert.Same(pair.First.Left, pair.Second.Left);
                    Assert.Same(pair.First.Right, pair.Second.Right);
                }
            stableBindings = boundary.CurrentBindings;
            RectangleCoordinates(boundary, (0, 0), (8, 0), (8, 4), (0, 4));
        }
        Assert.Same(recipes[2], F(boundary.Surface)[3]);
        Assert.NotSame(recipes[0], recipes[1]);
        Assert.Equal(5, F(boundary.Timeline).Length); // origin, initial, three publications
    }

    [Fact]
    public void SharedBeatResizeKeepsClosureWhileOneFaultyOccurrenceLeavesItOpen()
    {
        var c = new ConstructionRecipes();
        var width = Value(8); var height = Value(4);
        var repeated = c.Path(c.Zero, c.Point(1, 0), [width, height], 2);
        width.Add(4);
        var good = MachineRunner.Run(repeated);
        Assert.Empty(F(F(good)[4]));
        var points = F(F(good)[2]);
        Point(points[1], 12, 0);
        Point(points[3], 0, 4);
        Point(points[4], 0, 0);

        var faulty = c.Path(c.Zero, c.Point(1, 0), [width, height, Value(13), height], 1);
        var bad = MachineRunner.Run(faulty);
        Assert.NotEmpty(F(F(bad)[4]));
        Point(F(F(bad)[2])[4], -1, 0);
        var boundary = new ShapeInterface();
        Assert.True(boundary.Publish(good));
        var oldSelected = boundary.Selected;
        var oldBindings = boundary.CurrentBindings;
        Assert.False(boundary.Publish(bad));
        Assert.Same(oldSelected, boundary.Selected);
        Assert.Equal(oldBindings, boundary.CurrentBindings);
        RectangleCoordinates(boundary, (0, 0), (12, 0), (12, 4), (0, 4));
    }

    [Fact]
    public void ResizeMovesStableTickReferencesAndKeepsFrozenPublicationCoordinates()
    {
        var c = new ConstructionRecipes();
        var width = Value(8); var height = Value(4);
        var recipe = c.Offsets(c.Zero, width, height);
        var boundary = new ShapeInterface();
        Assert.True(boundary.Publish(MachineRunner.Run(recipe)));
        var publicPoints = boundary.CurrentBindings.Select(b => b.Right).ToArray();
        var beforeTicks = publicPoints.SelectMany(p => F(p).Cast<CarrierLocation>()).ToArray();
        var firstStep = F(boundary.Timeline)[2];
        var frozenAfter = F(firstStep)[3];

        width.Add(8);
        Assert.True(boundary.Publish(MachineRunner.Run(recipe)));

        var afterPoints = boundary.CurrentBindings.Select(b => b.Right).ToArray();
        var afterTicks = afterPoints.SelectMany(p => F(p).Cast<CarrierLocation>()).ToArray();
        for (var i = 0; i < beforeTicks.Length; i++) Assert.True(beforeTicks[i].SameTickRefAs(afterTicks[i]));
        for (var i = 0; i < publicPoints.Length; i++) Assert.Same(publicPoints[i], afterPoints[i]);
        Point(Assert.IsType<Relation>(F(frozenAfter)[1]).Right, 8, 0);
        Point(afterPoints[1], 16, 0);
    }

    [Fact]
    public void RoofAndDoorConsumeUnchangedPublicSidesAcrossMakerReplacementAndResize()
    {
        var c = new ConstructionRecipes();
        var boundary = new ShapeInterface();
        var top = boundary.Side(2); var floor = boundary.Side(0);
        var roof = Attachments.Roof(c, top, Value(3));
        var door = Attachments.Door(c, floor, Value(2));
        var w = Value(8); var h = Value(4);
        Assert.True(boundary.Publish(MachineRunner.Run(c.Path(c.Zero, c.Point(1, 0), [w, h], 2))));
        var firstRoof = F(Attachments.Read(roof, boundary.CurrentBindings))[2];
        Point(Assert.IsType<Relation>(F(firstRoof)[0]).Right, 4, 7);

        w.Add(8);
        Assert.True(boundary.Publish(MachineRunner.Run(c.Offsets(c.Zero, w, h))));
        var finalRoof = F(Attachments.Read(roof, boundary.CurrentBindings))[2];
        var finalDoor = F(Attachments.Read(door, boundary.CurrentBindings))[2];

        Assert.Same(top, boundary.Side(2));
        Assert.Same(floor, boundary.Side(0));
        Assert.Same(top, F(roof)[2]);
        Assert.Same(floor, F(door)[0]);
        Point(Assert.IsType<Relation>(F(finalRoof)[0]).Right, 8, 7);
        var opening = Assert.IsType<Relation>(F(finalDoor)[1]);
        Point(opening.Left, 6, 0);
        Point(opening.Right, 10, 0);
    }

    [Fact]
    public void RoofReadRetainsItsSingleRidgeSiteAcrossBothIncidentEdges()
    {
        var c = new ConstructionRecipes();
        var boundary = new ShapeInterface();
        Assert.True(boundary.Publish(MachineRunner.Run(c.Offsets(c.Zero, Value(8), Value(4)))));
        var roof = Attachments.Roof(c, boundary.Side(2), Value(3));
        var edges = F(F(Attachments.Read(roof, boundary.CurrentBindings))[2]);

        Assert.Same(Assert.IsType<Relation>(edges[0]).Right, Assert.IsType<Relation>(edges[1]).Left);
    }

    [Fact]
    public void DoorCutRetainsACompleteBoundaryAndCanMoveWithoutReplacingIt()
    {
        var c = new ConstructionRecipes();
        var boundary = new ShapeInterface();
        Assert.True(boundary.Publish(MachineRunner.Run(c.Offsets(c.Zero, Value(16), Value(8)))));
        var floor = boundary.Side(0);
        var originalBindings = boundary.CurrentBindings;
        var middle = Attachments.Door(c, floor, Value(3), 3, 5, 8);
        var moved = Attachments.Door(c, floor, Value(3), 1, 3, 8);
        var firstRead = F(F(Attachments.Read(middle, originalBindings))[2]);
        var secondRead = F(F(Attachments.Read(moved, originalBindings))[2]);

        Assert.Same(floor, F(middle)[0]);
        Assert.Same(floor, F(moved)[0]);
        Assert.Equal(originalBindings, boundary.CurrentBindings);
        Assert.Equal(4, F(boundary.Boundary).Length);
        Point(Assert.IsType<Relation>(firstRead[1]).Left, 6, 0);
        Point(Assert.IsType<Relation>(secondRead[1]).Left, 2, 0);
        Point(Assert.IsType<Relation>(secondRead[0]).Left, 0, 0);
        Point(Assert.IsType<Relation>(secondRead[0]).Right, 16, 0);
        Assert.Equal(2, F(secondRead[2]).Length);
    }

    [Fact]
    public void AnOpenOriginKeepsTheFourRoleTopologyAndRefusesPrematurePublication()
    {
        var c = new ConstructionRecipes();
        var open = new Placeholder();
        var recipe = c.Offsets(open, Value(8), Value(4));
        var boundary = new ShapeInterface();

        Assert.False(boundary.Publish(MachineRunner.Run(recipe)));

        Assert.Empty(boundary.CurrentBindings);
        Assert.IsType<Placeholder>(boundary.Selected);
        var roles = F(boundary.Roles);
        var edges = F(boundary.Boundary).Cast<Relation>().ToArray();
        for (var i = 0; i < 4; i++)
        {
            Assert.Same(roles[i], edges[i].Left);
            Assert.Same(roles[(i + 1) % 4], edges[i].Right);
        }
        Assert.NotEmpty(F(F(boundary.LastAttempt!)[3]));
    }

    [Fact]
    public void FractionalReplacementIsRejectedBeforeAnyPublicTickMoves()
    {
        var c = new ConstructionRecipes();
        var boundary = new ShapeInterface();
        Assert.True(boundary.Publish(MachineRunner.Run(c.Offsets(c.Zero, Value(8), Value(4)))));
        var selected = boundary.Selected;
        var fractionalOrigin = c.Times(c.Point(1, 0), 1, 2);
        var attempt = MachineRunner.Run(c.Offsets(fractionalOrigin, Value(8), Value(4)));

        Assert.False(boundary.Publish(attempt));

        Assert.Same(selected, boundary.Selected);
        RectangleCoordinates(boundary, (0, 0), (8, 0), (8, 4), (0, 4));
        Assert.NotEmpty(F(F(boundary.LastAttempt!)[3]));
    }

    [Fact]
    public void FirstPublicationRejectsOneCornerWithLookalikeCoordinatesOnForeignAxes()
    {
        var c = new ConstructionRecipes();
        var machine = c.Offsets(c.Zero, Value(8), Value(4));
        var application = MachineRunner.Run(machine);
        var fields = F(application); var points = F(fields[2]);
        var foreign = new ConstructionRecipes();
        points[2] = foreign.Point(8, 4);
        fields[2] = G(points);
        var malformed = G(fields);
        var boundary = new ShapeInterface();

        Assert.False(boundary.Publish(malformed));
        Assert.Empty(boundary.CurrentBindings);
        Assert.NotEmpty(F(F(boundary.LastAttempt!)[3]));
    }

    [Fact]
    public void ARecipeRestoresWithoutItsBuilderAndUsesItsOwnRetainedParameters()
    {
        var c = new ConstructionRecipes();
        var width = Value(8); var height = Value(4);
        var recipe = c.Path(c.Zero, c.Point(1, 0), [width, height], 2);
        var restored = F(GojoNumericFormat.Decode(GojoNumericFormat.Encode(G(recipe, width, height)))[0]);
        var loadedMachine = Assert.IsType<Relation>(restored[0]);
        var loadedWidth = Assert.IsType<Carrier>(restored[1]);
        loadedWidth.Add(4);

        var application = MachineRunner.Run(loadedMachine);

        Assert.Empty(F(F(application)[4]));
        var points = F(F(application)[2]);
        Point(points[1], 12, 0);
        Point(points[2], 12, 4);
        Point(points[4], 0, 0);
        Assert.Equal(8UL, width.Value);
        Assert.Equal(12UL, loadedWidth.Value);
        var boundary = new ShapeInterface();
        Assert.True(boundary.Publish(application));
    }

    [Fact]
    public void OffsetAndCentreMachinesHaveDifferentResponsesToTheSameWidthChange()
    {
        var c = new ConstructionRecipes();
        var width = Value(8); var halfWidth = Value(4);
        var offset = c.Offsets(c.Zero, width, Value(4));
        var centred = c.OppositePairs(c.Point(4, 2), halfWidth, Value(2));
        width.Add(4);
        halfWidth.Add(2);

        var anchored = F(F(MachineRunner.Run(offset))[2]);
        var balanced = F(F(MachineRunner.Run(centred))[2]);

        Point(anchored[0], 0, 0);
        Point(anchored[1], 12, 0);
        Point(balanced[0], -2, 0);
        Point(balanced[1], 10, 0);
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(1, 2, 2, 2)]
    [InlineData(1, 1, 4, 0)]
    public void NativeQuadraticMachineRestoresAndCalculatesItsCurve(long n, long d, long x, long y)
    {
        var c = new ConstructionRecipes();
        var cap = Attachments.QuadraticCap(c, c.Point(0, 0), c.Point(2, 4), c.Point(4, 0));
        var loaded = Assert.IsType<Relation>(GojoNumericFormat.Decode(GojoNumericFormat.Encode(cap))[0]);
        var fields = F(loaded);
        var parameter = F(fields[0])[0];
        var context = F(fields[4]);
        var grade = Assert.IsType<Carrier>(context[4]);
        var endpoints = Assert.IsType<Relation>(context[2]);
        var controls = F(context[3]);
        Assert.Same(controls[0], endpoints.Left);
        Assert.Same(controls[2], endpoints.Right);

        var read = MachineRunner.Run(loaded, new Relation(parameter, ProjectionResidue.Create(grade, n, d)));

        Point(F(F(read)[2])[0], x, y);
        Assert.Empty(F(F(read)[4]));
        Assert.Equal(3, F(F(read)[3]).Length);
    }

    [Fact]
    public void CurveWithMissingOrOutOfSupportSampleStaysOpen()
    {
        var c = new ConstructionRecipes();
        var cap = Attachments.QuadraticCap(c, c.Point(0, 0), c.Point(2, 4), c.Point(4, 0));
        var fields = F(cap);
        var missing = MachineRunner.Run(cap);
        var outside = MachineRunner.Run(cap, new Relation(F(fields[0])[0],
            ProjectionResidue.Create((Carrier)F(fields[4])[4], 3, 2)));

        foreach (var read in new[] { missing, outside })
        {
            Assert.IsType<Placeholder>(F(F(read)[2])[0]);
            Assert.NotEmpty(F(F(read)[4]));
        }
    }

    [Fact]
    public void ReadingAnArchedDoorBindsPublicCornersButRetainsTheCurveSampleAsAnOpenInput()
    {
        var c = new ConstructionRecipes();
        var boundary = new ShapeInterface();
        Assert.True(boundary.Publish(MachineRunner.Run(c.Offsets(c.Zero, Value(16), Value(8)))));
        var door = Attachments.Door(c, boundary.Side(0), Value(4), arch: true);
        var observedDoor = F(F(Attachments.Read(door, boundary.CurrentBindings))[2]);
        var cap = Assert.IsType<Relation>(F(observedDoor[3])[1]);
        var fields = F(cap);
        var sample = Assert.IsType<Placeholder>(F(fields[0])[0]);
        var grade = Assert.IsType<Carrier>(F(fields[4])[4]);

        Assert.NotEmpty(F(F(MachineRunner.Run(cap))[4]));
        var read = MachineRunner.Run(cap, new Relation(sample, ProjectionResidue.Create(grade, 1, 2)));

        Point(F(F(read)[2])[0], 8, 5);
        Assert.Empty(F(F(read)[4]));
    }

    [Fact]
    public void OpenTurningLawCanBeBoundLaterWithoutReplacingTheSquareMachine()
    {
        var c = new ConstructionRecipes();
        var turn = new Placeholder();
        var recipe = c.Path(c.Zero, c.Point(1, 0), [Value(8)], 4, turn);
        var unresolved = MachineRunner.Run(recipe);
        Assert.NotEmpty(F(F(unresolved)[4]));

        var completed = MachineRunner.Run(recipe, new Relation(turn, c.QuarterTurn));

        Assert.Same(recipe, F(completed)[0]);
        Assert.Empty(F(F(completed)[4]));
        var points = F(F(completed)[2]);
        Point(points[1], 8, 0);
        Point(points[2], 8, 8);
        Point(points[3], 0, 8);
        Point(points[4], 0, 0);
        Assert.True(new ShapeInterface().Publish(completed));
    }

    [Fact]
    public void AFiveBeatPathWithBoundQuarterTurnsCalculatesButDoesNotPretendToClose()
    {
        var c = new ConstructionRecipes();
        var turn = new Placeholder();
        var recipe = c.Path(c.Zero, c.Point(1, 0), [Value(4), Value(8), Value(4), Value(4), Value(8)], 1, turn);
        var completed = MachineRunner.Run(recipe, new Relation(turn, c.QuarterTurn));

        var points = F(F(completed)[2]);
        Assert.Equal(6, points.Length);
        Assert.All(points, point => Assert.True(MachineRunner.TryNumbers(point, out _)));
        Point(points[^1], 8, 4);
        Assert.NotEmpty(F(F(completed)[4]));
        Assert.False(new ShapeInterface().Publish(completed));
    }

    [Fact]
    public void NativeRoundtripRetainsAnOpenTurningLawAndItsLaterBindingSurface()
    {
        var c = new ConstructionRecipes();
        var turn = new Placeholder();
        var recipe = c.Path(c.Zero, c.Point(1, 0), [Value(6)], 4, turn);
        var loaded = F(GojoNumericFormat.Decode(GojoNumericFormat.Encode(G(recipe, turn, c.QuarterTurn)))[0]);
        var loadedMachine = Assert.IsType<Relation>(loaded[0]);
        var loadedLaw = Assert.IsType<Placeholder>(loaded[1]);

        Assert.NotEmpty(F(F(MachineRunner.Run(loadedMachine))[4]));
        var completed = MachineRunner.Run(loadedMachine, new Relation(loadedLaw, loaded[2]));

        Assert.Empty(F(F(completed)[4]));
        Point(F(F(completed)[2])[2], 6, 6);
        Point(F(F(completed)[2])[4], 0, 0);
        Assert.True(new ShapeInterface().Publish(completed));
        Assert.NotEmpty(F(F(MachineRunner.Run(recipe))[4]));
    }

    private static Carrier Value(ulong value)
    {
        var carrier = new Carrier();
        carrier.Add(value);
        return carrier;
    }

    private static void RectangleCoordinates(ShapeInterface boundary, params (long X, long Y)[] points)
    {
        var bindings = boundary.CurrentBindings;
        for (var i = 0; i < points.Length; i++) Point(bindings[i].Right, points[i].X, points[i].Y);
    }

    private static void Point(IElement point, long x, long y)
    {
        Assert.True(MachineRunner.TryNumbers(point, out var values));
        Assert.Equal(x * values[0].D, values[0].N);
        Assert.Equal(y * values[1].D, values[1].N);
    }
}
