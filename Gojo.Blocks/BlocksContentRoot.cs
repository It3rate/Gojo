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
            Path.Combine(Environment.CurrentDirectory, "Gojo.Blocks", relativeRoot),
            Path.Combine(Environment.CurrentDirectory, relativeRoot)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, sentinelFile)))
            {
                return new BlocksContentRoot(relativeRoot, candidate, assembly);
            }
        }

        var suffix = ResourceSuffix(relativeRoot, sentinelFile);
        if (assembly.GetManifestResourceNames().Any(name =>
                name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            return new BlocksContentRoot(relativeRoot, null, assembly);
        }

        throw new DirectoryNotFoundException(
            $"{failureDescription} Looked under: {string.Join("; ", candidates)}; " +
            $"embedded suffix: {suffix}");
    }

    private string FindResourceName(string relativeFile)
    {
        var suffix = ResourceSuffix(RelativeRoot, relativeFile);
        return assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) ??
            throw new FileNotFoundException(
                $"Could not find embedded content resource ending with '{suffix}'.");
    }

    private static string ResourceSuffix(string relativeRoot, string relativeFile) =>
        Path.Combine(relativeRoot, relativeFile)
            .Replace('\\', '.')
            .Replace('/', '.');
}
