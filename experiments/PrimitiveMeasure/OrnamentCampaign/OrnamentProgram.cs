using System.Collections.ObjectModel;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

/// <summary>A and B are owner-local port positions. Bit a+2b of Truth selects the
/// Boolean output for two known positive-context participation words.</summary>
public readonly record struct SensorRow(byte A, byte B, byte Truth);
public readonly record struct WriterRow(byte A, byte B, byte Truth, byte From, byte To);

/// <summary>Two independent ordered read tables joined by one thin port.
/// The last sensor output is exported. Each writer row also has a material
/// endpoint pair. Equal endpoints publish the read without an associated edit.
/// Flags: bit 0 reads the ordered frame backwards; bit 1 inserts a delay.</summary>
public sealed class OrnamentProgram
{
    public const byte Version = 1;
    public ReadOnlyCollection<SensorRow> Sensor { get; }
    public ReadOnlyCollection<WriterRow> Writer { get; }
    public byte Flags { get; }

    public OrnamentProgram(SensorRow[] Sensor, WriterRow[] Writer, byte Flags = 0)
    {
        ArgumentNullException.ThrowIfNull(Sensor);
        ArgumentNullException.ThrowIfNull(Writer);
        if (Sensor.Length is < 1 or > 4 || Writer.Length is < 1 or > 4 || Flags > 3)
            throw new ArgumentException("Supply one to four rows per owner and the two declared frame flags.");
        for (var i = 0; i < Sensor.Length; i++)
        {
            var row = Sensor[i];
            if (row.A >= 16 + i || row.B >= 16 + i || row.Truth > 15)
                throw new ArgumentException("Sensor rows can read only published inputs and earlier local rows.", nameof(Sensor));
        }
        for (var i = 0; i < Writer.Length; i++)
        {
            var row = Writer[i];
            if (row.A >= 8 + i || row.B >= 8 + i || row.Truth > 15 || row.From > 3 || row.To > 3)
                throw new ArgumentException("Writer rows require earlier input positions and two material roles.", nameof(Writer));
        }
        this.Sensor = Array.AsReadOnly((SensorRow[])Sensor.Clone());
        this.Writer = Array.AsReadOnly((WriterRow[])Writer.Clone());
        this.Flags = Flags;
    }

    public OrnamentMachine Compile() => new(this);

    /// <summary>Version, row counts minus one plus flags, then packed truth
    /// nibbles, sensor read pairs (5 bits/ref), writer read pairs (4 bits/ref)
    /// and writer passage endpoints (2 bits/ref). Each table pads its end.</summary>
    public byte[] Encode()
    {
        var truth = new EndpointRows();
        var sensor = new EndpointRows();
        var writer = new EndpointRows();
        var passages = new EndpointRows();
        foreach (var row in Sensor)
        {
            truth.Append(4, row.Truth);
            sensor.Append(5, row.A, row.B);
        }
        foreach (var row in Writer)
        {
            truth.Append(4, row.Truth);
            writer.Append(4, row.A, row.B);
            passages.Append(2, row.From, row.To);
        }
        return [Version, (byte)((Sensor.Count - 1) | (Writer.Count - 1) << 2 | Flags << 4),
            .. truth.PackedBytes(1, 4), .. sensor.PackedBytes(2, 5),
            .. writer.PackedBytes(2, 4), .. passages.PackedBytes(2, 2)];
    }

    public static OrnamentProgram Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2 || bytes[0] != Version || (bytes[1] & 0xC0) != 0)
            throw new FormatException("Invalid ornament block header.");
        var ns = 1 + (bytes[1] & 3);
        var nw = 1 + ((bytes[1] >> 2) & 3);
        var flags = (byte)(bytes[1] >> 4);
        var truthBytes = (ns + nw + 1) / 2;
        var sensorBytes = (10 * ns + 7) / 8;
        var materialBytes = (nw + 1) / 2;
        if (bytes.Length != 2 + truthBytes + sensorBytes + nw + materialBytes)
            throw new FormatException("The packed rows must exactly fill their declared block.");
        var laws = bytes.Slice(2, truthBytes);
        var sensors = bytes.Slice(2 + truthBytes, sensorBytes);
        var writers = bytes.Slice(2 + truthBytes + sensorBytes, nw);
        var transfers = bytes[^materialBytes..];
        var sr = new SensorRow[ns];
        var wr = new WriterRow[nw];
        for (var i = 0; i < ns; i++)
            sr[i] = new(Field(sensors, 2 * i, 5), Field(sensors, 2 * i + 1, 5), Field(laws, i, 4));
        for (var i = 0; i < nw; i++)
            wr[i] = new(Field(writers, 2 * i, 4), Field(writers, 2 * i + 1, 4), Field(laws, ns + i, 4),
                Field(transfers, 2 * i, 2), Field(transfers, 2 * i + 1, 2));
        var program = new OrnamentProgram(sr, wr, flags);
        if (!bytes.SequenceEqual(program.Encode())) throw new FormatException("Unused padding contains participation.");
        return program;
    }

    private static byte Field(ReadOnlySpan<byte> source, int index, int width)
    {
        int bit = index * width;
        int pair = source[bit / 8];
        if ((bit & 7) + width > 8) pair |= source[bit / 8 + 1] << 8;
        return (byte)((pair >> (bit & 7)) & ((1 << width) - 1));
    }
}
