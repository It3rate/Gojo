# Gojo.EncodingC

Status: **exploratory**, 10 November 2026. It replaces `Gojo.EncodingB`, which
was deleted once this project covered what was worth keeping. The old project
is in git history (`git show c9d4abb2:Gojo.EncodingB/README.md`).

```powershell
dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj
```

## The goal: a lean, bit-level encoding

This project encodes structures that compute, using as little as possible:
bits at positions, small tables of bits, and the order in which things are
wired. Concretely:

- **Structures** are built from bit encodings in the sense of ODV. Each bit's
  role comes from its position, with no header and no name.
- **Ports** carry what flows from one structure to another.
- **Values** flow through the structures once they are running.
- **What structures do with them:**
  - run operations;
  - branch and merge flow;
  - switch and change their own structure;
  - copy a selection into a workspace and change it there;
  - learn laws from outcomes.

At this level everything stays primitive and minimal. Higher-level concepts are
meant to *emerge* from structures like these, as they crystallize. They are not
written into the encoding.

Every choice is checked against the substrate rule in the canonical prompt
(§9.6): the concept must be as expressible by a small organism or an analog
circuit as by a computer. Positions, bits, tables of what may follow what, and
an order of events pass that test. Strings, type tags and 64-bit tiles do not.
Packing bits into machine words is how this .NET adapter runs. It is never part
of what something means.

### What this project refuses

- **No strings in the encoding.** Planes, positions, roles, values, ports and
  laws are numbers. Names belong to a [`Lens`](Lens/Lens.cs) that the encoding
  never reads. `TheEncodingHoldsNoStrings` checks every type by reflection.
- **No fixed sizes.** A structure has as many positions as it needs, and a
  plane is as long as its structure. A law runs across as many lanes as it has
  rows, and a reference costs ⌈log₂ n⌉ bits for n targets. The remaining bounds
  belong to this adapter, not to the concepts:
  - a unary count holds up to 63;
  - a binary code has up to 65,536 values;
  - a law table has up to 4,194,304 bits, so it can be checked exhaustively
    when it is made.
- **No descriptions on ports.** A port is ordered lanes and planes. It carries
  no unit, type, value names, law hashes or accounting class (canonical prompt
  §4.6).
- **No flags for copying or moving.** Whether a connection reads or takes is
  visible in its law: which roles the table changes, and whether a preserved
  sum covers them.
- **No required calibration.** A new connection is one-to-one and uncalibrated.
  A calibration is a law or a small structure that someone chose to wire.
- **No meaning in code artifacts.** A class or enum here organizes bits. What a
  bit means is what the laws wired to it do.

## Nouns

| Noun | Types | What it is |
| --- | --- | --- |
| Plane | [`Plane`](Substrate/Plane.cs) | A bit per position. State, selections, lane sets and supports are all planes |
| Layout | [`Shape`](Substrate/Shape.cs), [`Field`](Substrate/Field.cs), [`PackedInts`](Substrate/PackedInts.cs) | How a value sits in bits (binary or unary), which planes hold it, and references packed at the width they need |
| Structure | [`Structure`](Substrate/Structure.cs) | Positions and planes, constant planes, a live plane, version and epoch |
| Port | [`Port`](Substrate/Port.cs) | Ordered lanes onto an owner's positions and an ordered list of its planes |
| Law | [`RelationLaw`](Laws/RelationLaw.cs), [`TransitionLaw`](Laws/TransitionLaw.cs), [`Accounting`](Laws/Accounting.cs) | Tables of admitted tuples or transitions, with promises checked when the table is made |
| Wiring | [`Wiring`](Flow/Wiring.cs), `Binding` | A law bound to rows over structures and ports. Topology is kept apart from rule |
| Engine | [`Engine`](Flow/Engine.cs), [`Unary`](Flow/Unary.cs), [`Flows`](Flow/Flows.cs) | Steps wirings in waves; units enter from outside; stock laws for moving |
| Workspace | [`Workspace`](Workspaces/Workspace.cs), [`Supports`](Workspaces/Supports.cs), [`Selections`](Workspaces/Selections.cs) | A selection copied into its own structure, validated writeback, and possible-value planes |
| Learning | [`LearnedTable`](Learning/LearnedTable.cs) | A transition table learned from what happened, crystallized into a law |
| Measure | [`OdvWord`, `Odv`](Measure/Odv.cs) | The three-bit ODV word, its planes and its arithmetic as laws |
| Lens | [`Lens`, `OdvNotation`](Lens/Lens.cs) | Names and notation for people, outside the encoding |

## Where the bits are

**Planes and structures.** Bit i of a plane belongs to position i. A structure
holds:
- one plane per stored plane, the length of its structure;
- a *constant plane* for anything the whole structure shares, held as a single
  bit and never per position (ODV orientation is one);
- a live plane;
- two counters: `Version` advances on every state change, and `Epoch` on every
  structural one.

Retired positions are never reused, so a reference cannot silently change
target.

**Shapes.** A binary value of size s takes ⌈log₂ s⌉ bits, holding the code
Σ bitₖ·2ᵏ. A unary count of capacity k takes k bits, where bit j means "more
than j", so 3 is `0111`:
- adding one shifts a bit in, and removing one shifts a bit out, at every
  selected position at once;
- comparing two counts is a mask test;
- only prefixes decode. A broken count is reported as such, never repaired.

Unary is the primitive medium: more of something sets more bits, and no
arithmetic unit is needed.

**References.** A wiring row stores one reference per role, packed back to back
at the width the largest reference needs. When a larger one arrives the run is
repacked. Port lanes are stored the same way.

**Laws.**
- A relation sets one bit per admitted tuple. The tuple index is mixed-radix
  with role 0 lowest.
- A transition sets bit `before + N·after`, where N is the number of whole
  states.
- The passage law (source, gate, target) is the eight bytes
  `01 02 04 00 10 20 48 80`, which as one integer is the LUT6 value
  `0x8048201000040201`. Run across lanes, its sum of products reduces to
  `S' = S & (~C | T)` and `T' = T | (S & C)`.
- Laws run *across lanes*. For each before-state, the lanes in that state are
  the AND of the matching code planes, and those lanes take the after-state's
  bits. Lanes whose bits are not codes, or whose state admits no transition or
  several, keep their bits and are reported.

**ODV.**
- V and D are stored planes 0 and 1. O is constant plane 2, so a whole
  structure reorients with one constant flip and `D ^= live`.
- All eight words pack as `88 C6 FA`.
- `Odv.Addition(threshold)` derives addition by enumerating a threshold reader:
  - Threshold 1 is the exact-zero contract, the historical table.
  - Threshold 2 or more is the detection contract. It widens six ordered pairs:
    `+0 + +0 → {+0, +?}`, `−0 + −0 → {−0, −?}`, and the four mixed pairs
    `(+0, −?)`, `(−?, +0)`, `(−0, +?)`, `(+?, −0)`.
  - One position read twice admits only diagonal tuples. So `X + X`, with X
    either `+?` or `−?`, excludes zero.

**Workspace.** The workspace keeps three things:
- its own structure, with the same planes;
- a map back to the source, packed, with 0 marking an introduced position;
- a copy of the bits it read.

The commit rule compares that copy with the source.

**Supports.** One plane per value. Every value set is open, one is settled,
none is a contradiction.

## Copying and moving

A port says nothing about whether it may be read or taken. That follows from
the law a receiver wires to it:

- A role the law never changes is **read**. Through a port, that is a
  reference: the sender is untouched, and any number of receivers can hold one.
- A role the law changes is **written**:
  - Writing a structure's own positions is that structure's business.
  - Writing *through a port* reaches into another structure. It is allowed only
    when the law's accounting puts the role in a preserved sum, so what leaves
    there arrives somewhere the law also writes. That is a move, and nothing can
    be made or destroyed in another structure. A law that would drain a port
    without accounting for it is refused when it is wired.

Different receivers may take the same flow in their own terms. In
`TwoReceiversCanTakeTheSameFlowInTheirOwnTerms`, an "eye" reads the light by
reference while a "body" takes it as units of heat.

## Schedule

- **Within a wiring.** Rows that write a position another row touches run in a
  later wave, after the earlier row; the rest run together. Row order decides
  which goes first.
- **Across wirings.** The engine does the same with wirings, in the order they
  were added.
- **Same answer either way.** Conflicting work always lands in different waves,
  so running wirings one after another in declared order gives exactly the
  result of running each wave at once. `EngineStep.Waves` reports which wirings
  could have run together.

Declared order is structure, recorded and replayable. It is not a hidden
priority, and it is what stops two takers from both having one unit
(`TwoTakersCannotBothHaveOneUnit`). Branches and merges are rows of one wiring
in declared order (`BranchesAndMergesAreRowsOfOneWiringInDeclaredOrder`).

## Calibration

None is needed to connect. `Flows.Move` is one-to-one. When a structure wants a
rate, it wires one:
- `Flows.Convert(take, give, …)` preserves give·source + take·target. Two units
  of light for one of heat is that, with the remainder staying with the sender.
- A calibration can also be a small structure of its own. In
  `ACalibratorIsJustASmallStructureWithAnInputAndAnOutput`, an oscillator's bit
  is read by reference by an edge counter (input, last input, unary count). The
  counter's output is a value for the frequency, and anything can read it.
  Another reader takes the same signal raw.

Where a calibration lives (in the sender, in the receiver, or standing alone)
is left to whatever evolves the structures.

## Changing structure

- **Flow.** A structure's own laws write its own bits. Gates are bits, so a
  structure switches its own flow (`AStructureSwitchesItsOwnFlowWithItsOwnBits`).
- **Topology.** These operations are structural edits that advance the epoch:
  - adding positions or planes, and retiring positions;
  - publishing a port;
  - rebinding or extending a lane;
  - adding, rewiring or retiring a row.

  A port's owner can move what a lane shows, and every connection follows,
  because connections hold lane numbers.
- **Through a workspace.** Introduced positions are adopted into the source on
  commit.

## Workspaces

A structure holds what is. A workspace holds what is noticed, supposed or
attempted:
- **A copy.** `new Workspace(source, selection)` copies the selected positions
  into a structure of its own, and laws run on it like on any structure.
- **Mapping.** `Map(wiring)` copies the source rows whose positions are all
  selected. A row that reaches an unselected position cannot run there: the law
  would see only part of what it accounts for. It is reported as missing.
- **Commit.** `Commit()` writes back only if nothing the workspace read has
  changed in the source. Otherwise it writes nothing and returns the stale
  positions.
- **Introduced positions.** A position introduced in the workspace is a
  candidate. It is never read as evidence about the source, and it is added to
  the source on commit.
- **Prediction.** A workspace that is never committed is a prediction.

## Learning

`LearnedTable` counts witnessed (before, after) pairs and reads each pair one of
three ways:
- **admitted:** seen often enough;
- **unsupported:** its before-state was seen often enough without it;
- **unknown:** otherwise. Unknown pairs keep predictions open rather than
  guessed.

Once nothing is unknown, the table crystallizes into a law. That law is checked
against the accounting it is asked to keep, like any authored law. A table that
would make or lose units is refused, and the breaking pairs are returned. In
`AStructureLearnsThePassageLawFromWhatHappened`, eight rows in eight states are
run four times, and the learned table equals `Flows.Passage` bit for bit.

## Non-signal parts

| Kind | Where it lives |
| --- | --- |
| Not selected | Outside the workspace's selection plane: no claim either way |
| Open / competing | A support plane per value; more than one set |
| Contradiction | A support with no value set |
| Not a code | `WiringStep.Unadmitted`: the bits stay as they are and are reported |
| Several possible next states | `WiringStep.Ambiguous`: never picked from |
| Retired | `WiringStep.Skipped` |
| Remainder / blocked | What did not move stays where it was; the lane is unchanged |
| Overflow / underflow | Returned by `Unary.Add` and `Unary.Remove` as planes |
| Stale | `Commit.Stale`; nothing is written |
| Missing scope | The `Missing` rows from `Workspace.Map` |
| Introduced | `Workspace.Introduced`; never source evidence |
| Unknown law rows | `LearnedRead.Unknown`; predictions through them stay open |
| Prediction | `TransitionLaw.PredictJoint`, or a workspace never committed |
| Lossy read | A relation law such as presence over a count. It bounds what it dropped but cannot recover it (`ACoarseReadIsARelationAndCannotRecoverWhatItDropped`) |

## Where the ideas came from

**Kept from EncodingB**, with every name removed:
- the bit-sliced law engine: tables, sum-of-products steps, narrowing, joint
  prediction;
- accounting checked at load;
- unary counts;
- ODV and its derived contracts;
- learned tables;
- selection as a plane;
- the workspace commit rule;
- waves by declared order.

**Borrowed as ideas from [`Gojo.Encoding/Core`](../Gojo.Encoding/Core/README.md)**
(no code was copied):
- **Packed references.** References stored at the width the count needs, with
  no header (`EndpointRows`).
- **Lanes through a binding.** A port's lane goes through an owner-local
  binding, so the owner can move what a lane shows without breaking a
  connection (`PortStructure` bindings and migration).
- **Constants by position.** Values shared by a group are supplied by position
  rather than stored; here that is the constant plane (`PositionalBuckets`
  fixing O and D).
- **Retire without reuse.** Retiring takes a position out of use and never
  reuses it (`PositionalBuckets.Retire`).
- **No hidden priority.** Several enabled outlets are a conflict, not an
  implicit priority (`PulseCircuit`). Here the order is declared structure, and
  a branch is one wiring.
- **Layout limits belong to implementations,** not concepts.

**Not taken from `Gojo.Encoding/Core`:**
- Width and orientation buckets. They are good storage packing but add groups
  this core does not need yet.
- The explicit Boolean-token protocol. Here a move is evident from its law.
- The native relation encodings, which depend on the kernel.

**Dropped from EncodingB:**

| What | Why |
| --- | --- |
| Site grammars and descriptors | Meaning lived in English sentences attached to each bit |
| The bit decoder | Replaced by the lens, which sits outside the encoding |
| Tiles and the 64-site cap | Replaced by structures and planes of any size |
| `PortBasis`, `Correspondence`, `PortAccounting` | Ports described their meaning, and copying versus moving was a flag |
| `RungMap` | A coarse read is a relation law and needs no type of its own |
| `TensionField` | Units and laws over unary fields cover it |
| `RouteTable`, `GapBalance`, `Energy`, the phased `Runtime` with checkpoints and replay | Host mechanisms that should become laws |
| `JointSupport`, `UncertaintyReads` | Host-side boolean arrays and recursion |
| Closed or open selection machines, `PredictionWorkspace` | Not needed in this core |
| Confluence classes, provenance strings | Not needed in this core |

## Not built

- **Laws that edit topology.** Positions, rows and lanes change through host
  calls, which a workspace commit can carry. No law writes a reference yet.
- **A general workspace across several structures.** A workspace copies a
  selection of one structure.
- **The full tension model:** positive and negative tension, an absorbed
  magnitude, an absorb law, and accounted loans.
- **Replay from checkpoints, energy tiers and missing-route witnesses.**
- **Joint supports beyond one position, decorrelation records and semiring
  reads.**
- **Composing calibrations, and discovering them** (the canonical prompt's
  "evolved calibration" boundary).
- **A parallel backend.** Wave assignment uses host dictionaries, linear in rows
  × roles; `Plane` words are where a SIMD or GPU backend would attach.
- **A bridge to the kernel.** Consolidation §5.2 proposes one; it is not built.

## Status against the consolidation roadmap

| Step | Here |
| --- | --- |
| R1 substrate contract | Planes, laws with accounting, ODV and the passage law. The decoder is replaced by the lens. Gap balancing is not ported |
| R2 house workspaces | Workspace copy, missing rows, stale and introduced commits. The house seam example is not ported |
| R3 scoped supports | Per-position supports and narrowing (U0). U1–U3 are dropped |
| R4 semiring reads and zero contracts | Exact and detection ODV contracts. Semiring reads are dropped |
| R5 tension and energy | Units enter through `Unary` and move through laws. Energy tiers are dropped |
| R6 tick contract | `Engine` with waves in declared order. Shuffled-order tests are not done |
| R7 witnesses by replay | Not built |
| R8 learned law tables | Implemented, without eligibility traces |
| R9 SIMD / GPU | Out of scope; `Plane` words are the seam |
| R10 crystallization by description length | Not done |

## Boundary

Like `Gojo.Encoding`, this project does not own Gojo meaning and does not
depend on the kernel. Under `AGENTS.md`, it is an exploratory substrate. A
mechanism that proves itself here is promoted into
[`Gojo.Encoding/Core`](../Gojo.Encoding/Core/README.md) with its grammar,
limits and tests.

## Tests

| File | What it shows |
| --- | --- |
| `SubstrateTests` | Planes of any length; growth and retirement; references at the width they need; constant planes; shapes; units entering as units, with overflow; the encoding holds no strings |
| `LawTests` | The passage bytes and LUT6 value; accounting refusal with the breaking pair; 130 lanes in one step; unary moves; bits that are not codes; one position used twice; a coarse read as a relation; tables compared by content; joint prediction |
| `FlowTests` | Ports as lanes and planes, and rebinding; references to one sender from many readers; writing through a port only as a move; one-to-one uncalibrated moves with a remainder; two receivers interpreting one flow; two takers and one unit; branches and merges; self-switching flow; a calibrator structure; a wired rate; retired positions |
| `OdvTests` | Packing `88 C6 FA`; the two zero contracts; one position read twice; reorientation; addition narrowed across 100 rows; threshold calibration |
| `WorkspaceTests` | Copy and commit; stale commits; rows outside the selection; introduced positions adopted; supports to a fixed point and contradictions; selections from structure |
| `LearningAndLensTests` | Learning the passage law; refusing a learned table that makes units; unknown pairs staying open; a lens that the encoding never reads |
