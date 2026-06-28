# Gojo Grammar Reads

This note sketches a grammar layer for Gojo structures. The goal is not to turn
Gojo into English, or to add linguistic tags to the kernel. The goal is to make
loose, partial, contextual structure computable in the same way language does:
through local order, selection, basis, pressure, settlement, residue, and
perceptual context.

## Core Claim

Grammatical structure is present all the way down, but it is not equally visible
at every level.

At low levels the possible reads are few, the basis is usually tight, and
settlement is often exact. This makes the grammar feel like math or mechanics.
At higher levels there are more possible bases, more missing structure, more
contextual alternatives, and more social/perceptual framing. This makes the same
kind of grammar feel like natural language.

So the difference is not:

```text
low level = no grammar
high level = grammar
```

It is closer to:

```text
low level  = constrained grammar with low ambiguity
high level = loose grammar with many possible readings
```

The gradient from math to language is a gradient of resolution, basis certainty,
permutation count, residue, and contextual pressure. The same relation grammar
can be used at both ends.

## Structure Is Not The Read

A Gojo structure should not store words such as `subject`, `object`, `figure`,
`ground`, `focus`, or `definite` as the source of meaning.

The structure should expose ordinary Gojo parts:

```text
carrier
location
relation
unit shape
boundary
flow
lane basis
transition
selection
capacity
settlement
residue / tension
timeline / repetition
```

A grammar read then asks how those parts currently behave:

```text
what is selected?
what is the basis?
what is changing?
what is stable?
what receives pressure?
what settles?
what remains unresolved?
what is being contrasted or emphasized?
what perceptual domain makes this readable?
```

The read result may contain fields such as figure, ground, subject-like,
object-like, focus, specificity, or completion, but those fields belong to the
read. They are not primitive structure.

## Slots Belong To Reads, Not To Reality

It is acceptable for a read result to say:

```text
figure: cup
ground: table
relation: on/support
residue: none
```

It is dangerous to make the underlying structure say:

```text
cup is permanently Figure
table is permanently Ground
```

The same connected scene may support several reads:

```text
A cup is on the table.
On the table is a cup.
The table has a cup on it.
The cup, not the plate, is on the table.
```

These should reuse the same underlying relation structure while changing the
entry point, selected carrier, basis, focus pressure, and construction order.

## Grammar As Incremental Interpretation

Grammar should be read as information arrives. A structure can start as a low
resolution relation and gain detail over time.

```text
A -- relation -- B
```

Initial read:

```text
two carriers participate in one relation
```

After stability and selection evidence:

```text
B reads as figure
A reads as ground
```

After flow evidence:

```text
A reads as source or support
B reads as target, occupant, or affected participant
```

After focus evidence:

```text
B is contrastively selected
```

After settlement:

```text
the scene is complete enough at this basis and resolution
```

This is a more useful model than a fixed grammar template. The structure does
not have to know all roles when it is created.

## Low-Level Equations

Equations can be read with the same grammar.

A symbolic equation such as:

```text
a + b = c
```

should not be reduced to fixed slots:

```text
left operand, operator, right operand, equals, result
```

A Gojo grammar read should see a settlement scene:

```text
selected contributions: a, b
participation pressure: accumulation / composition
basis: shared unit, lane, or domain
settlement target: c
residue: what fails to settle
```

If `a + b` settles exactly as `c`, the residue can read as zero. If not, the
residue remains visible as tension.

A partially known equation such as:

```text
x + 3 = 7
```

can read as:

```text
known contribution: 3
unresolved selected contribution: x
settlement target: 7
required unresolved pressure: 4
```

Solving is not only symbolic rewriting. It is a settlement search for the
missing participation that reduces residue under the requested basis.

## Arithmetic Operators As Grammar Reads

Arithmetic can be treated as compact notation over relation grammar.

| Notation | Grammar read |
| --- | --- |
| `a + b` | compatible contributions accumulating into a shared settlement surface |
| `a - b` | forward contribution opposed by inverse/removal pressure |
| `a * b` | repeated, scaled, paired, or filled participation depending on basis |
| `a / b` | whole-to-part, part-to-count, inverse scale, or residue-distribution read |
| `a = b` | settlement claim between two read surfaces |
| `x` | unresolved selected participant constrained by surrounding structure |

Multiplication is a useful example. `3 * 4` can mean:

```text
three groups of four
four scaled by three
three-by-four rectangle
three rows of four
three repetitions of a four-tick transition
```

The numeric settlement may be the same, but the grammar and perceptual basis are
not identical. Gojo should preserve enough structure to let these different
reads remain distinguishable when it matters.

## Grammar At The Low End

At the low end, grammar is less prominent because structure is tightly bounded.

For example, a carrier location on a known basis may only support a few reads:

```text
selected point
basis carrier
location ticks
resolution
residue
```

This feels mathematical because there is little ambiguity. But the grammar is
still present:

```text
figure: selected point
basis/ground: carrier axis
specificity: exact tick or low-resolution interval
completion: settled if basis and location are known
residue: missing calibration, overflow, or incompatible basis
```

The high-level language version has the same shape with more possible bases:

```text
The cup is on the table.
```

```text
figure: cup
basis/ground: table surface
relation: support/contact
specificity: definite cup and table
completion: settled local scene
residue: none, or unresolved exact position
```

## Perceptual Primitive Spaces

A grammar pattern becomes meaningful only against a perceptual or conceptual
space. The same structural pattern can appear in physical, social, temporal,
CAD, mathematical, electrical, or narrative contexts.

The spaces should not create unrelated grammars. They should calibrate common
Gojo structure.

Potential primitive spaces include:

| Space | Typical basis | Typical figure | Common relations |
| --- | --- | --- | --- |
| Physical position | surface, container, path, field | object, mark, body, tool | in, on, through, from, to, near |
| Temporal | timeline, cycle, event boundary | event, duration, date | before, after, during, into, until |
| Social | person, group, role network | participant, obligation, message | introduce, give, owe, belong, report |
| Attention | current focus, task, view | selected item, contrast, question | emphasize, background, repair, repeat |
| Mechanical/CAD | frame, sketch plane, edge, face, constraint | point, span, feature, operation | attach, offset, extrude, constrain, trim |
| Mathematical | unit basis, equation surface, domain | variable, value, term, residue | settle, transform, solve, compose |
| Visual | image plane, contour, region, light field | edge, blob, color patch, motion | contain, border, align, occlude |
| Auditory/speech | time stream, pitch contour, phrase boundary | syllable, stress, pause, voice | rise, fall, lengthen, contract, repeat |
| Electrical/flow | node, potential, path, capacity | current, charge, signal, load | source, sink, route, block, dissipate |

The table is intentionally not final. It is a starting inventory for training
and experiments. The important point is that these spaces reuse structural
patterns but change what counts as a basis, figure, route, settlement, and
residue.

## Shared Motion Grammar Across Domains

Many sentences use the same structural grammar while changing perceptual space.

| Sentence | Space | Figure | Ground / target | Motion or transition |
| --- | --- | --- | --- | --- |
| I put the book on the shelf. | physical | book | shelf | object moves to support surface |
| I introduced Mary to John. | social | Mary | John's social attention/network | participant enters relation availability |
| Thursday turned into Friday. | temporal | Thursday/event boundary | Friday/timeline successor | interval transitions across boundary |
| Move the sketch point onto the line. | CAD | point | line constraint | geometric location settles onto basis |
| Bind `x` to 4. | mathematical | unresolved parameter | value/basis 4 | equation residue settles through binding |

These are not identical events, but they share a grammar:

```text
selected figure
route or transition
basis / target / ground
settlement witness
residue if incomplete
```

The perceptual space decides what kind of movement is allowed. Physical motion,
social introduction, temporal succession, CAD constraint settlement, and equation
binding are different calibrations of common relation grammar.

## Small Words As Framing Devices

English often permits unusual order by adding small guide words.

```text
A cup on the table.
The table has a cup on it.
On the table is a cup.
There is a cup on the table.
```

Words such as `has`, `there`, `on`, `it`, `to`, `from`, `by`, `as`, `of`, and
`with` often do not add a new object to the scene. They steer the read.

They can indicate:

```text
entry point
selected perspective
basis shift
figure/ground reversal
route direction
implicit residue
completion expectation
possession or containment frame
role reinterpretation
```

For Gojo, these should usually become small read-framing relations rather than
large concept objects.

Example:

```text
A cup is on the table.
```

Read order:

```text
figure first -> relation -> ground
```

Example:

```text
The table has a cup on it.
```

Read order:

```text
ground first -> possession/containment frame -> figure -> relation back to ground
```

The word `it` is important because it closes the loop back to the table. The
scene is not just reordered; the mental construction path changes. Gojo can
represent this as a different traversal/read path over the same physical
relation structure.

## Vocal And Conversational Framing

Speech adds framing evidence that often behaves like structural pressure.

| Speech feature | Possible Gojo read |
| --- | --- |
| stress | focus pressure, contrast selection, higher requested resolution |
| rising pitch | unresolved question, proposed read, incomplete settlement |
| falling pitch | local settlement or completed claim |
| pause | boundary, grouping, delayed completion, repair site |
| contraction/reduction | backgrounded or low-resolution support structure |
| volume | increased pressure, urgency, command force, high salience |
| lengthening | intensity, hesitation, unresolved search, increased magnitude |
| repetition | repair, insistence, loop, count, rhythm, feedback |
| voice quality | stance, certainty, social frame, privacy, play, warning |

These are tempting to encode literally, and sometimes literal encoding is right.
Repetition can really be repeated structure. Emphasis can really be extra focus
pressure. A pause can really be a boundary. But the system should avoid turning
all of these into symbolic tags. They should be relation evidence that a read can
use.

## Articles And Specificity

Articles such as `a` and `the` should not necessarily become explicit markers in
low-level structure. Often their effect is already visible in the relation
network.

Possible reads:

| Language | Gojo read |
| --- | --- |
| `a cup` | introduced carrier with unresolved identity or low prior grounding |
| `the cup` | carrier resolved against shared basis or prior structure |
| `this cup` | selected proximal carrier in current basis |
| `that cup` | selected distal or contrastive carrier |
| `some cup` | partial or indefinite identity with visible residue |
| `any cup` | low-specificity selection over a compatible domain |

If Gojo already knows that a carrier is selected from a settled local basis, the
`the` read may be computable without storing a definite marker. If Gojo is
importing text, an article may be useful as provisional evidence until the
structure resolves.

## CAD And Mechanical Language

CAD is a strong candidate for this grammar layer because the domain is narrower
than ordinary conversation. The possible perceptual spaces are constrained:

```text
frame
sketch plane
point
line
edge
face
span
radius
offset
constraint
feature history
material region
tool path
```

A sentence such as:

```text
put the hole on the centerline
```

can read as:

```text
figure: hole feature or center point
basis/ground: centerline
relation: coincidence / alignment / projection
settlement: constraint witness
residue: offset, underconstraint, ambiguous axis, missing plane
```

A sentence such as:

```text
round the two front corners
```

can read as:

```text
selected structure: front side or face basis
figure: two corner feature sites
transition: corner meeting -> radial transition
settlement: radius/arc/tangent witness
residue: missing radius, ambiguous front, incompatible topology
```

This may cover a large useful subset of CAD interaction because CAD has fewer
valid interpretations than open-domain language.

## Training Shape

This grammar is well suited to machine learning if examples are organized around
read levels and witnesses rather than only final labels.

A training item should ideally include:

```text
source form:
  text, gesture, sketch, equation, CAD operation, or mixed input

raw structure:
  carriers, relations, locations, unit shapes, boundary/flow evidence

read request:
  requested basis, resolution, domain, and entry point

read outputs:
  figure/ground, participation, focus, specificity, settlement, residue

witnesses:
  why the read was accepted, rejected, partial, or ambiguous

round-trip target:
  prose, equation, CAD structure, rendered shape, or solver-compatible form
```

The read level should be explicit:

```text
L0 low-resolution tags or compact reads
L1 relation surfaces and basis evidence
L2 settled carriers, witnesses, residue, timelines
L3 external CAD/solver/rendered references connected back to witnesses
```

The goal is not to train a model to memorize labels. The goal is to train it to
move between loose human forms and Gojo structure while preserving evidence,
residue, and alternate reads.

## Proposed Document / Implementation Path

A practical path forward:

1. Keep building primitive Gojo structures: carrier, relation, unit shape,
   boundary, flow, transition, settlement, residue, timeline.
2. Define grammar reads as local interpretations over those structures.
3. Define a small table of perceptual spaces and their common basis/figure/route
   patterns.
4. Build examples that map the same grammar across physical, temporal, social,
   mathematical, and CAD spaces.
5. Identify small framing devices: prepositions, articles, possession words,
   pronouns, stress, pause, repetition, and contraction.
6. Decide which devices are already apparent from structure and which should be
   imported as provisional evidence.
7. Train or test against paired examples where one structure supports multiple
   valid readings.
8. Preserve residue and alternate reads instead of forcing one parse too early.

## Current Test Foothold

The first sentence-like slice is intentionally fixture-only in
`Gojo.Encoding.Tests`. That keeps language experiments out of the kernel while
still requiring them to be relation-shaped and inspectable. The smaller
interval and contextual projection reads have been extracted into
`Gojo.Encoding` because they are generic structural reads, not sentence grammar.

Current fixture:

```text
RedTraceNearPinGrammarFixture.CreateLedger
RedTraceNearPinGrammarFixture.CreatePerspectiveRelativeLedger
GrammarFixtureReadSurface.TryReadGrammar
IntervalBasisReadSurface.TryRead / TryLocate
ContextualProjectionReadSurface.TryRead / TryReadAt
```

The fixture encodes:

```text
red trace near pin
```

as token order, lexical candidates, a modifier candidate, a low-resolution
position witness between trace and pin, two possible phrase reads, a selected
coarse read, an interpretation bundle, a grammar trace, and residue for
unresolved color basis, exact position, and alternate attachment. The token
`near` is provenance for that read; the near-like meaning comes from the
low-resolution position on a ground, not from a stored preposition concept tree.

The extracted generic foothold reads an interval from structure alone. A
carrier span has start, center, and end landmarks plus coarse bands. Those are
not English words yet; they are low-resolution positions against a basis. A
spatial context can project the start band as `near` and the end band as `far`;
a temporal context can project the same start and end bands as `before` and
`after`. If a context has no projection witness, the interval role still reads
and the context-specific word simply remains unresolved. If several projection
witnesses fit, the read surface can return them together so alternatives remain
available for later settlement.

This is enough to begin the CAD-first grammar vertical slice. It is not enough
to freeze the packet format or claim a general language parser. The next lower
structural gaps to watch are saved read-witness encoding, domain basis surfaces,
alternate-read settlement under later context, and boundary/flow prepositions
such as `inside`, `outside`, `through`, and `against`.

## Compact Rule Set

```text
1. Grammar exists all the way down, but low-level grammar is tightly constrained.
2. A read can have slots; the underlying structure should not be slot-owned.
3. Figure, ground, subject, object, focus, and specificity are read outcomes.
4. Perceptual space calibrates common relation grammar.
5. Equations are settlement scenes, not only symbolic strings.
6. Small words and vocal features are framing evidence for read entry, basis,
   focus, completion, and residue.
7. If structure already implies a distinction, do not add a marker.
8. If imported language provides useful unresolved evidence, keep it as tension
   or provisional read evidence.
9. Prefer local traversal and witnessable settlement over global classification.
10. Ambiguity is training signal, not failure.
```
