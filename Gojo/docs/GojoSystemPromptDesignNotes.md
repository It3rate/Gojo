# Gojo System Prompt Design Notes

This note explains how `GojoSystemPrompt.md` was derived. It is not part of the
prompt itself.

## Baseline Assumption

The Core4-oriented instruction block supplied with the task was treated as the
current prompt baseline. It contained the ten broad Core4 axioms plus the site,
pin, relation, freeze, and attachment supplement. No separate prompt artifact
was found in the repository.

## Evidence Reviewed

The replacement was reconciled against:

- the current core types and settlement code in `Gojo/Core`;
- the kernel, equation, pattern, concept-surface, CAD, and encoding tests;
- the active Gojo axioms, especially carrier settlement, duality, unit shape,
  crystallization, ports, operations, repetition, resolution, and tension;
- `GojoReadSpine.md`, `StructuralAndConceptualLadders.md`, and
  `ParticipationSettlementReads.md`;
- the ConceptSurface boundary/flow and concept-shape codices;
- current repository and project READMEs;
- the Core4 kernel, carrier-read, and boundary-overflow notes;
- `LegacyCore4IdeaInventory.md` for intentionally retained design memory.

The current full solution passed its tests during this review. The prompt does
not embed the test count because that fact is already more current than the
count in the old repository instructions and will continue to change.

## Major Replacements

| Core4-era center | Gojo replacement |
| --- | --- |
| A single uncalibrated `Measure` as the primitive | An `IElement` vocabulary centered on carriers, signed carrier locations, tick references, ordered relations, placeholders, and unit shapes. |
| The `0/0`, `m/0`, `m/u` ladder as the general relation state | Open placeholders/bindings, frame projection with explicit residue, carrier tension, boundary-flow legality, and witnessed reads. The old ladder remains lineage, not the canonical runtime representation. |
| Origin, pin, freeze, basis, and attachment as an assumed construction sequence | Point and segment relations, explicit exposed ports, stable unit shapes, calibration/read surfaces, and crystallization. These can express the useful stages without reintroducing the removed wrapper ontology. |
| Four observer modalities as a foundational taxonomy | Local projection, settlement, operation, focus, transform, generation, and inspection surfaces whose role comes from relation shape and read context. |
| Opposed units as the ordinary source of negative scalar values | Signed locations on one carrier for scalar negation; opposed carrier faces for distinct flow participation; reversed relation order for traversal. |
| Fused two- or four-value structures as the general dimensional representation | Relation-shaped ports and groups, directed segments, product unit shapes, orthogonal outlets, participation settlement, and concept surfaces. |
| Self-fusion as a special power mechanism | Visible repeated product participation plus `RepeatSurface`, controller, feedback, residue, and timeline. |
| Boundary overflow as a broad future route layer | The executable carrier settlement foothold plus the newer boundary-flow chain: projection, lane basis, accepted/blocked transfer, target-local/shared capacity, target settlement, and timeline. |

## Site Supplement Terminology

The older site-relation supplement contained useful construction intuitions,
but its `0/0`, `m/0`, `m/u` state ladder is not the current Gojo core. The
replacement treats its terms as contextual verbs and reads:

| Older term | Gojo reading |
| --- | --- |
| site | A referencable `CarrierLocation`, point relation, shared `TickRef`, placeholder, or exposed relation site, depending on resolution. |
| pin | Select or share the exact site structurally; do not create basis meaning merely by naming it. |
| relation | The active ordered `A -> B` primitive. |
| freeze | Stabilize tick grammar or reusable structure through `UnitShape`, crystallization, or an exact stable surface. |
| basis | A relation-shaped frame, unit shape, lane basis, or resolution surface through which a source is read. |
| attachment | A live relation, binding, port, or output connection between actual sites and an exposed basis. |

This keeps the useful warning that selecting a site, relating it, stabilizing
the relation, and reading live participation through it are different acts. It
does not restore the removed wrapper types as the kernel ontology.

## Core4 Ideas Retained In Gojo Form

Several Core4 intentions remain important after changing representation:

- meaning should be numeric, inspectable, and structurally traversable;
- unresolved states are information rather than invalid math;
- relation shape should replace symbolic operation dispatch;
- stable structure may crystallize into a reusable unit;
- boundary crossings may route, wrap, count, branch, filter, or remain tension;
- live observations must not automatically drain their sources;
- repeated structure and intermediate states must remain visible;
- equal visible answers can retain different structural provenance;
- a system should lower locally visible tension rather than demand one global
  final solution.

The replacement prompt keeps these as Gojo rules while avoiding direct reuse of
Core4 wrappers, same-lineage carriers, fold machinery, or ratio-state ontology.

## Important Reconciliations

### Immutable Versus Dynamic

The earlier prompt broadly preferred immutable structures. Current Gojo has
deliberately mutable carrier value/tension and retargetable tick references,
while unit shapes and crystallized promises are stable. The new prompt narrows
mutability to physical state sites and keeps witnesses, unit shape, and
snapshots stable.

### Tension Versus Residue

Some historical notes use tension for nearly every unresolved quantity. Current
Gojo distinguishes read residue from transferred unsettled change. The new
prompt requires residue to become carrier tension only when a transfer, import,
or settlement would otherwise lose incoming participation.

### Code Versus Direction

Current code is authoritative about what works now, but it is intentionally a
foothold. Active Gojo axioms remain relevant for requested extensions. The new
authority section therefore requires status labels instead of either freezing
the code as the final ontology or treating speculative notes as implemented
law.

### Higher-Level Helpers

Gojo now contains many named helper surfaces. The prompt does not ban them. It
requires their fields to remain ordinary connected Gojo elements and their
readers to be local. This preserves the practical read spine without making
helper classes the semantic source.

### Summary Enums

Boundary and flow code has compact summary records and enums, while its newer
construction path preserves primitive face, side, lane, and transition
evidence. The prompt allows enums as projections and diagnostics but rejects
them as canonical meaning.

## September 2026 Syntax And Local Exploration Clarifications

The syntax review and the user's response clarified several foundational
distinctions. Partial construction is positive structural participation rather
than a lesser form waiting to become valid. A goal or claim can retain the
reason a local space focuses its exploratory work there.

Candidate preservation must not presume a final winner. Local spaces may
combine and filter contributions and retain several useful structures. The
current first-binding convention is not a proposed learning law. Likewise,
claim mismatch is conditional evidence about a statement, not an assignment of
fault to its expected output. Exact arithmetic remains valid and independently
testable; uncertainty concerns operands, applicability, resolution, and the
connected premises. Wider utility and support are intended feedback mechanisms,
not a global verifier or an implemented confidence policy.

The earlier immutable-versus-dynamic reconciliation remains appropriate for
stable tick promises and declared exact snapshots. It must not imply mandatory
permanent archival history. Adaptive memory may deliberately compress, revise,
or forget through visible transformations, with a narrower retained promise.
Similarly, connection participation and utility should eventually support
growth, decay, redirection, and pruning. The prompt labels these as direction;
the new local blend fixture supplies a small explicit experiment, not these
general mechanisms.

Source-space identity remains a direction grounded in exposed references and
correspondence. It does not authorize merging coordinate-equal sites or
compatible carriers. The current implementation still distinguishes carrier,
point relation, shared TickRef, and coordinate equality.

See [the updated syntax review](GojoSyntaxReview-2026-09.md) and
[the executable local blend fixture](GojoLocalBlendFixture.md).

## September 2026 Mathematical Conformance

The low-level audit found that ratio projection placed a scaled division
remainder on the source carrier without converting its basis. The correction
requires fractional source remainders even though the target read is integer.
The canonical projection axiom now states the reconstruction law explicitly.

`L(source,n) -> P(source,d)` supplies the subdivision numerically in ordinary
Gojo structure. Its residue reader is contextual and narrow. It does not add a
fraction tag, change carrier storage, or pretend general denominator-unit
cancellation has been implemented. Whole-tick imports reject these fractions
before applying collected slot changes. Observation, including follow and
boundary projection, keeps the exact structure. A later material refinement
mechanism can extend admission without changing arithmetic.

The positive-resolution rule prevents invalid authoring and saturated products
from altering a stable tick promise. Wide temporary arithmetic in projection,
structural ratio scaling, and operation accumulation fixes avoidable host
overflow while retaining checked limits for stored coordinates and fractions.
The prompt requires accounting and range discipline without claiming every
experimental reader supports arbitrary-precision inputs.

The conformance fixture exercises exact mathematics independently of a live
learning network, then checks composition through syntax, resolution and
material boundaries. This preserves the user's distinction between correct
local mathematics and uncertain applicability of connected premises. See
[mathematical conformance](GojoMathConformance.md) for coverage and remaining
low-level laws; general feedback discovery remains later work.

## Prompt Shape

The final prompt deliberately has four scales:

1. canonical low-level elements and executable arithmetic;
2. operational axioms for reads, settlement, units, orthogonality, and
   crystallization;
3. subsystem composition through ports, boundary-flow routing, witnesses,
   repetition, transforms, and ladders;
4. repository boundaries, workflow, tests, and maturity guardrails.

This lets an agent work on `Carrier` settlement, a concept-surface route, a CAD
refinement, an encoding experiment, or a renderer without switching to a
different source of meaning.

## Repository Distribution

The canonical prompt lives at `Gojo/docs/GojoSystemPrompt.md`. The repository
root `AGENTS.md` is intentionally a small bootstrap that requires agents to
read the canonical prompt completely before substantive work. Keeping the
large prompt out of `AGENTS.md` avoids the default project-instruction byte
limit while preserving one source of truth.

Both files should be committed with the repository. Any machine that clones or
pulls the repository then receives the prompt and its automatic bootstrap
without a per-machine copy or configuration step. Changes to foundational
rules should update this design note in the same commit as the prompt.

`GojoChatGPTPersonalization.md` is a compact, globally scoped companion for the
ChatGPT Personalization field. It omits repository operations and is not an
alternative authority for repository work; the canonical prompt remains
`GojoSystemPrompt.md`.
