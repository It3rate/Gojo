# Gojo Placeholders And Repetition

This note records the first placeholder and repeat foothold.

## Placeholder

A placeholder is the lowest-resolution element site:

```text
identity only
no carrier
no basis
no value
no unit shape until bound or constrained
```

The identity is important. `A * A` and `A * B` are different structures even
before `A` or `B` has a concrete carrier, relation, or value. A placeholder
also has an opposed face so dual participation can stay visible while sharing
the same abstract slot identity.

## Binding

Binding is ordinary relation-shaped structure:

```text
Placeholder(A) -> concrete element
```

The concrete element can be any `IElement`: a carrier, a carrier location, a
point surface, a segment, a port, a whole relation pattern, or another
placeholder. Binding is not assignment in hidden code. It is a relation that a
local pattern helper can read to instantiate a frame.

Unit-shape descriptor slots are also relation-shaped slots:

```text
DescriptorSlot(0) -> concrete descriptor or element
```

They remain the existing unit-shape algebra surface, not a new metadata table.
The pattern helper can now bind them the same way it binds identity
placeholders, so a bare unit-shape formula can be rebound to a concrete
provenance or value surface without changing the crystallized unit-shape
encoding. This is a bridge between "slot" as a repeat placeholder and "slot" as
an algebraic unit-shape descriptor.

The helper reads both forms through one local slot-surface pass:

```text
slot surface -> concrete element
```

At the moment, a slot surface can be an identity placeholder or a unit-shape
descriptor slot. The source of truth is still the element on the left of the
binding relation. The code-side helper only recognizes and substitutes that
visible surface.

Slot discovery is also local traversal:

```text
read slots(pattern) => first distinct slot surfaces found inside pattern
```

This gives equation and repeat helpers a way to ask which sites are open
without storing a separate list of ports. Descriptor slots are treated as whole
surfaces during this read; the helper does not descend into their marker
relation.

Partial binding is valid structure. If a frame does not supply every open
slot, application leaves the unbound slot surfaces in place:

```text
pattern:      A -> B
binding:      A -> concrete
application:  concrete -> B
unbound:      B
```

Completeness is therefore a read, not a precondition. This is important for
equation search, workspace experiments, and sensory data frames where missing
or low-resolution values should remain inspectable instead of rejecting the
whole structure.

## Repeat

The first executable repeat helper does not create a durable repeat object.
It takes:

```text
pattern element
binding frame 0
binding frame 1
...
```

and produces ordinary instantiated Gojo elements. For example:

```text
pattern:
  CurrentBoundary -> NextOrigin

frame 0:
  CurrentBoundary -> P(ones,10)
  NextOrigin      -> P(tens,0)

frame 1:
  CurrentBoundary -> P(tens,10)
  NextOrigin      -> P(hundreds,0)
```

The result is the visible carry chain:

```text
P(ones,10) -> P(tens,0)
P(tens,10) -> P(hundreds,0)
```

This is intentionally mundane. A power-like repeat is the same mechanism with
the same placeholder bound repeatedly. A recurrence can later use slot-to-slot
bindings between frames, such as `A_next -> B_current`.

## Frame Continuation

A transition is also relation-shaped binding, but its right side may contain
placeholders from the current frame:

```text
A -> B
B -> A -> B
```

Applying the current frame to these transition bindings creates the next frame:

```text
current:
  A -> first
  B -> second

next:
  A -> second
  B -> first -> second
```

This is the first foothold for recurrence-like structure. It can describe a
Fibonacci-style state shift, a repeated manufacturing step, a commute episode
that carries forward yesterday's residue, or a power machine that reuses the
same operand several times. The transition does not mutate an equation during
ordinary tension flow. It produces the next set of bindings at a frame
boundary.

## Step Boundary

Live placeholder rebinding should happen at frame boundaries:

```text
bind current frame
settle values and tension
read residue/output
advance bindings for the next frame
```

Flow-time rebinding is intentionally avoided for now. If a structure appears to
need it, that is probably a sign that the model needs a finer time step or
should expose unresolved tension.

## Controller Surfaces

The first repeat controller helpers create ordinary phase carriers and
relations:

```text
wrap        P(phase,k) -> P(phase,0)
clamp       P(phase,k) -> P(phase,k)
cycle count P(phase,k) -> P(cycles,0)
```

These are not symbolic loop modes. They are the same carrier settlement shapes
used elsewhere in Gojo. A wrapped phase can select one repeated frame by its
current residue. A cycle counter can absorb crossed wraps. A clamp exposes
rejected continuation as tension.

Ping-pong is currently only a read over a doubled wrap period:

```text
P(phase,2k) -> P(phase,0)
read phase as 0..k..0
```

That keeps the reflected value inspectable as a local read while we decide how
much of the direction reversal should become executable relation structure.

## Port And Operation Patterns

Placeholders can stand for whole port, input, and output surfaces:

```text
operation pattern:
  (XPort -> YPort) -> Output

input pattern:
  XInput -> YInput
```

Binding those placeholders produces ordinary relation structure before the
existing projection, operation, and crystallization reads run. This lets a
single operation pattern serve many repeated frames without introducing a
parallel equation executor:

```text
XPort  -> P(x,1) -> P(Unknown,1)
YPort  -> P(y,1) -> P(Unknown,1)
Output -> P(result,1)
XInput -> L(x,3)
YInput -> L(y,4)
```

The helper layer is only an adapter. Once instantiated, Gojo sees normal port
and operation relations.

## First-Class Repeat Surface

The broader repeat model should not collapse to a hidden code loop. Repetition
is one of the primary Gojo phenomena, alongside carriers, partitions, relations,
and unit-shape structure. A repeat can copy a pattern, advance a phase, carry
overflow, settle tension, compose equations, perturb a shape, select a frame, or
feed an output back into a later input.

The repeat surface therefore starts as connected structure:

```text
repeat root
  selected structure
  controller axis / count / phase / equation
  transition surface
  feedback frame
  residue or stop frame
  witnessed timeline
```

The selected structure is the thing being repeated. It may be a carrier
location, relation, equation surface, pattern, claim, carry route, or whole
nested repeat. The controller is not restricted to a literal count. It may be:

```text
P(repeat,0) -> P(repeat,n)        literal count/span
P(phase,k)  -> P(phase,0)        wrap controller
P(phase,k)  -> P(cycles,0)       overflow/cycle controller
parameter surface -> count site  count derived from another equation
unresolved slot                      count not known yet
```

This keeps `eqn(params, feedback, residue)^3` physically interpretable without
making `^3` a metadata tag. The exponent-like part is a controller relation on a
repeat axis; the equation remains ordinary selected structure.

The transition is the local motion from one repeat state to the next. It may be
an equation composition step, a product participation, a structural
perturbation, a carry relation, or a claim/residue route. Feedback is just
ordinary structure that says which outputs, residues, or changed sites are fed
into a later frame. Residue is not failure; it is the visible reason repetition
may continue, stop, branch, or remain unresolved.

The witnessed timeline uses the same shape as other step processes:

```text
time or repeat span
before evidence
transition evidence
after evidence
residue evidence
```

A repeat surface can exist before it has a timeline. That represents a repeat
intention or repeat-shaped surface. A timeline represents one traversal or one
inspection of that surface. Different traversals can share the same selected
structure and controller but produce different residue, resolution, or feedback
evidence.

This lets several existing mechanisms be seen as one family:

```text
StructuralPattern.Repeat  selected pattern + binding frames
RepeatController          phase controller + selected frame
ResolutionLadder          repeated carry/ratio transition across levels
EquationSequence          selected equations + output-as-next-input
OperationFollow.Settle    equality claim + residual feedback
Extrema/Proximity         repeated pairwise choice over candidate fields
Carrier settlement        repeat-until-quiet over attached relations
UnitShape.Product         repeated product participation
```

The current implementation foothold is `RepeatSurface`: it builds and reads the
surface above, but it does not execute the repeat. Execution belongs to local
interpreters that already know how to read the selected transition, such as
equation prediction, carrier settlement, structural pattern application, or
claim-based follow. The repeat surface gives them a common knowable skeleton.
`EquationPower` is the first repeated-product interpreter on that skeleton:
literal count controllers read as powers, while unresolved controllers remain
ordinary repeat surfaces.

Power controllers can now derive that count from several visible surfaces
without becoming a controller enum:

```text
literal span                       S(repeat,0,n)
visible count                      L(count,n) or P(count,n)
equation read                      equation + supplied input + prediction + span
resolution search                  selected sample input from the latest round
claim tension                      readable violation or residual magnitude
```

The important property is that each controller still carries its evidence. A
power repeat read now puts controller evidence inside the timeline initial
frame:

```text
product identity
controller evidence:
  original controller
  resolved count location
  resolved count span
  reason surface
  reason residue
```

An equation-derived count keeps the source equation, input, prediction,
resolved span, and residue. A resolution-derived count keeps the search and
selected sample. A claim-derived count keeps the claim surface and readable
mismatch. If none of those shapes yields a nonnegative count, the repeat
remains inspectable rather than pretending to execute.

If the equation-derived controller leaves its count input open, a power read can
supply the same input being powered. This gives `x^x` a first whole-tick
foothold without adding a symbolic exponent operation.

Longer-term hopes:

- A general repeat/power machine where `x^n` is repeated product participation
  over a controller axis, with missing or equation-derived `n` left visible.
- Solver loops where claim residue drives continuation and the timeline records
  every relaxation or narrowing pass.
- Structural perturbation families where each frame scales, shifts, rotates, or
  otherwise changes the selected relation before the next frame.
- Algorithmic loops represented as selected structure plus transition and
  feedback ports, not as opaque executable code.
- Repeat controllers that can be literal, phase-based, overflow-based,
  tension-based, or produced by another equation surface.
- Shared inspection over repeated processes so a caller can ask "what repeated,
  what changed, what fed back, and what residue remains?" without knowing the
  concrete repeat species.
