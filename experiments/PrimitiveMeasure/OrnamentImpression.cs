using System.Globalization;
using System.Text;

namespace Gojo.Experiments.PrimitiveMeasure;

public readonly record struct PenTrace(int Cell,int Tool,string Path,int Strokes,bool Nested);
public readonly record struct PlatePass(int Cell,int Stage,OrnamentCell Witness,int ChildCell=-1);

/// <summary>Calibrated plate read. Selection and arrival have already settled.
/// Each path is built from the actual tool's site references.</summary>
public sealed class OrnamentImpression
{
    public OrnamentDesign Design { get; }
    public OrnamentCell[] Cells { get; }
    public byte[] Tape { get; }
    public byte Join { get; }
    public IReadOnlyList<PlatePass> Timeline { get; }
    public int Passes => Timeline.Count;
    public OrnamentImpression(OrnamentDesign design,int rhythm,byte join)
    {
        Design=design; Tape=OrnamentPress.Strip(design,rhythm); Join=join;
        Cells=OrnamentPress.Print(design,Tape,join);
        var passes=new List<PlatePass>();
        foreach (var cell in Cells)
        {
            for (int i=0;i<3;i++) passes.Add(new(cell.Position,i,cell));
            if (cell.Finial && design.Inlay is {} socket)
            {
                var child=OrnamentPress.Print(socket.Child,socket.Child.HomeTape,8);
                foreach (var petal in child)
                    for (int i=0;i<3;i++) passes.Add(new(cell.Position,i,petal,petal.Position));
            }
        }
        Timeline=passes.AsReadOnly();
    }

    public sealed class View
    {
        public List<PenTrace> Paths { get; }=[];
        public List<(PenPoint Fork,PenPoint Eye)> Landmarks { get; }=[];
        internal View(OrnamentImpression print,int passes,bool oneCell)
        {
            var d=print.Design;
            if (passes<0 || passes>print.Passes) throw new ArgumentOutOfRangeException(nameof(passes));
            int focus=passes==0?0:print.Timeline[passes-1].Cell;
            int first=oneCell?focus:0, end=oneCell?first+1:d.Count;
            // The complete possible design fixes the viewport throughout playback,
            // even when a gate holds the very first impression.
            var bounds=new List<PenPoint>();
            for (int i=first;i<end;i++)
                for (int site=0;site<d.SiteCount;site++)
                    bounds.Add(d.Place(i,d.Point(site)));
            double xmin=bounds.Min(p=>p.X),xmax=bounds.Max(p=>p.X);
            double ymin=bounds.Min(p=>p.Y),ymax=bounds.Max(p=>p.Y);
            double scale=Math.Min(665/Math.Max(1,xmax-xmin),368/Math.Max(1,ymax-ymin));
            PenPoint Project(int cell,PenPoint local)
            {
                var p=d.Place(cell,local);
                return new(360+scale*(p.X-(xmin+xmax)/2),232+scale*(p.Y-(ymin+ymax)/2));
            }
            foreach (var pass in print.Timeline.Take(passes))
            {
                int i=pass.Cell;
                if (i<first || i>=end) continue;
                bool nested=pass.ChildCell>=0;
                var body=nested?d.Inlay!.Value.Child:d;
                PenPoint Site(int s)
                {
                    var local=body.Point(s);
                    if (nested)
                    {
                        var origin=d.Point(d.Inlay!.Value.Anchor);
                        var transformed=body.Place(pass.ChildCell,local);
                        local=new(origin.X+transformed.X,origin.Y+transformed.Y);
                    }
                    return Project(i,local);
                }
                if (pass.Stage==0 && !nested) Landmarks.Add((Site(d.Fork),Site(d.Eye)));
                var cell=pass.Witness;
                int tools=pass.Stage switch { 0=>1, 1=>cell.Arms<<1, _=>cell.Finial?8:0 };
                for (int tool=0;tool<4;tool++)
                {
                    if ((tools & (1<<tool))==0) continue;
                    var path=new StringBuilder();
                    int strokes=0;
                    foreach (int arity in new[]{2,4})
                    {
                        int previous=-1;
                        for (int r=0;r<body.RowCount(tool,arity);r++)
                        {
                            var row=body.Stroke(tool,arity,r);
                            if (row[0]!=previous) AppendPoint(path,'M',Site(row[0]));
                            for (int k=1;k<arity;k++)
                                AppendPoint(path,k==1?(arity==2?'L':'C'):' ',Site(row[k]));
                            previous=row[^1]; strokes++;
                        }
                    }
                    if (strokes>0) Paths.Add(new(i,tool,path.ToString(),strokes,nested));
                }
            }
        }
        private static void AppendPoint(StringBuilder path,char command,PenPoint p) =>
            path.Append(command).Append(p.X.ToString("0.###",CultureInfo.InvariantCulture)).Append(' ')
                .Append(p.Y.ToString("0.###",CultureInfo.InvariantCulture));
    }

    public View Draw(int passes,bool oneCell=false) => new(this,passes,oneCell);
    public string Svg()
    {
        var view=Draw(Passes);
        var svg=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 720 464\" role=\"img\"><title>")
            .Append(Design.Name).Append("</title><desc>").Append(Design.Instruction)
            .Append("</desc><g fill=\"none\" stroke=\"#183b39\" stroke-linecap=\"round\" stroke-linejoin=\"round\">\n");
        foreach (var path in view.Paths)
            svg.Append("<path data-cell=\"").Append(path.Cell).Append("\" data-nested=\"").Append(path.Nested?1:0).Append("\" data-tool=\"").Append(path.Tool)
                .Append("\" stroke-width=\"").Append(path.Tool==0?"0.85":"1.18").Append("\" d=\"")
                .Append(path.Path).Append("\"/>\n");
        return svg.Append("</g></svg>\n").ToString();
    }
}
