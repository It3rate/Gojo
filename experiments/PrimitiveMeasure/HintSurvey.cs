namespace Gojo.Experiments.PrimitiveMeasure;

public static class HintSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        void Save(string name,HintScene scene) => File.WriteAllText(Path.Combine(folder,name+".svg"),scene.Svg());
        Save("fret",HintDrawing.Rails(HintExamples.Frieze(false)));
        Save("rectangle",HintDrawing.Rails(HintExamples.Frieze(true)));
        Save("open-order",HintDrawing.Rails(HintExamples.Frieze(false,1)));
        Save("open-angle",HintDrawing.Rails(HintExamples.Frieze(true,2)));
        Save("skipped-repeats",HintDrawing.Rails(HintExamples.Frieze(false,0,true)));
        Save("marked-rectangle",HintDrawing.Circle(HintExamples.Circle(0)));
        Save("all-six",HintDrawing.Circle(HintExamples.Circle(1)));
        Save("marked-square",HintDrawing.Circle(HintExamples.Circle(3)));
        Save("hidden-gate",HintDrawing.Circle(HintExamples.Circle(0,3)));
        Save("streams-separate",HintDrawing.Streams(0));
        Save("streams-together",HintDrawing.Streams(1));
        Save("streams-mixed",HintDrawing.Streams(3));
        using(var output=File.CreateText(Path.Combine(folder,"circle-masks.csv")))
        {
            output.WriteLine("places,mark_strip,marked_sites,chords,rectangle,square,regular_polygon");
            foreach(int n in new[]{4,6,8,10,12})
            for(int mask=0;mask<(1<<n);mask++)
            {
                byte[] bits=Enumerable.Range(0,n).Select(i=>(byte)((mask>>i)&1)).ToArray();
                var c=new MarkedContour(RepetitionExamples.Contour(n,true,false),new(bits));var read=c.Read();
                output.WriteLine($"{n},{string.Join("",bits)},{read.Landmarks},{c.ChordCount},{read.Rectangle},{read.Square},{read.RegularPolygon}");
            }
        }
        using(var output=File.CreateText(Path.Combine(folder,"rail-witnesses.csv")))
        {
            output.WriteLine("construction,relaxation,variation,admitted,ordered,right_angle_joints,closed,rectangle,conflict");
            foreach(bool rectangle in new[]{false,true})
            foreach(int relaxed in Enumerable.Range(0,5))
            foreach(int variation in Enumerable.Range(0,36))
            {
                var sketch=HintExamples.Sketch(rectangle,relaxed);var read=sketch.Read();
                var sample=RailRealization.Sample(sketch,variation);
                output.WriteLine($"{(rectangle?"rectangle":"fret")},{relaxed},{variation},{sample?.Admitted(sketch)??false},{read.Ordered},{read.RightTurns},{read.Closed},{read.Rectangle},{read.Conflict}");
            }
        }
        Console.WriteLine("Exported 12 drawings, 5,456 circular mark strips and 360 rail witness checks.");
    }
}
