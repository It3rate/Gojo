namespace Gojo.Experiments.PrimitiveMeasure;

public static class FourHands
{
    // Leg -> elbow, bar through the upright, upright through the bar.
    // Only the upper leg and upright references change between hands.
    public static StrokeGraph Build(bool closed) => new(
        [new(3,[closed ? 4 : 0,1]),new(3,[1,2,3]),new(3,[4,2,5])],
        [2,4,10],[],new LetterGround([(-1,-1),(1,0),(0,-1)],[],[],3));

    private static readonly (string Name, StrokeGraph Ink)[] Models =
        [("open 4",Build(false)),("closed 4",Build(true)),("A",StraightCapitals.Build("A")),("T",StraightCapitals.Build("T"))];

    // Forget correspondence of the first run's start with other runs. Give
    // its occurrence a private tip in BOTH comparison views. Only incidence
    // outside this cut is tested. This projected graph cannot certify a loop.
    public static StrokeGraph ReleaseFirstTip(StrokeGraph ink)
    {
        var orders = Enumerable.Range(0,ink.RunCount).Select(i=>ink.Run(i).Sites()).ToArray();
        if (Enumerable.Range(0,ink.RunCount).Any(i=>!ink.Run(i).IsChain))
            throw new ArgumentException("This cut requires ordered runs.");
        var tip = orders[0][0];
        if (!orders.Skip(1).Any(run=>run.Contains(tip))) return ink;
        var used = orders.SelectMany(run=>run).ToHashSet();
        var fresh = Enumerable.Range(0,used.Count+1).First(site=>!used.Contains(site));
        orders[0][0] = fresh;
        var width = OrderedBoundary.Bits(Math.Max(fresh,used.Max())+1);
        return new(orders.Select(run=>new LandmarkRun(width,run)).ToArray(),ink.PenUses(),ink.GroundLinks(),
            ink.Frame,ink.ProjectedCrossings());
    }

    public static string[] Candidates(StrokeGraph observation, bool withTip)
    {
        var visible = withTip ? observation : ReleaseFirstTip(observation);
        return Models.Where(m=>visible.Fits(withTip ? m.Ink : ReleaseFirstTip(m.Ink))).Select(m=>m.Name).ToArray();
    }
    public static string[] ByLoops(int loops) => Models.Where(m=>m.Ink.Incidence().Cycles==loops).Select(m=>m.Name).ToArray();
}
