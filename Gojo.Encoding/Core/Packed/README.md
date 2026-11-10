# Packed encoding mechanisms

This is the reusable packed layer promoted from `experiments/PrimitiveMeasure`.
It stores small observations, references, shared topology and executable relation
tables. The original experiments remain intact. New work can reference
`Gojo.Encoding` and use the `Gojo.Encoding.Core.Packed` namespace directly.

The basic separation is: values describe what was read; references connect
parts; relation bits describe admitted combinations; passage bits describe
selected transitions. A read does not silently become a material operation.

## Meaning and current contracts

- **ODV:** O is the selected orientation, D the selected direction, and V a
  detection bit at the selected resolution. V=0 is not exact zero, deletion or
  lack of selection. V=1 does not specify the quantity one. Equal encodings do
  not establish equal real magnitudes. `ThreeBitMeasure` supplies packing and
  explicit read transforms, without implicit arithmetic.
- **Selection:** choosing references, a family-use group, a visible-site mask,
  or a bounded inspection selects what is read. Unselected sites are not
  rewritten to V=0. Allocation liveness and transport padding are storage
  concerns, separate from selection and measurement.
- **Orientation:** `Reorient` applies the selected symmetric mapping O^1,D^1.
  It is useful when that mapping is known; it does not establish a common
  origin, calibration, domain, or universal inverse between frames.
- **Wider codes:** adding bits increases storage capacity. `CoarseRead` maps a
  stored zero code to V=0 and other codes to V=1. That is a selected code read,
  not a promise that newly allocated bits contain finer evidence or calibrated
  physical distance.
- **Uncertainty:** a support byte is a set of candidate ODV words: bit n admits
  word n. Support 0 has no admitted candidate; support 1 admits word 000;
  support 3 admits words 000 and 001. A relation retains alternatives rather
  than choosing a hidden answer. Repeated input references retain their shared
  candidate. Different derived paths can still lose correlation; their combined
  support is conservative, not an exhaustive description of hidden reality.
- **Boolean reads:** the historical `ConnectionLaws.Presence` and
  `ExclusivePresence` names mean OR/XOR of already observed detection bits.
  They do not add represented quantities. Two subthreshold amounts can combine
  into a detectable amount even when both input V bits are zero.
- **Ports:** `PortStructure` owns packed storage, local bindings and exposed
  port positions. A connection holds the owner and a public port position;
  migration updates the owner-local binding. A foreign owner does not acquire a
  raw internal address. `ReadConnections` imports those ports and connects them
  through actual relation bitmaps. Reading does not drain the source.
- **Passages:** `PortStructure.ForBooleanTokens()` explicitly selects the small
  token/control occupancy protocol. Here a whole token can move from an
  occupied slot into a vacant slot, preserving the control bit. It does not
  infer vacancy from an undetectable physical amount. Ordinary ODV owners
  reject passage wiring. This contract is selected once for the local machine;
  it adds no per-value occupancy metadata. `PortSeams` applies the same protocol
  between exposed token ports, with compatible fixed O/D and paired writes.
  This compatibility rule is not a universal restriction on uncalibrated
  connections; richer physical projection/settlement belongs to the native
  carrier composition layer.
- **Topology:** shared fields and references encode correspondence. A family
  can share one value across several links or one seam across opposed uses.
  Reversing a use reverses its endpoints, not its ODV direction or material
  flow. Chain/cycle layouts guarantee successive shared endpoints; they do not
  guarantee geometric reach, a polygon, or a particular construction history.
- **Order:** `LandmarkRun` distinguishes positional chain order from separately
  declared partial comparisons. Missing comparisons remain unresolved; cycles
  are contradictions. `Orders()` enumerates possible completions only on
  explicit request and can be expensive. Incidence balance is not proof of one
  continuous loop. `StrictOrderRead` reports missing support as unavailable,
  distinct from an ambiguous supported claim.
- **Repetition and conditionals:** `PulseCircuit` stores one activation and
  clocked passage rows. A pulse attempts one outgoing transition. Unknown
  guards preserve alternatives without mutation; multiple enabled outlets are
  a conflict, not implicit priority. `ReferenceCycle` counts by visiting sites.
  Equal angular divisions or lengths require another selected contract.

## Small example

```csharp
using Gojo.Encoding.Core.Packed;

// A measured observation: clear V means below detection, not an empty slot.
var observations = new PortStructure();
var measured = observations.Create(0, 0, 1, 0);
var read = observations.Connect(observations.Publish(measured));
byte candidates = read.ReadSupport(); // 00000001: word 000 is admitted.

// A separate, explicitly chosen Boolean-token machine.
var machine = PortStructure.ForBooleanTokens();
int Site(ulong bit) => machine.Publish(machine.Create(0, 0, 1, bit));
var source = Site(1);
var control = Site(1);
var target = Site(0);
var passages = new PassageConnections(machine, PassageLaw.Controlled());
var row = passages.Connect(2, source, control, target);
byte uncertain = passages.Observe(row, visibleRoles: 5); // Omit control.
byte alternatives = passages.Predict(row, uncertain); // Hold or hand off.
bool applied = passages.TryStep(row, uncertain, out _); // false; no mutation.
applied = passages.TryStep(row, passages.Observe(row), out _); // true.
```

`State = S + 2*C + 4*T` is the passage protocol's joint occupancy encoding,
not one ODV word. The eight-byte passage bitmap has bit `before + 8*after`
for each admitted complete transition. Owner-local sequences preserve a shared
scope until the whole selected sequence resolves and commits.

## Promoted sources

Each source below was copied from `experiments/PrimitiveMeasure/` with the same
filename, rather than linked to the experiment executable. No experiment
project reference is required.

| Core files | Reusable mechanism |
|---|---|
| `ThreeBitMeasure.cs`, `ReadSupport.cs` | Three-bit codec and candidate-set reads |
| `PositionalBuckets.cs` | Positionally fixed O/D, packed width groups, retained dirty slots |
| `EndpointRows.cs` | Packed ordered local references, 1–8 roles |
| `BitRelation.cs`, `ConnectionLaws.cs` | Inspectable tuple bitmaps and selected Boolean read factories |
| `PortStructure.cs` | Local storage bindings and stable exposed observation ports |
| `ReadConnections.cs` | Acyclic relation wiring and explicit resolved observation capture |
| `PassageLaw.cs`, `PassageConnections.cs`, `PortStructure.Passage.cs` | Whole-token law, selected-site observation, scoped prediction and commit |
| `PortStructure.Handoff.cs` | Exposed token outlets/intakes and cross-owner seams |
| `OrderedBoundary.cs` | Ordered signed uses, boundary reads and strict-order cycle inspection |
| `FamilyTemplate.cs`, `FamilyTable.cs`, `FamilyCodec.cs` | Shared field/link templates, sparse populated widths, literal transport |
| `FamilyUseTable.cs`, `FamilyUseCodec.cs` | Shared/reversed uses and groups with transport |
| `StructuralRoute.cs` | Pair/chain/cycle layouts, conservative bounded inspection and structural edits |
| `LandmarkRun.cs` | Positional or partial landmark order |
| `PulseCircuit.cs`, `ReferenceCycle.cs` | Clocked conditional token routing and reference cycles |

The focused tests in `Tests/Gojo.Encoding.Core.Tests/Packed` include promoted
endpoint, family-codec, family-use and structural-route regression tests from
the corresponding files in `Tests/PrimitiveMeasure.Tests`. Additional contract
tests exercise current ODV meaning, explicit token protocol selection, shared
uncertainty, migration, cross-owner transfer, partial order and branching.

Promotion changes: namespace isolation; corrected V/read documentation;
removed historical measure arithmetic; explicit Boolean-token construction;
missing strict-order support stays unavailable; reference cycles and composed
read ports choose reference widths from their actual site count rather than a
demonstration quota. Cycle site lists are diagnostic copies.

## Adapter bounds and exclusions

The present .NET packing adapter uses up to 64 bits per field, 16 bits per
endpoint reference and 8 roles per endpoint row. A relation bitmap has 1–3 ODV
roles. A passage scope holds 3–5 Boolean sites in a 32-bit candidate bitmap; a
clock prediction enumerates at most 8 outgoing guards. These are implementation
budgets, not universal limits on concepts, ports, loops or structures.
Serialization counts and zero padding delimit these byte layouts; counts are
not extra conceptual values on every represented element.

This promotion intentionally does not include:

- `ThreeBitMeasure.Add`, `Subtract`, `Compare`, or `ConnectionLaws.Addition`.
  The old arithmetic treated V=0 as a literal zero/additive identity. A finite
  detection read does not justify those laws; encoded equality alone does not
  establish equality or cancellation of underlying quantities.
- The old `ContextTable` alternative, superseded here by positional buckets.
- `ParticipationStrip`/`GateWalk`, whose older participation vocabulary needs
  explicit selection semantics before promotion.
- `ContourPattern`/`RepeatedBody`, glyph-specific constructions, graphical
  settlement, tournament scoring, or learned domain policies. These remain
  experimental; the general reference cycle and clocked passages are reusable.
- A universal port language, material calibration algorithm, global scheduler,
  garbage collector, automatic discovery, or learned machine registry. Core
  supplies connected mechanisms; it does not claim these later layers exist.

Promote a new mechanism when its encoded contract can be stated, its unresolved
cases are preserved, and focused tests cover its structural behavior. Keep the
experiment that supplied the evidence as an independent historical snapshot.
