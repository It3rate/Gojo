using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Gojo.Core;
using Gojo.Structures;
using GojoCAD.GCode;
using GojoCAD.Structures;

namespace GojoCAD.SketchViewer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-scenes", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var scene in SketchSamples.CreateInitialScenes())
            {
                ValidateGeneratedScene(scene);
            }

            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();

    private static void ValidateGeneratedScene(SketchScene scene)
    {
        if (scene.OffsetStudy is { } offsetStudy)
        {
            ValidateOffsetStudyScene(scene, offsetStudy);
            return;
        }

        if (scene.Structure is Relation program &&
            GCodeStructure.TryReadProgram(program, out var basis, out var programElements) &&
            GCodeStructure.TryReadBasis(basis, out _))
        {
            ValidateGCodeScene(scene, programElements);
            return;
        }

        if (scene.Structure is not Relation ladder ||
            !StructuralLadderSurface.TryReadRungPath(ladder, out var rungs) ||
            rungs.Length == 0)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not read as a structural ladder.");
        }

        if (!StructuralLadderSurface.TryReadTimeline(ladder, out var timeline) ||
            !TimeStepSequence.TryRead(timeline, out _, out _, out var steps))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose a readable timeline.");
        }

        if (steps.Length != rungs.Length - 1)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a timeline/rung count mismatch.");
        }

        if (scene.Label == "Radial Shape Ladder")
        {
            ValidateRadialShapeLadder(scene, ladder, rungs);
        }

        foreach (var step in steps)
        {
            if (!StructuralLadderSurface.TryReadTransformStep(
                    step,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var application,
                    out _,
                    out _) ||
                !TransformSurface.TryReadApplication(
                    application,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var patch,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a non-transform ladder step.");
            }

            if (!TransformSurface.TryReadPatch(
                    patch,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a transform step without a readable patch.");
            }

            if (!TransformSurface.TryCreateOperationOutputRead(
                    new Carrier(),
                    application,
                    Relation.Point(Carrier.Unknown, 0),
                    Relation.Point(Carrier.Unknown, 0),
                    out var operationOutput) ||
                !TransformSurface.TryReadOperationOutput(
                    operationOutput,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a transform step without a readable operation output.");
            }
        }

        var normalizedParameterReads = 0;
        var templateInstances = 0;
        foreach (var relation in WalkRelations(ladder))
        {
            if (GraphicParameterSurface.TryReadNormalizedValue(
                    relation,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                normalizedParameterReads++;
            }

            if (GraphicParameterSurface.TryReadTemplateInstance(
                    relation,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                templateInstances++;
            }
        }

        if (scene.Label == "House Drawing Ladder" &&
            (normalizedParameterReads < 39 || templateInstances < 20))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve parameterized template evidence.");
        }
    }

    private static void ValidateRadialShapeLadder(SketchScene scene, Relation ladder, IReadOnlyList<Relation> rungs)
    {
        if (rungs.Count != 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' should expose the eight requested shape-ladder steps.");
        }

        var unionSurfaces = WalkRelations(ladder)
            .Count(relation => GraphicBooleanSurface.TryReadAreaUnion(relation, out _, out _, out _, out _));
        if (unionSurfaces < 2)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve fill union evidence.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[3],
                out _,
                out _,
                out var fillResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the separate fill rung.");
        }

        var fillReads = GraphicReadGraph.From(fillResult).ReadsOf<SharedFrameAreaRead>().Count();
        if (fillReads < 7)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose separate offset fill reads.");
        }

        var arcFillReads = GraphicReadGraph.From(fillResult)
            .ReadsOf<SharedFrameAreaRead>()
            .Count(area => area.Boundary.Arcs is { Count: > 0 });
        if (arcFillReads < 5)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve circular fills as arc boundaries.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[4],
                out _,
                out _,
                out var mergeResult,
                out _,
                out _,
                out _,
                out _,
                out _) ||
            mergeResult is not Relation mergeUnion ||
            !GraphicBooleanSurface.TryReadAreaUnion(mergeUnion, out var mergeAreaFrame, out _, out _, out _) ||
            mergeAreaFrame is not Relation mergeAreaFrameRelation ||
            !GraphicBooleanSurface.TryReadAreaFrame(mergeAreaFrameRelation, out var mergeParticipants))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the merged fill as an area union.");
        }

        var mergeAreas = GraphicReadGraph.From(mergeAreaFrameRelation)
            .ReadsOf<SharedFrameAreaRead>()
            .ToArray();
        var positiveMergeAreas = mergeAreas.Count(area => !area.IsAntiArea);
        var antiMergeAreas = mergeAreas.Count(area => area.IsAntiArea);
        if (mergeParticipants.Length != 11 ||
            positiveMergeAreas != 7 ||
            antiMergeAreas < 9)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the merged fill cuts as area-frame structure.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[5],
                out _,
                out _,
                out var outlineResult,
                out _,
                out _,
                out _,
                out _,
                out _) ||
            outlineResult is not Relation outlineUnion ||
            !GraphicBooleanSurface.TryReadAreaUnion(outlineUnion, out _, out var outlineWitness, out _, out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not keep the composite outline tied to the merged area union.");
        }

        var openingOutlines = GraphicReadGraph.From(outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>().Count();
        if (openingOutlines < 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the eight interior union openings.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[6],
                out _,
                out _,
                out var cuspResult,
                out _,
                out _,
                out _,
                out _,
                out _) ||
            !TryReadOrderedFrame(cuspResult, out var cuspSelection) ||
            cuspSelection.Length < 2)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the selected interior cusps.");
        }

        var cuspMarkers = GraphicReadGraph.From(cuspSelection[1])
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .Count();
        if (cuspMarkers < 12)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not mark the twelve interior non-tangent joins.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[7],
                out _,
                out _,
                out var roundedResult,
                out _,
                out _,
                out _,
                out _,
                out _) ||
            roundedResult is not Relation roundedUnion ||
            !GraphicBooleanSurface.TryReadAreaUnion(roundedUnion, out _, out var roundedOutlineWitness, out _, out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the rounded interior-corner result.");
        }

        var roundedCenterOpenings = GraphicReadGraph.From(roundedOutlineWitness)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .Count(boundary => boundary.Arcs is { Count: >= 4 });
        if (roundedCenterOpenings < 4)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not replace the interior corner joins with rounded arc outlines.");
        }
    }

    private static void ValidateGCodeScene(SketchScene scene, IReadOnlyList<IElement> programElements)
    {
        var motions = programElements.OfType<Relation>().ToArray();
        var lineCount = motions.Count(motion => GCodeStructure.TryReadLineMotion(
            motion,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
        var graph = GraphicReadGraph.From(scene.Structure);
        if (lineCount < 1 ||
            !graph.ReadsOf<SharedFrameSegmentRead>().Any())
        {
            throw new InvalidOperationException($"G-code scene '{scene.Label}' did not expose renderable imported motion.");
        }
    }

    private static void ValidateOffsetStudyScene(SketchScene scene, GojoDraw.GojoCadOffsetStudy offsetStudy)
    {
        if (offsetStudy.Samples.Count < 9)
        {
            throw new InvalidOperationException($"Offset scene '{scene.Label}' does not cover enough curve families.");
        }

        var requestReads = WalkRelations(scene.Structure)
            .Count(relation => GraphicOffsetSurface.TryReadRequest(relation, out _, out _, out _));
        if (requestReads < offsetStudy.Samples.Count)
        {
            throw new InvalidOperationException($"Offset scene '{scene.Label}' does not preserve offset request evidence.");
        }

        foreach (var sample in offsetStudy.Samples)
        {
            if (sample.Original.Count < 2 ||
                sample.Interior.Count != sample.Original.Count ||
                sample.Exterior.Count != sample.Original.Count)
            {
                throw new InvalidOperationException($"Offset scene '{scene.Label}' has an incomplete sample '{sample.Label}'.");
            }
        }
    }

    private static IEnumerable<Relation> WalkRelations(IElement element)
    {
        if (element is not Relation relation)
        {
            yield break;
        }

        yield return relation;

        foreach (var child in WalkRelations(relation.Left))
        {
            yield return child;
        }

        foreach (var child in WalkRelations(relation.Right))
        {
            yield return child;
        }
    }

    private static bool TryReadOrderedFrame(IElement element, out IElement[] elements)
    {
        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }
}

internal sealed class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "GojoCAD SketchViewer";
        Width = 1180;
        Height = 760;
        MinWidth = 860;
        MinHeight = 560;

        var document = SketchDocument.CreateInitial();
        var canvas = new SketchCanvas
        {
            Document = document
        };
        var sceneList = new ListBox
        {
            ItemsSource = document.Scenes.Select(scene => scene.Label).ToArray(),
            SelectedIndex = document.SelectedSceneIndex,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
        var nextButton = CreateButton("Next");
        var refreshButton = CreateButton("Refresh");
        var pointsButton = CreateButton(document.ShowConstructionPoints ? "Points On" : "Points Off");
        var ghostButton = CreateButton(document.ShowPreviousRungs ? "Ghost On" : "Ghost Off");
        var rungLabel = new TextBlock
        {
            FontSize = 12.5,
            Foreground = new SolidColorBrush(Color.FromRgb(57, 64, 74)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var rungSlider = new LadderSliderControl
        {
            Maximum = Math.Max(0, document.SelectedScene.RungCount - 1),
            Value = document.CurrentRungIndex,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(6, 0, 0, 0)
        };

        var isUpdatingSceneList = false;
        var isUpdatingRungSlider = false;
        void UpdateRungControls(SketchDocument next)
        {
            isUpdatingRungSlider = true;
            try
            {
                rungSlider.SetRange(Math.Max(0, next.SelectedScene.RungCount - 1), next.CurrentRungIndex);
                rungLabel.Text = $"{next.CurrentRungIndex + 1}/{next.SelectedScene.RungCount}: {next.SelectedScene.ReadRungLabel(next.CurrentRungIndex)}";
            }
            finally
            {
                isUpdatingRungSlider = false;
            }
        }

        void SetDocument(SketchDocument next)
        {
            document = next;
            canvas.Document = next;
            pointsButton.Content = next.ShowConstructionPoints ? "Points On" : "Points Off";
            ghostButton.Content = next.ShowPreviousRungs ? "Ghost On" : "Ghost Off";
            UpdateRungControls(next);
            isUpdatingSceneList = true;
            try
            {
                sceneList.ItemsSource = next.Scenes.Select(scene => scene.Label).ToArray();
                sceneList.SelectedIndex = next.SelectedSceneIndex;
            }
            finally
            {
                isUpdatingSceneList = false;
            }
        }

        sceneList.SelectionChanged += (_, _) =>
        {
            if (isUpdatingSceneList)
            {
                return;
            }

            if (sceneList.SelectedIndex >= 0 && sceneList.SelectedIndex < document.Scenes.Count)
            {
                SetDocument(document.Select(sceneList.SelectedIndex));
            }
        };
        nextButton.Click += (_, _) => SetDocument(document.Select((document.SelectedSceneIndex + 1) % document.Scenes.Count));
        refreshButton.Click += (_, _) => SetDocument(document.Resample());
        pointsButton.Click += (_, _) => SetDocument(document.ToggleConstructionPoints());
        ghostButton.Click += (_, _) => SetDocument(document.TogglePreviousRungs());
        rungSlider.ValueChanged += nextRung =>
        {
            if (!isUpdatingRungSlider)
            {
                SetDocument(document.SelectRung(nextRung));
            }
        };

        var buttonRowA = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        buttonRowA.Children.Add(nextButton);
        buttonRowA.Children.Add(refreshButton);
        var buttonRowB = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        buttonRowB.Children.Add(pointsButton);
        buttonRowB.Children.Add(ghostButton);
        var buttonRows = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 14, 0, 0)
        };
        buttonRows.Children.Add(buttonRowA);
        buttonRows.Children.Add(buttonRowB);
        var rungPanel = new StackPanel
        {
            Margin = new Thickness(0, 20, 0, 14)
        };
        rungPanel.Children.Add(new TextBlock
        {
            Text = "Drawing Ladder",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 37, 45))
        });
        rungPanel.Children.Add(rungLabel);
        rungPanel.Children.Add(rungSlider);
        UpdateRungControls(document);

        var header = new TextBlock
        {
            Text = "Sketch Scenes",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 37, 45)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var sideLayout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(rungPanel, 1);
        Grid.SetRow(sceneList, 2);
        Grid.SetRow(buttonRows, 3);
        sideLayout.Children.Add(header);
        sideLayout.Children.Add(rungPanel);
        sideLayout.Children.Add(sceneList);
        sideLayout.Children.Add(buttonRows);

        var side = new Border
        {
            Width = 260,
            Background = new SolidColorBrush(Color.FromRgb(246, 245, 240)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(65, 91, 97, 107)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(16, 16, 12, 16),
            Child = sideLayout
        };
        var root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        root.Children.Add(side);
        Grid.SetColumn(canvas, 1);
        root.Children.Add(canvas);
        Content = root;
    }

    private static Button CreateButton(string text) =>
        new()
        {
            Content = text,
            Padding = new Thickness(8, 3),
            MinWidth = 66,
            MinHeight = 26,
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromArgb(235, 255, 254, 250)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 82, 88, 96)),
            BorderThickness = new Thickness(1)
        };
}
