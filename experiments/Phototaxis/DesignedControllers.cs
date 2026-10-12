using Gojo.Core;
using static Phototaxis.Native;

namespace Phototaxis;

// Construction arguments only. Executing controllers read the authored native
// equations, choices and feedback stocks, and never consult this record.
public sealed record ControllerDesign(string Name, int Seek = 8, int Feed = 6, int Escape = 9,
    int LeaveAt = 11, int ResumeAt = 0, int Gain = 3, int TurnLimit = 6,
    int SearchBias = 0, int FeedBrightness = 12, int Deadband = 0,
    bool Normalized = false, bool BangBang = false, int SearchCycle = 0, int SearchOn = 0, bool EnergyPaced = false);

public static class DesignedControllers
{
    public static ControllerDesign Champion => Round(3)[12];
    public static ControllerDesign[] Round(int round) => round switch
    {
        1 => [
            new("R1-01", 4,4,4,16, Gain:0), new("R1-02", 8,8,8,16, Gain:0),
            new("R1-03", 6,6,6,16, Gain:1), new("R1-04", 8,6,8,16, Gain:3),
            new("R1-05", 6,6,8,10, Gain:1), new("R1-06", 6,6,8,10, Gain:2),
            new("R1-07", 6,6,8,10, Gain:3), new("R1-08", 6,6,8,10, Gain:6),
            new("R1-09", 8,6,8,11, Gain:2), new("R1-10", 8,6,10,12, Gain:3),
            new("R1-11", 8,5,10,12, Gain:4), new("R1-12", 8,4,9,11, Gain:6),
            new("R1-13", 8,5,9,11, Gain:30, Normalized:true),
            new("R1-14", 6,6,8,11, Gain:3, BangBang:true),
            new("R1-15", 6,5,9,12, Gain:5, BangBang:true),
            new("R1-16", 8,5,10,11, Gain:7, BangBang:true),
            new("R1-17", 8,5,9,11, Gain:3, SearchBias:1),
            new("R1-18", 8,5,9,12,8, Gain:3),
            new("R1-19", 9,5,10,13,9, Gain:4),
            new("R1-20", 10,6,10,14,8, Gain:6)
        ],
        2 => [
            new("R2-01",8,5,10,12, Gain:4), new("R2-02",8,4,10,12, Gain:4),
            new("R2-03",8,3,10,12, Gain:4), new("R2-04",6,5,8,12, Gain:4),
            new("R2-05",10,5,10,12, Gain:4), new("R2-06",8,5,6,12, Gain:4),
            new("R2-07",8,5,5,12, Gain:4), new("R2-08",8,5,4,12, Gain:4),
            new("R2-09",8,5,8,12, Gain:4), new("R2-10",8,5,8,12, Gain:3,TurnLimit:4),
            new("R2-11",8,5,8,12, Gain:6,TurnLimit:8),
            new("R2-12",8,5,8,12, Gain:8,TurnLimit:8),
            new("R2-13",8,5,8,12, Gain:4,TurnLimit:3),
            new("R2-14",8,5,8,13, Gain:4), new("R2-15",8,5,8,14, Gain:4),
            new("R2-16",8,5,8,15, Gain:4), new("R2-17",8,5,8,12,11,Gain:4),
            new("R2-18",8,5,8,13,11,Gain:4), new("R2-19",8,5,8,14,12,Gain:4),
            new("R2-20",8,4,6,13,Gain:45,Normalized:true)
        ],
        3 => [
            new("R3-01",8,5,8,14,12,Gain:4), new("R3-02",8,5,8,13,11,Gain:4),
            new("R3-03",8,5,8,15,12,Gain:4), new("R3-04",8,5,8,15,13,Gain:4),
            new("R3-05",8,5,8,14,13,Gain:4), new("R3-06",8,5,8,14,11,Gain:4),
            new("R3-07",8,4,8,14,12,Gain:4), new("R3-08",8,3,8,14,12,Gain:4),
            new("R3-09",8,5,5,14,12,Gain:4), new("R3-10",8,5,4,14,12,Gain:4),
            new("R3-11",10,4,5,14,12,Gain:4),
            new("R3-12",8,5,8,14,12,Gain:6,TurnLimit:8),
            new("R3-13",8,5,8,14,12,Gain:8,TurnLimit:8),
            new("R3-14",8,4,6,14,12,Gain:45,Normalized:true),
            new("R3-15",8,4,6,14,12,Gain:5,BangBang:true),
            new("R3-16",8,5,8,14,12,Gain:4,SearchBias:6,SearchCycle:20,SearchOn:4),
            new("R3-17",8,5,8,14,12,Gain:4,SearchBias:6,SearchCycle:40,SearchOn:5),
            new("R3-18",8,5,8,14,12,Gain:4,SearchBias:6,SearchCycle:60,SearchOn:6),
            new("R3-19",8,5,8,12,Gain:8,TurnLimit:8,SearchBias:6,SearchCycle:40,SearchOn:5),
            new("R3-20",8,3,5,14,12,Gain:4,EnergyPaced:true)
        ],
        0 => Round(1).Concat(Round(2)).Concat(Round(3)).ToArray(),
        _ => throw new ArgumentOutOfRangeException(nameof(round), "This authored round has not been created yet.")
    };

    public static Action<Units, Creature> Builder(ControllerDesign design) => (units, creature) => Install(units, creature, design);

    public static void Install(Units u, Creature c, ControllerDesign d)
    {
        IElement N(long value) => L(u.Feature, value);
        Relation Less(IElement a, IElement b) => ExpressionGraph.Less(u, a, b);
        Relation Greater(IElement a, IElement b) => ExpressionGraph.Greater(u, a, b);
        Relation Choose(IElement condition, IElement yes, IElement no) => ExpressionGraph.Choose(u, condition, yes, no);
        if (d.Gain == 0)
        {
            c.ConfigureFeedback();
            c.LeftMuscle.InstallInherited(new Circuit(u, new Component(u, c.Sources[0], d.Seek)));
            c.RightMuscle.InstallInherited(new Circuit(u, new Component(u, c.Sources[0], d.Seek)));
            return;
        }
        var cells = new List<Relation>();
        var fullness = c.Sources[4]; IElement hunting = Less(fullness, N(d.LeaveAt));
        if (d.ResumeAt > 0)
        {
            var mode = Limited(u.Feature, 15); Aim(mode, 15);
            var nextMode = Choose(Less(fullness, N(d.ResumeAt)), N(15),
                Choose(Greater(fullness, N(d.LeaveAt - 1)), N(0), mode.ValueSlot));
            cells.Add(ControllerFeedback.Create(u, mode, nextMode, 4, c.BornAt));
            hunting = mode.ValueSlot;
        }
        var brightness = ExpressionGraph.Add(u, c.Sources[1], c.Sources[2]);
        var delta = ExpressionGraph.Subtract(u, c.Sources[2], c.Sources[1]);
        IElement forage = d.EnergyPaced ?
            SumTo(new Carrier(u.Feature.UnitShape), (N(d.Seek),1),
                (Scale(fullness,d.Feed-d.Seek,15,new Carrier(u.Feature.UnitShape)),1)) :
            Choose(Greater(brightness, N(d.FeedBrightness)), N(d.Feed), N(d.Seek));
        var drive = Choose(hunting, forage, N(d.Escape));
        IElement response;
        if (d.BangBang)
            response = Choose(Greater(delta, N(d.Deadband)), N(d.Gain),
                Choose(Less(delta, N(-d.Deadband)), N(-d.Gain), N(0)));
        else if (d.Normalized)
            response = Divide(Scale(delta, d.Gain, 1, new Carrier(u.Feature.UnitShape)),
                ExpressionGraph.Add(u, brightness, N(1)), new Carrier(u.Feature.UnitShape));
        else response = Scale(delta, d.Gain, 1, new Carrier(u.Feature.UnitShape));
        var bounded = Choose(Greater(response, N(d.TurnLimit)), N(d.TurnLimit),
            Choose(Less(response, N(-d.TurnLimit)), N(-d.TurnLimit), response));
        var turn = Choose(hunting, bounded, Scale(bounded, -1, 1, new Carrier(u.Feature.UnitShape)));
        var left = new List<Component> { new(u, drive, 15), new(u, turn, 15) };
        var right = new List<Component> { new(u, drive, 15), new(u, turn, -15) };
        if (d.SearchBias != 0)
        {
            IElement bias = N(d.SearchBias);
            if (d.SearchCycle > 0)
            {
                var phase = new Carrier(u.Count.UnitShape);
                phase.Attach(new Relation(Relation.Point(phase,d.SearchCycle),Relation.Point(phase,0)));
                Aim(phase,c.Slot * 17L % d.SearchCycle);
                var increment = SumTo(new Carrier(u.Count.UnitShape), (phase.ValueSlot,1), (L(phase,1),1));
                var nextPhase = Choose(Greater(brightness,N(2)),N(0),
                    Scale(increment,1,1,new Carrier(u.Feature.UnitShape)));
                cells.Add(ControllerFeedback.Create(u,phase,nextPhase,4,c.BornAt));
                bias = Choose(Less(Scale(phase.ValueSlot,1,1,new Carrier(u.Feature.UnitShape)),N(d.SearchOn)),N(d.SearchBias),N(0));
            }
            var search = Choose(Less(brightness, N(3)), bias, N(0));
            left.Add(new Component(u, search, 15)); right.Add(new Component(u, search, -15));
        }
        c.ConfigureFeedback(cells.ToArray());
        c.LeftMuscle.InstallInherited(new Circuit(u, left.ToArray()));
        c.RightMuscle.InstallInherited(new Circuit(u, right.ToArray()));
    }
}
