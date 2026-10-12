using System.Threading;
using System.Runtime.Intrinsics;

namespace Phototaxis;

// These names select a disposable execution adapter, never a creature concept.
public enum NativeExecutionBackend { Compact, CompactSimd, Planned, Reference }

/// <summary>Host-only selection and optional measurements for a bounded run.</summary>
public sealed class NativeExecution : IDisposable
{
    private static readonly AsyncLocal<NativeExecution?> current = new();
    private readonly NativeExecution? previous;
    internal static NativeExecution? Current => current.Value;
    public NativeExecutionBackend Backend { get; }
    public long Batches { get; internal set; }
    public long PlanBuilds { get; internal set; }
    public long CacheHits { get; internal set; }
    public long DeferredBatches { get; internal set; }
    public long NumericSteps { get; internal set; }
    public long Fallbacks { get; internal set; }
    public long SimdBatches { get; internal set; }
    public long SimdLanes { get; internal set; }
    public static bool SimdSupported => Vector128.IsHardwareAccelerated;

    public NativeExecution(NativeExecutionBackend backend)
    {
        Backend = backend; previous = current.Value; current.Value = this;
    }

    public void Dispose() => current.Value = previous;
}
