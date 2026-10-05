using System.Text.Json;
using Gojo.Core;
using M = NegotiatedPorts.MarketProbe;

namespace MarketTraining;

internal static class Checks
{
    public static int Run()
    {
        SourceContinuation();
        DiagnosticRelease();
        NativeResume();
        OutcomeAccounting();
        return 4;
    }

    private static void SourceContinuation()
    {
        var source = new EnvironmentSource { Seed = 47 };
        for (var i = 0; i < 117; i++) source.Next();
        var restored = JsonSerializer.Deserialize<EnvironmentSource>(JsonSerializer.Serialize(source))!;
        var unknowns = 0;
        for (var i = 0; i < 12; i++)
        {
            var a = source.Next(); var b = restored.Next();
            Require(a == b && source.Level == restored.Level && source.Position == restored.Position,
                "The external generator resumes across the explicit missing observation and regime boundary.");
            if (!a.Input.HasValue) unknowns++;
        }
        Require(unknowns == 1, "The fixture preserves the disclosed unknown observation rather than replacing it with zero.");
    }

    private static void DiagnosticRelease()
    {
        var retained = M.Create();
        var released = M.Create();
        var operational = M.Operational(released);
        foreach (var input in Inputs())
        {
            retained = Admit(retained, input, out _);
            released = Admit(released, input, out var oldFrame);
            var oldFrameId = oldFrame.Id;
            var pendingIds = M.Pending(released).Select(issue => issue.Id).ToArray();
            released = Observation.ReleaseFrame(released);
            Require(ReferenceEquals(operational, M.Operational(released)), "Release keeps the physical operational world.");
            var snapshot = GojoEncodedSnapshot.CaptureGraph(released);
            Require(!snapshot.Relations.Any(relation => relation.Id == oldFrameId) &&
                pendingIds.All(id => snapshot.Relations.Any(relation => relation.Id == id)),
                "Completed frame is absent while pending obligation structure remains reachable.");
            Require(Canonical(Observation.ReleaseFrame(retained)) == Canonical(released),
                "Removing observer evidence leaves identical native structure, state and attachment ownership.");
        }
    }

    private static void NativeResume()
    {
        var directory = Path.Combine(Checkpoint.Repository(), "experiments/MarketTraining/obj", "checkpoint-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var original = Observation.ReleaseFrame(M.Create());
        var totals = new WindowTotals();
        var inputs = Inputs();
        for (var index = 0; index < 7; index++)
        {
            original = Admit(original, inputs[index], out var frame);
            Observation.Add(totals, original, frame, "check");
            original = Observation.ReleaseFrame(original);
        }
        // Split immediately after an unknown outcome, with pending current
        // obligations and a partial seven-arrival aggregate retained separately.
        var state = new CheckpointState("check-manifest", new EnvironmentSource { Seed = 47, Position = 7, Level = 118 }, 19, totals, 1);
        Checkpoint.Save(directory, original, state);
        var (resumed, restored) = Checkpoint.Load(directory, "check-manifest");
        Require(Canonical(original) == Canonical(resumed), "Native checkpoint restores all present structure and state, not report-derived substitutes.");
        Require(JsonSerializer.Serialize(totals) == JsonSerializer.Serialize(restored.Totals) && restored.WindowFileBytes == 19 &&
            restored.Environment.Seed == 47 && restored.Environment.Position == 7,
            "Partial observation totals and external source position round trip separately from the learner.");
        var wrongManifestRejected = false;
        try { Checkpoint.Load(directory, "other-manifest"); } catch (InvalidDataException) { wrongManifestRejected = true; }
        Require(wrongManifestRejected, "Resume refuses a mismatched manifest before interpreting native state.");
        for (var index = 7; index < inputs.Length; index++)
        {
            original = Observation.ReleaseFrame(Admit(original, inputs[index], out _));
            resumed = Observation.ReleaseFrame(Admit(resumed, inputs[index], out _));
            Require(Canonical(original) == Canonical(resumed),
                "Resumed pending predictions, prices, routes, wallets, sites and future native execution equal uninterrupted continuation.");
        }
        Checkpoint.Save(directory, resumed, restored with { Environment = new EnvironmentSource { Seed = 47, Position = inputs.Length } });
        Require(Directory.GetFiles(directory, "*.checkpoint.zip").Length == 1, "Checkpoint replacement retains one native world.");
    }

    private static void OutcomeAccounting()
    {
        var root = M.Create();
        var totals = new WindowTotals();
        foreach (var input in Inputs())
        {
            root = Admit(root, input, out var frame);
            Observation.Add(totals, root, frame, "check");
            root = Observation.ReleaseFrame(root);
        }
        Require(totals.Conserved && totals.Admissions == Inputs().Length && totals.Consumers.All(t =>
            t.Demanded == Inputs().Length && t.EvaluatedDemanded + t.UnresolvedDemanded == t.PriorDemanded &&
            t.PriorDemanded == Inputs().Length - 1 && t.OfferPurchases.Sum() == t.Purchases &&
            t.PaidContacts + t.UnfundedContacts == t.Contacts &&
            t.SameSupplierConsecutiveServices + t.NewConnections + t.OfferSwitches == t.Purchases &&
            t.PositivePaid + t.ZeroPaid + t.HarmfulPaid == t.EvaluatedPaid),
            "Counters separate demanded, unresolved, no-service, paid and connection outcomes without dropping denominators.");
        Require(totals.Consumers.All(t => t.UnresolvedDemanded > 0 && t.ReadableDemanded < t.Demanded),
            "Unknown input is visible in both opportunities and later unresolved obligations.");
        Require(totals.Consumers[0].RequestedFunding == 5 * Inputs().Length &&
            totals.Consumers[1].RequestedFunding == 8 * Inputs().Length &&
            totals.Consumers.All(t => t.AdmittedFunding > 0 && t.AdmittedFunding <= t.RequestedFunding),
            "Requested inlets and native admitted resource movement are reported separately.");
    }

    private static long?[] Inputs() => [100, 103, 106, 109, 112, 115, null, 121, 124, 127, 124, 121, 121, 121, 125];
    private static Relation Admit(Relation root, long? input, out Relation frame)
    {
        Require(M.TryObserve(root, input, true, true, 5, 8, out var next, out frame), "A current finite observation is admitted.");
        Require(M.Pass(M.Conservation(next)), "Native resource accounting remains conserved.");
        return next;
    }

    internal static string Canonical(Relation root)
    {
        var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
        // Process identities are deliberately fresh on load. Table topology,
        // ordered references, aliases, attachments, units and live state compare.
        return JsonSerializer.Serialize(snapshot with
        {
            Carriers = snapshot.Carriers.Select(row => row with { Id = 0 }).ToArray(),
            TickRefs = snapshot.TickRefs.Select(row => row with { Id = 0 }).ToArray(),
            Relations = snapshot.Relations.Select(row => row with { Id = 0 }).ToArray(),
            UnitShapes = snapshot.UnitShapes.Select(row => row with { Id = 0 }).ToArray()
        });
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Offline check failed: " + message);
    }
}
