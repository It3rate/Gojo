using Gojo.Experiments.PrimitiveMeasure;

if (args is ["--tension-choice", var choiceFolder])
{
    TensionChoiceSurvey.Export(choiceFolder);
    return;
}

if (args is ["--goal-flow", var goalFolder])
{
    GoalFlowSurvey.Export(goalFolder);
    return;
}

if (args is ["--race-order", var raceFolder])
{
    RaceSurvey.Export(raceFolder);
    return;
}

if (args is ["--invariance", var invarianceFolder])
{
    InvarianceSurvey.Export(invarianceFolder);
    return;
}

if (args is ["--structural-guarantees", var guaranteeFolder])
{
    GuaranteeSurvey.Export(guaranteeFolder);
    return;
}

if (args is ["--construction-behavior", var behaviorFolder])
{
    BehaviorSurvey.Export(behaviorFolder);
    return;
}

if (args is ["--construction-hints", var constructionFolder])
{
    ConstructionSurvey.Export(constructionFolder);
    return;
}
if (args is ["--families", var familyFolder])
{
    FamilySurvey.Export(familyFolder);
    return;
}
if (args is ["--strips",var stripFolder])
{
    StripSurvey.Export(stripFolder);
    return;
}
if (args is ["--whitespace",var gapFolder])
{
    GapSurvey.Export(gapFolder);
    return;
}
if (args is ["--drawing-hints",var hintFolder])
{
    HintSurvey.Export(hintFolder);
    return;
}
if (args is ["--ornament-plates",var folder])
{
    Directory.CreateDirectory(folder);
    for (int i=0;i<OrnamentPlates.All.Count;i++)
    {
        var print=new OrnamentImpression(OrnamentPlates.All[i],0,14);
        File.WriteAllText(Path.Combine(folder,$"plate-{i+1}.svg"),print.Svg());
        Console.WriteLine($"{print.Design.Name}: {print.Cells.Length} repeats, {print.Draw(print.Passes).Paths.Sum(p=>p.Strokes)} strokes.");
    }
    var paired=new OrnamentImpression(OrnamentPlates.PairedKey,0,14);
    File.WriteAllText(Path.Combine(folder,"paired-hooks.svg"),paired.Svg());
    using var survey=File.CreateText(Path.Combine(folder,"press-runs.csv"));
    survey.WriteLine("plate,rhythm,join_truth,outer_cells,finished_cells,outer_finials,child_sectors,passes,strokes");
    foreach(var plate in OrnamentPlates.All.Append(OrnamentPlates.PairedKey))
    for(int rhythm=0;rhythm<11;rhythm++)
    foreach(byte law in new byte[]{14,8,6})
    {
        var proof=new OrnamentImpression(plate,rhythm,law);
        int nested=proof.Timeline.Count(pass=>pass.ChildCell>=0 && pass.Stage==0);
        survey.WriteLine($"{plate.Name},{rhythm},{law},{proof.Cells.Length},{proof.Cells.Count(c=>c.Finished)},{proof.Cells.Count(c=>c.Finial)},{nested},{proof.Passes},{proof.Draw(proof.Passes).Paths.Sum(p=>p.Strokes)}");
    }
    return;
}
if (args.Contains("--timing-sockets"))
{
    SocketSurvey.Trials(Console.Out,args.Contains("--rows"));
    return;
}
if (args.Contains("--washer-probe-witnesses"))
{
    WasherProbeAudit.Witnesses(Console.Out);
    return;
}
if (args.Contains("--washer-probes"))
{
    WasherProbeAudit.Trials(Console.Out,args.Contains("--rows"));
    return;
}
if (args.Contains("--washer-counter"))
{
    WasherSurvey.Trials(Console.Out,args.Contains("--rows"));
    return;
}
if (args.Contains("--border-scores") || args.Contains("--border-windows"))
{
    if (args.Contains("--border-scores")) BorderSurvey.Scores(Console.Out,args.Contains("--rows"));
    else BorderSurvey.Windows(Console.Out,args.Contains("--rows"));
    return;
}
if (args.Contains("--bowl-reach"))
{
    foreach (var (name,body) in new[]{("Adjustable D",BowlReach.AdjustableD()),("Radial D",BowlReach.RadialD()),
        ("Ends-only D",BowlReach.EndsOnlyD()),("Independent B",BowlReach.B(false)),("Linked B",BowlReach.B(true))})
    {
        var prediction = BowlMotionReader.Predict(body.Before);
        body.Push();
        var after = body.ReadTips();
        Console.WriteLine($"{name}: {body.Before} -> {after}; predicted={prediction:X2} " +
            $"observed={BowlMotionReader.Compare(body.Before,after):X2} waiting={body.Waiting} bits={Convert.ToHexString(body.StoreBytes())}");
    }
    return;
}
if (args.Contains("--curved-capitals"))
{
    for (var i = 0; i < CurvedCapitals.Names.Count; i++)
    {
        var ink = CurvedCapitals.Ink(i);
        var read = ink.Read();
        Console.WriteLine($"{CurvedCapitals.Names[i],-26} loops={read.Loops} {read.Enter} -> {read.Leave} " +
            $"bend={read.BendSupport:X2} spans={Convert.ToHexString(ink.Catalogue.EdgeBytes())} drives={Convert.ToHexString(ink.DriveBytes())} " +
            $"coarse matches=[{string.Join(", ", CurvedCapitals.Compatible(ink.Read(false)))}]");
    }
    return;
}

if (args.Contains("--straight-capitals"))
{
    foreach (var name in StraightCapitals.Names)
    {
        var bare = StraightCapitals.Build(name, convention: false);
        var ink = StraightCapitals.Build(name);
        var layer = new StrokeFeatures(ink);
        Console.WriteLine($"{name,-9} unbound={string.Join("/", StraightCapitals.Candidates(bare)),-9} conventional={string.Join("/", StraightCapitals.Candidates(ink)),-5} " +
            $"corners={layer.Joints.Count(j => j.ProvedCorner)} branches={layer.Joints.Count(j => j.Arms >= 3)} separateCrossings={ink.ProjectedCrossings().Length} loops={layer.Loops.Count}");
        foreach (var loop in layer.Loops) Console.WriteLine($"  closed sites {string.Join(" -> ", loop.Sites())}, use bytes {Convert.ToHexString(loop.Bytes())}");
    }
    return;
}

if (args.Contains("--capitals"))
{
    foreach (var label in CapitalStudy.Labels)
    {
        var ink = CapitalStudy.Build(label);
        var incidence = ink.Incidence();
        Console.WriteLine($"{label}: sites {incidence.Sites}, shared {incidence.SharedSites}, cycles {incidence.Cycles}, candidates {string.Join("/", CapitalStudy.Candidates(ink))}; with supplied ground {string.Join("/", CapitalStudy.Candidates(CapitalStudy.Build(label, ground: true)))}");
    }
    var partialSpine = CapitalStudy.Build("E", partialE: true).Run(0);
    Console.WriteLine($"E spine: P/Q {partialSpine.Compare(0,1)}, Q/R {partialSpine.Compare(1,2)}");
    return;
}

if (args.Contains("--fork"))
{
    foreach (var tape in new[] { new ForkTape(false, false, 2, 4), new(false, false, 2), new(true, false), new(true, true) })
    {
        var machine = new ForkFixture(tape.LeftBusy, tape.RightBusy);
        var meter = machine.AttachRefusalCounter();
        var frames = new List<ForkFrame> { machine.Snapshot() };
        Console.WriteLine(tape);
        for (var beat = 1; beat <= 5; beat++)
        {
            tape.Apply(machine, beat);
            var frame = machine.Advance();
            frames.Add(frame);
            meter.Sample();
            Console.WriteLine($"beat {beat}  fork={frame.Fork:X2} exits={frame.Exits:X2} receipt={frame.Receipt:X2} refusal-meter={meter.Reading}");
        }
        Console.WriteLine(ForkCounts.From(frames));
    }
    return;
}

if (args.Contains("--seam-ladders"))
{
    foreach (var sameEdge in new[] { true, false })
    {
        var house = new HouseSeam(sameEdge);
        var room = house.Room;
        house.AddRoofCut();
        Console.WriteLine($"Room uses edge {room.Edge}, signed use {room.SignedUse}. Use bytes {Convert.ToHexString(house.UseBytes())}.");
        Console.WriteLine($"Roof cut read {string.Join(" -> ", house.Roof.Landmarks())}. Room cut read {string.Join(" -> ", room.Landmarks())}.");
        var bound = house.BindRoomToRoofCut();
        house.SetRoofSample(true);
        Console.WriteLine($"Added binding {bound}. Room now reads {string.Join(" -> ", room.Landmarks())}, flow word {Convert.ToString(room.Flow(), 2).PadLeft(3, '0')}.");
        Console.WriteLine($"Signed edge counts [{string.Join(", ", house.EdgeBalance())}].");
    }
    return;
}

if (args.Contains("--path-exchange"))
{
    PathExchangeExperiment.Run();
    return;
}

if (args.Contains("--boundary-scaling"))
{
    BoundaryScaling.Run();
    return;
}

if (args.Contains("--repetition"))
{
    RepetitionExamples.Run();
    return;
}

if (args.Contains("--saturation"))
{
    SaturationExamples.Run();
    return;
}

if (args.Contains("--conditionals"))
{
    ConditionalExamples.Run();
    return;
}

if (args.Contains("--passages"))
{
    PassageExamples.Run();
    return;
}

if (args.Contains("--potential"))
{
    StructuralPotentialExamples.Run();
    return;
}

if (args.Contains("--relations"))
{
    RelationExamples.Run();
    return;
}

if (args.Contains("--ports"))
{
    PortExamples.Run();
    return;
}

if (args.Contains("--connections"))
{
    ConnectionExamples.Run();
    return;
}

Console.WriteLine("O D V: orientation / direction / zero-or-nonzero");
Console.WriteLine("O: 0 positive-right, 1 positive-left. D: 0 positive, 1 negative. V: 0 zero, 1 nonzero.");
for (byte word = 0; word < 8; word++)
    Console.WriteLine($"{Convert.ToString(word, 2).PadLeft(3, '0')}  {Label(word)}");
var packed = ThreeBitMeasure.Pack([0, 1, 2, 3, 4, 5, 6, 7]);
Console.WriteLine($"\nEight words packed in order: {Convert.ToHexString(packed)} ({packed.Length} bytes, no header)");
Console.WriteLine("\nAddition in the positive-right orientation (? means nonzero of unknown magnitude):");
for (byte a = 0; a < 4; a++)
    for (byte b = 0; b < 4; b++)
        Console.WriteLine($"{Label(a)} + {Label(b)} = {string.Join(" or ", ThreeBitMeasure.Add(a, b).Select(Label))}");
Console.WriteLine("\nAt this resolution: -? < either zero < +?; identical words compare equal.");
Console.WriteLine("+? - +? can be negative, zero or positive: equal encodings do not prove equal magnitudes.");
Console.WriteLine($"Right-positive nonzero reoriented: 001 -> {Convert.ToString(ThreeBitMeasure.Reorient(1), 2).PadLeft(3, '0')}");
Console.WriteLine("Different orientations remain incomparable until that explicit change of perspective.");

Console.WriteLine("\nOne shared word has two opposed reads; neither read owns a second amount:");
byte shared = 0;
foreach (var next in new byte[] { 1, 3, 2, 5, 7 })
{
    shared = next;
    Console.WriteLine($"{Label(shared)} -> positive {Label(ThreeBitMeasure.ReadSide(shared, false))}, negative {Label(ThreeBitMeasure.ReadSide(shared, true))}");
}

Console.WriteLine("\nPrediction, actual merge, then conditional (opening order: negative / zero / positive):");
Console.WriteLine("Fine integers below belong only to the demonstration world; the predictor cannot inspect them.");
foreach (var (first, second) in new (long, long)[] { (5, -2), (2, -2), (2, -5) })
{
    long[] separate = [first, second];
    var a = FineFlowFixture.Observe(separate[0]);
    var b = FineFlowFixture.Observe(separate[1]);
    var prediction = ThreeBitMeasure.Add(a, b);
    var possibleBranches = prediction.Select(w => SignPassage.Select(w, SignPassage.All)!.Value).Distinct().Order();
    long[] cells = [FineFlowFixture.Join(separate), 0, 0, 0];
    var actual = cells[0];
    var observed = FineFlowFixture.Observe(actual);
    var selected = FineFlowFixture.Route(cells, SignPassage.All);
    Console.WriteLine($"{Label(a)} + {Label(b)} predicts [{string.Join(", ", possibleBranches.Select(BranchName))}]; " +
        $"world {first} + ({second}) settles {actual}, observed {Label(observed)} -> {BranchName(selected!.Value)}; " +
        $"outlets [{string.Join(", ", cells.Skip(1))}]");
}
Console.WriteLine("In this net-only fixture, zero selected a branch but retained no opposed participation.");
Console.WriteLine("Run with --connections to compare empty zero with retained balanced participation.");

long[] opposing = [2, -5];
long[] filtered = [FineFlowFixture.Join(opposing), 0, 0, 0];
var blocked = FineFlowFixture.Route(filtered, SignPassage.Positive);
Console.WriteLine($"\nOnly the positive opening exists: negative settlement is {(blocked is null ? "held" : "passed")}; source {filtered[0]}, outlets [{string.Join(", ", filtered.Skip(1))}].");
var released = FineFlowFixture.Route(filtered, SignPassage.All);
Console.WriteLine($"Opening the negative passage releases that held flow to {BranchName(released!.Value)}: [{string.Join(", ", filtered)}].");

long[] communicating = [5, -2];
FineFlowFixture.Transfer(communicating, SignPassage.All);
Console.WriteLine($"\nTwo separate stores, open passage: [5, -2] -> [{string.Join(", ", communicating)}].");
// Explicit new inputs in the fine demonstration world, not an ODV operation.
communicating[0] += 2;
communicating[1] -= 5;
FineFlowFixture.Transfer(communicating, 0);
Console.WriteLine($"After local inputs +2 and -5, with passage closed: [{string.Join(", ", communicating)}]. Both stores can now be occupied.");
FineFlowFixture.Transfer(communicating, SignPassage.All);
Console.WriteLine($"Reopen the passage: [{string.Join(", ", communicating)}]. These are still two stores, even when their behavior resembles one flow.");

static string BranchName(int position) => new[] { "negative", "zero", "positive" }[position];
static string Label(byte word) => $"{((word & 4) == 0 ? "R" : "L")}{((word & 2) == 0 ? "+" : "-")}{((word & 1) == 0 ? "0" : "?")}";
