# Tension: what is unaccounted, and what lowers it

Status: **proposal**, 23 November 2026. Nothing here is built in EncodingC
yet except where marked. It gathers what the canonical prompt (Axioms 5 to 7,
§2.7, §5.2, §5.4), the consolidation note (§5.9, §5.10) and the EncodingC
pieces already say. It then proposes one account of tension down to the bit,
as the motivator for the structural actions that come next: splitting,
merging and changing laws.

## 1. The stance

Gojo lowers tension; it does not solve equations. Something must therefore say
what tension *is*, how much of it there is, where it sits, and what may lower
it. Today there are several partial answers:

- **Kernel carrier tension:** a signed i64 absorbed into value (Axioms 5 to 7).
- **OrnamentTension's pulses,** with capacity and acknowledged relief.
- **EncodingC:**
  - units blocked at entry (`UnaryStep.Blocked`);
  - violated rows (`Wiring.Violations`) and evidence planes;
  - empty and open supports;
  - workspace gaps;
  - goal violations.

  These have no common account.

The proposal is one definition with a few kinds, all read from planes, so
that the structures that can act on tension can read it (§5.4: "a result only
reported to a caller cannot steer anything").

> **Tension is participation the structure has not accounted for, in a place
> where something requires it to be accounted for.**

The second half matters. The canonical prompt already says that uncertainty
alone does not introduce tension (§5.2), and §5.4 that a structure with
nothing open has nowhere left to change. Openness is room, not strain. It
becomes tension only where something demands it settle: a question, a goal,
a law that needs a code, a receiver that needs units.

## 2. What language teaches

Language is the oldest system that runs on uncertainty and treats it as a
feature. The habits that make that work translate directly.

- **Openness is free until demanded.** Speakers leave almost everything
  unspecified ("someone", "over there", "soon"), and hearers do not try to
  settle it unless the conversation needs it. Underspecification is the
  default; a question is what makes a gap cost something. Gojo: an open value
  carries no tension until a demand selects it (C14 in the closure note).
- **Two opposed economies.** Zipf saw that the speaker wants to say less and
  the hearer wants to be told more. Horn (1984) reduced Grice's maxims to that
  pair:
  - the Q-principle: say as much as you can, given R;
  - the R-principle: say no more than you must, given Q.

  Every kind of tension below has the same two faces: too little and too much.
  They are kept apart, never netted, because they are lowered by opposite
  moves.
- **Grice's maxims as the kinds:**
  - Quantity is the information deficit and excess.
  - Quality is contradiction.
  - Relation is a unit attached to nothing, a structural deficit.
  - Manner (avoid ambiguity, be brief) is a structural excess.
- **Obligations are debts.** A question, offer or request opens an obligation
  that persists until it is answered (adjacency pairs). A contribution is not
  part of common ground until the partner accepts it (Clark and Brennan,
  1991, grounding). Both are material in Gojo's sense: something owed or
  delivered and not yet absorbed.
- **Accommodation absorbs.** When something is presupposed and not present,
  the hearer adjusts the score so that the utterance fits, if it can (Lewis,
  1979). That is absorption: incoming participation finds a place by a small,
  permitted change on the receiving side.
- **Repair is cheap and targeted.** When understanding fails, people repair in
  the way that costs the pair least: a restricted "which one?" rather than an
  open "what?" (Clark and Wilkes-Gibbs, 1986; least collaborative effort). In
  Gojo: lower tension with the smallest question whose cone covers it, which
  closure already computes.
- **Granularity is chosen by relevance.** Things are treated as the same when
  no predicate that matters tells them apart (Hobbs, 1985, "Granularity").
  Resolution belongs to the read (Axiom 9): a coarse read is not in tension
  with a fine one unless something demands the fine one.

## 3. The kinds of tension

What is unaccounted can be **amounts**, **possibilities** or **structure**,
and each has two faces.

| | Deficit (too little) | Excess (too much) |
| --- | --- | --- |
| **Material** (amounts) | *Draw:* demand not supplied, an unpaid debt, an open obligation | *Pressure:* units arrived with no room, blocked or overflowing, a contribution not yet taken up |
| **Information** (possibilities) | *Openness under demand:* more than one value left where one is required | *Contradiction:* no value left; the premises disagree |
| **Structure** (form) | *Missing structure:* a residual no available route can lower; a unit joined to nothing | *Redundancy:* structure the flows do not use, or use twice; ambiguity that never settles |

Contention, two actions over one site, is not a fourth kind. It is an
information tension on a choice the structure has not represented, and,
when it persists, a structural one (§3.5).

### 3.1 Material

Material tension is the canonical prompt's carrier tension (Axiom 5): external
change enters as tension and is absorbed into value within capacity. Its two
faces are what the consolidation note calls `T+` and `T−`. They are **never
netted automatically**. A deficit and an excess at one site came from
different participation, and cancelling them would discard both witnesses. It
is the oldest rule of accounting: double entry keeps debit and credit apart,
and a balance is a read, not a merge. Cancelling one against the other is a
law, wired where the structure says they are the same quantity.

Material tension is conserved: it can move, be absorbed, or be paid, never
silently disappear.

### 3.2 Information

The supports of a value already show both faces. Let `n` be the number of
values still possible at a position where settling is demanded:

```text
n = 1    settled: no information tension
n > 1    deficit: n - 1 alternatives left to rule out
n = 0    excess: more constraint than any value can meet
```

A question's `Remaining` (built in `Question`) is this number. The target is
one, and both directions away from it are tension, with opposite remedies:
- **Deficit:** observe, ask, or reach further.
- **Excess:** find the premise to revise. Here the canonical prompt's caution
  applies: a residual does not say which participant is wrong (§5.1).

Information tension is not conserved. It is lowered by narrowing, which is
free, and it rises with every new demand. Possibility theory (Zadeh, 1978;
Dubois and Prade, 1988) is the closest formal relative: supports are a
possibility distribution with levels 0 and 1, the necessity of a set is the
impossibility of its complement, and total inconsistency is an empty
distribution. Graded possibility would be a thermometer per value; Gojo does
not need it yet.

### 3.3 Structure

Structural tension is about the form of the structure, not its state:
- **Deficit:** a residual that persists because no route can lower it. This
  is Axiom 11's orthogonal remainder made computable (consolidation §5.9:
  "quiet is not optimal"), or a unit attached to nothing (C12).
- **Excess:** structure nothing needs:
  - rows that never change anything;
  - two rows that do the same work;
  - positions that always move together;
  - alternatives that are kept but never resolve.

This is the motivator for splitting, merging and changing laws. Its measure
has an old name: description length. The shortest description that still
accounts for the evidence is preferred (Rissanen, 1978, minimum description
length). In Gojo it is literal: `Transport.Measure` already counts the bits a
machine takes. Too short leaves residuals, the deficit face. Too long carries
redundancy, the excess face. It is the same two pulls as Horn's.

### 3.4 Comparisons are sources, not kinds

Goal gaps and prediction errors are not further kinds. They are comparisons
that produce the kinds above, and §5.4 keeps three of them apart:
- **How well a structure expects to predict:** its open structure, an
  information read.
- **How well it did:** expected against observed, read as contradiction in a
  workspace (surprise), and over time as structural deficit.
- **How far the present is from a goal:** have against want, read as material
  deficit and excess in the goal's basis (the two-faced `Gap`, noted in
  `Gap.cs`), or as information deficit for a question.

A goal does not establish current truth (§5.2), so goal tension lives in the
supposition, never as carrier tension in the source.

### 3.5 Contention

When two rows would take the same unit, or write the same bit, in one step
taken together, both are held back (`Engine.StepTogether`). Neither row is
wrong, no premise is false and nothing is redundant: two consumers need one
resource. This point was raised in review; credit to that comment.

It breaks into the kinds above:

- **In one step, it is an unwritten choice.** "Which row takes the unit" is a
  value with two possibilities, and one taker per position demands one. That
  is information deficit on a choice variable the structure has not
  represented. Petri nets call two transitions that share an input place a
  conflict, and treat it the same way: as a choice.
- **Over time, it is missing structure.** The same pair colliding again and
  again says the structure lacks capacity or a route. That is the slow-clock
  signal to duplicate or split the bottleneck.
- **Its two faces:**
  - *Excess:* two writers for one bit.
  - *Deficit:* a unit something needs that no row takes (starvation).

Lowering it needs no special mechanism once the choice is a value. A
turn-taking gate is a law that settles the choice, for example by
alternating. The single-lane bridge's active-orientation bit in `TwoFaceTests`
is one already built. Conversation does the same: turn-taking has rules for
who goes next, and overlap is resolved by them (Sacks, Schegloff and
Jefferson, 1974).

So the missing piece is explicit route choice: the taker as an open value.
It belongs with the tension work, not as a separate case.

**Built on branch `claude_tension`** (`Supports.Possible`, gated moves in
`Tension`, `RouteChoiceTests`). A route is a move gated by one bit:
- **Gates from an open choice:** a choice with one support plane per route
  gates each route by "still possible". While several remain possible, their
  rows are contested, so the contested rows are exactly the open options.
  Narrowing the choice, by the host or by a law reading a fact, settles it,
  and taken together only the chosen route runs.
- **Gates from a settled code:** a turn bit gates one move directly and the
  other through its flipped view, so exactly one may run in a step. Absorbing
  and drawing on one value then take turns and both settle.

A turn bit that toggles every step never goes quiet: it is a driven
controller (consolidation class C4) and needs its own stop.

**Re-deciding a choice: justified possibility** (`Settling.Justify`, branch
`claude_tension`). Narrowing only rules out, which is right for what was
learned about the past, and wrong for a possibility about now that holds for
a reason that can stop holding. Reopening every step and narrowing again
writes the same bits both ways each step: taken together they contend, in
order they never go quiet. A justified support bit instead has one writer, a
law that sets it from its reasons each step: 1 while every reason holds,
0 otherwise. It reopens when its reasons return, writes only on change, and
never contends with itself (truth maintenance; Doyle, 1979). It must be
declared before what it gates, or the gate reads it a step stale. So
possibility comes in two kinds:

- **Accumulated:** narrowed by relations, never reopened. What was learned
  about the past.
- **Justified:** computed from current reasons by one law. What is possible
  now.

## 4. Down to the bit

Everything below follows §9.7: 1 is presence, here the presence of something
unaccounted.

**Material: two unary fields per site.** `T+` and `T−` are thermometer counts,
bit k set when more than k units are held.
- **Larger tension is more bits set,** physically readable.
- **Adding a unit is a shift;** comparing two counts is a mask test.
- **Capacity is the field's width.**

`Unary` already holds counts this way and returns what it could not take as
`Blocked`. Under this proposal, blocked units become `T+` at the receiving
site instead of being handed back to the host. Absorption is then an
ordinary transition law over (`T+`, value), conserving their sum, so it runs
in waves, is saved, and can be forked like any other law. A draw is the same
law the other way: `T−` is paid from value.

**Information: read, not stored.** The deficit and excess are computed from the
support planes and the demand that selects them:
- demand is attention, a question's cone, or a goal's selection;
- the open planes AND the demand plane is the deficit;
- the empty planes AND the demand plane is the excess.

Built as a law: a tally of the support planes (`Counts.Tally`) writes how
many values remain as a count, whose thermometer bits are the reads. Bit 0 is
"any possible", bit 1 "more than one"; settled is bit 0 without bit 1, and
contradicted is no bit 0.

**Laws: residuals in the evidence planes.** Each run already leaves one bit per
row and kind:
- unadmitted is a violation;
- ambiguous is openness at the law;
- skipped is a missing antecedent;
- aliased is a structural fault;
- contested is an open choice of taker that nothing settled (built).

A step in order never sets contested, since declared order chose.

These planes are a law's tension, at the row where it sits.

**Structure: counts over evidence.**
- *Idle:* a row whose changed bit stays clear over a window.
- *Duplicate:* two rows of one law binding the same positions by identity.
- *Joined:* two positions whose bits never differ over a window.
- *Persistent:* a residual that survives a whole settle.
- *Description length:* the machine's measured bits.

Each is a count over planes that already exist, and each points at a specific
place, so a structural action has somewhere to happen.

**Sign and orientation.** A deficit on one orientation is an excess on the
reoriented one, as a debt is a credit seen from the other side: excess credit
of 3 is a debt of −3. Reorientation keeps the amount and flips the sign and
face (Axiom 4: `Reorient(L(C,n)) = L(C*, −n)`). It does not turn a full
bucket into an empty one. Reorienting a site therefore swaps which of `T+`
and `T−` reads as positive: a relabelling, with no bit changed and no
capacity involved. This is a hypothesis to test with the work on radial and
linear direction; it would tie tension's two faces to ODV's D bit.

**Room is not reorientation.** A bounded unary count has a second reading:
the room left, capacity minus value. Inverting a unary count's planes and
reading them in reverse order gives exactly that, with nothing copied, so a
`Flipped` view bound with its planes in reverse order is the room. Room is a
different quantity from the reoriented value: 1 held of 3 is 2 of room, not
−1. It needs the capacity as a second landmark and measures from that end. In
the canonical prompt's terms it is anti-fill (§8.2), a dual read of a bounded
segment, not a reorientation under a shared origin. Keep the three moves
apart: the face swap (reorientation), the room read (anti-fill), and the
reversed plane order that builds the room view.

**Absorb, draw and cancel are one law.** The room read makes the material
operations a single existing law, `Flows.Move`, checked against EncodingC on
24 November 2026:

- **Absorb:** a move from `T+` into the value. It stops when the value is at
  capacity, and `T+` waits.
- **Draw:** a move from `T−` into the value's room. Filling the room empties
  the value, so the debt is paid from what is held. It stops when the value
  is 0, and the unmet draw stays in `T−`.
- **Cancel:** a move from `T+` into the room of `T−`, lowering both by one. It
  is wired only where the structure says the two are the same quantity, so
  nothing is netted by default.

Each conserves through the move's own preserved sum: value plus `T+`, value
minus `T−`, `T+` minus `T−`. This was pointed out in review; credit to that
analysis.

## 5. Ranking: which tension matters first

Tension kinds are not traded against each other freely. A violated accounting
rule is not paid for by a met preference. Linguistics has the clearest model:
in Optimality Theory, constraints are ranked, and no number of satisfied
lower constraints outweighs one violated higher constraint (strict
domination; Prince and Smolensky, 1993). Harmonic Grammar replaces ranks with
weights and allows small violations to gang up. Gojo should rank:

```text
hard        accounting broken: material made or lost     always 0, or it is a fault
obligation  laws and claims: contradictions, violations
material    T+ and T-: pressure and draw
goal        goal and question tension, in suppositions
structure   redundancy and missing structure, on the slow clock
```

Each tier is a count, and the energy is the tuple of counts read in order. A
lexicographic comparison of thermometer counts is a sequence of mask tests,
cheap and exact. **Admissibility** (consolidation §5.9): a step may not raise
a higher tier, and should lower some tier or be a declared neutral transfer.
Weights, and gang effects between tiers, are left out until a structure gives
a reason for them. A chosen weight would be meaning supplied from outside.

## 6. What lowers it, and when

Different tensions are lowered on different clocks (consolidation §5.10).

- **Fast: settling.**
  - *Material* moves along routes and is absorbed within capacity. Laws whose
    result does not depend on firing order (chip-firing's abelian property;
    Björner, Lovász and Shor, 1991) give one answer however they are
    scheduled. That is what `StepTogether` protects.
  - *Information* narrows monotonically to a fixed point (consolidation's
    class C1).
  - Piaget's term for this is *assimilation*: what arrives is fitted into the
    structure as it is.
- **Medium: choosing.** Where several routes or candidates fit, the choice
  that lowered tension before is favoured: route strengths revised by
  acknowledged relief (OrnamentTension). Repair belongs here: the narrowest
  question that lowers the tension.
- **Slow: restructuring.** When tension stays above a bound for long, change
  the structure. Ashby's homeostat (1952, *Design for a Brain*) is the exact
  model:
  - *Essential variables* have bounds. Ordinary feedback keeps them in bounds
    without changing the system.
  - When they leave their bounds, a *step change* alters the system's
    parameters, and ordinary feedback resumes.
  - This is *ultrastability*.

  In Gojo, the essential variables are the tension tiers, and the step changes
  are structural edits: split, merge, add a route, retire a row, change a law.
  Each edit happens at a place that was flexible, so it was uncertain whether
  or not anything recorded it (§5.4). Piaget calls this *accommodation*.
  Festinger's cognitive dissonance (1957) lists the same escapes from the
  inside: change one of the conflicting elements, add one that reconciles
  them, or lower the importance of the conflict.

**Escapes from a stuck state,** each visible (consolidation §5.9):
- widen the scope by connecting a route;
- take an explicit tension loan: a budgeted, recorded temporary rise, like a
  noise step in WalkSAT or a temperature in annealing, but accounted;
- propose a structural edit on the slow clock.

**A quiet state with tension left is a result, not a failure.** Its witness is
the tension and where it sits. Self-organized criticality (Bak, Tang and
Wiesenfeld, 1987) shows the other side: systems that store tension locally
and release it in cascades when a threshold is crossed. That is a useful
picture for structural change too, where many small residuals precede one
reorganization.

## 7. How the motivators map

The user's three motivators for structural action, as tension:

| Motivator | Tension | Action it calls for |
| --- | --- | --- |
| Too complex | Structural excess: description longer than the evidence needs | Merge, retire, crystallize (C18) |
| Redundant | Structural excess: duplicate rows, joined positions, idle rows | Merge, retire |
| Uncertain | Information deficit under demand, persisting | Reach further, ask, split a confused unit, add a route |
| Contradictory | Information excess, persisting | Revise a premise, split what was wrongly joined, lower a demand |
| Stuck | Material or goal tension with no route | Add a route, take a loan, change a law |
| Contended | Information deficit on an unwritten choice; persisting, structural deficit | Make the choice a value and settle it by a gate; duplicate or split the bottleneck |

Splitting has a numeric cue from the closure work: where `Closure.Common`'s
scores fall off, the uses of a unit stop sharing it, which is a candidate cut.

## 8. What is built, on branch `claude_tension`

1. **Material tension** (`Tension`, `Field.Room`). Change from outside enters
   as `T+` or `T−`, never into the value. Absorbing, paying and cancelling are
   each the one-unit move, into the value or into a count's room.
   **Delivering** (`Flows.Fulfill`, `Tension.Deliver`) pays a debt by sending
   the unit on to an outlet and closing the order in the same move, so a
   delivered unit is somewhere, not gone.
2. **Counting as laws** (`Counts`), emulating the arithmetic numeric support
   will provide:
   - `Sum`: accumulation, with an optional rate;
   - `Shortfall`: one face of a gap; the other face is the same law swapped;
   - `Tally`: bits set, used over a value's supports for information tension;
   - `AtLeast`: a threshold read, with the threshold held as data;
   - `Accumulate` and `Streak`: accumulation, held or reset.

   Each writes one result from read inputs, only when it changes, so its
   meaning is a saved table and a structure settles quietly around it. They
   replace the earlier host-computed views (§11).
3. **Information tension** is a tally of a value's supports: thermometer bit 0
   is "any possible", bit 1 "more than one". Settled is bit 0 without bit 1;
   contradicted is no bit 0. A law waits on "settled" through the bit and the
   flipped view of the other.
4. **Route choice** (§3.5): moves gated by any number of bits (`Flows.When`,
   `Tension.Route`, `Tension.Wire`); open choices whose contest is the choice;
   justified possibilities (`Settling.Justify`) that reopen with their
   reasons; and a turn (`Flows.Turn`) that alternates only while both sides
   have work, so taking turns settles.
5. **The slow clock** from small laws: a streak of a signal, read against a
   patience held as data, triggers a latch (`Settling.Latch`) that recruits or
   retires a row. Ashby's step change, made by structure.
6. **Revision of what was learned:** of two contradicting premises, the less
   firm is retired by a law (firmness compared by `Shortfall`, the trigger by
   `Justify`, the retirement by `Latch`), and a gated `Forget` reopens the
   value so the remaining premises narrow it again. No host call reopens it.

**Not built yet:** the tension tuple and its admissibility check (§5) as
structure, and structural reads (idle, duplicate, joined rows) as laws.

## 9. Field test: the corner shop

`ShopTests` runs a shop as four local structures joined only by ports: a
customer, the shop, and two wholesalers. The host builds them, steps them and
posts what arrives from outside; everything else is laws. Items and requests
are conserved across all four.

| Iteration | What runs | What it shows |
| --- | --- | --- |
| 1. Material | The customer's requests arrive through a port as backorders; the shop hands items over at a pickup counter, from which the customer collects through the shop's port | Five of six requests are filled from shelf and dock; the sixth stays owed, with a case booked against it that nobody can fill |
| 2. Reordering | On hand (shelf, dock, two per case on order) and wanted (target, backorders) are sums; their shortfall against a reorder point places orders; a wholesaler's delivery fills an order | The shop stops at four, in whole cases; selling three books one more |
| 2b. A cut line | The line to the wholesaler is a socket, unplugged and restored | Counting orders on the books, two cases are booked and delivered on restore. Not counting them, three are booked, leaving two extra at the dock and a case still owed |
| 3. Two wholesalers | A is closed; a law reading A's status makes A impossible | Narrowed, A never reopens and the shop ends owing two while A has stock. Justified, A reopens and everything is delivered |
| 4. The slow clock | B is wired but not in use; backorders that last three steps recruit it | A single request served at once does not recruit B; backorders that persist do, and everything owed is delivered |
| 5. The books | Batches' suppliers are open values narrowed by delivery notes; invoices are paid when the batch is settled | One note: paid. Conflicting notes of unequal firmness: the weaker is retired, the batch reopened and paid. Equal firmness: stays contradicted. No note: stays open |
| 6. A trial | "Buy only from B" on a saved and loaded copy of all four structures | A fork is refused while the shop takes through its lines; the copy runs the trial and leaves everything unchanged |

**Taken from the other branches.** Three branches built tension in parallel.
This one took, from `sol_tension`, the conserving delivery move, the customer
as a structure whose requests enter through a port, orders on the books closed
by the delivery that answers them, and gaps held in law tables rather than
host code; and from `tension_neon`, firmness resolving contradictions, the
slow clock as streaks and thresholds driving recruitment, and turn-taking
that goes quiet. Each was rebuilt from the small primitives above rather than
as a special-purpose law.

**Findings left open:**

- **Trials while taking.** A structure that takes through a port cannot fork,
  so a trial is a full copy and sees copies of its peers. `tension_neon`
  snapshots taken ports into a fork instead, refusing commit; that is cheaper
  and worth adopting.
- **One dock, two uses.** Receiving into the dock and shelving from it write
  the same count. The shop runs in declared order; taken together it would
  need a turn at the dock (`Flows.Turn` now exists for that).
- **Provenance is lost on the shelf.** Units in a count are interchangeable;
  writing a record when a case arrives needs allocation by laws.
- **Priority is declared order.** Serving, shelving and ordering run in the
  order they were added, not by a ranked tension tuple (§5).
- **Table size.** Counting laws enumerate their counts, so capacities stay
  small (a shelf of four). This is what numeric support removes.

## 10. Questions for the axioms

- **Whether "tension" should name all three kinds.** The canonical prompt uses
  tension for transferred change (carrier tension) and says uncertainty alone
  does not introduce it. This proposal agrees and adds that *demanded*
  uncertainty is tension of another kind. If accepted, the prompt should name
  material tension as today's carrier tension and add information and
  structural tension with their demands. If not, the other two need another
  name.
- **Whether material tension may ever be netted by default.** This proposes no,
  consistent with K6 in the consolidation note.
- **Ranking versus weights.** This proposes strict ranking until a structure
  supplies a reason for weights.
- **Where demand comes from.** Questions, goals and laws that need a code are
  clear. Whether attention alone is a demand is open.

## 11. Host-computed views, replaced by counting laws

An earlier version read the gap and information tension through live views,
`Difference` and `Openness`, whose meaning was computed in host code on every
read. Nothing in a plane or a table said what a deficit was, and no law could
change how one was computed. This came from two axioms pulling apart: "prefer
calculated to stored" led to live views, while "meaning lives in Gojo
structure" says the calculation itself must be structure.

Both were thresholds over counts, and both are now counting laws (§8, item
2): a sum and a shortfall for the gap, a tally and its thermometer bits for
information tension. The laws keep "calculated": a law writes its result from
current inputs each step and only when it changes, so nothing is stored that
is not derived. What changes is that the calculation is a table the structure
holds. The views, and the save and fork code they needed, are removed.

The counting laws are the emulation of the first numeric step. Their roles
are the arithmetic numeric support will provide: accumulation (`Sum`,
`Accumulate`, `Streak`) and threshold reads with thresholds held as data
(`AtLeast`, and the thermometer bits of a count, a bank of fixed thresholds).
With magnitudes in planes, each becomes arithmetic on a magnitude, the tables
stop growing with capacity, and every wiring that reads them stays as it is.

## 12. Field test: a short dialogue

`DialogueTests` (branch `claude_tension`) builds the mechanics of a short
exchange: asking, answering, asking which one, declining and grounding. What a
car or an owner is does not matter; only the structure of the exchange does.

**The construct.** Each speaker is a local structure; they meet only through
ports. An utterance has two parts: a **token**, an obligation moved and
conserved like any material, and **content**, a value the listener reads
through the speaker's port to narrow an open value of its own. A question's
token has one ledger: asked, owed (debt at the answerer), answered or declined
(the answerer's outbox), heard (pressure at the asker), grounded.

| Scenario | What runs | What it shows |
| --- | --- | --- |
| Whose car is this? (clear) | Ask, answer, acknowledge | The token passes through every stage, conserved; the answer is pressure until acknowledged |
| Whose car? "This" is one of two cars, both John's | The answer settles despite the open referent | Ambiguity is tracked (two cars still possible) but not repaired: it does not reach the answer |
| Whose car? Two cars, different owners | The answerer asks which one; the asker answers with the car it means | An inserted question, its own debt, resolves the referent; both questions are grounded |
| Is this car red? Same two cars, both red | Answered yes at once | Same data, different question: no repair. Scope, not data, decides |
| Whose car? The answerer does not know | Nothing to ask would settle it, so it declines | The obligation is discharged; the question stays open, its deficit visible |
| Two answerers disagree | Both answers heard, neither acknowledged | The asker's value is contradicted; both answers stay as pressure |

Saved, an exchange with a clarification is about 460 bytes, with under 0.5 KB
of law tables in memory, and settles in four rounds (about 2.6 ms).

**Strategies chosen, and why:**

- **Obligations as material tension.** A question is a unit of debt that only
  an answer or a decline can discharge, so "unanswered" is visible and
  conserved, not a flag.
- **Content read through ports, not sent as material.** Words are information:
  any number of listeners may read them, and reading narrows only the
  reader's own value. Narrowing waits until the words have arrived
  (`Settling.SameWhen`), so nothing is narrowed by what was never said.
- **Each utterance is its own position.** The one-taker rule applies to a
  position, and two listeners taking different fields of one position were
  refused. Rather than loosen the rule, each outgoing utterance became a slot
  of its own: an utterance is a thing, so it has its own identity.
- **Demand comes from the question.** The answerer repairs only while the
  answer, the value the question demands, is open. An open referent is
  information tension only if it reaches the answer, which the relation from
  car to answer decides by narrowing. Whether to resolve an ambiguity is
  computed from the question's scope, not from what happens to be uncertain.
- **Repair ranked by exclusive gates.** Answer if settled; else ask which one
  while an input to the answer is open; else decline. Each gate is a
  justified bit, and they exclude each other, so the cheaper repair is always
  the one taken, without a ranking table.
- **Turns from obligations, not a controller.** Whoever owes something acts.
  An obligation selects the next speaker, as in conversation (Sacks, Schegloff
  and Jefferson, 1974), so no turn bit runs, and the exchange settles.
- **Grounding waits on settledness.** The asker acknowledges an answer only
  when its value is settled, so a contradicted answer is never grounded.

**Strategies rejected, and why:**

- **A turn-taking controller** (`Flows.Turn`). Not needed: obligations already
  order the exchange, and a controller would add state with no work to do.
- **Closure to choose the clarifying question.** Closure is host planning.
  With one input to the answer, the clarification is wired in advance; choosing
  among several open inputs is the open case below.
- **A gap relation over open values.** Not needed here: "is the answer
  settled" is a tally of its supports, which works on open values directly.
- **Messages as one field of the speaker's position.** Refused by the
  one-taker rule, correctly: see slots above.
- **Narrowing from the speaker's words without waiting.** It would narrow by
  whatever the speaker's planes held before speaking.

**Open:**

- **Choosing among several clarifications.** With more than one open input
  ("which car?" and "when?"), the narrowest question is the one whose answer
  settles the most. That is a ranking over candidate questions, the first
  place ranking is needed as structure here.
- **Contradiction stays unresolved.** Firmness (as in the shop's books) could
  retire the weaker answer; the dialogue leaves it visible instead.
- **One question at a time.** A speaker's ledger holds one question; several
  open questions at once need slots allocated as they are asked.

## Sources

Recalled from reading, except where checked for this note (marked *):

- *Horn, L. R. (1984). A new taxonomy for pragmatic inference: Q-based and
  R-based implicature. In Schiffrin (ed.), *Meaning, Form, and Use in
  Context*. [Q-principle](https://en.wikipedia.org/wiki/Q-principle),
  [R-principle](https://en.wikipedia.org/wiki/R-principle).
- Zipf, G. K. (1949). *Human Behavior and the Principle of Least Effort*.
- Grice, H. P. (1975). Logic and conversation.
- *Clark, H. H. and Brennan, S. E. (1991). Grounding in communication.
  [PDF](https://web.stanford.edu/%7Eclark/1990s/Clark,%20H.H.%20_%20Brennan,%20S.E.%20_Grounding%20in%20communication_%201991.pdf).
- Clark, H. H. and Wilkes-Gibbs, D. (1986). Referring as a collaborative process.
- Lewis, D. (1979). Scorekeeping in a language game.
- *Hobbs, J. R. (1985). Granularity. IJCAI-85.
  [Proceedings](https://ijcai.org/proceedings/1985/84).
- *Prince, A. and Smolensky, P. (1993/2004). *Optimality Theory*; Legendre,
  Miyata and Smolensky (1990), Harmonic Grammar.
  [Pater, weighted constraints](https://roa.rutgers.edu/files/924-0907/924-PATER-0-0.PDF).
- *Zadeh, L. A. (1978); Dubois, D. and Prade, H. (1988). *Possibility Theory*.
  [Overview](https://en.wikipedia.org/wiki/Possibility_theory).
- Rissanen, J. (1978). Modeling by shortest data description.
- *Ashby, W. R. (1952). *Design for a Brain*.
  [Archive copy](https://ia800906.us.archive.org/32/items/designforbrain00ashb/designforbrain00ashb_bw.pdf).
- Piaget, J. (1975). *The Equilibration of Cognitive Structures*.
- Festinger, L. (1957). *A Theory of Cognitive Dissonance*.
- Björner, A., Lovász, L. and Shor, P. (1991). Chip-firing games on graphs.
- Bak, P., Tang, C. and Wiesenfeld, K. (1987). Self-organized criticality.
- Sacks, H., Schegloff, E. A. and Jefferson, G. (1974). A simplest systematics
  for the organization of turn-taking for conversation.
- Petri, C. A. (1962); Murata, T. (1989). Petri nets: properties, analysis
  and applications.
- Selman, B., Kautz, H. and Cohen, B. (1994). Noise strategies for improving
  local search (WalkSAT).
