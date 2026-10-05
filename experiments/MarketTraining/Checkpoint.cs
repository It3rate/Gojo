using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using M = NegotiatedPorts.MarketProbe;

namespace MarketTraining;

internal sealed record Manifest(string Format, string Generator, string Policy, long Seed, int Window,
    int Checkpoint, string SourceFingerprint, string ProtocolFingerprint, DateTimeOffset CreatedUtc);
internal sealed record CheckpointState(string ManifestHash, EnvironmentSource Environment, long WindowFileBytes,
    WindowTotals Totals, long Checkpoints);

internal static class Checkpoint
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static readonly JsonSerializerOptions Compact = new();

    public static string Repository()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Gojo.slnx"))) return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new InvalidOperationException("Run from a Gojo checkout or its built project directory.");
    }

    public static string SourceFingerprint(string repository)
    {
        var sources = new[] { "experiments/NegotiatedPorts/MarketProbe.cs", "experiments/ConnectionAssembly/NativeMath.cs" }
            .Select(path => Path.Combine(repository, path))
            .Concat(Directory.EnumerateFiles(Path.Combine(repository, "experiments/MarketTraining"), "*.cs", SearchOption.TopDirectoryOnly))
            .Append(Path.Combine(repository, "experiments/MarketTraining/MarketTraining.csproj"))
            .Concat(Directory.EnumerateFiles(Path.Combine(repository, "Gojo"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(repository, path).Split(Path.DirectorySeparatorChar)
                    .Any(part => part.Equals("obj", StringComparison.OrdinalIgnoreCase) || part.Equals("bin", StringComparison.OrdinalIgnoreCase))));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var source in sources.Distinct().Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(repository, source).Replace('\\', '/') + "\n"));
            hash.AppendData(File.ReadAllBytes(source));
        }
        // Source promise and executing build are both checked. --no-build with a
        // changed source cannot silently masquerade as the same checkpoint ABI.
        hash.AppendData(File.ReadAllBytes(typeof(Checkpoint).Assembly.Location));
        hash.AppendData(File.ReadAllBytes(typeof(Carrier).Assembly.Location));
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public static void Save(string directory, Relation root, CheckpointState state)
    {
        var path = Path.Combine(directory, "latest.checkpoint.zip");
        var temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                using (var entry = archive.CreateEntry("native.gojo", CompressionLevel.Fastest).Open())
                    entry.Write(GojoNumericFormat.Encode(root));
                using (var entry = archive.CreateEntry("runner.json", CompressionLevel.Fastest).Open())
                    JsonSerializer.Serialize(entry, state, Json);
            }
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporary, path, overwrite: true);
    }

    public static (Relation Root, CheckpointState State) Load(string directory, string manifestHash)
    {
        using var archive = ZipFile.OpenRead(Path.Combine(directory, "latest.checkpoint.zip"));
        using var stateStream = archive.GetEntry("runner.json")!.Open();
        var state = JsonSerializer.Deserialize<CheckpointState>(stateStream, Json) ?? throw new InvalidDataException("Missing runner state.");
        if (state.ManifestHash != manifestHash) throw new InvalidDataException("Checkpoint manifest differs from the immutable run manifest.");
        using var nativeStream = archive.GetEntry("native.gojo")!.Open();
        using var bytes = new MemoryStream();
        nativeStream.CopyTo(bytes);
        var roots = GojoNumericFormat.Decode(bytes.ToArray());
        if (roots is not [Relation root] || M.Version(root) != state.Environment.Position)
            throw new InvalidDataException("Native version and external source position disagree.");
        if (!M.Pass(M.Conservation(root))) throw new InvalidDataException("Loaded native resource conservation does not read true.");
        return (root, state);
    }
}
