using System.Numerics;
using System.Text.Json;

namespace Minds.Predictions;

// External evaluation only. No Gojo readers, no participant feedback. Exact
// arithmetic here checks the structural implementation, it does not supply it.
internal readonly record struct Rational
{
    public BigInteger N { get; }
    public BigInteger D { get; }
    public Rational(BigInteger n, BigInteger d)
    {
        if (d.IsZero) throw new DivideByZeroException();
        if (d.Sign < 0) { n = -n; d = -d; }
        var gcd = BigInteger.GreatestCommonDivisor(n,d); N=n/gcd; D=d/gcd;
    }
    public static Rational Parse(string s) { var p=s.Split('/'); return new(BigInteger.Parse(p[0]),p.Length==1?1:BigInteger.Parse(p[1])); }
    public static implicit operator Rational(long n) => new(n,1);
    public static Rational operator +(Rational a,Rational b) => new(a.N*b.D+b.N*a.D,a.D*b.D);
    public static Rational operator -(Rational a,Rational b) => new(a.N*b.D-b.N*a.D,a.D*b.D);
    public static Rational operator *(Rational a,Rational b) => new(a.N*b.N,a.D*b.D);
    public static Rational operator /(Rational a,Rational b) => new(a.N*b.D,a.D*b.N);
    public Rational Abs() => new(BigInteger.Abs(N),D);
    public int CompareTo(Rational b) => (N*b.D).CompareTo(b.N*D);
    public override string ToString() => D.IsOne ? N.ToString() : $"{N}/{D}";
}

public sealed record ReferenceRow(PredictionRow Affine, string? HoldLast, string? HoldLastError);
public sealed record PredictionDifference(int Index, string Field, string Actual, string Expected);
public sealed record ErrorScore(int Count, string? MeanAbsolute, string? MaxAbsolute, int ExactHits);
public sealed record PredictionSummary(int Targets, int Observed, int Missing, int Issued, int Scored,
    int AwaitingOutcome, int InsufficientHistory, int Refused, int FlatResidues,
    int HoldIssued, int HoldScored, int CommonTargets, ErrorScore AffineCommon,
    ErrorScore HoldCommon, ErrorScore HoldAll, int NoPredictionIssued, string? NoPredictionError,
    WorkCounts Calls, int[] Retained);

public static class PredictionEvaluation
{
    public static IReadOnlyList<ReferenceRow> Reference(IReadOnlyList<Sample> tape)
    {
        var history = new List<Sample>(); var rows = new List<ReferenceRow>();
        long local=0; var fit=0; var issue=0; var assess=0; var assessed=0;
        foreach (var sample in tape)
        {
            var selected = history.TakeLast(2).ToArray();
            Rational? prediction=null, hold=history.Count==0 ? null : (Rational)history[^1].Value!.Value;
            long? issueEvent=null, assessmentEvent=null; var flat=false;
            // Both forecasts are calculated using only the previous receipts.
            if(selected.Length==2)
            {
                var a=selected[0]; var b=selected[1]; fit++; issue++;
                prediction=(Rational)b.Value!.Value + ((Rational)b.Value.Value-a.Value!.Value)*
                    ((Rational)sample.Time-b.Time)/((Rational)b.Time-a.Time);
                issueEvent=local++; flat=a.Value==b.Value;
            }
            var arrival=local++;
            // The outcome becomes available here, after issuance above.
            Rational? error=null, holdError=null; bool? accepted=null;
            if(prediction is not null)
            {
                assessmentEvent=local++; assess++; accepted=sample.Value is not null;
                if(sample.Value is not null) { error=(Rational)sample.Value.Value-prediction.Value; assessed++; }
            }
            if(sample.Value is not null && hold is not null) holdError=(Rational)sample.Value.Value-hold.Value;
            var row=new PredictionRow(sample.Time,sample.Value,prediction is null?"insufficient-history":"issued",
                selected.Select(s=>s.Time).ToArray(),prediction?.ToString(),issueEvent,arrival,assessmentEvent,
                accepted,error?.ToString(),flat,selected.Length==0?null:sample.Time-selected[^1].Time,
                [rows.Count+1,issue,assessed],new(rows.Count+1,fit,issue,assess));
            rows.Add(new(row,hold?.ToString(),holdError?.ToString()));
            if(sample.Value is not null) history.Add(sample);
        }
        return rows;
    }

    public static IReadOnlyList<PredictionDifference> Compare(IReadOnlyList<PredictionRow> actual, IReadOnlyList<ReferenceRow> expected)
    {
        var differences=new List<PredictionDifference>();
        if(actual.Count!=expected.Count) differences.Add(new(-1,"count",actual.Count.ToString(),expected.Count.ToString()));
        for(var i=0;i<Math.Min(actual.Count,expected.Count);i++)
        {
            var a=JsonSerializer.SerializeToElement(actual[i]); var b=JsonSerializer.SerializeToElement(expected[i].Affine);
            foreach(var field in b.EnumerateObject())
                if(a.GetProperty(field.Name).GetRawText()!=field.Value.GetRawText())
                    differences.Add(new(i,field.Name,a.GetProperty(field.Name).GetRawText(),field.Value.GetRawText()));
        }
        return differences;
    }

    public static PredictionSummary Summarize(IReadOnlyList<PredictionRow> rows,IReadOnlyList<ReferenceRow> reference)
    {
        if(rows.Count!=reference.Count) throw new InvalidDataException("Unmatched evaluation targets.");
        var common=rows.Select((r,i)=>(r,i)).Where(x=>x.r.Error is not null && reference[x.i].HoldLastError is not null).ToArray();
        var issued=rows.Count(r=>r.Status=="issued"); var scored=rows.Count(r=>r.Error is not null);
        return new(rows.Count,rows.Count(r=>r.Outcome is not null),rows.Count(r=>r.Outcome is null),issued,scored,
            issued-scored,rows.Count(r=>r.Status=="insufficient-history"),
            rows.Count(r=>r.Status is "fit-refused" or "issue-refused" || r.Prediction is not null && r.Outcome is not null && r.AssessmentAccepted!=true),
            rows.Count(r=>r.TimeResidue),reference.Count(r=>r.HoldLast is not null),reference.Count(r=>r.HoldLastError is not null),
            common.Length,Score(common.Select(x=>x.r.Error!)),Score(common.Select(x=>reference[x.i].HoldLastError!)),
            Score(reference.Where(r=>r.HoldLastError is not null).Select(r=>r.HoldLastError!)),0,null,
            rows.LastOrDefault()?.Calls??new(0,0,0,0),rows.LastOrDefault()?.Retained??[0,0,0]);
    }

    public static ErrorScore Score(IEnumerable<string> errors)
    {
        var values=errors.Select(Rational.Parse).ToArray();
        if(values.Length==0) return new(0,null,null,0);
        Rational total=0, worst=0;
        foreach(var value in values) { var loss=value.Abs(); total+=loss; if(loss.CompareTo(worst)>0)worst=loss; }
        return new(values.Length,(total/values.Length).ToString(),worst.ToString(),values.Count(v=>v.N.IsZero));
    }
}
