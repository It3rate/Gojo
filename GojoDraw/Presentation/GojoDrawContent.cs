using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Generic visual cue over Gojo structure for tutorial pages, demos, and other
/// presentation surfaces. The cue is a display request, not the source of
/// meaning.
/// </summary>
public sealed record GojoDrawVisualCue(
    string Id,
    GojoDrawVisualCueKind Kind,
    string Label,
    IElement? Source = null,
    GojoDrawVisualTrack? Track = null,
    float Strength = 1f);

public enum GojoDrawVisualCueKind
{
    Pulse,
    TensionGlow,
    ResidueChip,
    FlowArrow,
    DragHandle,
    Correspondence,
    StructureLabel
}

/// <summary>Generic control description for GojoDraw-backed content.</summary>
public sealed record GojoDrawControl(
    string Id,
    string Label,
    GojoDrawControlKind Kind,
    double Value = 0,
    double Minimum = 0,
    double Maximum = 1,
    IReadOnlyList<string>? Options = null);

public enum GojoDrawControlKind
{
    Toggle,
    Slider,
    Stepper,
    Segmented,
    Button
}

/// <summary>
/// Generic component bounds for render-boundary UI such as buttons, sliders,
/// navigation controls, and draggable handles.
/// </summary>
public sealed record GojoDrawComponent(
    string Id,
    GojoDrawComponentKind Kind,
    string Label,
    SKRect Bounds,
    bool Enabled = true);

public enum GojoDrawComponentKind
{
    Button,
    Slider,
    Toggle,
    Segmented,
    Navigation,
    DragHandle,
    Label
}
