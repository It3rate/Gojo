# Closure: when a piece of structure is a unit

Status: **design guideline**, 23 November 2026. Parts are implemented in
EncodingC (marked **built**); the rest is direction. This note is the closest
thing Gojo has to guidelines for creating structure: what counts as a unit,
how units join, and how a selection grows and is trimmed.

## 1. The stance

Gojo is not trying to model language. Language is spoken, so it arrives in a
line, but a listener continually updates a model of the situation as the words
come in, and that model is not a line. It is a connected structure, sometimes
circular, sometimes ambiguous, revised as evidence arrives. That structure is
what Gojo builds. Language is useful because it is a very good hint of what
the structure looks like: which pieces count as units, when a piece is
finished, what it may attach to, and what it leaves open.

The units are **kinds of closure, not sizes or levels**. A noun phrase can be
longer than a clause; a row can bind more than a small structure holds. The
rungs are messy and overlap, as they do in thought. What makes something a
unit is which closure conditions it meets.

Sources (recalled, not re-read for this note): situation models and the
event-indexing model (Zwaan and Radvansky, 1998); construction-integration
(Kintsch, 1988); structure building (Gernsbacher, 1990); discourse
representation theory and its accessibility (Kamp and Reyle, 1993);
segmented DRT and the right frontier (Asher and Lascarides, 2003); centering
(Grosz, Joshi and Weinstein, 1995); rhetorical structure (Mann and Thompson,
1988); question semantics (Hamblin, 1973) and the question under discussion
(Roberts, 1996); event segmentation (Zacks and colleagues, 2007); frame
semantics (Fillmore, 1982); lexical-functional grammar's completeness and
coherence (Kaplan and Bresnan, 1982); phases (Chomsky, 2001); chunking
(Miller, 1956); gist and verbatim memory (Sachs, 1967).

## 2. The units

| Unit | What closes it | Gojo equivalent | Status |
| --- | --- | --- | --- |
| A part (morpheme, word) | Nothing: it is not complete | A bit, or one value of a code | built |
| A referent ("the store", "this car") | It picks out something | A position with its fields; identity is position (`SamePosition`) | built |
| A predicate awaiting its subject ("over the bridge") | Nothing until it attaches | A relation with a role not yet bound, such as a side word with no mover | not selectable alone |
| **A proposition, a clause ("Mary put it somewhere")** | **Every role its predicate requires is filled, even by "something"** | **A row: a law with every role bound, possibly to an open value** | **built: a wiring refuses an unbound role** |
| A situation (a sentence) | Clauses joined by shared participants, settling together | Rows joined through shared positions within one local structure | built |
| An episode (a paragraph) | Units about the same participants and one stretch of time | Units sharing identity through ports and sockets, along a trail | built in fixtures |
| A story | Events toward an outcome, with a point | A trail with a goal and completion reads | built in fixtures |
| A question ("whose car is this?") | A proposition with one role open and marked to be settled | An open value plus a goal that it settle | built: `Question` |

## 3. Closure rules

Each rule says when something counts as a unit, or how a selection grows or
is trimmed. "Gathers" rules add; "filters" rules remove.

**C1. Saturation** (valence; frame core elements). *Gathers.* A unit of
thought is a predicate with every required role bound. A role may be bound to
an open value ("somewhere", "someone"): that is complete and uninformative,
not incomplete. In Gojo a row is saturated by construction. **built**

**C2. Snapping up.** *Gathers.* Selecting part of a unit, a role or one
position of a row, selects the smallest saturated unit containing it: "Mary
put" is never selected, the clause is. A position alone may still be selected
as a referent (C3), but anything that runs is a whole row. **built: `Closure`**

**C3. Reference.** A referent is something pointed at: a position and its
fields. Its identity is its position, never its values: two equal-looking
referents are two (`SamePosition`). **built**

**C4. Completeness** (lexical-functional grammar). *Gathers.* A selected unit
brings in every position its rows bind. Nothing a selected row needs is left
out.

**C5. Coherence** (lexical-functional grammar). *Filters.* Nothing is kept that
no selected unit uses: positions no selected row binds, and planes no
selected row reads, are dropped. Seeds are kept as referents. **built:
`Closure`, for positions and planes**

**C6. Head.** Every unit has a head: the place its interface passes through
and the handle it is named by from outside. For a local structure it is its
ports; for a goal, its selected positions; for a question, its open value.

**C7. Thin boundary** (constituency by substitution). *Stops growth.* A group of
units is itself a unit when its connections to everything else pass through
a small interface, so the group could be replaced by that interface. Ports
are the clearest case. Growth stops at a thin boundary and keeps only the
head on the far side; a finer selection opens it. **built: ports kept as
heads, and opened by `open`**

**C8. Phase** (locality). *Filters.* Once a unit is settled, its inside is
sealed and only its edge is read. A settled local structure is read through
its ports; a settled clause through its referents. At a coarse resolution a
finished unit collapses to its head. **built: a position whose open value is
settled is read and not grown through, unless opened**

**C9. Accessibility** (discourse representation theory). A supposition sees
what it supposes about; what it supposes about does not see the supposition
until it is committed. A workspace reads its source; the source never reads
the workspace. A placeholder binds only within its own supposition or an
enclosing one. **built**

**C10. Right frontier** (discourse structure). New material attaches to units
still open, preferring the most recent; a closed unit takes no attachments.
In Gojo, evidence narrows open values; settled units are read, not extended.

**C11. Following a referent** (centering, protagonist tracking). *Gathers.* A
selection may follow one identity: every unit that binds the same position,
through ports and sockets. **built: `Closure.Common`, out to the structures
that use a referent and back to what they share (section 4a)**

**C12. Coherence relations** (rhetorical structure, segmented DRT). Units join
by relations: narration ("then"), cause, elaboration, contrast, condition. In
Gojo these are structure, not labels: "then" is order along a trail; cause is
a law whose output another reads; elaboration is a finer rung of the same
referent; condition is a supposition. A unit joined to nothing is a gap that
asks for a relation.

**C13. Ellipsis and accommodation.** Missing parts are restored from context: in
Gojo, open values narrowed by neighbouring units. Something presupposed and
not present may be introduced as a candidate when enough is said about it to
constrain it ("Pedro's sister"), but not when too little is ("she"). In Gojo,
a candidate position introduced in a workspace and marked as introduced, not
observed. **built: workspace candidates**

**C14. Questions are goals** (question semantics; question under discussion).
A question is a proposition with an open role, and its meaning is the set of
possible answers: exactly an open value's supports. Accepting a question makes
settling that value a goal. The selection a question needs is the cone back
from its open value: every unit that could narrow it. Answering is narrowing
to one value; no answer is a value left open, or a contradiction. See section 5.
**built: `Question`**

**C15. Event boundaries** (event segmentation). *Closes units in time.* An event
closes where the model's prediction fails or the situation changes: a turn, an
arrival, a new participant. Along a trail, the events Gojo already reads
(turned, reached, crossed) are the boundaries between episodes. **built: the
event reads**

**C16. Situation dimensions** (event-indexing model). A situation model tracks
time, space, causation, intention and participants, and a change on any of
them starts a new piece. Gojo's axes: moments and trails (time); paths and
sides (space); laws and flow (causation); goals (intention); identity
(participants). Each is a way a selection can follow or stop.

**C17. Structure building** (foundation, mapping, shifting). *Gathers, then
closes.* Comprehension lays a foundation, maps what follows onto it while it
coheres, and shifts to a new substructure when it no longer does. In Gojo:
a seed unit; attaching new units through shared positions; starting a new
unit, or a new local structure, when nothing joins.

**C18. Chunking and crystallization.** A unit used often becomes a single
referent at a coarser rung, read without its parts. In Gojo: fusion, a law
crystallized from a learned table, a coarse rung of a ladder.

**C19. Gist over wording.** *Filters.* A coarse rung keeps the gist and may let the
detail go; the detail can be read again where it still exists, or is lost
where the read was lossy. Gojo keeps these apart: a coarse read that can be
refined, and a lossy one that says what it dropped.

**C20. Ambiguity and circularity.** Several parses can hold at once, as sibling
workspaces or forks; a loop of units is legitimate and settles as a fixed
point, not an error. Revision changes a candidate interpretation, not what was
observed (garden-path repair). **built: forks, staleness, narrowing to a fixed
point**

## 4. Selection as closure over a seed

A selection is built, not listed:

1. **Seed:** a position, a role, a port, a goal or a question.
2. **Saturate (C1, C2):** a role or position snaps up to the rows that bind it.
3. **Complete (C4):** those rows bring in every position they bind.
4. **Grow (C11, C12, C16):** through shared positions, up to a reach, along
   whichever dimensions the selection follows.
5. **Stop (C7, C8):** at thin boundaries and settled units, keeping only their
   heads, unless the resolution asks to open them.
6. **Filter (C5, C19):** drop what no selected unit uses, and planes no selected
   row reads.

The result is ordinary selection: an area per store and the rows of each
wiring, so engines, workspaces and forks can use it at once.

**Resolution** is set by the knobs, not by fixed levels: the reach; whether
thin boundaries and settled units are opened or kept as heads; which
dimensions are followed; and, for continuous domains, which landmarks are
present. Gathering and filtering are both part of every selection: the
same rules that complete a thought also drop what it does not use.

## 4a. Out and back: what the uses of a referent share

Growth (section 4) only gathers, so the further it reaches, the more it
selects. Following a referent the other way can also trim. It is the closure
of formal concept analysis (Ganter and Wille, 1999), with structures as the
objects and the positions they reference as the attributes:

1. **Out:** from the seed, to every structure in view that references all of
   it. Positions are taken by identity, so a lane read through a port or a
   socket is its owner's position.
2. **Back:** from those structures, to the positions they share.

What is shared is what every use of the referent also involves: Bob's days
all happen on Main Street, so Bob and Main Street are one unit for selection,
whatever else each day adds. A position private to one structure drops out.
Doing it again from the result gives it back; it is closed.

**One number between too little and too much.** The strict intersection can
underselect: one odd use removes everything else. So the closure takes
`missing`, how many of the using structures may lack a position for it to
stay. At 0 it is the strict core; at one less than the number of users it is
everything any of them references, the union. The selections in between are
the partly shared ones, and the values of `missing` where the selection
changes are its natural resolutions: a ladder read from the structure, not
chosen in advance.

**What it needs to know** is only which rows bind which positions, structure
that is already there, plus which structures are in view. A port does not know
its readers, so the view is given: it is the asker's world, and a different
view is a different question.

**Gather, then trim.** Growth and sharing combine: each use grows by reach
from the seed through its own rows, and the uses then vote. A position's score
is how many uses reached it, a number that can be read and compared, and
`missing` sets how many votes it may lack. Reach says how far one use may
gather; sharing trims what too few uses gathered. Plain growth is the case
where one vote is enough; the strict concept is the case with unlimited reach
and every vote needed. A row is kept only whole: if a use reached it and every
position it binds stayed. Trimming never leaves part of a unit (C2 is applied
before C5).

**built:** `Closure.Common` with `reach`, `missing` and `Score`, tested in
`ClosureTests`.

## 5. Questions as goals

"Whose car is this?" builds "this car belongs to *someone*". *Someone* is an
open value; its supports are the possible answers. Accepting the question
marks that value as a goal: **settle it**. This is a goal whose target is not a
value but settledness, and it differs from the goals in `GoalTests`, which
hold a selected position to a fixed target.

What the question needs is the cone back from its open value: the rows that
could narrow it, and the positions they bind. Selecting by closure from the
question as seed gives exactly that, and leaves the rest of the world
untouched. Settling then either answers it (one value left), leaves it open
(not enough evidence), or contradicts (the premises disagree), and each is a
result.

Interpolating toward a goal is the same move as filling a question: suppose
the gap closed, and let narrowing and probes reveal what would have to hold.
A missing element is found by constructing the goal state with the element
open and settling toward it.

## 6. First implementation

Built, 23 November 2026: `Closure` and `Question` in EncodingC, tested in
`ClosureTests`. Steps 1 to 6 of section 4 are built. Step 5 treats a port and
a settled position as one kind of sealed boundary, and `open` is a number:
how many sealed boundaries growth may cross on the way to any position.
Crossing a port selects the owner's position and grows on in the owner's
structure; crossing a settled position rereads what settled it. A question's remaining
answers are a count: one answered, more open, zero contradicted.


- **Closure from a seed** (section 4, steps 1 to 6) over one local structure,
  with reach and with ports kept as heads, giving areas and rows that a fork
  or engine can use.
- **A question** as an open value plus the goal that it settle, read as
  answered, open or contradicted.

Tests:

- A role selected alone snaps up to its whole row; a lone position is kept as
  a referent.
- Closure from a seed gathers its rows and their positions, grows by reach,
  stops at a port, and drops planes no selected row reads.
- "Whose car is this?" in a structure with unrelated facts: closure from the
  question selects only what could answer it; settling answers it; with the
  deciding fact absent, it stays open; with conflicting facts, it contradicts.

## 7. Open questions

- **Thin boundaries inside a structure.** Ports mark them between structures;
  inside one, a boundary is a small cut, which is a measurable but costly
  read. When to look for one is open.
- **Optional roles.** Frames distinguish core from peripheral elements. Gojo
  rows bind every role; optional participation is a separate row. Whether
  that is enough is open.
- **Coherence relations as structure.** Section 3 names how each maps; only
  "then" and cause exist as structure today.
- **Rows or structures as the objects.** `Closure.Common` takes whole
  structures as the objects. Rows as objects give a finer closure, but a row
  of two roles shares little with another; a structure is the unit that has
  a point of view. Whether objects should be any unit, chosen like reach, is
  open.
- **Iterating with a tolerance.** With `missing` above 0 the result need not
  be closed: going out again from it can find different users. Whether to
  iterate to a fixed point, and whether that always settles, is open.
- **Trimming, then gathering.** Gathering by reach then trimming is built.
  The reverse, growing from the shared core, would keep growth anchored on
  what every use agrees on. Whether it differs usefully is open.
- **Scores beyond counting.** A use counts once whatever it is. Weighting uses
  by trust, recency or how far they reached is open, and would need a reason
  in the structure rather than a chosen weight.
- **Which settled units seal.** Today a settled open value seals growth
  through the row that reads it as open. A plain state code is never sealed,
  since a transition may still change it. Whether settled means more, such as
  a unit whose rows are all quiet, is open.
