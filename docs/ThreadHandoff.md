# Gojo Thread Handoff

Use this note to start a new Codex thread in the lean Gojo repo.

## Repository

Path:

```text
D:\source\repos\It3rate\Gojo
```

This repo is a clean extraction from the older `Core4` workspace. It intentionally contains only:

- `Gojo`
- `Gojo.Tests`
- `Gojo.slnx`
- repo-local instructions and docs

It intentionally excludes:

- old `Core4`
- `Core4.Kernel`
- `Core4.Runtime`
- Avalonia probe projects
- GearDemo projects and GearDemo tests
- SIMD experiments
- scratch `ConceptShapes` code

Initial extraction commit:

```text
441a79a Initial Gojo extraction
```

## Verification Baseline

The extraction passed:

```powershell
dotnet test Gojo.slnx
```

Result:

```text
543 tests passed
```

The old Core4 workspace had 552 Gojo-solution tests before extraction; the missing 9 are the intentionally removed GearDemo tests.

## Active Design Posture

Meaning should live in inspectable Gojo physical structure:

- carriers
- carrier locations
- relations
- unit shapes
- slots
- tension
- residue
- timelines
- local structural reads

Avoid making named concepts, enums, metadata bags, wrapper records, or global classifiers the source of meaning. Helpers are fine when they build or inspect structure that remains meaningful after the helper disappears.

## Current Concept Direction

Recent work focused on concept surfaces and boundary/flow/lane structure.

Current preferred shape:

- boundary/domain evidence belongs with carrier-like axes
- flow exposure evidence belongs with relation/traversal surfaces
- unit shape supplies possible lanes
- value surface supplies occupied lane
- lane transitions are explicit relation structure
- settlement leaves visible residue/tension when a crossing or transfer is unsupported
- dual language is preferred over invert language

Important files:

- `Gojo/ConceptSurface/BoundaryFlowSurface.cs`
- `Gojo/ConceptSurface/BoundaryFlowCodex.md`
- `Gojo/ConceptSurface/FlowBoundaryGradeCodex.md`
- `Gojo/ConceptSurface/ConceptShapesCodex.md`
- `Gojo/ConceptSurface/README.md`

The cleanup removed compatibility-oriented helpers such as carrier-face adapters and old complete-value read aliases. Future concept-surface work should continue from explicit structural evidence, not from `Carrier.IsInverse` as a hidden source of flow meaning.

## Recent Concept Conclusions

Boundary and flow should be thought of as related but distinct evidence layers:

- flow face evidence: forward or inverse ordered difference
- boundary side evidence: positive side, negative side, both, or bounded whole as a higher read
- legal lane basis: which boundary/flow lane combinations a selected structure admits
- occupied lane: where a particular unsigned magnitude currently sits

Conventional signed-forward values are not just "read however you want." They require visible structure:

- a `PN:F` lane basis
- an occupied lane such as `PF` or `NF`
- optional `PF -> NF` and `NF -> PF` lane-transition surfaces if underflow/overflow should route between lanes

If a lane is legal but no transition exists, crossing should create residue/tension rather than silently moving the value.

## Legacy Ideas Worth Remembering

See:

```text
docs\LegacyCore4IdeaInventory.md
```

The most useful old ideas to eventually re-express Gojo-natively are:

- boundary overflow routing
- traversal as carried structural signature
- basis encounter / seam as local continuation behavior
- support projection reads
- live samples through frozen basis
- resolved publication and feedback
- equal visible value with different structure/provenance
- topology examples such as T junctions, bridges, Y branches, loops, reflection, and diamond merges

Do not port old Core4 code directly. Rebuild any useful behavior from Gojo carriers, relations, unit shapes, tension, residue, and timelines.

## Near Next Steps

Good next work candidates:

1. Decide whether the ConceptSurface axis/face split should become core-adjacent.
2. Continue refining boundary/flow lane basis and lane transition settlement.
3. Rebuild topology/traversal examples Gojo-natively as relation/unit-shape structures.
4. Expand concept surfaces toward mechanical concepts while keeping focus, flow, parameters, settlement, and topology inspectable.
5. Clean old `Core4`-named copied axiom files if they start confusing the new repo's source of truth.

## Working Rule

When unsure, build the structure first. Then add the smallest local read helper needed to inspect it.
