namespace Gojo.Experiments.PrimitiveRuntime.Viewer;

// Presentation only. This snapshot is made on refresh, never used for execution.
internal sealed record InspectionView(
    long Step,
    bool Reversed,
    bool Calibrated,
    bool Open,
    ulong Mask,
    string[] Landmarks,
    double[]? Coordinates,
    ChoiceView[] Choices,
    string[] SiteLabels,
    string Phase,
    string Receipt,
    string[] ReceiptLines,
    string[] StorageLines,
    string[] MetricLines,
    int DomainSize);

internal readonly record struct ChoiceView(int Landmark, bool Forward, string Label);
