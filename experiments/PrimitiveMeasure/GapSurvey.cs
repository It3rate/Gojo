namespace Gojo.Experiments.PrimitiveMeasure;

public static class GapSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        void Save(string name,int shape,int budget=0,int wiring=0,bool settle=true)
        {
            var study=new GapStudy(shape,budget,wiring);
            if(settle)study.Pool.Settle();
            File.WriteAllText(Path.Combine(folder,name+".svg"),GapDrawing.Draw(study).Svg());
        }
        Save("fret-before",0,settle:false);Save("fret-even",0);Save("fret-remainder",0,1);Save("fret-finer",0,2);
        Save("triangle",1);Save("pentagon",3);Save("octagon",5);Save("neighbors",0,0,1);Save("unseen",0,0,2);
        using var csv=File.CreateText(Path.Combine(folder,"settlements.csv"));
        csv.WriteLine("shape,budget,wiring,substeps,initial,current,total,pairs,visits,moves,quiet,spread,regular_polygon,value_bytes,comparison_bytes,transfer_bytes");
        string[] names=["fret","triangle","square","pentagon","hexagon","octagon"];
        for(int shape=0;shape<6;shape++)for(int budget=0;budget<3;budget++)for(int wiring=0;wiring<4;wiring++)
        {
            var study=new GapStudy(shape,budget,wiring);var pool=study.Pool;pool.Settle();var audit=pool.Audit();
            csv.WriteLine($"{names[shape]},{budget},{wiring},{study.Subdivision.Count},{string.Join('|',study.Initial)},{string.Join('|',pool.Counts())},"+
                $"{audit.Total},{pool.PairCount},{audit.Visits},{audit.Moves},{audit.Quiet},{audit.Spread?.ToString()??"unknown"},{study.RegularNow},"+
                $"{pool.ValueBytes().Length},{pool.PairCount*2*pool.DemandBytes(0).Length},{pool.TransferBytes().Length}");
        }
        Console.WriteLine("Exported nine whitespace drawings and 72 settlement runs.");
    }
}
