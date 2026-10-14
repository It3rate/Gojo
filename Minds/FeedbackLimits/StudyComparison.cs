namespace Minds.FeedbackLimits;

public sealed record StudyMismatch(string Scope, int Index, string Field, string Gojo, string Reference);
public static class StudyComparison
{
    public static IReadOnlyList<StudyMismatch> Compare(StudyTrace actual, StudyTrace reference)
    {
        var differences = new List<StudyMismatch>();
        CompareFrames("event", actual.Events, reference.Events);
        CompareFrames("round", actual.Rounds, reference.Rounds);
        return differences;

        void CompareFrames(string scope, IReadOnlyList<StudyFrame> a, IReadOnlyList<StudyFrame> b)
        {
            if (a.Count != b.Count) differences.Add(new(scope, -1, "count", a.Count.ToString(), b.Count.ToString()));
            for (var i = 0; i < Math.Min(a.Count, b.Count); i++)
            {
                Check("accepted", a[i].Accepted?.ToString() ?? "none", b[i].Accepted?.ToString() ?? "none");
                foreach (var key in a[i].Values.Keys.Union(b[i].Values.Keys))
                    Check(key, a[i].Values.GetValueOrDefault(key, "absent"), b[i].Values.GetValueOrDefault(key, "absent"));
                void Check(string field, string x, string y) { if (x != y) differences.Add(new(scope, i, field, x, y)); }
            }
        }
    }
}
