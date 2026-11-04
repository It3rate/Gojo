using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage WasherPage()
    {
        var p = new ExplorerPage("Did the washer drop?",
            "Ask for one washer. When the desk hears nothing, should the counter release another on the retry?", "WasherCounter.md");
        Choose(p,"View",["Inside the machine","Desk only","Desk + tray lamp","Test both rules"],Mode,n=>
        {
            Mode = n;
            Control = 0;
            if (n == 3) Variant = 2;
        });
        Choose(p,"First failure",["Receipt lost","Request lost","Two receipts lost","No failure"],Variant,n=>Variant=n);
        int beats = 1 + Math.Min(Steps,2);
        int forward = Variant == 1 ? 6 : 7;
        int back = Variant switch { 0 => 6, 2 => 4, _ => 7 };
        if (Mode == 3)
        {
            Choose(p,"Read surface",["Returned receipts","Tray lamp","Release pulses"],Control,n=>Control=n);
            Stepper(p,2,"Next beat");
            ShowWasherProbe(p,forward,back,beats);
            return p;
        }
        Choose(p,"Dispenser",["Remember delivery","Serve every request"],Control,n=>Control=n);
        Stepper(p,2,"Next beat");
        byte law = Control == 0 ? WasherCounter.RememberDelivery : WasherCounter.ServeEveryRequest;
        var counter = new WasherCounter(law);
        var trace = WasherSurvey.Run(counter,forward,back,beats);
        var actual = WasherSurvey.Observe(trace);
        var last = trace[^1];
        p.Bits("Release rule","Bit L + 2R. L is an arrived request and R is a remembered delivery. The high bit changes the response to a retry.",
            [law],4,"rule");
        if (Mode == 0)
        {
            var predicted = WasherSurvey.Predict(law,forward,back,3);
            p.Bits("Request wire open","One bit per beat, earliest at bit 0. An open wire carries a request only while the order is pending.",[(byte)forward],3,"request");
            p.Bits("Receipt wire open","Permission to carry a receipt back on each beat.",[(byte)back],3,"receipt");
            p.Rows("Stock outlets → tray intake","Stock outlet 0 gets the first attempt at the shared empty inbox.",1,
                [[0,0],[1,0]],"tray",actual:counter.FeedRows);
            p.Bits("Actual stock","The two private stock sites after this beat.",[last.StockBits],2,"stock");
            p.Bits("Actual tray","Bit 0 is the inbox. Bit 1 is its parking place.",[last.TrayBits],2,"tray");
            p.Bits("Predicted releases","Full three-beat prediction from link words and release rule.",[predicted.Drops],3,"rule");
            p.Bits("Handoff law","At index before + 8 × after, one set bit admits a whole transition. Only 011 → 110 moves a washer.",
                counter.FeedLaw,64,"tray");
            p.Bits("Desk confirmation","This observation latch closes the request wire for the rest of this order.",[(byte)(last.Done ? 1 : 0)],1,"desk");
            p.Label($"Beat {beats} of 3   ·   one order outstanding at the start",350,48);
            p.Node("desk",last.Done ? "Desk · confirmed" : "Desk · waiting","The desk stops asking when it hears a receipt.",116,136,last.Done);
            p.Node("rule",last.Served ? "Delivery recorded" : "No delivery yet","The counter samples a successful transfer and holds that observation.",560,136,last.Served);
            p.Edge("request",last.RequestArrived ? "request arrived" : "no request arrived","Forward wire this beat.",190,109,469,109,last.RequestArrived);
            p.Edge("receipt",last.Heard ? "receipt heard" : last.ReceiptSent ? "receipt lost" : "no receipt","Return wire this beat.",469,166,190,166,last.Heard);
            p.Label("Material sites after this beat",350,224);
            p.Node("stock",$"Stock · {2-actual.Parts}","Each occupied stock site holds a separate washer.",140,295,last.StockBits!=0);
            p.Node("tray",$"Tray · {actual.Parts}","Inbox and shelf hold the delivered washers.",542,295,last.TrayBits!=0);
            p.Edge("tray",last.Dropped ? "washer dropped" : "no transfer","Both stock outlets compete for the same inbox.",220,295,460,295,last.Dropped);
            p.Label($"Looking ahead from the wire bits: {predicted.Parts} washer(s) by the end of beat 3.",350,368);
            p.Result = $"Beat {beats}. Tray holds {actual.Parts}. Desk {(last.Done ? "has its receipt" : "is still waiting")}.";
            p.Events.Add(last.Dropped ? "A stock site emptied and the tray inbox filled." :
                last.ReceiptSent ? "A past delivery supplied a fresh receipt. Both stock sites held their contents." : "No washer crossed the intake this beat.");
        }
        else
        {
            bool? lamp = Mode == 2 ? counter.TrayLamp : null;
            int mask = WasherSurvey.Mask(beats);
            var possible = WasherSurvey.Possible(law,beats,actual.Heard,mask,lamp);
            var counts = possible.Select(h=>h.Parts).Distinct().Order().ToArray();
            p.Bits("Receipts heard","One bit per observed beat. A clear observed bit is silence.",[actual.Heard],beats,"receipt",
                BeatMarks("receipt",beats));
            if (lamp.HasValue) p.Bits("Tray lamp","OR of inbox and shelf presence at this instant.",[(byte)(lamp.Value ? 1 : 0)],1,"lamp");
            p.Bits("Possible tray counts","External read. Bit n admits a tray containing n washers.",
                [(byte)counts.Sum(n=>1<<n)],3,"candidates",Enumerable.Range(0,3)
                    .Select(n=>new BitMark(n,1,["count"+n],$"Is a tray with {n} washers possible?")).ToArray());
            p.Label($"Desk record after {beats} of 3 beats",350,50);
            for (int i = 0; i < beats; i++)
            {
                bool heard = (actual.Heard & (1 << i)) != 0;
                p.Node("receipt"+i,$"{i+1} · {(heard ? "heard" : "silent")}","The desk samples only its return wire on this beat.",165+i*176,122,heard);
            }
            p.Node("lamp",lamp switch { true => "Lamp · occupied", false => "Lamp · empty", _ => "Lamp · unwired" },
                "The lamp reports occupancy even when the receipt was lost.",350,213,lamp==true,!lamp.HasValue);
            p.Label("Which tray contents fit those observations?",350,278);
            for (int n = 0; n <= 2; n++)
                p.Node("count"+n,$"{n} · {(counts.Contains(n) ? "possible" : "ruled out")}","Each admitted count has a complete compatible event history.",165+176*n,335,counts.Contains(n));
            p.Result = $"Beat {beats}. Possible tray counts: {string.Join(" or ",counts)}. {possible.Length} distinct release/receipt histories.";
            p.Events.Add("Choose Request lost or Receipt lost. The first silent beat looks identical from this desk.");
        }
        p.Boundary = "One pending order, two washers, three display beats. The links can drop a pulse. A fresh run starts with an empty tray and clear memory.";
        return p;
    }

    private static BitMark[] BeatMarks(string prefix,int beats) => Enumerable.Range(0,beats)
        .Select(i=>new BitMark(i,1,[prefix+i],$"Beat {i+1}")).ToArray();

    private void ShowWasherProbe(ExplorerPage p, int forward, int back, int beats)
    {
        p.Bits("Request wire open","The two counters receive the same forward schedule.",[(byte)forward],3,"request");
        p.Bits("Receipt wire open","The experimenter also fixes the return schedule.",[(byte)back],3,"receipt");
        byte[] laws = [WasherCounter.RememberDelivery, WasherCounter.ServeEveryRequest];
        byte[] readings = new byte[2];
        p.Label($"Same order and wires   ·   first {beats} beat(s)",350,48);
        for (int i = 0; i < 2; i++)
        {
            string name = i == 0 ? "Remember delivery" : "Serve every request";
            var trace = WasherSurvey.Run(new(laws[i]),forward,back,beats);
            var actual = WasherSurvey.Observe(trace);
            byte lamp = 0;
            for (int j = 0; j < beats; j++)
                if (trace[j].TrayBits != 0) lamp |= (byte)(1 << j);
            readings[i] = Control switch { 0 => actual.Heard, 1 => lamp, _ => actual.Drops };
            p.Bits(name+" · law","Truth table for the release gate.",[laws[i]],4,"law"+i);
            string key = $"read{i}-";
            p.Bits(name+" · reading","One bit per beat at the selected surface.",[readings[i]],beats,key,BeatMarks(key,beats));
            int y = 136 + 109*i;
            p.Node("law"+i,name,"These bits change how a repeat request is admitted.",127,y);
            for (int j = 0; j < beats; j++)
            {
                bool on = (readings[i] & (1<<j)) != 0;
                string pulse = Control switch { 0 => on ? "heard" : "silent", 1 => on ? "occupied" : "empty", _ => on ? "dropped" : "held" };
                p.Node(key+j,$"{j+1} · {pulse}","The selected public reading on this beat.",302+140*j,y,on);
            }
        }
        bool differs = readings[0] != readings[1];
        p.Label(differs ? "These observations distinguish the release rules." : "Either rule explains this reading.",350,351);
        p.Result = $"Beat {beats}. {(differs ? "Different traces" : "Same trace")} at this surface under the same wire schedule.";
        p.Boundary = "Try Two receipts lost. Release pulses distinguish the rules on beat 2, returned receipts on beat 3. Tray occupancy stays identical.";
        p.Events.Add("Both counters begin with two washers. Each run gets its own stock and tray.");
    }
}
