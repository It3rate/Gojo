namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One ordinal adjustment rung attached to a retained family. Its
/// field bindings, pins and passage rows own the response, without shape names
/// or a geometric basis. Every real field has its own local zero/rest/farther
/// rail. Shared uses of a field read one dial, never copied request material.
/// Initial reserve is one supplied request; later changes use passage laws.
/// The source's participating V bits remain unchanged throughout this trial.</summary>
public sealed class FamilyAdjustment
{
    private readonly PortStructure owner = new();
    private readonly EndpointRows fields = new(), pins = new();
    private readonly LandmarkRun[] rails;
    private readonly int[] dials, scope;
    private readonly PassageConnections offer, settle;
    private readonly ConnectionRow offerRow;
    private readonly ConnectionRow? settleRow;
    private readonly int reserve, waiting, launch, free, locked;
    private readonly List<FamilyAdjustmentStep> trace = [];

    public FamilyTable Table { get; }
    public FamilyEntry Entry { get; }
    public FamilyUseTable Uses { get; }
    public int? SelectedField { get; }
    public int ReferenceBits { get; }
    public int FieldCount => dials.Length;
    public int LogicalSiteCount => 5 + FieldCount;
    public int PinCount => pins.Count(3, ReferenceBits);
    public bool Waiting => owner.ReadPort(waiting) == 1;
    public bool ReservePresent => owner.ReadPort(reserve) == 1;
    public bool HasResponse => settleRow.HasValue;
    public IReadOnlyList<FamilyAdjustmentStep> Trace => trace.Select(Copy).ToArray();
    public int[] ScopePorts => scope.ToArray();

    public FamilyAdjustment(FamilyTable table, FamilyEntry entry, FamilyUseTable uses,
        int? selectedField, IReadOnlyCollection<int>? pinnedFields = null)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(uses);
        if (!ReferenceEquals(table.Template, uses.Template))
            throw new ArgumentException("Uses must belong to this exact family template.", nameof(uses));
        var count = table.Template.FieldCount;
        if (selectedField is int selected && (selected < 0 || selected >= count))
            throw new ArgumentOutOfRangeException(nameof(selectedField));
        var fixedFields = pinnedFields?.Distinct().ToArray() ?? [];
        if (fixedFields.Any(field => field < 0 || field >= count))
            throw new ArgumentOutOfRangeException(nameof(pinnedFields));
        ReferenceBits = OrderedBoundary.Bits(checked(count + 5));
        if (ReferenceBits > 16) throw new ArgumentException("Adjustment ports exceed the local reference grammar.");
        for (var field = 0; field < count; field++)
            if (table.ReadCoarse(entry, field) != 1)
                throw new ArgumentException("This adjustment rung requires participating positive-right fields.", nameof(entry));
        Table = table; Entry = entry; Uses = uses; SelectedField = selectedField;

        reserve = Site(true); waiting = Site(false);
        launch = Site(false); free = Site(false); locked = Site(true);
        dials = Enumerable.Range(0, count).Select(_ => Site(false)).ToArray();
        rails = Enumerable.Range(0, count).Select(_ => new LandmarkRun(2, [0, 1, 2])).ToArray();
        for (var field = 0; field < count; field++) fields.Append(ReferenceBits, field, dials[field], 1, 2);
        foreach (var field in fixedFields) pins.Append(ReferenceBits, field, dials[field], locked);

        offer = new(owner, PassageLaw.Unconditional());
        settle = new(owner, PassageLaw.Controlled(whenPresent: false));
        offerRow = offer.Connect(ReferenceBits, reserve, launch, waiting);
        if (selectedField is int chosen)
        {
            var pin = PinRows().FirstOrDefault(row => row[0] == chosen);
            var control = pin is null ? free : pin[2];
            settleRow = settle.Connect(ReferenceBits, waiting, control, dials[chosen]);
            // A separate launch guard allows a known offer without consulting
            // the optionally concealed response control.
            scope = [reserve, waiting, launch, control, dials[chosen]];
        }
        else scope = [reserve, waiting, launch];
    }

    private int Site(bool present) => owner.Publish(owner.Create(0, 0, 1, present ? 1UL : 0UL));

    /// <summary>Observe, then predict the two existing rows in order. Hiding
    /// control preserves both whole-scope alternatives; no concealed control
    /// is read. An absent response row does not invent a target or a refusal.</summary>
    public FamilyAdjustmentPrediction Predict(bool concealControl = false)
    {
        var visible = (1 << scope.Length) - 1;
        if (concealControl && HasResponse) visible &= ~(1 << 3);
        var before = offer.ObserveScope(scope, visible);
        var afterOffer = offer.PredictScope(offerRow, scope, before);
        var settlement = settleRow is { } row ? settle.PredictScope(row, scope, afterOffer) : afterOffer;
        var after = concealControl && HasResponse ? afterOffer : settlement;
        return new(before, after, settlement, scope.ToArray(), !HasResponse);
    }

    /// <summary>Offer the existing request, then attempt its response. An open
    /// or concealed response leaves the request waiting. Repeating this method
    /// cannot replenish reserve; revealing control can retry the same held
    /// request. The returned witness is observational and cannot drive a step.</summary>
    public FamilyAdjustmentStep Push(bool concealControl = false)
    {
        var prediction = Predict(concealControl);
        var beforeTips = Tips();
        var offerBefore = offer.Observe(offerRow);
        var offered = offer.TryStep(offerRow, offerBefore, out var offerAfter);
        byte? responseBefore = null, responseAfter = null;
        var responseCommitted = false;
        if (settleRow is { } row)
        {
            responseBefore = settle.Observe(row, concealControl ? 5 : 7);
            responseCommitted = settle.TryStep(row, responseBefore.Value, out var settled);
            if (responseCommitted) responseAfter = settled;
        }
        var visible = (1 << scope.Length) - 1;
        if (concealControl && HasResponse) visible &= ~(1 << 3);
        var actual = offer.ObserveScope(scope, visible);
        var witness = new FamilyAdjustmentStep(prediction, offerBefore, offered ? offerAfter : null,
            responseBefore, responseAfter, offered, responseCommitted, Waiting, beforeTips, Tips(), actual);
        trace.Add(Copy(witness));
        return witness;
    }

    private static FamilyAdjustmentStep Copy(FamilyAdjustmentStep step) => step with
    {
        Prediction = step.Prediction with { ScopePorts = step.Prediction.ScopePorts.ToArray() },
        BeforeTips = step.BeforeTips.ToArray(),
        AfterTips = step.AfterTips.ToArray()
    };

    // The same ordinal label in two different field rails does not establish
    // equal extent. The field and rail supply the scope of every tip read.
    public LandmarkRun Rail(int field) => rails[field];
    public int Tip(int field)
    {
        var row = fields.Read(new(4, ReferenceBits, field));
        return row[2 + owner.ReadPort(row[1])];
    }
    public int[] Tips() => Enumerable.Range(0, FieldCount).Select(Tip).ToArray();
    public IPrimitiveReadPort DialPort(int field) => owner.Connect(dials[field]);
    public IPrimitiveReadPort WaitingPort => owner.Connect(waiting);
    public IPrimitiveReadPort ReservePort => owner.Connect(reserve);

    /// <summary>Derived reads of currently extended field roles. Repeated
    /// appearances do not increase the number of occupied runtime dials.</summary>
    public int[] AffectedFields() => Enumerable.Range(0, FieldCount).Where(field => owner.ReadPort(dials[field]) == 1).ToArray();
    public int[] AffectedLinks()
    {
        var extended = AffectedFields().ToHashSet();
        return Enumerable.Range(0, Table.Template.LinkCount).Where(link => extended.Contains(Table.Template.Link(link).Field)).ToArray();
    }
    public int[] AffectedGroups()
    {
        var extended = AffectedFields().ToHashSet();
        return Enumerable.Range(0, Uses.GroupCount).Where(group => Uses.ReadGroup(Table, Entry, group)
            .Any(use => extended.Contains(use.Span.Value.Field))).ToArray();
    }

    public int[][] FieldRows() => Enumerable.Range(0, FieldCount).Select(i => fields.Read(new(4, ReferenceBits, i))).ToArray();
    public int[][] PinRows() => Enumerable.Range(0, PinCount).Select(i => pins.Read(new(3, ReferenceBits, i))).ToArray();
    public int[] OfferRow() => offer.Wiring(offerRow);
    public int[][] SettleRows() => settleRow is { } row ? [settle.Wiring(row)] : [];
    public byte[] FieldBytes() => fields.PackedBytes(4, ReferenceBits);
    public byte[] PinBytes() => pins.PackedBytes(3, ReferenceBits);
    public byte[] OfferBytes() => offer.PackedRows(ReferenceBits);
    public byte[] SettleBytes() => settle.PackedRows(ReferenceBits);
    public byte[] OfferLaw() => offer.LawBytes();
    public byte[] SettleLaw() => settle.LawBytes();
    public byte[] StoreBytes() => owner.PackedBytes(0, 0, 1);
}

/// <summary>Bits name complete scope states, not magnitudes. With a bound
/// response the scope is [reserve,waiting,launch,control,dial]; an open response
/// exposes only [reserve,waiting,launch]. AfterSupport predicts Push's actual
/// conservative execution policy, including holding when control is concealed.
/// SettlementSupport separately predicts the connected laws' possible results
/// if the response can execute after control is resolved. Prediction is no action.</summary>
public sealed record FamilyAdjustmentPrediction(uint BeforeSupport, uint AfterSupport, uint SettlementSupport,
    int[] ScopePorts, bool OpenResponse);

/// <summary>Read-only trial evidence. A null response state means execution
/// did not resolve, not that the dial settled to zero. ActualScopeSupport is
/// observed using the same visibility as the prediction.</summary>
public sealed record FamilyAdjustmentStep(FamilyAdjustmentPrediction Prediction,
    byte OfferBeforeSupport, byte? OfferAfterState, byte? SettleBeforeSupport, byte? SettleAfterState,
    bool OfferCommitted, bool SettlementCommitted, bool Waiting, int[] BeforeTips, int[] AfterTips,
    uint ActualScopeSupport)
{
    public bool Moved => !BeforeTips.SequenceEqual(AfterTips);
}
