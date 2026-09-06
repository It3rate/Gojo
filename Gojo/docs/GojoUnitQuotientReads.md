# Explicit unit quotient reads

Status: a bounded executable unit-expression surface, September 5, 2026.
The user clarified that dimensional reordering should be an **explicit read**.
Standards such as miles/hour and litres/100 km have chosen order and calibration;
an algebraic read must not make that authored structure arbitrary.

The arithmetic cancellation law is established. Its placement in Gojo is a
design choice: this implementation preserves the original expression and
records a separate read with evidence. It does not make all unit shapes or
ordered relations commutative.

## The expressed structure

[`UnitQuotient`](../Structures/UnitQuotient.cs) constructs and reads:

```text
group(ordered numerator factors, ordered denominator factors, selected standard)
```

Each supported factor is a one-tick point `P(C,1)` on a concrete unit-bearing
carrier. The native `UnitShape` states what that tick means. Its carrier value
is not a numeric operand in this dimensional read. Empty factor groups are
explicitly present; placeholders and unsupported factors remain present too.
An empty numerator with a nonempty denominator expresses reciprocal participation.

For example, the following uses existing syntax constructors:

```gojo
gojo 1;
let $mile = carrier();
let $hour = carrier();
let $clock = carrier(unit_of($hour));
let $standard = point($mile, 1) -> point($hour, 1);
root group(
    ports(point($mile, 1), point($clock, 1)),
    ports(point($hour, 1)),
    $standard
);
```

The explicit dimensional reading of `(mile/hour) * hour` can leave one mile
factor. It retains the two different time carrier identities and the standard.
The ordinary relation used as standard is calibration evidence, not itself a
new division opcode. The selected quotient context gives the three fields
their roles; arbitrary nested groups are not searched for convenient factors.

## Cancellation and its witness

`TryCancel` records a common `ReadWitness`:

| Field | Connected evidence |
| --- | --- |
| Selected source | Original quotient expression with its authored factor order |
| Read surface | Ordered group of one-tick numerator → denominator correspondences |
| Result | New quotient retaining uncanceled factors in their original relative order |
| Residue | Separate numerator/denominator groups of open or unsupported factors |
| Provenance | The same selected standard |

The read visits denominator factors in order and pairs each with the first
unused numerator occurrence having the **same complete native unit shape**.
Each occurrence participates at most once, even when an alias is reused at
several positions. The deterministic pairing records one valid dimensional
correspondence; it is not discovery of a physical producer-consumer connection.

The match preserves descriptor provenance, opposed-face structure, calibration,
and composite unit order. Bare algebraic resemblance does not override different
descriptors. This first read does not cancel a forward unit against an opposed
unit or silently commute the interior of a composite unit factor. Any needed
decomposition or conversion must be exposed explicitly first.

An unmatched known denominator remains part of the result; it is not an error.
An open factor remains in its original role and is also exposed as unresolved
read participation. Unknown factors do not cancel each other merely because
they share a placeholder or a wildcard carrier. Known factors can still cancel
around open factors in this selected commutative dimensional context.

`TryReadNumeratorUnit` derives the remaining ordered product only when the
denominator is empty and every surviving numerator factor has a supported unit.
When both groups are empty, `IsDimensionless` reads the absence of dimensional
factors. It does not substitute `Carrier.Unknown` as a universal unit or create
a new numeric value of 1.

These are observational reads. No carrier value or tension changes, no original
factor disappears, and no native unit promise is rewritten. Witnesses retain
source references; they are not a new immutable historical serialization
contract. Durable evidence still needs an explicit capture boundary.

## Standards and quantity arithmetic

For litres/100 km, the denominator can be a carrier whose native tick shape is
`ResolutionOf(kilometre,100)`. One kilometre tick cannot cancel that one
hundred-kilometre tick. A quantity must first be projected through an explicit
conversion, preserving its remainder, before its converted native basis is used.
Matching hundred-kilometre factors can then cancel and leave litre participation.

The standard field is retained structural evidence. The helper does not execute
arbitrary standard definitions, infer conversions from labels, or validate that
an external quantity conforms to a named real-world standard. Native factor
shapes govern this exact one-tick correspondence. The mph and consumption names
in the fixture are authoring aliases; they have no runtime authority.

Dimensional cancellation also does not prove numeric division is defined.
`u/u` can have no remaining dimensions while a particular numeric denominator
is zero, unknown, or an interval containing zero. Those cases still need their
numeric domain conditions and local claims. No numeric quotient evaluator or
zero-denominator policy was introduced here.

## Corrected unit factor inventory

Before this work, nested product inspection could expose its product frame as
an extra factor. It could also split an unreduced additive unit or an opposed
unit into its internal parts and marker. These were reader bugs.

`TryReadOrthogonalProductOperands` now unwraps nested product framing while
preserving additive, opposed, resolution, descriptor and orthogonal factors.
The read returns `(u*v)*w` as the three ordered factors `u,v,w`, without changing
the stored nesting. `(u+v)*w` keeps the additive factor intact; this is not an
implicit distributive expansion.

## Tests and remaining boundary

[Kernel tests](../../Gojo.Tests/Core/UnitQuotientTests.cs) cover matching and
unmatched units, reciprocal participation, repeated occurrences, preserved
order and standards, calibration mismatch, descriptors and opposed faces,
dimensionless versus numeric reads, partial factors and narrow shape reading.
A 256-case matrix checks signed exponent differences, one-use pairing and
idempotent reduction. [Factor inventory tests](../../Gojo.Tests/Core/UnitAlgebraConformanceTests.cs)
protect the three prerequisite reader corrections.

The [authored fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/unit-quotients.gojo)
and [syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxUnitQuotientTests.cs)
exercise speed/time cancellation in two authored orders, consumption calibration,
reciprocal participation and binding an open denominator. No parser constructor
was added.

This implements an explicit unit-expression read. It does **not** yet add a
general reciprocal `UnitShape`, nested quotient arithmetic, automatic operation
crystallization through denominator units, or fractional quantity settlement.
A composite factor stays whole until a caller explicitly exposes another
factorization. Bridging these quotient surfaces into stable tick promises and
numeric division, with domain conditions and calibration witnesses, is the next
extension. The original [mathematical checkpoint](GojoMathConformance.md) still
applies to exact quantities and resolution.

Verification: `dotnet test Gojo.slnx --nologo` passes **1,038 tests**, none failed
or skipped (687 kernel, 183 encoding, 20 gear-ladder, 148 CAD). There are 17 new
cases in this step, including the 256-case matrix inside one test. Documentation
links and `git diff --check` also pass.
