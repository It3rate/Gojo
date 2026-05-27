# Gojo

Gojo is a lean extraction of the current Gojo kernel and tests from the older Core4 workspace.

The central rule is that meaning should live in inspectable Gojo structure: carriers, locations, relations, unit shapes, slots, residue, timelines, and tension. Helper code may build or read that structure, but it should not become the source of meaning.

## Quick Start

```powershell
dotnet test Gojo.slnx
```

## Layout

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

docs/
  LegacyCore4IdeaInventory.md
```

## Extracted From

This repo was extracted from the Core4 workspace so Gojo can be read and developed without the older Core4, runtime, demo, SIMD, and probe projects in the way.

Older ideas worth remembering are summarized in `docs/LegacyCore4IdeaInventory.md`; they should be re-expressed Gojo-natively before becoming code.
