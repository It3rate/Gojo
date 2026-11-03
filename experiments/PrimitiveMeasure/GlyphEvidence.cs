using static Gojo.Experiments.PrimitiveMeasure.PenHeading;

namespace Gojo.Experiments.PrimitiveMeasure;

// A publication. Each null withdraws a particular answer from the reader.
public readonly record struct GlyphClues(int? Loops, int? FreeEnds, bool? TailBends,
    bool? BowlRight, bool? TailAbove)
{
    // In a complete nonempty finite ink graph, every nontrivial tree has
    // at least two leaves. A single tip seen through a crop is insufficient.
    public bool EndCountRequiresLoop => FreeEnds is 0 or 1;
    public bool Fits(GlyphClues sample) =>
        (Loops == null || Loops == sample.Loops) && (FreeEnds == null || FreeEnds == sample.FreeEnds) &&
        (TailBends == null || TailBends == sample.TailBends) && (BowlRight == null || BowlRight == sample.BowlRight) &&
        (TailAbove == null || TailAbove == sample.TailAbove);
}

/// <summary>Two optional ordered projections of ink sites onto the supplied
/// upright axes. Each starts or ends at the physical attachment of the tail.</summary>
public sealed class GlyphEvidence(CurveInk ink, LandmarkRun? across = null, LandmarkRun? up = null)
{
    public CurveInk Ink { get; } = ink;
    public LandmarkRun? Across { get; } = across;
    public LandmarkRun? Upward { get; } = up;

    public GlyphClues Read()
    {
        var edges = Enumerable.Range(0, Ink.InkCount).Select(Ink.Catalogue.Edge).ToArray();
        var ends = edges.SelectMany(e => e).GroupBy(site => site)
            .Where(group => group.Count() == 1).Select(group => group.Key).ToArray();
        var loops = Ink.Read().Loops;
        if (ends.Length != 1) return new(loops, ends.Length, null, null, null);

        var tip = ends[0];
        var tail = Array.FindIndex(edges, pair => pair.Contains(tip));
        var joint = edges[tail].Single(site => site != tip);
        var drive = Ink.DriveLinks().SingleOrDefault(row => row[0] == tail);
        bool? bends = drive == null ? null : (drive[1] & 1) != 0;
        bool? Order(LandmarkRun? projection, int site) => projection?.Compare(joint, site) switch
        {
            LandmarkRead.Before => true, LandmarkRead.After => false, _ => null
        };
        // The other horizontal witness is a marked site of the closed bowl.
        var sideSite = Across?.Sites().Single(site => site != joint);
        return new(loops, 1, bends, sideSite is int side ? Order(Across, side) : null, Order(Upward, tip));
    }

    private static GlyphEvidence WithTail(bool right, bool above, bool curvedTail, bool secondBowl = false)
    {
        // 0 and 1 bound the optional straight side, 2 is the free tip, and
        // 3 is a marked site on the far side of the bowl. Names have no order.
        int joint = above || curvedTail ? 0 : 1;
        var spans = new List<PenSpan>();
        var side = right ? Right : Left;
        var back = right ? Left : Right;
        var steer = right ? 1 : 3;
        if (curvedTail)
        {
            var vertical = above ? Down : Up;
            var opposite = above ? Up : Down;
            var bowlSteer = above == right ? 3 : 1;
            spans.Add(new(0,3,vertical,opposite,bowlSteer));
            spans.Add(new(3,0,opposite,vertical,bowlSteer));
            spans.Add(new(0,2,opposite,side,4 - bowlSteer));
        }
        else
        {
            spans.Add(new(1,0,Up,Up,0));
            spans.Add(new(0,3,side,Down,steer));
            spans.Add(new(3,1,Down,back,steer));
            var direction = above ? Up : Down;
            spans.Add(new(joint,2,direction,direction,0));
            if (secondBowl)
            {
                // A second return between the same stem ends adds a cycle
                // without changing the free tip or the original marked side.
                spans.Add(new(0,4,back,Down,4-steer));
                spans.Add(new(4,1,Down,side,4-steer));
            }
        }
        return new(new CurveInk(spans.ToArray(), spans.Count, (0,1)),
            new LandmarkRun(2, right ? [joint,3] : [3,joint]),
            new LandmarkRun(2, above ? [joint,2] : [2,joint]));
    }

    private static GlyphEvidence DoubleLoop()
    {
        // Two closed walks through one waist. The first travels up on its
        // left side and the second travels down on its left side.
        PenSpan[] spans = [new(0,1,UpLeft,Right,1), new(1,0,Right,DownLeft,1),
            new(0,2,DownLeft,Right,3), new(2,0,Right,UpLeft,3)];
        return new(new CurveInk(spans,4,(0,4)));
    }

    private static readonly GlyphEvidence Round = new(CurvedCapitals.Ink("O"));
    public static IReadOnlyList<(string Name, GlyphEvidence Body)> Examples { get; } =
        Array.AsReadOnly(new (string,GlyphEvidence)[] {
            ("b",WithTail(true,true,false)), ("d",WithTail(false,true,false)),
            ("p",WithTail(true,false,false)), ("q",WithTail(false,false,false)),
            ("6",WithTail(true,true,true)), ("9",WithTail(false,false,true)),
            ("o",Round), ("0",Round), ("O",Round), ("8",DoubleLoop()), ("c",new(CurvedCapitals.Ink("C")))
        });
    public static IReadOnlyList<(string Name, GlyphEvidence Body)> Observations { get; } =
        Array.AsReadOnly(Examples.Append(("Two bowls + tail",WithTail(true,true,false,secondBowl:true))).ToArray());
    public static (string Name, GlyphEvidence Body)[] Admitted(GlyphClues clues) =>
        Examples.Where(e => clues.Fits(e.Body.Read())).ToArray();
    public static string[] Candidates(GlyphClues clues) => Admitted(clues).Select(e => e.Name).ToArray();
}
