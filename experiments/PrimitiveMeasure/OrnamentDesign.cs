namespace Gojo.Experiments.PrimitiveMeasure;

public readonly record struct PenPoint(double X,double Y);

/// <summary>Local sites carry a drawing calibration. Arity-two rows are pen lines,
/// arity-four rows are cubic strokes. Each of four physical tables belongs to
/// one tool. Equal drawing coordinates never create a binding.</summary>
public sealed class OrnamentDesign
{
    private readonly List<PenPoint> sites = [];
    private readonly EndpointRows[] tools = [new(),new(),new(),new()];
    public string Name { get; }
    public string Instruction { get; }
    public IReadOnlyList<byte> HomeTape { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int Count => Columns*Rows;
    public ContourPattern? Rim { get; }
    public BorderScore? HookScore { get; }
    public int PitchX { get; }
    public int PitchY { get; }
    public int Drop { get; }
    public bool ReflectRows { get; }
    public int Fork { get; }
    public int Eye { get; }
    public (int Anchor,OrnamentDesign Child)? Inlay { get; private set; }
    internal void Attach(int anchor,OrnamentDesign child)
    {
        if ((uint)anchor>=sites.Count || Inlay.HasValue || ReferenceEquals(this,child) || child.Inlay.HasValue)
            throw new ArgumentException("This two-level press accepts one local socket and one leaf press.");
        Inlay=(anchor,child);
    }

    internal OrnamentDesign(string name,string instruction,byte[] tape,PenPoint fork,PenPoint eye,
        int columns,int rows,int pitchX,int pitchY,int drop=0,bool reflectRows=false,ContourPattern? rim=null,BorderScore? hookScore=null)
    {
        Name=name; Instruction=instruction; HomeTape=Array.AsReadOnly(tape);
        Columns=columns; Rows=rows; PitchX=pitchX; PitchY=pitchY; Drop=drop; ReflectRows=reflectRows; Rim=rim; HookScore=hookScore;
        Fork=Site(fork.X,fork.Y); Eye=Site(eye.X,eye.Y);
    }
    internal int Site(double x,double y) { sites.Add(new(x,y)); return sites.Count-1; }
    public int SiteCount => sites.Count;
    public PenPoint Point(int site) => sites[site];
    internal void Connect(int tool,params int[] ends) => tools[tool].Append(8,ends);
    public int RowCount(int tool,int arity) => tools[tool].Count(arity,8);
    public int[] Stroke(int tool,int arity,int row) => tools[tool].Read(new(arity,8,row));
    public byte[] StrokeBytes(int tool,int arity) => tools[tool].PackedBytes(arity,8);

    public PenPoint Place(int cell,PenPoint p)
    {
        int col=cell%Columns, row=cell/Columns;
        if (Rim is {} rim)
        {
            double t=2*Math.PI*col/rim.PartCount-Math.PI/2;
            return new(p.X*Math.Cos(t)-p.Y*Math.Sin(t),p.X*Math.Sin(t)+p.Y*Math.Cos(t));
        }
        return new(col*PitchX+p.X,row*PitchY+(col%2)*Drop+(ReflectRows && row%2==1 ? -p.Y : p.Y));
    }
}

// Authoring shorthand for binding an outline to one tool. Reflections share
// the explicitly supplied fork and eye. Other sites are minted locally.
internal sealed class OrnamentPen(OrnamentDesign design,int tool,int mx=1,int my=1)
{
    public int P(double x,double y) => design.Site(mx*x,my*y);
    public void To(int a,int b) => design.Connect(tool,a,b);
    public int Line(int a,double x,double y) { int b=P(x,y); To(a,b); return b; }
    public void CurveTo(int a,double x1,double y1,double x2,double y2,int b) =>
        design.Connect(tool,a,P(x1,y1),P(x2,y2),b);
    public int Curve(int a,double x1,double y1,double x2,double y2,double x,double y)
    {
        int b=P(x,y); CurveTo(a,x1,y1,x2,y2,b); return b;
    }
    public int Walk(int start,params (int X,int Y)[] points)
    {
        foreach (var (x,y) in points) start=Line(start,x,y);
        return start;
    }
    public void Leaf(int from,int to,double width,bool vein=false)
    {
        var a=design.Point(from); var b=design.Point(to);
        double x=mx*a.X,y=my*a.Y, dx=mx*(b.X-a.X),dy=my*(b.Y-a.Y);
        double length=Math.Sqrt(dx*dx+dy*dy), nx=-dy*width/length,ny=dx*width/length;
        CurveTo(from,x+.30*dx+nx,y+.30*dy+ny,x+.79*dx+nx,y+.79*dy+ny,to);
        CurveTo(to,x+.80*dx-nx,y+.80*dy-ny,x+.28*dx-nx,y+.28*dy-ny,from);
        if (vein) To(from,to);
    }
}
