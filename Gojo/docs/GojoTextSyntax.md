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

`crystal(relation)` is available only when the supplied relation already exposes
a crystallizable unit shape. Crystallization stabilizes an already readable
promise. It must not be used merely to give an arbitrary fragment a shorter
letter.

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

The parser deliberately leaves `32` as `32`. Normalizing it to `1` plus `12`
would be a material action with selected input, carry route, accepted
participation, remainder, result, and provenance. A future normalization helper
should record that witness rather than silently rewriting the source.

The same structure allows a finer rung to be added later without rewriting the
older value. A lower-resolution read can ignore the new rung; a high-resolution
read can follow the new correspondence. Refinement adds information instead of
making the valid coarse read false.

## Unknown, absent, zero, and forbidden

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

The version-1 parser currently accepts:

| Category | Constructors |
| --- | --- |
| carriers and sites | `carrier`, `tick`, `at`, `point`, `span` |
| primitive structure | `A -> B`, `group`, `ports`, `frame`, `bind` |
| structural views | `dual`, `reverse`, `orthogonal` |
| unit shapes | `unit_of`, `resolution`, `product_units`, `additive_units`, `crystal` |
| boundary/flow | `basis`, `lane`, `value`, `lane_action` |
| change | `action`, `act` |
| repetition | `count`, `repeat` |

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
   needs more exercise.
3. **Action closure.** An algebra should expose which actions are admitted,
   their composition, identities, inverses, blocked applications, and residue.
   A list of legal lanes is insufficient.
4. **Ladder settlement.** Carry relations exist, but importing and normalizing
   arbitrary fine-rung values should leave a complete conservation witness.
5. **Fractional repetition.** Treating reciprocal power as inverse repetition
   needs a controller basis and settlement law, not a special spelling.
6. **Text round-trip.** Version 1 parses text into structure. A canonical
   serializer is deliberately deferred until enough structures show which
   aliases improve readability without masquerading as crystallizations.
7. **Numeric encoding.** A later storage and transport surface should encode
   the same element identities, signed locations, ordered relations, unit
   shapes, shared tick references, open slots, residue, tension, and provenance
   using integer-oriented records. It must remain a reversible projection of
   Gojo structure, not a floating-point collapse, text hash, or second semantic
   enum model.

These are findings for continued Gojo validation, not permission for the syntax
layer to patch around missing kernel laws. If one becomes a required primitive,
it should be proposed and appraised at the Gojo level.
