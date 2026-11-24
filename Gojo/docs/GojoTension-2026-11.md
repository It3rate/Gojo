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

These reads are already available (`Supports.Open`, `Supports.Empty`,
`Question.Remaining`). What is missing is a law that writes them where other
laws can read them: a small relation from a value's support planes to a
two-bit code (settled, open, empty) per position.

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
reoriented one, as a debt is a credit seen from the other side. If that holds,
reorienting a site swaps `T+` and `T−`, a zero-copy view like `Flipped`. This
is a hypothesis to test with the work on radial and linear direction; it
would tie tension's two faces to ODV's D bit.

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

## 8. Proposed first steps in EncodingC

1. **Material tension.**
   - Make `T+` and `T−` unary fields at a site.
   - Make blocked entry write `T+`.
   - Add absorb and draw transition laws that conserve tension plus value.
   - Tests:
     - units arriving at a full site wait as `T+` and are absorbed when room
       frees;
     - a draw with no supply stays as `T−`;
     - the two are never netted unless a law connects them;
     - the conservation promise holds.
2. **The two-faced gap,** as planned in `Gap.cs`: a relation over (have, want,
   deficit, excess), read in a goal's workspace.
3. **Information tension as a law:** support planes to a settled/open/empty
   code per demanded position, so laws can read where questions are unanswered
   or contradicted.
4. **The tension tuple:** a read over all tiers of a structure, and an
   admissibility check on a step.
5. **Structural reads** from evidence (idle, duplicate, joined, persistent,
   contested) and description length, as the inputs for splitting and merging.
6. **Explicit route choice:** the taker as an open value, so a contest is
   narrowing and a turn-taking gate is an ordinary law.

The radial and linear work is a separate thread, but step 1 should keep the
face of each tension explicit, so the orientation hypothesis in §4 can be
tested there.

## 9. Questions for the axioms

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
