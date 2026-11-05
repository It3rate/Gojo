using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class InvarianceSurveyTests
{
    [Fact]
    public void DeclaredFiniteDomainsRetainCounterexamplesAndRejectGuessedCorrespondence()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gojo-invariance-" + Guid.NewGuid().ToString("N"));
        var rows = new Dictionary<string, int>
        {
            ["cases.csv"] = 36,
            ["addition-frames.csv"] = 64,
            ["joint-switches.csv"] = 16,
            ["collecting-inputs.csv"] = 4,
            ["merge-priority.csv"] = 4,
            ["boolean-spans.csv"] = 1600,
            ["cycle-participation.csv"] = 124
        };
        try
        {
            // Export checks the actual laws, ports and material outcomes for
            // every declared case; expected counterexamples are retained data.
            InvarianceSurvey.Export(folder);
            foreach (var (file, count) in rows)
                Assert.Equal(count + 1, File.ReadLines(Path.Combine(folder, file)).Count());
            var summary = File.ReadAllText(Path.Combine(folder, "summary.txt"));
            Assert.Contains("32 incompatible empty domains", summary);
            Assert.Contains("1 exact-state counterexample", summary);
            Assert.Contains("720 counterexamples", summary);
            Assert.Contains("same immutable equality-law instance", summary);
            Assert.Contains("All required conformance checks passed", summary);
        }
        finally
        {
            foreach (var file in rows.Keys.Append("summary.txt")) File.Delete(Path.Combine(folder, file));
            if (Directory.Exists(folder)) Directory.Delete(folder);
        }
    }
}
