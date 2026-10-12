using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveForager;

/// <summary>Builds native rules, amount laws and memory. The dictionaries below
/// name their projections for the replay; they do not hold the policy.</summary>
public sealed class NativeForager
{
    public Carrier EnergyAxis { get; } = new();
    public Carrier HeatAxis { get; } = new();
    public Carrier BodyOrderAxis { get; }
    public Carrier Heading { get; } = new();
    public Carrier Mode { get; } = new();
    public Carrier Wait { get; } = new();
    public Carrier Delay { get; } = new();
    public Relation EnergyPartition { get; }
    public Relation HeatPartition { get; }
    public Relation EnergyCapacity { get; }
    public Relation HeatCapacity { get; }
    public Relation Root { get; }
    public Relation Latest { get; private set; }
    private readonly Carrier scoreAxis, presenceAxis, intentAxis = new(), actionAxis = new(), queryAxis = new(), timeAxis = new();
    private readonly Carrier energyRole = new(), heatRole = new(), fastRole = new(), slowRole = new(), presenceRole = new(),
        modeRole = new(), headingRole = new(), waitRole = new(), delayRole = new(), reserveRole = new(), forecastRole = new(),
        intentRole = new(), actionRole = new(), orderRole = new();
    private readonly Relation refineEnergyRules, refineHeatRules, modeRules, dipRules, budgetQueryRules, queryRules,
        intentRules, actionRules, headingRules, waitRules, delayRules, waitLeak, delayLeak, orientation,
        reservePartition, reserveLaw, reserveTable, energyCost, heatCost, amountContracts;
    private readonly CoarseDifferencePlan? packed;
    private readonly string backend;
    private readonly Dictionary<Relation, string> labels = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Carrier, string> roleNames = new(ReferenceEqualityComparer.Instance);
    public readonly string Variant;
    public int SpatialQueries { get; private set; }
    public int FineQueries { get; private set; }

    public NativeForager(Carrier evidenceAxis, Carrier signalPresenceAxis, Carrier bodyOrderAxis,
        Relation installedOrientation, string variant = "full", string executionBackend = "Packed")
    {
        scoreAxis = evidenceAxis; presenceAxis = signalPresenceAxis; BodyOrderAxis = bodyOrderAxis;
        Variant = variant; backend = executionBackend; orientation = installedOrientation;
        Heading.Set(2);
        EnergyPartition = CoarseReadSurface.Partition(new Carrier(), EnergyAxis,
            (0,14), (15,39), (40,69), (70,84), (85,100));
        HeatPartition = CoarseReadSurface.Partition(new Carrier(), HeatAxis,
            (0,19), (20,44), (45,69), (70,84), (85,100));
        EnergyCapacity = FiniteReadSurface.Capacity(new Carrier(), EnergyAxis, -100, 100);
        HeatCapacity = FiniteReadSurface.Capacity(new Carrier(), HeatAxis, -100, 100);
        reservePartition = CoarseReadSurface.Partition(new Carrier(), EnergyAxis,
            (-100,-1), (0,19), (20,39), (40,69), (70,100));
        reserveLaw = CoarseReadSurface.DifferenceLaw(new Carrier(), EnergyPartition, reservePartition);
        reserveTable = CoarseReadSurface.CompileDifference(new Carrier(), reserveLaw);
        packed = CoarseDifferencePlan.Capture(reserveTable);
        // Declared conservative wait allowances: four time ticks reserve
        // sixteen energy ticks and twenty heat ticks. With the stated query
        // fees these dominate four stationary, non-overheated world steps.
        // They are conditional wait forecasts, not a bound on all actions.
        // Neither is
        // a brightness calibration or an inferred distance between samples.
        var horizon = Relation.Point(timeAxis, 4);
        var drain = new Relation(Relation.Segment(timeAxis,0,4),Relation.Segment(EnergyAxis,0,16));
        var rise = new Relation(Relation.Segment(timeAxis,0,4),Relation.Segment(HeatAxis,0,20));
        energyCost = Project(horizon.Left, drain);
        heatCost = Project(horizon.Left, rise);
        amountContracts = G(horizon, drain, rise, energyCost, heatCost, EnergyCapacity, HeatCapacity);
        Relation C(Carrier role, Carrier axis, int low, int high) => SupportDecisionSurface.Condition(new Carrier(),role,Relation.Segment(axis,low,high));
        Relation E(int low,int high) => C(energyRole,EnergyAxis,low,high);
        Relation H(int low,int high) => C(heatRole,HeatAxis,low,high);
        Relation F(int low,int high) => C(fastRole,scoreAxis,low,high);
        Relation S(int low,int high) => C(slowRole,scoreAxis,low,high);
        Relation M(int value) => C(modeRole,Mode,value,value);
        Relation W(int low,int high) => C(waitRole,Wait,low,high);
        Relation D() => C(delayRole,Delay,0,0);
        Relation P(int value) => C(presenceRole,presenceAxis,value,value);
        Relation I(int value) => C(intentRole,intentAxis,value,value);
        Relation A(int value) => C(actionRole,actionAxis,value,value);
        Relation O(int value) => C(orderRole,BodyOrderAxis,value,value);
        Relation Reserve() => C(reserveRole,EnergyAxis,20,100);
        Relation Forecast() => C(forecastRole,HeatAxis,-100,74);
        Relation Const(string name,Carrier axis,int value,params Relation[] conditions) => R(name,new CarrierLocation(axis,value),Identity(axis),conditions);
        Relation Rules(params Relation[] entries) => SupportDecisionSurface.Create(new Carrier(),entries);

        refineEnergyRules = variant == "always_fine" ? Rules(Const("Always request finer energy",queryAxis,1)) : variant == "no_refinement" ? Rules(Const("Fine energy service removed",queryAxis,0)) : Rules(
            Const("Energy support crosses the full threshold",queryAxis,1,M(0),E(70,84)),
            Const("Energy support crosses the return threshold",queryAxis,1,M(1),E(15,39)),
            Const("Current energy support suffices",queryAxis,0));
        refineHeatRules = variant == "always_fine" ? Rules(Const("Always request finer heat",queryAxis,1)) : variant == "no_refinement" ? Rules(Const("Fine heat service removed",queryAxis,0)) : Rules(
            Const("Heat support crosses the safety threshold",queryAxis,1,H(70,84)),
            Const("Cooling support crosses the return threshold",queryAxis,1,M(1),H(20,44)),
            Const("Current heat support suffices",queryAxis,0));
        modeRules = Rules(Const("Heat safety: leave the source",Mode,1,H(75,100)),
            Const("Low reserve: seek light",Mode,0,E(0,25)),
            Const("Sufficient energy: leave to cool",Mode,1,E(80,100)),
            Const("Cooled and below full: seek again",Mode,0,M(1),H(0,24),E(0,79)),
            R("Retain current feeding/cooling memory",Mode.ValueSlot,Identity(Mode)));
        dipRules = Rules(Const("Short decline against longer positive context",queryAxis,1,M(0),P(1),F(-16,-4),S(1,16),W(0,0),D()),
            Const("No dip requiring an amount forecast",queryAxis,0));
        budgetQueryRules = variant == "no_refinement" ? Rules(Const("Budget refinement removed",queryAxis,0)) : Rules(
            Const("Reserve support straddles the wait budget",queryAxis,1,E(40,69)),
            Const("Coarse reserve support suffices",queryAxis,0));
        var holdConditions = variant == "no_budget" ? new[] {M(0),P(1),F(-16,-4),S(1,16),W(0,0),D()} :
            new[] {M(0),P(1),F(-16,-4),S(1,16),W(0,0),D(),Reserve(),Forecast()};
        var queryEntries = new List<Relation>();
        if (variant == "neutral") queryEntries.Add(Const("Neutral does not order the eyes",queryAxis,0));
        queryEntries.Add(Const("A bounded wait is still active",queryAxis,0,W(1,4)));
        queryEntries.Add(Const("Dark cooling rest needs no eye comparison",queryAxis,0,M(1),H(0,24),E(70,100)));
        if (variant != "no_slow") queryEntries.Add(Const("Dip wait is admitted",queryAxis,0,holdConditions));
        if (variant != "no_fast") queryEntries.Add(Const("Recent rise supports continuing",queryAxis,0,M(0),P(1),F(4,16)));
        queryEntries.Add(Const("Direction is action-relevant: compare the two eyes",queryAxis,1,P(1)));
        queryEntries.Add(Const("No readable presence: no eye request",queryAxis,0));
        queryRules = Rules(queryEntries.ToArray());
        var intents = new List<Relation>();
        if (variant == "neutral") intents.Add(Const("Neutral keeps moving right",intentAxis,1));
        if (variant != "no_presence")
        {
            intents.Add(Const("Measured absence: conserve movement for now",intentAxis,0,P(0)));
            // A missing publication cannot admit any presence-dependent rule;
            // the final route below keeps it open while holding this tick.
        }
        intents.Add(Const("Complete the bounded wait",intentAxis,0,W(1,4)));
        intents.Add(Const("Cooled with a healthy reserve: rest",intentAxis,0,M(1),H(0,24),E(70,100)));
        if (variant != "no_slow") intents.Add(Const("Brief decline: budget permits a four-tick wait",intentAxis,2,holdConditions));
        if (variant != "no_fast") intents.Add(Const("Recent relative rise: continue without finer sensing",intentAxis,1,M(0),P(1),F(4,16)));
        intents.Add(Const("Seek the brighter left eye",intentAxis,3,M(0),P(1),O(-1)));
        intents.Add(Const("Seek the brighter right eye",intentAxis,4,M(0),P(1),O(1)));
        intents.Add(Const("Cooling: leave the brighter left side",intentAxis,4,M(1),P(1),O(-1)));
        intents.Add(Const("Cooling: leave the brighter right side",intentAxis,3,M(1),P(1),O(1)));
        intents.Add(Const("Comparable eyes are equal: retain heading",intentAxis,1,P(1),O(0)));
        intents.Add(Const(variant == "no_presence" ? "Presence handling removed: continue on open evidence" : "Unresolved acquisition: hold while preserving uncertainty",intentAxis,variant == "no_presence" ? 1 : 0));
        intentRules = Rules(intents.ToArray());
        actionRules = Rules(Const("Hold",actionAxis,0,I(0)),Const("Hold and arm the wait",actionAxis,0,I(2)),
            Const("Move left",actionAxis,-1,I(3)),Const("Move right",actionAxis,1,I(4)),
            variant == "neutral" ? Const("Move right",actionAxis,1,I(1)) : R("Move in remembered direction",Heading.ValueSlot,
                new Relation(Relation.Segment(Heading,0,2),Relation.Segment(actionAxis,-1,1)),I(1)));
        headingRules = Rules(Const("Remember left",Heading,0,A(-1)),Const("Remember right",Heading,2,A(1)),
            R("Retain heading while holding",Heading.ValueSlot,Identity(Heading)));
        waitRules = Rules(R("Arm the amount forecast's four-tick wait",horizon.Left,
            new Relation(Relation.Segment(timeAxis,0,4),Relation.Segment(Wait,0,4)),I(2)),R("Retain remaining wait",Wait.ValueSlot,Identity(Wait)));
        delayRules = Rules(Const("Do not re-arm the same dip immediately",Delay,12,I(2)),R("Retain dip delay",Delay.ValueSlot,Identity(Delay)));
        waitLeak = new Relation(Relation.Segment(Wait,1,4),Relation.Segment(Wait,0,3));
        delayLeak = new Relation(Relation.Segment(Delay,1,12),Relation.Segment(Delay,0,11));
        (Carrier Role,string Name)[] roles = [(presenceRole,"presence"),(fastRole,"fast"),(slowRole,"slow"),(energyRole,"energy"),
            (heatRole,"heat"),(modeRole,"mode"),(headingRole,"heading"),(waitRole,"wait"),(delayRole,"delay"),
            (reserveRole,"reserve"),(forecastRole,"forecast"),(intentRole,"intent"),(actionRole,"action"),(orderRole,"orientation")];
        foreach (var (role,name) in roles) roleNames.Add(role,name);
        Root = G(G(roles.Select(x => (IElement)G(x.Role)).ToArray()),G(Heading,Mode,Wait,Delay),EnergyPartition,HeatPartition,
            amountContracts,reserveLaw,reserveTable,orientation,refineEnergyRules,refineHeatRules,modeRules,dipRules,budgetQueryRules,
            queryRules,intentRules,actionRules,headingRules,waitRules,delayRules,waitLeak,delayLeak);
        Latest = Root;
    }

    public BrainDecision Decide(Relation sample,Relation fastRead,Relation slowRead,Relation energy,Relation heat,
        Func<Relation> finerEnergy,Func<Relation> finerHeat,Func<Relation> spatial)
    {
        var witnesses = new List<IElement>(); var used = new HashSet<string>();
        var bindings = new Dictionary<Carrier,Relation>(ReferenceEqualityComparer.Instance);
        var spatialBefore = SpatialQueries; var fineBefore = FineQueries; var finerEnergyCount=0; var finerHeatCount=0;
        var eBefore = Text(energy); var hBefore = Text(heat);
        SetLeak(Wait,waitLeak,witnesses); SetLeak(Delay,delayLeak,witnesses);
        Relation Presence()
        {
            IElement value = SamplingSurface.TryReadSample(sample,out var p,out _) ? p : new Placeholder();
            return ReadWitness.Create(new Carrier(),sample,Relation.Segment(presenceAxis,0,1),value,new Placeholder(),sample);
        }
        bindings[presenceRole] = Presence(); bindings[fastRole] = Focus(fastRead); bindings[slowRole] = Focus(slowRead);
        if (Variant == "no_fast") bindings[fastRole] = Open(scoreAxis,fastRead);
        if (Variant == "no_slow") bindings[slowRole] = Open(scoreAxis,slowRead);
        bindings[energyRole] = energy; bindings[heatRole] = heat;
        MemoryBindings();
        var energyQuery = Pick(refineEnergyRules); var heatQuery = Pick(refineHeatRules);
        if (energyQuery.Point.Ticks == 1) { energy = finerEnergy(); FineQueries++; finerEnergyCount++; bindings[energyRole] = energy; }
        if (heatQuery.Point.Ticks == 1) { heat = finerHeat(); FineQueries++; finerHeatCount++; bindings[heatRole] = heat; }
        var mode = Pick(modeRules); Apply(Mode,mode.Point); MemoryBindings();
        var dip = Pick(dipRules);
        var reserve = Open(EnergyAxis,dip.Witness); var forecast = Open(HeatAxis,dip.Witness);
        var budgetCalculated = dip.Point.Ticks == 1 && Variant != "no_budget";
        if (budgetCalculated)
        {
            // Coarse arithmetic uses the explicitly calibrated energy domain.
            // Its regions are not ranks of uncalibrated light landmarks.
            ReadWitness.TryRead(energyCost,out _,out _,out var costResult,out _,out _);
            var coarseCost = costResult is CarrierLocation costPoint ?
                CoarseReadSurface.Measure(new Carrier(),EnergyPartition,(int)costPoint.Ticks,energyCost) :
                CoarseReadSurface.Unknown(new Carrier(),EnergyPartition,energyCost);
            if (CoarseReadSurface.TryReadMask(energy,EnergyPartition,out _)) reserve = backend switch
            {
                "Computed" => CoarseReadSurface.DifferenceComputed(new Carrier(),reserveLaw,energy,coarseCost),
                "Lookup" => CoarseReadSurface.DifferenceLookup(new Carrier(),reserveTable,energy,coarseCost),
                _ => packed!.Evaluate(new Carrier(),energy,coarseCost)
            };
            else reserve = FiniteReadSurface.Subtract(new Carrier(),EnergyCapacity,energy,FiniteCost(energyCost,EnergyCapacity));
            forecast = FiniteReadSurface.Add(new Carrier(),HeatCapacity,ToFinite(heat,HeatCapacity),FiniteCost(heatCost,HeatCapacity));
            bindings[reserveRole] = reserve; bindings[forecastRole] = forecast;
            var budgetQuery = Pick(budgetQueryRules);
            if (budgetQuery.Point.Ticks == 1 && CoarseReadSurface.TryReadMask(energy,EnergyPartition,out _))
            {
                energy = finerEnergy(); FineQueries++; finerEnergyCount++; bindings[energyRole] = energy;
                reserve = FiniteReadSurface.Subtract(new Carrier(),EnergyCapacity,energy,FiniteCost(energyCost,EnergyCapacity));
                bindings[reserveRole] = reserve;
            }
            witnesses.Add(G(reserve,forecast));
        }
        else { bindings[reserveRole] = reserve; bindings[forecastRole] = forecast; }
        var query = Pick(queryRules);
        var order = Open(BodyOrderAxis,query.Witness);
        if (query.Point.Ticks == 1)
        {
            var raw = spatial(); SpatialQueries++;
            if (ReadWitness.TryRead(raw,out _,out _,out var result,out _,out _) && result is CarrierLocation point)
                order = Variant == "no_orientation" ?
                    Publication(new CarrierLocation(BodyOrderAxis,point.Ticks),G(raw),raw) : Project(point,orientation,raw);
            witnesses.Add(G(raw,order));
        }
        bindings[orderRole] = order;
        var intent = Pick(intentRules); bindings[intentRole] = intent.Witness;
        var action = Pick(actionRules); bindings[actionRole] = action.Witness;
        var heading = Pick(headingRules); Apply(Heading,heading.Point);
        var waiting = Pick(waitRules); Apply(Wait,waiting.Point);
        var delay = Pick(delayRules); Apply(Delay,delay.Point);
        Latest = G(G(bindings.Select(x => (IElement)SupportDecisionSurface.Binding(new Carrier(),x.Key,x.Value)).ToArray()),
            G(witnesses.ToArray()),sample,fastRead,slowRead);
        // The intent witness supplies causal inputs; dependency expansion
        // attaches the amount law only when a selected wait actually uses it.
        var intentUsed = Uses(intent.Selection);
        foreach (var id in intentUsed) used.Add(id);
        // Mode selection may be decisive even when final intent only tests the
        // resulting native mode. Retain that dependency in the projection.
        if (intentUsed.Contains("mode")) foreach (var id in Uses(mode.Selection)) used.Add(id);
        if (used.Contains("orientation")) used.Add("order");
        if (used.Contains("reserve")) { used.Add("amount"); used.Add("energy"); }
        if (used.Contains("forecast")) { used.Add("amount"); used.Add("heat"); }
        if ((used.Contains("energy") && CoarseReadSurface.TryReadMask(energy,EnergyPartition,out _)) ||
            (used.Contains("heat") && CoarseReadSurface.TryReadMask(heat,HeatPartition,out _)) ||
            (used.Contains("reserve") && CoarseReadSurface.TryReadMask(reserve,reservePartition,out _))) used.Add("coarse");
        if ((used.Contains("energy") && finerEnergyCount>0) || (used.Contains("heat") && finerHeatCount>0)) used.Add("precision");
        string State(Relation publication) => IsOpen(publication) ? "open" : "settled";
        var evidence = new[]
        {
            Ev("presence","Unknown / presence","Uncalibrated signal acquisition",bindings[presenceRole],true,0),
            Ev("fast","Recent direction","Weighted ordinal comparisons",bindings[fastRole],true,0),
            Ev("slow","Longer context","Weighted ordinal comparisons",bindings[slowRole],true,0),
            Ev("energy","Coarse energy → selected detail","Declared local energy ticks",energy,true,finerEnergyCount*.25,eBefore+" → "+Text(energy)),
            Ev("heat","Coarse heat → selected detail","Declared local heat ticks",heat,true,finerHeatCount*.25,hBefore+" → "+Text(heat)),
            Ev("reserve","Reserve arithmetic","Energy − declared four-tick drain",reserve,budgetCalculated,0),
            Ev("forecast","Finite amount arithmetic","Heat + declared four-tick rise",forecast,budgetCalculated,0),
            Ev("orientation","Orientation / eye order","Instrument frame → body frame",order,SpatialQueries>spatialBefore,SpatialQueries-spatialBefore)
        };
        return new((int)action.Point.Ticks,intent.Reason,evidence,used.ToArray(),
            evidence.Where(e=>e.State=="open").Select(e=>e.Id).ToArray(),SpatialQueries-spatialBefore,FineQueries-fineBefore,
            (int)Heading.Value,(int)Mode.Value,(int)Wait.Value,Text(reserve)+"/"+Text(forecast));

        void MemoryBindings()
        {
            bindings[modeRole] = Memory(Mode,1); bindings[headingRole] = Memory(Heading,2);
            bindings[waitRole] = Memory(Wait,4); bindings[delayRole] = Memory(Delay,12);
        }
        Picked Pick(Relation rules)
        {
            var selection = SupportDecisionSurface.Select(new Carrier(),rules,bindings.Select(x=>SupportDecisionSurface.Binding(new Carrier(),x.Key,x.Value)).ToArray());
            if (!SupportDecisionSurface.TryReadSelection(selection,out var rule,out var response,out _,out _) ||
                response is not Relation authored || !authored.TryReadOrderedGroup(out var parts) || parts.Length!=2 || parts[1] is not Relation calibration)
                throw new InvalidDataException("The authored fixture has no readable native response.");
            var projection = Project(parts[0],calibration,selection);
            if (!ReadWitness.TryRead(projection,out _,out _,out var output,out _,out _) || output is not CarrierLocation point)
                throw new InvalidDataException("The native response projection is unresolved.");
            witnesses.Add(G(selection,projection));
            return new(point,projection,selection,rule is Relation nativeRule && labels.TryGetValue(nativeRule,out var name)? name:"Unresolved native rule");
        }
        string[] Uses(Relation selection)
        {
            if (!SupportDecisionSurface.TryReadSelection(selection,out _,out _,out var admitted,out _) || !admitted.TryReadOrderedGroup(out var inputs)) return [];
            return inputs.OfType<Relation>().Select(input => { input.TryReadOrderedGroup(out var parts); return parts.Length>0 && parts[0] is Carrier role && roleNames.TryGetValue(role,out var name)? name:null; })
                .OfType<string>().Distinct().ToArray();
        }
        Evidence Ev(string id,string label,string basis,Relation publication,bool queried,double cost,string? text=null) =>
            new(id,label,basis,text??Text(publication),!queried?"not requested":used.Contains(id)?"used":State(publication),queried,used.Contains(id),
                IsOpen(publication)?"The exposed support does not settle this read.":null,cost,$"latest/{id}");
    }

    private Relation R(string label,IElement source,Relation calibration,params Relation[] conditions)
    {
        var rule = SupportDecisionSurface.Rule(new Carrier(),G(source,calibration),conditions); labels.Add(rule,label); return rule;
    }
    private Relation Focus(Relation read)
    {
        IElement value = RelativeSignalSurface.TryReadBankRead(read,out var score,out _,out _,out _) ? score : new Placeholder();
        return ReadWitness.Create(new Carrier(),read,Relation.Segment(scoreAxis,-16,16),value,new Placeholder(),read);
    }
    private static Relation FiniteCost(Relation projection,Relation capacity)
    {
        ReadWitness.TryRead(projection,out _,out _,out var value,out _,out _);
        return value is CarrierLocation point ? FiniteReadSurface.Publish(new Carrier(),capacity,point,projection) : ReadWitness.Create(new Carrier(),projection,capacity,new Placeholder(),projection,projection);
    }
    private static Relation ToFinite(Relation publication,Relation capacity)
    {
        if (FiniteReadSurface.TryReadSupport(publication,out _,out _)) return publication;
        if (!ReadWitness.TryRead(publication,out _,out var surface,out _,out _,out _) || surface is not Relation partition ||
            !CoarseReadSurface.TryReadMask(publication,partition,out var mask) || mask==0 || !CoarseReadSurface.TryReadPartition(partition,out var axis,out var regions)) return publication;
        var bounds = regions.Where((_,i)=>(mask & 1<<i)!=0).Select(Frame).ToArray();
        // The interval is a conservative envelope of the public union; the
        // nominal endpoint is authored locally and adds no supplier precision.
        var low=bounds.Min(x=>x.Low); var high=bounds.Max(x=>x.High);
        return QualifiedReadSurface.Create(new Carrier(),publication,Relation.Segment(axis,0,1),new CarrierLocation(axis,low),
            new Relation(new CarrierLocation(axis,low),new CarrierLocation(axis,high)),new Placeholder(),capacity,publication,new Relation(publication,capacity));
    }
    public static string Text(Relation publication)
    {
        if (FiniteReadSurface.TryReadSupport(publication,out var lower,out var upper)) return $"[{lower?.Ticks.ToString()??"open"}…{upper?.Ticks.ToString()??"open"}]";
        if (ReadWitness.TryRead(publication,out _,out var surface,out var result,out _,out _))
        {
            if (result is CarrierLocation point) return point.Ticks.ToString();
            if (surface is Relation partition && CoarseReadSurface.TryReadMask(publication,partition,out var mask) && CoarseReadSurface.TryReadPartition(partition,out _,out var regions))
                return mask==0?"empty support":string.Join(" ∪ ",regions.Where((_,i)=>(mask&1<<i)!=0).Select(r=>{var f=Frame(r);return $"[{f.Low}…{f.High}]";}));
        }
        return "open";
    }
    private static bool IsOpen(Relation publication) => Text(publication).Contains("open",StringComparison.Ordinal);
    public static Relation Project(IElement source,Relation calibration,IElement? selected=null)
    {
        var target = calibration.Right as Relation; var axis = target is null ? Carrier.Unknown : Frame(target).Axis;
        var coordinate = source is CarrierLocation point ? point : source is Relation {Left:CarrierLocation origin} ? origin : default;
        if (coordinate.Carrier is null || !calibration.TryProject(coordinate,axis,out var projected) || projected.Left is not CarrierLocation result)
            return ReadWitness.Create(new Carrier(),selected??source,calibration,new Placeholder(),source,calibration);
        return Publication(result,calibration,G(selected??source,projected));
    }
    private static void SetLeak(Carrier memory,Relation leak,List<IElement> witnesses)
    {
        if (memory.Value==0) return;
        var publication=Project(memory.ValueSlot,leak); ReadWitness.TryRead(publication,out _,out _,out var result,out _,out _);
        if (result is CarrierLocation point) { witnesses.Add(publication); Apply(memory,point); }
    }
    private static void Apply(Carrier memory,CarrierLocation point)
    { if (!point.Carrier.SameIdentityAs(memory) || point.Ticks<0) throw new InvalidDataException("Native response targets a different memory."); memory.Set((ulong)point.Ticks); }
    private static Relation Memory(Carrier memory,int max) => Publication(new CarrierLocation(memory,(long)memory.Value),Relation.Segment(memory,0,max),memory);
    private static Relation Open(Carrier axis,IElement selected) => ReadWitness.Create(new Carrier(),selected,Relation.Segment(axis,-100,100),new Placeholder(),selected,selected);
    public static Relation Publication(CarrierLocation result,IElement surface,IElement evidence) => ReadWitness.Create(new Carrier(),evidence,surface,result,new Placeholder(),evidence);
    private static Relation Identity(Carrier axis) => new(Relation.Segment(axis,0,1),Relation.Segment(axis,0,1));
    public static (Carrier Axis,long Low,long High) Frame(Relation frame)
    {
        if (frame.Left is Relation {Left: CarrierLocation a} && frame.Right is Relation {Left:CarrierLocation b} && a.Carrier.SameIdentityAs(b.Carrier))
            return (a.Carrier,a.Ticks,b.Ticks);
        return (Carrier.Unknown,0,0);
    }
    public static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(),elements);
    private record Picked(CarrierLocation Point,Relation Witness,Relation Selection,string Reason);
}

public record BrainDecision(int Action,string Reason,Evidence[] Evidence,string[] Used,string[] Unresolved,
    int SpatialQueries,int FineQueries,int Heading,int Mode,int Wait,string BudgetSignature);
