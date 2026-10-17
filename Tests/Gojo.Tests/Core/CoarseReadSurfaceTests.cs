using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class CoarseReadSurfaceTests
{
    public static IEnumerable<object[]> AgencyCases()
    {
        foreach (var partition in new[] { "baseline", "uniform", "dim_focused", "bright_focused" })
        foreach (var deadband in new[] { 4, 10 })
        foreach (var orientation in new[] { 1, -1 })
            yield return [partition, deadband, orientation];
    }

    [Theory]
    [MemberData(nameof(AgencyCases))]
    public void AgencyFiveRegionDifferencesMatchEnumeratedValuesForAllMasks(
        string partitionName, int deadband, int orientation)
    {
        var axis = new Carrier();
        var ranges = AgencyRanges(partitionName);
        var input = CoarseReadSurface.Partition(new Carrier(), axis, ranges);
        var output = AgencyDifferencePartition(axis, deadband);
        var law = CoarseReadSurface.DifferenceLaw(new Carrier(), input, output, orientation);
        var table = CoarseReadSurface.CompileDifference(new Carrier(), law);
        var plan = Assert.IsType<CoarseDifferencePlan>(CoarseDifferencePlan.Capture(table));
        var valueRegions = Enumerable.Range(0, 101).Select(value =>
            Array.FindIndex(ranges, range => value >= range.Low && value <= range.High)).ToArray();
        Assert.All(valueRegions, region => Assert.InRange(region, 0, 3));
        for (var leftMask = 0; leftMask < 16; leftMask++)
        for (var rightMask = 0; rightMask < 16; rightMask++)
        {
            var left = CoarseReadSurface.Publish(new Carrier(), input, leftMask, new Placeholder());
            var right = CoarseReadSurface.Publish(new Carrier(), input, rightMask, new Placeholder());
            var calculated = CoarseReadSurface.DifferenceComputed(new Carrier(), law, left, right);
            var lookedUp = CoarseReadSurface.DifferenceLookup(new Carrier(), table, left, right);
            var projected = plan.Evaluate(new Carrier(), left, right);
            if (leftMask == 0 || rightMask == 0)
            {
                AssertOpen(calculated, output); AssertOpen(lookedUp, output); AssertOpen(projected, output);
                continue;
            }
            // Enumerate actual admissible integer pairs. This oracle never
            // subtracts interval endpoints or consumes implementation table rows.
            var expected = 0;
            for (var a = 0; a <= 100; a++)
            for (var b = 0; b <= 100; b++)
                if ((leftMask & 1 << valueRegions[a]) != 0 && (rightMask & 1 << valueRegions[b]) != 0)
                    expected |= 1 << AgencyDifferenceRegion(orientation * (a - b), deadband);
            AssertMask(calculated, output, expected);
            AssertMask(lookedUp, output, expected);
            AssertMask(projected, output, expected);
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    public void AgencyDeadbandsRetainExactZeroAndBothNonzeroNearRegions(int deadband)
    {
        var axis = new Carrier();
        var output = AgencyDifferencePartition(axis, deadband);
        Assert.True(CoarseReadSurface.TryReadPartition(output, out var readAxis, out var regions));
        Assert.True(readAxis.SameIdentityAs(axis));
        Assert.Equal(5, regions.Length);
        Assert.Equal(0, Point(Assert.IsType<Relation>(regions[2].Left)).Ticks);
        Assert.Equal(0, Point(Assert.IsType<Relation>(regions[2].Right)).Ticks);
        var source = new Placeholder();
        foreach (var value in new[] { -100, -deadband - 1, -deadband, -1, 0, 1, deadband, deadband + 1, 100 })
            AssertMask(CoarseReadSurface.Measure(new Carrier(), output, value, source), output,
                1 << AgencyDifferenceRegion(value, deadband));
        AssertOpen(CoarseReadSurface.Measure(new Carrier(), output, -101, source), output);
        AssertOpen(CoarseReadSurface.Measure(new Carrier(), output, 101, source), output);
    }

    [Fact]
    public void NeutralSupportCanSettleADeadbandWithoutClaimingExactZero()
    {
        var axis = new Carrier();
        var input = CoarseReadSurface.Partition(new Carrier(), axis, AgencyRanges("dim_focused"));
        var gentleOutput = AgencyDifferencePartition(axis, 4);
        var broadOutput = AgencyDifferencePartition(axis, 10);
        var left = CoarseReadSurface.Publish(new Carrier(), input, 1, new Placeholder());
        var right = CoarseReadSurface.Publish(new Carrier(), input, 1, new Placeholder());
        Assert.NotSame(left, right);
        // Distinct samples in 0..7 admit -7..7. Broad deadband contains
        // all possibilities, including both signs; gentle deadband does not.
        var gentle = CoarseReadSurface.DifferenceComputed(new Carrier(),
            CoarseReadSurface.DifferenceLaw(new Carrier(), input, gentleOutput), left, right);
        var broad = CoarseReadSurface.DifferenceComputed(new Carrier(),
            CoarseReadSurface.DifferenceLaw(new Carrier(), input, broadOutput), left, right);
        AssertMask(gentle, gentleOutput, 0b11111);
        AssertMask(broad, broadOutput, 0b01110);
        Assert.True(CoarseReadSurface.TryReadMask(broad, broadOutput, out var support));
        Assert.NotEqual(0b00100, support);
        Assert.Equal(0, support & ~0b01110);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    public void ASignOnlyQueryPublishesBothMagnitudeRegionsRatherThanInventingASize(int deadband)
    {
        var magnitudeAxis = new Carrier();
        var orderAxis = new Carrier();
        var output = AgencyDifferencePartition(magnitudeAxis, deadband);
        foreach (var sign in new[] { -1, 0, 1 })
        {
            var acquisitions = new Relation(new Placeholder(), new Placeholder());
            var instrument = ReadWitness.Create(new Carrier(), acquisitions, Relation.Segment(orderAxis, -1, 1),
                new CarrierLocation(orderAxis, sign), new Placeholder(), acquisitions);
            var expected = sign switch { -1 => 0b00011, 0 => 0b00100, _ => 0b11000 };
            var query = CoarseReadSurface.Publish(new Carrier(), output, expected, instrument);
            AssertMask(query, output, expected);
            Assert.True(ReadWitness.TryRead(query, out var source, out _, out var result, out _, out _));
            Assert.Same(instrument, source);
            Assert.True(Assert.IsType<Relation>(result).TryReadOrderedGroup(out var alternatives));
            Assert.Equal(sign == 0 ? 1 : 2, alternatives.Length);
            Assert.Equal(sign == 0, (expected & ~0b01110) == 0);
            // A native rule admitting the whole sign union can steer after
            // the higher-priority neutral rule, without claiming near or far.
            if (sign < 0) Assert.Equal(0, expected & ~0b00011);
            if (sign > 0) Assert.Equal(0, expected & ~0b11000);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void ComputedAndCompiledDifferencesAgreeForEveryThreeRegionMask(int orientation)
    {
        var f = new Regions(orientation);
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        var plan = Assert.IsType<CoarseDifferencePlan>(CoarseDifferencePlan.Capture(table));
        for (var leftMask = 0; leftMask < 8; leftMask++)
        for (var rightMask = 0; rightMask < 8; rightMask++)
        {
            // Equal masks remain separate publications, not one quantity.
            var left = f.Publish(leftMask);
            var right = f.Publish(rightMask);
            var calculated = CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, left, right);
            var lookedUp = CoarseReadSurface.DifferenceLookup(new Carrier(), table, left, right);
            var projected = plan.Evaluate(new Carrier(), left, right);
            if (leftMask == 0 || rightMask == 0)
            {
                AssertOpen(calculated, f.Output);
                AssertOpen(lookedUp, f.Output);
                AssertOpen(projected, f.Output);
            }
            else
            {
                var expected = ExpectedDifference(leftMask, rightMask, orientation);
                AssertMask(calculated, f.Output, expected);
                AssertMask(lookedUp, f.Output, expected);
                AssertMask(projected, f.Output, expected);
            }
        }
    }

    [Fact]
    public void DisconnectedPossibilitiesKeepTheMiddleExcluded()
    {
        var f = new Regions();
        var disconnected = f.Publish(0b101);
        AssertMask(disconnected, f.Input, 0b101);
        Assert.True(ReadWitness.TryRead(disconnected, out _, out _, out var result, out _, out _));
        Assert.True(Assert.IsType<Relation>(result).TryReadOrderedGroup(out var included));
        Assert.True(CoarseReadSurface.TryReadPartition(f.Input, out _, out var regions));
        Assert.Equal(2, included.Length);
        Assert.Same(regions[0], included[0]);
        Assert.Same(regions[2], included[1]);

        var difference = CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, disconnected, f.Publish(0b010));
        // {0,1,4,5,6} - {2,3} admits both signs but never zero.
        AssertMask(difference, f.Output, 0b1110110);
        Assert.True(CoarseReadSurface.TryReadMask(difference, f.Output, out var mask));
        Assert.Equal(0, mask & (1 << 3));
    }

    [Fact]
    public void EqualLookingPublicationsDoNotEstablishExactCancellation()
    {
        var f = new Regions();
        var sharedDescription = new Placeholder();
        var left = CoarseReadSurface.Publish(new Carrier(), f.Input, 0b010, sharedDescription);
        var right = CoarseReadSurface.Publish(new Carrier(), f.Input, 0b010, sharedDescription);
        Assert.NotSame(left, right);
        AssertMask(CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, left, right), f.Output, 0b0011100);
        AssertMask(CoarseReadSurface.DifferenceLookup(new Carrier(), CoarseReadSurface.CompileDifference(new Carrier(), f.Law), left, right),
            f.Output, 0b0011100);
        // This first fixture deliberately does not refine correlated operands.
        AssertMask(CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, left, left), f.Output, 0b0011100);
    }

    [Fact]
    public void ReversingTheReadFrameChangesDifferenceSignWithoutChangingAmountRegions()
    {
        var f = new Regions();
        var reverse = CoarseReadSurface.DifferenceLaw(new Carrier(), f.Input, f.Output, -1);
        var large = f.Publish(0b100);
        var small = f.Publish(0b001);
        AssertMask(CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, large, small), f.Output, 0b1100000);
        AssertMask(CoarseReadSurface.DifferenceComputed(new Carrier(), reverse, large, small), f.Output, 0b0000011);
        AssertMask(CoarseReadSurface.DifferenceLookup(new Carrier(), CoarseReadSurface.CompileDifference(new Carrier(), reverse), large, small),
            f.Output, 0b0000011);
        AssertMask(large, f.Input, 0b100);
        AssertMask(small, f.Input, 0b001);
        AssertEquationOrientation(f.Law, f.Axis, 1);
        AssertEquationOrientation(reverse, f.Axis, -1);
    }

    [Fact]
    public void EmptyUnknownAndAbsentAreNotNumericZero()
    {
        var f = new Regions();
        var empty = f.Publish(0);
        var unknown = CoarseReadSurface.Unknown(new Carrier(), f.Input, new Placeholder());
        var absent = SamplingSurface.Sample(new Carrier(), new Placeholder(), new Carrier(), false);
        var known = f.Publish(1);
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        AssertMask(empty, f.Input, 0);
        AssertOpen(unknown, f.Input);
        Assert.False(CoarseReadSurface.TryReadMask(absent, f.Input, out _));

        foreach (var unavailable in new[] { empty, unknown, absent })
        {
            AssertOpen(CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, unavailable, known), f.Output);
            AssertOpen(CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, known, unavailable), f.Output);
            AssertOpen(CoarseReadSurface.DifferenceLookup(new Carrier(), table, unavailable, known), f.Output);
            AssertOpen(CoarseReadSurface.DifferenceLookup(new Carrier(), table, known, unavailable), f.Output);
        }
    }

    [Fact]
    public void AnExactSingletonDifferenceCanPublishTheActualZeroRegion()
    {
        var axis = new Carrier();
        var input = CoarseReadSurface.Partition(new Carrier(), axis, (0, 0), (1, 1), (2, 2));
        var output = CoarseReadSurface.Partition(new Carrier(), axis, (-2, -1), (0, 0), (1, 2));
        var law = CoarseReadSurface.DifferenceLaw(new Carrier(), input, output);
        var a = CoarseReadSurface.Publish(new Carrier(), input, 0b010, new Placeholder());
        var b = CoarseReadSurface.Publish(new Carrier(), input, 0b010, new Placeholder());
        AssertMask(CoarseReadSurface.DifferenceComputed(new Carrier(), law, a, b), output, 0b010);
        AssertMask(CoarseReadSurface.DifferenceLookup(new Carrier(), CoarseReadSurface.CompileDifference(new Carrier(), law), a, b), output, 0b010);
    }

    [Fact]
    public void ExternalMeasurementDiscardsInteriorRawCoordinates()
    {
        var axis = new Carrier();
        var input = CoarseReadSurface.Partition(new Carrier(), axis, (0, 10), (11, 100), (101, 1000));
        var source = new Placeholder();
        var measured = CoarseReadSurface.Measure(new Carrier(), input, 123, source);
        AssertMask(measured, input, 0b100);
        Assert.True(ReadWitness.TryRead(measured, out var selected, out _, out _, out _, out _));
        Assert.Same(source, selected);
        Assert.DoesNotContain(Reachable(measured), element => element is CarrierLocation location && location.Ticks == 123);
        AssertOpen(CoarseReadSurface.Measure(new Carrier(), input, -1, source), input);
        AssertOpen(CoarseReadSurface.Measure(new Carrier(), input, 1001, source), input);
    }

    [Fact]
    public void NativeCompiledRowsRetainTheirLawAndRegionCorrespondences()
    {
        var f = new Regions();
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        Assert.True(table.TryReadOrderedGroup(out var fields));
        Assert.Equal(3, fields.Length);
        Assert.Same(f.Law, fields[0]);
        var captured = Assert.IsType<Relation>(fields[1]);
        Assert.NotSame(f.Law, captured);
        Assert.True(CoarseReadSurface.TryReadLaw(captured, out var input, out var output, out var orientation));
        Assert.Equal(1, orientation);
        Assert.True(CoarseReadSurface.TryReadPartition(input, out var axis, out var inputRegions));
        Assert.True(CoarseReadSurface.TryReadPartition(output, out _, out var outputRegions));
        Assert.True(axis.SameIdentityAs(f.Axis));
        AssertEquationOrientation(captured, f.Axis, 1);
        Assert.True(Assert.IsType<Relation>(fields[2]).TryReadOrderedGroup(out var rows));
        Assert.Equal(9, rows.Length);
        var pairs = new HashSet<(int, int)>();
        foreach (var element in rows)
        {
            var row = Assert.IsType<Relation>(element);
            Assert.True(Assert.IsType<Relation>(row.Left).TryReadOrderedGroup(out var pair));
            Assert.Equal(2, pair.Length);
            var i = Array.FindIndex(inputRegions, region => region.Equals(pair[0]));
            var j = Array.FindIndex(inputRegions, region => region.Equals(pair[1]));
            Assert.InRange(i, 0, 2); Assert.InRange(j, 0, 2);
            Assert.True(pairs.Add((i, j)));
            Assert.True(Assert.IsType<Relation>(row.Right).TryReadOrderedGroup(out var selected));
            Assert.NotEmpty(selected);
            Assert.All(selected, region => Assert.Contains(outputRegions, candidate => candidate.Equals(region)));
        }
    }

    [Fact]
    public void WrongAxisOrDifferentPartitionNeedsAnExplicitCorrespondence()
    {
        var f = new Regions();
        var otherAxis = new Carrier(f.Axis.UnitShape);
        var foreign = CoarseReadSurface.Partition(new Carrier(), otherAxis, Regions.InputRanges);
        var different = CoarseReadSurface.Partition(new Carrier(), f.Axis, (0, 0), (1, 2), (3, 6));
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        foreach (var partition in new[] { foreign, different })
        {
            var incompatible = CoarseReadSurface.Publish(new Carrier(), partition, 1, new Placeholder());
            Assert.False(CoarseReadSurface.TryReadMask(incompatible, f.Input, out _));
            AssertOpen(CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, incompatible, f.Publish(1)), f.Output);
            AssertOpen(CoarseReadSurface.DifferenceLookup(new Carrier(), table, incompatible, f.Publish(1)), f.Output);
        }
    }

    [Fact]
    public void InvalidPartitionsPreserveTheirUnresolvedStructure()
    {
        var axis = new Carrier();
        var invalid = new[]
        {
            CoarseReadSurface.Partition(new Carrier(), axis),
            CoarseReadSurface.Partition(new Carrier(), axis, (0, 1), (3, 4)),
            CoarseReadSurface.Partition(new Carrier(), axis, (0, 2), (2, 4)),
            CoarseReadSurface.Partition(new Carrier(), axis, (2, 3), (0, 1)),
            CoarseReadSurface.Partition(new Carrier(), axis, (1, 0)),
            CoarseReadSurface.Partition(new Carrier(), axis, (-4097, 0)),
            CoarseReadSurface.Partition(new Carrier(), axis, (0, 4097)),
            CoarseReadSurface.Partition(new Carrier(), axis, (int.MinValue, int.MaxValue)),
            CoarseReadSurface.Partition(new Carrier(), Carrier.Unknown, (0, 1)),
            CoarseReadSurface.Partition(new Carrier(), axis, Enumerable.Range(0, 9).Select(n => (n, n)).ToArray())
        };
        foreach (var partition in invalid)
        {
            Assert.False(CoarseReadSurface.TryReadPartition(partition, out _, out _));
            AssertOpen(CoarseReadSurface.Publish(new Carrier(), partition, 1, new Placeholder()), partition);
        }
    }

    [Fact]
    public void InvalidLawsDoNotCompileOrCalculateAnAnswer()
    {
        var f = new Regions();
        var invalid = new[]
        {
            CoarseReadSurface.DifferenceLaw(new Carrier(), f.Input, f.Output, 0),
            CoarseReadSurface.DifferenceLaw(new Carrier(), f.Input, f.Output, 2),
            CoarseReadSurface.DifferenceLaw(new Carrier(), f.Input,
                CoarseReadSurface.Partition(new Carrier(), f.Axis, (-5, -1), (0, 0), (1, 5))),
            CoarseReadSurface.DifferenceLaw(new Carrier(), f.Input,
                CoarseReadSurface.Partition(new Carrier(), f.Axis, (-6, -1), (1, 6))),
            CoarseReadSurface.DifferenceLaw(new Carrier(), f.Input,
                CoarseReadSurface.Partition(new Carrier(), new Carrier(f.Axis.UnitShape), Regions.OutputRanges))
        };
        foreach (var law in invalid)
        {
            Assert.False(CoarseReadSurface.TryReadLaw(law, out _, out _, out _));
            var left = f.Publish(1); var right = f.Publish(4);
            AssertOpen(CoarseReadSurface.DifferenceComputed(new Carrier(), law, left, right), f.Output);
            AssertOpen(CoarseReadSurface.DifferenceLookup(new Carrier(), CoarseReadSurface.CompileDifference(new Carrier(), law), left, right), f.Output);
        }
    }

    [Fact]
    public void OutOfDomainMasksAndMalformedPublicationsCannotBecomeKnownZero()
    {
        var f = new Regions();
        AssertOpen(CoarseReadSurface.Publish(new Carrier(), f.Input, -1, new Placeholder()), f.Input);
        AssertOpen(CoarseReadSurface.Publish(new Carrier(), f.Input, 8, new Placeholder()), f.Input);
        var malformed = ReadWitness.Create(new Carrier(), new Placeholder(), f.Input,
            G(new CarrierLocation(f.Axis, 0)), new Placeholder(), f.Input);
        Assert.False(CoarseReadSurface.TryReadMask(malformed, f.Input, out _));
        AssertOpen(CoarseReadSurface.DifferenceComputed(new Carrier(), f.Law, malformed, f.Publish(1)), f.Output);
    }

    [Fact]
    public void AnEquationOnAnotherInputAxisCannotJustifyTheLocalDifferenceLaw()
    {
        var f = new Regions();
        var foreignAxis = new Carrier(f.Axis.UnitShape);
        Assert.True(f.Law.TryReadOrderedGroup(out var fields));
        var equation = Assert.IsType<Relation>(fields[3]);
        var ports = Assert.IsType<Relation>(equation.Left);
        var foreignPort = new Relation(Relation.Segment(foreignAxis, 0, 1),
            Assert.IsType<Relation>(ports.Left).Right);
        var forgedEquation = new Relation(new Relation(foreignPort, ports.Right), equation.Right);
        var forgedLaw = G(fields[0], fields[1], fields[2], forgedEquation);
        Assert.False(CoarseReadSurface.TryReadLaw(forgedLaw, out _, out _, out _));
        AssertOpen(CoarseReadSurface.DifferenceComputed(new Carrier(), forgedLaw, f.Publish(1), f.Publish(4)), f.Output);
        Assert.Null(CoarseDifferencePlan.Capture(CoarseReadSurface.CompileDifference(new Carrier(), forgedLaw)));
    }

    [Fact]
    public void AValidLookingRowWithTheWrongResultCannotReplaceTheEquationLaw()
    {
        var f = new Regions();
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        Assert.True(table.TryReadOrderedGroup(out var fields));
        Assert.True(Assert.IsType<Relation>(fields[2]).TryReadOrderedGroup(out var rows));
        Assert.True(CoarseReadSurface.TryReadPartition(f.Output, out _, out var outputRegions));
        // Row 0 is small minus small, admitting -1, 0 and +1. A forged
        // well-formed union containing only the zero region is too precise.
        rows[0] = new Relation(Assert.IsType<Relation>(rows[0]).Left, G(outputRegions[3]));
        var forged = G(fields[0], fields[1], G(rows));
        Assert.False(CoarseReadSurface.TryReadTable(forged, out _, out _, out _));
        AssertOpen(CoarseReadSurface.DifferenceLookup(new Carrier(), forged, f.Publish(1), f.Publish(1)), f.Output);
        Assert.Null(CoarseDifferencePlan.Capture(forged));
    }

    [Fact]
    public void FiniteDomainEndpointsCanBeReadWithoutWiderArithmetic()
    {
        var axis = new Carrier();
        var input = CoarseReadSurface.Partition(new Carrier(), axis, (-4096, -4096), (-4095, -1), (0, 0));
        var output = CoarseReadSurface.Partition(new Carrier(), axis, (-4096, -1), (0, 0), (1, 4096));
        var law = CoarseReadSurface.DifferenceLaw(new Carrier(), input, output);
        Assert.True(CoarseReadSurface.TryReadLaw(law, out _, out _, out _));
        var low = CoarseReadSurface.Publish(new Carrier(), input, 1, new Placeholder());
        var high = CoarseReadSurface.Publish(new Carrier(), input, 4, new Placeholder());
        AssertMask(CoarseReadSurface.DifferenceComputed(new Carrier(), law, low, high), output, 1);
        AssertMask(CoarseReadSurface.DifferenceLookup(new Carrier(), CoarseReadSurface.CompileDifference(new Carrier(), law), high, low), output, 4);
    }

    [Fact]
    public void StaleCompiledLawIsRejectedInsteadOfUsingOldRows()
    {
        var f = new Regions();
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        Assert.True(table.TryReadOrderedGroup(out var fields));
        Assert.True(CoarseReadSurface.TryReadLaw(Assert.IsType<Relation>(fields[1]), out var frozenInput, out var frozenOutput, out _));
        var left = CoarseReadSurface.Publish(new Carrier(), frozenInput, 1, new Placeholder());
        var right = CoarseReadSurface.Publish(new Carrier(), frozenInput, 4, new Placeholder());
        AssertMask(CoarseReadSurface.DifferenceLookup(new Carrier(), table, left, right), frozenOutput, 0b0000011);

        RetargetMinimum(f.Input, 1);
        Assert.True(CoarseReadSurface.TryReadLaw(f.Law, out _, out _, out _));
        AssertOpen(CoarseReadSurface.DifferenceLookup(new Carrier(), table, left, right), frozenOutput);
        Assert.True(CoarseReadSurface.TryReadPartition(frozenInput, out _, out var stableRegions));
        Assert.Equal(0, Point(Assert.IsType<Relation>(stableRegions[0].Left)).Ticks);
    }

    [Fact]
    public void ArchiveRetainsTheLawRowsAndPublicationCorrespondenceWithinItsNewWorld()
    {
        var f = new Regions();
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        var bundle = G(table, f.Publish(5), f.Publish(2));
        var restored = Assert.IsType<Relation>(GojoEncodedSnapshot.CaptureArchive(bundle).RestoreRoots()[0]);
        Assert.True(restored.TryReadOrderedGroup(out var roots));
        var restoredTable = Assert.IsType<Relation>(roots[0]);
        Assert.True(restoredTable.TryReadOrderedGroup(out var fields));
        var restoredLaw = Assert.IsType<Relation>(fields[0]);
        Assert.True(CoarseReadSurface.TryReadLaw(restoredLaw, out _, out var output, out _));
        var left = Assert.IsType<Relation>(roots[1]); var right = Assert.IsType<Relation>(roots[2]);
        AssertMask(CoarseReadSurface.DifferenceComputed(new Carrier(), restoredLaw, left, right), output, 0b1110110);
        AssertMask(CoarseReadSurface.DifferenceLookup(new Carrier(), restoredTable, left, right), output, 0b1110110);
    }

    [Fact]
    public void CapturedProjectionKeepsItsServiceAfterOriginalSourceCoordinatesMove()
    {
        var f = new Regions();
        var table = CoarseReadSurface.CompileDifference(new Carrier(), f.Law);
        var plan = Assert.IsType<CoarseDifferencePlan>(CoarseDifferencePlan.Capture(table));
        Assert.True(CoarseReadSurface.TryReadTable(plan.NativeTable, out _, out var law, out var rows));
        Assert.True(CoarseReadSurface.TryReadLaw(law, out var input, out var output, out _));
        var left = CoarseReadSurface.Publish(new Carrier(), input, 5, new Placeholder());
        var right = CoarseReadSurface.Publish(new Carrier(), input, 2, new Placeholder());
        var before = plan.Evaluate(new Carrier(), left, right);
        AssertMask(before, output, 0b1110110);
        Assert.True(ReadWitness.TryRead(before, out var selected, out _, out _, out _, out var provenance));
        Assert.True(Assert.IsType<Relation>(selected).TryReadOrderedGroup(out var operands));
        Assert.Same(left, operands[0]); Assert.Same(right, operands[1]);
        Assert.True(Assert.IsType<Relation>(provenance).TryReadOrderedGroup(out var evidence));
        Assert.True(Assert.IsType<Relation>(evidence[0]).Equals(plan.NativeTable));
        Assert.True(Assert.IsType<Relation>(evidence[1]).TryReadOrderedGroup(out var used));
        Assert.Equal(2, used.Length);
        Assert.True(rows[1].Equals(used[0]));
        Assert.True(rows[7].Equals(used[1]));

        RetargetMinimum(f.Input, 1);
        Assert.True(CoarseReadSurface.TryReadLaw(f.Law, out _, out _, out _));
        AssertOpen(CoarseReadSurface.DifferenceLookup(new Carrier(), table, left, right), output);
        AssertMask(plan.Evaluate(new Carrier(), left, right), output, 0b1110110);
    }

    [Fact]
    public void EditingAnInspectionCopyDoesNotRetargetTheCapturedProjection()
    {
        var f = new Regions();
        var plan = Assert.IsType<CoarseDifferencePlan>(CoarseDifferencePlan.Capture(
            CoarseReadSurface.CompileDifference(new Carrier(), f.Law)));
        var inspection = plan.NativeTable;
        Assert.True(CoarseReadSurface.TryReadTable(inspection, out var sourceLaw, out _, out _));
        Assert.True(CoarseReadSurface.TryReadLaw(sourceLaw, out var exposedInput, out _, out _));
        RetargetMinimum(exposedInput, 1);

        Assert.True(CoarseReadSurface.TryReadTable(plan.NativeTable, out var intactLaw, out _, out _));
        Assert.True(CoarseReadSurface.TryReadLaw(intactLaw, out var intactInput, out _, out _));
        Assert.True(CoarseReadSurface.TryReadPartition(intactInput, out _, out var regions));
        Assert.Equal(0, Point(Assert.IsType<Relation>(regions[0].Left)).Ticks);
        AssertMask(plan.Evaluate(new Carrier(), f.Publish(5), f.Publish(2)), f.Output, 0b1110110);
    }

    [Fact]
    public void RetargetingRetainedWitnessSitesInvalidatesTheCapturedProjection()
    {
        var f = new Regions();
        var plan = Assert.IsType<CoarseDifferencePlan>(CoarseDifferencePlan.Capture(
            CoarseReadSurface.CompileDifference(new Carrier(), f.Law)));
        var left = f.Publish(5); var right = f.Publish(2);
        var read = plan.Evaluate(new Carrier(), left, right);
        AssertMask(read, f.Output, 0b1110110);
        Assert.True(ReadWitness.TryRead(read, out _, out _, out _, out _, out var provenance));
        Assert.True(Assert.IsType<Relation>(provenance).TryReadOrderedGroup(out var evidence));
        var exposedTable = Assert.IsType<Relation>(evidence[0]);
        Assert.True(CoarseReadSurface.TryReadTable(exposedTable, out _, out var capturedLaw, out _));
        Assert.True(CoarseReadSurface.TryReadLaw(capturedLaw, out var exposedInput, out _, out _));
        RetargetMinimum(exposedInput, 1);
        // No hidden recomputation or reinterpretation of the stale integer rows.
        AssertOpen(plan.Evaluate(new Carrier(), left, right), f.Output);
    }

    private static int ExpectedDifference(int leftMask, int rightMask, int orientation)
    {
        var result = 0;
        for (var a = 0; a <= 6; a++)
        for (var b = 0; b <= 6; b++)
        {
            if ((leftMask & (1 << InputRegion(a))) == 0 || (rightMask & (1 << InputRegion(b))) == 0) continue;
            result |= 1 << OutputRegion(orientation * (a - b));
        }
        return result;
        static int InputRegion(int value) => value <= 1 ? 0 : value <= 3 ? 1 : 2;
        static int OutputRegion(int value) => value <= -4 ? 0 : value <= -2 ? 1 : value == -1 ? 2 :
            value == 0 ? 3 : value == 1 ? 4 : value <= 3 ? 5 : 6;
    }

    private static (int Low, int High)[] AgencyRanges(string name) => name switch
    {
        "baseline" => [(0, 14), (15, 44), (45, 79), (80, 100)],
        "uniform" => [(0, 24), (25, 49), (50, 74), (75, 100)],
        "dim_focused" => [(0, 7), (8, 23), (24, 55), (56, 100)],
        "bright_focused" => [(0, 49), (50, 79), (80, 91), (92, 100)],
        _ => throw new ArgumentException("Unknown test fixture partition.", nameof(name))
    };

    private static Relation AgencyDifferencePartition(Carrier axis, int deadband) =>
        CoarseReadSurface.Partition(new Carrier(), axis, (-100, -deadband - 1), (-deadband, -1),
            (0, 0), (1, deadband), (deadband + 1, 100));

    private static int AgencyDifferenceRegion(int value, int deadband) =>
        value < -deadband ? 0 : value < 0 ? 1 : value == 0 ? 2 : value <= deadband ? 3 : 4;

    private static void AssertEquationOrientation(Relation law, Carrier axis, int orientation)
    {
        Assert.True(law.TryReadOrderedGroup(out var fields));
        var equation = Assert.Single(fields.OfType<Relation>(), candidate =>
            candidate.Right is Relation point && point.Left is CarrierLocation location && location.Ticks == 1 &&
            location.Carrier.SameIdentityAs(axis) && candidate.Left is Relation { Left: Relation, Right: Relation });
        var ports = Assert.IsType<Relation>(equation.Left);
        var operands = new[] { ports.Left, ports.Right };
        for (var i = 0; i < 2; i++)
        {
            var port = Assert.IsType<Relation>(operands[i]);
            var input = Assert.IsType<Relation>(port.Left);
            Assert.Equal(0, Point(Assert.IsType<Relation>(input.Left)).Ticks);
            Assert.Equal(1, Point(Assert.IsType<Relation>(input.Right)).Ticks);
            var output = Assert.IsType<Relation>(port.Right);
            Assert.Equal(0, Point(Assert.IsType<Relation>(output.Left)).Ticks);
            Assert.Equal(i == 0 ? orientation : -orientation, Point(Assert.IsType<Relation>(output.Right)).Ticks);
        }
    }

    private static CarrierLocation Point(Relation point) => Assert.IsType<CarrierLocation>(point.Left);
    private static void RetargetMinimum(Relation partition, int minimum)
    {
        Assert.True(partition.TryReadOrderedGroup(out var fields));
        Assert.True(CoarseReadSurface.TryReadPartition(partition, out _, out var regions));
        Point(Assert.IsType<Relation>(Assert.IsType<Relation>(fields[0]).Left)).Tick.Set(minimum);
        Point(Assert.IsType<Relation>(regions[0].Left)).Tick.Set(minimum);
    }
    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static void AssertMask(Relation publication, Relation partition, int expected)
    {
        Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _));
        Assert.IsType<Relation>(result);
        Assert.True(CoarseReadSurface.TryReadMask(publication, partition, out var mask));
        Assert.Equal(expected, mask);
    }
    private static void AssertOpen(Relation publication, Relation partition)
    {
        Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _));
        Assert.IsType<Placeholder>(result);
        Assert.False(CoarseReadSurface.TryReadMask(publication, partition, out _));
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
    }
    private sealed class Regions
    {
        public static readonly (int, int)[] InputRanges = [(0, 1), (2, 3), (4, 6)];
        public static readonly (int, int)[] OutputRanges = [(-6, -4), (-3, -2), (-1, -1), (0, 0), (1, 1), (2, 3), (4, 6)];
        public Carrier Axis { get; } = new();
        public Relation Input { get; }
        public Relation Output { get; }
        public Relation Law { get; }
        public Regions(int orientation = 1)
        {
            Input = CoarseReadSurface.Partition(new Carrier(), Axis, InputRanges);
            Output = CoarseReadSurface.Partition(new Carrier(), Axis, OutputRanges);
            Law = CoarseReadSurface.DifferenceLaw(new Carrier(), Input, Output, orientation);
        }
        public Relation Publish(int mask) => CoarseReadSurface.Publish(new Carrier(), Input, mask, new Placeholder());
    }
}
