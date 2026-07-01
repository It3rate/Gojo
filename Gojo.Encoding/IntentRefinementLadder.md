# Intent Refinement Ladders

Design status: active experiment.

This note records the current direction for using Gojo structure as both an
intent encoding and a solution surface. The goal is to preserve why a solution
exists, not only the final settled shape.

## Core Rule

A refined rung does not repeat the previous intent as a new independent
statement, and it is not just a detached reference to it. It elaborates the
previous rung by using that rung's settled structure as the basis for a sharper
read.

```text
rung 0:
  A -> B

rung 1:
  read rung 0 through obstacle/clearance basis
  result: A -> B with obstacle tension visible

rung 2:
  read rung 1 through repair/branch basis
  result: A -> entry -> branch -> exit -> B
```

The A and B of rung 1 are derived from rung 0 because rung 0 remains the basis
being elaborated. The refined rung should not mint a second unrelated A-to-B
goal unless the intent has actually forked.

This is similar to a resolution ladder. A higher rung can remain true while a
lower rung becomes stale. The lower rung is a more resolved interpretation of
the earlier rung under additional basis, constraint, or local evidence.

## PCB Example

For PCB routing, the ladder can be both the editable encoding and the solving
surface:

```text
base intent:
  connect pad A to pad B

constraint refinement:
  connect A to B while preserving clearance from obstacle trace X

failed direct read:
  straight A -> B intersects occupied/forbidden structure
  residue/tension: blocked direct span

repair refinement:
  leave before the obstacle
  flow through an alternate carrier
  rejoin after the obstacle
  reach B
```

Different repairs are refinements of the same parent intent:

```text
same-layer route:
  A -> entry -> side lane -> exit -> B

via route:
  A -> entry -> top-to-bottom outlet -> bottom layer -> bottom-to-top merge -> B

zero-ohm bridge:
  A -> pad -> component bridge -> pad -> B
```

The final copper, vias, or bridge component are materialized reads from the
ladder. They are not the only source of truth. The ladder preserves the route
intent, the constraint that forced the route, the chosen repair, and any
remaining residue.

## Change And Staleness

When a dependency changes, the stable upper rungs can remain while lower
settlements become stale:

```text
obstacle removed:
  rung 0 still says A -> B
  rung 1 no longer produces obstacle tension
  detour/via/bridge repair rungs are stale
  direct A -> B can settle again
```

When a better path appears, the parent intent remains:

```text
old rung 2:
  via route settles the obstacle

new rung 2:
  same-layer route settles the obstacle with less residue

unchanged:
  rung 0 A -> B
  rung 1 A -> B under clearance basis
```

This preserves intention without freezing the first found geometry.

## Solver Shape

A local solver can operate by reading ladder tension:

```text
1. Read current rung.
2. If residue/tension is low enough, keep it.
3. If blocked-flow tension appears, wake compatible repair shapes.
4. Bind each repair shape to local attach, outlet, branch, and merge evidence.
5. Create candidate refined rungs.
6. Prefer the candidate with lower residue/cost.
7. Keep failed candidates as inspectable residue when useful.
```

The repair shape is a small local intelligence. An `around`-like shape does not
need to know PCB routing as vocabulary. It needs enough structure to recognize:

```text
goal continuity
blocked middle
entry before blockage
alternate carrier or lane
exit after blockage
rejoined continuity
```

The PCB domain then supplies physical carriers, legal layers, clearances, vias,
pads, bridge components, costs, and manufacturing rules.

## Encoding Requirements

An intent ladder should keep these pieces visible:

- source basis: the previous rung or its settled result
- focus/basis read: what new constraint, perspective, or repair was applied
- result: the elaborated structure
- correspondence: how old participation maps to new participation
- residue/tension: what remains unresolved or costly
- provenance/timeline: what evidence or change produced this rung

The refined result may contain the previous result as a participant. This is not
semantic duplication. It is the physical sign that the new rung is an
elaboration of the prior rung.

## JSON Sketches

The current JSON sketches live in:

```text
Gojo.Encoding.Tests/Fixtures/EncoderJson/grammar/intent-refinement-ladder-pcb-route.json
Gojo.Encoding.Tests/Fixtures/EncoderJson/grammar/intent-refinement-ladder-message-relay.json
```

These are not the final low-level compact relation interchange. They are a
human-readable ladder interchange for inspecting sectioning, basis elaboration,
and cross-domain mapping fits.

Each file keeps the same top-level shape:

```text
sourceId
encodingNote
languageHints
carriers
sections
```

The `sections` array carries the ladder:

```text
base-intent
constraint-elaboration
settlement-refinement
```

The `basis` field connects a lower section to the section it elaborates. Inside
section bodies, `use` means the previous structure is shared forward into the
new section. It is an interchange convenience for basis sharing, not a claim
that the Gojo meaning is a detached text pointer.

The message relay example also includes `mappingFits` to show the high-level
language behavior:

```text
deliver = exact structure with context
send    = partial source/conveyance fit
move    = partial transform-only fit
through = preposition carrier fit
```

This is intentionally tolerant of imperfect metaphorical mappings. A perfect
structural fit should map cleanly; a metaphorical or partial fit should preserve
which pieces matched and which pieces remained open.

## Open Questions

The current detour experiment treats an obstacle as a blocked segment. That is
useful but incomplete. More physical versions should try:

```text
inverted flow on the primary carrier
forbidden/removed region inside the route span
clearance field that creates tension near occupied copper
capacity/width constraint that leaves no legal positive flow lane
```

The format should allow ambiguity when ambiguity is useful. A branch can be an
unspecified orthogonal escape. A stricter branch can carry positive or inverse
outlet evidence. The ladder should preserve both possibilities as readable
structure, then let local tension and domain rules decide which one is enough.
