# Gojo

Gojo is a lean extraction of the current Gojo kernel and tests from the older Core4 workspace.

The central rule is that meaning should live in inspectable Gojo structure: carriers, locations, relations, unit shapes, slots, residue, timelines, and tension. Helper code may build or read that structure, but it should not become the source of meaning.

For the research, start with the [experiment overview](experiments/OVERVIEW.md):
a chronological guide to both `Minds/` and `experiments/`, with each study's
question, results, conclusions and links to the evidence.

## Quick Start

The default integration branch is `master` (renamed from `main`). New feature
work uses `codex/...` branches. In another existing clone, rename its local
`main` to `master`, fetch with pruning, set its upstream to `origin/master`, and
refresh `origin/HEAD`; do not create a second integration branch.

```powershell
dotnet test Gojo.slnx
```

## Layout

The [Minds notebook](Minds/README.md) retains the two-space feedback and
prospective prediction experiments, including protocols, recorded outcomes,
independent comparisons and the static `/minds/` viewers.

```text
Gojo/
  Core/             carriers, locations, relations, tick refs, unit shapes
  Structures/       structural helpers such as repeat surfaces and claims
  Patterns/         placeholders, structural binding, repeat controllers
  Equations/        relation-shaped equation surfaces and reads
  ConceptSurface/   low-resolution concept-surface experiments
  axioms/           current model notes

Gojo.Tests/
  Core/
  Structures/
  Patterns/
  Equations/
  ConceptSurface/

GojoCAD/
  graphical format definition surfaces over Gojo structure

GojoCAD.Tests/
  graphical format read/construction tests

GojoDraw/
  Skia adapter for GojoCAD elements

GojoDraw.Visualizer/
  demo-only no-XAML Avalonia app that exercises GojoDraw

docs/
  LegacyCore4IdeaInventory.md
```

## Extracted From

This repo was extracted from the Core4 workspace so Gojo can be read and developed without the older Core4, runtime, demo, SIMD, and probe projects in the way.

Older ideas worth remembering are summarized in `docs/LegacyCore4IdeaInventory.md`; they should be re-expressed Gojo-natively before becoming code.

## Simulation and learning experiments

The experiments are retained on `master`, including the Minds notebook,
Phototaxis swimming demo, prediction and market studies, and later learning,
composition and lifecycle work. Read the [chronological overview](experiments/OVERVIEW.md)
for findings, or the [experiment index](experiments/README.md) for runnable
commands, retained results and the lossless large-result restore helper.
