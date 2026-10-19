# Gojo

Gojo is a lean extraction of the current Gojo kernel and tests from the older Core4 workspace.

The central rule is that meaning should live in inspectable Gojo structure: carriers, locations, relations, unit shapes, slots, residue, timelines, and tension. Helper code may build or read that structure, but it should not become the source of meaning.

For the research, start with the [experiment overview](experiments/OVERVIEW.md):
a chronological guide to the studies in `experiments/`, with each study's
question, results, conclusions and links to the evidence.

## Quick Start

Work directly on `master` and commit completed, verified checkpoints locally.
Create a feature branch only when requested. Pushing remains a separate action.

```powershell
dotnet test Gojo.slnx
```

## Layout

The [Minds notebook](experiments/Minds/README.md) retains the two-space feedback and
prospective prediction experiments, including protocols, recorded outcomes,
independent comparisons and the static `/minds/` viewers.

```text
Gojo/                       numeric and structural kernel
  Core/                     carriers, locations, relations, tick refs, unit shapes
  Structures/               repeat surfaces, claims and other structural helpers
  Patterns/                 placeholders, bindings and repeat controllers
  Equations/                relation-shaped equations and reads
  ConceptSurface/           low-resolution concept-surface experiments
  axioms/                   current model notes
  docs/                     model documentation and experiment reports
Gojo.Encoding/              experimental encoding and concept-address layer
Applications/
  GojoCAD/                  graphical and geometric Gojo structure
  GojoDraw/                 Skia presentation adapter over GojoCAD
Visualizations/
  Gojo.Blocks/               interactive structural explorer
  Gojo.Blocks.Browser/       browser host for Blocks
  Gojo.GearShapeLadder/      gear and shape-ladder explorer
  Gojo.GearShapeLadder.Browser/
  GojoCAD.SketchViewer/      sketch inspection UI
  GojoCAD.SketchViewer.Browser/
  GojoDraw.Visualizer/       demo app exercising GojoDraw
  Gojo.Demos.Web/            static experiment viewers
Tests/                      every test project, retaining its existing name
experiments/                research fixtures, runners and retained results
  Minds/                    two-space feedback and prediction notebook
artifacts/                  generated build and export artifacts
```

Solution folders follow these same categories. Each solution keeps its existing
project selection; some retained experiments and viewers are run directly.
Project names, assemblies and namespaces remain unchanged by this layout.

## Extracted From

This repo was extracted from the Core4 workspace so Gojo can be read and developed without the older Core4, runtime, demo, SIMD, and probe projects in the way.

Older ideas worth remembering are summarized in the
[Core4 idea inventory](Gojo/docs/LegacyCore4IdeaInventory.md); they should be
re-expressed Gojo-natively before becoming code.

## Simulation and learning experiments

The experiments are retained on `master`, including the Minds notebook,
Phototaxis swimming demo, prediction and market studies, and later learning,
composition and lifecycle work. Read the [chronological overview](experiments/OVERVIEW.md)
for findings, or the [experiment index](experiments/README.md) for runnable
commands, retained results and the lossless large-result restore helper.

The [qualitative recognition study](Gojo/docs/GojoQualitativeRecognition.md)
examines recurring motifs and larger flow arrangements from segment observations
with construction history and position words withheld. Its retained
[interactive gallery](experiments/QualitativeFrieze/results/qualitative-recognition/gallery.html)
compares several interpretations of each field.
The [evidence and placement follow-up](Gojo/docs/GojoQualitativeRecognitionFirmness.md)
separates admission from demonstrated flow, adds ordinal chamber placement,
and rechecks preserved motifs against local changes in its
[gallery](experiments/QualitativeFrieze/results/recognition-firmness/gallery.html).
The [hierarchical recognition study](Gojo/docs/GojoHierarchicalRecognition.md)
composes motifs, chamber roles and row programs across opaque interfaces.
Its [interactive gallery](experiments/QualitativeFrieze/results/hierarchical-recognition/gallery.html)
follows questions downward and actual evidence back upward, including contrary,
ambiguous and capacity-limited answers.
The [ornament program encoding](Gojo/docs/GojoOrnamentProgramEncoding.md)
separates shared motifs and periodic layout rules from parameter bindings,
sampling windows and expanded execution. Its
[gallery](experiments/QualitativeFrieze/results/ornament-program/gallery.html)
includes six samples from a saved native program, with standalone replay.
The [Bourgoin construction study](Gojo/docs/GojoBourgoinEncodingStudy.md)
tests a broader source-derived vocabulary and explicit seams between repeated
units. Its [comparison gallery](experiments/QualitativeFrieze/results/bourgoin-patterns/gallery.html)
places native generated fields beside the cited historical diagrams.
The [source-correction follow-up](Gojo/docs/GojoBourgoinRefinementStudy.md)
uses local lattice refinements and closed negative chambers to improve those
reconstructions, with preserved first-pass images and source overlays.
The [construction vocabulary study](Gojo/docs/GojoBourgoinVocabularyStudy.md)
adds twenty source-based skeletons, finite curved profiles and a
[single-sheet atlas](experiments/QualitativeFrieze/results/bourgoin-patterns/overview.html),
while separating authored explanations from computed graph reads.
The [contact and course study](Gojo/docs/GojoBourgoinContactAndCourseStudy.md)
refines partial-side sliding contacts, flat-tipped stars, slender arms and
open ribbon courses while retaining the earlier constructions for comparison.
The [coloured ornament collection](experiments/QualitativeFrieze/results/coloured-ornament/gallery.html)
adds native landmark-bound petals, leaves and medallions, with three selectable
focuses guided by Jones's colour and hierarchy proposals. Its
[study](Gojo/docs/GojoColouredOrnamentStudy.md) also corrects B32's diagonal continuation.
The [full ornament design collection](experiments/QualitativeFrieze/results/ornament-designs/gallery.html)
extends all 36 skeletons into 108 separately constructed designs, with
geometric fields, intermediate flowers and foliage, and fine local details.
Its [single-page sheet](experiments/QualitativeFrieze/results/ornament-designs/overview.html)
puts all three versions of every skeleton together; the
[construction study](Gojo/docs/GojoOrnamentDesignCollection.md) describes the
finer unmeasured vocabulary and native attachment evidence.
The [detail study](experiments/QualitativeFrieze/results/ornament-details/gallery.html)
develops ten richer motifs and four scaffold-following B02/B05 integrations,
with one-unit close-ups and a single comparison sheet.

For example, run the prospective-prediction experiment and its notebook tests
from the repository root:

```powershell
dotnet run --project experiments/Minds -- --experiment3
dotnet test Tests/Minds.Tests/Minds.Tests.csproj
```
