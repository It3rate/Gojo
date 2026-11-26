# Gojo.EncodingC

Status: **exploratory**, 12 November 2026. It replaces `Gojo.EncodingB`, which
was deleted once this project covered what was worth keeping. The old project
is in git history (`git show c9d4abb2:Gojo.EncodingB/README.md`).

For a cloud-session starting point, see the
[19 November handoff](../Gojo/docs/GojoEncodingCCloudHandoff-2026-11-19.md).
It connects the current implementation to the broader glyph, ornament,
symmetry and learning work, separating retained decisions from open proposals.

```powershell
dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj
```

## The goal: a lean, bit-level encoding

This project encodes structures that compute, using as little as possible:
bits at positions, small tables of bits, and the order in which things are
wired. Concretely:

- **Structures** are built from bit encodings in the sense of ODV. Each bit's
  role comes from its position, with no header and no name.
- **Ports** carry what flows from one local structure to another.
- **Values** flow through the structures once they are running.
- **What structures do with them:**
  - run operations;
  - branch and merge flow;
  - switch and change their own structure;
  - work in workspaces: live views of a store that copy only what they change;
  - hold what is not settled yet in the same planes as what is, and use it:
    open values, choices nothing has made, and the gap between what was
    supposed and what is;
  - run themselves: observe, narrow, decide, act and forget are wirings the
    engine steps, with no host code in the loop;
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
- **No fixed sizes.** A store has as many positions as it needs, and a
  plane is as long as its store. A law runs across as many lanes as it has
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

The [glossary](GLOSSARY.md) lists every term used here, with what it means and
how it is held in bits.

| Noun | Types | What it is |
| --- | --- | --- |
| Plane | [`Plane`](Substrate/Plane.cs) | A bit per position. State, selections, lane sets and supports are all planes |
| Layout | [`Shape`](Substrate/Shape.cs), [`Field`](Substrate/Field.cs), [`PackedInts`](Substrate/PackedInts.cs) | How a value sits in bits (binary or unary), which planes hold it, and references packed at the width they need |
| Surface | [`Surface`](Substrate/Surface.cs) | What a law's role binds to: a store, a port, a workspace, the gap between a workspace and its source, or whether a store's positions are in use. Every read and write goes through it |
| Store | [`Store`](Substrate/Store.cs), [`Life`](Substrate/Life.cs) | A block of state: positions and planes, and constant planes. It holds bits and does nothing. A position is spare, in use or retired, and `Life` shows that as a surface, so a law grows and prunes |
| Local structure | [`Engine`](Flow/Engine.cs) | The stores and wirings that form one local intelligence, and the order it settles in. `StepTogether` takes a step in which every row reads the state before the step and two rows writing one bit, or different bits of one code, are contested rather than ranked; `Settle` steps until a step changes neither state nor evidence. See [Stores and local structures](#stores-and-local-structures) |
| Path | [`Paths`](Laws/Paths.cs) | Sites and a declared order between neighbours, as tables: a step and its ODV step word (with the path, against it, or a stay), a unit entering a site shared one way at a time, a site read as a side of a landmark, a moment of a trail recorded from occupancy by a clock, a unit advancing one site in the use's direction, a one-sided bound, the sites between two landmarks and the gap beyond them, a segment's natural start, a turn. Pre-numeric: order is reachability, with no distance |
| Sides | [`Sides`](Laws/Sides.cs) | Where something is against a landmark: which side of it, and whether at it at this resolution, as the V and D bits of an ODV word on the store's orientation. A continuous stretch is a few such words, not a chain of sites. Its laws come from ODV arithmetic: the sides agree (subtraction), a step moves the side (addition), and turning back; continuity promises that two samples in a row are never detectably on opposite sides of a mark. Events against a landmark are presence bits: at it, a step toward or away, crossed, passed between two moments, turned back, reached, abandoned, under way |
| Fork | [`Fork`](Flow/Fork.cs) | A local structure's own supposition: its state read by reference through copy-on-write workspaces, its material (planes its conserving laws move units on, found from the laws) held fixed by default while information stays live, its topology copied so it can be rewired, its laws shared. It never binds its parent's stores directly and cannot take through a port; its commit writes state back all or nothing, refused if the parent moved |
| View | [`Flipped`](Substrate/Flipped.cs) | Another surface with chosen planes shown inverted, live and without copying. `Odv.Reoriented` is the view over D and O: the store read by a reader facing the other way. A view belongs to its store's local structure and is saved as a view |
| Socket | [`Socket`](Substrate/Socket.cs) | The receiving end of a connection: the shape a local structure expects from another's port, and which port fills it now |
| Port | [`Port`](Substrate/Port.cs) | Ordered lanes onto an owner's positions and an ordered list of its planes |
| Law | [`RelationLaw`](Laws/RelationLaw.cs), [`TransitionLaw`](Laws/TransitionLaw.cs), [`Accounting`](Laws/Accounting.cs) | Tables of admitted tuples or transitions, with promises checked when the table is made |
| Wiring | [`Wiring`](Flow/Wiring.cs), `Binding` | A law bound to rows over stores and ports. A role is bound to a code or, open, to what a value may still be. The rows are a store of their own, so topology is state |
| Engine | [`Engine`](Flow/Engine.cs), [`Unary`](Flow/Unary.cs) | Steps wirings in waves: a transition changes state, a relation rules out. Units enter from outside |
| Stock laws | [`Flows`](Flow/Flows.cs), [`Settling`](Laws/Settling.cs) | Small tables for moving things, and for observing, forgetting and acting on what is known |
| Workspace | [`Workspace`](Workspaces/Workspace.cs), [`Gap`](Workspaces/Gap.cs), [`Selections`](Workspaces/Selections.cs) | A fragment of an existing structure, held apart from it and refined there: a live view with copy on write, selection by layers and by use, validated partial commits. Its gap is where it and its source disagree |
| Closure | [`Closure`](Workspaces/Closure.cs), [`Question`](Workspaces/Question.cs) | A selection built from a seed: whole rows, what they bind, grown by reach, sealed at ports and settled values unless opened, or the positions every use of a referent shares, and only the planes selected rows read. A question is an open value whose settling is the goal, its cone the closure from it |
| Tension | [`Tension`](Flow/Tension.cs), `Field.Room` | Material tension beside a unary value: pressure (units arrived, not absorbed) and debt (draws not paid), two counts never netted by default. Change enters as tension; absorbing, paying and cancelling are each the one-unit move, into the value or into a count's room. Each move can be gated by any number of bits (`Flows.When`, `Tension.Route`, `Tension.Wire`): an open choice's `Supports.Possible`, so contention is the choice left open, or a turn (`Flows.Turn`) that alternates while both sides have work. A choice about now is justified (`Settling.Justify`): one law holds each possibility from its reasons, so it reopens when they return. `Tension.Deliver` sends a unit on to an outlet against a debt (`Flows.Fulfill`), closing the order in the same move |
| Counting | [`Counts`](Laws/Counts.cs) | Arithmetic as small law tables, emulating numeric support (`Sum`, `Shortfall`, `Tally` and `Accumulate` are bindings of a balance): `Sum` (accumulation), `Shortfall` (one face of a gap), `Tally` (bits set, over supports: information tension), `AtLeast` (a threshold held as data), `Accumulate` and `Streak` (accumulation held or reset). Each writes one result only when it changes, so its meaning is a saved table. `Rank` combines tiers so a higher one decides whenever it changed (strict domination as a law) |
| Settling, further | [`Settling`](Laws/Settling.cs) | `Justify` holds a possibility from its reasons, so it reopens; `Latch` sets or clears a bit while a trigger holds, recruiting or retiring rows; `SameWhen` narrows only once words have arrived; `ValueGap` relates two open values and whether they disagree, the gap for values that are not counts |
| Operation | [`Operations`](Laws/Operations.cs), [`Polynomial`](Laws/Polynomial.cs) | A relation and a binding. The relation, often a polynomial written as integers, says what holds; the binding says which roles a step reads, which it writes and which are free residue, at what cost, and whether a blocked bit records a step that held. See [Operations and numbers](#operations-and-numbers) |
| Numbers | [`Numbers`, `Ladder`](Laws/Numbers.cs), [`Portions`](Laws/Portions.cs) | Place value over counts: a digit per rung, each rung its own partition, joined by column balances that narrow in every direction. A measurement leaves finer rungs open, and what does not fit is the last carry or a residue role. `Portions` adds live-denominator validity, guarded pooling, same-whole addition, projection and comparison |
| Recognition | [`Recognition`](Laws/Recognition.cs) | Finding the polynomial relations a table or a few witnesses keep, with what they leave unexplained, multi-residue balance systems (`Residues` for `min`, `max`, and `|a − b|`), and comparing or restoring a law from one only where its table is the same |
| Compilation | [`CompiledPlan`](Compilation/CompiledPlan.cs) | Optional, derived, epoch-guarded execution backend qualified strictly from table bits, accounting and bindings (relational and concrete mixed-partition SWAR adders, portable `Vector<ulong>` SIMD unary packet conversion, bounded multiply, SIMD small tables), falling back to `Engine` wherever ineligible, dynamic-topology, or stale; never saved |
| Supports | [`Supports`](Workspaces/Supports.cs) | A value not settled yet: one plane per value in the store, set where the value is still possible |
| Learning | [`LearnedTable`](Learning/LearnedTable.cs) | Small counts of what happened, held in a store, crystallized into a law |
| Measure | [`OdvWord`, `Odv`](Measure/Odv.cs) | The three-bit ODV word, its planes and its arithmetic as laws |
| Transport | [`Transport`](Transport/Transport.cs) | A machine as bits, restored without the code that built it |
| Lens | [`Lens`, `OdvNotation`](Lens/Lens.cs) | Names and notation for people, outside the encoding |

## Stores and local structures

Two things are kept apart.

A **store** is a block of state: positions and the planes over them. It holds
bits and does nothing. The code's `Store` is this. It was called `Structure`
until 12 November 2026.

A **local structure** is the group of stores and wirings that forms one local
intelligence. The canonical prompt (§4.7) also calls it a local space.

- **Inside** a local structure, laws connect to any of its stores freely. Its
  parts share state, patterns and calculation, and it settles as one.
- **Between** local structures the only connection is a port, and each
  settles independently.

In this README, "structure" on its own means connected parts that compute, in
that sense.

What is built, and what is not:

- The rules for crossing a port are built. Through a port a law reads, or
  takes as a move, and never rules anything out on the other side (see
  [Copying and moving](#copying-and-moving)).
- The boundary is built (`LocalStructureTests`). An `Engine` is one local
  structure. A store belongs to the engine that holds it (`Hold`) or first
  binds it directly, and to no other. An engine refuses a wiring that would
  bind another's store directly: its positions, its `Life`, or a workspace
  over it. The refusal is all or nothing. Membership is by connection: no
  name, flag or index marks it.
- A **socket** is the receiving end of a connection. It holds the shape a
  structure expects from a port: lanes, planes, and which planes are
  constant. A wiring binds it where it would bind a foreign port. With
  nothing plugged in, or on a lane the port lacks, a row reaches nowhere and
  is reported skipped. That is how an absent peer shows.
- Each engine steps on its own, in an order chosen by whoever runs them. A
  peer that is not stepped holds still.
- **One taker per position.** Any number of connections may read a port; at
  most one takes through it: one wiring bound to it, or one socket plugged
  into it. Two taken ports of one owner never show the same position, and
  rebinding or extending a port cannot make them. So no unit is contested
  between structures, and stepping them in any order moves the same units.
  An owner serving two takers splits what it has with its own law and
  exposes a port for each. Inside the taking structure, rows still compete
  for a unit, and row order decides.
- **Fusion.** `Engine.Fuse` makes two partial structures one: the other's
  stores join, its wirings run after, and their ports and sockets keep
  working as inside connections. Saved, a socket filled from inside is
  written as the port it reads.
- A saved machine is one local structure: the restored engine holds
  everything saved with it. A foreign port is saved as a socket of its shape
  and comes back empty. Several structures saved together (`SaveLocals`) also
  keep which port fills each socket, as a structure, a store in its order and
  a port. No foreign position is written.
- A host can still reach past the boundary by calling `Wiring.Apply`
  itself, or by publishing or writing another structure's store. The engine
  guards what it runs, not what the host does. A taker's claim on a port
  lasts as long as the wiring; only a socket can let go.

Local structures communicating over thin connections is essential to Gojo. The
primitives are being settled first.

## Where the bits are

**Planes and stores.** Bit i of a plane belongs to position i. A store
holds:
- one plane per stored plane, the length of its store;
- a *constant plane* for anything the whole store shares, held as a single
  bit and never per position (ODV orientation is one). It is positional, like
  the core's buckets: belonging to the store gives a position the bit, so
  everything in it turns together, and turning one word means moving it into
  a store that faces the other way;
- which positions are in use. A position is spare (room, not yet in use), in
  use, or retired, and only moves forward through those. `store.Life`
  shows "in use" as a surface with one plane, so a law recruits a spare
  position by setting its bit and retires one by clearing it;

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

**References.** A wiring's rows are the positions of a store of its own.
Each role's reference is a value in that store's planes, ⌈log₂ n⌉ bits for
n targets, and a plane is added when a larger reference arrives. So a law
bound to those planes reads or rewires a row, a saved machine keeps topology
as it keeps any state, and retiring a row is retiring a position. Once a
wiring records, six more planes hold what each row did in the last step. Port
lanes are packed references at the width they need.

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
  store reorients with one constant flip and `D ^= live`.
- Each bit is 1 for the dominant member (canonical prompt §9.7): V 1 is
  detectable, D 1 is positive, O 1 is positive-right, the forward face.
  `R+?` is `111` and `L-0` is `000`.
- Three moves change orientation: `Reverse` flips D (the site mirrors),
  `FaceFlip` flips O (the site mirrors), `Reorient` flips both (the same site,
  read the other way). Each undoes itself and is the other two composed.
  `Odv.Reoriented(store)` is a live reoriented view with nothing copied.
- All eight words, in code order, pack as `88 C6 FA`.
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

**Workspace.** A workspace keeps, per plane it has written, three planes over
its positions: which bits it holds, its value for them, and the source bits
they replaced. Positions it has not written cost nothing. It also keeps:
- its selection, as a short list of layers (an area plane or "all", and a
  keep-or-ignore bit);
- a unary count per position brought in by use: how many steps are left;
- packed references for introduced positions (0 while a candidate, s + 1 once
  adopted as source position s);
- a packed parent per position for changes made together.

**Supports.** One plane per value, in the store, set where the value is
*still possible* (canonical prompt §9.7: 1 is presence). Every bit set is
open, one left is settled, none, all zeros, is a contradiction. Support
planes fill: every position, including one added later, starts at 1, open
with no extra step, and each bit cleared is one thing learned. What two
constraints allow together is the AND of their planes. "Every value" means
every value of a declared, finite set; a support says nothing about values
outside it. A one-bit value costs two planes: unknown, 0, 1 and
contradiction.

**Transport.** A machine saves as one run of bits: stores (constant
planes, retired positions if any, stored planes, ports), laws (role shapes,
table, accounting), and wirings (law, bindings, and which store holds the
rows), then the engine's order where a wiring runs more than once. A law with
one next state per state is saved as that list of next states; a law read as
a relation is saved once, as a reading. Whole numbers are Elias gamma codes of
n + 1, so 0 takes one bit. There are no names, headers or fixed widths; a
store is identified by its place in the order. The passage chain of five
positions and two rows is 22 bytes (see [How big it is](#how-big-it-is)).

## Copying and moving

A port says nothing about whether it may be read or taken. That follows from
the law a receiver wires to it:

- A role the law never changes is **read**. Through a port, that is a
  reference: the sender is untouched, and any number of receivers can hold one.
- A role the law changes is **written**:
  - Writing a store directly is the business of the local structure it
    belongs to.
  - Writing *through a port* reaches into another local structure. It is
    allowed only
    when the law's accounting puts the role in a preserved sum of positive
    amounts, so what leaves there arrives somewhere the law also writes. That
    is a move, and nothing can be made or destroyed in another local
    structure. A
    law that would drain a port without accounting for it is refused when it
    is wired. A sum with a zero or negative weight is still a promise, but
    not conservation, and does not count.
- **One bit is one position.** A binding or a port names each plane once, so
  no role can count one bit twice. References are live, so a port's owner can
  move a lane after a row was wired. A row whose roles now meet at a bit one
  of them writes would let the law count one thing twice. It does not run, and
  it is reported in `WiringStep.Aliased` until the lane moves again.
- **A supposition is never mixed with what is.** A law that writes a workspace
  writes only that workspace; the workspace's commit decides what reaches
  anything else.

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
- **Topology.** These operations are structural edits:
  - adding positions or planes, and retiring positions;
  - publishing a port;
  - rebinding or extending a lane;
  - adding, rewiring or retiring a row.

  A port's owner can move what a lane shows, and every connection follows,
  because connections hold lane numbers.
- **By a law.** A row's references are values in the planes of the wiring's
  rows, so a law bound to them rewires the row. In
  `ALawRewiresARowAndFlowFollows`, a law reads two trust counts and points a
  row's source at the more trusted supplier. A reference written to nowhere is
  not an error: the row does not run and is reported as skipped.
- **Growing and pruning by a law.** Growth is recruiting room that is already
  there. A row added with `AddSpare` is wired and dormant; a law that sets its
  bit in `Rows.Life` brings it into use
  (`ALawBringsADormantRowToLifeWhenFlowIsBlocked`: a second route opens once
  the first target is full). A law that clears the bit retires a row
  (`APredictorWhoseTrustRunsOutIsRetiredByTheStructure`). Allocating new room
  is still a host call.
- **Attention is a reference.** What a row looks at is a value in planes, so a
  law moves it: a watcher shifts its own focus along a chain until it finds
  the unit (`AWatcherMovesItsOwnAttentionUntilItFindsTheUnit`).
- **Through a workspace.** Values, and introduced positions, reach the source
  when a workspace commits.

## Workspaces

A store holds what is. A workspace holds what is noticed, supposed or
attempted. It holds a fragment of an existing structure: it reads that
fragment from its source, and a change made there is a value of the
workspace's own. It is separate from the structure it refers to, and its
work is refining that structure. What it did can later be dropped or folded
back in. The type was called `Rung` until 12 November 2026; a rung is a level
of a ladder.

For now, what is selected is whatever is in a workspace. Selection is kept
that primitive.

- **Reference first.** `new Workspace(source)` copies nothing. Every position
  it has not written reads the source as the source is now, so a workspace sees
  the world keep moving. A live read can be slightly out of date; the system
  keeps running and later reads correct it.
- **Copy on write.** Writing a position copies the whole position in, every
  plane but the constants, so a count spread over several planes is never half
  the workspace's and half the source's. A workspace costs what it changes, not
  what it sees, so many can be open at once.
- **Laws run unchanged.** A workspace is a surface, so a wiring binds to it
  like to a store. `workspace.Apply(wiring)` runs it; a law run in a workspace
  writes only the workspace.
- **Selection, coarse to fine.** `SelectAll()`, `Select(area)` and
  `Ignore(area)` add layers; later layers override earlier ones. Nobody lists
  positions. "All" follows the source as it grows.
- **Selection by use.** A row that touches a selected position and unselected
  ones brings those in when it runs, one step further from the request, up to
  the workspace's reach (three by default). A row that would go further, or
  into an ignored area, does not run and is returned in `WorkspaceStep.Beyond`.
- **Constants** are read by reference and cannot be changed in a workspace.
  Every position of the store shares them.
- **Leaving a workspace:**
  - *Drop it.* Nothing in the source changed.
  - *Compare it.* `Changes()` is where it reads differently from the source.
    Checked after the source has moved on, that is how far a prediction missed
    (`APredictionIsAWorkspaceComparedWithWhatTheSourceThenDid`). `Differs`
    compares two sibling workspaces over one source.
  - *Revert part of it.* The positions read the source live again. Changes
    made together are reverted together: a unit moved into an introduced
    position goes back to its donor, and the candidate stays, empty.
  - *Commit part or all of it.* Everything is checked before anything is
    written. A position whose source moved or was retired since it was copied
    is stale, and a stale commit writes nothing. Changes one row made together,
    such as both ends of a move, commit together or not at all (`Split`). The
    wiring records them, however it is run, and a commit into a parent workspace
    keeps them together there.
  - *Adopt what it introduced.* `Introduce()` adds a candidate position with no
    source. It is always selected, never read as evidence about the source,
    and becomes a source position on commit.
- **Workspaces over workspaces.** One workspace over another sees its parent's
  changes live, and commits into the parent, not the store underneath.

## Working with what is not settled

Everything that flows in is uncertain until something accounts for it, and the
places where a structure can change are the places that are not settled. So
what is unsettled is kept in the same planes as what is settled, where laws
read it, relations narrow it and workspaces suppose about it.

- **Open values.** A field's bits hold one value. Its supports hold what the
  value may still be: one plane per value, set where still possible
  (`Supports.On`). A role bound open to those planes (`supports.Variable`, or
  `Binding.Open(surface, planes)`) is narrowed whenever a relation wired to it
  runs: a value stays possible only if some admitted tuple supports it. A
  plain role of a relation is a sample and is only read, so observing is the
  relation `Settling.Same` between a field's bits and its supports.
- **Forgetting and acting are laws too.** `Settling.Forget` makes every value
  possible again; wired to run every step it is the passing of time.
  `Settling.Enact` writes a settled value into a field's bits.
- **The gap.** A workspace differs from its source only where it was written.
  `workspace.Gap` shows the difference bit by bit as a surface, read by
  reference and never stored. If the workspace ran a law ahead, the gap after
  the source moves is how the prediction missed. If a wanted state was written
  into the workspace, the gap is what is left to do. A law bound to it turns a
  miss into less trust (`AMissIsASignalALawReadsAndTrustFollowsIt`).
- **Choice.** A law that admits several next states does not run alone.
  `WithChoice` gives the choice a role, which can be bound to a bit a
  structure sets, or left open and narrowed from what happened
  (`AmbiguityBecomesAChoiceSomethingCanMake`).
- **A law read both ways.** `AsRelation` is the same table read as a relation
  between now and next. It is one reading of the law, saved with it once.
- **Knowledge across a boundary.** A port can expose supports. A reader
  narrows its own values from them, and nothing behind a port is ever
  narrowed (`WhatAnotherStructureKnowsIsReadThroughAPortAndNeverNarrowedThere`).
  In the same way, what is supposed never narrows what is known: when a
  relation reads a workspace, only roles on workspaces are narrowed.
- **Values that vary together.** Per-value planes cannot say two values are
  linked. The link is a third value, the pair, with a relation from it to
  each (`ValuesThatVaryTogetherAreOneValueWithTwoReads`).

### A machine that reaches a goal by itself

`AGoalTwoStepsAwayOpensTheValveNow` is the whole loop with no host code in it.
A plant has a source, a gate and a target that takes one unit per step; two
units must reach the target within two steps. A plan, one store, holds what is
possible at three moments, all open. Each engine step runs these wirings in
declared order:

| Wiring | Law | What it does |
| --- | --- | --- |
| forget | `Settling.Forget` | Opens the plan again |
| observe | `Settling.Same` | Narrows the first moment to what the plant reads |
| aim | `Settling.Same` | Narrows the last moment to the wanted level |
| ahead, twice | the move law, `AsRelation` | Narrows from moment to moment; the second pass carries the goal back to now |
| act | `Settling.Enact` | Writes the gate's one remaining value into the plant |
| flow | the move law | Moves the unit |

The goal settles the gate at the first moment, because both steps are needed.
Asked for what cannot be reached, the plan holds a contradiction, the gate
stays as it was, and the next step starts clean. The machine saves as 116
bytes and runs the same when restored.

### A goal is a selection, a condition and a target

A goal is ordinary structure in three parts (`GoalTests`):

- a **selection**: the plan positions its rows reference, and nothing else;
- a **condition**: the law on those rows, such as `Settling.Same`;
- a **target**: the values it holds fixed, read as samples that narrowing
  never changes.

Its horizon is the plan's last moment; a stay is a step, so "arrive by then"
is "be there then". What a goal does not select it does not constrain, so
"end at the store" selects only the last moment and leaves every route open,
by road or by helicopter, while "go by the road" selects every moment and
settles the trip. A waypoint selects one moment in the middle. Whether a goal
is met is a read of its rows' violations against a plan or a trail. Why a
goal was chosen is not modelled yet: goals are supplied.

### Other ways to use what is open

- **A supposition in a workspace.** Seen through a workspace
  (`supports.In(workspace)`), narrowing is supposed rather than known. A
  supposition that reaches a contradiction is dropped and the store never held
  it (`ASuppositionIsNarrowedInAWorkspaceAndDroppedWhenItContradicts`). A
  one-off decision works the same way: forget the places that may change in one
  workspace, write the wanted state into a next-moment workspace, narrow
  between them, then enact and commit what settled
  (`AGoalSettlesTheOpenChoiceThatReachesIt`).
- **Steering by the gap.** A law that reads a level and the wanted level, held
  in a workspace, opens whichever valve closes the gap. The flow brings the
  structure to the goal, follows the goal when it changes, and sends on a
  unit that arrives unasked (`FlowIsSteeredTowardAGoalByTheGapItLeaves`).
  Unlike a plan, this makes partial progress toward a goal it cannot reach.
- **Prediction that never stops.** Predictors write their guess of a bit into
  their own planes before the world moves; a judge compares each guess with
  what happened and moves a count of trust. It is one store and four
  wirings, 48 bytes saved, and it keeps the trust, not the history
  (`AStructurePredictsJudgesItselfAndTrustFollows`).
- **Counting what happened.** A wiring that records leaves each row's result
  in planes. A count and a law that reads the "changed" bit are an
  accumulator of what flowed through the row
  (`WhatARowDidIsLeftInPlanesALawCanCount`).
- **A goal that gives way.** A plan that contradicts itself leaves that in a
  plane of its rows. A law that reads the bit adds one to how far the aim is
  lowered, so a machine asked for what it cannot reach aims for what it can,
  and reaches it (`AGoalThatCannotBeReachedGivesWayUntilItCan`).
- **A law that does not say which.** Run on bits, a law with several next
  states stops. Read as a relation into a next moment, it leaves that moment
  open, and a later observation settles it
  (`AnAmbiguousLawLeavesTheNextMomentOpenInsteadOfStopping`).
- **Overflow up a rung.** A carry law from a fine count to a coarser one turns
  three fine units into one coarse, so arrivals are never turned away. The
  weights of its preserved sum are the place values: two counts and a carry
  are a ladder (`WhatDoesNotFitIsCarriedUpARung`).

### What narrowing cannot settle, and supposing

Narrowing settles what is forced and nothing more. Each row rules out what its
own table cannot support, which is sound and incomplete: when what rules a
choice out lies in another row, both values stay possible and nothing is
enacted (`NarrowingSettlesOnlyWhatOneRowCanSee`).

The way past that is to try an idea that may be wrong. A probe is a second
plan wired like the first, with one thing supposed: here, that the gate stays
shut. If the probe contradicts itself, a law that reads that bit rules the
supposed value out in the real plan, which then has one value left and acts
(`ASuppositionThatContradictsRulesTheChoiceOut`). The supposition is ordinary
structure, saved with the machine. It costs a second plan, 226 bytes against
116, so how much can be tried at once is bounded by room. Which choices
deserve a probe is not decided by anything yet.

## Operations and numbers

An operation is a relation and a binding ([`Operations`](Laws/Operations.cs)).
The relation says what holds among some amounts and moves nothing. The
binding says how one step uses it: each relation role reads a law role before
the step, writes one after it, or is left free. One relation therefore gives
several operations:

- **Heap.** The balance a + b − total = 0 is three operations:
  - a sum written into a new count;
  - the same sum written over the count it read (accumulate);
  - read backward, subtraction.
- **The gap.** have + short = want + over is shortfall with the excess left
  free, and excess with the roles swapped.
- **The product.** a · b = c pairs two counts forward. Narrowed, it divides,
  and with both inputs bound to one sample it squares.
- **The bit rung.** a + b = 2 · carry + sum is the half adder: its carry is
  AND and its sum XOR. Polarity is also a binding: a role is bound to a bit
  or to its flipped view. Those two laws, a wire and the two constants then
  make all sixteen two-input functions.

What a binding must say, and what happens when nothing fits:

- **A free role can account for residue.** The completion with the least
  Σ cost · value is kept, including ties. With no cost every completion
  qualifies. Distinct written outcomes stay open, while completions that
  agree on the written roles yield the same after-state. Connect a residue
  role explicitly to retain it in the machine.
- **Where nothing fits, the step holds,** as a move holds when its target is
  full. The written roles keep their values, and the relation is left unmet
  where anything can read it. An optional blocked bit says which kind of
  quiet a step was: 1 where it held, 0 where it wrote a completion.
- **Settling is not time.** A result written over its own input changes once
  a step; given its own count, it settles at once. So feedback has to state
  its time boundary: a step, a captured before-state or a new count.

Where an operation sits among the others is read from its table and its
accounting, never from a name:
- **Used up or kept:** whether a written role's gain is paid for by another
  role (a move) or by nothing (a read).
- **Along or across:** whether units trade one for one, or meet at a rate or
  a corner.

`Bind(Polynomial, …)` can derive conservation when the same positive
weighted accounts appear on both sides of an authored balance. `Wiring`
checks the resulting material permission at the port. A read into a fresh
count can keep its inputs without a preserved sum.

Polarity, where the result goes, read direction and folding are bindings,
open to every law. `Sum`, `Shortfall`, `Tally` and `Accumulate` in `Counts`
are now bindings of a balance, with exactly the bits of the tables they
replace.

### Equations as numbers

A relation written as numbers is a [`Polynomial`](Laws/Polynomial.cs):
Σ weight · Π value^exponent = 0. Each term has an integer weight and one
exponent per role.
- A balance is the polynomial whose terms are single roles.
- A product is the term ab against the term c.
- A column between rungs is a balance whose weight is the radix.

It is kept in canonical form: equal monomials merged, weights divided by
their common factor, the first one positive. So two equations compare as
numbers, and an equation can be perturbed and searched without a name. Only
the table it builds runs. `Bits` is its description length, and
`Lens.Describe` writes it for people, as in "a + b + c − 10d − e = 0".

### Numbers as rungs

[`Numbers`](Laws/Numbers.cs) and `Ladder` are a place-value layer over counts.
They add range and erase nothing: a digit is a count, and a ladder can be
read over the planes of material counts.

- **A rung is a partition.** A digit is a count with as many values as its
  rung's radix, held unary or binary-coded. The shape's size says exactly
  which values exist. Each rung has its own radix: fifths under whole stars
  (radices 5 and 6), or minutes under hours (60 and 24).
- **Rungs are joined by columns.** a + b + carry in = radix · carry out + sum
  is a balance, so a ladder of columns narrows forward, backward, and with
  digits left open.
  - Three decimal rungs add 487 + 356 and subtract 843 − 487 with the same
    wiring. One sum table over those values would need about two billion
    bits.
  - `Scale` and `Multiply` do the same for a · k and a · b, as a lattice of
    digit products.
- **Units and scopes.** `AtUnitRung` selects an existing rung as a local
  unit and `TicksPerUnit` reads its scale through the columns. `TryAdd`
  leaves unequal selections unbound. A change of calibration uses an
  explicit relation. Published member ports can support a tally of disjoint
  scopes. `DisjointPositions` checks current live site identity, even when
  two ports look alike. Recheck that qualification when a member port moves.
- **The chosen whole determines the sum.** Three of five and two of five
  pool to five of ten across disjoint scopes. Compatible portions on one
  whole give five fifths. Keep the original whole when displaying a reduced
  share.
  - `SameShare` says two shares are equal, by a balance whose weights are
    the denominators.
  - `Rescale` reads a count out of one partition as a count out of another.
    The part that does not divide is a residue role.
  - Reducing 5/10 to 1/2 is a read the caller asks for, never automatic.
- **Resolution is structure.**
  - A count of fifths has its finer rungs closed. A measurement read only to
    the fifth (`Measure`) leaves them open.
  - Adding two measurements carries that openness into the sum, so its
    fifths may carry.
  - `Interval` is a sound bound, read rung by rung. A supposition in a
    workspace can tighten it.
- **Nothing is dropped.**
  - A sum that does not fit is the last carry.
  - A difference below zero is a borrow, and with borrow forbidden, a
    contradiction.
  - The finest carry in, and the fact that a product fits, are constant
    planes: structure, not observation.
- **Re-observing.** Narrowing is monotone within one observation.
  - Reopening one ladder leaves what was derived from it narrowed, so a new
    reading contradicts it visibly.
  - `Ladder.Reopen(network, position)` reopens shared positions on writable
    supports in an aligned network. `ReopenRows` follows the references of
    selected rows in a mapped network. Constants and supplier ports stay.
- **Material.** Each of these conserves the place-value sum:
  - `Admit` takes one unit of pressure into a two-rung number atomically,
    carrying when the fine digit is full.
  - `Group` and `Refine` move whole packets.

The design and its limits are in the
[operations synthesis](../Gojo/docs/GojoOperationsSynthesis-2026-11.md).

## Learning

`LearnedTable` counts witnessed (before, after) pairs and reads each pair one of
three ways:
- **admitted:** seen often enough;
- **unsupported:** its before-state was seen often enough without it;
- **unknown:** otherwise. Unknown pairs keep predictions open rather than
  guessed.

Everything it knows is a store of small unary counts (`Counts`): one per
pair, one per before-state, and one for rows it was shown and could not read.
So what was learned is saved like any store, a law can read how sure the
table is, and nothing is kept beside the encoding. A count stops at the larger
threshold, because "often enough" is all it has to say, and no history of
when things happened is kept. For the passage law that is 73 counts, 40 bytes
saved.

"Unsupported" is a rule of thumb, not proof. A crystallized law marks the pairs
it left out only for not being seen (`Crystallized.Unsupported`), the counts
they came from stay in the store, and the table keeps counting, so a later
witness admits them again.

Once nothing is unknown, the table crystallizes into a law. That law is checked
against the accounting it is asked to keep, like any authored law. A table that
would make or lose units is refused, and the breaking pairs are returned. In
`AStructureLearnsThePassageLawFromWhatHappened`, eight rows in eight states are
run four times, and the learned table equals `Flows.Passage` bit for bit.

Counting is the first step, not the aim. A law that fits predicts its own
history, so only what it gets wrong is worth storing. The direction is
prediction by structure: a simple rule, such as one that stretches a value by
two, compared with what happens next, with trust kept as a small count.

### From tables to equations

When a table keeps a short equation, [`Recognition`](Laws/Recognition.cs)
finds it. It is `Bind` read backward, and everything it returns is numbers:
weights, exponents and the indexes of exceptions.

- **From a full table,** `Relations` finds every polynomial relation of the
  lowest degree the tuples keep, as a system. The method is the vanishing
  ideal of a finite point set, computed in unbounded integers.
  `Found.Unexplained` lists the tuples the system admits that nothing
  witnessed. Empty means exact; otherwise the system is weaker than its data,
  and the list says where.
- **From a few witnesses,** `Fits` writes one role as a short integer
  combination of monomials of the others, simplest first, allowing a stated
  number of exceptions.
  - A wall, where a count has no room, is an exception, not another law.
  - `Survivors` keeps the candidates the witnesses allow.
  - `Probe` chooses the input whose answer splits the survivors most.
- **Witnesses** are read through a binding, from a functional law or
  directly from a `LearnedTable`'s counts. No second dense table is built, so
  an equation can be found before the table is complete.
- **A hidden residue role** is found by `Residue`: shortfall's excess, for
  example, or accumulate's overflow.
- **Nothing is accepted automatically.**
  - `Compare` sets the table beside the equation run through the same
    binding. It reports whether the two are the same, how many before-states
    differ, both sizes in bits, and whether the equation keeps the law's
    accounting.
  - `Restore` rebuilds a law from its equation only where the table is the
    same, then attaches the original's promises.
  - Keeping the promises is not enough: an equation that keeps the
    accounting but behaves differently is refused.

Transport still saves tables. An equation is not part of the saved form.

## Non-signal parts

Signal and noise are encoded together, as a layout has content and whitespace.
The table says where each unresolved kind lives today. *Bits* means planes of a
store, which laws read and transport saves. *Workspace* means a workspace's own
planes, which are a working view and not saved. *Live* means a difference read
by reference while both sides exist. *Returned* means a record handed to the
caller after one call; nothing in the machine keeps it.

| Kind | Where it lives | Kept |
| --- | --- | --- |
| Not selected | Outside a workspace's selection: no claim either way. The nearest thing to nothing | Workspace |
| Ignored | A workspace's ignore layer; use never brings it in | Workspace |
| Open / competing | Supports with several values still possible | Bits |
| Contradiction | Supports with no value possible: all zeros | Bits |
| A choice nothing has made | The choice role of `WithChoice`, bound to a bit or left open | Bits |
| Not a code | The bits stay as they are; reported as unread | Bits |
| Broken count | `Unary.Broken`, `UnaryStep.Broken`; never repaired | Bits |
| Undetectable magnitude | ODV V = 0: a sample below this read's resolution, still an operand | Bits |
| Spare | Room that is allocated and not in use; `Store.Spare`, and `Life` for laws | Bits |
| Retired | No longer in use, never reused; reported as skipped | Bits |
| A reference to nowhere | A row whose reference a law wrote out of range; reported as skipped | Bits |
| Aliased | Roles that meet at a written bit, after a lane or a reference moved. Found again each step | Bits |
| Remainder / blocked | What did not move stays where it was; the lane is unchanged | Bits |
| Lossy read | A relation law such as presence over a count. It bounds what it dropped but cannot recover it (`ACoarseReadIsARelationAndCannotRecoverWhatItDropped`) | Bits |
| What a row did last step | Changed, unread, unadmitted, ambiguous, skipped, aliased, contested: `WiringStep`, and seven planes of the rows once the wiring records | Bits when recording, else returned |
| How often a pair was seen, and rows a learner could not read | `LearnedTable.Counts` | Bits |
| Left out for not being seen | `Crystallized.Unsupported`, read again from the counts at any time | Bits |
| Supposed | What a workspace holds: bits written and supports narrowed there | Workspace |
| Stale | The workspace's basis planes; `WorkspaceCommit.Stale`, and nothing is written | Workspace |
| Changed together | The workspace's groups; `WorkspaceCommit.Split` | Workspace |
| Introduced | `Workspace.Introduced`; never source evidence | Workspace |
| Prediction missed / goal not reached | `Workspace.Gap`: the bits where a workspace and its source disagree | Live |
| Overflow / underflow | `UnaryStep.Blocked`, unless a carry to a coarser count is wired | Returned |
| Beyond reach | `WorkspaceStep.Beyond`: rows that would reach too far from the request | Returned |

In a step, "unadmitted" is a state the law admits nothing for; for a relation
that is a contradiction. "Ambiguous" is several admitted possibilities and
nothing to choose: for a transition, several next states; for a relation, a
role still open.

## What runs itself, and what the host still does

The aim is a structure that computes by being what it is. An accumulator is a
count with a law wired to it; it collects because the engine steps, not
because any code knows about accumulating. A running machine does two things,
both table lookups across all rows at once:

- a transition row takes the next state its table gives;
- a relation row rules out what no admitted tuple supports.

Everything else in a step is those two rules applied in declared order. There
is no arithmetic unit and no real number: `TheEncodingUsesNoRealNumbers`
checks every field, parameter and result by reflection. Counting is unary, so
adding is setting a bit and comparing is a table.

| Part | Lines | What it is |
| --- | --- | --- |
| The machine: planes, stores, ports, laws, wirings, engine | about 2,150 | Storage, the two step rules (about 230 lines), and checks made when something is built |
| Stock tables: `Flows`, `Settling`, `Odv` | about 370 | Builders that produce tables; nothing runs from them but the tables |
| Host tools: `Workspace`, `Gap`, `Selections`, `Supports`, `Unary`, `LearnedTable` | about 1,100 | Code that reads or edits structure from outside |
| Save, load and names: `Transport`, `Lens` | about 630 | Outside the machine by nature |

Line counts include comments.

What a machine does for itself, as wirings:

- move, branch, merge and switch flow, and carry overflow up a rung;
- observe, narrow, forget and act on what it knows;
- predict, judge its predictions and keep trust;
- plan back from a goal, lower a goal it cannot reach, and settle a choice by
  supposing and being wrong;
- count what flowed through a row;
- reroute a row, move its own attention, bring a dormant row into use and
  retire one.

What still needs the host:

- **Making room.** A law recruits and retires what is there. Allocating
  positions, planes and rows, and wiring a law to something new, are host
  calls.
- **Workspaces as a tool.** A workspace's copy on write, selection, reach and
  commit are host code, the largest single piece, and a workspace is not saved.
  Inside a machine the same job is done by ordinary structure: a plan or a
  probe that observes what it attends to, narrows on its own planes and acts
  through a law. That form has no copy on write and no automatic selection.
- **Deciding what to try.** A probe is wired by hand for one choice.
- **Learning a table.** The counts are planes, and the admitted table is one
  of them, but which pair a row showed is worked out by host code, and so is
  making a law from the counts. A law's table is not itself in the planes,
  so a machine cannot yet change its own rules.
- **The edge of the machine.** Units from outside enter through `Unary.Add`.
  Plugging a socket, and the order in which several local structures step,
  are host acts.
- **The schedule.** Waves are worked out with host dictionaries. On a physical
  substrate that is locality, not a computation.
- **Checking promises.** A law's accounting is verified when the law is made.

## How big it is

Sizes of saved machines, from `Transport.Measure` and pinned in tests:

| Machine | Bytes | Stores / laws / bindings, in bits |
| --- | --- | --- |
| A passage chain: 5 positions, 2 rows, 1 law | 22 | 57 / 80 / 37 |
| Two predictors, a judge and their trust | 48 | |
| The machine that plans two steps ahead: 3 stores of state, 5 laws, 8 wirings | 116 | 308 / 291 / 327 |
| The same with a goal that gives way and a probe that supposes | 226 | |
| What a learner of the passage law knows: 73 small counts | 40 | |

What the bits go to:

- **State** costs one bit per plane per position. A wiring's rows are state
  too: ⌈log₂ n⌉ bits per reference.
- **A law** with one next state per state is saved as that list:
  N·⌈log₂ N⌉ bits for N states, 24 bits for the passage law, which is what a
  table-driven device holds. Other laws take one bit per tuple, N² for a
  transition. In memory this adapter keeps every table as one bit per tuple,
  so it can check it exhaustively.
- **An open value** costs one plane per value instead of ⌈log₂⌉ bits: two bits
  for a one-bit value, five for a count of 0 to 4. That is the price of
  holding "not settled" in the same medium as "settled".

What makes it grow, and what holds it down:

- **Law tables grow fastest.** N is the product of the role sizes. A law over
  two counts of 0 to 4 and two bits has 100 states and 700 bits, and one more
  such count multiplies that by more than five. The pressure is toward many
  small laws wired together, and coarse roles, not one wide law.
- **Options multiply.** A choice role multiplies a law's states by the number
  of options. A plan costs its positions once per moment it looks ahead.
- **History is not kept.** Nothing here stores what happened step by step. A
  law is the compact form of a history: 24 bits predict every step of every
  row it is wired to. What a law gets wrong is what is worth keeping, and it
  is kept small: a trust count of a few bits, a learner's counts that stop at
  "often enough", evidence planes that hold only the last step.
- **Workspaces cost what they change**, three planes per plane written, and are
  not saved.

Not done: sparse planes (a mostly empty plane still costs a bit per position),
storing only the difference between a learned law and a simpler one that
nearly fits, and saving a regular table by the rule that makes it or as
smaller laws wired together (see the glossary,
[Is a law always a table?](GLOSSARY.md#is-a-law-always-a-table)).

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
| Tiles and the 64-site cap | Replaced by stores and planes of any size |
| `PortBasis`, `Correspondence`, `PortAccounting` | Ports described their meaning, and copying versus moving was a flag |
| `RungMap` | A coarse read is a relation law and needs no type of its own |
| `TensionField` | Units and laws over unary fields cover it |
| `RouteTable`, `GapBalance`, `Energy`, the phased `Runtime` with checkpoints and replay | Host mechanisms that should become laws |
| `JointSupport`, `UncertaintyReads` | Host-side boolean arrays and recursion |
| Closed or open selection machines, `PredictionWorkspace` | Not needed in this core |
| Confluence classes, provenance strings | Not needed in this core |

## Not built

- **Growing and joining connections by law.** The boundary, sockets, one
  taker per position, fusion and saving with connections are built. Ports a
  structure grows, sockets it grows, plugging by a law rather than the host,
  splitting one structure into two, and identifying a position of one fused
  part with a position of another are not. A parallel backend is also not
  built, though one taker per position means the order between structures
  no longer matters. See [Stores and local structures](#stores-and-local-structures).
- **Selection at several resolutions.** Selection is one rule now: a wiring
  runs live rows its attention plane holds 1 for, within the selection of
  every workspace it binds, bringing in by use what running touches, alone
  or in an engine. It should also work at any resolution: a whole tree or
  machine in one coarse selection, or a pathway or landmark within it.
  Selection layers are host lists today, not planes, and are not saved.
  Closure from a seed (`Closure`) builds a selection from structure, sealed at
  ports and settled values unless opened, and follows a referent out to the
  structures that use it, grows by reach in each, and keeps what enough
  uses reached (`Closure.Common`); choosing dimensions is not built
  ([closure rules](../Gojo/docs/GojoStructureClosure-2026-11.md)).
- **The step taken together, further.** Which kind of step a machine takes
  is chosen by whoever steps it and is not saved with the machine. Between
  local structures the order they step in still decides what each reads of
  the others, though one taker per position means no unit is contested
  between them. A workspace's own copied bits are planned against as they
  stand, like everything else.
- **Forks, further.** A fork copies topology instead of reading rows by
  reference; its rewiring is not committed; it cannot suppose about whether
  positions are in use; and holding material copies its planes when the
  fork is made, so a large material store costs a full copy of those planes.
- **Laws in the planes.** A law's table is fixed when it is made. A table held
  in a store, so that what was learned runs at once and a machine can
  change its own rules, is the largest open step. It needs an answer for how
  promises are kept by a table that changes.
- **Allocation by laws.** A law recruits and retires what is there; it cannot
  make new positions, planes or rows, or wire a law to something new.
- **Deciding what to suppose.** A probe is wired by hand. Nothing yet picks
  which open choices to try, shares one probe between several choices, or
  drops a probe that has done its work.
- **A plan that moves with time.** A plan is forgotten every step, not shifted
  one moment forward, and looks a fixed number of moments ahead.
- **Learning as structure.** Which pair a row showed, and making a law from
  counts, are host code. Prediction structures that replace a learned table
  are not built.
- **Workspaces in a machine.** A workspace is not saved and the engine does not
  manage one. A workspace views one store or workspace. A wiring's rows cannot
  be supposed about through a workspace.
- **Tension field tests.** The corner shop and the short dialogue that
  exercise the tension laws are experiments kept on branch
  `tension_synthesis`, not part of the core
  ([tension note](../Gojo/docs/GojoTension-2026-11.md), sections 9 and 12).
- **The full tension model.** Material tension (`Tension`), counting laws
  (`Counts`), information tension as a tally, route choice, the slow clock
  and revision by firmness are built, with ranking as a law (`Counts.Rank`)
  and the gap between open values (`Settling.ValueGap`). Ranking whole
  structures by a tension tuple, structural reads as laws, accounted loans,
  and small-law trees in place of large counting tables are not
  ([tension note](../Gojo/docs/GojoTension-2026-11.md)).
- **Replay from checkpoints, energy tiers and missing-route witnesses.**
- **Decorrelation records and semiring reads.**
- **Composing calibrations, and discovering them** (the canonical prompt's
  "evolved calibration" boundary).
- **A parallel backend.** Wave assignment uses host dictionaries, linear in rows
  × roles; `Plane` words are where a SIMD or GPU backend would attach.
- **A bridge to the kernel.** Consolidation §5.2 proposes one; it is not built.
- **Operations and numbers, further.** These are not built:
  - saving an equation, or a step's kind, in the transport;
  - general compiled lowering for arbitrary higher-degree `Polynomial` tables beyond the qualified kernels in `CompiledPlan`;
  - rounding to nearest, which needs a joint window over coarse and fine
    digits;
  - intervals read jointly across rungs;
  - weighted uncertainty;
  - unequal regions;
  - nonlinear propagation in general;
  - a faster `Fits`;
  - accepting a learned equation automatically.

  The order in which they are taken up is in the
  [operations synthesis](../Gojo/docs/GojoOperationsSynthesis-2026-11.md).

## Status against the consolidation roadmap

| Step | Here |
| --- | --- |
| R1 substrate contract | Planes, laws with accounting, ODV and the passage law. The decoder is replaced by the lens. Gap balancing is not ported |
| R2 house workspaces | Workspaces: live reference with copy on write, layered and by-use selection, stale, split and introduced commits, sibling comparison. The house seam example is not ported |
| R3 scoped supports | Per-position supports in the planes, narrowed by relation wirings the engine runs, through laws read as relations, on stores, workspaces and through ports (U0). A joint is a pair value with two relations. U1–U3 are dropped |
| R4 semiring reads and zero contracts | Exact and detection ODV contracts. Semiring reads are dropped |
| R5 tension and energy | Units enter through `Unary` and move through laws. Energy tiers are dropped |
| R6 tick contract | `Engine` with waves in declared order, running transitions and relations. Shuffled-order tests are not done |
| R7 witnesses by replay | Not built |
| R8 learned law tables | Implemented with counts in a store, without eligibility traces |
| R9 SIMD / GPU | Out of scope; `Plane` words are the seam |
| R10 crystallization by description length | `Recognition.Compare` sets a table's bits beside an equation's; nothing is crystallized automatically |

## Boundary

Like `Gojo.Encoding`, this project does not own Gojo meaning and does not
depend on the kernel. Under `AGENTS.md`, it is an exploratory substrate. A
mechanism that proves itself here is promoted into
[`Gojo.Encoding/Core`](../Gojo.Encoding/Core/README.md) with its grammar,
limits and tests.

## Tests

| File | What it shows |
| --- | --- |
| `SubstrateTests` | Planes of any length; growth and retirement; references at the width they need; constant planes; shapes; units entering as units, with overflow; broken counts reported, never repaired; the encoding holds no strings and uses no real numbers |
| `LawTests` | The passage bytes and LUT6 value; accounting refusal with the breaking pair; 130 lanes in one step; unary moves; bits that are not codes told apart from states a law is silent on; one position used twice; a coarse read as a relation; tables compared by content; joint prediction; read-only views; at most 64 values per read; a Boolean operation as one byte read in every direction; adding two counts as one relation that also subtracts |
| `FlowTests` | Ports as lanes and planes, and rebinding; references to one sender from many readers; writing through a port only as a move, and only with positive weights; a moved lane aliasing a row; no role counting one bit twice; one-to-one uncalibrated moves with a remainder; two receivers interpreting one flow; two takers and one unit; branches and merges; self-switching flow; a calibrator structure; a wired rate; retired positions |
| `OdvTests` | Packing `88 C6 FA`; detection as the default and as one contract, checked against a reference reader; exact zero by request; a wrong orientation refused before any bit changes; one position read twice; reorientation; addition narrowed across 100 rows; threshold calibration |
| `WorkspaceTests` | Live reads and copy on write; stale and retired positions refusing a commit; changes made together, however the wiring is run, through a parent workspace, and when reverted into a candidate; coarse-to-fine selection; selection by use up to a reach, counted from the nearest request; sibling workspaces and workspaces over workspaces; constants by reference; a prediction checked against the source; introduced positions adopted; supports to a fixed point, open at positions added later; selections from structure; a supposition read through a flipped view never narrowing what is known |
| `LocalStructureTests` | Own stores bound freely and another's refused (positions, life, workspace), all or nothing; identity as position, through ports and sockets, not equal bits; one taker per position, two takers served alike in either order, and owner layouts that would share a taken position refused; foreign supports read and never narrowed; a taker stepping on through an absent peer, a stalled peer and an unplugged socket; a retired lane reported and rebound; sockets taking only a port of their shape; a structure saved alone keeping its socket and plugged in again; three structures saved together keeping boundaries and connections, step for step; refusal to save a store no structure holds; partial structures fused into one; a socket taken through having one taker, a second refused |
| `PathTests` | One-sided bounds as rays, a bound from above as the same law on the reoriented path, and two sides as the segment; two lower bounds cancelling to the greater, the other redundant; a gap beyond both landmarks, the complement of the closed segment, the same either way round and on either orientation, with unordered branch sites in neither; a segment as the sites an open position can take between landmarks, with open and closed ends, either written order, adjacent and coincident landmarks; the lower landmark as natural start on each orientation; a range keeping its sites when reoriented; landmarks with no order enclosing nothing; a branching route left open; a use read on the reoriented path by swapping site roles, its steps swapped and its turn kept |
| `PathFlowTests` | A unit flowing along a path, one site a step by declared row order, conserved: advancing to the store and resting; turning back as the same path used with the direction set back, retracing to home and staying at rest, its step directions and its one turn read from the sites by the path's step law; a cut in the path holding the unit, the move reported skipped; a fork where the row order, not the path, chooses the route |
| `SidesTests` | Two landmarks giving five places and no sites, with the detection contract's one extra (at both, when close); a fact against one landmark narrowing the other; evidence that cannot be a point between two landmarks kept as information while declared facts and ordinary positions stand; nothing jumping across a mark; the sentence on sides, one word per moment and per step, arrival possible and never past the store until seen; a landmark inside the stretch refining without undoing the coarse read |
| `BridgeObserverTests` | Knowing only what observers see: a world of hikers on a road, observers that read it through ports (a counting strip blind to who and which way, a tracker on one hiker, a watcher at the store), and a story narrowed only from what they expose by authored map knowledge; no crossing ruling out the store; the strip unable to tell reaching the store from turning past the bridge and a watcher telling them apart; two hikers sharing one count until a tracker splits it; a tracker keeping a trail by its own clock and reading from it alone its steps, the turn and both bridge crossings; no structure binding the world except through ports |
| `SidesEventTests` | Goal, source and route, and how far a use got, as presence bits a trail settles against one landmark (at, toward, away, crossed, reached, abandoned, under way): a goal reached after heading toward it; turning back short of it abandoning it; past, to-and-back and from as different patterns of the same bits; partial evidence leaving the outcome open; expected as a supposition, in a workspace inside the trail's own structure, that the last step continues |
| `SidesSentenceTests` | The sentence on sides of one landmark, a bridge, with one side word and one step word per moment and no road length stored: each clause's partial sense alone; being seen at the bridge on the way back establishing that the approach crossed it, in either step order; being seen short of it ruling out a turn past it and leaving the rest open; the lanes as the seam |
| `OrientationMoveTests` | Reverse, face flip and reorient each undoing themselves, commuting and composing into one another for all eight words; reorient keeping the site and the others mirroring it; a reoriented view reading and writing the store turned around without copying; a law on the view matching the store reoriented in place; a view keeping its store's boundary and saved as a view |
| `TwoFaceTests` | Traffic both ways as two faces of one road, each flowing forward on its own orientation: two units passing mid-road at once; a face-aware strip whose gross is two and whose net, one count read as a side of the other, is at zero; a single-lane bridge used one way at a time by an explicit active-orientation bit, never both lanes on it, nothing lost |
| `GoalTests` | A goal as selection, condition and target: an outcome goal selecting only the end and leaving every route open; a route goal selecting every moment and settling the trip; a goal met or not read from its violations, the helicopter meeting the outcome and not the route; a waypoint selecting one moment in the middle |
| `SelectionTests` | A five-position window of a thousand-position structure costing only the window, the rows leaving it reported as beyond; reach bringing in by use what the window's edge rows touch, and stopping; an ignored area staying out though touched; a route plan bringing in the bridge's status it never selected, finding the route impossible while the world is untouched; an attention plane connecting selection to execution in a running structure, moved by one bit and saved with the machine |
| `ForkTests` | A fork reading its parent live and copying only what it writes, turning when the parent's direction changes; a rewired fork taking another route while the parent keeps its own; sibling forks holding different versions; a fork attending to a window of a long road, holding only the window and what running brings in, and stopping at the edge of its attention; a commit writing back, and refused when the parent moved; a fork unable to bind its parent's stores or take through a port; a fork reading everything live seeing a moving parent's unit beside its own, and its staleness telling it so; by default a fork holding its material fixed, keeping only its own unit while the parent moves, still turning with live information, and refusing to commit over a parent that moved; material reached through a flipped view found, held and not following the parent; a move between two of the parent's stores running in the fork; a self-recruiting structure recruiting its own copy of a row |
| `ClosureTests` | A seed snapping up to its whole row, the row seeded directly being the same unit, and a lone position kept as a referent; closure growing a row per unit of reach, keeping a port's lane as a head and not growing through it, and leaving out a plane no selected row reads; opening a port selecting the owner's position and plane and growing in the owner's structure; a settled garage sealing the question's cone until opened, which rereads a conflicting deed; following Bob out to the structures that read him, through ports and a socket, and back to what they share, Bob and Main Street, closed when computed again, and widening to everything any use reaches as more users may lack a position, rows staying whole; gathering by reach in each use and trimming by how many uses reached a place, keeping the bakery two rows out that two days reach and dropping the river only one does; "whose car is this?" selecting only the garage, the deed and the rows joining them, answered when pursued while an unrelated deed never runs, open with too little reach or without the deed, and contradicted by two deeds that disagree |
| `TensionTests` | Room read from a count's capacity as a view; units waiting as pressure and absorbed one a step within capacity, value plus pressure unchanged, the last absorbed once a draw makes room; a draw paid from the value with what cannot be paid left as debt; pressure and debt never netted until a cancelling law is wired; absorbing and drawing together contending for the value, and settling in order; value, pressure and debt found and held by a fork as material, and saved and restored |
| `RouteChoiceTests` | Two routes from one count contested exactly while the choice between them is open, settled by narrowing the choice and contested again when it reopens; a law reading a fact narrowing the choice so only the chosen route runs, until full; a turn following the work letting absorbing and drawing take turns on one value without contending, and then settling |
| `CountsTests` | Sums and both faces of a gap exact for every pair of counts; each face opening one valve so have settles at want from either side; a threshold held as data and moved by writing it; a tally of supports reading open, settled or contradicted; a persistent signal recruiting a spare row through a streak, a threshold and a latch, a blip not; filling an order moving a unit and closing the order together; a fill counted by a receipt tally; oversized tables refused before they are built; a higher tier of tension deciding and a lower one only breaking ties; the gap between open values narrowing every way |
| `RuntimeTests` | A step taken together: a unit moving one site a step whichever way its rows are declared, where a step in order chains it along the road; units in a line each reading the road as it was; two rows racing for one unit both contested, the unit staying put, and the contest left in an evidence plane laws can read; absorbing into and drawing from one count contested though their bits differ, so the count stays whole; settling going on until an observer has read the contest; narrowing taken together settling to the same values as in order, agreeing rows not contested; settling stopping at the first quiet step |
| `TwoClauseTests` | "Moving toward the store, then turned back" as two local structures using one path definition, the turning moment one position owned by the approach: the return's step back starts from it through a socket, and the approach reads the return's next moment through a second lane; each clause's partial sense alone, the return's missing antecedent left as skipped rows; the approach settling that it set out from home and never reached the store, in either step order; an equal-looking approach, equal in every bit, told apart by position and not by value; arrival possible at the closed end and established only by seeing the mover there; the lanes as a seam and the steps inside as not; fused or saved, the return still continuing the same turn |
| `TransportTests` | A machine restored from bits running step for step with the same unresolved parts; 22 bytes for the passage chain, by part; workspaces refused; trailing bits refused; any damaged bit either loads or is refused as malformed |
| `SelfRunningTests` | Machines that only step: a law rewiring a row to the more trusted supplier; a dormant row recruited when flow is blocked; a watcher moving its own attention; a row's activity left in planes and counted by a law; predictors judged and trusted by the structure itself, and one retired when its trust runs out; a goal two steps away opening a valve now, saved and restored; a goal that gives way; what narrowing cannot settle, and a probe that settles it by being wrong; an ambiguous law leaving its next moment open; overflow carried up a workspace; knowledge read through a port and never narrowed there; linked values as a pair with two relations |
| `UncertaintyTests` | Open values in the planes, read by a law and saved; a supposition narrowed in a workspace and dropped on contradiction; a goal settling the open choice that reaches it; ambiguity given a choice role; a miss read by a law as trust; flow steered to a goal that changes, with an unasked unit sent on |
| `OperationTests` | The counting laws as bindings of a balance, keeping their bits; one product answering in every role, narrowed or run; division that holds or leaves the choice open; heap read backward subtracting; the gap keeping one face by cost; bindings checked role by role |
| `OperationPrimitiveTests` | Used up or kept, along or across, read from tables and accounting; join, tear out and cancel as one move on different faces; compare, threshold and accumulate as heap read backward, folded or bound in place |
| `BitRungTests` | The half adder as one balance and the Booleans as its reads; sixteen functions as four acts under polarity; a ripple adder settling in the steps its carries need; a lattice of bit products as the wide product |
| `SettlingVersusTimeTests` | Double as a relation on one position, once a tick written over itself, quiet but unmet under settling with a blocked bit telling the two apart, and met at once in a new count |
| `NumberTests` | A column as a balance with the radix as its weight; two rungs adding as the wide balance does; decimal rungs adding, subtracting and keeping open digits open; what does not fit as the last carry; a count grouped into rungs and refined back |
| `NumberResolutionTests` | Each rung its own partition, fifths under stars and minutes under hours; three of five plus two of five as five of ten; a measurement carrying its uncertainty through a sum; the same share as different knowledge; a change of partition keeping what does not fit; partitions multiplying; ladders multiplying across rungs; units admitted and carried atomically; reopening a ladder, and a sum or product network, before a new observation |
| `RecognitionTests` | Polynomials as canonical numbers; full tables giving their relations back; a wall as an exception; an irregular table staying a table; one wrong entry as an exception; a question telling a sum from a product, and a captured repeat from a sampled one; traces telling coasting from stretching; a learned table becoming an equation before it is complete; a system as tight as the data and saying where not; single and two-residue balance systems (`min`, `max`, `|a − b|`); a restored law keeping the original's promises and behaviour, or refused |
| `SynthesisUnitTests` | Two portions adding on one selected whole; different units refused until an explicit common refinement; measurement range checks that neither wrap nor narrow on failure |
| `SynthesisScopeTests` | Scope read from current port positions; two published scopes pooling and one scope published twice refused; pending scopes keeping five, six or seven tenths through save and load; a missing scope, an observed zero and a contradiction leaving different evidence |
| `SynthesisPublicationTests` | A receiver refining only through the published rung of the same observation; reframing keeping the source basis and the coupled loss across save and load |
| `SynthesisScopedWorkspaceTests` | A scoped change keeping a shared joint and committing both endpoints; a changed calibration leaving the whole patch stale; a grouped change skipping an excluded endpoint and refusing one out of range |
| `ExecutionScheduleConformanceTests` | Both schedules matching their own trace and quiet tick; a machine saved on master continuing under the caller's schedule; a reopened ladder matching a fresh observation; reopening a mapped observation leaving its supplier held; same-tick port/socket row rewrites, same-tick `Store.Life` recruitment/retirement, port lane rebinds, aliased non-identity packet rows, shared-position multi-wave packet rows, and empty-runnable ladders matching canonical `EngineStep` and `WiringStep` evidence |
| `OperationsEvidenceConformanceTests` | Experiments B14 to B18 on every execution path: a repeated pair across transport, four holds and what releases each, a quiet factored product not standing in for the exact inverse, one packet a tick, overlapping patches committing once |
| `PolynomialExactnessTests` | Exponent vectors that cannot collide, admissibility that cannot wrap, coefficients reduced before range checks, recognition refusing a wrapped high power |
| `ExecutionParityFuzzTests` | Random networks of ladders, ripple columns, conversions, tables, recruiting and retiring laws, ports, sockets and row rewiring, run on every execution path in both step modes, matching the engine in state, saved bytes, waves and every row's evidence, refusals included |
| `VariablePartitionAndUncertaintyTests` | Mixed-partition half-stars adding, subtracting and narrowing jointly in a fork; doubling one coarse sample versus summing two independent samples; shared-site cancellation versus independent difference; ordinal, interval and ratio reads; live-denominator portions rejecting zero denominator and keeping two wholes for `5/5 + 5/5`; material packets not splitting; atomic admission at partition 5 surviving `SaveLocals` mid-carry |
| `CompiledPlanTests` | Pass-1 relational ladders and concrete mixed-partition/decimal ripple adders taking the compiled SWAR path; portable SIMD `Flows.Convert`, bounded multiply and SIMD small-table paths; clean fallback on shared sites, recorded wirings, broken codes, simultaneous multi-wiring schedules, self-rewiring and stale edits; reflection check that Pass 3 adds no enums |
| `LearningAndLensTests` | Learning the passage law into counts that are saved and restored; evidence read around each wave; a learned exclusion undone by a later witness; a learner counting what it could not read; refusing a learned table that makes units; unknown pairs staying open; a lens that the encoding never reads |
