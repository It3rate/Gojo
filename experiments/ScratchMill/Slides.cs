//namespace MathDemo;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Threading.Tasks;
//using Gojo.Core;
//using ScratchMill;
//using ScratchMill.Comm;
//using ScratchMill.GCode;
//using ScratchMill.KiCad;
//using ScratchMill.KiCad.Elements;
//using ScratchMill.KiCad.Graphics;
//using ScratchMill.Machine;
//using ScratchMill.Macros;
//using ScratchMill.Properties;
//using ScratchMill.Utils;
//using SkiaSharp;

//public class Slides : DemoBase
//{
//	private Brain Brain { get; }
//	private KcPcb? _pcb;

//	public static string UserPath = "";
//	public static string UserTargetDir = "";

//	public GrblComm Comm { get; private set; }
//	public GrblSender Sender { get; private set; }
//    public ScratchJobs Jobs => Sender.Jobs;
//    private SKRect _jobBounds => Sender.Jobs.JobBounds;
//    public GCodeWriter? GCodeWriter => Sender.gCodeWriter;
//	public CarveSettings CarveSettings => Jobs.CarveSettings;

//    private static float _skiaScale = 1f;
//    public static SKMatrix Transform = SKMatrix.Identity;
//    public static float SkiaRotation = (float)(Math.PI / 12.0);

//    public Slides(Brain brain, GrblComm comm)
//	{
//		Brain = brain;
//		Comm = comm;
//		Sender = new GrblSender(comm);

//		SKWorkspaceMapper.DefaultWorkspaceGhostText = CorePens.GetText(SKColor.Parse("#B0C0D0"), 18);
//		SKWorkspaceMapper.DefaultWorkspaceText = CorePens.GetText(SKColor.Parse("#3030A0"), 18);
//		Pages.AddRange(new PageCreator[]
//		{
//			PCBTest,
//		});
//	}

//	private SKWorkspaceMapper PCBTest()
//	{
//		var wm = new SKWorkspaceMapper(_currentMouseAgent, 20, 35, 600, 250);
//		wm.ShowAll();
//		var path = Settings.Default.lastPath;
//		if (path == null || path == "")
//		{
//			path = @"F:\edev\Repos\eProjects\frogClamp\circuits\mosfetSwitches\mosfetSwitches\mosfetSwitches.kicad_pcb";
//		}
//		var dir = Path.GetDirectoryName(path);
//		UserTargetDir = dir ?? UserTargetDir;
//		ParseNewProject(path);
//        return wm;
//	}

//	private void Reset()
//	{
//		Jobs.Clear();
//		Jobs.JobBounds = SKRect.Empty;
//	}
//	public KcPcb? ParseNewProject(string path, bool rememberPath = true)
//	{
//		KcPcb? result = null;
//		if (Path.Exists(path))
//		{
//			if (!path.EndsWith(".kicad_pcb"))
//			{
//				var folderPath = Path.GetDirectoryName(path);
//				if (folderPath != null)
//				{
//					var files = Directory.GetFiles(folderPath, "*.kicad_pcb");

//					if (files.Length > 0)
//					{
//						path = files[0];
//					}
//				}
//			}

//			if (rememberPath)
//			{
//				UserPath = path;
//				var dir = Path.GetDirectoryName(path);
//				UserTargetDir = dir ?? UserTargetDir;
//				result = ParsePath(UserPath);

//				Settings.Default.lastPath = UserPath;
//				Settings.Default.Save();
//			}
//			else
//			{
//				result = ParsePath(UserPath);
//			}
//		}
//		return result;
//	}
//	public void SetNewOutputFolder(string targetFolder)
//	{
//		UserTargetDir = targetFolder;
//		if (!Path.Exists(UserTargetDir))
//		{
//			var dir = Path.GetDirectoryName(targetFolder);
//			UserTargetDir = dir ?? UserTargetDir;
//		}
//		ParsePath(UserPath);
//	}
//	public KcPcb? ParsePath(string path)
//	{
//		if (!Path.Exists(path))
//		{
//			return null;
//		}
//		Reset();
//		_pcb = KiCadParser.Parse(path);
//		if (_pcb != null)
//		{
//			var (r0, r1) = _pcb.GetEdgeBoundsBL_TR();
//			var bnds = new SKRect((float)r0.X, (float)r0.Y, (float)r1.X, (float)r1.Y);

//			Transform = SkMatrixExtension.GetMatrix(_skiaScale, _pcb.OriginCenter.SKPoint().Negate(), 0);

//			for(int layer = 0; layer < 2; layer++)
//            {
//				var boardSide = layer == 0 ? BoardSide.Front : BoardSide.Back;
//				var isTop = boardSide.IsNotBottom();
//				var sidePrefix = boardSide.Prefix();
//                var layerName = $"{sidePrefix}.Cu";

//                var copperTracks = _pcb.GetTracksForLayer(layerName);

//                var contourPass = RenderTrackPaths(CarveSettings.ContourPass.ContourOffsets, copperTracks.Values, PassKind.Track);
//				Jobs.AddContourTracks(isTop, contourPass);
//				Jobs.AddBasePaths(isTop, contourPass.Select(t => t.BasePath));

//                var cleanupPass = RenderTrackPaths(CarveSettings.BurrPass.ContourOffsets, copperTracks.Values, PassKind.BurrRemoval);
//                Jobs.AddCleanupTracks(isTop, cleanupPass);

//				var continuityTests = GetContinuityTests(boardSide); // todo: all elements should use board side, as can be pattern test, etc.
//				Jobs.AddContinuityTests(isTop, continuityTests);

//				var maskLayer = $"{sidePrefix}.Mask";

//				var probePad = KcPad.CreateProbePad(1f, -1f, 2f, maskLayer); // clean outer place to probe drill height
//				probePad.FootprintOffset = r0;
//				var probePadTrack = new KcNetTracks([probePad]);
//				Jobs.AddProbePad(isTop, probePadTrack);

//				var maskTracks = _pcb.GetTracksForLayer(maskLayer);
//				maskTracks.Add(probePad.NetId, probePadTrack);
//                var maskPass = RenderMaskPaths(CarveSettings.MaskRemovalPass.ContourOffsets[0], maskTracks.Values);
//                Jobs.AddMaskRemoval(isTop, maskPass);

//				var edgeScratches = _pcb.GetGraphicsForLayers("Edge.Cuts");
//				var edgeScratchesPass = RenderGraphicPaths(edgeScratches, PassKind.EdgeCut, 2);
//				Jobs.AddEdgeScratches(isTop, edgeScratchesPass);


//				var routerEdgePass = edgeScratches.ToSkiaPath(SKMatrix.Identity);// RenderGraphicPaths(edgeScratches, PassKind.RouterCut, 1);
//				var cutContour = TrackData.GetContourPath([routerEdgePass], SKMatrix.Identity, 1.5f); // assume 3mm bit for now
//				var cutContourData = new GraphicData(edgeScratches, SKMatrix.Identity, PassKind.RouterCut);
//				Jobs.AddRouterCuts(isTop, [cutContourData]);
//			}
//            var drillHoles = _pcb.GetPadDrillElements();// _skiaScale, true);
//            RenderPadDrillHoles(drillHoles, Jobs.DrillJob);
//			Jobs.JobBounds = Jobs.GetBounds();
//		}
//		return _pcb;
//	}

//	private List<TrackData> RenderTrackPaths(float[] millOffsets, IEnumerable<KcNetTracks> tracks, PassKind passKind)
//	{
//		var store = new List<TrackData>();
//		foreach (var track in tracks)
//		{
//			var trackData = new TrackData(track, Transform, passKind);
//			trackData.GenerateContours(millOffsets);
//			store.Add(trackData);
//		}
//		return store;
//	}
//	private List<TrackData> RenderMaskPaths(float bitOffset, IEnumerable<KcNetTracks> maskTracks)
//	{
//		var store = new List<TrackData>();
//		foreach (var mask in maskTracks)
//		{
//			var maskData = new TrackData(mask, Transform, PassKind.SolderMask);
//			maskData.OverlapTail = false;
//			maskData.GenerateFillPath(bitOffset);
//			store.Add(maskData);
//			// todo: need to spiral the masks
//		}
//		return store;
//	}
//	private List<TrackData> GetContinuityTests(BoardSide boardSide)
//	{
//		var result = new List<TrackData>();
//		foreach (var trackDataPass in Jobs.GetTrackContoursForSide(boardSide))
//		{
//			foreach (var trackData in trackDataPass)
//			{
//				if (trackData != null && trackData.TestPadCenter != null)
//				{
//					result.Add(trackData);
//				}

//			}
//		}
//		return result;
//	}
//	private List<GraphicData> RenderGraphicPaths(KcGraphics graphics, PassKind passKind, int passes = 1)
//	{
//		var result = new List<GraphicData>();
//		for (int passIndex = 0; passIndex < passes; passIndex++)
//		{
//			foreach (var gr in graphics.Elements)
//			{
//				var graphicData = new GraphicData(gr, Transform, passKind, passIndex);
//				graphicData.GeneratePath();
//				result.Add(graphicData);
//			}
//		}
//		return result;
//	}
//	private void RenderPadDrillHoles(List<KcPad> drillHoles, List<DrillData> store)
//	{
//		var result = new List<DrillData>();
//		foreach (var drillHole in drillHoles)
//		{
//			var data = new DrillData(drillHole.PadHolePath(), drillHole.HoleLocation, drillHole.HoleSize, drillHole);
//			store.Add(data);
//		}
//	}
//	private void RenderViaDrillHoles(List<IKcDrillable> drillHoles, List<(double, SKPoint)> store)
//	{
//		var drillMatrix = SkMatrixExtension.GetMatrix(_skiaScale, SKPoint.Empty, 0);
//		foreach (var drillHole in drillHoles)
//		{
//			var curPt = drillHole.At.SKPointTransformed(drillMatrix);
//			//maskData.Track.ToSvg();
//			store.Add((drillHole.DrillSize * drillMatrix.ScaleX, drillHole.At.SKPoint()));
//		}
//	}

//	protected override void OnDrawingComplete(object? sender, CanvasEventArgs e)
//	{
//		base.OnDrawingComplete(sender, e);

//		var canvas = e.Canvas;
//		canvas.Save();

//		var boardSide = Jobs.Side;
//		canvas.GetDeviceClipBounds(out var clipBounds);

//		var ep = Jobs.GetEdgeProbeForSide(boardSide);
//		var rr = -ep.RotationRadians;

//		SKRotatedRect rrect = ep.ValidCorners; // pre rotated if needed
//		var jobSize = rrect.Size();

//		var leftDockWidth = MainView.Instance!.pnLeft.Width;
//		var rightDockWidth = MainView.Instance!.pnRight.Width;
//		var dockWidth = leftDockWidth + rightDockWidth;
//		var renderAreaSize = new SKSize(clipBounds.Width - dockWidth, clipBounds.Height);
//		var renderCenter = new SKPoint(renderAreaSize.Width / 2 + leftDockWidth, renderAreaSize.Height / 2);

//		float scale = Math.Min(
//			(renderAreaSize.Width - 40) / jobSize.Width * .85f,
//			(renderAreaSize.Height - 40) / jobSize.Height * .85f);

//		var isTopFactor = boardSide.IsNotBottom() ? 1f : -1f;
//		var xform = SKMatrix.Identity;
//		xform = xform.PreConcat(SKMatrix.CreateTranslation(renderCenter.X, renderCenter.Y));
//		if(boardSide.IsNotBottom())
//		{
//			xform = xform.PreConcat(SKMatrix.CreateRotation(rr * isTopFactor));
//			xform = xform.PreConcat(SKMatrix.CreateScale(scale * isTopFactor, scale));
//		}
//		else
//		{
//			xform = xform.PreConcat(SKMatrix.CreateRotation(-rr * isTopFactor));
//			xform = xform.PreConcat(SKMatrix.CreateScale(scale * isTopFactor, scale));
//		}
//		canvas.SetMatrix(xform);

//		int key = 0;
//		foreach (var trackPath in Jobs.GetBaseTrackPathsForSide(boardSide))
//		{
//			var col = _colors[key++];
//			float darkness = 0.8f;
//			_fillPaint.Color = new SKColor((byte)(col.Red * darkness), (byte)(col.Green * darkness), (byte)(col.Blue * darkness), 140);

//			key = key >= _colors.Count ? 0 : key;
//			canvas.DrawPath(trackPath, _fillPaint);
//		}

//		if (Jobs.IncludeTrackPaths)
//		{
//			foreach (var trackDataPass in Jobs.GetTrackContoursForSide(boardSide))
//			{
//				foreach (var trackData in trackDataPass)
//				{
//					canvas.DrawPath(trackData.BasePath, _contourPaint);
//				}
//			}
//		}
//		if (Jobs.IncludeCleanupPaths)
//		{
//			foreach (var trackDataPass in Jobs.GetCleanupForSide(boardSide))
//			{
//				foreach (var trackData in trackDataPass)
//				{
//					canvas.DrawPath(trackData.BasePath, _contourPaint);
//				}
//			}
//		}

//		if (Jobs.IncludeMaskRemovalPaths)
//		{
//			foreach (var maskData in Jobs.GetMasksForSide(boardSide))
//			{
//				canvas.DrawPath(maskData.OutlinePath, _maskFillPaint);

//				foreach (var mask in maskData.PathContours)
//				{
//					canvas.DrawPath(mask, _maskPaint);
//				}
//			}
//		}

//		if (Jobs.IncludeDrillPaths)
//		{
//			foreach (var drill in Jobs.DrillJob)
//			{
//				canvas.DrawPath(drill.BasePath, _drillPaint);
//				canvas.DrawPath(drill.BasePath, _edgePaint);
//				canvas.DrawLine(
//					new SKPoint(drill.BasePath.Bounds.MidX, drill.BasePath.Bounds.Top),
//					new SKPoint(drill.BasePath.Bounds.MidX, drill.BasePath.Bounds.Bottom),
//					_drillOutline);
//				canvas.DrawLine(
//					new SKPoint(drill.BasePath.Bounds.Left, drill.BasePath.Bounds.MidY),
//					new SKPoint(drill.BasePath.Bounds.Right, drill.BasePath.Bounds.MidY),
//					_drillOutline);
//			}
//		}

//		if (Jobs.IncludeRecuts)
//		{
//			var testPointJob = Jobs.GetContinuityTestsForSide(boardSide);
//			var toRecut = testPointJob.TracksToRecut;
//			foreach (var tp in testPointJob.GetTests())
//			{
//				if(tp.TestPadCenter != null)
//				{
//					_testPointPaint.Color = toRecut.Contains(tp) ? 
//						new SKColor(230, 22, 22, 200) : new SKColor(22, 128, 22, 200);
//					canvas.DrawCircle(tp.TestPadCenter!.Value, 0.2f, _testPointPaint);
//				}
//			}
//		}
//		if (Jobs.IncludeEdgeCutsPaths)
//		{
//			foreach (var edgeCut in Jobs.GetEdgeScratchesForSide(boardSide))
//			{
//				canvas.DrawPath(edgeCut.BasePath, _edgePaint);
//			}

//		}

//		if (!boardSide.IsNotBottom())
//		{
//			xform = SKMatrix.Identity;
//			xform = xform.PreConcat(SKMatrix.CreateTranslation(renderCenter.X, renderCenter.Y));
//			xform = xform.PreConcat(SKMatrix.CreateRotation(-rr * isTopFactor));
//			xform = xform.PreConcat(SKMatrix.CreateScale(-scale * isTopFactor, scale));
//			canvas.SetMatrix(xform);
//		}
//		var bnds = Jobs.GetBounds();

//		var textLine = boardSide.IsNotBottom() ? 
//			new SKSegment(bnds.BottomLeft(), bnds.BottomRight()).LeftToRight().ShiftOffLine(30f / scale) :
//			new SKSegment(bnds.BottomRight(), bnds.BottomLeft()).LeftToRight().ShiftOffLine(30f / scale);
//		DrawText(canvas, textLine);

//		var corner = Jobs.GetEdgeProbeForSide(boardSide).ChosenCorner.State;
//		if (corner != RectDirections.None)
//		{
//			var fpt = bnds.CornerFromOrientaion(corner);
//			DrawFiducial(canvas, fpt, scale, 16f);

//			var margin = CarveSettings.Margin;
//			var edgeBnds = Jobs.GetBounds();
//			edgeBnds.Inflate(margin, margin);
//			var edges = edgeBnds.CornerLinesFromOrientaion(corner);
//			canvas.DrawLine(edges.Item1.StartPoint, edges.Item1.EndPoint, _copperPaint);
//			canvas.DrawLine(edges.Item2.StartPoint, edges.Item2.EndPoint, _copperPaint);
//		}

//		canvas.Restore();

//		if (Jobs.IncludeAutolevel && Jobs.GetAutolevelForSide(boardSide).HasAutolevelData)
//		{
//			canvas.Save(); // Autolevel data is in board view, based on 0,0 of the intital probe

//			var xform2 = SKMatrix.Identity;
//				xform2 = xform2.PreConcat(SKMatrix.CreateTranslation(
//					renderCenter.X - rrect.Center.X * scale, 
//					renderCenter.Y + rrect.Center.Y * scale));
//			if (boardSide.IsNotBottom())
//			{
//				xform2 = xform2.PreConcat(SKMatrix.CreateScale(scale * isTopFactor, scale));
//			}
//			else
//			{
//				xform2 = xform2.PreConcat(SKMatrix.CreateScale(-scale * isTopFactor, scale));
//			}
//			canvas.SetMatrix(xform2);

//			var al = Jobs.GetAutolevelForSide(boardSide);
//			var range = al.ZMax - al.ZMin;
//			var rowCount = al.RowCount;

//			foreach (var ptData in al.SerpentineIterator)
//			{
//				var row =  ptData.Item1;
//				var column = ptData.Item2;
//				var pt = ptData.Item3;
//				var drawRow = row;

//				if(al.HasValueAt(row, column))
//				{
//					var z = al.HeightMap[drawRow, column];
//					var color = (byte)((z / range) * 255);
//                    var r = (z < 0) ? color : (byte)0;
//                    var b = (z > 0) ? color : (byte)0;
//                    _fillPaint.Color = new SKColor(r, 0, b, 80);
//					canvas.DrawCircle(pt.X, -pt.Y, 0.25f, _fillPaint);
//				}
//				else
//                {
//                    _fillPaint.Color = new SKColor(0, 0, 0, 30);
//                    canvas.DrawCircle(pt.X, -pt.Y, 0.4f, _contourPaint);
//                }
//			}
//			canvas.Restore();
//		}
//    }

//    private void DrawFiducial(SKCanvas canvas, SKPoint pt, float scale = 1f, float size = 16f)
//    {
//        canvas.DrawCircle(pt, (size / 2f) / scale, _markerPaint);
//        canvas.DrawLine(-size / scale + pt.X, +pt.Y, size / scale + pt.X, +pt.Y, _markerPaint);
//        canvas.DrawLine(pt.X, -size / scale + pt.Y, pt.X, size / scale + pt.Y, _markerPaint);
//    }
//    private void DrawText(SKCanvas canvas, SKSegment baseLine)
//    {
//		var width = _jobBounds.Width.ToString("0");
//		var height = _jobBounds.Height.ToString("0");
//		var txPaint = CorePens.GetText(SKColors.DarkBlue, 0.7f);
//		canvas.DrawText($"Width: {width}mm      Height: {height}mm", baseLine.StartPoint, txPaint);
//    }
//    SKPaint _fillPaint = new SKPaint
//	{
//		Style = SKPaintStyle.Fill,
//		Color = SKColors.DarkBlue,
//		IsAntialias = true,
//	};
//	SKPaint _contourPaint = new SKPaint
//	{
//		Color = new SKColor(24, 64, 200, 128),
//		//Color = SKColors.Red,
//		Style = SKPaintStyle.Stroke,
//		StrokeCap = SKStrokeCap.Round,
//		StrokeJoin = SKStrokeJoin.Round,
//		StrokeWidth = 0.04f * Transform.ScaleX,
//		IsAntialias = true,
//	};
//	SKPaint _testPointPaint = new SKPaint
//	{
//		Color = new SKColor(22, 128, 22, 200),
//		Style = SKPaintStyle.Stroke,
//		StrokeCap = SKStrokeCap.Round,
//		StrokeJoin = SKStrokeJoin.Round,
//		StrokeWidth = 0.15f * Transform.ScaleX,
//		IsAntialias = true,
//	};
//	SKPaint _maskPaint = new SKPaint
//	{
//		Color = new SKColor(150, 150, 0, 160),
//		//PathEffect = SKPathEffect.CreateCorner(2f),
//		Style = SKPaintStyle.Stroke,
//		StrokeCap = SKStrokeCap.Round,
//		StrokeJoin = SKStrokeJoin.Round,
//		StrokeWidth = 0.04f * Transform.ScaleX,
//		IsAntialias = true,
//	};
//	SKPaint _maskFillPaint = new SKPaint
//	{
//		Style = SKPaintStyle.Fill,
//		Color = new SKColor(255, 255, 255, 128),
//		IsAntialias = true,
//	};
//	SKPaint _markerPaint = new SKPaint
//	{
//		Color = SKColors.Red,
//		Style = SKPaintStyle.Stroke,
//		StrokeCap = SKStrokeCap.Round,
//		StrokeJoin = SKStrokeJoin.Round,
//		StrokeWidth = 0.08f * Transform.ScaleX,
//		IsAntialias = true,
//	};
//	SKPaint _drillOutline = new SKPaint
//	{
//		Color = new SKColor(64, 64, 64, 128),
//		Style = SKPaintStyle.Stroke,
//		StrokeCap = SKStrokeCap.Round,
//		StrokeJoin = SKStrokeJoin.Round,
//		StrokeWidth = 0.08f * Transform.ScaleX,
//		IsAntialias = true,
//	};
//	SKPaint _drillPaint = new SKPaint
//	{
//		Style = SKPaintStyle.Fill,
//		Color = new SKColor(255, 255, 255, 255),
//		IsAntialias = true,
//	};
//	SKPaint _edgePaint = new SKPaint
//	{
//		Color = SKColors.DarkSlateBlue,
//		Style = SKPaintStyle.Stroke,
//		StrokeCap = SKStrokeCap.Round,
//		StrokeJoin = SKStrokeJoin.Round,
//		StrokeWidth = 0.07f * Transform.ScaleX,
//		IsAntialias = true,
//	};
//	SKPaint _copperPaint = new SKPaint
//	{
//		Color = new SKColor(184, 115, 51, 32),
//		Style = SKPaintStyle.Stroke,
//		StrokeCap = SKStrokeCap.Round,
//		StrokeJoin = SKStrokeJoin.Round,
//		StrokeWidth = 0.1f * Transform.ScaleX,
//		IsAntialias = true,
//	};

//	private List<SKColor> _colors = new List<SKColor>() {
//			SKColors.DarkRed,
//			SKColors.Red,
//			SKColors.Orange,
//			SKColors.DarkGoldenrod,
//			SKColors.YellowGreen,
//			SKColors.Green,
//			SKColors.Cyan,
//			SKColors.Blue,
//			SKColors.Violet,
//			SKColors.Gray,
//			SKColors.Brown,
//			SKColors.DarkCyan,
//			SKColors.DarkKhaki,
//			SKColors.Coral,
//		};
//}
