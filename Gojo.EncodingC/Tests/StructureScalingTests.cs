using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Phase 3 of GojoOperations-2026-11.md (X11): whole-structure
/// scaling on the canonical plate-with-hole-and-datum fixture inside grouped
/// Forks — separating O(1) frame/port recalibration, Attention-selected
/// uniform dilation hitting a fixed mounting/capacity bound (local T+),
/// 1-axis stretch turning a circular hole into a connected mismatch,
/// structural replication, and Plate × Thickness with through-hole exclusion.</summary>
public sealed class StructureScalingTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field UnaryField(Store store, int capacity) =>
        new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);

    /// <summary>Builds a multi-position plate store:
    /// position 0 = outline X (width=3), position 1 = outline Y (height=2),
    /// position 2 = hole radius X (r=1), position 3 = hole radius Y (r=1),
    /// position 4 = mounting datum offset (fixed outside attention focus).
    /// Also tracks a local capacity ceiling (max=6) where overflow lands in T+.</summary>
    private static (Store Plate, Field Measure, Tension TensionSite, Field Thickness, Store FrameStore, Field UnitScale) BuildPlate()
    {
        var plate = new Store(0, 5);
        var measure = UnaryField(plate, 6);
        var site = new Tension(plate, measure, capacity: 8);
        var thickness = UnaryField(plate, 4);

        measure.Write(plate, 0, 3); // outline width
        measure.Write(plate, 1, 2); // outline height
        measure.Write(plate, 2, 1); // hole rx
        measure.Write(plate, 3, 1); // hole ry
        measure.Write(plate, 4, 2); // mounting datum (outside metric focus)
        thickness.Write(plate, 0, 2);

        var frameStore = new Store(0, 1);
        var unitScale = UnaryField(frameStore, 4);
        unitScale.Write(frameStore, 0, 1); // 1x scale initially
        return (plate, measure, site, thickness, frameStore, unitScale);
    }

    /// <summary>1. O(1) Frame / Port recalibration: retargeting the shared
    /// unit-scale landmark scales every external read without editing a single
    /// interior bit of the plate, keeping all internal ratios invariant.</summary>
    [Fact]
    public void FramePortRecalibrationScalesAllReadsInConstantTimeWithoutTouchingInterior()
    {
        var (plate, measure, _, _, frameStore, unitScale) = BuildPlate();
        var platePort = plate.Publish([0, 1, 2, 3], [.. measure.Planes]);
        var framePort = frameStore.Publish([0], [.. unitScale.Planes]);

        // External reader applies the shared frame's unit scale to every port lane.
        var reader = new Store(0, 4);
        var scaled = new Field(Shape.Binary(25), [.. Enumerable.Range(0, 5).Select(_ => reader.AddPlane())]);
        var scaleRead = TransitionLaw.FromFunction(
            [Shape.Unary(6), Shape.Unary(4), Shape.Binary(25)],
            s => [s[0], s[1], s[0] * s[1]],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));

        var plateSock = Socket.For(platePort); plateSock.Plug(platePort);
        var frameSock = Socket.For(framePort); frameSock.Plug(framePort);
        var wiring = new Wiring(scaleRead, new Binding(plateSock, [.. Enumerable.Range(0, 6)]), new Binding(frameSock, [.. Enumerable.Range(0, 4)]), scaled.On(reader));
        for (var lane = 0; lane < 4; lane++) wiring.Add(lane, 0, lane);

        var plateEngine = new Engine().Hold(plate);
        var frameEngine = new Engine().Hold(frameStore);
        var readerEngine = new Engine().Add(wiring);

        var interiorBefore = Transport.Save(plateEngine, plate);

        // Retarget single shared frame unit from 1 -> 3 (O(1)):
        unitScale.Write(frameStore, 0, 3);
        Assert.NotNull(readerEngine.Settle(8));

        Assert.Equal([9, 6, 3, 3], [.. Enumerable.Range(0, 4).Select(i => scaled.Read(reader, i)!.Value)]);
        // Circular hole ratio rx == ry stayed invariant (3 == 3) and plate interior bits are untouched!
        Assert.Equal(scaled.Read(reader, 2), scaled.Read(reader, 3));
        Assert.Equal(interiorBefore, Transport.Save(plateEngine, plate));
    }

    /// <summary>2. Attention-selected uniform dilation (×3) inside a grouped
    /// Fork: measure lanes 0..3 scale ×3, mounting datum (lane 4, unattended)
    /// stays put, and width 3×3=9 exceeding capacity 6 surfaces exact excess 3
    /// as localized T+ tension right on lane 0!</summary>
    [Fact]
    public void SelectedLaneBroadcastExposesLocalCapacityTensionAndKeepsDatumUntouched()
    {
        var (plate, measure, site, _, _, _) = BuildPlate();
        // Add +2x units (net ×3) into Tension.Plus on selected lanes, then Absorb into capacity-6 Value.
        // One-shot trigger bit per lane ensures single application under Settle().
        var pending = plate.AddPlane();
        for (var i = 0; i < 5; i++) plate.Write(pending, i, true);

        var scaleAddToPlus = TransitionLaw.FromFunction(
            [Shape.Unary(6), Shape.Unary(8), Shape.Bit], // (val, plus, pending)
            s => s[2] == 0 ? s : [s[0], s[1] + 2 * s[0] <= 8 ? s[1] + 2 * s[0] : 8, 0],
            new Accounting(true, new Unchanged(0), new NeverIncreases(2)));

        var all5 = Plane.Ones(5);
        var broadcast = Tension.Wire(scaleAddToPlus, [measure.On(plate), site.Plus.On(plate), new Binding(plate, pending)], all5);
        var att = broadcast.Attend();
        broadcast.Rows.Write(att, 4, false); // lane 4 (mounting datum) is outside the focus!

        var parent = new Engine().Add(broadcast).Add(site.Absorb(all5));

        // Run as a candidate inside a Fork — parent stays completely untouched until Commit!
        var fork = parent.Fork(holdMaterial: false);
        Assert.NotNull(fork.Engine.Settle(32));
        var view = fork.Of(plate);

        // Lane 0 (width 3 -> 9): hits capacity 6 with 3 units waiting in T+!
        Assert.Equal((6, 3), (measure.Read(view, 0), site.Plus.Read(view, 0)));
        // Lane 1 (height 2 -> 6): fits exactly with 0 T+.
        Assert.Equal((6, 0), (measure.Read(view, 1), site.Plus.Read(view, 1)));
        // Lanes 2, 3 (hole rx=1->3, ry=1->3): stay circular with 0 T+.
        Assert.Equal((3, 0), (measure.Read(view, 2), site.Plus.Read(view, 2)));
        Assert.Equal((3, 0), (measure.Read(view, 3), site.Plus.Read(view, 3)));
        // Lane 4 (mounting datum, unattended): untouched at 2!
        Assert.Equal((2, 0), (measure.Read(view, 4), site.Plus.Read(view, 4)));

        // Parent still holds the original unscaled plate.
        Assert.Equal(3, measure.Read(plate, 0));
    }

    /// <summary>3. Anisotropic 1-axis stretch (×3 along X only): width scales
    /// 3->6(+3 T+) and hole rx scales 1->3 while hole ry stays 1 — immediately
    /// triggering the circularity constraint (ValueGap disagrees == 1) on the
    /// hole as an ellipse/connected mismatch.</summary>
    [Fact]
    public void OneAxisStretchSurfacesCircularHoleMismatchViaValueGap()
    {
        var (plate, measure, site, _, _, _) = BuildPlate();
        var pending = plate.AddPlane();
        for (var i = 0; i < 5; i++) plate.Write(pending, i, true);

        var scaleAddToPlus = TransitionLaw.FromFunction(
            [Shape.Unary(6), Shape.Unary(8), Shape.Bit],
            s => s[2] == 0 ? s : [s[0], s[1] + 2 * s[0] <= 8 ? s[1] + 2 * s[0] : 8, 0],
            new Accounting(true, new Unchanged(0), new NeverIncreases(2)));

        // Attention selects ONLY X lanes (0 = outline width, 2 = hole rx).
        var xOnly = Plane.Of(5, 0, 2);
        var stretchX = Tension.Wire(scaleAddToPlus, [measure.On(plate), site.Plus.On(plate), new Binding(plate, pending)], xOnly);
        var engine = new Engine().Add(stretchX).Add(site.Absorb(xOnly));
        Assert.NotNull(engine.Settle(32));

        // Now check circularity constraint rx == ry using master's Settling.ValueGap!
        var checkStore = new Store(0, 1);
        var rxOpen = Supports.On(checkStore, new Field(Shape.Unary(6), [.. Enumerable.Range(0, 6).Select(_ => checkStore.AddPlane())]));
        var ryOpen = Supports.On(checkStore, new Field(Shape.Unary(6), [.. Enumerable.Range(0, 6).Select(_ => checkStore.AddPlane())]));
        var mismatch = Supports.On(checkStore, new Field(Shape.Bit, checkStore.AddPlane()));
        rxOpen.Restrict(0, 1UL << measure.Read(plate, 2)!.Value); // 3
        ryOpen.Restrict(0, 1UL << measure.Read(plate, 3)!.Value); // 1

        var circ = new Wiring(Settling.ValueGap(Shape.Unary(6)), rxOpen.Variable, ryOpen.Variable, mismatch.Variable);
        circ.Add(0, 0, 0);
        Assert.NotNull(new Engine().Add(circ).Settle(8));

        // rx = 3 != ry = 1 -> circular passage constraint settles mismatch (disagrees) = 1!
        Assert.Equal((3, 1), (measure.Read(plate, 2), measure.Read(plate, 3)));
        Assert.Equal(1UL << 1, mismatch.Values(0));
    }

    /// <summary>4. Structural replication (3 × Plate) vs bilinear extrusion
    /// (Plate × Thickness with through-hole exclusion): replication gives three
    /// distinct identities sharing design, while Plate × Thickness forms a 3D
    /// occupied volume excluding the through-hole at every thickness layer.</summary>
    [Fact]
    public void StructuralReplicationAndPlateTimesThicknessPreserveIdentityAndThroughHole()
    {
        // Replication: 3 placed plate instances (positions 0, 1, 2) each own independent identity/state.
        var copies = new Store(0, 3);
        var width = UnaryField(copies, 4);
        var height = UnaryField(copies, 4);
        for (var i = 0; i < 3; i++) { width.Write(copies, i, 3); height.Write(copies, i, 2); }
        // Perturbing instance 1 never mutates instances 0 or 2.
        width.Write(copies, 1, 4);
        Assert.Equal([3, 4, 3], [.. Enumerable.Range(0, 3).Select(i => width.Read(copies, i)!.Value)]);

        // Plate × Thickness (3×3 plate with 1×1 center through-hole × 2 thickness layers):
        // Occupied 2D plate cells = 9 - 1 = 8; extruded across 2 thickness layers = 16 occupied voxels (2 excluded hole voxels).
        const int PlateCells = 9;
        const int Layers = 2;
        var geom = new Store(3, PlateCells * Layers); // plane 0: plate occupancy, plane 1: layer active, plane 2: voxel occupied
        for (var z = 0; z < Layers; z++)
            for (var c = 0; c < PlateCells; c++)
            {
                var idx = z * PlateCells + c;
                geom.Write(0, idx, c != 4); // cell 4 is the through-hole (excluded!)
                geom.Write(1, idx, true);   // thickness layer present
            }

        var voxOpen = Supports.On(geom, new Field(Shape.Bit, 2));
        var extrude = new Wiring(Booleans.And, new Binding(geom, 0), new Binding(geom, 1), voxOpen.Variable);
        for (var idx = 0; idx < PlateCells * Layers; idx++) extrude.Add(idx, idx, idx);
        Assert.NotNull(new Engine().Add(extrude).Settle(8));

        var occupiedVoxels = Enumerable.Range(0, PlateCells * Layers).Count(idx => voxOpen.Values(idx) == (1UL << 1));
        var excludedHoleVoxels = Enumerable.Range(0, PlateCells * Layers).Count(idx => voxOpen.Values(idx) == (1UL << 0));
        Assert.Equal(16, occupiedVoxels);
        Assert.Equal(2, excludedHoleVoxels);
    }
}
