# Gojo Agent Instructions

## Active Scope

- This repository is exclusively the Gojo kernel extraction.
- Gojo is the active kernel model.
- Core code lives in `Gojo/Core`.
- Tests live in `Gojo.Tests`.
- Use `Gojo.slnx` for builds and tests.
- Historical Core4 ideas that were kept intentionally are summarized in `docs/LegacyCore4IdeaInventory.md` and in copied `Gojo/axioms` notes.

## Gojo Structure Rules

- Use Gojo carriers, locations, relations, unit shapes, and tension as the canonical internal representation.
- A concept exists only when its parts are connected by Gojo relation structure. Code may inspect that structure, but code objects must not become the source of meaning.
- Before adding a class, record, enum, metadata field, or wrapper, ask: if this code artifact disappeared, would the Gojo structure still represent the concept? If not, build the relation/carrier structure first.
- Prefer local discovery from a carrier, relation, or unit shape over static classifiers that receive unrelated pieces and decide globally what they mean.
- Meaning should be physically traversable through connected structure. Avoid standalone bags of related values.
- If a concept is hard to express, extend the Gojo numeric/structural format rather than falling back to symbolic operation enums, opaque tags, ad hoc metadata, or case-by-case dispatch.
- Prefer calculated and locally read values to stored duplicates.
- Stable crystallized structures such as `UnitShape` are valid when stability is the concept being represented.
- Incomplete or incompatible states are information. Prefer tension, failed reads, residue, or explicit unresolved structure over exceptions and guard-heavy invalid-value checks.
- Resolution is part of the read, not an error state. A low-resolution read is a valid read.
- Repetition is first-class structure: selected structure, controller, transition, feedback, residue, and timeline should remain inspectable.
- For concept-surface work, prefer boundary/flow evidence, lane basis, lane transitions, focus/read surfaces, capacity, settlement witnesses, and timelines over named concept classes or symbolic tags.

## Tension, Residue, And Reads

- Tension is unresolved transferred change that must remain available to settlement.
- Read residue is the remainder from interpreting an existing source through a requested basis or resolution.
- Preserve residue as tension when transfer/import/settlement would otherwise discard incoming change.
- A lower-resolution read is not an error; it is the requested resolution.

## Testing

Run:

```powershell
dotnet test Gojo.slnx
```

Treat `Gojo/axioms`, `Gojo/ConceptSurface`, `docs`, and current Gojo code as the active source of truth. Historical Core4 notes are useful comparison material only where they have been copied into `docs` or `Gojo/axioms`.

The initial extraction passed 543 tests after removing GearDemo tests and references.

## UI And Demo Code

- Use Avalonia for UI code, but never use XAML. All rendering is in native skia or other low level library, not xaml.
- GojoDraw is a render library that converts GojoCad elements to skia or other low level drawing libraries, so it is the skia level for Gojo Core, but can include things like animation or components that are very general, like buttons and sliders.
- Use native Skia rendering and render per time based tick for animation. Redraw the areas that need updating.
- Keep UI code and logic separate from Gojo core unless a missing kernel capability is clearly discovered, in that case suggest a port of a feature.
- Reusable UI pieces can live in component libraries, but they should not own Gojo meaning.
- Convert GojoCad structures to `int`, `double`, pixels, strings, colors, or display DTOs (data transfer objects) only at adapter/render boundaries.
- Avoid React-style state snapshots and broad global read records. Prefer local observers/inspectors over the structures being displayed. Dynamically compute rather than store values, where possible.
- Use GojoDraw only as a render library that creates meaningful visualization of GojoCad elements and structures. We can have a visualizer in there that excercises the library, but all demo code should be in a new project.
