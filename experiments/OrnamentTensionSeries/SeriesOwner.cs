using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentTensionSeries;

public enum SeriesMode { Live, TrayOnly, PulseOnly, Hold, Clock }
public readonly record struct SeriesMove(int Tool,int Lane,int Pulse,int Before,int Spent,int After,bool Echo,int Overflow);

// This owner's only input is a frieze byte. There is deliberately no PathTrace,
// learned critic, desired picture, program-name observation or geometry query here.
public sealed class SeriesOwner
{
    private readonly byte mask, selectionLaw, passageLaw, outlet;
    private readonly int[] memory=new int[2];
    private int seen,tool,charge,sequence,overflow;
    private bool echo;
    private static readonly bool[,,] Gates=Compile();
    public SeriesOwner(byte[] bytes,int first=0)
    {
        if(bytes.Length!=4 || bytes[0]==0 || bytes[1]>15 || bytes[2]>15 ||
            (bytes[3]&3)>2 || bytes[3]>23 || first is <0 or >1)
            throw new ArgumentException("Invalid four-byte series controller.");
        mask=bytes[0];selectionLaw=bytes[1];passageLaw=bytes[2];outlet=bytes[3];
        tool=first;
    }
    private static bool[,,] Compile()
    {
        var table=new bool[16,2,2];
        for(int m=0;m<16;m++)
        {
            var law=BitRelation.FromTuples(3,from a in Enumerable.Range(0,2) from b in Enumerable.Range(0,2)
                select new byte[]{(byte)a,(byte)b,(byte)((m>>(a+2*b))&1)});
            for(int a=0;a<2;a++)for(int b=0;b<2;b++)
                table[m,a,b]=law.Predict([(byte)(1<<a),(byte)(1<<b)])==2;
        }
        return table;
    }
    public void Receive(byte publication)
    {
        int marks=publication&mask;
        echo=seen>0 && (marks==memory[0] || seen>1 && marks==memory[1]);
        if(seen>0)
        {
            int offer=echo ? 1 : -1;
            int settled=Math.Clamp(charge+offer,-3,3);
            overflow=charge+offer-settled;
            charge=settled;
        }
        memory[1]=memory[0];memory[0]=marks;seen++;
    }
    public SeriesMove Next(SeriesMode mode)
    {
        int before=charge,clock=sequence&1;
        // First draw starts on the declared side with no material input.
        bool choose=seen>0 && Gates[selectionLaw,charge>0 ? 1 : 0,clock];
        bool admit=seen>0 && charge!=0 && Gates[passageLaw,echo ? 1 : 0,clock];
        bool switchTray=(mode is SeriesMode.Live or SeriesMode.TrayOnly) && choose || mode==SeriesMode.Clock && sequence!=0;
        if(switchTray)tool^=1;
        int spent=0,pulse=0;
        if(admit && (mode is SeriesMode.Live or SeriesMode.PulseOnly))
        {
            int width=1+(outlet>>3);
            spent=Math.Sign(charge)*Math.Min(width,Math.Abs(charge));
            pulse=(outlet&4)==0 ? spent : -spent;
            charge-=spent;
        }
        // Reading pressure for a tray choice is a non-draining switch. Only
        // the explicit external pulse passage spends the signed quantity.
        var result=new SeriesMove(tool,outlet&3,pulse,before,spent,charge,echo,overflow);
        overflow=0;sequence++;
        return result;
    }
}
