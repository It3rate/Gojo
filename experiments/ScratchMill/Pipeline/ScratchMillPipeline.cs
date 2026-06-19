using Gojo.Core;
using ScratchMill.GCode;
using ScratchMill.GojoImport;
using ScratchMill.KiCad;
using ScratchMill.KiCad.Graphics;
using ScratchMill.Utils;
using SkiaSharp;

namespace ScratchMill.Pipeline;

public sealed class ScratchMillPipelineOptions
{
    public SKMatrix GeometryTransform { get; set; } = SKMatrix.Identity;
    public SKMatrix CncTransform { get; set; } = SKMatrix.Identity;
    public float[] TrackContourOffsets { get; set; } = [0.15f, 0.19f];
    public bool IncludeBackCopper { get; set; } = true;
    public bool IncludeEdgeCuts { get; set; } = true;
    public bool IncludeDrills { get; set; } = true;
}

public sealed record ScratchMillPipelineResult(
    KcPcb Pcb,
    Relation GojoStructure,
    ScratchJobs Jobs,
    string GCode);

public static class ScratchMillPipeline
{
    public static ScratchMillPipelineResult ConvertFile(string filePath, ScratchMillPipelineOptions? options = null)
    {
        var pcb = KiCadParser.Parse(filePath);
        return ConvertPcb(pcb, options);
    }

    public static ScratchMillPipelineResult ConvertPcb(KcPcb pcb, ScratchMillPipelineOptions? options = null)
    {
        options ??= new ScratchMillPipelineOptions();

        var gojoStructure = KiCadGojoImporter.ImportPcb(pcb);
        var jobs = BuildJobs(pcb, options);
        var gcode = new GCodeWriter(jobs).WriteAll();

        return new ScratchMillPipelineResult(pcb, gojoStructure, jobs, gcode);
    }

    public static ScratchJobs BuildJobs(KcPcb pcb, ScratchMillPipelineOptions? options = null)
    {
        options ??= new ScratchMillPipelineOptions();

        var jobs = new ScratchJobs
        {
            FrontTransform = options.CncTransform,
            BackTransform = options.CncTransform
        };

        AddTracks(pcb, "F.Cu", isTop: true, jobs, options);
        if (options.IncludeBackCopper)
        {
            AddTracks(pcb, "B.Cu", isTop: false, jobs, options);
        }

        if (options.IncludeEdgeCuts)
        {
            AddEdgeCuts(pcb, jobs, options);
        }

        if (options.IncludeDrills)
        {
            AddDrills(pcb, jobs, options);
        }

        return jobs;
    }

    private static void AddTracks(KcPcb pcb, string layerName, bool isTop, ScratchJobs jobs, ScratchMillPipelineOptions options)
    {
        if (pcb.GetLayer(layerName) is null)
        {
            return;
        }

        var tracks = new List<TrackData>();
        foreach (var track in pcb.GetTracksForLayer(layerName).Values)
        {
            if (track.Elements.Count == 0)
            {
                continue;
            }

            var trackData = new TrackData(track, options.GeometryTransform, PassKind.Track);
            trackData.GenerateContours(options.TrackContourOffsets);
            tracks.Add(trackData);
        }

        if (tracks.Count > 0)
        {
            jobs.AddContourTracks(isTop, tracks);
        }
    }

    private static void AddEdgeCuts(KcPcb pcb, ScratchJobs jobs, ScratchMillPipelineOptions options)
    {
        var edgeCuts = pcb.GetGraphicsForLayers("Edge.Cuts");
        if (edgeCuts.Count == 0)
        {
            return;
        }

        var edgeData = new GraphicData(edgeCuts.Elements, options.GeometryTransform, PassKind.EdgeCut);
        edgeData.GeneratePath();
        jobs.AddEdgeScratches(isTop: true, [edgeData]);
    }

    private static void AddDrills(KcPcb pcb, ScratchJobs jobs, ScratchMillPipelineOptions options)
    {
        foreach (var drill in pcb.GetPadDrillElements().Cast<IKcDrillable>().Concat(pcb.GetViaDrillElements()))
        {
            var location = drill.At.SKPoint();
            var path = new SKPath();
            path.AddCircle(location.X, location.Y, (float)drill.DrillSize / 2f);
            jobs.DrillJob.Add(new DrillData(path, location, (float)drill.DrillSize, drill));
        }
    }
}
