namespace Gojo.EncodingC;

/// <summary>Where one value sits in a structure: a shape, and the planes that
/// hold its bits, low bit first. A field is a position in the layout, not a
/// name. The same planes can be read as different fields by different
/// readers. A field reads any surface that shows those planes: the structure,
/// a rung over it (where a write is a supposition), or a port.</summary>
public sealed class Field
{
    private readonly int[] planes;

    public Field(Shape shape, params int[] planes)
    {
        ArgumentNullException.ThrowIfNull(planes);
        if (planes.Length != shape.Bits)
            throw new ArgumentException($"A shape of {shape.Size} values needs {shape.Bits} planes.", nameof(planes));
        if (planes.Distinct().Count() != planes.Length) throw new ArgumentException("A plane holds one bit of a field.", nameof(planes));
        Shape = shape;
        this.planes = planes.ToArray();
    }

    public Shape Shape { get; }

    public IReadOnlyList<int> Planes => Array.AsReadOnly(planes);

    public static Field Bit(int plane) => new(Shape.Bit, plane);

    /// <summary>The bit pattern at a position, low bit first.</summary>
    public ulong Pattern(Surface surface, int position)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ulong pattern = 0;
        for (var k = 0; k < planes.Length; k++)
            if (surface.Read(planes[k], position)) pattern |= 1UL << k;
        return pattern;
    }

    /// <summary>The value at a position, or null when the bits are not a code
    /// of the shape.</summary>
    public int? Read(Surface surface, int position) => Shape.Decode(Pattern(surface, position));

    /// <summary>Writes a value: into a structure, or into a rung as a
    /// supposition. A bit on a constant plane must already agree; every such
    /// bit is checked before anything is written. A port is not written this
    /// way: only a law that moves what it takes writes another structure.</summary>
    public void Write(Surface surface, int position, int value)
    {
        ArgumentNullException.ThrowIfNull(surface);
        if (surface.ThroughPort || surface.ReadOnly)
            throw new InvalidOperationException("A value is written into a structure or a rung. A port is written only by a law that moves what it takes.");
        var pattern = Shape.Encode(value);
        for (var k = 0; k < planes.Length; k++)
        {
            var bit = ((pattern >> k) & 1) != 0;
            if (surface.IsConstant(planes[k]) && surface.Read(planes[k], position) != bit)
                throw new ArgumentException($"Plane {planes[k]} is constant {(bit ? 0 : 1)} across the structure.", nameof(value));
        }
        for (var k = 0; k < planes.Length; k++)
            if (!surface.IsConstant(planes[k])) surface.Set(planes[k], position, ((pattern >> k) & 1) != 0);
    }
}
