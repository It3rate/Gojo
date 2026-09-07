# Gojo Text Syntax: First Executable Foothold

Status: exploratory implementation, September 2026.

This document defines the first deliberately small text syntax that can be
parsed by `Gojo.Encoding`. It is not yet a canonical Gojo language. Its purpose
is to make simple numeric structures simple to write while forcing important
distinctions to remain visible when the structures become more elaborate.

The parser expands the text directly into ordinary Gojo elements. It does not
retain a semantic abstract syntax tree, class taxonomy, or operation opcode.
After parsing, meaning must remain discoverable from carrier identity, signed
locations, ordered relations, unit shapes, selected structure, action
structure, results, residue, and provenance.

For a review of the current guarantees, measured limitations, and proposed
next experiments, see [Gojo Syntax Review](GojoSyntaxReview-2026-09.md).
That review is design analysis, not an extension of the version-1 grammar.

This syntax is consequently a blend:

- near the bottom it resembles numeric notation;
- where a value needs context it resembles a structural notation;
- where a structure can change it resembles a small programming language.

That continuum is intentional. A large Gojo “number” can be a connected,
partly unresolved recipe rather than one scalar.

## First-pass commitments

The first parser makes these commitments:

1. There are no decimal literals. Values are signed integer ticks on carriers.
2. `$name` is a file-local alias. It preserves shared identity while parsing,
   then disappears. It is not a crystallization and is not a source of meaning.
3. `?name` is a persistent open slot within one parsed document. Reusing the
   name reuses the same `Placeholder` identity.
4. `A -> B` constructs an ordered Gojo relation. Order is never discarded.
5. Lane mnemonics such as `PF` are input shorthand. They expand to the current
   relation-shaped boundary/flow lane surface; the mnemonic is not stored as
   the meaning of the lane.
6. An allowed lane and an action between lanes are separate structures.
7. Applying an action records its selected scope, operation structure, result,
   residue, and provenance. The word “inverse” does not secretly dispatch on a
   runtime type.
8. Repetition is structural. Arithmetic identities made from repetition,
   inversion, addition, or multiplication are not flattened into opcodes.
9. Ladder normalization is not performed by the parser. An unnormalized value,
   a normalized digit surface, and the action that connects them can all remain
   inspectable.

## Document grammar

The implemented document form is:

```gojo
gojo 1;

let $local_name = expression;
root expression;
```

Line comments start with `//`. Names contain letters, digits, and underscores.
The file must declare syntax version `1`.

An expression is one of:

```text
$alias
?open_slot
signed_integer
symbol
constructor(expression, ...)
(expression)
expression -> expression
```

Relations associate from the left:

```gojo
root $a -> $b -> $c;
```

means:

```gojo
root (($a -> $b) -> $c);
```

Parentheses should be used whenever the intended relation frame might be
unclear.

## Primitive carrier, location, point, and segment forms

```gojo
gojo 1;

let $axis = carrier();
let $shared_tick = tick(7);

root at($axis, -3);
root point($axis, $shared_tick);
root span($axis, 0, 20);
```

The constructors are:

| Form | Expanded structure |
| --- | --- |
| `carrier()` | a fresh carrier with its own atom unit shape |
| `carrier(unit)` | a fresh carrier with the supplied stable unit shape |
| `tick(n)` | a shareable `TickRef` containing signed integer `n` |
| `at(C,n)` | signed location `L(C,n)` |
| `point(C,n)` | grounded point `P(C,n) = L(C,n) -> C` |
| `span(C,a,b)` | directed segment `S(C,a,b) = P(C,a) -> P(C,b)` |
| `A -> B` | an ordered relation whose left is `A` and right is `B` |

Two `at` forms using the same `tick(...)` alias share a coordinate site across
carrier frames. Two independently written equal integers do not share that
`TickRef` identity. Identity, coordinate equality, contact, and projection
equality therefore remain different reads.

Negative ticks are locations on the selected carrier. They do not by themselves
mean an opposed carrier face, reverse relation, inverse unit, subtraction, or
division.

## Grouping, slots, and local names

```gojo
let $inputs = ports(?left, ?right);
let $bindings = frame(
    bind(?left, point($axis, 3)),
    bind(?right, point($axis, 5))
);

root group($inputs, $bindings);
```

`group(...)`, `ports(...)`, and `frame(...)` currently create the same ordered
group shape. Their different spellings are reading cues in the source, not
stored tags. `bind(A,B)` is the ordinary relation `A -> B`.

`group_on(holder, ...)` creates that same group shape on an explicitly supplied
carrier, including an empty group. It is useful when several coordinate tuples
or other surfaces must preserve a shared authored holder. The first argument
must be a carrier; all remaining arguments must be Gojo elements. It does not
infer coordinate compatibility or turn the holder into a geometry tag.

This is a useful test of the source-of-meaning rule. A reader that needs to know
whether an ordered group is acting as a frame or a port surface must establish
that from its connected use, not from the constructor spelling, because the
spelling is gone after parsing.

## Unit shapes and repeated participation

```gojo
let $distance = carrier();
let $time = carrier();

let $distance_unit = unit_of($distance);
let $time_unit = unit_of($time);

let $area_unit = product_units($distance_unit, $distance_unit);
let $mixed_unit = additive_units($distance_unit, $time_unit);

root carrier($area_unit);
root carrier($mixed_unit);
```

`product_units(u,u)` preserves both participations. It does not silently reduce
the result to one occurrence of `u`. This is the structural footing beneath a
read such as `u^2 = u -> u`.

`resolution(unit,n)` expresses the current `UnitShape.ResolutionOf` operation:
one tick of the returned shape represents `n` ticks of its source shape. It is
a coarsening surface, not notation for “n partitions per source tick.” Values
that use several place-like rungs are better made with explicit carriers and
carry or ratio relations, as below.

The factor must be positive. Factor 1 preserves the source unit; nested factors
multiply and must fit the supported signed tick range. Zero, negative and
overflowing factors are located syntax errors.

Exact projection can leave a fractional **source** remainder even with integer
output ticks. In that residue context, `at($source,n) -> point($source,d)`
expresses `n/d` source ticks, with positive, reduced subdivision `d`; whole
remainders remain locations. This uses existing constructors and does not add
decimal literals or fractional carrier tension. See the
[mathematical conformance checkpoint](GojoMathConformance.md) and its
[executable fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/math-conformance.gojo).

`crystal(relation)` is available only when the supplied relation already exposes
a crystallizable unit shape. Crystallization stabilizes an already readable
promise. It must not be used merely to give an arbitrary fragment a shorter
letter.

`quotient_unit(expression)` explicitly captures a complete unit quotient:

```gojo
let $standard = point($distance, 1) -> point($time, 1);
let $rate_units = group(
    ports(point($distance, 1)), ports(point($time, 1)), $standard);
let $rate = carrier(quotient_unit($rate_units));
root group(at($distance, 7), at($time, 2), $rate, $standard);
```

The last group supplies a selected `QuantityQuotient` read with numerator,
denominator, output basis and standard. Parsing constructs the request; calling
the reader produces the witness. It reads 3 rate ticks plus 1 distance tick of
source residue. The output carrier remains unchanged.

The unit expression preserves both ordered factor groups and captures the
standard as provenance. It does not perform cancellation or conversions.
`ports()` in the numerator expresses reciprocal participation; empty groups in
both roles express a dimensionless promise under the selected standard.
Repeated denominator points remain repeated participation. A placeholder factor
or standard is valid in the expression, but cannot yet supply `quotient_unit`;
premature capture is a located syntax error. Keep the partial expression as a
root and apply explicit bindings before requesting that read.

See [quotient reads and limits](GojoUnitQuotientReads.md) and the executable
[quantity fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/quantity-quotients.gojo).

## Boundary and flow lanes

The first surface uses two independent summaries:

- boundary exposure: `P`, `N`, `PN`, or `S`;
- flow exposure: `F`, `I`, `FI`, or `None`.

```gojo
let $magnitude = carrier();

let $positive_forward_only = basis($magnitude, P, F);
let $full_four_lane_basis = basis($magnitude, PN, FI);

root $positive_forward_only;
root $full_four_lane_basis;
```

The readable occupied lanes are:

```text
PF  PI  NF  NI  PNone  NNone  SF  SI  SNone
```

These mnemonics are compact source marks for relation recipes. They should not
be mistaken for a final enumeration of all possible numeric dimensions.
Additional dimensions can be reached structurally without inventing a larger
flat enum.

An unsigned magnitude can occupy one lane:

```gojo
let $pf = lane(PF);
root value($magnitude, 12, $pf, $positive_forward_only, ?residue);
```

The magnitude must be nonnegative. Orientation comes from the occupied lane and
its basis, not from smuggling a second sign into the magnitude.

Most importantly, this basis says what is exposed or legal. It does not by
itself say that an inverse action may be performed. Presenting only the `PF`
lane and exposing no action is a structurally positive-only numeric surface.

## Ambient possibility and local access

An omitted lane is not proof that the corresponding mathematics does not
exist. Version 1 now separates a known ambient basis from the smaller ports and
actions exposed by one local tool:

```text
access(ambient, exposed_ports, capability_ports,
       blocked_attempt_ports, residue, provenance)
```

The expanded structure begins with the relation:

```text
ambient basis -> exposed ports
```

and keeps capability ports, blocked attempts, residue, and provenance beside
it. `AccessCapabilitySurface.TryReadOutwardPorts` returns only the two outward
surfaces; it does not hand the ambient basis to a consumer merely because the
owning structure retains it.

```gojo
let $axis = carrier();
let $ambient = basis($axis, PN, FI);
let $pf = lane(PF);
let $pi = lane(PI);
let $inverse_recipe = $pf -> $pi;

let $blocked_attempt = act(
    $pf,
    $inverse_recipe,
    ?unreached_inverse,
    $pi,
    ?capability_not_exposed
);

let $restricted_tool = access(
    $ambient,
    ports($pf),
    ports(),
    ports($blocked_attempt),
    $pi,
    ?tool_provenance
);
```

Here `PI` remains possible in the ambient basis. The empty capability port says
only that this tool exposes no action to it. The attempted target remains in
residue rather than being declared nonexistent. `ports()` is explicit empty
access. An unresolved capability surface is conceptually different, but version
1 requires a concrete ordered port group in this argument: passing
`?capabilities` directly is currently rejected. `ports(?capability)` preserves
one open capability slot; it does not mean an unknown number of capabilities or
an entirely unknown interface. General open interface patterns remain a design
question.

A wider tool may expose `lane_action($pf,$pi,...)`. That action references an
already possible lane; it does not create the lane universe. The executable
example is
[`ambient-access.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/ambient-access.gojo).

## Actions are scoped relations

The clarification motivating this syntax is that a lane transition is an
action, not merely another row in an enumeration. “Inverse” is best treated as
a verb whose effect is constrained by the selected structural scope.

The first parser therefore does not contain a universal `inverse(x)` built-in.
It provides a lane-specific action form plus two neutral structural forms:

```text
lane_action(source_lane, target_lane, operation, witness)
action(input_ports, operation, output_ports, residue_policy, provenance)
act(selected, operation, result, residue, provenance)
```

`lane_action(...)` creates the existing Gojo lane-transition/action surface,
including its source, target, operation evidence, and witness. `action(...)`
creates a general reusable transform surface. `act(...)` creates a witnessed
occurrence. All three expand to existing Gojo relation structures.

`act(...)` does not itself mutate carrier state or move ticks. It records a
selected/action/result claim as a `ReadWitness`. A material application still
needs explicit transfer, settlement, output, patch, or tension-flow evidence.
After parsing, a reader recognizes the action from the connected lane-action or
transform surface, not from the vanished spelling `act`.

The supplied result is a claim, not a result calculated or validated by the
parser. For example, the preservation fixture explicitly supplies its points
for five, two, and ten. Recognition as a `ReadWitness` alone does not prove
multiplication, capability admission, conservation, or execution. A later local
read must connect the operation's actual output and any mismatch to that claim.

### One-lane scope

If the selected surface is one lane, an inverse recipe can connect that lane to
the lane understood as its negation in the current algebra:

```gojo
gojo 1;

let $pf = lane(PF);
let $pi = lane(PI);
let $inverse_pf_recipe = $pf -> $pi;
let $inverse_pf = lane_action(
    $pf,
    $pi,
    $inverse_pf_recipe,
    ?why_this_inverse_is_legal
);

root act(
    $pf,
    $inverse_pf,
    $pi,
    ?unaccounted_change,
    ?application_provenance
);
```

This records `PF -> PI` as operation evidence inside a discoverable lane action.
It does not assert in the parser that every use of the word inverse must make
this move. If `$pi` already exists, the application result references it. If the
result is not yet settled, the result may be an open slot. Creation versus
discovery can remain an interpretation of the action until connected evidence
resolves it.

An algebra-like surface can connect the legal basis and allowed actions without
changing either mechanism:

```gojo
let $positive_lane_basis = basis($magnitude, P, FI);
let $pf_inverse_algebra = group($positive_lane_basis, $inverse_pf);
root $pf_inverse_algebra;
```

The basis exposes `PF` and `PI` as possible lanes but no move between them. The
grouped surface also exposes the action. By contrast, `basis($magnitude,P,F)`
exposes only `PF`; it neither exposes the inverse lane nor grants the inverse
action. A later algebra can add composition, identity, closure, and
blocked-action evidence in the same structural manner rather than growing a
central enum.

### Signed-orientation scope

At a wider scope, an inverse action can exchange exposed positive and negative
orientations while preserving the internal forward/inverse distinction:

```gojo
let $positive_side = group(lane(PF), lane(PI));
let $negative_side = group(lane(NF), lane(NI));

let $orientation_inverse = action(
    ports(?selected_orientation),
    $positive_side -> $negative_side,
    ports(?result_orientation),
    ?residue_policy,
    ?operation_provenance
);

root act(
    $positive_side,
    $orientation_inverse,
    $negative_side,
    ?orientation_residue,
    ?application_provenance
);
```

The operation is wider than a one-lane move because the selected and resulting
surfaces are wider. The language does not infer scope from a class name.

### Four-lane or dimensional scope

At the full four-lane scope, the corresponding inverse action may expose an
orthogonal outlet rather than merely moving occupancy to another lane:

```gojo
let $axis = carrier();
let $four_lanes = basis($axis, PN, FI);
let $other_axis = orthogonal($four_lanes);

let $dimension_inverse = action(
    ports(?source_axis),
    $four_lanes -> $other_axis,
    ports(?target_axis),
    ?dimension_residue_policy,
    ?dimension_provenance
);

root act(
    $four_lanes,
    $dimension_inverse,
    $other_axis,
    ?dimension_residue,
    ?why_this_is_orthogonal
);
```

`orthogonal(x)` expands to Gojo's current uncalibrated orthogonal outlet
surface. In Gojo, orthogonal means unaccounted for in the current structural
order, not automatically ninety degrees. The application and provenance must
carry the evidence that lets a later system read this particular outlet as a Y
axis, another electrical component, or something else.

These examples do not require four occupied lanes before another dimension can
be introduced. They say that the *reference scope of the action* is the wider
surface. This is also why a first-class multi-component value surface is a
likely later requirement: a single read may need to expose occupancy and action
across several connected lanes without flattening them into one signed scalar.

## Operations, inverses, and repetition

The word “inverse” covers several different structural ideas. The syntax keeps
these candidates apart:

- `dual(element)` asks for the element's existing structural dual;
- `reverse(relation)` reverses relation order;
- a contextual inverse action is an explicit `action(...)` recipe;
- an opposed carrier face belongs in carrier/unit structure;
- an additive inverse of a participating value belongs in an additive algebra;
- a multiplicative reciprocal belongs in a multiplicative algebra;
- an inverse action that undoes a prior action needs correspondence and history;
- a fractional repetition or power needs an explicit repeat/controller basis.

Consequently, “invert add is subtract” and “invert multiply is divide” are useful
algebra candidates, not parser rewrite rules. They become valid when connected
operation structures establish those laws. The prevalence of mathematical
duals may indeed follow from primitive inversion, but the syntax should not
erase which kind of inversion was selected.

Repetition uses:

```text
count(carrier,n)
repeat(selected,controller,transition,feedback,residue[,timeline])
```

For example:

```gojo
let $counter = carrier();
let $input = ?input;
let $output = ?output;
let $step = $input -> $output;

root repeat(
    $step,
    count($counter, 4),
    $step,
    $output -> $input,
    ?repeat_residue,
    ?repeat_timeline
);
```

`RepeatAdd` and `RepeatMultiply` should therefore be understood as candidate
compositions: a repeat mechanism selecting an additive or multiplicative action.
They need not become primitive tokens. Feedback, stopping, residue, and timeline
remain inspectable even when a familiar arithmetic read can be derived.

## Preservation, history, and rollback

An action witness does not decide whether its input was preserved, consumed,
changed in place, or copied. Those are different structural outcomes:

- an observation leaves its selected source available and records a result;
- a same-identity change has distinct before and after evidence on one carrier;
- a fork result explicitly contains the retained source and a new identity;
- a material duplication additionally needs accounting for introduced
  participation;
- a rollback uses historical evidence and is not automatically the mathematical
  inverse of the earlier operation.

Version 1 exposes the existing time structures as:

```text
step(time, start, end, before, transition, after, residue)
timeline(time, initial[, step...])
```

For a same-identity change claim:

```gojo
let $number = carrier();
let $clock = carrier();
let $before = point($number, 5);
let $after = point($number, 10);
let $change = act($before, $scale_action, $after, ?residue, ?provenance);
let $change_step = step($clock, 0, 1, $before, $change, $after, ?residue);
root timeline($clock, $before, $change_step);
```

The literal before and after points remain stable when the carrier's live value
later changes. That is stronger than retaining only the mutable carrier identity.
It is not yet a general immutability guarantee: `TickRef` is intentionally
retargetable, and a timeline can still be given mutable structures. Durable
history needs an explicit snapshot or crystallization boundary, and a corrected
memory should normally append a new interpretation rather than rewrite the old
witness.

An explicit fork can return `group($source,$new_identity)`. Merely referencing
the source in two places is not material duplication; conservation and source
cost remain open until an accounting surface settles them.

For a lossy erasure, a rollback recipe can read `history -> restored`. This is
structurally different from `reverse(erasure_recipe)`: the history is required
to recover information the operation did not preserve. The executable cases
are in
[`preservation.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/preservation.gojo).

## Partition ladders and relative digits

Suppose one fine carrier is partitioned into twentieths of a millimetre and a
coarser carrier represents whole millimetres. The important structure is not
the spelling `$mm_20`; it is the shared unit shape and carry relation:

```gojo
gojo 1;

let $fine = carrier();
let $whole = carrier(unit_of($fine));
let $twentieths_to_whole = point($fine, 20) -> point($whole, 1);

// Raw fine-rung observation: thirty-two fine ticks.
let $raw = point($fine, 32);

// Relative, place-like read: one whole tick plus twelve fine ticks.
let $relative = group(
    point($whole, 1),
    point($fine, 12),
    $twentieths_to_whole
);

root group($raw, $relative, $twentieths_to_whole);
```

Both observations can read as the conventional value 1.6 mm through the stated
ladder. But the relative form is `(whole:1, fine:12)`, not the redundant tuple
`(1,16,32)`. It behaves like place value: the higher digit records carried
participation and the lower digit records local residue.

The parser deliberately leaves `32` as `32`. A non-mutating normalization read
can expose `1` plus `12` with its basis, correspondence, and residue while the
source remains unchanged. Material carry instead redistributes participation
through settlement and needs selected input, carry route, accepted
participation, remainder, result, and provenance. A future helper must expose
which of these occurred rather than silently rewriting the source.

The same structure allows a finer rung to be added later without rewriting the
older value. A lower-resolution read can ignore the new rung; a high-resolution
read can follow the new correspondence. Refinement adds information instead of
making the valid coarse read false.

## Unknown, absent, zero, and forbidden

The [unit quotient fixture](GojoUnitQuotientReads.md) applies these distinctions
to ordered numerator and denominator groups. An open factor remains in its
role, and an explicit dimensional cancellation read retains the chosen standard
and every pairing. It does not add an implicit commutative rewrite to parsing.

The syntax makes `?slot` available because these states must not collapse:

- `point(C,0)` is known zero on carrier `C`;
- `?result` is an open result slot;
- omitted optional syntax may mean that a surface was not recorded;
- residue can preserve participation not accounted for by the selected read;
- a blocked or illegal lane is evidence that a proposed participation was not
  accepted;
- a positive-only basis exposes no inverse action, but that is not the same as
  an inverse value of zero.

This matters for a net that exists before its board trace. The net, intended
route, pins, software role, prospective copper, open geometry, and constraints
can be connected in one larger structure. The missing copper is an open or
residual part of that structure, not a second nullable object unrelated to the
net.

## Constructor reference

The [local blend fixture](GojoLocalBlendFixture.md) exercises the existing
vocabulary with partial construction, calibrated contributions, a retained
alternative, conditional claim mismatch, explicit gain feedback, and history.
Its test driver performs the local reads and perturbations; parsing the
document does not execute that schedule or establish a general learning law.

In particular, a partial target frame can be written as
`point($signal, 0) -> ?endpoint`. Binding a grounded point to `?endpoint`
completes that frame without accepting an unknown as an integer coordinate.

The version-1 parser currently accepts:

| Category | Constructors |
| --- | --- |
| carriers and sites | `carrier`, `tick`, `at`, `point`, `span` |
| primitive structure | `A -> B`, `group`, `ports`, `frame`, `bind` |
| structural views | `dual`, `reverse`, `orthogonal` |
| unit shapes | `unit_of`, `resolution`, `subdivision`, `product_units`, `additive_units`, `quotient_unit`, `crystal` |
| boundary/flow | `basis`, `lane`, `value`, `lane_action` |
| access and change | `access`, `action`, `act` |
| repetition and history | `count`, `repeat`, `step`, `timeline` |

The small vocabulary is a first pass, not a promise that each constructor
deserves permanent syntax. A constructor should survive only when it makes a
stable Gojo relation recipe easier to read without becoming the source of that
recipe's meaning.

## Known questions exposed by this pass

This pass intentionally leaves several questions open:

1. **Contextual inverse surface.** We can already write scoped action recipes,
   but Gojo does not yet have one settled structural law that derives the
   appropriate inverse recipe from arbitrary selected structure.
2. **Multi-component values.** Boundary/flow occupancy is readable, but a
   general crystallizable value spanning several lane or dimensional components
   needs more exercise. The local blend fixture is a generic partial-component
   example, not yet a general crystallizable multi-component value.
3. **Action closure.** An algebra should expose which actions are admitted,
   their composition, identities, inverses, blocked applications, and residue.
   A list of legal lanes is insufficient.
4. **Ladder settlement.** Explicit [material refinement and borrow](GojoMaterialRefinement.md)
   now retain packet conservation, unavailable demand and remainder. General
   automatic ladder admission and fractional tension settlement remain open.
5. **Fractional repetition.** Treating reciprocal power as inverse repetition
   needs a controller basis and settlement law, not a special spelling.
6. **Text round-trip.** Version 1 parses text into structure. A canonical
   serializer is deliberately deferred until enough structures show which
   aliases improve readability without masquerading as crystallizations.
7. **Numeric encoding.** [Version 1 graph transport](GojoNumericFormat.md) now
   preserves these structures, slot sharing, unit descriptors and attachment
   ownership. Restoration creates an independent local world without settling
   it. All current syntax fixtures round-trip; this does not supply a canonical
   text writer or persistent identity across independent packets.
8. **Historical stability.** Timelines preserve distinct before/after evidence,
   but current elements can still contain mutable carriers or retargetable tick
   references. A durable-history boundary must say what is snapshotted,
   crystallized, merely referenced, or later reinterpreted.

These are findings for continued Gojo validation, not permission for the syntax
layer to patch around missing kernel laws. If one becomes a required primitive,
it should be proposed and appraised at the Gojo level.

## September 6: composition and finer material bases

`subdivision(unit,n)` captures a finer tick, `n` of which correspond to one
source tick. Like `resolution`, it requires a positive integer, preserves flow
face/provenance and checks combined factor overflow. Its relation surface
records scale correspondence; it is not a denominator dimension.

The [composition fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/quotient-composition.gojo)
authors nested quotient units and requests in ordinary groups. Explicit kernel
expansion/cancellation witnesses select equivalent output units; the parser
does not silently simplify units or execute division. The
[material fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/material-refinement.gojo)
authors coarse/fine calibration spans, transfer requests and an open request.
Its tests execute refinement, coarsening and borrowing both before and after
numeric restoration. Syntax and these new kernel mechanisms are in step.

## Bounded quantities and communication focus

The [bounded-quantity fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/bounded-quantities.gojo)
uses existing `group`, `ports`, placeholders, spans and affine calibrations.
A bounded sample connects its slot, support and evidence; affine reads connect
an offset and calibrated contributions. Shared slots remain shared after
restoration. No uncertainty tag, probability field or new parser constructor
supplies the semantics.

Explicit kernel reads perform combination, product/quotient enclosure and
conformance. The fixture also retains known fine mileage outside a coarse
communication focus, and records an actual material transfer separately from
the goal and before/after observations. See the
[structural contract and limits](GojoBoundedQuantityFixture.md).

## Graphic structure exercise

The [geometry review](GojoSyntaxGeometryReview-2026-09.md) compares authored
parameters, directed segments, native line/circle carriers, partial sections,
Bézier construction and frieze generators with current Blocks and CAD reads.
Its three fixtures preserve shared sites, holders, control dependencies and
open transformation obligations through numeric restoration. `group_on` is
the only parser addition; no curve or pattern opcodes are introduced.

The review distinguishes representing a construction from evaluating it, and
records ambiguities in compact curve recognition rather than presenting those
heuristics as complete geometry laws.

The follow-up [affine construction fixture](GojoAffineConstructionFixture.md)
supplies the explicit interpolation contract: sample, ordered parameter frame,
allowed support and ordered coordinate basis. It also exercises derivative
constraints and exposed motif correspondence using existing syntax. Exact
fractional results, partial coordinates and computed witness chains survive
numeric restoration; the parser gains no evaluation or geometry constructor.

The [curve replay walkthrough](GojoCurveReplayWalkthrough.md) connects those
requests to versioned input publication, application dependencies, pending
replay and a timeline. The new `bezier-replay.gojo` fixture uses the existing
syntax throughout, including the interval in which the current sample is
unavailable while its historical value remains readable.
