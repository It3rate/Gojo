namespace Gojo.EncodingB;

/// <summary>A forecast: what a prediction workspace expects, when it was issued,
/// how far ahead, and what it assumed.</summary>
public sealed record Forecast(
    string Rows,
    long IssueVersion,
    int Horizon,
    string Assumptions,
    ulong Sites,
    IReadOnlyDictionary<string, ulong> Predicted);

/// <summary>The forecast compared with what the source then held. A mismatch is
/// a claim residual on the forecast; it does not say which premise was wrong
/// and introduces no tension.</summary>
public sealed record ForecastResolution(Forecast Forecast, long ResolvedVersion, IReadOnlyDictionary<string, ulong> Mismatch)
{
    public bool Matched => Mismatch.Values.All(m => m == 0);
}

/// <summary>A workspace that never commits. It has no commit path, so prediction
/// cannot move material by construction. It runs laws on captured copies.</summary>
public sealed class PredictionWorkspace(string name, Tile source, Selection focus) : Workspace(name, source, focus)
{
    public Forecast Forecast(LawRows rows, int horizon, string assumptions)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (horizon < 1) throw new ArgumentOutOfRangeException(nameof(horizon));
        Capture();
        var issued = ReadVersion;
        for (var step = 0; step < horizon; step++) ApplyLocal(rows, rows.AllRows);
        var sites = rows.SitesOf(rows.AllRows);
        var planes = rows.RoleFields.SelectMany(Grammar.PlanesOf).Distinct();
        return new Forecast(rows.Name, issued, horizon, assumptions, sites,
            planes.ToDictionary(plane => plane, plane => Read(plane) & sites));
    }

    public ForecastResolution Resolve(Forecast forecast)
    {
        ArgumentNullException.ThrowIfNull(forecast);
        return new ForecastResolution(forecast, Source.Version,
            forecast.Predicted.ToDictionary(p => p.Key, p => (Source.Read(p.Key) ^ p.Value) & forecast.Sites));
    }
}
