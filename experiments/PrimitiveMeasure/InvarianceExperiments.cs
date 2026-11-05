namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Editorial descriptions of apparatus, never runtime operation tags.</summary>
public sealed record InvarianceCase(string Name, string Concept, string Limit,
    string PreservingChange, string ChallengingChange);

/// <summary>An observer projection of a connected comparison. The probe and its
/// captured source ports own the read; sentences and diagrams do not drive it.</summary>
public sealed record InvarianceResult(CorrespondenceProbe Probe, byte BeforeSupport,
    byte AfterSupport, string Change, string Observation, byte[] BeforeBytes,
    byte[] AfterBytes, string[] Details, int[][] BeforeLinks, int[][] AfterLinks)
{
    public byte VerdictSupport => Probe.ReadSupport();
    public IPrimitiveReadPort Port => Probe;
    public byte[] LawBytes => Probe.LawBytes();
    public byte[] RowBytes => Probe.RowBytes();
    public int[] Wiring => Probe.Wiring();
    public int ReferenceBits => Probe.ReferenceBits;
    /// <summary>Which second codes would satisfy the retained correspondence
    /// for the admitted first codes. This prediction does not select an actual
    /// outcome or supply missing correlation between independent observations.</summary>
    public byte ExpectedAfterSupport
    {
        get
        {
            byte support = 0;
            foreach (var first in ReadSupport.Words(BeforeSupport))
            for (byte second = 0; second < 8; second++)
                if (Probe.Law.Allows(first, second, 1)) support |= ReadSupport.Of(second);
            return support;
        }
    }
    public RetainedPassagePattern? RetainedPattern { get; init; }
    public int[] PatternRolePorts { get; init; } = [];
}

/// <summary>Explicit experiment builders over existing local mechanisms. The
/// catalogue selects apparatus, not an interpreter for concept words. Every
/// result is read by the same packed correspondence law with exposed inputs.
/// Structural edits, observational transforms and material steps stay distinct.</summary>
public static class InvarianceExperiments
{
    public static IReadOnlyList<InvarianceCase> Cases { get; } = Array.AsReadOnly<InvarianceCase>([
        new("Object · one source", "One connected object can persist while its state changes.",
            "Adjusting this object does not discover or create a new object.",
            "Send one adjustment through the shared source.", "Replace the shared source with separate sources."),
        new("Binding · several uses", "Several uses follow one source through a change.",
            "Matching independent readings do not establish shared extent or common calibration.",
            "Adjust the source observed by both uses.", "Adjust only one of two independent sources."),
        new("Sequence · retained order", "Participants can retain their order across different encodings.",
            "Order supplies neither distances nor durations.",
            "Express the same chain through explicit precedence relations.", "Exchange the middle landmarks."),
        new("Loop · retained return", "Following the connected order returns to the starting site.",
            "Return alone supplies neither enclosed area nor active circulation.",
            "Change participation without deleting an endpoint connection.", "Expand the cycle and detach its final arrival."),
        new("Path · reversed reading", "A whole path can be read backwards with its connections intact.",
            "Reversing one part alone does not guarantee a continuous path.",
            "Reverse both occurrence order and every use.", "Reverse only the first use."),
        new("Switch · Y and T", "A control selects a route through different joint constructions.",
            "Unknown control cannot select an outlet; a similar drawing need not share the law.",
            "Map the Y switch's three roles to a T switch.", "Replace the T switch with a straight-through passage."),
        new("Collection · either input", "Either contributor can make a common read participate.",
            "A common presence read does not merge or duplicate the material.",
            "Exchange the two contributors to a collecting Y.", "Remove the last participating contributor."),
        new("Priority · limits of exchange", "A single arrival can use either inlet of a merge.",
            "Simultaneous arrivals expose row priority even when final outlet presence agrees.",
            "Exchange the only arriving contributor.", "Exchange two simultaneous contributors without exchanging priority."),
        new("Perspective · reorientation", "Changing the frame changes the answer in a predictable way.",
            "Matching possible-answer sets does not establish the same unknown actual outcome.",
            "Reorient both operands of an addition.", "Reverse direction while claiming to have reoriented the frame."),
        new("Mapping · opposite sides", "Opposite uses of shared extents preserve a rectangle construction.",
            "Removing the binding loses the guarantee; it does not prove the result is not rectangular.",
            "Adjust a shared extent while retaining opposite-side bindings.", "Replace opposite-side sharing with independent extents."),
        new("Overlap · shared portions", "Exchanging two spans preserves their shared occupied portions.",
            "Overlap needs a common ordered basis and specifies no measured size.",
            "Exchange the operands of intersection on one rail.", "Reorder landmarks so the spans separate."),
        new("Joint · common site", "Several routes meet through one shared site.",
            "A shared site does not specify branching, collection or a turn angle.",
            "Retarget the common arrival in every attached route.", "Retarget just one arrival away from the common site.")
    ]);

    // One immutable law is shared by different apparatus and probe owners.
    // No concept label is needed to select an outcome from these tuple bits.
    public static BitRelation Equality { get; } = CorrespondenceProbe.EqualWords();
    public static BitRelation Reorientation { get; } = CorrespondenceProbe.Through(
        Enumerable.Range(0, 8).Select(i => ThreeBitMeasure.Reorient((byte)i)).ToArray());
    public static BitRelation YToT { get; } = CorrespondenceProbe.Through(
        Enumerable.Range(0, 8).Select(i => (byte)(((i << 1) & 7) | (i >> 2))).ToArray());
    public static BitRelation ExchangeInputs { get; } = CorrespondenceProbe.Through(
        Enumerable.Range(0, 8).Select(i => (byte)((i & 4) | ((i & 1) << 1) | ((i & 2) >> 1))).ToArray());

    public static InvarianceResult Run(int caseIndex, int variation)
    {
        if ((uint)caseIndex >= Cases.Count || (uint)variation > 2)
            throw new ArgumentOutOfRangeException(nameof(caseIndex));
        return caseIndex switch
        {
            0 => Object(variation), 1 => Binding(variation), 2 => Sequence(variation),
            3 => Loop(variation), 4 => Path(variation), 5 => Switch(variation),
            6 => Collection(variation), 7 => Priority(variation), 8 => Perspective(variation),
            9 => Mapping(variation), 10 => Overlap(variation), _ => Joint(variation)
        };
    }

    // These adapters inspect their captured connected source. They never
    // select a result from the catalogue name, a shape tag or a screen position.
    private sealed class View(Func<byte> read) : IPrimitiveReadPort
    {
        public byte ReadSupport() => read();
    }
    private static byte Truth(bool value) => value ? (byte)2 : (byte)1;
    private static CapturedRead Capture(Func<byte> read) => CapturedRead.Capture(new View(read));
    private static CapturedRead Hidden() => Capture(() => 3);
    private static InvarianceResult Finish(int which, int variation, IPrimitiveReadPort before,
        IPrimitiveReadPort after, byte[] beforeBytes, byte[] afterBytes, string observation,
        string[] details, int[][]? beforeLinks = null, int[][]? afterLinks = null, BitRelation? law = null)
    {
        var left = CapturedRead.Capture(before);
        var right = CapturedRead.Capture(after);
        var probe = new CorrespondenceProbe(law ?? Equality, left, right);
        var description = Cases[which];
        return new(probe, left.ReadSupport(), right.ReadSupport(), variation == 2
            ? "Withhold the selected observation or correspondence." : variation == 0
                ? description.PreservingChange : description.ChallengingChange,
            observation, beforeBytes, afterBytes, details, beforeLinks ?? [], afterLinks ?? []);
    }
    private static int[][] Links(SharedVariation source) => Enumerable.Range(0, source.Table.Template.LinkCount)
        .Select(i => { var link = source.Table.Template.Link(i); return new[] { link.Start, link.End }; }).ToArray();
    private static int[][] RouteLinks(StructuralRoute route) => Enumerable.Range(0, route.StepCount)
        .Select(i => { var rows = route.TopologyRows(); return route.Layout == 0 ? rows[i] :
            new[] { rows[i][0], rows[(i + 1) % rows.Length][0] }; }).ToArray();

    private static InvarianceResult Object(int variation)
    {
        var source = SharedVariation.Reaches(true);
        var beforeBytes = source.SourceBytes().Concat(source.StoreBytes()).ToArray();
        var before = Capture(() => Truth(source.SharesSource(0, 3)));
        source.Push();
        var target = variation == 1 ? SharedVariation.Reaches(false) : source;
        if (variation == 1) target.Push();
        var after = variation == 2 ? Hidden() : Capture(() => Truth(target.SharesSource(0, 3)));
        return Finish(0, variation, before, after, beforeBytes,
            target.SourceBytes().Concat(target.StoreBytes()).ToArray(),
            variation == 1 ? "The selected replacement no longer has one source for all occurrences." :
                "The adjustment changes a local ordinal dial; the retained binding is a separate read.",
            [$"Original source tips after adjustment: {string.Join(",", source.Adjustment.Tips())}.",
             $"Target fields: {target.Table.Template.FieldCount}; uses: {target.Uses.UseCount}.",
             "The source object persists in the parameter trial; replacement is an explicit structural comparison."],
            Links(source), Links(target));
    }

    private static InvarianceResult Binding(int variation)
    {
        var source = SharedVariation.Reaches(variation != 1);
        var first = source.Adjustment.DialPort(source.Read(0).Field);
        var second = source.Adjustment.DialPort(source.Read(1).Field);
        var comparison = new CorrespondenceProbe(Equality, first, second);
        var before = CapturedRead.Capture(comparison);
        var beforeBytes = source.StoreBytes();
        source.Push();
        var after = variation == 2 ? Hidden() : CapturedRead.Capture(comparison);
        return Finish(1, variation, before, after, beforeBytes, source.StoreBytes(),
            "The inner read compares dial states; the outer read checks whether that agreement survived adjustment.",
            [$"Binding rows: {string.Join("; ", source.BindingRows().Select(r => string.Join(",", r)))}.",
             $"Uses changed by the actual request: {string.Join(",", source.ChangedUses())}.",
             "Matching dial positions on independent rails do not establish equal physical lengths."],
            Links(source), Links(source));
    }

    private static InvarianceResult Sequence(int variation)
    {
        var first = new LandmarkRun(2, [0, 1, 2, 3]);
        var second = variation == 1 ? new LandmarkRun(2, [0, 2, 1, 3]) :
            new LandmarkRun(2, [0, 1, 2, 3], variation == 2 ? [[0, 1], [2, 3]] : [[0, 1], [1, 2], [2, 3]]);
        byte Read(LandmarkRun rail) => rail.Compare(1, 2) switch
        { LandmarkRead.Before => 2, LandmarkRead.After or LandmarkRead.SameSite => 1, _ => 3 };
        return Finish(2, variation, Capture(() => Read(first)), Capture(() => Read(second)),
            first.SiteBytes(), second.SiteBytes().Concat(second.ClaimBytes()).ToArray(),
            "The question is whether landmark 1 precedes landmark 2; array order and explicit claims supply different evidence.",
            [$"First order: {string.Join(" -> ", first.Sites())}.",
             second.IsChain ? $"Second order: {string.Join(" -> ", second.Sites())}." :
                $"Second claims: {string.Join("; ", second.Claims().Select(r => $"{r[0]} -> {r[1]}"))}.",
             "The member array in a partial layout supplies membership only. Unrelated claims do not fill the missing order."],
            [[0, 1], [1, 2], [2, 3]], second.IsChain ? [[0, 2], [2, 1], [1, 3]] : second.Claims());
    }

    private static InvarianceResult Loop(int variation)
    {
        var route = StructuralRoute.Cycle(5, [0, 1, 2, 3]);
        route.Inspect(int.MaxValue);
        var before = CapturedRead.Capture(route.Closure);
        var beforeBytes = route.Encode();
        var beforeLinks = RouteLinks(route);
        if (variation == 1) { route.Unpack(); route.TryRetargetEndpoint(3, 1, 4); route.Inspect(int.MaxValue); }
        else route.SetWord(0, 0);
        var after = variation == 2 ? Hidden() : CapturedRead.Capture(route.Closure);
        return Finish(3, variation, before, after, beforeBytes, route.Encode(),
            "The reader asks about retained endpoint return, not whether every link is currently participating.",
            [$"First participation is now {route.Word(0) & 1}.",
             "A structural split changes a connection; setting V to zero does not delete it.",
             "Concealment withholds the closure observation, not a value inside the route."], beforeLinks, RouteLinks(route));
    }

    private static InvarianceResult Path(int variation)
    {
        var source = SharedVariation.Reaches(true);
        var uses = source.Uses;
        var changed = new FamilyUseTable(source.Table.Template, variation == 1 ? [1, 2, 4, 6] : [7, 5, 3, 1]);
        var before = Capture(() => Truth(uses.IsClosed(source.Table, source.Entry, 0)));
        var after = variation == 2 ? Hidden() : Capture(() => Truth(changed.IsClosed(source.Table, source.Entry, 0)));
        int[][] Ends(FamilyUseTable view) => Enumerable.Range(0, view.UseCount).Select(i =>
        {
            var use = view.SignedUse(i); var link = source.Table.Template.Link(use >> 1);
            return (use & 1) == 0 ? new[] { link.Start, link.End } : [link.End, link.Start];
        }).ToArray();
        return Finish(4, variation, before, after, uses.UseBytes(), changed.UseBytes(),
            "Signed uses select a traversal over the same retained links; source ODV words are untouched.",
            [$"Uses before: {string.Join(",", Enumerable.Range(0, 4).Select(uses.SignedUse))}.",
             $"Uses after: {string.Join(",", Enumerable.Range(0, 4).Select(changed.SignedUse))}.",
             "Closure is invariant under a full reverse read; the ordered endpoints transform."], Ends(uses), Ends(changed));
    }

    private static InvarianceResult Switch(int variation)
    {
        // All three material roles participate in the correspondence. The
        // control role remains separate and is not counted as transported flow.
        var y = GlyphJunction.YBranch(12);
        var t = GlyphJunction.TBar(9, optionalTurn: variation != 1);
        // Retain the complete Y passage structure, independent of its builder
        // and values. The reloaded pattern binds to different local arm roles.
        var retained = RetainedPassagePattern.Decode(RetainedPassagePattern.Capture(y.Flow).Encode());
        var applied = variation == 1 ? RetainedPassagePattern.Capture(t.Flow) : retained;
        var bound = applied.Bind(t.Flow.Owner, variation == 1 ? t.Flow.Ports :
            [t.Flow.Ports[1], t.Flow.Ports[2], t.Flow.Ports[0], t.Flow.Ports[3]]);
        y.Advance();
        byte actualSupport;
        if (variation == 2)
        {
            var possibilities = bound.Predict(bound.Observe(7));
            actualSupport = 0;
            foreach (var word in ReadSupport.Words(ProjectLowThree(possibilities)))
                actualSupport |= ReadSupport.Of((byte)(((word << 1) & 7) | (word >> 2)));
            bound.TryStep(bound.Observe(7), out _); // unresolved: retains actual source
        }
        else { bound.TryStep(bound.Observe(), out _); actualSupport = ReadSupport.Of((byte)(t.Flow.Stored & 7)); }
        var before = Capture(() => ReadSupport.Of((byte)(y.Flow.Stored & 7)));
        var after = Capture(() => actualSupport);
        return Finish(5, variation, before, after, [y.Flow.Stored, .. y.ArmBytes(), .. retained.Encode()],
            [t.Flow.Stored, .. t.ArmBytes(), .. applied.Encode()],
            "This is a comparison of two constructed switches after a pulse, under the explicit arm map Y(0,1,2) -> T(1,2,0).",
            [$"Y material bits: {y.Flow.Stored & 7}; T actual material bits: {t.Flow.Stored & 7}.",
             variation == 2 ? "T control is hidden: predicted outlets remain alternatives; the actual activation stays at its source." :
                "Each joint retains exactly one material participant. Control occupancy is separate.",
             $"Retained Y pattern: {retained.Encode().Length} bytes, {retained.StepCount} ordered passages; bound roles [{string.Join(",", bound.RolePorts())}].",
             "The reloaded pattern runs directly through its law bits. Its source values and concept label are absent."],
            Enumerable.Range(0, 3).Select(y.Arrival).ToArray(), Enumerable.Range(0, 3).Select(t.Arrival).ToArray(), YToT)
            with { RetainedPattern = applied, PatternRolePorts = bound.RolePorts() };
    }

    private static byte ProjectLowThree(uint support)
    {
        byte result = 0;
        for (var state = 0; state < 32; state++) if ((support & (1u << state)) != 0) result |= ReadSupport.Of((byte)(state & 7));
        return result;
    }

    private static InvarianceResult Collection(int variation)
    {
        var first = GlyphJunction.YCollect(1);
        var second = GlyphJunction.YCollect(variation == 1 ? (byte)0 : (byte)2);
        first.Advance(); second.Advance();
        return Finish(6, variation, CapturedRead.Capture(first.Stem), variation == 2 ? Hidden() : CapturedRead.Capture(second.Stem),
            [first.Flow.Stored, .. first.OutletBytes()], [second.Flow.Stored, .. second.OutletBytes()],
            "Two separate receiving sites feed a shared OR observation. Exchanging the source changes its physical destination, while presence agrees.",
            [$"Actual first store: {first.Flow.Stored}; second store: {second.Flow.Stored}.",
             "The read is non-draining. Both contributors would remain two participants when both are present.",
             "Removing the last participant is a known failure of presence preservation, not uncertainty."],
            [[0, 2], [1, 4]], [[0, 2], [1, 4]]);
    }

    private static InvarianceResult Priority(int variation)
    {
        var first = GlyphJunction.YMerge(variation == 0 ? (byte)1 : (byte)3);
        var second = GlyphJunction.YMerge(variation == 0 ? (byte)2 : (byte)3);
        first.Advance(); second.Advance();
        var before = Capture(() => ReadSupport.Of((byte)(first.Flow.Stored & 7)));
        var after = Capture(() => variation == 2 ? (byte)((1 << 5) | (1 << 6)) : ReadSupport.Of((byte)(second.Flow.Stored & 7)));
        return Finish(7, variation, before, after, [first.Flow.Stored], [second.Flow.Stored],
            "Exchange the two contributor roles but retain the serial passage order. Compare every material role, not only the occupied outlet.",
            [$"First material pattern: {first.Flow.Stored & 7}; second: {second.Flow.Stored & 7}.",
             $"Both outlet reads: {first.Stem.ReadSupport()} and {second.Stem.ReadSupport()}.",
             "With two arrivals, the first row wins the available receiver. Coarse agreement hides a different expected remaining contributor."],
            [[0, 2], [1, 2]], [[0, 2], [1, 2]], ExchangeInputs);
    }

    private static InvarianceResult Perspective(int variation)
    {
        byte a = 1, b = variation == 2 ? (byte)3 : (byte)1;
        var first = ThreeBitMeasure.Add(a, b);
        byte Transform(byte word) => variation == 1 ? ThreeBitMeasure.ReverseDirection(word) : ThreeBitMeasure.Reorient(word);
        var second = ThreeBitMeasure.Add(Transform(a), Transform(b));
        byte Support(IEnumerable<byte> words) => words.Aggregate((byte)0, (acc, word) => (byte)(acc | ReadSupport.Of(word)));
        var expectedSet = first.Select(ThreeBitMeasure.Reorient).Order().SequenceEqual(second.Order());
        return Finish(8, variation, Capture(() => Support(first)), Capture(() => Support(second)),
            [a, b, Support(first)], [Transform(a), Transform(b), Support(second)],
            "The law compares actual candidate words after an explicit frame transform; a separate diagnostic compares complete predicted sets.",
            [$"Complete support-set correspondence: {expectedSet}.",
             "In the uncertain case the sets correspond exactly, but separate unknown outcomes are not thereby the same event.",
             "Reorientation and direction reversal are distinct operations; no implicit common frame is invented."], law: Reorientation);
    }

    private static InvarianceResult Mapping(int variation)
    {
        var table = new FamilyTable(new([new(0, 0), new(0, 0)],
            [new(0, 1, 0), new(1, 2, 1), new(2, 3, 0), new(3, 0, 1)]));
        var entry = table.Append(1, 1, 1);
        var uses = new FamilyUseTable(table.Template, [0, 2, 4, 6]);
        var adjustment = new FamilyAdjustment(table, entry, uses, 0);
        OrthogonalPathFixture Build(bool detached, bool concealed)
        {
            // The detached construction owns four genuinely separate sources,
            // not four wrappers that still reach the original two fields.
            var independent = new PortStructure();
            var ports = detached
                ? Enumerable.Range(0, 4).Select(_ => (IPrimitiveReadPort)independent.Connect(
                    independent.Publish(independent.Create(0, 0, 1, 1)))).ToArray()
                : Enumerable.Range(0, 2).Select(i => (IPrimitiveReadPort)new View(() =>
                    concealed && i == 0 ? (byte)3 : ReadSupport.Of(table.ReadCoarse(entry, i)))).ToArray();
            var shape = new OrthogonalPathFixture(ports);
            for (var lane = 0; lane < 4; lane++) shape.Connect(lane, lane, (lane + 1) % 4, 4 + (detached ? lane : lane % 2));
            return shape;
        }
        var first = Build(false, false);
        var before = Capture(() => first.Read().NondegeneracySupport ?? 3);
        var beforeBytes = Enumerable.Range(0, 4).SelectMany(first.PackedLane).Concat(adjustment.StoreBytes()).ToArray();
        adjustment.Push();
        var second = Build(variation == 1, variation == 2);
        var after = Capture(() => second.Read().NondegeneracySupport ?? 3);
        return Finish(9, variation, before, after, beforeBytes,
            Enumerable.Range(0, 4).SelectMany(second.PackedLane).Concat(adjustment.StoreBytes()).ToArray(),
            "Four perpendicular straight-edge lanes are an explicit geometric premise. Shared opposite extents establish closure for any positive A and B.",
            [$"Opposite references retained: {second.Read().SharedOppositeReaches}.",
             $"Actual ordinal tips after the adjustment: {string.Join(",", adjustment.Tips())}.",
             "Absent correspondence yields an unresolved rectangle read, not a declaration that the boundary is nonrectangular."],
            Enumerable.Range(0, 4).Select(i => first.Wiring(i)[..2]).ToArray(),
            Enumerable.Range(0, 4).Select(i => second.Wiring(i)[..2]).ToArray());
    }

    private static InvarianceResult Overlap(int variation)
    {
        var rail = new LandmarkRun(2, [0, 1, 2, 3]);
        var a = OrdinalInk.Closed(rail, 0, 2); var b = OrdinalInk.Closed(rail, 1, 3);
        var first = a.Combine(b, 8)!;
        var target = variation == 1 ? new LandmarkRun(2, [0, 2, 1, 3]) : rail;
        var c = OrdinalInk.Closed(target, 1, 3);
        var d = OrdinalInk.Closed(variation == 2 ? new LandmarkRun(2, [0, 1, 2, 3]) : target, 0, 2);
        var second = c.Combine(d, 8);
        return Finish(10, variation, Capture(() => Truth(first.ContainsInterval)),
            Capture(() => second is null ? (byte)0 : Truth(second.ContainsInterval)),
            [(byte)a.Bits, (byte)b.Bits, (byte)first.Bits], [(byte)c.Bits, (byte)d.Bits, (byte)(second?.Bits ?? 0)],
            second is null ? "The two latter spans have no common rail correspondence. There is no admitted combined read; zero padding in the byte chart is not a measured empty overlap." :
                "Intersection is calculated over occupied landmarks and gaps. No screen distance enters the read.",
            [$"First occupied intersection bits: {first.Bits}.",
             second is null ? "Second intersection: not represented in a common basis." : $"Second occupied intersection bits: {second.Bits}.",
             "The preserving trial exchanges operands; arbitrary spacing realizations are outside this metric-free encoding."],
            [[0, 2], [1, 3]], [[1, 3], [0, 2]]);
    }

    private static InvarianceResult Joint(int variation)
    {
        var route = StructuralRoute.Pairs(5, [[0, 3], [1, 3], [2, 3]]);
        byte CommonArrival() => Truth(route.TopologyRows().Select(row => row[1]).Distinct().Count() == 1);
        var before = Capture(CommonArrival);
        var beforeBytes = route.Encode(); var beforeLinks = route.TopologyRows();
        for (var row = 0; row < (variation == 1 ? 1 : 3); row++) route.TryRetargetEndpoint(row, 1, 4);
        return Finish(11, variation, before, variation == 2 ? Hidden() : Capture(CommonArrival), beforeBytes, route.Encode(),
            "The selected read checks whether all arrivals name one actual site within this owner. It does not interpret a Y or T drawing.",
            ["Retargeting each incidence is an explicit structural operation; intermediate partial edits need not preserve the joint.",
             "The endpoint references carry meeting. Passage law, angle and source values are separate.",
             "Moving just one arrival is a concrete counterexample to a preserved common joint."], beforeLinks, route.TopologyRows());
    }
}
