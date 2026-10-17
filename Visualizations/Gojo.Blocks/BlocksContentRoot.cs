using System.Reflection;

namespace Gojo.Blocks;

internal sealed class BlocksContentRoot
{
    private readonly Assembly assembly;

    private BlocksContentRoot(string relativeRoot, string? physicalPath, Assembly assembly)
    {
        RelativeRoot = relativeRoot;
        PhysicalPath = physicalPath;
        this.assembly = assembly;
    }

    public string RelativeRoot { get; }

    public string? PhysicalPath { get; }

    public string ReadAllText(string relativeFile)
    {
        if (PhysicalPath is not null)
        {
            return File.ReadAllText(Path.Combine(PhysicalPath, relativeFile));
        }

        var resourceName = FindResourceName(relativeFile);
        using var stream = assembly.GetManifestResourceStream(resourceName) ??
            throw new FileNotFoundException($"Embedded content resource was not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public string AssetPath(string relativeFile) =>
        PhysicalPath is not null
            ? Path.Combine(PhysicalPath, relativeFile)
            : $"{RelativeRoot.Replace('\\', '/')}/{relativeFile.Replace('\\', '/')}";

    public static BlocksContentRoot Resolve(
        string relativeRoot,
        string sentinelFile,
        Assembly assembly,
        string failureDescription)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, relativeRoot),
            Path.Combine(Environment.CurrentDirectory, "Visualizations", "Gojo.Blocks", relativeRoot),
            Path.Combine(Environment.CurrentDirectory, relativeRoot)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, sentinelFile)))
            {
                return new BlocksContentRoot(relativeRoot, candidate, assembly);
            }
        }

        var suffixes = ResourceSuffixes(relativeRoot, sentinelFile);
        if (assembly.GetManifestResourceNames().Any(name =>
                suffixes.Any(suffix => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))))
        {
            return new BlocksContentRoot(relativeRoot, null, assembly);
        }

        throw new DirectoryNotFoundException(
            $"{failureDescription} Looked under: {string.Join("; ", candidates)}; " +
            $"embedded suffixes: {string.Join("; ", suffixes)}");
    }

    private string FindResourceName(string relativeFile)
    {
        var suffixes = ResourceSuffixes(RelativeRoot, relativeFile);
        return assembly.GetManifestResourceNames()
            .FirstOrDefault(name => suffixes.Any(suffix =>
                name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))) ??
            throw new FileNotFoundException(
                $"Could not find embedded content resource ending with one of: {string.Join("; ", suffixes)}.");
    }

    private static string ResourceSuffix(string relativeRoot, string relativeFile) =>
        Path.Combine(relativeRoot, relativeFile)
            .Replace('\\', '.')
            .Replace('/', '.');

    private static string[] ResourceSuffixes(string relativeRoot, string relativeFile)
    {
        var literal = ResourceSuffix(relativeRoot, relativeFile);
        var relativeDirectory = Path.GetDirectoryName(relativeFile);
        var directory = string.IsNullOrEmpty(relativeDirectory)
            ? relativeRoot
            : Path.Combine(relativeRoot, relativeDirectory);
        var normalizedDirectory = string.Join(
            ".",
            directory
                .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Replace('-', '_')));
        var normalized = $"{normalizedDirectory}.{Path.GetFileName(relativeFile)}";

        return string.Equals(literal, normalized, StringComparison.Ordinal)
            ? [literal]
            : [literal, normalized];
    }
}
