namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A common sampled read of two independently advancing structures.
/// The body publishes a local parameter/phase participation; a driver appends
/// formal placement translations. Each sample binds that participation to the
/// then-current exposed placement. No world coordinates or material are added.</summary>
public sealed class MovingFramePattern
{
    public RepeatedBody Body { get; } = new([0, 1], 4, continuous: true);
    public ReferenceCycle Driver { get; } = new(2);
    private readonly EndpointRows translations = new();
    private readonly EndpointRows samples = new();
    public int Placement { get; private set; } = 1;
    public int SampleCount => samples.Count(3, 8);

    public MovingFramePattern() => Body.BindTurns(RepetitionExamples.Contour(4, true, false));

    public BodyStep Pulse()
    {
        if (Placement >= 254) throw new InvalidOperationException("Retained chart is full.");
        var step = Body.Pulse();
        if (step.Parameter is int parameter) samples.Append(8, parameter, step.Phase, Placement);
        if (Driver.Circuit.Pulse().Moved)
        {
            translations.Append(8, Placement, Placement + 1, 0);
            Placement++;
        }
        return step;
    }

    public int[] Sample(int index) => samples.Read(new(3, 8, index));
    public byte[] SampleBytes() => samples.PackedBytes(3, 8);
    public byte[] TranslationBytes() => translations.PackedBytes(3, 8);
}
