# Gojo Agent Instructions

## Active Scope

- Gojo is the active kernel model.
- Core code lives in `Gojo/Core`.
- Tests live in `Gojo.Tests`.
- Use `Gojo.slnx` for builds and tests.

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

Treat `Gojo/axioms` and current Gojo code as the active source of truth. Historical Core4 notes are useful comparison material only where they have been copied into `docs` or `Gojo/axioms`.
