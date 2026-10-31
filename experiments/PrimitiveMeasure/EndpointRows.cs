namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A row position within one owner's connection arrays.</summary>
public readonly record struct ConnectionRow(int Arity, int ReferenceBits, int Index);

/// <summary>Ordered local endpoint positions, packed by endpoint count and
/// reference width. A row stores no operation tag, amount, or global identity.
/// The containing structure supplies endpoint meaning and the connection law.
/// Append-only and single-threaded; rows are neither merged nor deduplicated.</summary>
public sealed class EndpointRows
{
    private readonly Bucket?[,] buckets = new Bucket?[8, 16];

    private sealed class Bucket
    {
        public byte[] Packed = [];
        public int Count;
    }

    /// <summary>Stores endpoint 0 first, low bit first within each reference.
    /// All inputs are checked before any existing bytes or row counts change.</summary>
    public ConnectionRow Append(int referenceBits, params int[] endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        CheckLayout(endpoints.Length, referenceBits);
        var maximum = (1 << referenceBits) - 1;
        foreach (var endpoint in endpoints)
            if (endpoint < 0 || endpoint > maximum)
                throw new ArgumentOutOfRangeException(nameof(endpoints), "Endpoint position does not fit the selected width.");

        var bucket = buckets[endpoints.Length - 1, referenceBits - 1] ??= new Bucket();
        var nextCount = checked(bucket.Count + 1);
        var length = checked((int)((nextCount * (long)endpoints.Length * referenceBits + 7) / 8));
        if (length != bucket.Packed.Length) Array.Resize(ref bucket.Packed, length);
        var row = new ConnectionRow(endpoints.Length, referenceBits, bucket.Count);
        var start = BitPosition(row);
        for (var endpoint = 0; endpoint < endpoints.Length; endpoint++)
            for (var bit = 0; bit < referenceBits; bit++)
            {
                var position = start + endpoint * referenceBits + bit;
                var index = checked((int)(position / 8));
                var mask = (byte)(1 << (int)(position % 8));
                bucket.Packed[index] = (byte)((bucket.Packed[index] & ~mask) |
                    (((endpoints[endpoint] >> bit) & 1) != 0 ? mask : 0));
            }
        bucket.Count = nextCount;
        return row;
    }

    /// <summary>Returns every endpoint role in order, including repeated
    /// references. A missing row or trailing byte padding is not a connection.</summary>
    public int[] Read(ConnectionRow row)
    {
        var start = BitPosition(row);
        var bucket = Find(row.Arity, row.ReferenceBits);
        if (bucket is null || row.Index >= bucket.Count)
            throw new ArgumentOutOfRangeException(nameof(row), "Connection row does not exist in this owner.");
        var endpoints = new int[row.Arity];
        for (var endpoint = 0; endpoint < endpoints.Length; endpoint++)
            for (var bit = 0; bit < row.ReferenceBits; bit++)
            {
                var position = start + endpoint * row.ReferenceBits + bit;
                if ((bucket.Packed[checked((int)(position / 8))] & (1 << (int)(position % 8))) != 0)
                    endpoints[endpoint] |= 1 << bit;
            }
        return endpoints;
    }

    public int Count(int arity, int referenceBits) => Find(arity, referenceBits)?.Count ?? 0;

    /// <summary>Diagnostic copy of payload bytes; only final-byte padding is
    /// unused. There is no header or padding between rows.</summary>
    public byte[] PackedBytes(int arity, int referenceBits) =>
        Find(arity, referenceBits)?.Packed.ToArray() ?? [];

    public static long BitPosition(ConnectionRow row)
    {
        CheckLayout(row.Arity, row.ReferenceBits);
        if (row.Index < 0) throw new ArgumentOutOfRangeException(nameof(row));
        return checked((long)row.Index * row.Arity * row.ReferenceBits);
    }

    private Bucket? Find(int arity, int referenceBits)
    {
        CheckLayout(arity, referenceBits);
        return buckets[arity - 1, referenceBits - 1];
    }

    private static void CheckLayout(int arity, int referenceBits)
    {
        if (arity is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(arity));
        if (referenceBits is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(referenceBits));
    }
}
