namespace Gojo.EncodingC;

/// <summary>Selected positions of one structure, copied into a structure of
/// their own with the same planes. Laws run on a workspace exactly as on any
/// structure. Nothing reaches the source until <see cref="Commit"/>, and a
/// commit is refused when any bit the workspace read has changed in the source
/// since. Positions introduced here have no source; a commit adds them to it.
/// A workspace that is never committed is a prediction.
/// <para>A structure holds what is. A workspace holds what is noticed,
/// supposed or attempted: a selection, local work and candidates.</para></summary>
public sealed class Workspace
{
    private readonly Structure source;
    private readonly List<int> localOf;
    private readonly PackedInts sourceOf = new();
    private Plane[] read = [];
    private bool[] readConstant = [];

    public Workspace(Structure source, Plane selection)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.Length != source.Count) throw new ArgumentException("A selection has one bit per source position.", nameof(selection));
        if (!source.Live.Covers(selection)) throw new ArgumentException("Only live positions can be selected.", nameof(selection));
        this.source = source;
        Selection = selection.Clone();
        Local = new Structure(0);
        for (var p = 0; p < source.PlaneCount; p++)
        {
            if (source.IsConstantPlane(p)) Local.AddConstantPlane(source.ConstantValue(p));
            else Local.AddPlane();
        }
        localOf = Enumerable.Repeat(-1, source.Count).ToList();
        foreach (var position in selection.Indexes())
        {
            var local = Local.Add();
            localOf[position] = local;
            sourceOf.Add(position + 1);
            for (var p = 0; p < source.PlaneCount; p++)
                if (!source.IsConstantPlane(p)) Local.Write(p, local, source.Read(p, position));
        }
        Capture();
    }

    /// <summary>The workspace's own structure. Wire laws to it to work here.</summary>
    public Structure Local { get; }

    /// <summary>The selected source positions.</summary>
    public Plane Selection { get; }

    public int? SourceOf(int local) => sourceOf[local] is var s and > 0 ? s - 1 : null;

    public int? LocalOf(int sourcePosition) =>
        (uint)sourcePosition < (uint)localOf.Count && localOf[sourcePosition] >= 0 ? localOf[sourcePosition] : null;

    /// <summary>Adds a local position with no source: a candidate, such as a
    /// proposed closing site. It is never read as evidence about the source.</summary>
    public int Introduce()
    {
        var local = Local.Add();
        sourceOf.Add(0);
        return local;
    }

    /// <summary>Local positions with no source.</summary>
    public Plane Introduced => Plane.Of(Local.Count, Enumerable.Range(0, Local.Count).Where(i => SourceOf(i) is null));

    /// <summary>Runs a source wiring here. Rows whose positions are all selected
    /// are copied onto the workspace; a row that touches an unselected position
    /// cannot run here, because the law would see only part of what it accounts
    /// for. Those rows are returned as missing.</summary>
    public (Wiring Local, Plane Missing) Map(Wiring sourceWiring)
    {
        ArgumentNullException.ThrowIfNull(sourceWiring);
        foreach (var binding in sourceWiring.Bindings)
            if (!ReferenceEquals(binding.Surface, source))
                throw new ArgumentException("Every role must be bound to the workspace's source structure.", nameof(sourceWiring));
        var local = new Wiring(sourceWiring.Law, sourceWiring.Bindings.Select(b => new Binding(Local, b.Planes.ToArray())).ToArray());
        var missing = new Plane(sourceWiring.Count);
        foreach (var row in sourceWiring.Live.Indexes())
        {
            var mapped = sourceWiring.Row(row).Select(LocalOf).ToArray();
            if (mapped.Any(m => m is null)) missing[row] = true;
            else local.Add(mapped.Select(m => m!.Value).ToArray());
        }
        return (local, missing);
    }

    /// <summary>Writes the workspace's changes back, if nothing it read has
    /// changed in the source. Because a stale commit is refused, writing back
    /// the bits the workspace's laws produced is the same as running those laws
    /// on the source. Introduced positions are added to the source.</summary>
    public Commit Commit()
    {
        var stale = new Plane(source.Count);
        var constantsMoved = false;
        for (var p = 0; p < source.PlaneCount; p++)
        {
            if (source.IsConstantPlane(p))
            {
                constantsMoved |= source.ConstantValue(p) != readConstant[p];
                continue;
            }
            for (var local = 0; local < read[p].Length; local++)
                if (SourceOf(local) is int position && source.Read(p, position) != read[p][local]) stale[position] = true;
        }
        if (constantsMoved) stale = stale.Or(Selection);
        if (stale.Any) return new Commit(false, stale, new Plane(source.Count), []);

        var written = new Plane(source.Count);
        for (var p = 0; p < source.PlaneCount; p++)
        {
            if (source.IsConstantPlane(p))
            {
                if (Local.ConstantValue(p) != readConstant[p]) source.SetConstant(p, Local.ConstantValue(p));
                continue;
            }
            for (var local = 0; local < read[p].Length; local++)
                if (SourceOf(local) is int position && Local.Read(p, local) != read[p][local])
                {
                    source.Write(p, position, Local.Read(p, local));
                    written[position] = true;
                }
        }
        var adopted = new List<int>();
        foreach (var local in Introduced.Indexes())
        {
            var position = source.Add();
            for (var p = 0; p < source.PlaneCount; p++)
                if (!source.IsConstantPlane(p)) source.Write(p, position, Local.Read(p, local));
            sourceOf[local] = position + 1;
            while (localOf.Count < position) localOf.Add(-1);
            localOf.Add(local);
            adopted.Add(position);
        }
        Capture();
        return new Commit(true, new Plane(source.Count), written, adopted);
    }

    private void Capture()
    {
        read = Enumerable.Range(0, Local.PlaneCount).Select(p => Local.Read(p)).ToArray();
        readConstant = Enumerable.Range(0, Local.PlaneCount).Select(p => Local.IsConstantPlane(p) && Local.ConstantValue(p)).ToArray();
        while (localOf.Count < source.Count) localOf.Add(-1);
    }
}

/// <summary>A commit's result. <see cref="Stale"/> marks source positions whose
/// bits changed since the workspace read them; a stale commit writes nothing.
/// <see cref="Written"/> marks source positions changed. <see cref="Adopted"/>
/// lists the source positions added for introduced ones.</summary>
public sealed record Commit(bool Committed, Plane Stale, Plane Written, IReadOnlyList<int> Adopted);
