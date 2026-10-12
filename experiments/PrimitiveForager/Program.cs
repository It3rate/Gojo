using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveForager;

var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
try
{
    if(args.Length==2 && args[0]=="--render-existing")
    {
        Lab.RenderExisting(Path.GetFullPath(args[1]),options);
        return 0;
    }
    var output = "artifacts/primitive-forager"; var ticks = 1536; var quick = false;
    for (var n=0;n<args.Length;n++) switch(args[n])
    {
        case "--help": Console.WriteLine("PrimitiveForager [--quick] [--ticks <32..4096>] [--output <directory>]\nPrimitiveForager --render-existing <saved-directory>"); return 0;
        case "--quick": quick=true; break;
        case "--output" when n+1<args.Length: output=args[++n]; break;
        case "--ticks" when n+1<args.Length && int.TryParse(args[++n],out ticks) && ticks is >=32 and <=4096: break;
        default: throw new ArgumentException("Unknown or incomplete option: "+args[n]);
    }
    if (quick) ticks=Math.Min(ticks,128);
    output=Path.GetFullPath(output); Directory.CreateDirectory(output);
    string[] variants=["full","no_presence","no_orientation","no_fast","no_slow","no_budget","no_refinement","always_fine","neutral"];
    int[] seeds=quick?[7,19]:[7,19,31,43];
    // Warm construction and native read paths outside each recorded loop.
    Lab.Run("full",7,24,"Packed",0,true,Path.Combine(output,"warmup"),false);
    var runs=new List<ForagerRun>();
    foreach(var seed in seeds) foreach(var variant in variants)
    {
        var run=Lab.Run(variant,seed,ticks,"Packed",0,true,output,true); runs.Add(run);
        Console.WriteLine($"{run.Id}: cycles {run.Summary.Cycles}, starvation {run.Summary.StarvationTicks}, heat {run.Summary.OverheatTicks}, optional queries {run.Summary.SpatialQueries+run.Summary.FineQueries}, {run.Summary.ElapsedMs:F0}ms");
    }
    var diagnosticList=Diagnostics.Run().ToList();
    var reference=runs.Single(run=>run.Seed==7 && run.RemovalLabel=="full");
    foreach(var backend in new[]{"Computed","Lookup"})
    {
        var run=Lab.Run("full",7,ticks,backend,0,true,output,true); runs.Add(run);
        diagnosticList.Add(new("Native amount arithmetic / lookup parity","coarse arithmetic",run.Summary.TrajectoryHash==reference.Summary.TrajectoryHash,
            "The computed conservative image, native row table and captured packed rows must preserve full observer, action, memory, resource and query trajectories.","Packed: "+reference.Summary.TrajectoryHash,backend+": "+run.Summary.TrajectoryHash));
    }
    foreach(var encoding in new[]{1,2})
    {
        var run=Lab.Run("full",7,ticks,"Packed",encoding,true,output,true); runs.Add(run);
        diagnosticList.Add(new("No brightness origin required","relative scale",run.Summary.TrajectoryHash==reference.Summary.TrajectoryHash,
            "A strictly increasing private light transformation must preserve scores, supports, actions, memory, physiology and service requests; private coordinates are excluded.",ForagingWorld.PrivateEncodingDescriptions[0],ForagingWorld.PrivateEncodingDescriptions[encoding]));
    }
    var forward=Lab.Run("full",7,ticks,"Packed",0,false,output,true); runs.Add(forward);
    diagnosticList.Add(new("Sensor orientation correspondence","orientation",forward.Summary.TrajectoryHash==reference.Summary.TrajectoryHash,
        "The same forager with a mirrored or forward instrument uses its exposed calibration. Physical trajectories must match.","Mirrored: "+reference.Summary.TrajectoryHash,"Forward: "+forward.Summary.TrajectoryHash));
    var comparisons=variants.Select(variant=>
    {
        var group=runs.Where(run=>run.RemovalLabel==variant && run.Backend=="Packed" && run.Encoding=="raw" && run.Mirrored).ToArray();
        return new MatchedComparison(Lab.Name(variant),group.Length,group.Sum(r=>r.Summary.Cycles),group.Sum(r=>r.Summary.StarvationTicks),
            group.Sum(r=>r.Summary.OverheatTicks),group.Sum(r=>r.Summary.SpatialQueries),group.Sum(r=>r.Summary.FineQueries),group.Sum(r=>r.Summary.ComparisonQueries),
            group.Sum(r=>r.Summary.QueryCost),group.Average(r=>r.Summary.MeanCharge));
    }).ToArray();
    var episodes=Lab.Episodes(reference).ToArray();
    var metadata=new
    {
        cells=ForagingWorld.Cells,ticks,defaultRunId=reference.Id,handDesigned=true,seeds,
        worldRules=ForagingWorld.WorldRules,
        sourceBoundary="Light offers opaque acquisitions, known presence or absence, and requested pair order. Unavailable acquisition is open. No light magnitude, patch location, position or outcome is written to brain memory. Body sensors have separately declared local energy and heat units. All rules, orientation, amount laws and persistent memory are native Gojo relations/carriers; JSON is a read-only projection.",
        observationCadence="Presence each tick; comparison-memory acquisition every two ticks. One-tick reuse of the last bank read is explicit in the loop. Unavailable acquisitions preserve the banks and expose open current evidence. Leaks count admitted acquisition steps, not wall-clock time.",
        costs="Each actual reference comparison costs0.002 energy; a requested eye ordering costs1; a selected finer body read costs0.25. Fractions accrue in the external world ledger. These are declared experimental costs, not measured CPU pricing or paid model calls.",
        amountContract="Separate local domains: energy/heat capacities−100..100. Four time ticks correspond to a declared conservative drain16 and heat rise20. Coarse energy subtraction uses a native table. Finer amount arithmetic preserves clipping/open support. Light order is never subtracted as a distance.",
        limitations="Authored controller, no learning or breeding. Baseline reference comparisons are eager, not a free lookup or lazy observer. Active memory is bounded; replay contains projections and each run retains only its final native archive. Diagnostic conditions demonstrate local boundary utility; aggregate outcomes may favor removals."
    };
    var result=new{generatedAt=DateTimeOffset.UtcNow,metadata,runs,episodes,matchedComparisons=comparisons,diagnostics=diagnosticList};
    var compactOptions=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    var json=JsonSerializer.Serialize(result,compactOptions);
    File.WriteAllText(Path.Combine(output,"results.json"),json,new UTF8Encoding(false));
    // All steps remain in the machine-readable report. The self-contained
    // viewer keeps one complete matched seed plus the other full-controller
    // seeds; duplicate backend/encoding proofs use their checked summaries.
    var replayRuns=runs.Where(run=>run.Backend=="Packed" && run.Encoding=="raw" && run.Mirrored && (run.Seed==7 || run.RemovalLabel=="full")).ToArray();
    var replay=new{generatedAt=DateTimeOffset.UtcNow,metadata,runs=replayRuns,episodes,matchedComparisons=comparisons,diagnostics=diagnosticList};
    var replayJson=JsonSerializer.Serialize(replay,compactOptions);
    File.WriteAllText(Path.Combine(output,"index.html"),File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"lab.html")).Replace("__FORAGER_DATA__",replayJson,StringComparison.Ordinal),new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(output,"report.md"),Lab.Report(runs,comparisons,diagnosticList.ToArray(),ticks,seeds),new UTF8Encoding(false));
    Console.WriteLine(JsonSerializer.Serialize(new{output,ticks,totalTicks=runs.Sum(r=>r.Summary.Ticks),diagnostics=diagnosticList,comparisons,
        nativeArchivesVerified=runs.All(r=>r.Summary.ArchiveVerified)},options));
    return diagnosticList.All(check=>check.Passed)?0:1;
}
catch(Exception error) when(error is ArgumentException or IOException or UnauthorizedAccessException)
{ Console.Error.WriteLine(error); return 1; }

static class Lab
{
    public static void RenderExisting(string output,JsonSerializerOptions options)
    {
        using var saved=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(output,"results.json")));
        var document=saved.RootElement;
        var metadata=document.GetProperty("metadata");
        var runs=document.GetProperty("runs").Deserialize<List<ForagerRun>>(options)??throw new InvalidDataException("Saved trajectories are unavailable.");
        var comparisons=document.GetProperty("matchedComparisons").Deserialize<MatchedComparison[]>(options)!;
        var diagnostics=document.GetProperty("diagnostics").Deserialize<Diagnostic[]>(options)!;
        var episodes=document.GetProperty("episodes").Deserialize<Episode[]>(options)!;
        var ticks=metadata.GetProperty("ticks").GetInt32();
        var seeds=metadata.GetProperty("seeds").Deserialize<int[]>(options)!;
        // Presentation naming only: final reserve arithmetic can be finite
        // after an earlier coarse table read. Do not label the final point LUT.
        foreach(var run in runs) foreach(var step in run.Steps)
            for(var i=0;i<step.Evidence.Length;i++) if(step.Evidence[i].Id=="reserve")
                step.Evidence[i]=step.Evidence[i] with{Label="Reserve arithmetic"};
        var replayRuns=runs.Where(run=>run.Backend=="Packed" && run.Encoding=="raw" && run.Mirrored && (run.Seed==7 || run.RemovalLabel=="full")).ToArray();
        var replay=new{generatedAt=document.GetProperty("generatedAt"),metadata,runs=replayRuns,episodes,matchedComparisons=comparisons,diagnostics};
        var json=JsonSerializer.Serialize(replay,new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        File.WriteAllText(Path.Combine(output,"index.html"),File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"lab.html")).Replace("__FORAGER_DATA__",json,StringComparison.Ordinal),new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(output,"report.md"),Report(runs,comparisons,diagnostics,ticks,seeds),new UTF8Encoding(false));
        Console.WriteLine($"Rendered {replayRuns.Length} saved replays; preserved {runs.Count} trajectories and their recorded signatures. No simulation or native memory changed.");
    }
    public static ForagerRun Run(string variant,int seed,int ticks,string backend,int encoding,bool mirrored,string output,bool archive)
    {
        var world=new ForagingWorld(seed,mirrored,encoding); var source=new Carrier(); var presenceAxis=new Carrier();
        var orderAxis=new Carrier(); var eyeAxis=new Carrier(); var bodyOrder=new Carrier();
        var state=RelativeSignalSurface.Create(new Carrier(),source,presenceAxis,orderAxis,maxReferences:48);
        RelativeSignalSurface.TryReadState(state,out _,out var policy,out _,out _,out _);
        RelativeSignalSurface.TryReadPolicy(policy,out _,out _,out _,out var scoreAxis,out _,out _);
        var brain=new NativeForager(scoreAxis,presenceAxis,bodyOrder,world.Orientation(eyeAxis,bodyOrder),variant,backend);
        var fast=Open(scoreAxis); var slow=Open(scoreAxis); var steps=new List<ForagerStep>();
        var timer=Stopwatch.StartNew(); var allocated=GC.GetAllocatedBytesForCurrentThread();
        for(var tick=0;tick<ticks;tick++)
        {
            var before=world.ComparisonQueries;
            var sample=world.Acquire(source,presenceAxis,tick);
            var readable=SamplingSurface.TryReadSample(sample,out var presence,out _);
            if(tick%2==0 && readable)
            {
                state=RelativeSignalSurface.Observe(new Carrier(),state,sample,(current,anchor)=>world.Compare(current,anchor,orderAxis));
                if(!RelativeSignalSurface.TryReadState(state,out _,out _,out _,out _,out var latest) ||
                    !RelativeSignalSurface.TryReadLatest(latest,out _,out fast,out slow,out _,out _))
                    throw new InvalidDataException("Native comparison banks lost their published reads.");
            }
            // Do not reuse a readable old score as today's missing/absent read.
            var currentFast=readable && presence.Ticks==1?fast:Open(scoreAxis,sample);
            var currentSlow=readable && presence.Ticks==1?slow:Open(scoreAxis,sample);
            var decision=brain.Decide(sample,currentFast,currentSlow,world.Energy(brain.EnergyPartition),world.HeatRead(brain.HeatPartition),
                ()=>world.FineEnergy(brain.EnergyCapacity),()=>world.FineHeat(brain.HeatCapacity),()=>world.Spatial(source,presenceAxis,eyeAxis,tick));
            var comparisons=world.ComparisonQueries-before;
            var cost=comparisons*.002+decision.SpatialQueries+decision.FineQueries*.25;
            var f=Read(currentFast,orderAxis); var s=Read(currentSlow,orderAxis);
            var physical=new PhysicalFrame(world.X,world.PeakX(tick),world.Charge,world.Heat,world.IsCloud(tick),world.IsDark(tick),world.IsUnavailable(tick));
            var stateSignature=BankSignature(state)+$"/{f.Score}:{f.State}/{s.Score}:{s.State}/{decision.BudgetSignature}";
            var evidence=decision.Evidence.Select(e=>e.Id is "fast" or "slow" ? e with{QueryCost=e.Id=="fast"?comparisons*.002:0,
                Queried=tick%2==0 && readable && presence.Ticks==1,PublicationText=e.PublicationText+(tick%2==1?" (read age1 tick)":"")} : e).ToArray();
            steps.Add(new(tick,physical,decision.Action<0?"Move left":decision.Action>0?"Move right":"Hold",decision.Reason,
                decision.Mode==0?"Seek / feed":"Leave / cool",f.Score,s.Score,f.State,s.State,evidence,
                new(decision.Reason,decision.Used,decision.Unresolved),comparisons+decision.SpatialQueries+decision.FineQueries,cost,
                decision.Action,decision.Heading,decision.Mode,decision.Wait,comparisons,decision.SpatialQueries,decision.FineQueries,stateSignature));
            world.Advance(tick,decision.Action,cost);
        }
        timer.Stop(); allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        var id=$"{variant}_s{seed}_{backend.ToLowerInvariant()}_e{encoding}_{(mirrored?"mirror":"forward")}";
        var signature=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(steps.Select(s=>new
        {
            s.Tick,s.World,s.Action,s.Heading,s.Mode,s.Wait,s.Reason,s.FastScore,s.SlowScore,s.FastState,s.SlowState,
            s.ComparisonQueries,s.SpatialQueries,s.FineQueries,s.QueryCost,s.NativeSignature,s.Decision.UsedEvidenceIds
        }))))).ToLowerInvariant();
        long bytesCount=0; var verified=false; var archiveFile=id+".gojo";
        if(archive)
        {
            Directory.CreateDirectory(output);
            var graph=NativeForager.G(state,brain.Root,brain.Latest);
            var bytes=GojoNumericFormat.Encode(graph); bytesCount=bytes.Length;
            File.WriteAllBytes(Path.Combine(output,archiveFile),bytes); VerifyArchive(bytes,state,brain); verified=true;
        }
        var uses=steps.SelectMany(step=>step.Decision.UsedEvidenceIds).GroupBy(x=>x).ToDictionary(g=>g.Key,g=>g.Count());
        var summary=new ForagerSummary(ticks,world.Cycles,world.StarvationTicks,world.OverheatTicks,world.Movements,
            (double)world.ChargeSum/ticks,brain.SpatialQueries,brain.FineQueries,world.ComparisonQueries,world.TotalQueryCost,
            timer.Elapsed.TotalMilliseconds,allocated,signature,uses,archiveFile,bytesCount,verified);
        return new(id,$"{Name(variant)} · seed{seed} · {backend} · e{encoding} · {(mirrored?"mirrored":"forward")}",variant,seed,backend,
            encoding==0?"raw":encoding==1?"shifted":"nonlinear",mirrored,summary,steps.ToArray());
    }
    public static string Name(string variant)=>variant switch
    {
        "full"=>"All capabilities","no_presence"=>"Remove absence handling","no_orientation"=>"Ignore installed orientation",
        "no_fast"=>"Remove recent trend","no_slow"=>"Remove longer context","no_budget"=>"Remove wait budget math",
        "no_refinement"=>"Remove finer body service","always_fine"=>"Always request finer body values","neutral"=>"Neutral: always right",_=>variant
    };
    private static (int? Score,string State) Read(Relation read,Carrier orderAxis)
    {
        if(!RelativeSignalSurface.TryReadBankRead(read,out var score,out _,out _,out _)) return(null,"open");
        var allSame=RelativeSignalSurface.TryReadBankComparisons(read,out var pairs) && pairs.Length>0 && pairs.All(pair=>
            ReadWitness.TryRead(pair,out _,out _,out var answer,out _,out _) && answer is CarrierLocation point && point.Carrier.SameIdentityAs(orderAxis) && point.Ticks==0);
        return((int)score.Ticks,allSame?"all same":score.Ticks==0?"mixed / rounded zero":score.Ticks>0?"relative rise":"relative fall");
    }
    private static string BankSignature(Relation state)
    {
        RelativeSignalSurface.TryReadState(state,out _,out _,out var fast,out var slow,out _);
        return string.Join("/",new[]{fast,slow}.Select(bank=>
        {
            RelativeSignalSurface.TryReadBank(bank,out _,out var references);
            return string.Join(",",references.Select(reference=>
            {RelativeSignalSurface.TryReadReference(reference,out _,out var weight);return weight.Value.ToString();}));
        }));
    }
    private static Relation Open(Carrier axis,IElement? source=null)=>ReadWitness.Create(new Carrier(),source??new Placeholder(),Relation.Segment(axis,-16,16),new Placeholder(),new Placeholder(),source??new Placeholder());
    private static void VerifyArchive(byte[] bytes,Relation state,NativeForager brain)
    {
        var roots=GojoNumericFormat.Decode(bytes);
        if(roots.Length!=1 || roots[0] is not Relation graph || !graph.TryReadOrderedGroup(out var fields) || fields.Length!=3 ||
            fields[0] is not Relation restored || fields[1] is not Relation restoredPolicy || !restoredPolicy.TryReadOrderedGroup(out var policy) ||
            policy[1] is not Relation memory || !memory.TryReadOrderedGroup(out var cells) || cells.Length!=4 ||
            cells[0] is not Carrier heading || cells[1] is not Carrier mode || cells[2] is not Carrier wait || cells[3] is not Carrier delay ||
            heading.Value!=brain.Heading.Value || mode.Value!=brain.Mode.Value || wait.Value!=brain.Wait.Value || delay.Value!=brain.Delay.Value ||
            BankSignature(restored)!=BankSignature(state) || policy[5] is not Relation law || !CoarseReadSurface.TryReadLaw(law,out _,out _,out _) ||
            policy[6] is not Relation table || !CoarseReadSurface.TryReadTable(table,out _,out _,out _))
            throw new InvalidDataException("Final archive changed native memory, observer weights or amount law/table correspondence.");
        if(PolicySignature(brain.Root)!=PolicySignature(restoredPolicy))
            throw new InvalidDataException("Final archive changed the native controller graph.");
    }
    private static string PolicySignature(IElement root)
    {
        var axes=new List<Carrier>(); var placeholders=new List<Placeholder>();
        int Axis(Carrier carrier)
        { var index=axes.FindIndex(axis=>axis.SameIdentityAs(carrier));if(index>=0)return index;axes.Add(carrier);return axes.Count-1; }
        string Read(IElement element) => element switch
        {
            Carrier carrier=>$"C{Axis(carrier)}:{carrier.Value}",
            CarrierLocation coordinate=>$"P{Axis(coordinate.Carrier)}:{coordinate.Ticks}",
            Relation relation when relation.TryReadOrderedGroup(out var fields)=>"G("+string.Join(",",fields.Select(Read))+")",
            Relation relation=>"R("+Read(relation.Left)+","+Read(relation.Right)+")",
            Placeholder open=>"?"+Open(open),
            _=>throw new InvalidDataException("The fixture archive contains an unsupported policy shape.")
        };
        int Open(Placeholder open)
        {var index=placeholders.FindIndex(value=>ReferenceEquals(value,open));if(index>=0)return index;placeholders.Add(open);return placeholders.Count-1;}
        // Compare the exposed grammar, scalar values and within-graph aliases;
        // serialized node ids can legitimately differ when a subtree is encoded.
        return Read(root);
    }
    public static IEnumerable<Episode> Episodes(ForagerRun run)
    {
        yield return new(run.Id,0,"First acquisition","Presence is available; comparison memory is still uncalibrated and open.");
        foreach(var id in new[]{"presence","fast","slow","reserve","forecast","orientation","precision"})
        {
            var step=run.Steps.FirstOrDefault(s=>s.Decision.UsedEvidenceIds.Contains(id));
            if(step is not null) yield return new(run.Id,step.Tick,"First use: "+id,step.Reason);
        }
        var missing=run.Steps.FirstOrDefault(s=>s.World.Unavailable); if(missing is not null) yield return new(run.Id,missing.Tick,"Unavailable is not dark","The current acquisition stays open; it does not become a zero-light landmark.");
        var cloud=run.Steps.FirstOrDefault(s=>s.World.Cloud); if(cloud is not null) yield return new(run.Id,cloud.Tick,"A short cloud","Short and longer comparison context may disagree. This does not guarantee that waiting is useful.");
    }
    public static string Report(List<ForagerRun> runs,MatchedComparison[] comparisons,Diagnostic[] diagnostics,int ticks,int[] seeds)
    {
        var text=new StringBuilder("# Primitive numbers: a consolidated forager\n\n");
        text.AppendLine($"Nine authored controllers across {seeds.Length} seeds, {ticks} ticks each; {runs.Count} runs including native backend, scale and orientation checks, {runs.Sum(r=>r.Summary.Ticks):N0} habitat ticks. No training, breeding, paid AI calls or reward-written brain memory.");
        text.AppendLine("\n## Amounts and orders stay distinct\n");
        text.AppendLine("Light starts as opaque acquisitions, presence/absence and requested order. Fast/slow score is attention-weighted ordinal comparison evidence, not a brightness estimate, confidence probability or arithmetic on landmark ranks. Zero can retain opposed votes. Body energy/heat have explicit separate local tick units; a four-time-tick service supplies conservative energy16 / heat20 costs. Coarse range arithmetic and native tables filter actions; finite arithmetic/refinement operates only in those declared amount domains. Rules admit a response only when all advertised support is contained in a condition. Explicit fallback preserves earlier unresolved rule reads.");
        text.AppendLine("\n## World and service prices\n");
        foreach(var line in ForagingWorld.WorldRules) text.AppendLine("- "+line);
        text.AppendLine("\nPresence is acquired each tick; center reference banks update every two ticks. Decay measures successful acquisition steps. The last bank read is reused for one intervening tick. Missing or absent current evidence stays open even if old banks are still retained. The eager baseline order observer pays0.002 energy per actual pair comparison; spatial ordering costs1 and finer body reads0.25. These experimental fees are not actual machine-time prices. All variants retain the same instrument/observer unless the named connection is removed; unused slow banks still cost computation.");
        text.AppendLine("\n## Matched outcomes\n\n| Controller | Cycles | Starvation ticks | Overheat ticks | Eye queries | Fine body queries | Reference comparisons | Query energy | Mean charge |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach(var c in comparisons) text.AppendLine($"| {c.Label} | {c.Cycles} | {c.StarvationTicks} | {c.OverheatTicks} | {c.SpatialQueries} | {c.FineQueries} | {c.ComparisonQueries} | {c.QueryCost:F1} | {c.MeanCharge:F2} |");
        var full=comparisons.Single(c=>c.Label==Name("full"));
        var noFast=comparisons.Single(c=>c.Label==Name("no_fast"));
        var noFine=comparisons.Single(c=>c.Label==Name("no_refinement"));
        var alwaysFine=comparisons.Single(c=>c.Label==Name("always_fine"));
        text.AppendLine("\n## What the results establish\n");
        text.AppendLine($"- Coarse information is sufficient for useful agency: full completes {full.Cycles} feeding/cooling cycles; the uninformed always-right reference completes {comparisons.Single(c=>c.Label==Name("neutral")).Cycles}. Correct orientation matters in the mirrored installation; ignoring it completes {comparisons.Single(c=>c.Label==Name("no_orientation")).Cycles}.");
        text.AppendLine($"- Selective body readings use {full.FineQueries} requests versus {alwaysFine.FineQueries} always-fine requests. Focused equal-action encounters establish that sufficient coarse support can avoid precision;79/80 establishes a boundary where finer information changes the response. The long always-fine comparison combines service fees, published detail and subsequent resource/trajectory divergence. It does not separately isolate their effects.");
        text.AppendLine($"- Removing all fine body service leaves {noFine.OverheatTicks} overheat ticks versus {full.OverheatTicks} full, but starvation is {noFine.StarvationTicks} versus {full.StarvationTicks}. This is a safety/behavior tradeoff, not an overall victory for higher resolution.");
        text.AppendLine($"- Recent evidence saves {noFast.SpatialQueries-full.SpatialQueries} eye requests and {noFast.QueryCost-full.QueryCost:F2} declared query energy, but full completes {full.Cycles} cycles versus {noFast.Cycles} without the recent input. Averaged comparison positivity can outlast fresh dimming after passing the source. Removing recent input also prevents its dependent dip waits, so that aggregate gap does not isolate only one rule.");
        text.AppendLine("- Slow context and budget arithmetic change the focused wait decisions and are used in the long habitat. Their added policy does not improve aggregate feeding here. A locally safe wait is not proof that waiting is useful. The next policy should test fresh temporal direction before treating older-relative positivity as progress, and reserve contextual waits for evidence that actually favors waiting.");
        text.AppendLine("\nAll primary controllers use the same four world seeds, initial state, sensor services, query fees and physical rules. They are mirrored installations, making the exposed orientation materially relevant. The separate forward/full check verifies equivalent calibrated behavior. Neutral always-right still maintains the instrument for equal baseline cost; it is an uninformed movement reference, not a zero-computation lower bound. No body-precision ablation guesses a fine value; it retains coarse support and takes an explicit fallback.");
        text.AppendLine("\n## Focused encounters and contract checks\n");
        foreach(var d in diagnostics) text.AppendLine($"- **{d.Title}: {(d.Passed?"passed":"FAILED")}**. {d.Description} Full: {d.Full}. Comparison: {d.Without}.");
        text.AppendLine("\n## Evidence utilization\n");
        foreach(var run in runs.Where(r=>r.RemovalLabel=="full" && r.Backend=="Packed" && r.Encoding=="raw" && r.Mirrored))
            text.AppendLine($"Seed{run.Seed}: "+string.Join(", ",run.Summary.CapabilityUses.OrderBy(x=>x.Key).Select(x=>$"{x.Key}={x.Value}"))+".");
        text.AppendLine("\nUse counts come from selected native condition bindings and explicit dependency expansion, not invocation counts. They establish participation; matched outcomes and focused changed decisions establish the specific measured utility. A signal may be used and still harm this policy.");
        text.AppendLine($"\n## Runtime and retained proof\n\nLoop runtime {runs.Sum(r=>r.Summary.ElapsedMs)/1000:F2}s in total, excluding graph construction, archive readback, rendering and file output; loop allocations {runs.Sum(r=>r.Summary.AllocatedBytes)/1024d/1024/1024:F2}GiB across all sequential runs. Final archives verified: {runs.Count(r=>r.Summary.ArchiveVerified)}/{runs.Count}. Each archive retains observer banks, native law/table correspondence, decision structure and carrier memory. JSON retains replay projections, not every historical native operational graph. Packed lookup remains a derived projection of the authoritative native table.\n");
        text.AppendLine("This is a diagnostic sandbox, not proof of learning or general intelligence. Source schedule/fees/thresholds are authored. Precision services and units are declared by local instruments rather than discovered by the organism. The display is a small SVG replay, separate from the existing Skia/WebAssembly aquarium. Selective body/spatial queries are demand-driven; the comparison-memory observer is still eager. Opportunity remains to make it sparse or lazy without discarding the native support and witness contracts.");
        return text.ToString();
    }
}
