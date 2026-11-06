namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Two supply sites can refill one cell. A persistent target asks for
/// occupancy. External use moves its contents into one of two receiving sites.</summary>
public sealed class GoalMaintenance
{
    private readonly PortStructure owner = new();
    private readonly ReadConnections reads = new();
    private readonly int gapTable, mirror;
    private readonly ConnectionRow gapRow;
    private readonly PassageConnections fills, uses;
    private readonly ConnectionRow[] fillRows, useRows;
    private readonly IPrimitiveReadPort target;

    public GoalMaintenance(IPrimitiveReadPort target)
    {
        this.target = target;
        int Site(bool occupied = false) => owner.Publish(owner.Create(0, 0, 1, occupied ? 1UL : 0UL));
        var supply = new[] { Site(true), Site(true) };
        var cell = Site();
        var used = new[] { Site(), Site() };
        mirror = owner.Create(0, 0, 1, 0);
        var control = owner.Publish(mirror);
        var externalUse = Site(true);
        gapTable = reads.AddTable(ConnectionLaws.FirstWithoutSecond);
        gapRow = reads.Wire(gapTable, 3, reads.Import(target), reads.Import(owner.Connect(cell)),
            reads.Import(owner.Connect(control)));
        fills = new(owner, PassageLaw.Controlled());
        fillRows = supply.Select(port => fills.Connect(3, port, control, cell)).ToArray();
        uses = new(owner, PassageLaw.Unconditional());
        useRows = used.Select(port => uses.Connect(3, cell, externalUse, port)).ToArray();
    }

    // Each row samples the current gap. A missing or ambiguous observation
    // cannot enable a move using the control bit left by an earlier beat.
    public void Settle()
    {
        foreach (var row in fillRows)
            if (reads.TryCapture(gapTable, gapRow, owner, mirror))
                fills.TryStep(row, fills.Observe(row), out _);
    }

    public void TakeOne()
    {
        foreach (var row in useRows)
            uses.TryStep(row, uses.Observe(row), out _);
    }

    public MaintenanceRead Read() => new(target.ReadSupport(),
        reads.Predict(gapTable, gapRow), (byte)(owner.PackedBytes(0, 0, 1)[0] & 31));

    public byte[] FillRows() => fills.PackedRows(3);
    public byte[] UseRows() => uses.PackedRows(3);
    public byte[] GapRow() => reads.PackedRows(gapTable, 3);
}

// Bits 0,1: stock; bit 2: the cell; bits 3,4: consumed stock retained at outlets.
public readonly record struct MaintenanceRead(byte Target, byte Gap, byte Material);
