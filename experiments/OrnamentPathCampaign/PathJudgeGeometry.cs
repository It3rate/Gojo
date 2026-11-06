namespace Gojo.Experiments.OrnamentPathCampaign;

public static partial class PathJudge
{
    // A half-step line interval retains its unoriented supporting slope and exact
    // midpoint in quarter-steps. Splitting a stroke or reversing its drawing
    // direction does not change this geometric read. There is no filled grid.
    private readonly record struct Atom(int X,int Y,int Axis);

    private static void MeasureInk(PathTrace trace, double[] score)
    {
        int width=trace.Pitch*trace.Cells;
        var ink=new HashSet<Atom>();
        var crossed=new bool[trace.Edges.Length];
        var falseContact=new bool[trace.Edges.Length];
        double deposited=0, unique=0, inside=0, momentY=0;
        int minY=int.MaxValue, maxY=int.MinValue;
        var columns=new List<int>?[width];
        var accents=new double[4*trace.Cells];
        for (int i=0; i<trace.Edges.Length; i++)
        {
            var e=trace.Edges[i]; var a=trace.Sites[e.From]; var b=trace.Sites[e.To];
            int dx=Math.Sign(b.X-a.X), dy=Math.Sign(b.Y-a.Y);
            int length=StepLength(trace,e);
            if (length==0 || (dx!=0 && dy!=0 && Math.Abs(b.X-a.X)!=Math.Abs(b.Y-a.Y)))
                throw new ArgumentException("Observer requires nonzero axial or diagonal edges.");
            int axis=dx==0 ? 2 : dy==0 ? 0 : dx==dy ? 1 : 3;
            double weight=(axis&1)==0 ? .5 : Math.Sqrt(.5);
            deposited+=2*length*weight;
            for (int h=0; h<2*length; h++)
            {
                var atom=new Atom(4*a.X+dx*(2*h+1),4*a.Y+dy*(2*h+1),axis);
                if (!ink.Add(atom)) continue;
                unique+=weight;
                if (atom.X<0 || atom.X>4*width) continue;
                inside+=weight;
                momentY+=weight*(atom.Y*.25);
                minY=Math.Min(minY,atom.Y-Math.Abs(dy));
                maxY=Math.Max(maxY,atom.Y+Math.Abs(dy));
                int bin=Math.Min(accents.Length-1,atom.X/trace.Pitch);
                accents[bin]+=weight;
                // The two halves of the same unit edge produce the same
                // intersection ordinate at x+1/2; collapse it after sorting.
                if (axis!=2)
                {
                    int col=atom.X/4;
                    int y=atom.Y + (4*col+2-atom.X)*dx*dy;
                    (columns[col]??=[]).Add(y);
                }
            }
            for (int j=0;j<i;j++)
            {
                var other=trace.Edges[j];
                var c=trace.Sites[other.From]; var d=trace.Sites[other.To];
                if (Math.Max(a.X,b.X)<Math.Min(c.X,d.X) || Math.Max(c.X,d.X)<Math.Min(a.X,b.X)
                    || Math.Max(a.Y,b.Y)<Math.Min(c.Y,d.Y) || Math.Max(c.Y,d.Y)<Math.Min(a.Y,b.Y))
                    continue;
                long p=Side(a,b,c), q=Side(a,b,d), r=Side(c,d,a), s=Side(c,d,b);
                if (Opposed(p,q) && Opposed(r,s)) { crossed[i]=crossed[j]=true; continue; }
                if (e.From==other.From || e.From==other.To || e.To==other.From || e.To==other.To) continue;
                if (p==0 && q==0)
                {
                    // Positive-length collinear overlap is charged by unique /
                    // deposited support. A single shared coordinate is contact.
                    int overlap=dx!=0
                        ? Math.Min(Math.Max(a.X,b.X),Math.Max(c.X,d.X))-Math.Max(Math.Min(a.X,b.X),Math.Min(c.X,d.X))
                        : Math.Min(Math.Max(a.Y,b.Y),Math.Max(c.Y,d.Y))-Math.Max(Math.Min(a.Y,b.Y),Math.Min(c.Y,d.Y));
                    if (overlap==0) falseContact[i]=falseContact[j]=true;
                }
                else if ((p==0 && OnBox(a,b,c)) || (q==0 && OnBox(a,b,d)) ||
                         (r==0 && OnBox(c,d,a)) || (s==0 && OnBox(c,d,b)))
                    falseContact[i]=falseContact[j]=true;
            }
        }

        score[0]=Math.Clamp(unique/deposited,0,1);
        score[1]=1-(double)crossed.Count(x=>x)/trace.Edges.Length;
        score[2]=1-(double)falseContact.Count(x=>x)/trace.Edges.Length;
        score[15]=Math.Clamp(inside/unique,0,1);
        if (inside==0) { score[9]=0; score[13]=0; return; }
        double gaps=0, gapsSq=0, roomy=0; int gapsCount=0, reached=0;
        foreach (var col in columns)
        {
            if (col==null) continue;
            reached++;
            col.Sort();
            int prior=col[0];
            for (int i=1;i<col.Count;i++)
            {
                if (col[i]==prior) continue;
                double gap=(col[i]-prior)*.25;
                gaps+=gap; gapsSq+=gap*gap; gapsCount++;
                roomy+=Math.Min(1,gap/(.18*trace.Pitch));
                prior=col[i];
            }
        }
        score[9]=reached/(double)width;
        double height=(maxY-minY)*.25;
        if (height>0) score[10]=1-Math.Min(1,Math.Abs(momentY/inside-(minY+maxY)*.125)/(height*.5));
        if (gapsCount>0) score[11]=roomy/gapsCount;
        if (gapsCount>=2) score[12]=1/(1+Variation(gaps,gapsSq,gapsCount));
        score[13]=Window(inside*trace.Pitch/(width*Math.Max(height,trace.Pitch*.5)),0,1.4,3,6.5);
        score[14]=Window(Variation(inside,accents.Sum(x=>x*x),accents.Length),0,.22,.75,1.6);

        var maps=new MapCounts[5];
        foreach (var atom in ink)
        {
            if (atom.X<0 || atom.X>4*width) continue;
            int x=atom.X,y=atom.Y,flipped=(4-atom.Axis)&3;
            double weight=(atom.Axis&1)==0 ? .5 : Math.Sqrt(.5);
            int otherY=minY+maxY-y;
            Check(0,new(x+4*trace.Pitch,y,atom.Axis),4*trace.Pitch);
            Check(1,new(x,otherY,flipped),0);
            Check(2,new(4*width-x,y,flipped),0);
            Check(3,new(4*width-x,otherY,atom.Axis),0);
            Check(4,new(x+2*trace.Pitch,otherY,flipped),2*trace.Pitch);

            void Check(int i,Atom target,int shift)
            {
                if (x>=shift) maps[i].Target+=weight;
                if (target.X>4*width) return;
                maps[i].Source+=weight;
                if (ink.Contains(target)) maps[i].Both+=weight;
            }
        }
        score[4]=maps[0].Dice;
        if (height>0) score[16]=maps[1].Dice;
        score[17]=maps[2].Dice;
        if (height>0) { score[18]=maps[3].Dice; score[19]=maps[4].Dice; }
    }

    private struct MapCounts
    {
        public double Source, Target, Both;
        public readonly double Dice => Source+Target==0 ? double.NaN : Math.Min(1,2*Both/(Source+Target));
    }
    private static long Side(PathSite a,PathSite b,PathSite p)
        => (long)(b.X-a.X)*(p.Y-a.Y)-(long)(b.Y-a.Y)*(p.X-a.X);
    private static bool Opposed(long a,long b) => (a<0 && b>0) || (a>0 && b<0);
    private static bool OnBox(PathSite a,PathSite b,PathSite p)
        => p.X>=Math.Min(a.X,b.X) && p.X<=Math.Max(a.X,b.X)
        && p.Y>=Math.Min(a.Y,b.Y) && p.Y<=Math.Max(a.Y,b.Y);
}
