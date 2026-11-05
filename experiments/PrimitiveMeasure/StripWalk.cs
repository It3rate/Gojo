namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A selected regular convex, three-to-five-direction construction.
/// Meaning resides in a shared contour, normal/depth bindings, paired support
/// lines, shared corner sites, and a cyclic body with an inward or return seam.
/// No coordinates, angles, per-pass instructions or desired repeat count are
/// stored. This is a deliberately narrow geometric law, not a general offset
/// solver. Retained rows are its output and execution correspondence.</summary>
public sealed class StripWalk
{
    private readonly EndpointRows frames=new(),body=new(),normals=new(),units=new();
    private readonly EndpointRows lines=new(),joints=new(),ink=new(),bands=new(),lifts=new();
    private readonly IPrimitiveReadPort widthRead,cornerRead;
    public const int TemplateBits=4;
    public StripDepth Depth { get; }
    public ContourPattern Contour { get; }
    public ReferenceCycle Phase { get; }
    public PulseCircuit Circuit { get; }=new(3);
    public int Check { get; }
    public int Commit { get; }
    public int Done { get; }
    public int PresentTable { get; }
    public int AbsentTable { get; }
    public int FeedbackTable { get; }
    public int ReferenceBits { get; }
    public int Sides=>Contour.PartCount;
    public int EdgeCount=>ink.Count(3,ReferenceBits);
    public int LineCount=>lines.Count(2,ReferenceBits);
    public int JointCount=>joints.Count(2,ReferenceBits);
    public int BandCount=>bands.Count(3,ReferenceBits);
    public int LiftCount=>lifts.Count(2,ReferenceBits);
    public int Visits { get; private set; }
    public bool Complete=>Circuit.Active==Done;
    public bool ClosedPass=>Body(Sides-1)[1]==Body(0)[0];
    public StripVisit? Last { get; private set; }
    public IPrimitiveReadPort Fit { get; }

    public StripWalk(int sides,int depth,bool closePass=false,IPrimitiveReadPort? width=null,
        IPrimitiveReadPort? corner=null,bool commonUnit=true,bool equalTurns=true)
    {
        if(sides is < 3 or > 5)throw new ArgumentOutOfRangeException(nameof(sides));
        Depth=new(depth);ReferenceBits=OrderedBoundary.Bits(sides*(depth+2)+2);
        widthRead=width??new Constant(2);cornerRead=corner??new Constant(2);
        Check=Circuit.State(true);Commit=Circuit.State();Done=Circuit.State();
        int guard=Circuit.Guard(),always=Circuit.Guard();
        Phase=new(sides,Circuit.Expose(Commit));
        Contour=new(new Constant(2),new Constant(2));
        int[] sites=[ContourPattern.Start,..Enumerable.Range(5,sides-1),ContourPattern.End];
        for(int i=0;i<sides;i++)
        {
            Contour.Arc(sites[i],sites[i+1],equalTurns?32:32+i);
            normals.Append(6,Phase.Sites[i],sites[i]);
            frames.Append(TemplateBits,i,i,closePass?1:0);
            body.Append(TemplateBits,i,i+1<sides?i+1:closePass?0:sides);
            lines.Append(ReferenceBits,i,0);
        }
        Contour.Close();
        if(!closePass)frames.Append(TemplateBits,sides,0,1);
        // Two parameter roles refer to the same local unit, unless deliberately
        // left incompatible. This is observational width reuse, not area flow.
        units.Append(2,0,0);units.Append(2,1,commonUnit?0:1);
        for(int i=0;i<sides;i++)joints.Append(ReferenceBits,(i+sides-1)%sides,i);
        Fit=PresencePorts.All(new GeometryRead(this),widthRead,cornerRead,new RoomRead(this));
        Circuit.Bind(guard,Fit);
        PresentTable=Circuit.AddTable(PassageLaw.Controlled());
        AbsentTable=Circuit.AddTable(PassageLaw.Controlled(false));
        FeedbackTable=Circuit.AddTable(PassageLaw.Unconditional());
        Circuit.Link(PresentTable,Check,guard,Commit);
        Circuit.Link(AbsentTable,Check,guard,Done);
        Circuit.Link(FeedbackTable,Commit,always,Check);
    }
    private sealed class Constant(byte support):IPrimitiveReadPort { public byte ReadSupport()=>support; }
    private sealed class RoomRead(StripWalk walk):IPrimitiveReadPort
    { public byte ReadSupport()=>walk.Depth.Pending>=0?(byte)2:walk.Depth.HasRoom.ReadSupport(); }
    private sealed class GeometryRead(StripWalk walk):IPrimitiveReadPort
    {
        public byte ReadSupport()
        {
            var read=walk.Contour.Read();
            bool compatible=walk.units.Read(new(2,2,0))[1]==walk.units.Read(new(2,2,1))[1];
            return read.Circle && read.EqualParts && compatible && Enumerable.Range(0,walk.Sides).All(i=>
                walk.normals.Read(new(2,6,i)).SequenceEqual(new[]{walk.Phase.Sites[i],walk.Contour.ArcRow(i)[0]}))?(byte)2:(byte)0;
        }
    }
    public StripVisit Step()
    {
        if(Complete)throw new InvalidOperationException("The terminal site has no continuation.");
        int phase=Phase.Position,depth=Depth.ActiveDepth;byte support=Fit.ReadSupport();
        var check=Circuit.Pulse();Visits++;
        if(!check.Moved || Circuit.Active==Done)
            return Last=new(phase,depth,support,false,Complete,null,null);
        if(Depth.Pending<0)Depth.Reserve();
        depth=Depth.ActiveDepth;
        var row=Body(phase);int current=Resolve(row[0],depth),next=Resolve(row[1],depth);
        int start=EdgeCount==0?0:Edge(EdgeCount-1)[1];
        if(ClosedPass && phase==0)
        {
            int previous=Resolve(Body(Sides-1)[0],depth);
            int innerStart=Joint(previous,current);
            lifts.Append(ReferenceBits,start,innerStart);start=innerStart;
        }
        int end=Joint(current,next),edge=EdgeCount;
        ink.Append(ReferenceBits,start,end,current);
        if(phase==Sides-1)Depth.Retain();
        if(!Phase.Circuit.Pulse().Moved || !Circuit.Pulse().Moved)
            throw new InvalidOperationException("The declared feedback cycle failed.");
        return Last=new(phase,depth,support,true,false,edge,end);
    }
    public void Run(int budget=10000)
    {
        for(int i=0;i<budget && !Complete;i++)if(!Step().Drew)break;
    }
    private int Resolve(int frame,int depth)
    {
        var row=Frame(frame);return Line(row[1],depth+row[2]);
    }
    private int Line(int normal,int depth)
    {
        for(int i=0;i<LineCount;i++)if(LineRow(i).SequenceEqual(new[]{normal,depth}))return i;
        if(depth<=0 || depth>=Depth.Count)throw new InvalidOperationException("A support line must retain positive inner depth.");
        int outer=Line(normal,depth-1),index=LineCount;
        lines.Append(ReferenceBits,normal,depth);
        bands.Append(ReferenceBits,outer,index,0);
        return index;
    }
    private int Joint(int a,int b)
    {
        for(int i=0;i<JointCount;i++)if(JointRow(i).SequenceEqual(new[]{a,b}))return i;
        int index=JointCount;joints.Append(ReferenceBits,a,b);return index;
    }
    public int[] Body(int i)=>body.Read(new(2,TemplateBits,i));
    public int[] Frame(int i)=>frames.Read(new(3,TemplateBits,i));
    public int FrameCount=>frames.Count(3,TemplateBits);
    public int[] LineRow(int i)=>lines.Read(new(2,ReferenceBits,i));
    public int[] JointRow(int i)=>joints.Read(new(2,ReferenceBits,i));
    public int[] Edge(int i)=>ink.Read(new(3,ReferenceBits,i));
    public int[] Band(int i)=>bands.Read(new(3,ReferenceBits,i));
    public int[] Lift(int i)=>lifts.Read(new(2,ReferenceBits,i));
    public byte[] BodyBytes()=>body.PackedBytes(2,TemplateBits);
    public byte[] FrameBytes()=>frames.PackedBytes(3,TemplateBits);
    public byte[] NormalBytes()=>normals.PackedBytes(2,6);
    public byte[] UnitBytes()=>units.PackedBytes(2,2);
    public byte[] LineBytes()=>lines.PackedBytes(2,ReferenceBits);
    public byte[] JointBytes()=>joints.PackedBytes(2,ReferenceBits);
    public byte[] InkBytes()=>ink.PackedBytes(3,ReferenceBits);
    public byte[] BandBytes()=>bands.PackedBytes(3,ReferenceBits);
    public byte[] LiftBytes()=>lifts.PackedBytes(2,ReferenceBits);
}

public sealed record StripVisit(int Phase,int Depth,byte FitSupport,bool Drew,bool Stopped,int? Edge,int? Joint);
