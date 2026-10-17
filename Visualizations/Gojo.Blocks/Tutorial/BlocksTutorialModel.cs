using Gojo.Core;
using GojoDraw;
using SkiaSharp;

namespace Gojo.Blocks;

internal sealed record BlocksTutorialPage(
    string Id,
    string Title,
    string TeachingGoal,
    string Narration,
    BlocksTutorialScene Scene,
    IReadOnlyList<GojoDrawControl> Controls,
    BlocksAiContext AiContext,
    BlocksCompletionCue Completion,
    GojoDrawAudioCue? Audio = null);

internal sealed record BlocksTutorialScene(
    IElement? Structure,
    SKRect? ModelBounds,
    int ProjectionSeed,
    TimeSpan Duration,
    IReadOnlyList<GojoDrawVisualCue> VisualCues,
    bool Loops = true) : IGojoDrawTimeline
{
    public bool HasAnimation => Duration > TimeSpan.Zero || VisualCues.Any(cue => cue.Track is not null);
}

internal sealed record BlocksAiContext(
    string PageGoal,
    IReadOnlyList<string> Concepts,
    IReadOnlyList<string> CodeReferences,
    string SelectedSource = "",
    string ReadSurface = "",
    string Result = "",
    string ResidueOrTension = "");

internal sealed record BlocksCompletionCue(
    string Description,
    string? ControlId = null,
    string? ExpectedStructure = null);
