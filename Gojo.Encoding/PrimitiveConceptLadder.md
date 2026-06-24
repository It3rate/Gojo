# Primitive Concept Ladder

This note is a working inspection ladder for primitive Gojo concept encoding.
It sits between the compact glossary vocabulary and the broad seed sketch in
`README.md`.

The goal is not to make a dictionary. The goal is to ask which useful words can
be built from earlier structure without becoming detached labels. A word here is
a reusable crystallized read:

```text
Def(word, lower-footing) + Prov(lower-footing) + Residue?
```

If a word is decoded from its footing and comes back as a nearby idea, that is
usually fine. The difference can remain as residue. If it decodes into a very
different idea, the footing is too weak, too broad, or missing a primitive.

## Resolution Note

Resolution should be treated as a family of reads over one shared primitive
shape:

```text
selected structure
  compared through a chosen basis, partition, or rung
  produces readable participation
  leaves lower, missing, incompatible, or unused detail as residue/tension
```

That shared shape can appear in several categories:

| Resolution family | Footing | Read |
| --- | --- | --- |
| Partition resolution | `Part(E,n)` | `E` is made locally countable through `n` readable divisions. |
| Unit resolution | `U(Comp(E,F))` | `E` is compared to `F` so one can be read as a unit or calibration for the other. |
| Observer resolution | `Read(E,B,Res(n))` | A read surface selects what distinctions are visible at this rung. |
| Omission resolution | `Slot(role,?)` or `Open(E,role?)` | A structure intentionally leaves a role open without becoming invalid. |
| Approximation resolution | `Comp(E,target?,Res(n)) -> compatible + Residue(delta?)` | `E` is near enough at this rung, while sharper difference remains open. |
| Ladder resolution | `Rung(E,r) -> Rung(E,r+1) + Residue?` | A coarse read and a sharper read remain connected by refinement. |
| Runtime resolution | `Run(E,input,t,Res(n)) -> result + Residue + Tension` | Resolution belongs to this run/read, not permanently to `E`. |

The important distinction:

```text
unknown
  no settled read target or basis yet

approximate
  compatible with a sharper target at this resolution, with residue preserved

coarse
  valid lower-rung read, not a failed fine read

omitted
  a slot or role is left available by the structure itself

ambiguous
  multiple reads remain possible at this resolution
```

These should not be collapsed into one `unknown` bucket. They are different
local read states.

## Primitive Footing Groups

These groups are ordered loosely from lower to higher. The order is not final,
but a later word should be explainable by earlier groups or by a previously
crystallized word.

### Presence And Reference

| Concept | Footing | Useful words |
| --- | --- | --- |
| reached structure | `E?` | thing, something, item, object, subject, matter, case |
| absence in requested frame | `Read(E,B) -> Residue(missing)` | nothing, none, absent, gap, missing, void |
| open unknown | `Slot(role,?)` | unknown, unspecified, blank, pending, variable, placeholder |
| local reference | `X` | address, site, mark, reference, anchor, handle |
| role at reference | `A@X` | role, instance, participant, occupant, named-site |
| shared reference | `A@X + B@X` | coincident, same-place, together, stacked, co-located |
| distinct references | `A@X + B@Y; X != Y` | separate, apart, different-place, distributed |
| aspirational reference | `Ref(target?)` | intended, expected, wanted, future, promised |
| unresolved identity | `A@X + B@X + Id?` | same?, duplicate?, alias?, lookalike |
| identity at resolution | `Comp(A,B,Res(n)) -> same + Residue` | same, equal, equivalent, matching |
| distinction at resolution | `Comp(A,B,Res(n)) -> different + Residue` | different, distinct, other, contrast |

Reverse-read confidence:

```text
thing/object/item
  low confidence, intentionally broad; should decode only to reached structure.

same/different
  medium confidence; must carry the selected resolution or it becomes global identity.

coincident
  high confidence only when reference-sharing is visible; not enough for a collapsed basis.
```

### Participation And Support

| Concept | Footing | Useful words |
| --- | --- | --- |
| position | `P(X)` | point, place, location, position, here |
| source | `Src(E)` | source, origin, root, input, cause-seed |
| support | `B(E)` or support read over `E` | support, base, frame, ground, substrate |
| carrier potential | `Pot(E)` | possible-line, possible-frame, seed, latent support |
| occupied support | `Occ(E,B)` | occupied, filled, used, taken, present |
| empty support | `Empty(E,B) + Residue?` | empty, unused, available, open-space |
| blocked support | `Block(E,B) + Tension` | blocked, closed, unavailable, obstructed |
| capacity | `Cap(B,Res(n))` | capacity, room, allowance, bandwidth, space |
| overflow | `Occ(E,B) > Cap(B)` | overflow, excess, spill, surplus, overrun |
| deficit | `Need(E,B) + missing support` | shortage, need, lack, deficit, underfilled |
| contact | `Touch(A,B,Res(n))` | touch, contact, meet, adjacent-contact |
| attachment | `Touch(A,B) + Link(A,B)` | attached, joined, connected, mounted |
| detachment | `Link(A,B) -> Residue(removed)` | detached, free, loose, separated |

Reverse-read confidence:

```text
support/base/frame
  medium confidence; must expose what is being supported and through which basis.

empty/available
  weak unless a possible support frame is visible. Absence without frame is just unknown.

blocked
  high only when an intended route or support is present and fails.
```

### Extent, Carrier, And Span

| Concept | Footing | Useful words |
| --- | --- | --- |
| span | `S(P(A@X),P(B@Y)); X != Y` | span, segment, interval, distance-read |
| collapsed basis | `S0(P(A@X),P(B@X))` | zero-span, collapsed, coincident-basis |
| length read | `Read(S(...),B,Res(n))` | length, distance, extent, reach |
| width read | `Read(S(...),basis=across)` | width, breadth, thickness |
| height read | `Read(S(...),basis=up/down)` | height, elevation, depth |
| depth read | `Read(S(...),basis=in/out)` | depth, penetration, layer-depth |
| scale | `Comp(E,F,Res(n))` | scale, size, proportion, ratio |
| near | `Comp(A,B,Res(n)) -> low distance` | near, close, local, nearby |
| far | `Comp(A,B,Res(n)) -> high distance` | far, distant, remote, nonlocal |
| between | `A -- S? -- X -- S? -- B` | between, middle, interval-inside |
| endpoint | `Bd0(S)` or `Bd1(S)` | end, endpoint, terminal, start |
| middle | `Part(S,2) + selected central site` | middle, centerline, halfway, median |
| limit | `Bd(E)` | limit, bound, edge, threshold |
| range | `Bd0(E)+Bd1(E)+support` | range, band, interval, window |

Reverse-read confidence:

```text
span/segment/interval
  high when shared carrier evidence is visible.

distance/length
  medium; needs unit or basis to become measured distance instead of only extent.

near/far
  medium to weak; always relative to resolution, basis, and local norm.
```

### Orientation, Lane, Side, And Face

Orientation should probably be lower than boundary. Boundary becomes readable
only after a side, support, flow face, or limiting role can be distinguished.

| Concept | Footing | Useful words |
| --- | --- | --- |
| lane mark | `+f`, `-f`, `+i`, `-i` | lane, face, orientation-value |
| forward face | `Dir(E,+f)` or `Dir(E,-f)` | forward, normal, outward-face |
| inverse face | `Dir(E,+i)` or `Dir(E,-i)` | inverse, inward-face, opposed-face |
| signed value | `Sign(E,+|-)` | positive, negative, plus, minus |
| orientation unknown | `Dir(E,?)` | unoriented, directionless, ambiguous-direction |
| same lane | `Dir(A,lane) + Dir(B,lane)` | aligned, same-way, compatible |
| opposed lane | `Dir(A,+f) + Dir(B,+i)` | opposed, facing, counter, inverse-related |
| side | `Side(Bd(E),lane?)` | side, inside/outside candidate, face-side |
| left/right read | `Side(E,orientation-basis)` | left, right, port, starboard |
| front/back read | `Side(E,forward-basis)` | front, back, ahead, behind |
| above/below read | `Side(E,vertical-basis)` | above, below, upper, lower |
| handedness | `Dir(E,lane)+ContourDir(E,lane2)` | clockwise, counterclockwise, handed |
| reorientation | `Read(E,B2) from Read(E,B1)` | rotate, flip, invert, turn |

Reverse-read confidence:

```text
forward/inverse
  high only when lane evidence is present. Do not infer from text order alone.

left/right/front/back
  medium; they require a local body or orientation basis.

same-way/opposed
  high if both lane reads share a substrate. Without shared substrate they are only similar labels.
```

### Flow, Route, And Outlet

Flow is still one concept. Radial, sheet-like, volumetric, and contour forms are
read consequences of source and support.

| Concept | Footing | Useful words |
| --- | --- | --- |
| orthogonal outlet | `O(E)` | outlet, escape, normal, perpendicular possibility |
| flow | `F(O(E))` | flow, movement-potential, spread, pressure |
| source flow | `Src(E)+F(O(E))` | emit, radiate, leak, output |
| inward flow | `F(O(E))+Dir(...,+i)` | absorb, receive, draw-in, sink |
| outward flow | `F(O(E))+Dir(...,+f)` | emit, expand, push-out, radiate |
| route | `Path(E*)` | route, path, course, channel, trace |
| conduit | `Path(E*) + Cap(Path)` | conduit, pipe, wire, lane, track |
| blockage | `Path(E*) + Block(...)` | blockage, dam, obstacle, wall |
| transfer | `Src(A) -> Path -> target(B)` | transfer, carry, pass, deliver |
| projection | `Read(A,BasisTarget)` | project, map, translate, cast |
| fanout | `Src(E) -> Path(A*)` | branch, distribute, broadcast, split-output |
| merge | `Path(A*) -> target(E)` | merge, converge, collect, combine |
| circulation | `Path(E*) + Closure` | loop, cycle, orbit, circulation |
| residue flow | `Flow + Residue(unsettled)` | leakage, drift, loss, unresolved-flow |

Reverse-read confidence:

```text
flow
  high as open support/tension movement, low as a specific geometric shape.

route/path/channel
  medium; path means ordered walk, not necessarily measured geometry.

transfer/projection
  high only when source, target, and basis are visible.
```

### Boundary, Containment, And Area

Boundary words should reuse the boundary/area work already in
`GlossaryEncodingVocabulary.md`. This table names the useful higher concepts
that can crystallize from those forms.

| Concept | Footing | Useful words |
| --- | --- | --- |
| source boundary | `Bd0(E)` | start-boundary, origin-edge, lower-bound |
| terminal boundary | `Bd1(E)` | end-boundary, terminal-edge, upper-bound |
| self boundary | `BdSelf(E)` | closed-boundary, self-contained, loop-bound |
| separator boundary | `BdArea(E)` | separator, border, divider, edge |
| bounded range | `Bd0(E)+Bd1(E)` | bounded, limited, interval, window |
| contour | `Path(E*) + Closure -> Bd(Path)` | contour, outline, loop, perimeter |
| interior | `Bd(E)+Side(inner)` | inside, interior, contained, within |
| exterior | `Bd(E)+Side(outer)` | outside, exterior, excluded, beyond |
| containment | `E inside Bd(F)` | contain, include, enclose, hold |
| exclusion | `E outside Bd(F)` | exclude, reject, omit, outside |
| area support | `Area?(Bd...,side?)` | area, region, face, surface |
| positive area | `Area+(compatible lane evidence)` | filled, positive-area, occupied-region |
| anti-area | `Area-(compatible inverse evidence)` | hole, void-region, anti-fill |
| mixed-lane residue | `Area?(...) + Residue(non-area)` | diagonal-residue, unsettled-region, shear-like residue |
| shell | `Bd0(E)+Bd1(E)+Area? between` | shell, band, buffer, margin |
| island | `Area+ inside Area- context` | island, retained region, inclusion |
| hole | `Area- inside Area+ context` | hole, cutout, void, exclusion |

Reverse-read confidence:

```text
boundary
  medium until source/terminal/self/separator context is visible.

inside/outside
  medium; needs side evidence or a chosen read convention.

area/hole/island
  high only when area and anti-area construction or crystallized definitions are present.
```

### Partition, Unit, Calibration, And Measure

| Concept | Footing | Useful words |
| --- | --- | --- |
| partition | `Part(E,n)` | divide, subdivision, tick-layout, bins |
| tick site | `P(tick@Part(E,n))` | tick, mark, notch, sample-site |
| unit comparison | `U(Comp(E,F))` | unit, measure, calibration, ruler |
| coarse unit | `UnitShape.ResolutionOf(source,n)` | coarse, grouped-unit, scale-unit |
| fine unit | `Part(unit,n)` | fine, detailed, subunit, finer-rung |
| count | `Count(Part(E,n))` | count, number, quantity, amount |
| ratio | `Comp(E,F)` | ratio, proportion, relative-size |
| rate | `Comp(change,basis)` | rate, speed, frequency, pace |
| tolerance | `Comp(E,target,Res(n)) + allowed Residue` | tolerance, allowable-error, slack |
| exactness | `Comp(E,target,Res(n)) + no relevant Residue` | exact, precise, settled-value |
| approximation | `Comp(E,target?,Res(n)) + Residue(delta?)` | approximate, about, roughly, near-enough |
| granularity | `Res(n) over support` | granularity, detail, coarseness, resolution |
| sample | `Read(E, selected sites)` | sample, measurement, probe, observation |
| quantization | `Read(E,Part(E,n)) + Residue` | quantize, discretize, bin, snap |

Reverse-read confidence:

```text
unit/measure/calibration
  high when comparison and basis are visible.

approximate/about/roughly
  medium; must preserve target uncertainty and residue.

exact
  high only relative to a requested resolution, never globally exact.
```

### Residue, Tension, Settlement, And Failure

| Concept | Footing | Useful words |
| --- | --- | --- |
| residue | `Residue(E?)` | residue, remainder, leftover, unhandled-detail |
| tension | `Tension(E?)` | tension, stress, pressure, unresolved-change |
| failed read | `Read(E,B) -> Tension/Residue` | fail, mismatch, reject, unresolved-read |
| partial read | `Read(E,B) -> E_read + Residue` | partial, incomplete, approximate-read |
| settlement | `Settle(E,B,Res(n))` | settle, resolve, satisfy, account |
| settled witness | `Settle(...) -> E_settled + Residue?` | solution, answer, stable-result |
| contradiction | `A + not-A over same read` | contradiction, conflict, impossible-state |
| incompatibility | `Comp(A,B) -> blocked residue` | incompatible, mismatch, conflict |
| compromise | `Settle(A+B) -> partial support + Residue` | compromise, tradeoff, negotiated-result |
| slack | `Capacity - Occupied > 0` | slack, margin, tolerance-space |
| pressure | `Tension over boundary/support` | pressure, drive, demand, force |
| release | `Tension -> route/settlement` | release, discharge, drain, relax |
| stability | repeated reads settle similarly | stable, robust, durable |
| instability | repeated reads leave shifting residue | unstable, fragile, sensitive |

Reverse-read confidence:

```text
tension/residue
  high if attached to a read surface. Weak if stored without provenance.

settlement/solution
  medium; must say which basis and resolution were used.

stable/unstable
  weak without repeated reads or perturbation evidence.
```

### Crystallization, Definition, And Reuse

| Concept | Footing | Useful words |
| --- | --- | --- |
| definition | `Def(N,E)+Prov(E)` | define, name, meaning, concept |
| crystallized read | `Cr(Read(E))` | crystallized, reusable, hardened |
| provenance | `Prov(E)` | provenance, origin-story, source-evidence |
| reference reuse | `Cr(Def(N,E); context=C)` | reuse, apply, instance, local-use |
| abstraction | `Read(E,lower-detail) -> Cr(general)` | abstract, general, category |
| refinement | `Cr(general) -> sharper E + Residue` | refine, specialize, detail |
| family resemblance | `Comp(A,B,Res(low))` | similar, like, related, analogous |
| metaphor | `Read(A through B basis) + Residue` | metaphor, analogy, transfer-sense |
| type | `Cr(Def(N,E)) + instances` | type, kind, class, category |
| instance | `Use(Cr(type), local E)` | instance, example, member |
| default | `Cr(E_default) + Open(override?)` | default, fallback, usual |
| exception | `Use(Cr(type)) + Residue(specific conflict)` | exception, special-case, outlier |

Reverse-read confidence:

```text
name/definition
  high only if body and provenance remain traversable.

type/category
  medium; categories are often low-resolution reads over examples.

metaphor/analogy
  intentionally residue-bearing. Clean decoding would be suspicious.
```

### Order, Sequence, And Time-Like Structure

Temporal words can be built from order without claiming clock time.

| Concept | Footing | Useful words |
| --- | --- | --- |
| ordered pair | `Order(A,B,basis)` | before, after, prior, later |
| sequence | `Path(step*)` | sequence, chain, list, timeline |
| first | `Bd0(sequence)` | first, start, initial, opening |
| last | `Bd1(sequence)` | last, end, final, terminal |
| during | `E inside interval/support` | during, while, within-time |
| since | `source boundary + continuing support` | since, from-then, originating |
| until | `continuing support + terminal boundary` | until, up-to, ending-at |
| repeat | `Rep(Control,E,Step,Residue?)` | repeat, again, cycle, recur |
| frequency | `Count(repeat)/basis` | frequency, rate, tempo |
| delay | `expected transition + later actual transition` | delay, lag, wait |
| duration | `S(t0,t1)` over time basis | duration, interval, timespan |
| simultaneity | `A@T + B@T` | simultaneous, together-in-time, concurrent |
| dependency order | `A must settle before B` | prerequisite, upstream, dependency |
| future potential | `target? + route?` | future, possible, predicted, planned |
| history | `Trace(Read/Run steps)` | history, memory, record, provenance |

Reverse-read confidence:

```text
before/after
  medium to high if the order basis is visible. Do not force it to temporal context.

simultaneous
  same-reference in sequence basis, not global equality.

future/history
  weak to medium; depends on timeline provenance.
```

### Comparison And Quality Words

Most quality words are low-resolution comparison reads. They should carry their
comparison basis or leave basis residue.

| Concept | Footing | Useful words |
| --- | --- | --- |
| greater | `Comp(A,B) -> A>B at Res(n)` | greater, more, larger, higher |
| lesser | `Comp(A,B) -> A<B at Res(n)` | less, smaller, lower, fewer |
| equal | `Comp(A,B) -> same at Res(n)` | equal, same, matching, level |
| high intensity | `Amount(E) high vs norm` | strong, bright, loud, intense |
| low intensity | `Amount(E) low vs norm` | weak, dim, quiet, mild |
| sharp | `Boundary/edge + narrow transition + high contrast` | sharp, pointed, cutting, crisp |
| smooth | `low contrast between neighboring reads` | smooth, gradual, continuous, blended |
| rough | `neighbor reads leave variation residue` | rough, jagged, noisy, uneven |
| dense | `many participations per support` | dense, crowded, packed, concentrated |
| sparse | `few participations per support` | sparse, scattered, thin, empty-ish |
| clear | `read settles with low ambiguity` | clear, obvious, distinct, legible |
| noisy | `read has high irrelevant residue` | noisy, muddy, cluttered, confusing |
| simple | `low part/dependency count at Res(n)` | simple, plain, easy, compact |
| complex | `many parts/dependencies/residue` | complex, complicated, rich, tangled |
| hard | `high resistance to change` | hard, rigid, constrained, difficult |
| soft | `low resistance/permissive boundary` | soft, flexible, yielding, gentle |
| fast | `high change per basis` | fast, quick, rapid, steep |
| slow | `low change per basis` | slow, delayed, gradual, shallow |
| accurate | `close to target at requested Res` | accurate, correct, faithful |
| wrong | `contradicts target read` | wrong, false, mistaken, off |

Reverse-read confidence:

```text
sharp/smooth/rough
  medium; they are often perceivable but basis-sensitive.

simple/complex
  weak unless the read level is stated.

accurate/wrong
  medium to high only relative to a target and requested resolution.
```

### Relation And Preposition-Like Words

These are included because many will soon become language prepositions, but at
this level they are still noun-like relation shapes that can be crystallized.

| Concept | Footing | Useful words |
| --- | --- | --- |
| interior relation | `E inside Bd(F)` | in, inside, within, contained |
| exterior relation | `E outside Bd(F)` | out, outside, beyond, excluded |
| support contact | `E supported by F` | on, upon, mounted-on |
| detachment | `E not supported by F + residue` | off, away-from-support |
| localized site | `E at X` | at, located-at, address |
| source relation | `Src(E)` | from, out-of, originating |
| target relation | `Target(E)` | to, toward, destination |
| path relation | `Path(A...B)` | through, via, along, across |
| side relation | `Side(E,basis)` | beside, next-to, alongside |
| opposition relation | `Oppose(A,B,basis)` | against, versus, contrary |
| co-participation | `A+B in shared frame` | with, together, among |
| part-whole | `PartOf(A,B)` | of, part-of, belonging-to |
| method relation | `Use(route/tool)` | by, via, through-method |
| purpose relation | `Goal(target)` | for, in-order-to, intended-for |
| interpretation relation | `Read(E as B)` | as, like, interpreted-as |
| comparison bridge | `Comp(A,B)` | than, relative-to, compared-with |
| ratio bridge | `Comp(A per B)` | per, each, rate-of |

Reverse-read confidence:

```text
in/on/at/from/to
  high only when the support or path role is visible.

of/for/by/as
  weak to medium; these are heavily context-shaped.

like
  intentionally low-resolution with residue.
```

### Operation Seeds Still Close To Nouns

These are not full verbs yet. They are operation-shaped nouns or process
surfaces that can become verbs later.

| Concept | Footing | Useful words |
| --- | --- | --- |
| addition surface | `Add(A,B) -> result + Residue` | sum, addition, combine-read |
| subtraction surface | `Remove(A from B)` | difference, removal, deficit-read |
| multiplication surface | `Product(A,B)` | product, area-like, compound-unit |
| division surface | `Comp(A per B)` | quotient, ratio, allocation |
| split surface | `E -> parts` | split, partition, branch, fork |
| merge surface | `parts -> E` | merge, union, join, fold |
| map surface | `input -> output correspondence` | map, transform, translation |
| filter surface | `Sel(E,Focus) + carry complement` | filter, selection, mask |
| clamp surface | `E constrained inside Bd` | clamp, limit, bound |
| threshold surface | `Comp(E,bound) -> transition` | threshold, trigger, gate |
| interpolation surface | `Interp(A,B,t)` | interpolation, blend, transition |
| smoothing surface | `Settle(rough, continuity basis)` | smoothing, rounding, blending |
| normalization surface | `Read(E,shared basis)` | normalize, adapt, align |
| sampling surface | `Read(E,selected sites)` | sample, probe, snapshot |
| prediction surface | `Read(history -> future?)` | prediction, forecast, guess |
| verification surface | `Comp(claim,evidence)` | verification, test, proof-read |

Reverse-read confidence:

```text
add/product/ratio
  high if unit and compatibility evidence are visible.

interpolation/smoothing
  currently weak; needs stronger curve/change footing.

prediction/verification
  medium; depends on claim/evidence/provenance structure.
```

### CAD-Oriented Useful Words From The Same Primitives

These are not ECAD-specific ontology entries yet. They are common CAD words that
should be expressible from the primitive ladder.

| Concept | Footing | Useful words |
| --- | --- | --- |
| vertex | `P(X) + incident boundaries` | vertex, corner-site, node |
| edge | `Bd(S)` or boundary over span | edge, side, line-boundary |
| corner | `two edge reads + shared site + angle residue` | corner, cusp, joint |
| face | `Area?(Bd...,side)` | face, surface, region |
| contour loop | `Path(E*) + Closure` | loop, outline, perimeter |
| offset curve | `Off(E,O(E),d,Res(n))` | offset, inset, outset, parallel |
| fillet | `Settle(corner, radius/continuity)` | fillet, rounded-corner |
| chamfer | `Replace(corner, span)` | chamfer, bevel, cut-corner |
| hole | `Area- inside Area+` | hole, cutout, void |
| island | `Area+ inside Area-` | island, retained-pocket |
| clearance | `Comp(distance, minimum, Res(n))` | clearance, spacing, gap |
| overlap | `Occ(A) + Occ(B) over shared support` | overlap, collision, interference |
| touch | `A and B share boundary/site` | tangent-contact, touch, meet |
| tangent | `same local direction at contact` | tangent, smooth-contact |
| normal | `O(E)` through basis | normal, perpendicular, orthogonal |
| layer | `support frame + stack order` | layer, level, plane |
| grid | `Part(Basis,n*)` | grid, lattice, tick-field |
| snap | `Read(E through grid) + Residue` | snap, quantized-placement |
| constraint | `Need(settlement condition)` | constraint, rule, requirement |
| datum | `Cr(reference basis)` | datum, origin, reference-frame |

Reverse-read confidence:

```text
edge/face/loop/hole
  medium to high with boundary and area evidence.

fillet/chamfer/tangent
  currently medium to weak until curve and interpolation footings improve.

clearance/snap/grid
  high if partition, unit, and comparison basis are visible.
```

## Reverse-Read Checklist

For every new crystallized word, ask these in order:

1. Can the word be rebuilt from earlier primitive structure or earlier
   definitions?
2. Does the footing expose the read surface, basis, and resolution?
3. Does missing detail remain as residue or tension rather than becoming
   hidden text?
4. Can a local reader discover the same shape without a global classifier?
5. If the word is decoded from the expression alone, does it come back as the
   same idea, a nearby idea, or a very different idea?

Suggested labels for audit rows:

```text
stable
  should reverse-read cleanly from structure

near
  may decode as a close neighbor and preserve the difference as residue

weak
  useful word, but current footing is too broad or underbuilt

blocked
  do not crystallize yet; a lower primitive is missing
```

Current strongest areas:

```text
position
shared reference
span when carrier evidence is present
lane-marked orientation
boundary with source/terminal/self context
area/anti-area when lane evidence is present
partition/unit/comparison
residue/tension attached to a read
definition/provenance/reuse
```

Current weakest areas:

```text
approximation without known target
inside/outside without side convention
selection with conditional stop/continue
curve, smoothing, interpolation, and acceleration
category/type without example frame
social/intentional words such as ownership, permission, promise, trust
```

## First Implementation Shape

The first small executable surface is resolution-centered and avoids becoming a
word classifier. `ResolutionDefinitionSurface` builds the sample ledger, and
`ResolutionReadSurface` discovers the facets from compound bodies and dependency
structure rather than from definition names. The sample set:

```text
partitioned span
  S -> Part(S,5) -> tick sites

unit comparison
  U(Comp(S,unit-span)) -> measurement read + residue

coarse read
  Read(E,Res(rough)) -> E_rough + Residue(sharp-detail?)

approximation witness
  Comp(E,target?,Res(n)) -> compatible + Residue(delta?)

omitted slot
  Slot(input,?) inside an otherwise readable structure

ambiguous read
  Read(E,B?) -> possible reads + Tension(unselected)

ladder refinement
  Rung(E_rough) -> Rung(E_fine) + correspondence + residue
```

Blind reconstruction for those samples hides the text handles and asks whether a
generic reader can recover:

```text
partition
unit/calibration
coarse-but-valid read
approximate compatibility
missing but meaningful slot
ambiguous read
refinement ladder
```

If this works, words such as `about`, `near`, `rough`, `exact`, `missing`,
`optional`, `default`, `detail`, `sample`, and `refine` can be built with much
less ambiguity.

The first pass works at the compact definition level. The next deepening step is
to lower compact bodies such as `Part(...)`, `Read(...)`, `Approx(...)`,
`Refine(...)`, and `Run(...)` into more native carrier/relation/unit-shape
surfaces where the footing is clear enough.
