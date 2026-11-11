# Gojo.EncodingC

Status: **exploratory**, 11 November 2026. It replaces `Gojo.EncodingB`, which
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
  - work in rungs: live views of a structure that copy only what they change;
  - hold what is not settled yet in the same planes as what is, and use it:
    open values, choices nothing has made, and the gap between what was
    supposed and what is;
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

  One bound is deliberate: a support or a projection tells apart at most 64
  values, and refuses more instead of silently merging value 64 into value 0.
  That bounds one primitive read, not a structure. Primitives are coarse
  (canonical prompt §9.6); finer resolution comes from ladders of coarse rungs.
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
| Surface | [`Surface`](Substrate/Surface.cs) | What a law's role binds to: a structure, a port, a rung, or the gap between a rung and its source. Every read and write goes through it |
| Structure | [`Structure`](Substrate/Structure.cs) | Positions and planes, constant planes, a live plane, version and epoch |
| Port | [`Port`](Substrate/Port.cs) | Ordered lanes onto an owner's positions and an ordered list of its planes |
| Law | [`RelationLaw`](Laws/RelationLaw.cs), [`TransitionLaw`](Laws/TransitionLaw.cs), [`Accounting`](Laws/Accounting.cs) | Tables of admitted tuples or transitions, with promises checked when the table is made |
| Wiring | [`Wiring`](Flow/Wiring.cs), `Binding` | A law bound to rows over structures and ports. Topology is kept apart from rule |
| Engine | [`Engine`](Flow/Engine.cs), [`Unary`](Flow/Unary.cs), [`Flows`](Flow/Flows.cs) | Steps wirings in waves; units enter from outside; stock laws for moving |
| Rung | [`Rung`](Workspaces/Rung.cs), [`Gap`](Workspaces/Gap.cs), [`Selections`](Workspaces/Selections.cs) | A workspace: a live view with copy on write, selection by layers and by use, validated partial commits. Its gap is where it and its source disagree |
| Supports | [`Supports`](Workspaces/Supports.cs) | A value not settled yet: one plane per value in the structure, set where the value is ruled out |
| Learning | [`LearnedTable`](Learning/LearnedTable.cs) | A transition table learned from what happened, crystallized into a law |
| Measure | [`OdvWord`, `Odv`](Measure/Odv.cs) | The three-bit ODV word, its planes and its arithmetic as laws |
| Transport | [`Transport`](Transport/Transport.cs) | A machine as bits, restored without the code that built it |
| Lens | [`Lens`, `OdvNotation`](Lens/Lens.cs) | Names and notation for people, outside the encoding |

## Where the bits are

**Planes and structures.** Bit i of a plane belongs to position i. A structure
holds:
- one plane per stored plane, the length of its structure;
- a *constant plane* for anything the whole structure shares, held as a single
  bit and never per position (ODV orientation is one). It is positional, like
  the core's buckets: belonging to the structure gives a position the bit, so
  everything in it turns together, and turning one word means moving it into
  a structure that faces the other way;
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
- only prefixes decode. A broken count, such as `010`, is noise this read
  cannot interpret: `Unary.Add` and `Unary.Remove` report it in
  `UnaryStep.Broken` and leave its bits alone, never shifting it into a count
  that looks valid.

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
  several, keep their bits and are reported, each kind apart.
- A transition table is also a relation between a before and an after, with
  the same bits (`AsRelation`), so supports can be narrowed through a law in
  either direction.
- Where a table admits several next states, `WithChoice` returns the same law
  with one more role that picks among them.

**ODV.**
- V and D are stored planes 0 and 1. O is constant plane 2, so a whole
  structure reorients with one constant flip and `D ^= live`.
- All eight words pack as `88 C6 FA`.
- `Odv.Addition(threshold)` derives addition by enumerating a threshold reader:
  - The default is the detection contract, under which an undetectable word
    is still an operand. Every threshold from 2 up gives the same table, so
    detection is one contract, built once; a test checks it against a
    reference reader at every threshold up to 7. Against the exact-zero table it widens six
    ordered pairs: `+0 + +0 → {+0, +?}`, `−0 + −0 → {−0, −?}`, and the four
    mixed pairs `(+0, −?)`, `(−?, +0)`, `(−0, +?)`, `(+?, −0)`.
  - `Odv.ExactZero` (threshold 1) is the historical contract that reads
    undetectable as a hidden zero. A reader has to ask for it.
  - One position read twice admits only diagonal tuples. So `X + X`, with X
    either `+?` or `−?`, excludes zero.
- A zero read is a sample, not nothing: the word keeps its orientation and its
  place in the computation. Nothing is what is not selected.

**Rung.** A rung keeps, per plane it has written, three planes over its
positions: which bits it holds, its value for them, and the source bits they
replaced. Positions it has not written cost nothing. It also keeps:
- its selection, as a short list of layers (an area plane or "all", and a
  keep-or-ignore bit);
- a unary count per position brought in by use: how many steps are left;
- packed references for introduced positions (0 while a candidate, s + 1 once
  adopted as source position s);
- a packed parent per position for changes made together.

**Supports.** One plane per value, in the structure, set where the value is
*ruled out*. Nothing ruled out is open, all but one is settled, all is a
contradiction. A fresh position is therefore open with no extra step, and each
bit set is one thing learned. "Every value" means every value of a declared,
finite set; a support says nothing about values outside it. A one-bit value
costs two planes: unknown, 0, 1 and contradiction.

**Transport.** A machine saves as one run of bits: structures (plane flags,
live plane, stored planes, ports), laws (role shapes, table bits, accounting),
and wirings (law, bindings, live rows, and each role's references packed at
the width they need), in the engine's order. Whole numbers are Elias gamma
codes of n + 1, so 0 takes one bit. There are no names, headers or fixed
widths; a structure is identified by its place in the order. The passage
chain of five positions and two rows is 25 bytes, a third of it the table.

## Copying and moving

A port says nothing about whether it may be read or taken. That follows from
the law a receiver wires to it:

- A role the law never changes is **read**. Through a port, that is a
  reference: the sender is untouched, and any number of receivers can hold one.
- A role the law changes is **written**:
  - Writing a structure's own positions is that structure's business.
  - Writing *through a port* reaches into another structure. It is allowed only
    when the law's accounting puts the role in a preserved sum of positive
    amounts, so what leaves there arrives somewhere the law also writes. That
    is a move, and nothing can be made or destroyed in another structure. A
    law that would drain a port without accounting for it is refused when it
    is wired. A sum with a zero or negative weight is still a promise, but
    not conservation, and does not count.
- **One bit is one position.** A binding or a port names each plane once, so
  no role can count one bit twice. References are live, so a port's owner can
  move a lane after a row was wired. A row whose roles now meet at a bit one
  of them writes would let the law count one thing twice. It does not run, and
  it is reported in `WiringStep.Aliased` until the lane moves again.
- **A supposition is never mixed with what is.** A law that writes a rung
  writes only that rung; the rung's commit decides what reaches anything else.

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

Each row's before and after are read around its own wave. A unit that runs
down a chain in one step is then recorded as a handoff at each row, not as a
unit destroyed at the first row and made at the last, which is what a
learner would otherwise learn.

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
- **Through a rung.** Values, and introduced positions, reach the source when
  a rung commits.

## Rungs (workspaces)

A structure holds what is. A rung holds what is noticed, supposed or attempted.

- **Reference first.** `new Rung(source)` copies nothing. Every position it has
  not written reads the source as the source is now, so a rung sees the world
  keep moving. A live read can be slightly out of date; the system keeps
  running and later reads correct it.
- **Copy on write.** Writing a position copies the whole position in, every
  plane but the constants, so a count spread over several planes is never half
  the rung's and half the source's. A rung costs what it changes, not what it
  sees, so many can be open at once.
- **Laws run unchanged.** A rung is a surface, so a wiring binds to it like to a
  structure. `rung.Apply(wiring)` runs it; a law run in a rung writes only the
  rung.
- **Selection, coarse to fine.** `SelectAll()`, `Select(area)` and
  `Ignore(area)` add layers; later layers override earlier ones. Nobody lists
  positions. "All" follows the source as it grows.
- **Selection by use.** A row that touches a selected position and unselected
  ones brings those in when it runs, one step further from the request, up to
  the rung's reach (three by default). A row that would go further, or into an
  ignored area, does not run and is returned in `RungStep.Beyond`.
- **Constants** are read by reference and cannot be changed in a rung. Every
  position of the structure shares them.
- **Leaving a rung:**
  - *Drop it.* Nothing in the source changed.
  - *Compare it.* `Changes()` is where it reads differently from the source.
    Checked after the source has moved on, that is how far a prediction
    missed (`APredictionIsARungComparedWithWhatTheSourceThenDid`). `Differs`
    compares two sibling rungs over one source.
  - *Revert part of it.* The positions read the source live again. Changes
    made together are reverted together: a unit moved into an introduced
    position goes back to its donor, and the candidate stays, empty.
  - *Commit part or all of it.* Everything is checked before anything is
    written. A position whose source moved or was retired since it was copied
    is stale, and a stale commit writes nothing. Changes one row made together,
    such as both ends of a move, commit together or not at all (`Split`). The
    wiring records them, however it is run, and a commit into a parent rung
    keeps them together there.
  - *Adopt what it introduced.* `Introduce()` adds a candidate position with no
    source. It is always selected, never read as evidence about the source,
    and becomes a source position on commit.
- **Rungs over rungs.** A rung over a rung sees its parent's changes live, and
  commits into the parent, not the structure underneath.

## Working with what is not settled

Everything that flows in is uncertain until something accounts for it, and the
places where a structure can change are the places that are not settled. So
what is unsettled is kept in the same planes as what is settled, where laws
can read it and rungs can suppose about it. Four pieces do this.

- **Open values.** A field's bits hold one value. Its supports hold what the
  value may still be (`Supports.On`). `Observe` narrows a position to what its
  bits read; a relation wiring narrows every row at once (`Supports.Narrow`,
  `Propagate`); `Forget` opens a position again; `Enact` writes a settled
  value into the bits. Seen through a rung (`supports.In(rung)`), all of this
  is supposed rather than known: a supposition that reaches a contradiction
  is dropped and the structure never held it
  (`ASuppositionIsNarrowedInARungAndDroppedWhenItContradicts`).
- **The gap.** A rung differs from its source only where it was written.
  `rung.Gap` shows the difference bit by bit as a surface, read by reference
  and never stored. If the rung ran a law ahead, the gap after the source
  moves is how the prediction missed. If a wanted state was written into the
  rung, the gap is what is left to do. A law bound to it turns a miss into
  less trust (`AMissIsASignalALawReadsAndTrustFollowsIt`).
- **Choice.** A law that admits several next states does not run alone.
  `WithChoice` gives the choice a role, which can be bound to a bit a
  structure sets, or left open and narrowed from what happened
  (`AmbiguityBecomesAChoiceSomethingCanMake`).
- **A law read both ways.** `AsRelation` is the same table read as a relation
  between now and next. Bind its before roles to a rung of now and its after
  roles to a rung of the next moment.

Together these are a way to move toward a goal by using what is open
(`AGoalSettlesTheOpenChoiceThatReachesIt`):

1. Observe what is known now.
2. In one rung, forget the places that may be changed: a valve, a choice.
3. In another rung, the next moment, forget everything, because it has not
   happened, then write the wanted state and observe it there. The prediction
   is set to the goal.
4. Narrow through the law as a relation between the two rungs. The open
   places settle to what reaches the goal, or the rungs reach a contradiction
   and are dropped, leaving what is known as it was.
5. Enact and commit the settled choices. That commit is the decision.
6. Run the law for real. The gap of the next-moment rung empties as the
   source arrives, or shows where it did not.

For continuous steering no planning is needed. A law that reads a level and
the wanted level, held in a rung, opens whichever valve closes the gap; the
flow brings the structure to the goal, follows the goal when it changes, and
sends on a unit that arrives unasked
(`FlowIsSteeredTowardAGoalByTheGapItLeaves`).

## Learning

`LearnedTable` counts witnessed (before, after) pairs and reads each pair one of
three ways:
- **admitted:** seen often enough;
- **unsupported:** its before-state was seen often enough without it;
- **unknown:** otherwise. Unknown pairs keep predictions open rather than
  guessed.

History is kept small: counts, and the last few ticks that witnessed each pair.
The counts are readable (`Count`, `Seen`), so how sure the table can be is
evidence, not only what it says. Rows it was shown and could not read are
counted too (`Unread`).
"Unsupported" is a rule of thumb, not proof. A crystallized law marks the pairs
it left out only for not being seen (`Crystallized.Unsupported`), and the table
keeps counting, so a later witness admits them again.

The intended next step is prediction by structure: record a little history,
then let a simple rule, such as an operation that stretches a value by two and
is compared with the next input in a rung, stand in for the table.

Once nothing is unknown, the table crystallizes into a law. That law is checked
against the accounting it is asked to keep, like any authored law. A table that
would make or lose units is refused, and the breaking pairs are returned. In
`AStructureLearnsThePassageLawFromWhatHappened`, eight rows in eight states are
run four times, and the learned table equals `Flows.Passage` bit for bit.

## Non-signal parts

Signal and noise are encoded together, as a layout has content and whitespace.
The table says where each unresolved kind lives today. *Bits* means planes of
a structure, which laws read and transport saves. *Rung* means a rung's own
planes, which are a working view and not saved. *Live* means a difference
read by reference while both sides exist. *Returned* means a record handed to
the caller after one call; nothing in the machine keeps it. *Beside* means a
host object next to a structure.

| Kind | Where it lives | Kept |
| --- | --- | --- |
| Not selected | Outside a rung's selection: no claim either way. The nearest thing to nothing | Rung |
| Ignored | A rung's ignore layer; use never brings it in | Rung |
| Open / competing | Supports with several values not ruled out | Bits |
| Contradiction | Supports with every value ruled out | Bits |
| A choice nothing has made | The choice role of `WithChoice`, bound to a bit or left open | Bits |
| Not a code | The bits stay as they are; `WiringStep.Unread` reports them | Bits |
| Broken count | `Unary.Broken`, `UnaryStep.Broken`; never repaired | Bits |
| Undetectable magnitude | ODV V = 0: a sample below this read's resolution, still an operand | Bits |
| Retired | The live plane; `WiringStep.Skipped` | Bits |
| Aliased | `WiringStep.Aliased`: roles that meet at a written bit, after a lane moved. Found again from the lanes | Bits |
| Remainder / blocked | What did not move stays where it was; the lane is unchanged | Bits |
| Lossy read | A relation law such as presence over a count. It bounds what it dropped but cannot recover it (`ACoarseReadIsARelationAndCannotRecoverWhatItDropped`) | Bits |
| Supposed | What a rung holds: bits written and supports narrowed there | Rung |
| Stale | The rung's basis planes; `RungCommit.Stale`, and nothing is written | Rung |
| Changed together | The rung's groups; `RungCommit.Split` | Rung |
| Introduced | `Rung.Introduced`; never source evidence | Rung |
| Prediction missed / goal not reached | `Rung.Gap`: the bits where a rung and its source disagree | Live |
| A state the law is silent on | `WiringStep.Unadmitted`; the lane keeps its bits | Returned |
| Several possible next states | `WiringStep.Ambiguous`: never picked from, and the lane keeps its old bits | Returned |
| Overflow / underflow | `UnaryStep.Blocked` | Returned |
| Beyond reach | `RungStep.Beyond`: rows that would reach too far from the request | Returned |
| Left out for not being seen | `Crystallized.Unsupported`; the law it comes with does not keep it | Returned |
| Unknown law rows, how often a pair was seen, rows a learner could not read | `LearnedTable`: `Read`, `Count`, `Seen`, `Unread` | Beside |

Open values, choices and the gap are now in the planes or read live, so laws
can react to them. What a step did not resolve, overflow, and a learner's
counts are still reported rather than encoded (see [Not built](#not-built)).

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

- **The rest of uncertainty in the planes.**
  - Step evidence (changed, unread, unadmitted, ambiguous, blocked) written to
    planes a law or a rung's selection can read, so attention can go where
    things did not resolve and a row's flow can be counted.
  - A step through an ambiguous law that writes "one of these" into supports
    instead of keeping the old bits.
  - Overflow carried up a ladder rung instead of handed back.
  - A learned table held in planes and saved, and a learned law that keeps
    which of its exclusions were guesses.
- **Blame.** The gap says where a result differs from what was wanted, not
  which earlier choice caused it. Narrowing back through one step finds the
  choice; a chain of next-moment rungs for several steps is not built.
- **Narrowing inside a rung's selection.** `Supports.Narrow` on a rung does not
  yet go through the rung's selection and reach.
- **Supports about what a port shows**, and supports that vary together
  (joint supports beyond one position).
- **Laws that edit topology.** Positions, rows and lanes change through host
  calls; a rung commit can add positions it introduced. No law writes a
  reference yet.
- **A rung across several structures.** A rung views one structure or rung.
- **Relation wirings in a saved machine.** Transport saves an engine, which runs
  transition laws.
- **Prediction structures that replace a learned table.** A trust count can
  follow a rung's misses through its gap, but nothing yet adopts or drops a
  rung on that count.
- **The full tension model:** positive and negative tension, an absorbed
  magnitude, an absorb law, and accounted loans.
- **Replay from checkpoints, energy tiers and missing-route witnesses.**
- **Decorrelation records and semiring reads.**
- **Composing calibrations, and discovering them** (the canonical prompt's
  "evolved calibration" boundary).
- **A parallel backend.** Wave assignment uses host dictionaries, linear in rows
  × roles; `Plane` words are where a SIMD or GPU backend would attach.
- **A bridge to the kernel.** Consolidation §5.2 proposes one; it is not built.

## Status against the consolidation roadmap

| Step | Here |
| --- | --- |
| R1 substrate contract | Planes, laws with accounting, ODV and the passage law. The decoder is replaced by the lens. Gap balancing is not ported |
| R2 house workspaces | Rungs: live reference with copy on write, layered and by-use selection, stale, split and introduced commits, sibling comparison. The house seam example is not ported |
| R3 scoped supports | Per-position supports in the planes, narrowed through relations and through laws read as relations, on structures and rungs (U0). U1–U3 are dropped |
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
| `SubstrateTests` | Planes of any length; growth and retirement; references at the width they need; constant planes; shapes; units entering as units, with overflow; broken counts reported, never repaired; the encoding holds no strings |
| `LawTests` | The passage bytes and LUT6 value; accounting refusal with the breaking pair; 130 lanes in one step; unary moves; bits that are not codes told apart from states a law is silent on; one position used twice; a coarse read as a relation; tables compared by content; joint prediction; read-only views; at most 64 values per read |
| `FlowTests` | Ports as lanes and planes, and rebinding; references to one sender from many readers; writing through a port only as a move, and only with positive weights; a moved lane aliasing a row; no role counting one bit twice; one-to-one uncalibrated moves with a remainder; two receivers interpreting one flow; two takers and one unit; branches and merges; self-switching flow; a calibrator structure; a wired rate; retired positions |
| `OdvTests` | Packing `88 C6 FA`; detection as the default and as one contract, checked against a reference reader; exact zero by request; a wrong orientation refused before any bit changes; one position read twice; reorientation; addition narrowed across 100 rows; threshold calibration |
| `RungTests` | Live reads and copy on write; stale and retired positions refusing a commit; changes made together, however the wiring is run, through a parent rung, and when reverted into a candidate; coarse-to-fine selection; selection by use up to a reach, counted from the nearest request; sibling rungs and rungs over rungs; constants by reference; a prediction checked against the source; introduced positions adopted; supports to a fixed point, open at positions added later; selections from structure |
| `TransportTests` | A machine restored from bits running step for step with the same unresolved parts; 25 bytes for the passage chain; rungs refused; trailing bits refused; any damaged bit either loads or is refused as malformed |
| `UncertaintyTests` | Open values in the planes, read by a law and saved; a supposition narrowed in a rung and dropped on contradiction; a goal settling the open choice that reaches it; ambiguity given a choice role; a miss read by a law as trust; flow steered to a goal that changes, with an unasked unit sent on |
| `LearningAndLensTests` | Learning the passage law; evidence read around each wave; a learned exclusion undone by a later witness; a learner counting what it could not read; refusing a learned table that makes units; unknown pairs staying open; a lens that the encoding never reads |
