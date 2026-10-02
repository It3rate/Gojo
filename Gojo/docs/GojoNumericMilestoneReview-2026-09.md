# Numeric structure milestone review

Status: bounded implementation milestone, September 6, 2026.
Starting checkpoint: `346752b`. This supersedes the earlier remaining-work
lists for nested quotients, explicit material refinement and numeric transport.

## Assessment

Gojo now has a credible first milestone for **exact local numeric structure,
explicit structural reads and restorable local worlds**. Syntax has continued
alongside the new mechanisms. The core distinctions in the premises remain
useful and consistent with the tested behavior.

This is narrower than complete numeric algebra for all dimensions, a generally
convergent settlement engine, or autonomous learning. Several demos explore
those directions without supplying all their laws. No additional design
feedback was needed for the completed work; the larger choices below belong
to the next stage.

## Reexamining the premises

1. **Locality is a selected relation set.** A planetary-motion view can be simple
   while its participants are enormously complex. Exposed ports, selected bases
   and witnesses support that interpretation. Locality should not become a fixed
   size, spatial radius or class hierarchy.

2. **Uncertainty is structural, not permission for wrong arithmetic.** Open
   slots, zero denominators, incompatible units, fractional remainders, blocked
   flow and unmet draw are distinguishable states. A partial expression remains
   transportable and bindable. A constructor requiring a stable integer or unit
   can refuse premature capture without invalidating the surrounding pattern.
   Measured uncertainty and correlation still need additional laws.

3. **Tension does not identify a guilty participant.** A claim compares under
   selected premises. Neither a mismatched expectation nor an unfamiliar
   relation is automatically wrong. Arithmetic test oracles validate those
   premises conditionally; they are not a proposed global judge of the network.
   The equation comparison correction follows directly: unrelated 7-tick
   samples no longer appear as zero error.

4. **Order, identity, orientation and calibration stay separate.** Default unit
   comparison remains ordered. Explicit cancellation retains the source and
   each occurrence. Denominator participation, negative coordinates, opposed
   faces, reversal and finer calibration remain different structures.
   Coordinate-equal sites are not automatically shared sites.

5. **Meaning is connected structure read in context.** Authoring aliases
   disappear after parsing; frames, slots and correspondences remain. The format
   transports these primitives rather than operation tags. Current readers
   still need a caller to select interpretation and execution context.
   Automatic discovery of a useful local algebra does not follow from parsing
   or serialization alone.

6. **Learning may blend rather than select one winner.** The local blend fixture
   is a useful foothold. First-binding behavior, minimum tournaments and
   ID-ordered settlement are particular policies, not a general learning law.
   Filtering, confidence, growth/decay and attribution of mismatch remain
   experiments. This milestone does not harden a candidate-selection convention
   into the premise.

7. **History serves useful structure.** Exact transport protects evidence that
   is actually retained. It does not require keeping everything. Compression
   into a useful algorithm remains compatible with Gojo when its retained
   promise and loss are visible. A witness whose live tick already changed
   cannot retroactively supply an earlier historical sample.

## Capability and syntax matrix

| Area | Current support | Remaining boundary |
| --- | --- | --- |
| Identity/sites | Carrier identity, signed locations, shared ticks, ordered point/segment frames; aliases and numeric reference preservation | Persistent identity across independent spaces/packets needs correspondence |
| Exact arithmetic | Selected sums/products, affine reads, source residue, checked final coordinates; authored math fixture | Stored coordinates and tension are i64, not arbitrary precision |
| Units/dimensions | Ordered products, repetition, additive/orthogonal shapes, stable native quotients and retained descriptors | General metric/vector/tensor algebra and automatic unit conversion |
| Quotient composition | Explicit nested expansion, one-use cancellation and equivalent output observation; authored fixture executes after restore | General operation crystallization through denominators and arbitrary reciprocal evaluators |
| Inversions | Signed location, opposed carrier, reversal, contextual correspondence; distinct syntax | Universal inverse of arbitrary lossy or multi-input transforms |
| Resolution | Positive coarse/fine tick promises, exact residue and checked radix reads; resolution/subdivision syntax | General rational unit normalization and automatic finer admission |
| Material refinement | Explicit packet transfer, coarsening/borrow, shortage/remainder and receiver settlement; restored syntax execution | Fractional tension storage and automatic multi-passage borrowing |
| Partial construction | Repeated slots, dual faces, binding and failed-read witnesses; lossless numeric transport | Open integer/unit constructor arguments still need enclosing partial patterns |
| Live worlds | Attachments, live outputs, carry, local routes and timelines; restoration preserves ownership and pending state | General convergence, fair scheduling and utility-driven connection changes |
| Formats | Text-to-structure parser and versioned integer graph encoder/decoder; all nine fixtures round-trip | Canonical text writer, compact encoding, migration and distributed naming |

The selected follow-ups are implemented within these bounds:
[quotient composition](GojoUnitQuotientReads.md),
[material refinement](GojoMaterialRefinement.md), and
[numeric graph transport](GojoNumericFormat.md).

## Corrections found

- Resolution could strip opposed flow. Coarse/fine construction now preserves
  that wrapper.
- Carrier absorption mishandled the minimum signed draw. Wide intermediate
  state calculation accepts representable results and rejects direct overflow
  before changing either state field.
- Radix scales/composites, claim magnitudes, follow predictions, fit totals and
  proximity scores could silently saturate. Exact stored results now check
  range. Wide contributions can cancel or divide before final storage.
- Candidate interpolation could overflow before division despite representable
  sample coordinates. It now uses a wide intermediate.
- Incomparable equation samples could appear as zero error. The non-scalar
  residual now remains present and the scalar comparison reader refuses it.
- Diagnostic snapshots dropped open-slot identity and unit descriptors. They
  now expose both. Complete transport also records value-site identity and
  attachment ownership.
- The old sparse JSON export inferred “known” concepts from captions and entry
  IDs. `EntryFromStructure` now reads backing structure alone. The historical
  mixed projection remains explicitly named `EntryFromTeachingExample`; its
  hints and definition examples are documentary, not structural conformance.
- Recursive snapshot/group traversal failed on deep graphs. Capture and the
  relevant carrier/location/group traversals now use work queues/stacks,
  preserving repeated acyclic participation.
- Raw mutable tick descriptors could alter frozen unit promises. Unit capture
  now requires grounding in locations. Transport rejects exposed writable
  aliases into frozen units and inconsistent cached opposite-unit pairs.
- The gear README described four rungs although code/tests have six. It now
  describes the current sequence and observational motion boundary.

These correct implementation and reporting; they do not change ordinary
mathematics. Unrepresentable exact answers raise overflow rather than becoming
plausible but different magnitudes.

## Blocks and demo comparison

### Blocks

The catalogue spans resolution/tension, sites/flow/boundaries, actions/repeat,
units, bases, grades, normals, curves, branches, Boolean participation,
connected paths, derived geometry, crystallization, ladders, constraints,
topology and documentary PCB attachments.

All 208 exported backing graphs pass two numeric round trips. This establishes
representational coverage, not every caption's mathematical interpretation.
Specific examples in [the catalogue](../../Gojo.Blocks/Glossary/BlocksGlossaryStructureCatalog.cs)
run ahead of their backing structure:

- `unspecified-site` groups a concrete site with an open dual slot; it is not
  yet a constraint connecting that slot to an unknown coordinate.
- `failed-read` and `residue-witness` group a line with a tension slot; they
  do not encode the complete selected-source/read/result witness.
- `settled-read` is a shared polyline, not proof or a record of convergence.
- `repeat` and `stretch-repeat` use partitioned/line geometry without a full
  controller, transition and feedback sequence.
- `volume-hint` uses a rectangle area. `point-pair-normal` uses linear-site/
  flow structure. Neither establishes volume or general 3D metric laws.

The README and specimen notes now identify these illustrative boundaries.
The caption-dependent sparse fixture export is also explicitly separated from
the structure-only read, with a regression against prose changing meaning.
Keep the experiments, but promote proposed concepts individually with narrow
readers, partial cases and conformance fixtures. They are not a canonical
primitive list.

Tutorial scenes also have presentation-level duration and visual cues. A
visible animation is not automatically a Gojo transition that can be serialized.
The syntax's repeat, step, timeline, transform and dependency fixtures provide
a stronger basis for executable dynamic content.

### Gear shape ladder

The six stages are one sized circle, two circles, coupled circles, triangular
teeth, flat tips and sampled involute teeth. Tests cover refinement, shared
pitch/perimeter identity, timelines, mesh phase and opposing perimeter
projection. A new test restores the whole ladder and rereads every stage and
contour while preserving shared identity.

This demonstrates chosen levels of detail well. It does not solve tooth contact,
force or general 3D mechanics. Sampled involutes and display angles use
conventional approximate geometry at declared resolution; they do not establish
an exact transcendental algebra.

### CAD, equations and connected surfaces

CAD tests cover shared frames, points/segments/arcs, closed boundaries,
positive/anti-area participation, outlines, selection, derived parameters and
documentary electronics. Those are useful structural interpretations, not yet
a universal metric, solid model or equation solver.

Equations cover affine/polynomial reads, relation-shaped operands, claims,
follow, search and timelines. This review's range and false-zero corrections
strengthen them. Search is still a candidate policy; an unresolved claim's
ranking convention does not establish truth or learned confidence.

Boundary/flow and concept-surface tests remain the broader evidence for ports,
legal lanes, accepted/blocked transfer, local/shared capacity, settlement,
time and residue. New refinement follows this separation in a narrower positive
zero-origin passage, rather than replacing the boundary model.

## Remaining work and decisions

The next mathematical fixture should express **bounded approximate operands and
a claim across coarse/fine bases**. Declare an interval convention, then check
containment under sums/products, zero-crossing division and refinement
consistency. Include shared-source versus independent uncertainty: `x-x` is
zero for two references to the same exact underlying sample; two independent
uncertain samples do not warrant that inference.

Larger gaps remain explicit:

1. **Convergence and execution budgets.** Local settlement has a bounded pass
   loop and deterministic ordering. That is not a convergence proof for
   arbitrary networks. Oscillation/exhaustion evidence, scheduling and
   connection utility need their own contract. Direct transfer validation
   does not provide rollback for all downstream settlement.

2. **Component and inverse laws.** Ordered unit expressions are ahead of
   general vector, tensor, metric and contextual inverse operations. Reciprocal
   dimensions do not themselves evaluate numeric reciprocals, and dimensional
   equivalence does not supply a calibration factor.

3. **Uncertainty semantics.** Guaranteed bounds, empirical confidence and
   competing hypotheses are different promises. Arithmetic for a declared
   interval is determined; which promise a local space learns/exposes needs
   design judgment. Confidence is not a bound and mismatch is not fault
   localization.

4. **Autonomous discovery.** Mixing/filtering, admission, pruning, evidence
   compression and mismatch routing are not mathematically unique. Preserve
   several useful constructions. This is the main area for future user
   feedback, not another decision about whether arithmetic holds.

5. **Authoring completeness.** There is no canonical text writer, direct
   unbound-carrier constructor or arbitrary descriptor authoring form. Numeric
   transport preserves the corresponding kernel constructs; readable syntax
   for every possible graph remains separate.

6. **Transport limits.** Version 1 captures a quiescent local graph, not a
   concurrent checkpoint, distributed identity/import protocol, callback
   serializer or semantic migration across kernels. Transporting cyclic
   relations does not make all legacy recursive readers cycle-safe.
   Compact canonical encoding and deliberately lossy learned memory are later
   layers.

No new user choice was necessary for the completed corrections. For the next
uncertainty fixture, the recommended starting assumption is an explicit
guaranteed-bound read, with confidence and learned applicability held as separate
connected evidence. This keeps mathematical tests unambiguous while leaving
the adaptive model open.

The subsequent [uncertainty reference](GojoUncertaintyReference.md) develops
that recommendation: begin guaranteed-bound reads with shared unknowns and
observation dependencies, and distinguish actual-value evidence from nominal
stops, tolerance, selected detail and goals. It is a documentation/design
follow-up; the runtime and verification counts below describe this numeric
checkpoint, not an implemented uncertainty-propagation engine.

The next [bounded-quantity fixture](GojoBoundedQuantityFixture.md) adds the
first conditional shared-slot affine reads, conservative nonlinear enclosures,
conformance and authored focus/goal examples. Its verification and remaining
limits are recorded separately from this earlier checkpoint.

## Verification

`dotnet test Gojo.slnx --nologo` passes **1,101 tests**, with no failures or
skips: 720 kernel, 212 encoding, 21 gear ladder and 148 CAD. This is 40 more
test cases than the starting checkpoint; generated matrices and the 208-item
Blocks inventory run inside those cases. Documentation links and
`git diff --check` are also checked before committing.

Three complementary approaches support this checkpoint:

- Arithmetic laws and input reconstruction, including the earlier projection,
  quantity, operation and dimensional cancellation matrices.
- Structural refusal cases: open patterns, distinct sites, changed standards,
  incompatible units, zero domains, clamped receivers and exhausted sources.
- Graph and behavioral restoration: two round trips of every current syntax
  fixture and Blocks export; restored live output/carry, quotient/refinement
  execution, and gear rungs/contours.

This is substantial, finite evidence for the bounded milestone, not proof that
every experimental reader or possible local network is correct.
