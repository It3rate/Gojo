namespace Gojo.Experiments.PrimitiveMeasure;

public static class StripSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach(int sides in new[]{3,4,5})foreach(bool close in new[]{false,true})
        {
            var walk=new StripWalk(sides,8,close);walk.Run();
            File.WriteAllText(Path.Combine(folder,$"{sides}-{(close?"perimeters":"spiral")}.svg"),StripDrawing.Draw(walk).Svg());
        }
        var partial=new StripWalk(5,8);partial.Step();partial.Step();
        File.WriteAllText(Path.Combine(folder,"pending.svg"),StripDrawing.Draw(partial).Svg());
        using var csv=File.CreateText(Path.Combine(folder,"strip-runs.csv"));
        csv.WriteLine("sides,closed,depth,edges,visits,available,pending,retained,stopped,body_bytes,frame_bytes,ink_bytes,strip_bytes");
        foreach(int sides in new[]{3,4,5})foreach(bool close in new[]{false,true})foreach(int depth in Enumerable.Range(2,31))
        {
            var walk=new StripWalk(sides,depth,close);walk.Run();var counts=walk.Depth.AuditCounts();
            csv.WriteLine($"{sides},{close},{depth},{walk.EdgeCount},{walk.Visits},{counts[0]},{counts[1]},{counts[2]},{walk.Complete},"+
                $"{walk.BodyBytes().Length},{walk.FrameBytes().Length},{walk.InkBytes().Length},{walk.BandBytes().Length}");
        }
        Console.WriteLine("Exported seven strip drawings and 186 loop runs.");
    }
}
