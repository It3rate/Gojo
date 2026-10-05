using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;

namespace FailureSubscriptions;

internal sealed record Manifest(string Format, long Seed, string Scenario, int Horizon, bool Prospects, int Rotation, string SourceFingerprint, string ProtocolFingerprint);
internal sealed record RunState(string ManifestHash, EnvironmentSource Environment, long WindowBytes, ReportTotals Totals);

internal static class RunStore
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

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
        throw new InvalidOperationException("Run from the Gojo checkout or its build directory.");
    }

    public static string Fingerprint(string repository)
    {
        var sources = Directory.EnumerateFiles(Path.Combine(repository, "experiments/FailureSubscriptions"), "*.cs")
            .Append(Path.Combine(repository, "experiments/FailureSubscriptions/FailureSubscriptions.csproj"))
            .Append(Path.Combine(repository, "experiments/ConnectionAssembly/NativeMath.cs"))
            .Concat(Directory.EnumerateFiles(Path.Combine(repository, "Gojo"), "*.cs", SearchOption.AllDirectories)
                .Where(p => !Path.GetRelativePath(repository, p).Split(Path.DirectorySeparatorChar).Any(s => s is "bin" or "obj")));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var path in sources.Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(repository, path).Replace('\\', '/') + "\n"));
            hash.AppendData(File.ReadAllBytes(path));
        }
        hash.AppendData(File.ReadAllBytes(typeof(RunStore).Assembly.Location));
        hash.AppendData(File.ReadAllBytes(typeof(Carrier).Assembly.Location));
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    public static void Save(string directory, Relation root, RunState state)
    {
        var path = Path.Combine(directory, "latest.checkpoint.zip");
        using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                using (var entry = archive.CreateEntry("native.gojo", CompressionLevel.Fastest).Open())
                    entry.Write(GojoNumericFormat.Encode(root));
                using (var entry = archive.CreateEntry("runner.json", CompressionLevel.Fastest).Open())
                    JsonSerializer.Serialize(entry, state, Json);
            }
            stream.Flush(true);
        }
        File.Move(path + ".tmp", path, true);
    }

    public static (Relation Root, RunState State) Load(string directory, string manifestHash)
    {
        using var archive = ZipFile.OpenRead(Path.Combine(directory, "latest.checkpoint.zip"));
        using var stateStream = archive.GetEntry("runner.json")!.Open();
        var state = JsonSerializer.Deserialize<RunState>(stateStream, Json)!;
        if (state.ManifestHash != manifestHash) throw new InvalidDataException("Checkpoint manifest differs.");
        using var nativeStream = archive.GetEntry("native.gojo")!.Open();
        using var buffer = new MemoryStream(); nativeStream.CopyTo(buffer);
        var roots = GojoNumericFormat.Decode(buffer.ToArray());
        if (roots is not [Relation root]) throw new InvalidDataException("Checkpoint has no native root.");
        return (root, state);
    }
}
