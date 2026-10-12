using System.Text.Json;

namespace CompositeEmergence.Tests;

public sealed class StudyIntegrationTests
{
    [Fact]
    public void WholeStudyUsesSharedPortsAndKeepsFrozenModelsAndResourceAccountsConsistent()
    {
        var output = Path.Combine(Path.GetTempPath(), "GojoCompositeTest-" + Guid.NewGuid().ToString("N"));
        try
        {
            var settings = new StudySettings(1, 0, 320, 96, 64, 128, 128, .04, 1, true);
            var data = new StudyRunner(settings, output).Run();
            var source = Assert.Single(data.Results, row => row.Stage == "source" && row.Variant == "learned" && row.Condition == "baseline");
            Assert.Equal(1, source.Accuracy);
            Assert.Equal(1, Assert.Single(data.Results, row => row.Stage == "source" && row.Condition == "same-state").Accuracy);
            Assert.Equal(0, Assert.Single(data.Results, row => row.Stage == "source" && row.Condition == "opposite-state").Accuracy);
            Assert.Equal(0, Assert.Single(data.Results, row => row.Stage == "source" && row.Condition == "absent-state").Coverage);
            Assert.True(Assert.Single(data.Results, row => row.Stage == "source" && row.Variant == "no-expansion").Accuracy < .75);
            Assert.Equal(1, Assert.Single(data.Results, row => row.Stage == "transfer" && row.Variant == "reused-service" && row.TrainEpisodes == 32).Accuracy);
            foreach (var row in data.Results.Where(row => row.Stage != "discovery"))
            {
                var gross = 8L * row.Correct - 12L * (row.Answered - row.Correct);
                var price = row.Condition.EndsWith("expensive-probe") ? 5 : settings.ProbePrice;
                Assert.Equal(gross - (row.Stage == "refresh" ? price * row.Queries : 0), row.ReturnSum);
            }
            Assert.All(data.Archives, archive => Assert.True(archive.SignatureMatch && archive.BehaviorMatch));
            foreach (var delay in new[] { 0, 4 })
            {
                using var policy = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, $"refresh-{delay}-0.json")));
                Assert.Equal(settings.RefreshTrain, policy.RootElement.GetProperty("updateCount").GetInt64());
            }
        }
        finally
        {
            // This exact freshly created test directory is the only cleanup target.
            if (Directory.Exists(output)) Directory.Delete(output, true);
        }
    }
}
