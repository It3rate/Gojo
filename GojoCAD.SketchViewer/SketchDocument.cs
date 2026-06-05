namespace GojoCAD.SketchViewer;

internal sealed record SketchDocument(
    IReadOnlyList<SketchScene> Scenes,
    int SelectedSceneIndex)
{
    public SketchScene SelectedScene => Scenes[Math.Clamp(SelectedSceneIndex, 0, Scenes.Count - 1)];

    public static SketchDocument CreateInitial() =>
        new(SketchSamples.CreateInitialScenes(), 0);

    public SketchDocument Select(int index) =>
        this with { SelectedSceneIndex = Math.Clamp(index, 0, Scenes.Count - 1) };

    public SketchDocument Resample()
    {
        var scenes = SketchSamples.CreateInitialScenes();
        return new SketchDocument(scenes, Math.Clamp(SelectedSceneIndex, 0, scenes.Count - 1));
    }
}
