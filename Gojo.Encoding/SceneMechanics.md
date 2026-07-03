# Scene Mechanics

Design status: pre-code working note.

This note names the layer just below verb-like and preposition-like language
reads. A scene mechanic is a small, reusable, inspectable Gojo structure that
can be selected, focused, run through a basis, refined, reversed, or combined
with another mechanic.

The goal is not to make a verb ontology. The goal is to build a compact set of
mechanical shapes that can later read as many words depending on focus,
resolution, scale, perspective, domain, and residue.

```text
scene mechanic
  connected Gojo structure
  local transform or settlement behavior
  focus/read surfaces over the structure
  residue/tension when the read does not fully settle
  lexical words only as later read labels or provenance
```

The same lower mechanic may read as physical motion, time order, social
transfer, CAD editing, electrical routing, or argument structure after it is
bound to a context.

## Core Rule

Durable meaning is created by multiplying a simple mechanic through several
read dimensions:

```text
mechanic
  * selected participation
  * focus basis
  * perspective or entry point
  * domain context
  * scale and resolution
  * result/residue pressure
  = stable concept read
```

Changing one of those dimensions should often change the word-like read without
changing the lower mechanic.

For example, a directed transfer mechanic can read as:

```text
move      focus on change of position
send      focus on source and outbound path
deliver   focus on arrival and settled recipient
bring     focus from the target side
take      focus from the source side
return    reverse the prior transfer basis
transmit  same transfer shape through an informational or electrical medium
```

This is why broad verbs such as `move` feel weak. They read a very low
resolution part of the scene. Richer verbs usually focus a more specific part:
contact, route, result, obstacle, capacity, boundary, recipient, residue, or
repair.

## Selection, Focus, Context

Selection, focus, and context should remain separate enough to inspect.

```text
selection
  which connected participation is being used by this read

focus
  which part of the selected participation carries the read pressure

context
  which basis/domain calibrates the same mechanic
```

A selected span can be the whole route, the contact region, the entering
boundary, the carried object, the residue, or the final settled result. A focus
can then ask for motion, contact, completion, source, target, obstruction,
repair, or leftover tension.

The context should not be a giant meaning tree. It is a basis that tells the
read what kind of carriers, endpoints, supports, flows, or capacities are
available.

```text
physical context
  carriers are surfaces, paths, objects, supports

temporal context
  carriers are ordered intervals and event spans

social context
  carriers are people, roles, permissions, obligations, ownership

informational context
  carriers are messages, symbols, channels, readers

CAD context
  carriers are sketch geometry, constraints, parameters, tool operations

electrical context
  carriers are nets, traces, layers, conductors, components
```

The same scene mechanic can be bound to any of these when enough structure fits.
Unfit pieces remain as residue instead of forcing a false exact match.

## Basic Mechanic Families

These are candidate scene mechanics, not final categories.

| Mechanic | Simple shape | Common reads | Reverse or dual |
| --- | --- | --- | --- |
| transform | source state becomes result state | move, change, become, shift | restore, undo |
| directed transfer | source, route, carried participation, target | send, deliver, bring, take, transmit | return, receive |
| contact | two participations reach a shared boundary | touch, hit, bump, tap, meet | separate, miss |
| pressure along carrier | force/flow changes participation against support | push, pull, drag, steer, guide | resist, yield |
| boundary crossing | route passes an edge with inside/outside reads | enter, exit, pass, penetrate | leave, exclude |
| containment | occupant settles against capacity/boundary | fill, contain, hold, store | empty, release |
| surface coverage | selected material occupies a face or visible band | cover, coat, paint, hide | uncover, reveal |
| separation | one connected whole becomes distinct parts | split, cut, divide, break | join, merge, attach |
| joining | distinct parts gain shared support or link | join, attach, combine, assemble | separate, detach |
| obstruction | intended flow accumulates tension at a boundary | block, obstruct, stop, prevent | open, permit, conduct |
| conduit | support allows flow through a route | carry, conduct, channel, route | block, leak |
| avoidance | preserve goal while refusing a region | avoid, bypass, route around | pursue, seek |
| pursuit | reduce gap to selected moving or missing target | chase, follow, seek, search | avoid, flee |
| support placement | participation settles on/against stable basis | put, set, place, hang, lean | remove, fall |
| perception/read | focus resolves structure through a sensing basis | see, hear, feel, notice, inspect | miss, ignore |
| social exchange | transfer shape over ownership/permission/obligation | give, lend, pay, trade, ask, tell | receive, owe |

Many of these are reverse-readable. Separation and joining are the clearest
pair. Fill and empty, cover and uncover, enter and exit, send and return, attach
and detach, reveal and hide, block and conduct are also the same or neighboring
mechanics under a reversed basis or changed focus.

Some are not simple opposites. `avoid` and `pursue` both use a gap and route
relation, but their tension gradients differ. `avoid` keeps distance from an
unwanted region while preserving some other goal. `pursue` reduces distance to a
wanted moving or missing target.

## Worked Mechanic: Directed Transfer

A directed transfer is one of the most reusable mechanics. It can be physical,
informational, electrical, social, temporal, or CAD-like.

### Lower Gojo Shape

```text
scene:
  source site A
  target site B
  route carrier R
  carried participation P
  start witness P at A
  end witness P at B
  time or refinement carrier T
  residue/tension E

route:
  start point on R
  end point on R
  optional middle span
  optional capacity or blockage

timeline:
  before: P participates at A
  during: P participates along R or in an unresolved route
  after: P participates at B
```

In Gojo terms, this should be built from ordinary carriers, points, segments,
relations, read witnesses, focus surfaces, and timeline/refinement evidence. It
should not require a `Transfer` enum or a verb-owned object.

One possible focus surface:

```text
source structure:
  A, B, R, P, route span, before/after participation

focus basis:
  route span, endpoint basis, or arrival settlement

selected participation:
  P at A
  P moving or mapped through R
  P at B

carry-forward complement:
  the parts of the scene not being changed by this read

replacement or result:
  P settled at B

correspondence:
  source site maps to prior participation
  target site maps to result participation
  route sites map to intermediate participation

residue/tension:
  missing route
  blocked middle
  unresolved target
  capacity mismatch
  ambiguous carried participation
```

### Word Reads From Focus

| Read | Focus | Required settlement |
| --- | --- | --- |
| move | P changes position or state through any compatible basis | result differs from source |
| send | A/source and outbound route are salient | route begins, target may remain unresolved |
| deliver | B/target and completion are salient | P settles at B with low arrival residue |
| bring | target-side perspective is salient | route resolves toward observer or goal side |
| take | source-side removal is salient | P leaves A and source loses participation |
| transmit | route medium is salient | P is signal/information and carrier continuity matters |
| return | previous source becomes current target | transfer reverses or closes a prior path |

The mechanic is the same. The read changes by focus and perspective.

### Context Examples

```text
physical:
  deliver package from shelf to desk

informational:
  send message from Alice to Bob through a channel

electrical:
  transmit signal from pad A to pad B through a net

CAD:
  move sketch point from old constraint site to new constraint site

social:
  give ownership from person A to person B
```

In each case, the domain supplies what counts as source, route, target, carried
participation, capacity, and residue. A perfect structural fit can map exactly.
A metaphorical fit can leave extra residue.

## Worked Mechanic: Boundary Change

Boundary change covers separation, joining, coverage, filling, and many
result-focused verbs. It is not one verb. It is a family of reads over support,
boundary, participation, and result settlement.

### Separation And Joining

```text
before separation:
  whole support W
  connected participation across W
  possible boundary or cut path C

focus basis:
  selected boundary C
  partition or contact path
  tension where continuity fails or is forced to fail

after separation:
  part W0
  part W1
  boundary residue between them
  optional tool/contact provenance
```

This can read several ways:

| Read | Focus |
| --- | --- |
| split | result parts and partition are salient |
| cut | tool/contact path C is salient |
| divide | partition basis is salient, often cleaner and less violent |
| break | excessive tension/failure is salient |
| separate | loss of shared support is salient |

Run backward or through a reverse basis:

```text
before joining:
  W0 and W1 are distinct
  compatible endpoints, faces, or supports are visible

after joining:
  shared support or bridge exists
  continuity settles across the prior boundary
```

| Read | Focus |
| --- | --- |
| join | resulting continuity is salient |
| attach | stable contact/link is salient |
| merge | flows or identities combine into shared support |
| assemble | multiple joins under an intended structure |
| repair | joining reduces prior damage or residue |

### Coverage And Filling

Coverage uses a boundary or face as the support. Filling uses capacity or
interior support.

```text
coverage:
  support face F
  applied participation M
  occupied band over F
  visible/unvisible read from observer basis

filling:
  boundary B
  interior capacity C
  occupant M
  fullness read = occupied capacity / available capacity
```

| Read | Focus |
| --- | --- |
| cover | occupied face or occluded target is salient |
| coat | thin surface layer is salient |
| paint | applied material plus color/read surface is salient |
| hide | observer read fails because coverage blocks it |
| fill | interior capacity settlement is salient |
| empty | reverse capacity settlement is salient |
| overflow | occupant exceeds capacity and leaves residue outside |

These should also be parameterized. A low-resolution coverage read may only
know that "enough of the face is occupied." A high-resolution CAD read may need
exact area, boundary, tolerance, and material thickness.

## Prepositions

Prepositions are usually read-framing mechanics rather than standalone objects.
They steer basis, entry point, route, support, or boundary.

| Preposition | Mechanical read |
| --- | --- |
| from | source endpoint or prior participation |
| to | target endpoint or intended settlement |
| into | boundary crossing with target interior |
| out of | boundary crossing with source interior |
| on | support/contact against a ground or face |
| in | interior participation within a boundary |
| through | route with entry, interior traversal, and exit |
| around | preserved goal plus rejected obstacle region plus alternate carrier |
| over | crossing through orthogonal or layer basis |
| under | crossing/support through inverse layer or ground-relative basis |
| with | co-participation, carried support, or instrument participation |
| against | contact plus opposed pressure or blocked flow |
| before | earlier site on ordered carrier |
| after | later site on ordered carrier |
| between | selected participation inside a bounded interval |

The same lower interval can read spatially or temporally. A start region can
read as `near`, `before`, `source`, or `entry` depending on context.

## Adjectives And Adverbs

Adjectives often read stable qualities, result states, or low-motion focus over
a noun-like participation.

```text
wide      extent read through across-basis
full      capacity read with low missing residue
empty     capacity read with low occupied participation
blocked   intended flow with unresolved obstacle tension
open      possible flow or available support
sharp     high contrast, small transition radius, or fast change
near      low-resolution distance read against a local basis
red       color/signal read through a visual/material basis
```

Adverbs often read the transform itself:

```text
quickly     high change per time basis
slowly      low change per time basis
carefully   high requested resolution and low allowed damage/collision residue
directly    route with low branch/repair residue
almost      small remaining residue under the requested result
completely  result settlement with low residue at current resolution
gently      low contact impulse or low damage tension
roughly     intentionally low-resolution result
```

This is where numeric units become important. `mph` is not a separate deep
meaning. It is a strict calibrated basis for a transfer/motion read:

```text
distance participation / time participation
  under mile and hour unit bases
  read as speed
```

The same generic rate shape can become words like `fast`, `slow`, `rapid`,
`late`, `early`, or a precise engineering value depending on scale, threshold,
and context.

## Parameterization And Scale

Parameterization is one reason the primitive set can stay compact.

Useful knobs include:

```text
resolution
  coarse, exact, approximate, omitted, ambiguous

scale
  local, far, large, small, fast, slow

orientation
  forward, inverse, orthogonal, ambiguous

continuity
  continuous flow, discrete jump, teleport-like mapping

completion
  attempted, partial, settled, overfilled, failed

pressure
  weak, strong, blocked, opposed, released

perspective
  source-side, target-side, observer-side, route-side
```

`near` and `far` are a clean example: shared comparison structure, different
focus/scale thresholds. Many verbs appear to work the same way. `tap`, `hit`,
`strike`, `bump`, and `crash` can share a contact mechanic while differing in
speed, pressure, intent, result damage, and residue.

## Combining Mechanics

More complex concepts can be assembled from smaller mechanics without becoming
a giant tree of definitions.

```text
send + obstruction + avoidance
  route around an obstacle

transfer + joining
  attach delivered component to target

separation + transfer
  distribute parts from one source to many targets

coverage + perception
  hide or reveal

contact + pressure
  push, hit, press, rub, scrape

boundary crossing + containment
  enter, exit, store, release

pursuit + failed perception
  search
```

The combination should preserve the lower pieces. A solver or reader should be
able to inspect which mechanic created which pressure, where the settlement
failed, and which repair reduced residue.

## Inspectable Geometry

These mechanics should have diagrammatic forms, but the diagram should be a
read over Gojo structure, not the source of meaning.

An inspectable diagram for transfer might show:

```text
A --- route --- B
P at A  ->  P along route  ->  P at B
```

An inspectable diagram for obstruction and avoidance might show:

```text
A --- blocked span --- B
      [obstacle]

A --- entry
        |
        branch carrier
        |
      exit --- B
```

The drawn object is useful because humans and models can inspect it, but the
important object is still the underlying carriers, points, segments, relations,
focus surfaces, timeline, residue, and settlement witnesses.

## Encoding Consequences

This suggests the next encoding milestone:

1. Keep the first pure-structure fixture set in
   `Gojo.Encoding/Fixtures/EncoderJson/scene-mechanics/`.
2. Choose one scene mechanic, probably directed transfer, and build the first
   read layer over its existing structure.
3. Add several read surfaces over the same fixture: `move`, `send`, `deliver`,
   `return`, and `transmit` as lexical/provenance labels only.
4. Bind the same mechanic into at least two contexts, such as physical package
   transfer and message relay.
5. Preserve exact matches, partial matches, and missing pieces as residue.
6. Add a small visual or folded JSON inspection artifact that shows the same
   mechanic before and after focus/context changes.

If that works, the next candidate should be boundary change, because it tests
reverse reads, result focus, tool/contact focus, and capacity/coverage reads in
one compact family.

## Open Risks

The main risk is accidentally rebuilding an ontology with nicer words. The
guardrail is:

```text
if the label disappears, the Gojo structure should still carry the mechanic
```

Another risk is overfitting to literal geometry. The mechanic should be
geometric enough to inspect, animate, and settle, but abstract enough to bind to
time, social relation, information, CAD, and electrical contexts.

The third risk is pretending every read is computable by static rules. Some
matching, especially metaphorical fitting, may need learned attention. That is
acceptable as long as the learned layer proposes bindings and residue over
inspectable structure instead of replacing the structure.
