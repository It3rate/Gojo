# Experiments

This folder holds source snapshots that are useful while exploring Gojo integration,
but are not Gojo kernel code.

## ScratchMill

`ScratchMill` is a vendored local fork of
<https://github.com/It3rate/ScratchMill>. It is copied into this repository so
the KiCad-to-G-code pipeline can be converted experimentally without changing the
upstream project.

The current goal is to keep the real KiCad parsing and real G-code generation
available while moving the intermediate representation toward Gojo carriers,
relations, unit shapes, and inspectable tension.

The active build surface is intentionally pruned to:

- `KiCad/**` for parsing `.kicad_pcb` files.
- `GojoImport/KiCadGojoImporter.cs` for converting parsed board geometry into
  Gojo carrier/relation structure.
- `Pipeline/ScratchMillPipeline.cs` for the current parse/import/toolpath bridge.
- `GCode/**` and small `Utils/**` helpers for preserving real G-code output.

The old UI, GRBL adapter, and deleted Numbers/Foom references are not part of
the active ScratchMill experiment build. They can remain as source/reference
material where useful, but they should not own new Gojo meaning.

## Foom

`Foom` was initially vendored only because the original ScratchMill project
referenced it. The pruned ScratchMill experiment no longer references those
projects.

## Solution

Use `Gojo.ScratchMill.slnx` when working on this experiment. The normal
`Gojo.slnx` remains focused on the Gojo kernel and existing Gojo projects.
