using ScratchMill.KiCad.Elements;
using ScratchMill.Machine;
using SkiaSharp;

namespace ScratchMill.GCode;

public class ScratchJobs
{
    public CarveSettings CarveSettings { get; } = new();

    public SKMatrix FrontTransform { get; set; } = SKMatrix.Identity;
    public SKMatrix BackTransform { get; set; } = SKMatrix.Identity;

    public List<SKPath> ABaseTrackPaths { get; } = [];
    public List<List<TrackData>> ATrackContoursJob { get; } = [];
    public List<List<TrackData>> ACleanupJob { get; } = [];
    public List<TrackData> AMaskJob { get; } = [];
    public List<TrackData> APasteJob { get; } = [];
    public List<GraphicData> AGraphics { get; } = [];
    public List<GraphicData> AEdgeScratchesJob { get; } = [];
    public List<GraphicData> ARouterCutJob { get; } = [];

    public List<SKPath> BBaseTrackPaths { get; } = [];
    public List<List<TrackData>> BTrackContoursJob { get; } = [];
    public List<List<TrackData>> BCleanupJob { get; } = [];
    public List<TrackData> BMaskJob { get; } = [];
    public List<TrackData> BPasteJob { get; } = [];
    public List<GraphicData> BGraphics { get; } = [];
    public List<GraphicData> BEdgeScratchesJob { get; } = [];
    public List<GraphicData> BRouterCutJob { get; } = [];

    public List<DrillData> DrillJob { get; } = [];

    public KcNetTracks? AProbePad { get; private set; }
    public KcNetTracks? BProbePad { get; private set; }

    public SKMatrix GetTransformForSide(BoardSide side) =>
        side.IsNotBottom() ? FrontTransform : BackTransform;

    public List<SKPath> GetBaseTrackPathsForSide(BoardSide side) =>
        side.IsNotBottom() ? ABaseTrackPaths : BBaseTrackPaths;

    public List<List<TrackData>> GetTrackContoursForSide(BoardSide side) =>
        side.IsNotBottom() ? ATrackContoursJob : BTrackContoursJob;

    public List<List<TrackData>> GetCleanupForSide(BoardSide side) =>
        side.IsNotBottom() ? ACleanupJob : BCleanupJob;

    public List<TrackData> GetMasksForSide(BoardSide side) =>
        side.IsNotBottom() ? AMaskJob : BMaskJob;

    public List<GraphicData> GetGraphicsForSide(BoardSide side) =>
        side.IsNotBottom() ? AGraphics : BGraphics;

    public List<GraphicData> GetEdgeScratchesForSide(BoardSide side) =>
        side.IsNotBottom() ? AEdgeScratchesJob : BEdgeScratchesJob;

    public void AddBasePaths(bool isTop, IEnumerable<SKPath> paths)
    {
        if (isTop)
        {
            ABaseTrackPaths.AddRange(paths);
        }
        else
        {
            BBaseTrackPaths.AddRange(paths);
        }
    }

    public void AddContourTracks(bool isTop, List<TrackData> tracks)
    {
        if (isTop)
        {
            ATrackContoursJob.Add(tracks);
        }
        else
        {
            BTrackContoursJob.Add(tracks);
        }
    }

    public void AddCleanupTracks(bool isTop, List<TrackData> tracks)
    {
        if (isTop)
        {
            ACleanupJob.Add(tracks);
        }
        else
        {
            BCleanupJob.Add(tracks);
        }
    }

    public void AddMaskRemoval(bool isTop, List<TrackData> masks)
    {
        if (isTop)
        {
            AMaskJob.AddRange(masks);
        }
        else
        {
            BMaskJob.AddRange(masks);
        }
    }

    public void AddGraphics(bool isTop, List<GraphicData> graphics)
    {
        if (isTop)
        {
            AGraphics.AddRange(graphics);
        }
        else
        {
            BGraphics.AddRange(graphics);
        }
    }

    public void AddEdgeScratches(bool isTop, List<GraphicData> edgeScratches)
    {
        if (isTop)
        {
            AEdgeScratchesJob.AddRange(edgeScratches);
        }
        else
        {
            BEdgeScratchesJob.AddRange(edgeScratches);
        }
    }

    public void AddRouterCuts(bool isTop, List<GraphicData> edgeCuts)
    {
        if (isTop)
        {
            ARouterCutJob.AddRange(edgeCuts);
        }
        else
        {
            BRouterCutJob.AddRange(edgeCuts);
        }
    }

    public void AddProbePad(bool isTop, KcNetTracks probePad)
    {
        if (isTop)
        {
            AProbePad = probePad;
        }
        else
        {
            BProbePad = probePad;
        }
    }

    public SKRect GetBounds()
    {
        var result = SKRect.Empty;

        foreach (var bounds in AEdgeScratchesJob
            .Concat(BEdgeScratchesJob)
            .Select(edge => edge.Bounds)
            .Concat(ATrackContoursJob.SelectMany(batch => batch).Select(track => track.Bounds))
            .Concat(BTrackContoursJob.SelectMany(batch => batch).Select(track => track.Bounds))
            .Where(bounds => !bounds.IsEmpty))
        {
            result = result.IsEmpty ? bounds : SKRect.Union(result, bounds);
        }

        return result.IsEmpty ? new SKRect(0, 0, 100, 100) : result;
    }

    public void Clear()
    {
        ABaseTrackPaths.Clear();
        ATrackContoursJob.Clear();
        ACleanupJob.Clear();
        AMaskJob.Clear();
        APasteJob.Clear();
        AGraphics.Clear();
        AEdgeScratchesJob.Clear();
        ARouterCutJob.Clear();

        BBaseTrackPaths.Clear();
        BTrackContoursJob.Clear();
        BCleanupJob.Clear();
        BMaskJob.Clear();
        BPasteJob.Clear();
        BGraphics.Clear();
        BEdgeScratchesJob.Clear();
        BRouterCutJob.Clear();

        DrillJob.Clear();
        AProbePad = null;
        BProbePad = null;
    }
}
