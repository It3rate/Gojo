namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Selected letter convention. Construction sites 0→1 and 0→2 supply
/// the positive right and up axes from a common bottom-left zero. Ink and
/// construction have separate site tables. Contacts assert zero projection.</summary>
public sealed class LetterGround
{
    private readonly EndpointRows axes = new(), travel = new(), contacts = new();
    public int ContactBits { get; }
    public int RunCount => travel.Count(2, 2);
    public LetterGround((int X, int Y)[] signs, int[] left, int[] bottom, int siteBits)
    {
        axes.Append(2, 0, 1);
        axes.Append(2, 0, 2);
        ContactBits = siteBits;
        foreach (var (x,y) in signs)
        {
            if (x == 0 && y == 0) throw new ArgumentException("A participating straight run needs a direction.");
            travel.Append(2, Word(x), Word(y));
        }
        foreach (var site in left) contacts.Append(siteBits, site, 0);
        foreach (var site in bottom) contacts.Append(siteBits, site, 1);
    }
    private static int Word(int sign) => sign switch
    {
        -1 => 3, 0 => 0, 1 => 1, _ => throw new ArgumentOutOfRangeException(nameof(sign))
    };
    public int[] Motion(int run, bool reversed = false)
    {
        var words = travel.Read(new(2, 2, run));
        if (reversed)
            for (var i = 0; i < words.Length; i++)
                if ((words[i] & 1) != 0) words[i] ^= ThreeBitMeasure.Direction;
        return words;
    }
    public int[][] ZeroLinks() => Enumerable.Range(0, contacts.Count(2, ContactBits))
        .Select(i => contacts.Read(new(2, ContactBits, i))).ToArray();
    public byte[] AxisBytes() => axes.PackedBytes(2, 2);
    public byte[] MotionBytes() => travel.PackedBytes(2, 2);
    public byte[] ContactBytes() => contacts.PackedBytes(2, ContactBits);
}
