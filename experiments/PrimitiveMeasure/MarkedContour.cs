namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A mark strip is zipped to the ordered arcs of an existing contour.
/// A zero retains its arc and skips only its landmark. Chords connect settled
/// marked sites in encounter order. Closing them requires a completed walk and
/// an explicit return request. No metric coordinates participate in the read.</summary>
public sealed class MarkedContour
{
    public ContourPattern Contour { get; }
    public ParticipationStrip Marks { get; }
    public GateWalk Walk { get; }
    public GateVisit[] Trace { get; }
    private readonly EndpointRows bindings = new(), chords = new(), seam = new();
    public int[] Selected => Trace.Where(v=>v.Completed && v.Marked).Select(v=>v.Slot).ToArray();
    public int ChordCount => chords.Count(2,Contour.ReferenceBits);

    public MarkedContour(ContourPattern contour,ParticipationStrip marks,bool close = true,int hiddenSlot = -1)
    {
        if (marks.Count!=contour.PartCount) throw new ArgumentException("One mark slot must bind each contour step.");
        Contour=contour; Marks=marks; Walk=new(marks,hiddenSlot);
        for (int i=0;i<marks.Count;i++) bindings.Append(contour.ReferenceBits,i,contour.ArcRow(i)[0]);
        Trace=Walk.Run();
        var selected=Selected;
        for (int i=1;i<selected.Length;i++) chords.Append(contour.ReferenceBits,Site(selected[i-1]),Site(selected[i]));
        if (close && Walk.Complete && selected.Length>=3)
        {
            seam.Append(contour.ReferenceBits,Site(selected[^1]),Site(selected[0]));
            chords.Append(contour.ReferenceBits,Site(selected[^1]),Site(selected[0]));
        }
    }
    public int Site(int slot) => bindings.Read(new(2,Contour.ReferenceBits,slot))[1];
    public int[] Chord(int index) => chords.Read(new(2,Contour.ReferenceBits,index));
    public byte[] BindingBytes() => bindings.PackedBytes(2,Contour.ReferenceBits);
    public byte[] ChordBytes() => chords.PackedBytes(2,Contour.ReferenceBits);
    public byte[] SeamBytes() => seam.PackedBytes(2,Contour.ReferenceBits);

    public MarkedContourRead Read()
    {
        var radial=Contour.Read(); var selected=Selected; int n=Marks.Count;
        bool closed=seam.Count(2,Contour.ReferenceBits)==1;
        bool opposed=closed && radial.Circle && radial.EqualParts && selected.Length==4 && n%2==0 &&
            selected.All(i=>selected.Contains((i+n/2)%n));
        var gaps=selected.Length<2?[]:selected.Select((slot,i)=>(selected[(i+1)%selected.Length]-slot+n)%n).ToArray();
        bool regular=closed && radial.Circle && radial.EqualParts && gaps.Distinct().Count()==1;
        return new(Walk.Complete,closed,selected.Length,opposed,opposed&&regular,regular);
    }
}

public readonly record struct MarkedContourRead(bool Complete,bool Closed,int Landmarks,
    bool Rectangle,bool Square,bool RegularPolygon);
