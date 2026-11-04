using System.Numerics;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static string TestName(int row)
    {
        var p = WasherProbeBank.Calibrated.Probe(row);
        string surface = p.Surface switch { WasherProbe.Receipt => "receipt", WasherProbe.Lamp => "lamp", _ => "chute" };
        return row == WasherProbeBank.Feedback ? "Early receipt, then chute" : $"Beat {p.Beats} · {surface}";
    }

    private ExplorerPage ProbeDeskPage()
    {
        var p = new ExplorerPage("Which test should run next?",
            "At a factory test station, a controller tries identically loaded washer dispensers from one batch and chooses its next test from the answers so far.", "WasherProbeDesk.md");
        Choose(p,"Test station",["All sensors","Five beats · receipts","Chute only","Old rack · chute","Lamp only","Protect stock","First read missing","Inspect stuck lamp"],Mode,
            n => { Mode = n; if (n == 7) Variant = 0; });
        string[] fixtures = Enumerable.Range(0,12).Select(k =>
            $"{(k / 3 % 2 == 0 ? "Once" : "Retry")} · {k%3} · {(k<6 ? "wired" : "cut")}").ToArray();
        Choose(p,"Hidden batch",fixtures,Variant,n=>Variant=n);
        Choose(p,"Question",["Tell batches apart","Can two fall?","Late receipt?"],Control,n=>Control=n);
        Choose(p,"Prefer",["Shorter test","Even split"],A-1,n=>A=n+1);
        Stepper(p,WasherProbeBank.Count,"Run next test");

        int sensors = Mode switch { 1 => 1, 2 or 3 => 4, 4 => 2, _ => 7 };
        int budget = Mode == 1 ? 5 : Mode == 7 ? 18 : 9;
        int sockets = WasherProbeBank.AllSockets;
        if (Mode == 3) sockets &= ~(1 << WasherProbeBank.Feedback);
        var goal = WasherProbeAudit.Goal(Control);
        var desk = new WasherProbeDesk(sensors,budget,goal,protectStock:Mode==5,checkPromises:Mode==7,
            preferBalanced:A==2,sockets:sockets);
        var bank = WasherProbeBank.Calibrated;
        var stand = new WasherTestStand(Variant);
        var path = new List<ProbeTrial>();
        byte Sample(WasherProbe probe)
        {
            byte read = stand.Sample(probe);
            if (Mode == 6 && path.Count == 0) return 0;
            if (Mode == 7 && probe.Surface == WasherProbe.Lamp) return 2;
            return read;
        }
        for (int n = 0; n < Steps; n++)
        {
            if (desk.Step(Sample) is not {} test) break;
            path.Add(test);
        }
        var offers = desk.Offers();
        int count = BitOperations.PopCount((uint)desk.Live);
        var marks = fixtures.Select((name,k)=>new BitMark(k,1,["slot"+k],name)).ToArray();
        p.Bits("Possible batches","Each lit bit still explains every sample. The hidden batch selector is part of the apparatus.",
            LowBytes((uint)desk.Live,2),12,"live",marks);
        p.Bits("Open test sockets","These sockets can receive the desk's one activation. The first open row takes it.",
            LowBytes((uint)offers.Open,2),10,"choices",Enumerable.Range(0,10).Select(i=>new BitMark(i,1,["probe"+i],TestName(i))).ToArray());
        p.Label("Possible dispenser designs after the observed tests",355,20);
        p.Label("Remembers delivery",214,51); p.Label("Serves every retry",517,51);
        for (int k = 0; k < 12; k++)
        {
            bool live = (desk.Live & (1<<k)) != 0;
            int y = k<6 ? 103 : 172;
            p.Node("slot"+k,$"{k%3} · {(live ? "fits" : "out")}",$"{fixtures[k]}. Stock is the initial number of washers.",122+(k%6)*95,y,live);
        }
        p.Label("Wired",44,94); p.Label("Cut",44,164);
        p.Label("Number in each box = washers loaded at the start of a test",350,205);
        string status;
        if (desk.Live == 0) status = "The observations conflict with every calibrated design.";
        else if (goal.HasValue && desk.Answered) status = (desk.Live & goal.Value) != 0 ? "The answer is yes." : "The answer is no.";
        else if (count == 1) status = "One calibrated design fits.";
        else if (offers.Open != 0) status = "Another test can run.";
        else if (WasherProbeAudit.Settled(desk.Live,sensors,goal)) status = "These designs agree at the connected sensors.";
        else status = "The remaining differences need a test outside the current limits.";

        if (path.Count == 0) p.Node("choices","Ready to test","Run the next test to let a single open socket take the activation.",350,273,true);
        else
        {
            var last = path[^1];
            string sample = last.Sample switch { 0=>"missing", 1=>"off", 2=>"on", _=>"uncertain" };
            p.Node("probe"+last.Row,TestName(last.Row),"Last test executed on a fresh dispenser.",212,273,true);
            p.Node("sample","Read · "+sample,"Only this final sensor sample goes to the desk.",483,273,last.Sample==2,last.Sample==0);
            p.Edge("sample","sensor wire","The rest of the test trace is private to the apparatus.",309,273,425,273);
            p.Bits("Last test program","Low three bits drive requests, next three enable returns, top two choose the sensor.",
                [bank.Probe(last.Row).Word],8,"probe"+last.Row);
            p.Bits("Last sensor sample","01 means off, 10 means on, 00 missing and 11 unresolved.",[last.Sample],2,"sample");
            p.Bits("Calibrated ON predictions","Intersect the prior mask with these contacts for an ON reading, or their complement for OFF.",
                LowBytes((uint)bank.Yes(last.Row),2),12,"row",marks);
            p.Bits("Before this test","The prior survives intact as a timeline witness.",LowBytes((uint)last.Before,2),12,"before",marks);
            p.Bits("Second washer possible","If a live bit overlaps this mask, the stock-protection gate can veto this test.",
                LowBytes((uint)bank.DoubleRelease(last.Row),2),12,"risk",marks);
            p.Events.Add("Tests run in order: "+string.Join(" → ",path.Select(t=>TestName(t.Row))));
        }
        int next = offers.Open == 0 ? -1 : BitOperations.TrailingZeroCount((uint)offers.Open);
        if (next < 0) p.Label("No test socket is open",350,337);
        else p.Node("probe"+next,$"Next · {TestName(next)}","The first open passage receives the shared activation.",350,349,true);
        int shown = next >= 0 ? next : path.Count == 0 ? -1 : path[^1].Row;
        if (shown >= 0) p.Bits($"Socket {shown} · {TestName(shown)}","Four packed bytes. The wide fields hold measured contacts for all twelve designs.",
            LowBytes(bank.Row(shown),4),32,"probe"+shown,
            new BitMark(0,3,["probe"+shown],"Forward wire"),new BitMark(3,3,["probe"+shown],"Return wire"),
            new BitMark(6,2,["probe"+shown],"Sensor port"),new BitMark(8,12,["probe"+shown],"Predicts ON"),
            new BitMark(20,12,["probe"+shown],"Can release twice"));
        p.Bits("Activation routes","Ten ordered [credit, gate, socket] rows. Each endpoint address takes five bits.",
            desk.RouteBytes,150,"choices",Enumerable.Range(0,10)
                .Select(i=>new BitMark(15*i,15,["probe"+i],$"Test socket {i}")).ToArray());
        if (next >= 0) p.Events.Add(offers.Checking ? "Checking a shared prediction. A disagreement can reject all remaining designs." :
            "The next socket has both an ON prediction and an OFF prediction among the survivors.");
        p.Result = $"{count} of 12 designs fit. {desk.BeatsLeft} beats left. {status}";
        p.Boundary = "Each test gets a fresh cartridge from the same batch. The desk sees one sensor bit per test. The reference designs and priority wiring are supplied.";
        return p;
    }
}
