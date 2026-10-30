# Primitive runtime: a connection before numbers

Implemented experimental checkpoint, 30 October 2026. This starts the
[primitive runtime handoff](../../Gojo/docs/GojoPrimitiveRuntimeHandoff-2026-10-30.md)
at the pre-numeric layer, independently of QuickDraw, recognition and training.

The experiment encodes connected native relations and placeholders as indexed
records, derives finite transition and application tables from native reads,
and executes one persistent occurrence directly on those tables. There is no
text parsing, serialization or native graph expansion during a step. Integer
addresses and support bits are physical storage; they do not assign magnitudes
to the represented participants.

**This checkpoint executes a bounded pre-numeric fragment.** It does not yet
execute Gojo carriers, numeric tension settlement or general arithmetic in the
compact arena. An optional calibrated observer uses the existing native numeric
kernel separately. The [complete Level-one audit](Coverage.md) records that
boundary for every inventory row.

## Native construction

The source is ordinary connected Gojo structure:

```text
landmark order = Closed(A -> B, B -> C, C -> D)
forward = A -> D; backward = D -> A
state = landmark -> heading
state domain = Closed(A->forward, A->backward, ... D->backward)

connection = anchor -> moving
attached connection = moving -> farAnchor
independent connection = anchor -> independent
anchor -> A; farAnchor -> D; independent -> B; moving -> input

law = ((state domain -> command domain) -> state domain) -> Closed(rows)
row = (before state -> input command) -> output support
controller = (input -> law) -> (output -> (output -> input))
current binding = input -> current state support
```

The moving and independent sites initially bind to B. Only the actual shared
moving site joins the first two connections; an equal binding never merges the
independent site. The initial state is B-forward. A pulse follows the selected
adjacent landmark. At an endpoint an explicit row reverses heading without
moving; the next pulse returns along the chain. A separate command reverses
travel. A third command has no admitted row and leaves the binding unchanged.
The UI timer offers pulses; it does not calculate positions or choose responses.
The drawn lines are a schematic view of connections, not an interpolation law.

Whole ordered state tuples are support atoms. `A-forward OR D-backward` is two
alternatives, not four combinations from independent position/direction masks.
Supports supplied for the two operands of a finite read are otherwise a Cartesian
product. Cross-operand correlations require another joint domain.

The application guard is also structure. In
[TransitionApplicationGuard](TransitionApplicationGuard.cs), a singleton pattern
is `seam -> (selected participant -> seam)`: its repeated seam requires closure,
and its one spine cell requires one participant. The selected guard matches
closed singleton before/input/result scopes and exactly one consumed native row
with those same participants and a closed singleton output. The existing
`StructuralConstraint.ApplyUnique` supplies its permission. Capture derives
each atomic permission through this guard; there is no second authored motion
algebra in the fast path.

Open, contradictory, ambiguous and missing-transition states remain distinct.
An open support may have known alternatives as well as an unaccounted tail.
Known outcomes can be read, but the guard does not apply an incomplete or
competing proposal. Missing input pairs and consumed proof rows remain available
in the current receipt. This is guarded structural input, not material transfer:
there is no conserved amount, capacity or carrier tension to invent here.

## Physical representation

These are in-process layouts on the tested little-endian Windows x64 runtime,
not a published binary transport format. Records contain fixed-width words;
arena addresses are unsigned 32-bit indices and address zero is reserved.
The CLR array length bounds the usable arena below the address envelope.
Support bit `i` addresses the `i`th participant in the captured domain, least
significant bit first. Domain order has no implicit numeric magnitude.

| Storage | Fields in physical order | Bytes |
| --- | --- | ---: |
| Arena record | u32 storage kind; u32 left address/slot identity; u32 right address/placeholder face; u32 reserved extension | 16 |
| Support | u64 known alternatives; u32 open-tail address; u32 reserved extension | 16 |
| Current receipt | u64 event ordinal; before/command/result/after supports; u32 missing-pair count; u32 consumed-row count; u32 diagnostic status; u32 extension; u32 state-slot address; 4-byte alignment; u64 occurrence identity | 104 |
| Mutable occurrence payload | current support; current receipt; u64 current event ordinal | 128 |
| Occurrence metadata | u32 state-slot address; u64 occurrence identity, before object alignment | 12 |
| Managed occurrence, measured x64 | payload, metadata, plan reference, object header and alignment | 168 |

Storage kinds select **placeholder** or **ordered relation**, not operations such
as rectangle, move or add. Placeholder records retain an arena-local slot number
and a separate face. Relation records retain ordered child addresses. Identical
relation bytes do not merge identities. Template addresses are qualified by the
occurrence identity; two occurrences sharing a plan do not share mutable sites.
Placeholder face, reversed relation and negative coordinate are not one flag.

Each finite domain has at most 64 participants. The plan stores one u64 result
mask and one derived permission byte per atomic state/command pair, domain
addresses, proof offsets, and actual captured proof-row addresses. This is linear
in the number of atomic pairs, not an exponential table of all possible supports.
The motion fixture uses eight states and three commands.

`00000000` means closed contradiction when the open-tail address is zero. All
eight bits set means every known state. An open-tail address is a separate actual
forward placeholder, and never a magic zero or probability. If both operands
have different open causes, the result carries one representative tail; both
causes remain in the receipt's operands and the reconstructed native read.

Nonzero support/receipt extension words, out-of-domain bits and invalid open
tails are refused. Unsupported element kinds are refused at capture; numeric
extensions will need a new explicit contract. Raw masks and receipts require
their owning plan. They are not self-identifying network packets.

## Live state and inspection

The immutable arena is a physical snapshot of the native definition. The tables
are a derived execution plan with addresses back to that snapshot. Native source
references and lookup maps are retained separately for correspondence and
explicit validation; the step loop never traverses them.

An edit to a source graph does not silently alter this owned snapshot.
`ValidateSource()` is an explicit synchronization boundary; disagreement latches
the plan stale and prevents further application/import. Adopting an edited law
requires a fresh capture and explicit state correspondence. Automatic local
invalidation, topology replacement and retained-state migration remain future
experiments. Snapshot reads remain possible after invalidation.

Each occurrence holds only its current state and current receipt. Event ordinals
use checked arithmetic and express order, not elapsed time. `Import` explicitly
replaces a binding and clears its prior current receipt. `Reset` starts a new
local event sequence while preserving occurrence identity. Idle state can resume
when another pulse arrives; this does not claim structural detach or dormancy.

`PrimitiveMotion.Inspect()` reconstructs an independent native inspection on
demand, preserving aliases across the fixture, input binding, output, law,
guard and current read. It uses frozen records, so a captured witness does not
drift when the working occurrence advances. Callers may retain selected receipts
or inspections; full history is not required. These are current before/after
evidence, not a complete native `TimeStepSequence` or saved-world replay. In
particular, stale-source validation cause and event ordinal remain compact
receipt evidence. Native inspection of actual cyclic relation objects is not
implemented; ordinary placeholder seam closure is supported.

## Optional numeric observation

Calibration connects the same A/B/C/D identities to native points at
`0, 3, 8, 12`. The unequal gaps make the separation from ordinal addresses
visible. A selected positive quantum projects through a corresponding native
frame; observer reversal selects the reverse frame without changing motion.
The witness retains the selected landmark, input location, projection, result
and exact source-basis residue.

For B at quantum 4, the forward read is 0 coarse ticks with residue +3 native
ticks. The reversed read is 2 coarse ticks with residue -1 native tick:
`3 = 12 - 2*4 - 1`. This is an exact quotient/remainder observation, not deliberate
loss or material settlement. It allocates outside the step loop. No metric
calibration is created until requested.

## Run and verify

From the repository root:

```powershell
dotnet run --project experiments/PrimitiveRuntime.Viewer -c Release
dotnet run --project experiments/PrimitiveRuntime -c Release -- --steps=1000000 --output=D:/_quickDraw/_work/primitive-runtime/benchmark.json
dotnet test Tests/PrimitiveRuntime.Tests/PrimitiveRuntime.Tests.csproj
dotnet test Gojo.slnx
```

The viewer has play/pause, single-step, reset, separate travel/observer reversal,
open and alternative bindings, a blocked input, and optional calibration with
quantums 1, 2 and 4. See its [rendering instructions](../PrimitiveRuntime.Viewer/README.md)
for headless PNG inspection. [Measured findings](Findings.md) separate construction,
capture, explicit validation, direct execution and optional inspection costs.

Focused tests cover native/direct finite support and application conformance,
shared versus independent sites, correlated alternatives, open/empty support,
placeholder faces, independent occurrences, refused input, frozen inspection,
invalid encodings, source invalidation, projection residue and bounded live
state without per-step allocation. The next numerical slice should encode
carrier/tick identity, stable unit promises and a selected tension settlement
law before attempting the handoff's turning rectangle or contour experiments.
