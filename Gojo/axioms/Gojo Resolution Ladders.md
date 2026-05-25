# Gojo Resolution Ladders

This note captures the current working direction for values that are expressed
across multiple resolution levels. It is guidance for the static
`Gojo.Structures.ResolutionLadder` helpers and
future comparison, gating, percent, and stream-read work.

## 1. Core Idea

A value does not always need to be one carrier holding one large tick count. A
value can be a rooted structure of level carriers connected by scale/carry
relations.

For example, a base-10 value can be represented as:

```text
ones      Segment(ones,0,10)      -> Point(tens,0)
tens      Segment(tens,0,10)      -> Point(hundreds,0)
hundreds  Segment(hundreds,0,10)  -> Point(thousands,0)
```

The root says the levels participate in one value. The levels remain distinct
carrier sites so each resolution has its own value, tension, and residue.

```text
ValueRoot
  + ones
  + ones->tens relation
  + tens
  + tens->hundreds relation
  + hundreds
  + hundreds->thousands relation
  + thousands
```

This keeps the representation physically inspectable. The code helper may make
the ladder easier to create and read, but the Gojo structure is the source of
meaning.

The carry/scale relations are not a separate hidden "carry structure." They are
ordinary relations that route tension when their shape is compatible with the
current settlement rules, and remain inspectable structure otherwise.

## 2. Why Not One Carrier ID

It is tempting to say every level is the same carrier at a different
resolution. Current Gojo carriers are physical value sites. The ones level and
hundreds level need independent values and tension, so they should be distinct
carriers.

The shared value identity is carried by structure:

```text
shared root/holder
ordered level group
explicit carry relations
shared or compatible unit shape
```

The levels can share the root's unit shape so they are the same kind of
participation, while the radix relations express scale.

The root does not have to store creation information such as radix, level
count, or carry type. Those facts are recoverable from the attached level and
step relations when the structure is explicit enough. Code can also read the
same root through a supplied pattern, such as a decimal, binary, or custom
scale ladder.

## 3. Steps Are General Relations

A decimal carry ladder is only one case. A step can be any relation between
adjacent levels:

```text
Segment(level0,0,10) -> Point(level1,0)    carry-route convention
Point(level0,10)     -> Point(level1,1)    explicit 10:1 ratio surface
Point(level0,1)      -> Point(level1,10)   reversed/dividing ratio surface
Point(level0,-1)     -> Point(level1,0)    inverse/delta-like structural step
Point(level0,0)      -> Point(level1,0)    shared/additive/degenerate foothold
```

Only some of these shapes actively settle today. For example, the current
carrier settlement route uses an origin receiver:

```text
Segment(lower,0,n) -> Point(upper,0)
```

That does not mean other step shapes are invalid. They are structural surfaces
that later reads, projections, comparison lenses, or settlement extensions can
interpret.

For this reason, the helper should allow arbitrary step relations. Repeating
base-10 creation is just shorthand.

## 4. Whole Values And Partial Resolution

A decimal value such as 396 can be entered as separate level values:

```text
hundreds = 3
tens     = 9
ones     = 6
```

The composite read is calculated from the ladder:

```text
396 = (3 * 100) + (9 * 10) + 6
```

But partial resolution is also meaningful:

```text
hundreds = 3
tens     = 9
ones     = 0 or unread

read: 390-ish at tens resolution
```

The absence of lower detail should not be treated as fake precision. A read can
choose a coarser level and stop there.

## 5. Presence And Fill-Like Markers

The lowest level can act as a simple presence or fill marker when that is the
only detail needed:

```text
unit/presence level = 1
deeper levels       = unread or zero
```

This matches low-resolution situations such as:

```text
filled / not filled
occupied / not occupied
present / not present
dark / light
```

A later read can ask for deeper levels if the structure has them.

## 6. Percent And Whole Reads

A ladder makes a useful distinction between current composite value and the span
made available by a selected resolution.

In base 10:

```text
one level   span 0..10
two levels  span 0..100
three levels span 0..1000
```

So a percent read can be ordinary projection:

```text
Segment(valueRoot,0,100) -> Segment(percent,0,100)
```

If the tens level alone is set to 4, the value reads as 40 in a two-level
0..100 span. That gives 10% steps without pretending there are ones-level
details.

## 7. Comparison And Selection

Resolution ladders are promising for comparators and selection.

A comparison can read high levels first:

```text
compare thousands
if unresolved/equal, compare hundreds
if unresolved/equal, compare tens
if unresolved/equal, compare ones
```

This lets a system choose without reading unnecessary detail. If one candidate
is better at coarse resolution, the lower levels do not matter yet.

This should support:

```text
nearest / min / max reads
threshold support
goal nearness
budget exhaustion
attention routing
coarse branch selection
fine tie-break reads
```

The comparator should be relation-shaped. It should not become a hidden
operation enum that knows how to compare every possible ladder globally.

## 8. Branching, Gates, And Weighted Flow

Many future branch/gate mechanics can be built on ladder-like values:

```text
support ladder
confidence ladder
cost ladder
attention ladder
error ladder
budget ladder
```

Coarse support may be enough to open or close a gate. Fine support can be read
only when several candidates are close.

Weighted routing can use the same structure:

```text
input tension
  -> candidate A support ladder
  -> candidate B support ladder
  -> candidate C support ladder
```

The selected resolution decides how much discrimination is needed. This gives a
route toward adaptive selection without adding broad metadata fields.

## 9. Ladders As Reusable Patterns

The ladder pattern itself should be reusable:

```text
base-10 decimal ladder
base-2 binary ladder
percent ladder
signed delta ladder
confidence ladder
coarse-to-fine perceptual ladder
```

Instantiating a pattern should create ordinary carriers and relations. The
pattern can be a construction helper or a crystallized template, but the
instantiated structure remains inspectable.

This is similar to SIMD or data-frame thinking: one formula applies across
many levels or many values. But Gojo should preserve the relation structure
that makes the formula meaningful.

## 10. Multiple Ladders On One Root

One value root may have several ladder structures attached:

```text
decimal ladder: 1, 10, 100, 1000
binary ladder:  1, 2, 4, 8, 16
custom ladder:  positive/negative or uneven resolution
```

These are not exclusive. They are different resolution/read structures over
the same conceptual value root. Code may discover them by inspection or create
them deliberately for reuse.

The same root can therefore support:

```text
human decimal read
binary storage read
coarse perceptual read
domain-specific threshold read
```

## 11. Compatibility With Current Code

The first code foothold is `Gojo.Structures.ResolutionLadder`. It lives outside
`Gojo/Core` because it is a construction/inspection layer over primitive
carriers and relations, not a new kernel primitive.

It can create:

```text
Root              value holder
Levels            carriers sharing Root.UnitShape
Steps             arbitrary relations between adjacent levels
Structure         OrderedGroup(Root, level0, step0, level1, step1, ...)
```

Carry-route steps such as `Segment(lower,0,10) -> Point(upper,0)` are attached
directly to the adjacent level carriers, so ordinary carrier settlement
accounts for overflow. Ratio, inverse, or degenerate steps may be created as
structure before all of their settlement/read behavior exists.

The helper can read:

```text
composite value through supplied level scales
repeated-radix composite values
one level contribution on the root
composite location on the root
projection into another frame, such as percent
level and step participants from the root ordered group
```

This is intentionally a helper over ordinary Gojo structure. It should not be
treated as a wrapper class that owns the only meaning.

## 12. Forgiving Construction

Resolution ladder helpers should not treat malformed convenience input as a
program-halting error. A ladder is just structure. Missing, excessive, or coarse
inputs should produce smaller, repeated, truncated, or unresolved structure that
can still be inspected.

Current convention:

```text
zero requested levels      create no level carriers and return a root route
too few step ticks         repeat the last supplied step
too many step ticks        stop when adjacent levels run out
too few ratio steps        repeat the last supplied ratio
too many ratio steps       stop when adjacent levels run out
no supplied step pattern   use a zero step relation where a step is needed
too many digits            fill available levels and ignore the rest
invalid level read         read as zero on the requested root
```

The important point is not that these are mathematically final defaults. The
point is that construction remains a permissive structural shorthand. If the
structure is under-specified, Gojo should expose that as absent levels, zero
steps, tension, residue, or low-resolution reads, rather than hiding it behind
an exception.

## 13. Missing Fused Values

The current helper reads a multi-level value with code-side scale lists:

```text
value = level0 * scale0 + level1 * scale1 + ...
```

That is useful for tests and adapters, but it is not the final Gojo shape. A
resolution ladder wants to behave more like a fused value:

```text
(ones, tens, hundreds, thousands)
```

This is the same family of problem as a coordinate:

```text
(x, y)
(x, y, z)
(red, green, blue)
```

Current `OrderedGroup(holder, ...)` and relation-shaped port inputs can already
carry ordered multi-slot values. A position can be represented as an ordered
group of x and y locations, and multi-slot ports can import matching
relation-shaped inputs. What is still missing is a stronger first-class fused
value shape: one inspectable value identity whose slots are ordered carriers or
locations, whose slot count is itself structural, and whose interpretation can
be read by local relation structure rather than by an external scale array.

For ladders, that fused shape would let a decimal value be passed as:

```text
RootValue(ones=6, tens=9, hundreds=3)
```

without pretending the helper's `ReadComposite` method owns the meaning. The
root, slots, step surfaces, and chosen read lens would all remain traversable.

This also clarifies additive participant reads. `TryReadAdditiveParticipantsOn`
reads one explicit holder-origin ordered group. It does not choose a path
through an arbitrary branching graph. If structure contains:

```text
A -> B
B -> C
B -> D
```

then both `A,B,C` and `A,B,D` can be meaningful paths, but the choice between
them needs explicit path/group/selection structure. The additive group read is
a local shape reader, not a global classifier that decides which branch was
intended.

## 14. Open Questions

- How should lower-level unknown detail differ from explicit zero?
- Should there be a first-class unresolved level marker?
- How should signed ladders borrow downward or route inverse deltas?
- Which non-origin step relations should become executable settlement routes?
- How should a comparator expose coarse equality and request deeper levels?
- What structure should represent several alternate ladders on one root?
- Can ladder patterns harden as reusable unit-shape or relation templates?
- How should dense data streams attach to one level and distribute downward or
  upward through a ladder?
- What is the cleanest way to read a ladder as min/max capacity without
  confusing exclusive span with inclusive maximum?
- What exact relation shape should harden a tuple/fused value so `(x,y)` and
  `(ones,tens,hundreds)` are both single values with multiple inspectable
  slots?
