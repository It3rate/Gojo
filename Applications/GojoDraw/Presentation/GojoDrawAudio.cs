namespace GojoDraw;

/// <summary>
/// Audio cue metadata for presentation hosts. Desktop and web hosts can map
/// the same cue to their platform audio backend.
/// </summary>
public sealed record GojoDrawAudioCue(
    string Id,
    string Source,
    GojoDrawAudioCueKind Kind,
    TimeSpan Offset = default,
    bool Loop = false,
    double Volume = 1);

public interface IGojoDrawAudioPlayer
{
    ValueTask PlayAsync(GojoDrawAudioCue cue, CancellationToken cancellationToken = default);

    ValueTask StopAsync(string cueId, CancellationToken cancellationToken = default);
}

public sealed class GojoDrawNullAudioPlayer : IGojoDrawAudioPlayer
{
    public ValueTask PlayAsync(GojoDrawAudioCue cue, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    public ValueTask StopAsync(string cueId, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}

public enum GojoDrawAudioCueKind
{
    Narration,
    Effect,
    Ambience
}
