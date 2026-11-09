# Three Honest Bits: a podcast script

Status: **script**, 9 November 2026. It is a spoken walkthrough of the
[encoding consolidation note](GojoEncodingConsolidation-2026-11.md) and the
[Workspace README](../Workspace/README.md). It makes no claim of its own. Where
it differs from those documents, they win.

Single narrator, roughly 45 minutes at a conversational pace. It splits into
two episodes at the break after Part 5. Lines in square brackets are production
cues. The `show notes` blocks are for the episode page, not for reading aloud;
the spoken text says the same thing in words.

---

## Cold open

Imagine a sensor that can tell something is there, but has no ruler.

It can't tell you how much. It can tell you three things. Which way it calls
positive. Whether the input points with that, or against it. And whether there
is anything it can detect at all.

That's three bits.

Give it two inputs, both pointing the same way, both detectable, and ask for
the sum. It knows the answer: positive, and detectable. Now turn one input
around and ask again. This time it can't pick. Positive, negative and zero are
all possible, because it doesn't know which input was bigger. But it can tell
you exactly which answers fit. It doesn't make one up.

This episode is about what happens when you take that attitude, *say exactly
what fits and never invent the rest*, and build a whole computing substrate
out of it. One where every bit sits at an address that tells you what it
means. Where computing means settling tension. And where you can inspect the
whole thing, all the way down, whether it's running on a CPU, a GPU or an
FPGA.

[theme in, then under]

## Introduction

The project is called Gojo. Over the past few weeks it has run a long series
of small bit-level encoding experiments: three-bit words, passages that move
items through gates, families of shared templates, gap stores, pressure routes
that learn which way to push. Each one worked, and each one invented its own
layout. This episode is about consolidating them into one structure, and then
walking up that structure one level at a time.

First, an honest label. The lowest levels exist as tested experiments. The
substrate that ties them together is a proposal. I'll say which is which as we
go.

At every level I'll ask the same three questions. What does a bit mean here?
Where does the unknown go? And what would a chip do with it?

Here's the climb. One word. Many words at once. Rules written as tables. What
isn't known. Who is asking. Time. What pushes things, which is tension. Resolution.
Where the rules come from, which is learning. And finally concepts, metaphor and
language. Then we'll come back down and talk about what exists, what's missing,
and what comes next.

[theme out]

## Part 1: One word

The word has three bits. Call them O, D and V, written from high to low.

V is the lowest bit. Zero means "no magnitude I can detect at this
resolution." One means "something detectable." And here is the first thing
that trips people up. V equals one does not mean the number one. It means
*some*. There's no ruler, remember.

D is the middle bit, the direction. Zero is positive, one is negative, within
the current orientation.

O is the top bit, the orientation: which way this reader calls positive. Zero
is positive-to-the-right, one is positive-to-the-left.

So the word zero-zero-one reads: positive-right frame, positive direction,
something detectable. Something, pointing right. The word zero-one-one is
something pointing left, measured in that same frame.

Two operations look alike but aren't. *Reversing* flips D alone. Zero-zero-one
becomes zero-one-one. The thing now points the other way. *Reorienting* flips
O and D together. Zero-zero-one becomes one-one-one. Nothing moved; the reader
turned around. What was "positive, in a frame where right is positive" is now
described as "negative, in a frame where left is positive." Same thing,
different description. Keeping
those two apart, moving the thing versus moving the observer, matters at every
level we're about to climb.

Notice that zero has a direction too. Zero-zero-zero and zero-one-zero are
positive zero and negative zero. As magnitudes they compare equal, but the
direction is kept. That isn't waste. Later it becomes direction memory: which
way something was going when it went quiet.

Now addition. Within one orientation, positive-something plus
positive-something is positive-something. The read can't see that the sum got
bigger. It has no ruler. Positive-something plus negative-something gives four
possible answers: positive-something, negative-something, positive zero and
negative zero. The function doesn't return a guess. It returns the set.

Subtraction holds a lesson. Something minus something that *looks* the same
does not cancel. Two words that read the same aren't known to be the same
amount; the word forgot the information that would justify cancelling. If you
want X minus X to be zero, you have to know it's the *same* X. That's a fact
about references, not values. Hold on to it. It comes back several times.

```text
show notes
  O D V      O: 0 positive-right, 1 positive-left
             D: 0 positive, 1 negative (within the orientation)
             V: 0 not detectable at this resolution, 1 detectable
  reverse    flip D        001 -> 011
  reorient   flip O and D  001 -> 111
  (+?) + (−?) = {+?, −?, +0, −0}
  all eight words, packed low bit first at offsets 0, 3, 6 ... = 88 C6 FA
```

Packing is plain. All eight possible words, packed three bits apiece, fill
exactly three bytes: eight-eight, C-six, F-A. No header, no metadata per word.
Position supplies the order.

The most important decision at this level is something that *isn't* there.
There is no ninth word meaning "unknown." Uncertainty is never squeezed into
the value. It lives beside it, as a set of words that are still possible.
Everything we build later follows from that one refusal.

One refinement came recently: what exactly V equals zero means. It means "not
detectable at this resolution," not "exactly nothing." The first experiment
happened to choose exact zero as its resolution, so its tables are exact. But
under a coarser detector, two undetectable positive inputs can add up to
something detectable. Think of two whispers. So positive zero plus positive
zero widens to "positive zero, or positive-something." Any rule that treats a
zero as "adds nothing" has to say which kind of zero it means.

And here's what V equals zero does *not* mean: "not taking part." Whether a
word participates in a computation isn't stored in the word at all. We'll get
there in Part 5.

Status: implemented as a tested experiment.

[short bridge]

One word is a toy. The first step up isn't a bigger word. It's many words at
once, and the trick is to turn the layout on its side.

## Part 2: Planes

Take sixty-four sites. Instead of storing sixty-four three-bit words one after
another, store all sixty-four V bits in one 64-bit integer, and all sixty-four
D bits in another. Bit *i* of the V plane is site *i*'s V. This is called
bit-slicing; it's an old trick from fast cryptography. Now one machine
instruction touches sixty-four sites at once.

And O? In most structures, orientation belongs to the frame, not to each
site. So it isn't stored. The tile's basis supplies it. That's a general rule:
a plane that would be constant across a tile isn't stored at all. The layout
supplies it.

That brings us to the central rule of the whole design. I call it the
address-meaning invariant. Every stored bit has an address: which tile, which
plane, which site. That address, read through the tile's declared layout and
its laws, determines what the bit means. No bit's meaning depends on a host
object, a variable name, or the value of some other bit, except through
connections that are themselves declared. Even padding is declared padding.

The consequence is that you can write one universal decoder that prints the
meaning of every bit in any buffer. That's what "inspectable" means here. It's
not a debugger bolted on afterwards. It's a contract every layout has to
satisfy.

Position also carries constraint. If an entry can only ever be positive, it
doesn't get a sign bit. There's nothing there to corrupt. Releasing that
constraint is a structural move, adding a sign plane, not a value write. It's a
type system written into the address space.

Operations become operations on whole planes. Reorient a tile: flip O in the
basis, and XOR the D plane with all ones. Reverse a chosen subset of sites:
XOR the D plane with that subset's mask. One instruction each, for sixty-four
sites.

```text
show notes
  plane V: one u64, bit i = site i's V      plane D: likewise
  reorient tile        O ^= 1;  D ^= ALL
  reverse subset m     D ^= m
  source planes        V detectable, D sign, F flow face, M[k] magnitude,
                       T+ / T− tension, H history
```

The proposal names a small set of planes that hold what a structure *is*:
detectable, sign, flow face, magnitude, positive and negative tension, and
history. I'll introduce each one as we reach it.

What about neighbors? The best case is implicit topology. A chain, a ring or a
grid is defined by index, so a neighbor is just a shift, and nothing is stored
for it. One experiment ran a cycle of more than sixteen thousand steps that
read zero stored connection rows. The guarantee came entirely from position.
When implicit topology isn't enough, a tile stores explicit local rows,
compact lists of references. When structure crosses tiles, it goes through
ports, which we'll meet later.

Status: positional storage and stable ports exist as experiments. The tile
layout itself is proposed.

[short bridge]

So now we have sixty-four values sitting in a couple of integers. Something has
to tell them how to change. In most systems that's code. Here it's a table, and
the table is data.

## Part 3: Laws

The smallest interesting law is a passage. Three sites. A source, S. A
control, C, which is a gate. A target, T. Each one is one bit: occupied or
empty, and for the gate, open or closed. Pack them as S plus two C plus four T,
and every state of the passage is a number from zero to seven.

The law answers one question: which before-state is allowed to become which
after-state? Eight befores times eight afters is sixty-four yes-or-no answers.
Sixty-four bits. Eight bytes. The bit for a given pair sits at
*before plus eight times after*.

Here are the eight bytes: zero-one, zero-two, zero-four, zero-zero, one-zero,
two-zero, four-eight, eight-zero.

Read them one byte at a time. Each byte is an after-state. Each bit set inside
it is a before-state that's allowed to land there.

Bytes zero, one and two each admit only themselves. An empty source stays
empty. A source behind a closed gate keeps its item. An open gate with nothing
to pass stays as it is.

Byte three is zero. After-state three means "item at the source, gate open,
target empty." Nothing is allowed to end up there. It isn't a stable state:
the item always moves.

Byte six is four-eight, which is bits three and six. After-state six means
"gate open, item at the target." You can reach it from state three, which is
the handoff, or from state six, which had already moved.

Bytes four, five and seven: an occupied target stays put. No merging two items
into one.

When this law loads, a checker runs once over all sixty-four bits. It confirms
that the total number of items across source and target is preserved, and that
the gate never changes. A table that creates, discards, reverses or duplicates
an item is rejected. That check is the law's accounting signature.

Now the hardware. A function of six input bits is exactly what an FPGA's
six-input lookup table computes. Its sixty-four-bit initialization value is
these same eight bytes, read as one hexadecimal number. The law isn't compiled
into logic. It *is* the logic.

On a CPU, with planes, sixty-four passages step at once. The new S is S AND
(NOT C, OR T). The new T is T OR (S AND C). C is unchanged. That's three
operations. The accounting falls out for free: what was offered is S AND C;
what was blocked is the offered set AND T; what was accepted is the offered set
AND NOT T.

```text
show notes
  state code      S + 2C + 4T
  law bit         before + 8 * after
  law bytes       01 02 04 00 10 20 48 80
  FPGA LUT6       INIT = 64'h8048201000040201
  64 passages     S' = S & (~C | T);  T' = T | (S & C);  C' = C
  accounting      offered = S & C;  blocked = offered & T;
                  accepted = offered & ~T
```

Generalize that, and there are three kinds of law table. Relation tables say
which combinations are admitted; they only observe and never change anything.
Transition tables say which before-and-after pairs are admitted; they change
state, including moving material. Rung maps connect coarse and fine views;
they're Part 8.

Every law carries three things. An accounting signature, checked
exhaustively when it loads. A confluence class, which says what the law means
when it runs in parallel; that's Part 6. And a content hash. The hash
identifies the *table*, not the concept. The same table attached to different
planes means different things.

The key decision here is to keep topology separate from rule. Which sites are
connected is one thing. Which combinations are allowed is another. The
executor reads bits, not names, and small laws can be checked completely,
every row, before they ever run.

Status: the passage law, relation bitmaps and family templates exist as
experiments. The native finite-law compiler, which turns connected laws into
compact masks, ran about 196 times faster on a recurrent circuit. That was on
one thread, without retaining a witness for every read.

[bridge, a little longer]

So far everything has been known. The gate was open or it was closed. Most of
the interesting world isn't like that. This next level is where the design
spends most of its care: everything that *isn't* signal.

## Part 4: What isn't known

The basic tool is a support set. For each site and each value *v* that site
could take, keep a plane called S of *v*. A set bit means "*v* is still
possible here."

Count the set bits for one site and you learn its status. One means settled.
More than one means competing candidates. Zero means contradiction: no value
fits. A contradiction is recorded against the laws that emptied the set, so
you can see why. None of these is a probability. The word is "possible," not
"likely."

Addition works directly on support planes. Give each operand four planes:
positive zero, positive-something, negative zero, negative-something. About
twenty bitwise operations then perform sixty-four additions at once, with the
full possibility semantics of the original table. An empty operand gives an
empty result. Nothing is invented.

Prediction works the same way. Take the passage again, but this time we don't
know whether the gate is open. The experiment reads the source and the target
and deliberately leaves the gate unobserved, through a visibility mask: one,
zero, one. The code doesn't even load the omitted bit. Now keep eight planes
per passage, J zero through J seven, one for each joint state. Bit *i* of J
three means "passage *i* might be in state three."

Predicting the next step takes two operations. OR J three into J six, then
clear J three. Everything that might be "ready to move" becomes "moved."
Sixty-four passages, two instructions. If the possible states before were one
and three, gate closed or gate open, the possible states after are one and
six: the item stayed, or the item moved.

```text
show notes
  support         S[v] plane: bit i = "value v still possible at site i"
  popcount        1 settled, >1 candidates, 0 contradiction (+ residual)
  joint planes    J[0..7], indexed by S + 2C + 4T
  predict         J[6] |= J[3];  J[3] = 0
  example         before {1, 3} (0x0A)  ->  after {1, 6} (0x42)
```

Now the hard part: correlation. Chain two passages. A feeds B, B feeds T. The
first gate is unknown; the second is open. If you track each site separately,
then after both steps you have: A, maybe occupied. B, empty. T, maybe
occupied. Multiply those out and you get four combinations. Two of them are
impossible. Both empty means the item was lost. Both full means it was
duplicated. Tracking sites separately forgot that A and T were linked.

There are three remedies, and the design keeps all of them.

One: track the whole chain jointly, gates included, as a cluster. That's five
sites and thirty-two joint states. It's exact, but joint tracking grows
exponentially with the number of sites.

Two: use conservation. The passage law's accounting signature already says
there is exactly one item among A, B and T. That single counting constraint
removes both impossible states, at nearly the cost of tracking sites
separately. So conservation does double duty. It validates laws, and it
cheaply restores exactly the correlations that conservation implies.

Three: if you use neither, record that correlation was dropped, in the
witness. Never present the product of marginals as the answer.

That gives an uncertainty ladder. U zero tracks each site alone. U one tracks
each law's sites jointly. U two merges laws into exact clusters. U three is U
one plus conservation constraints.

One more rule: the epoch rule. Within an epoch, supports only narrow. If
evidence says the source itself *changed*, that starts a new epoch, in which
supports may widen again; the old epoch stays as history. Narrowing only ever
removes possibilities, and that is the property that matters for hardware: the
result doesn't depend on the order you apply the narrowing. Any fair order
reaches the same fixed point. Order-free means safe to run in parallel.

Step back and look at all the non-signal kinds together. Each one gets its own
location, so no bit can be ambiguous between them. An open slot is structure,
a row pointing at a placeholder, and not a zero. A contradiction is not a
measured zero. Resolution loss is not uncertainty about the source. Blocked is
not destroyed. Stale, meaning a plan captured before an edit, is not wrong, only
unverified. And tension is not confidence.

How do you rank alternatives without inventing a confidence number? You read
the same rows under a different algebra. Under the Boolean algebra, you ask "is
it possible?" Under counting, "how many complete solutions are there?" Under
the tropical algebra, which is minimum and plus, "what's the least-tension
completion?" Under the bottleneck algebra, "how much capacity gets through?"
Probability is allowed only when a justified probability law is actually
connected. Everything else stays in integers: exact and inspectable.

Status: support masks, joint masks and visibility masks exist in experiments.
The uncertainty ladder and the alternative algebras are proposals.

[bridge]

There's a question hiding in all of this. *Who is asking?* A support set isn't
a fact about the world. It's a fact about somebody's view of the world. Two
observers can hold different supports for the very same site. That question
led to the idea that reshaped the design.

## Part 5: Selection and workspaces

An earlier draft of this design added a bit to every site meaning "taking
part." The better answer turned out to be simpler. Participation isn't a
property of the value at all. It's *selection*: a read held by whoever is
using the value.

That gives four situations, and it's worth saying them slowly.

Not selected: no claim. Nothing about the value, nothing even about whether it
exists.

Selected, but nothing supplied: the question is open. Don't manufacture a zero.

Selected, and V is zero: nothing detectable at this resolution, but it's still
an operand. It still takes part.

Selected, and V is one: something detectable. Still not "one."

Inside a single view, something unselected and something nonexistent read
exactly the same. Telling them apart takes another look, or a *closed*
selection: one declared to cover everything in a scope. "All of these," as
opposed to just "these." Only a closed selection can support the claim "there
is no X here," and only inside its scope.

Here's an example. Six equal markers sit around a contour. Select them as one,
zero, one, one, zero, one, dropping an opposite pair, and read the rest in
contour order. You get a rectangle. No marker changed. Another selection can
still use all six. And the rectangle appears only because the contour divides
evenly. Drop an arbitrary pair, and there's no guarantee.

A selection is structure too, made by small machines. Every second element: AND
the scope with the alternating pattern, zero-one-zero-one all the way across.
Everything within *k* connections: start from seed sites F, and *k* times,
replace F with F OR its neighbors. On a chain, the neighbors are just F
shifted left, OR F shifted right. Everything in an area: a predicate plane in a
chosen frame. A mask counts as structure only while it stays connected to its
scope and to the machine that made it. A detached mask is just a bag of
indexes.

```text
show notes
  every second element   Sel = 0x5555555555555555 & scope
  k connections deep     repeat k:  F = F | N(F)
                         chain:     N(F) = (F << 1) | (F >> 1)
  commit admissible      (accountingScope & ~Sel) == 0
  provenance K (2 bits)  referenced live | referenced captured |
                         introduced | diverged (copy on write)
```

This leads to what I think is the most primitive kind of execution. Growing a
selection outward from seeds only ever adds. It's a union, it reaches a fixed
point, and the order doesn't matter. Narrowing what's possible inside that
selection only ever removes. It's an intersection, it also reaches a fixed
point, and again the order doesn't matter. Those are two order-free kinds of
execution. They're safe on any parallel hardware without locks. Moving
material comes after both, and it's the first step that needs accounting. So
the most primitive way to run a structure is to select over structure.

Where do selections live? In a workspace. And a workspace is just another
tile. Its sites are either references to source sites, held as a gather map,
or sites it introduced itself. A two-bit provenance plane, K, labels each one.
Referenced live: reads the source as it is now. Referenced captured: a snapshot
taken at a recorded epoch. Introduced: created here, like a proposed edge that
would close an open shape. Diverged: started as a reference, then changed
locally, with its link back to the source kept. That last one is copy on write.
Nothing has to be copied up front.

So here's the rule the whole design rests on. The source holds what *is*:
structure, value at its resolution, and tension. The workspace holds what is
*noticed, supposed or attempted*: selection, supplied evidence, support,
predictions and candidates.

Looking and acting are different. A workspace can observe through any
selection it likes. Looking away from a flowing passage doesn't stop it. But
it can commit a material change only if its selection covers the law's whole
accounting scope. In bits: take the accounting scope, AND it with the inverse
of the selection, and the result must be zero. That's one instruction per
sixty-four sites. Partial attention can never lose or duplicate material.

Writing back is careful too. A workspace records the epochs it read. A proposed
change commits only if those epochs still match. If the source changed in the
meantime, the proposal stays a proposal, and the mismatch stays visible as
residue. It never overwrites the newer state. Databases call this optimistic
concurrency control.

Picture a house drawn as a roof on top of a body. The roof and the body share
one edge. The roof reads that edge left to right; the body reads it right to
left. Neither one reverses the edge. Neither one copies it. They're two
sibling workspaces over one source, like two concurrent transactions. Add a
landmark to that edge in the source, and both of them see it.

There's one guard I'd insist on. Say a workspace closes an open stroke by
introducing an edge, and the computation succeeds. That shows the closure was
*useful*. It doesn't show the source was ever closed. The credit goes to the
rule that proposed the closure. Adopting it into the source has to be an
explicit edit, labelled "introduced by this rule, adopted after this many
uses," and never "observed." Often it's better still to keep the open source
beside the closed reading. Two constructions that agree today can respond
differently to the next input.

Prediction is simply a workspace that never commits. It has no commit path, so
by construction it cannot move material. A forecast records its epoch, its
horizon and what it assumed. When the outcome arrives, the mismatch becomes a
residual, and that residual feeds learning.

Workspaces form a graph, not a ladder. Siblings, candidates, trials,
predictions, metaphors and coarser views are all workspaces with different
promises. A workspace can even select from another workspace.

And physically, this is how hardware already works. Registers, GPU shared
memory and scratch space in cache are all workspaces: gather what you need,
compute locally, commit with a check, which on a GPU is a compare-and-swap on
an epoch. A workspace made of masks and references costs a few words per
sixty-four sites, so most of them can be thrown away after use.

Status: this is documented direction. There's no workspace code yet, but there
are footholds: the house experiment's shared seam, a focus surface in the core
structures, a private preview workspace for construction machines, and a trial
machine that acts only after an explicit grant. The first planned test is the
house.

[music bridge, episode break if splitting]

## Part 6: The tick

We now have values, laws, uncertainty and points of view. What we don't have
is time. What does "one step" mean when thousands of these run in parallel, on
hardware that has no single global order?

The proposal is a tick with phases. Ingest: tension arrives from ports.
Observe: evidence is supplied, possibly starting a new epoch. Propagate:
supports narrow to a fixed point. Then offer, accept and return, where material
actually moves. Then account. And on slower clocks, adapt, and restructure.

Offer, accept and return are double-buffered. Every site writes its offers
into one buffer. Each receiver decides what to accept, writing only its own
planes. Then the return phase computes what was blocked and what remains. No
two writers touch the same bit in the same phase, so it's race-free. And one
identity is checked every time: offered equals accepted plus blocked plus
remainder. A violation is a bug, not a tradeoff.

Each law declares a confluence class, which says what it means in parallel.

Class one is monotone narrowing: any fair order gives the same answer.

Class two is self-local transfer: a site acts only on its own state, like
grains toppling in a sandpile, and when that system has the right property,
the final state doesn't depend on order either.

Class three is descent with a schedule. Balancing gaps between neighbors is
an example. Here the order *can* matter, so the rotation or priority is stored
as state, and it's part of the record.

Class four is driven or periodic: clocks and repeating cycles. These need an
explicit controller and evidence for when to stop.

That's what gives parallel hardware a defined meaning. Either the order doesn't
matter, or the order is state you can replay.

There are three clocks. Settle is fast. Adapt is medium. Restructure is slow.

One more piece: witnesses, the records of why a bit is what it is. Storing a
witness for every read is expensive. The 196-times speedup came partly from not
doing that. So the proposal is replay. Keep checkpoints, the ingest log, the
law hashes and the schedule state. Any witness can then be regenerated on
demand. Forgetting means dropping checkpoints past a horizon. Witnesses from
before that point become explicitly unrecoverable. They're open, not invented.

```text
show notes
  phases     ingest -> observe -> propagate -> offer -> accept -> return
             -> account -> (adapt, every k) -> (restructure, every m)
  identity   offered = accepted + blocked + remainder
  classes    C1 monotone narrowing    C2 self-local transfer
             C3 descent + schedule    C4 driven / periodic
```

[bridge]

A tick moves things. But toward what? Something has to say which states are
better than others. That's tension.

## Part 7: Tension and energy

Today Gojo has three different notions of tension. The core carriers hold a
signed integer that gets absorbed into value. The pressure-routing experiments
send unit pulses with capacities and acknowledgements. And the compact bit
experiments have none at all; a step simply holds or it doesn't. The proposal
makes them one thing.

Site tension lives in two planes, T plus and T minus: positive and negative
participation that hasn't been accounted for yet. They're never netted against
each other automatically. A site holding plus three and minus three is not the
same as a site holding nothing. The opposition is kept beneath a zero net.

Each law has a residual: how many of its tuples are violated, or, for a goal,
the signed mismatch in its own units.

Energy is read in tiers, compared strictly in order. Hard: accounting
violations, which must always be zero. Obligations: constructions and claims.
Goals: relations we want. Preferences: costs and aesthetics. A lower tier can
never buy its way out of a higher one.

A move is admissible if it doesn't raise energy, and either strictly lowers it
or is a declared neutral transfer. Finitely many states plus strict descent
means settling always terminates.

Here's a concrete case: balancing gaps. Each gap is stored in thermometer
code: bit *k* is set when the gap is bigger than *k*. So a gap of three is
zero-one-one-one. An exchange from gap *a* to gap *b* is allowed when *a*
exceeds *b* by at least two. That's two bit tests. Is bit one of *a* set,
meaning *a* is at least two? And do all of *b*'s bits fit inside *a* shifted
right by two, meaning *b* is at most *a* minus two? The move itself: shift *a*
right by one, and shift *b* left by one and set its lowest bit. Each move
lowers the sum of the squared gaps by at least two, so it has to stop.

```text
show notes
  thermometer   bit k set  <=>  gap > k          3 = 0b0111
  admissible    (t_a & 0b10) != 0  and  (t_b & ~(t_a >> 2)) == 0
  move          t_a = t_a >> 1;   t_b = (t_b << 1) | 1
  descent       each move lowers sum of g^2 by 2(g_a − g_b) − 2 >= 2
```

Now put three gaps on a path: one, two, three. Neighbors differ by only one,
so no move is admissible. The system is quiet. But the goal, equal gaps, isn't
met; the spread is two. That's a legitimate output. *Quiet is not optimal.*
The record names why: there's no route between the first gap and the last. Add
that route, and three can give to one, reaching two, two, two.

When a system is quiet but unhappy, there are three visible escapes. Widen the
scope by connecting a route. Take a tension loan: a budgeted, recorded,
temporary increase, like the random steps in local search, but accounted for.
Or propose a structural edit on the slow clock. Which tension goes to which
route is learned, through route strengths. There's no global optimizer looking
down from above.

Status: pressure routing with learned route strengths from zero to four, and
gap settlement with shifted slot comparisons, both exist as experiments. The
unified tension planes and the energy tiers are proposals.

[bridge]

Everything so far lives at one resolution. Real structure doesn't. You see the
outline before the detail, the hundreds before the ones. So the next step up
is resolution itself.

## Part 8: Rungs

The design uses a short ladder of codes, each making a bigger promise than the
last.

Rung zero is one bit: detectable or not. That's V.

Rung one is a thermometer, or unary, code. K bits, and bit *k* means "more than
*k*." Adding one is shifting a bit in. Subtracting one is shifting a bit out.
Comparing is a mask test. It's monotone and physical: more stuff, more bits.

Rung two is ordinary place value: K bits giving two-to-the-K levels. But only
under a declared weighting, and only when it pays. It's never assumed just
because a field happens to be wide.

Rung three is rational and affine arithmetic: the kernel's calibrated numbers.

Between rungs there's a pair of maps. One goes down in detail: from fine to
coarse. Thermometer to presence, for example, just asks "is it nonzero?" The
other goes back up, but it can't recover detail. It gives the *set* of fine
values compatible with the coarse one. "Present" means "somewhere from one to
K." Mathematicians call this pair a Galois connection. The soundness rule is
that a finer read never contradicts a valid coarser one. An exact rung keeps
residue planes, so the detail can be rebuilt. A lossy rung keeps loss-support
planes instead, and promises no reconstruction.

Here's the distinction in miniature. "Hundreds equals three" is a coarse
witness: about three hundred, with the lower detail outside the boundary.
"Hundreds three, tens zero, ones zero" is exact, because the lower slots took
part. Those two zeros were selected and supplied. They're operands, not
absences. That's Part 1 and Part 5 meeting.

Pooling is how coarse rungs are built. Coarse presence is the OR of the
children. A coarse count is their sum, with the carry kept as residue. These
are the same reductions GPUs already perform when they build mipmaps.

And this gives the hierarchy a predictive shape. A coarse rung predicts the
fine supports, through the upward map. Fine evidence sends residuals back up,
through the downward map. In workspace terms, a rung is just one more relation
between workspaces. Too much residue says "import finer resolution." Unused
detail says "collapse to something coarser."

Status: rungs zero and one exist, as the word's V bit and the gap stores. Rung
three is the kernel. Rung two is documented direction, and the rung maps are a
proposal.

[bridge, slower]

Now the system can hold structure at many resolutions, and settle it. The last
climb is the one machine learning actually cares about. Where do the laws come
from? Everything so far, somebody wrote.

## Part 9: Learning

There's a ladder here too.

Level zero: fixed laws. Settlement only.

Level one: route counters. Each route has a small saturating counter, zero to
four, raised or lowered by the relief it actually delivered and that the
receiver acknowledged. It's a counter on a named route, so you can always see
why the system prefers it. This exists.

Level two: eligibility traces. A decaying bit on every route that recently
carried a unit, so that credit arriving later can still find the routes that
earned it.

Level three: learned law tables. Keep two tables over the same tuple index.
Table A, admitted: this transition was witnessed at least some threshold number
of times. Table U, unsupported: this before-state was seen many times and never
went to this after-state. Everything that's in neither is *unknown*: NOT A AND
NOT U. A prediction through an unknown row gives an open result, not a guess.
And every admitted bit can be traced back to the transitions that admitted it.
Laws get the same non-signal structure that values have.

```text
show notes
  A[t]  admitted      tuple t witnessed >= θ times
  U[t]  unsupported   before-state seen n times, this after never
  unknown = ~A & ~U   -> prediction through it is open, not guessed
```

Level four: structural edits drawn from a declared grammar of edits, kept only
if they help on held-out cases. One experiment already earns a timing socket
this way.

Level five: crystallization, when it pays in bits. If a template plus its
instances plus whatever residue is left costs fewer bits than the raw
structure, and held-out predictions still hold, keep the template. Because the
encoding is physical, description length isn't a metaphor here. You can count
it. In one experiment, four uses of one shared field cost five payload bits,
against twenty for four independent fields.

Levels six and seven are analogy and language, which is the next part.

Two cautions run through all of this. First, the self-confirmation guard from
Part 5 applies at every hardening step. Something that made a computation work
isn't evidence about the world. Second, authored, selected and learned
structure must keep separate provenance. The historical failure of symbolic
AI was hand-built structure that didn't scale. Writing more machines by hand
won't settle the question. The decisive work is levels three to five:
acquiring laws and motifs from data.

[bridge]

Which brings us to the top of the ladder, and the most speculative part.

## Part 10: Concepts, metaphor and language

A concept, in this design, is a crystallized motif. It's a template of rows,
laws and ports, stored once, whose instances are packed variable fields. That's
exactly what the family-table experiments already do for small templates.

A metaphor is a workspace. Select a source structure at low resolution, map it
into a target, and let it fit or fail. The mapping pairs rows with rows and
laws with laws, and it prefers large connected mappings over scattered
matches. Psychologists call that systematicity. Where the fit stops, there's
residue, and that residue isn't an error. It's information about where the
metaphor ends. Take the rhythm of turn-taking in one setting, map it onto a
conversation, and the residue tells you exactly where conversation behaves
differently.

Language uses the same parts. Words reach motifs through hint tokens. A parse
is a workspace: a sequence of selections and bindings over ports. Ambiguity is
support. Two readings stay possible, side by side, retained rather than
averaged. Meaning is the settled scene. Predicting the next word is a forward
read in a prediction workspace, and surprise is that prediction's residual.

Take a phrase like "red trace near the pin." That's three selections over a
scene, a binding between them, and possibly an unresolved slot if there are two
pins. No new machinery. It's the same support, selection, tension and residue
we built in Parts 1 through 5.

Status: small fixtures only. This part is a direction, not a result.

## Part 11: What a chip does with it

Here's the third question, answered once for the whole stack.

On a CPU, a plane is one 64-bit word, and vector units handle four to eight
tiles per instruction. Small laws become ANDs and ORs. Population count reads
how many sites are set.

On a GPU, a warp's vote instruction builds a plane from thirty-two lanes in
one step. Shared memory is the workspace. Commit is a compare-and-swap on an
epoch. Offer and accept become two phases separated by a barrier.

On an FPGA, planes are registers. Small laws are lookup tables; the passage law
is one. Implicit topology is literally wires. Tension can be a thermometer code
or a train of pulses. The tropical "least-cost" read can use race logic, where
the first signal to arrive *is* the minimum. Learning counters look like
branch-predictor counters. And inspection is a scan chain.

On measurement, one honest note. The 196-times speedup came from compact
tables on one thread, with no vector instructions and no bit-slicing across
cells. Bit-slicing is the obvious next multiplier, but it hasn't been measured,
so I'm not quoting a number.

## Part 12: Where it stands

Let's come back down the ladder.

What exists, as tested experiments: the three-bit word and its arithmetic,
packing, passages, relation bitmaps, family templates, gap stores, pressure
routing with learned route strengths, native finite laws with compiled plans,
and the house experiment with its shared seam.

What's proposed: the single substrate, the workspace contract, the tick, the
unified account of tension, the full rung ladder, and everything in learning
above route counters.

The gaps, roughly in order. The biggest is acquisition: today the landmarks,
frames, seams and families are written by hand, and learning them from
drawings, signals or experience is the central open problem. Then joint
uncertainty that scales. Delayed credit. Front-ends that turn strokes, images
or signals into planes. And parallel back-ends beyond a single thread.

What's next is deliberately small. Step one is a substrate fixture that
re-expresses the word arithmetic, the passage chain and gap balancing in the
shared layout, matches the existing experiments bit for bit, and has the
universal decoder print a meaning for every stored bit. Step two is the house:
two workspaces reading one seam in opposite directions, an introduced closure
that never counts as evidence, a stale commit that fails cleanly and leaves
residue, and a commit that's refused because its selection missed part of an
accounting scope.

## Close

Let's go back to the sensor with no ruler.

It couldn't measure anything. What it could do was say exactly which answers
fit, and refuse to invent the rest. Every level we climbed kept that refusal.
The unknown got its own planes. Points of view got their own workspaces.
Conservation got checked when a law loads. Quiet was allowed to be imperfect,
as long as it said why. And every bit kept an address that tells you what it
means.

The bet is that this kind of honesty scales: that you can build up from three
bits to concepts and language without ever reaching a point where you have to
stop looking inside.

Thanks for listening.

[theme out]
