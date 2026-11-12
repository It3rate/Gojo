# Gojo.EncodingC

Status: **exploratory**, 12 November 2026. It replaces `Gojo.EncodingB`, which
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
| Local structure | No type yet | The stores and wirings that form one local intelligence. See [Stores and local structures](#stores-and-local-structures) |
| Port | [`Port`](Substrate/Port.cs) | Ordered lanes onto an owner's positions and an ordered list of its planes |
| Law | [`RelationLaw`](Laws/RelationLaw.cs), [`TransitionLaw`](Laws/TransitionLaw.cs), [`Accounting`](Laws/Accounting.cs) | Tables of admitted tuples or transitions, with promises checked when the table is made |
| Wiring | [`Wiring`](Flow/Wiring.cs), `Binding` | A law bound to rows over stores and ports. A role is bound to a code or, open, to what a value may still be. The rows are a store of their own, so topology is state |
| Engine | [`Engine`](Flow/Engine.cs), [`Unary`](Flow/Unary.cs) | Steps wirings in waves: a transition changes state, a relation rules out. Units enter from outside |
| Stock laws | [`Flows`](Flow/Flows.cs), [`Settling`](Laws/Settling.cs) | Small tables for moving things, and for observing, forgetting and acting on what is known |
| Workspace | [`Workspace`](Workspaces/Workspace.cs), [`Gap`](Workspaces/Gap.cs), [`Selections`](Workspaces/Selections.cs) | A fragment of an existing structure, held apart from it and refined there: a live view with copy on write, selection by layers and by use, validated partial commits. Its gap is where it and its source disagree |
| Supports | [`Supports`](Workspaces/Supports.cs) | A value not settled yet: one plane per value in the store, set where the value is ruled out |
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
- The boundary is not built. No type holds a local structure, a wiring belongs
  to no one, and a wiring can bind any store directly. Every machine in the
  tests is one local structure, and a saved machine has no ports to the
  outside. Until this is built, the boundary is kept by whoever wires.

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
*ruled out*. Nothing ruled out is open, all but one is settled, all is a
contradiction. A fresh position is therefore open with no extra step, and each
bit set is one thing learned. "Every value" means every value of a declared,
finite set; a support says nothing about values outside it. A one-bit value
costs two planes: unknown, 0, 1 and contradiction.

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
  value may still be: one plane per value, set where ruled out
  (`Supports.On`). A role bound open to those planes (`supports.Variable`, or
  `Binding.Open(surface, planes)`) is narrowed whenever a relation wired to it
  runs: a value stays possible only if some admitted tuple supports it. A
  plain role of a relation is a sample and is only read, so observing is the
  relation `Settling.Same` between a field's bits and its supports.
- **Forgetting and acting are laws too.** `Settling.Forget` clears what was
  ruled out; wired to run every step it is the passing of time.
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
stays as it was, and the next step starts clean. The machine saves as 114
bytes and runs the same when restored.

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
  wirings, 47 bytes saved, and it keeps the trust, not the history
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
structure, saved with the machine. It costs a second plan, 221 bytes against
114, so how much can be tried at once is bounded by room. Which choices
deserve a probe is not decided by anything yet.

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
| Open / competing | Supports with several values not ruled out | Bits |
| Contradiction | Supports with every value ruled out | Bits |
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
| What a row did last step | Changed, unread, unadmitted, ambiguous, skipped, aliased: `WiringStep`, and six planes of the rows once the wiring records | Bits when recording, else returned |
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
- **The schedule.** Waves are worked out with host dictionaries. On a physical
  substrate that is locality, not a computation.
- **Checking promises.** A law's accounting is verified when the law is made.

## How big it is

Sizes of saved machines, from `Transport.Measure` and pinned in tests:

| Machine | Bytes | Stores / laws / bindings, in bits |
| --- | --- | --- |
| A passage chain: 5 positions, 2 rows, 1 law | 22 | 55 / 80 / 36 |
| Two predictors, a judge and their trust | 47 | |
| The machine that plans two steps ahead: 3 stores of state, 5 laws, 8 wirings | 114 | 298 / 291 / 320 |
| The same with a goal that gives way and a probe that supposes | 221 | |
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

- **Local structures as a boundary.** No type holds a local structure. A
  wiring belongs to no one and can bind any store directly. So nothing checks
  that local structures connect only through ports, nothing steps two of
  them independently, and a saved machine has no ports to the outside. See
  [Stores and local structures](#stores-and-local-structures).
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
| R2 house workspaces | Workspaces: live reference with copy on write, layered and by-use selection, stale, split and introduced commits, sibling comparison. The house seam example is not ported |
| R3 scoped supports | Per-position supports in the planes, narrowed by relation wirings the engine runs, through laws read as relations, on stores, workspaces and through ports (U0). A joint is a pair value with two relations. U1–U3 are dropped |
| R4 semiring reads and zero contracts | Exact and detection ODV contracts. Semiring reads are dropped |
| R5 tension and energy | Units enter through `Unary` and move through laws. Energy tiers are dropped |
| R6 tick contract | `Engine` with waves in declared order, running transitions and relations. Shuffled-order tests are not done |
| R7 witnesses by replay | Not built |
| R8 learned law tables | Implemented with counts in a store, without eligibility traces |
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
| `SubstrateTests` | Planes of any length; growth and retirement; references at the width they need; constant planes; shapes; units entering as units, with overflow; broken counts reported, never repaired; the encoding holds no strings and uses no real numbers |
| `LawTests` | The passage bytes and LUT6 value; accounting refusal with the breaking pair; 130 lanes in one step; unary moves; bits that are not codes told apart from states a law is silent on; one position used twice; a coarse read as a relation; tables compared by content; joint prediction; read-only views; at most 64 values per read; a Boolean operation as one byte read in every direction; adding two counts as one relation that also subtracts |
| `FlowTests` | Ports as lanes and planes, and rebinding; references to one sender from many readers; writing through a port only as a move, and only with positive weights; a moved lane aliasing a row; no role counting one bit twice; one-to-one uncalibrated moves with a remainder; two receivers interpreting one flow; two takers and one unit; branches and merges; self-switching flow; a calibrator structure; a wired rate; retired positions |
| `OdvTests` | Packing `88 C6 FA`; detection as the default and as one contract, checked against a reference reader; exact zero by request; a wrong orientation refused before any bit changes; one position read twice; reorientation; addition narrowed across 100 rows; threshold calibration |
| `WorkspaceTests` | Live reads and copy on write; stale and retired positions refusing a commit; changes made together, however the wiring is run, through a parent workspace, and when reverted into a candidate; coarse-to-fine selection; selection by use up to a reach, counted from the nearest request; sibling workspaces and workspaces over workspaces; constants by reference; a prediction checked against the source; introduced positions adopted; supports to a fixed point, open at positions added later; selections from structure |
| `TransportTests` | A machine restored from bits running step for step with the same unresolved parts; 22 bytes for the passage chain, by part; workspaces refused; trailing bits refused; any damaged bit either loads or is refused as malformed |
| `SelfRunningTests` | Machines that only step: a law rewiring a row to the more trusted supplier; a dormant row recruited when flow is blocked; a watcher moving its own attention; a row's activity left in planes and counted by a law; predictors judged and trusted by the structure itself, and one retired when its trust runs out; a goal two steps away opening a valve now, saved and restored; a goal that gives way; what narrowing cannot settle, and a probe that settles it by being wrong; an ambiguous law leaving its next moment open; overflow carried up a workspace; knowledge read through a port and never narrowed there; linked values as a pair with two relations |
| `UncertaintyTests` | Open values in the planes, read by a law and saved; a supposition narrowed in a workspace and dropped on contradiction; a goal settling the open choice that reaches it; ambiguity given a choice role; a miss read by a law as trust; flow steered to a goal that changes, with an unasked unit sent on |
| `LearningAndLensTests` | Learning the passage law into counts that are saved and restored; evidence read around each wave; a learned exclusion undone by a later witness; a learner counting what it could not read; refusing a learned table that makes units; unknown pairs staying open; a lens that the encoding never reads |
