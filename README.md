# Gojo

Gojo is a lean extraction of the current Gojo kernel and tests from the older Core4 workspace.

The central rule is that meaning should live in inspectable Gojo structure: carriers, locations, relations, unit shapes, slots, residue, timelines, and tension. Helper code may build or read that structure, but it should not become the source of meaning.

For the research, start with the [experiment overview](experiments/OVERVIEW.md):
a chronological guide to the studies in `experiments/`, with each study's
question, results, conclusions and links to the evidence.

The [relational shape study](Gojo/docs/GojoRelationalShapeStudy.md) reconstructs
100 sources with separate pairwise spatial cues, length and angle equivalence,
and qualitative support and opposition. Its
[gallery](experiments/QualitativeFrieze/results/relational-shapes/gallery.html)
compares observation, requested completion, sparse evidence and a control
withholding equivalence. Layout and closure remain revisable hypotheses.

The [joint composition study](Gojo/docs/GojoJointCompositionReconstruction.md)
reconstructs native shapes from anonymous joints, ordinal positions and separate
symmetry evidence. Its [gallery](experiments/QualitativeFrieze/results/joint-reconstruction/gallery.html)
compares three authored policies, retained assumptions and message-only feedback;
this composition round does not train a model.

The [ornament learning pilot](Gojo/docs/GojoOrnamentLearningPilot.md) trains a
portable native route dictionary from a streamed curriculum and tests partial
evidence, conflicts and held-out constructions. Its
[gallery](experiments/QualitativeFrieze/results/ornament-learning/gallery.html)
keeps teacher observations, open correspondence and generated hypotheses
separate; [whole-family holdout](experiments/QualitativeFrieze/results/ornament-learning-family/gallery.html)
exposes the limits of route recall without compositional learning.

The [native interior discovery study](Gojo/docs/GojoNativeInteriorDiscovery.md)
finds local enclosures in lattice passages through native word order and cyclic
ray walks, then reads side structure, notches and zigzags with explicit evidence.
Its [gallery](experiments/QualitativeFrieze/results/native-interiors/gallery.html)
separates observed boundaries from proposed gap closures; the
[overview](experiments/QualitativeFrieze/results/native-interiors/overview.html)
compares twenty source patches.

The [native lattice evidence study](Gojo/docs/GojoNativeLatticeEvidence.md)
connects executed ornament graphs to query-directed geometric evidence through
published ports. Its [gallery](experiments/QualitativeFrieze/results/native-lattice-evidence/gallery.html)
compares actual source supports, requested premises and derived boundary views,
including missing geometry and bounded searches.

The [structural potential study](Gojo/docs/GojoStructuralPotential.md) connects
partial evidence to query-directed relational completions. Its
[gallery](experiments/QualitativeFrieze/results/structural-potential/gallery.html)
keeps supporting-line possibilities, finite support, and introduced assumptions
separate from the original source.

The [reusable rule-pack study](Gojo/docs/GojoReusableRulePacks.md) applies the same
native contracts to ornaments, glyphs and path networks, then connects defaults
to provisional generation and revision. Its
[interactive gallery](experiments/QualitativeFrieze/results/rule-pack-transfer/gallery.html)
keeps source evidence, construction assumptions and actual output checks separate.

The [ornament default study](Gojo/docs/GojoOrnamentDefaults.md) separates required
structure, revisable defaults and deliberately unused cues across 72 native
drawings. Its [interactive gallery](experiments/QualitativeFrieze/results/ornament-defaults/gallery.html)
shows which alternatives survive changes to count, symmetry, overlap and
proportion expectations, with counterexamples and missing evidence retained.

The [ornament concept-equivalence study](Gojo/docs/GojoOrnamentConceptEquivalence.md)
compares 38 native designs under explicit whole-arrangement questions. Its
[interactive gallery](experiments/QualitativeFrieze/results/ornament-concepts/gallery.html)
shows how a rosette, fan and branching window can share one route concept while
remaining visually different, and exposes designs that agree with earlier local
questions but violate the requested whole arrangement.

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
The [lattice construction ladders](Gojo/docs/GojoLatticeConstructionLadders.md)
retain actual grid ancestors through 19 related constructions, with separate
current-graph questions and an explicit history-forgetting experiment. Their
[interactive gallery](experiments/QualitativeFrieze/results/lattice-ladders/gallery.html)
lets you walk each branch and compare its earlier bases.
The [hundred-lattice catalogue](Gojo/docs/GojoLatticeCatalogue.md) expands this
into twenty parameterized families with explicit constraint withdrawal,
reformulation, coarse compartments and finite rule assessments. Its
[gallery](experiments/QualitativeFrieze/results/lattice-catalogue/gallery.html)
shows construction strips and supports local ratings; the
[whole sheet](experiments/QualitativeFrieze/results/lattice-catalogue/overview.html)
compares all hundred unfilled scaffolds.
The [geometric transformation follow-up](Gojo/docs/GojoLatticeGeometryTransformations.md)
then changes the actual spans with notches, bends, cuts, shared runs and moved
hubs. Its [before/after gallery](experiments/QualitativeFrieze/results/lattice-transformations/gallery.html)
separates real support change from regrouping or rescaling the same pattern.
The [interior-first study](Gojo/docs/GojoLatticeInteriorStudy.md) merges actual
atomic faces into non-overlapping cells, with explicit competing seam claims.
Its [hundred-case gallery](experiments/QualitativeFrieze/results/lattice-interiors/gallery.html)
compares filled interiors, outlines and removed dividers.
The [Gestalt exploration plan](Gojo/docs/GojoGestaltViewsExploration.md) proposes
overlapping cue-based reads and compares constructive contexts for the same shape.
The [construction-context experiments](Gojo/docs/GojoConstructionContextExperiments.md)
exercise equal support with different edit scopes, constraint withdrawal,
optional construction memory and overlapping views consumed by new flow.
Their [interactive gallery](experiments/QualitativeFrieze/results/construction-contexts/gallery.html)
and [all-step overview](experiments/QualitativeFrieze/results/construction-contexts/overview.html)
compare seventeen path steps and three shared-field constructions.
The [junction interpretation study](Gojo/docs/GojoJunctionInterpretationExperiments.md)
then separates actual joins from projected crossings, endpoint contacts and
speculative gap continuations. Its [gallery](experiments/QualitativeFrieze/results/junction-interpretations/gallery.html)
retains competing cut and depth readings without altering source topology.
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
The [refined 108-design collection](experiments/QualitativeFrieze/results/ornament-refined/gallery.html)
extends that vocabulary across the catalogue with optional curves over native
guides, angular comparisons and close-ups. The
[curve studies](experiments/QualitativeFrieze/results/ornament-curves/gallery.html)
isolate the vine, corrected curl and interrupted guilloche.
The [B08–B14 interior studies](experiments/QualitativeFrieze/results/ornament-refined/interiors-gallery.html)
develop larger flowers, interior gardens and four-colour interlocking shapes,
with [24 designs on one sheet](experiments/QualitativeFrieze/results/ornament-refined/interiors.html).
The [silhouette and colour review](experiments/QualitativeFrieze/results/ornament-refined/balance.html)
compares the enlarged detail with its previous checkpoint; a
[conceptual-complexity table](experiments/QualitativeFrieze/results/ornament-refined/complexity.html)
keeps reviewed reusable vocabularies separate from native encoding counts.
The [public ornament gallery](https://gojo.inklingai.ca/ornaments/flowing-grammar/)
includes all 108 designs and their executed-unit views. Its bounded website
edition keeps the research archives and alternate construction views in this checkout.

For example, run the prospective-prediction experiment and its notebook tests
from the repository root:

```powershell
dotnet run --project experiments/Minds -- --experiment3
dotnet test Tests/Minds.Tests/Minds.Tests.csproj
```
