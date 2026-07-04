# Scene Mechanics

Design status: pre-code working note.

This note names the layer below verb-like and preposition-like reads. A scene
mechanic is not a word category. It is a small inspectable Gojo structure that
can later be selected, focused, animated, bound to context, or combined with
other structures.

The goal is a compact mechanical alphabet, not a giant ontology.

```text
scene mechanic primitive
  connected carriers, points, spans, and relations
  optional flow or energy evidence over that support
  later selection/focus/read surfaces
  residue/tension when a read or settlement does not fit
  lexical labels only as later read names or provenance
```

## Three Layers

Keep these layers separate whenever possible.

```text
support structure
  the carrier, sites, spans, partitions, branches, links, and relations

flow or energy
  directed participation, repeated movement, pressure, animation, or timeline
  evidence that runs over support

read
  the focus and context that make a structure read as move, send, contact,
  perceive, join, avoid, before, near, inside, or another word-like concept
```

For example, `back and forth` should not require a special back-and-forth
object. The lower structure can be a bounded carrier. The flow layer can carry
one direction and then the reverse. A later read can call that oscillation,
return, reciprocal motion, undo, or conversational exchange depending on
context.

## Carrier Economy

Do not create one carrier for every noun in the English explanation. A carrier
should represent independently readable support.

Use one carrier when the evidence lives on one support:

```text
bounded carrier:
  site A on carrier C
  site B on carrier C
  span A-B on carrier C

directed flow:
  same carrier C
  directed participation from A to B
```

That base shape can later read as `interval`, `start`, `end`, `middle`,
`between`, `before`, `after`, `near`, `far`, or `endpoint`, depending on
selection, focus, perspective, scale, and context. Motion-like reads such as
`move`, `transform`, or `transfer` need additional flow, energy, or animation
evidence over the carrier.

Use another carrier when there is another independently readable participant or
support:

```text
contact:
  carrier A has its own approach/support
  carrier B has its own approach/support
  relation links the contact sites

perception/link:
  observer support and target support remain distinct
  relation connects them without forcing a transfer carrier

branch:
  primary support exists
  alternate support is attached to it at one or more sites
```

A relation between carriers is often enough. A third carrier is only needed
when the connector is itself readable support, such as a bridge, route, glue
line, wire, via, or conduit.

## Primitive Fixture Set

The compact JSON fixtures in
`Gojo.Encoding/Fixtures/EncoderJson/scene-mechanics/` are meant to be primitive
mechanical structures. They intentionally do not include read/focus labels.

| Fixture | Carrier count | Primitive shape | Later reads it can support |
| --- | ---: | --- | --- |
| `001-bounded-carrier` | 1 | two sites and a span | interval, endpoints, middle, between, near/far, before/after |
| `002-directed-flow` | 1 | support span plus one directed flow witness | move, transfer, send, toward, push |
| `003-reversing-flow` | 1 | support span plus forward and reverse flow | return, back-and-forth, oscillate, reciprocal |
| `004-partitioned-carrier` | 1 | whole span, middle site, subspans | split, midpoint, start/end, before/after |
| `005-nested-span` | 1 | spans nested inside one support | contain, cover, overlap, inside, capacity |
| `006-boundary-crossing` | 1 | flow passes a boundary site | enter, exit, pass, threshold |
| `007-two-carrier-contact` | 2 | two supports meet at a relation | touch, hit, bump, meet, collide |
| `008-two-carrier-link` | 2 | two supports linked by relation | perceive, refer, attend, depend, contact socially |
| `009-obstructed-carrier` | 2 | path support conflicts with obstacle support | block, resist, prevent, tension, avoid |
| `010-outlet-branch` | 2 | second support leaves one site | branch, emit, escape, layer hop, outlet |
| `011-branch-merge` | 2 | alternate support leaves and rejoins | around, bypass, over, via, repair |
| `012-bridged-join` | 3 | two supports connected by readable bridge support | join, attach, glue, repair, conduct |

These are not final categories. They are low-resolution seeds. A useful next
pass may merge, split, or remove some of them once the read layer becomes more
capable.

## Word-Like Reads Are Later

Several earlier examples were too word-shaped. They are better treated as reads
over the primitive set.

```text
transform
  bounded carrier + optional directed flow
  focus: source site becomes result site

directed transfer
  bounded carrier + directed flow
  focus: outbound path, incoming settlement, carried marker, or route

contact
  two-carrier contact
  focus: shared relation, pressure at relation, residue after relation

perception
  two-carrier link
  focus: observer side, target side, missing evidence, resolution

joining
  contact, branch/merge, or bridged join
  focus: local relation, resulting continuity, connector support

avoidance
  obstructed carrier + branch/merge
  focus: rejected span, alternate support, retained goal
```

This lets multiple words share one lower structure, and it lets one lower
structure produce different words under different focus.

## Structure And Energy

Static support and moving energy should not be collapsed.

```text
support:
  carrier C from 0 to 10

flow:
  directed witness 0 -> 10

reverse flow:
  directed witness 10 -> 0

animation:
  a later timeline samples or settles these witnesses over time
```

The current JSON fixtures can represent flow witnesses with `directedSegment`.
That is enough for the glossary and early tests. It is not yet a mature
animation/timeline encoding. A fuller version should probably use repeated
state, transition, controller, feedback, and residue structures from the Gojo
repetition/timeline direction.

## Context Binding

The same primitive can bind into several domains.

```text
physical:
  carrier is a path, surface, object support, or contact region

temporal:
  carrier is an ordered interval or event span

social:
  carrier is a role, obligation, permission, ownership, or attention relation

informational:
  carrier is a message path, reference, channel, or reader-target link

CAD:
  carrier is sketch geometry, constraint support, operation path, or ladder

electrical:
  carrier is net, trace, layer, conductor, pad, bridge, via, or obstacle
```

The primitive should not need to know which domain it is in. The context
supplies calibration. Unfit pieces remain as residue or tension.

## Combining Primitives

Complex concepts should combine lower structures rather than become new root
types.

```text
send
  bounded carrier + directed flow

deliver
  directed flow + target-side settlement read

hide
  nested span + perception link with blocked or missing target read

avoid
  obstructed carrier + branch/merge

PCB via detour
  obstructed carrier + outlet branch + layer/context binding + branch/merge

glue two materials
  two-carrier contact + bridged join + material/context binding
```

The read should preserve which primitive supplied each piece of evidence. A
solver can then change the lower rung without losing the higher intent.

## Glossary Rendering

The Gojo Blocks glossary page reads these JSON structures and draws a small
diagram from carriers, points, spans, directed spans, and relations. The final
Skia drawing takes liberties for clarity, such as offsetting overlapping spans
on the same carrier, but it is still a rendering of the structure rather than a
separate hand-authored icon set.

## Next Milestone

1. Keep this primitive fixture set small.
2. Add a read layer that selects and focuses the same primitive several ways.
3. Add a tiny animation/timeline sample for directed and reversing flow.
4. Compose `obstructed carrier + branch/merge` into an avoidance repair.
5. Bind that repair into a PCB routing context and inspect whether via, bridge,
   and around-style solutions can be read as refinements of the same intent.

The main risk is still rebuilding an ontology with nicer words. The guardrail
is simple:

```text
if the label disappears, the Gojo structure should still carry the mechanic
```
