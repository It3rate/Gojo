namespace GojoCAD.SketchViewer;

internal sealed record SketchDocument(
    IReadOnlyList<SketchScene> Scenes,
    int SelectedSceneIndex,
    int SelectedRungIndex,
    bool ShowConstructionPoints)
{
    public SketchScene SelectedScene => Scenes[Math.Clamp(SelectedSceneIndex, 0, Scenes.Count - 1)];

    public int CurrentRungIndex =>
        Math.Clamp(SelectedRungIndex, 0, SelectedScene.LastRungIndex);

    public static SketchDocument CreateInitial()
    {
        var scenes = SketchSamples.CreateInitialScenes();
        return new(scenes, 0, scenes.Count == 0 ? 0 : scenes[0].LastRungIndex, ShowConstructionPoints: false);
    }

    public SketchDocument Select(int index) =>
        this with
        {
            SelectedSceneIndex = Math.Clamp(index, 0, Scenes.Count - 1),
            SelectedRungIndex = Scenes[Math.Clamp(index, 0, Scenes.Count - 1)].LastRungIndex
        };

    public SketchDocument SelectRung(int index) =>
        this with { SelectedRungIndex = Math.Clamp(index, 0, SelectedScene.LastRungIndex) };

    public SketchDocument ToggleConstructionPoints() =>
        this with { ShowConstructionPoints = !ShowConstructionPoints };

    public SketchDocument Resample()
    {
        var scenes = SketchSamples.CreateInitialScenes();
        var sceneIndex = Math.Clamp(SelectedSceneIndex, 0, scenes.Count - 1);
        return new SketchDocument(
            scenes,
            sceneIndex,
            Math.Clamp(SelectedRungIndex, 0, scenes[sceneIndex].LastRungIndex),
            ShowConstructionPoints);
    }
}
