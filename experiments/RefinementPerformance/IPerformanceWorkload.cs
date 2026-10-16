using Gojo.Core;

namespace Gojo.Experiments.RefinementPerformance;

/// <summary>Diagnostic boundary; each sample owns a fresh connected workload.</summary>
public interface IPerformanceWorkload
{
    IElement Surface { get; }
    long Run(int rounds);
}
