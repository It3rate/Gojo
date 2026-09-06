# Low-level structural mathematics and syntax status

Status: coverage review and measured limitations, September 5, 2026, against
`40c31ee`. This audit pauses the move toward broader feedback discovery. It
does not change arithmetic, parser behavior, or settlement laws.

The kernel has substantial executable mathematics. The text notation can
construct many of those structures through its small vocabulary, but it has
not demonstrated comprehensive mathematical coverage. Three different claims
must remain separate: a graph can be authored; an appropriate kernel reader
exists; that reader and its composition laws have been exercised through text.

## Coverage overview

| Area | Present kernel support | Current text surface and remaining boundary |
| --- | --- | --- |
| Identity and topology | Carriers, signed locations, shared TickRefs, ordered relations, points, spans, groups, bindings | Direct `carrier`, `tick`, `at`, `point`, `span`, `->`, aliases, slots. Strong basic coverage. No general external-reference, cyclic-graph, or lossless round-trip contract. |
| Signed arithmetic | Signed location reads; carrier addition/removal through tension; scalar additive and negative-ratio reads | Signed literals and explicit port topology can express the reads. A negative coordinate is different from an unsigned carrier value with an unmet draw. Text constructs state descriptions; it does not activate a settlement world. |
| Addition and multiplication | Point-output sums, segment-output products, per-input calibration, product units, preserved source participation | Expanded relations can express operations; `product_units` preserves repeated units. Kernel tests are broader than the syntax's end-to-end mathematical fixtures. |
| Ratios and affine frames | Forward/backward projection, offsets, reversed extents, integer quotient and remainder | Direct point/span relations. A confirmed general-ratio residue-basis issue prevents claiming a complete source-remainder accounting law; see below. |
| Duality and inversion | Carrier opposed face, scalar location negation, relation dual, relation reversal; tested opposed-face product cycles | `dual` and `reverse` express distinct primitive moves. They do not provide general algebraic inversion, reciprocal units, or arbitrary transform inversion. |
| Unit dimensions | Shared units, repeated products, distinct product/additive participation, descriptor provenance, crystallization | `unit_of`, `product_units`, `additive_units`, `crystal` cover a useful subset. No general denominator/cancellation algebra. No direct syntax for attaching arbitrary unit descriptors. |
| Multiple components and spatial structure | Independent carrier participation, compound ports, higher structural/CAD views | Generic relations can preserve multiple axes and components. `group(x,y)` alone does not establish a vector space, metric, rotation law, or dimensional compatibility. No demonstrated complete vector, complex, or tensor algebra in syntax. |
| Orthogonal shapes and fill | Point-, zero-span-, and nonzero-span-derived orthogonal units; interval fill and crystallization reads | `crystal(orthogonal(...))` works for the three basic source shapes. That is shape/unit construction, not complete circle, surface, volume, angular, or field mathematics. Fill measurement and shape-only crystallization must remain separate. |
| Resolution and ladders | Coarse unit shapes, nested coarse spans, explicit ratio/carry ladders, partial digit reads, positive carry settlement | `resolution` and explicit frames/routes can author these. Finer reads need an appropriate basis; no general automatic bidirectional borrow/refill. `resolution` is not a universal precision setting. |
| Boundary/flow and occupancy | Legal lanes, transitions, occupied values, local transfer/capacity/settlement surfaces | `basis`, `lane`, `value`, `lane_action`, `access` expose part of this. An authored action or witness is not proof of its result or execution. Unit-attached lane evidence has richer kernel construction paths than the parser exposes directly. |
| Unknown mathematics | Placeholder patterns, partial bindings, claims, read residue, unresolved structure | Open element positions work; a concrete location or carrier still requires concrete ticks or a stable unit. Partial patterns can retain missing participants. General propagation of bounded measurement uncertainty remains incomplete. |
| Numeric range | Integer storage and checked arithmetic, with some helper-specific saturation | Text literals use signed 64-bit integers. This is not arbitrary-precision mathematics; some representable inputs/results still fail on intermediate overflow. |

Evidence includes [ordinary arithmetic](../../Gojo.Tests/Core/OrdinaryMathFlowTests.cs),
[operation surfaces](../../Gojo.Tests/Core/OperationSurfaceTests.cs),
[unit shapes](../../Gojo.Tests/Core/UnitShapeTests.cs),
[duality](../../Gojo.Tests/Core/ElementDualityTests.cs),
[resolution ladders](../../Gojo.Tests/Core/ResolutionLadderTests.cs),
[fill crystallization](../../Gojo.Tests/Core/FillCrystallizationTests.cs), and
[syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxTests.cs).

## A confirmed ratio-remainder mismatch

The following syntax constructs a valid current ratio and input:

```gojo
gojo 1;
let $source = carrier();
let $target = carrier();
root point($source, 3) -> point($target, 2);
root at($source, 10);
root $target;
```

Using the kernel's `TryProject` on this parsed structure returns:

```text
target result:   L(target, 6)
reported residue: L(source, 2)
```

The arithmetic performed by
[ProjectBetween](../Core/Relation.Projection.cs) is:

```text
scaled numerator = 10 * 2 = 20
quotient = 20 / 3 = 6
remainder = 20 % 3 = 2
20 = 6 * 3 + 2
```

That numerator identity is correct. The unresolved contract is its basis: six
target ticks account for nine source ticks, leaving one source tick. Treating
the stored remainder as two ordinary source ticks reconstructs eleven, not ten.
The remainder needs its scale retained or must be represented in an appropriate
source/finer basis. Simply dividing it by the target extent in integer arithmetic
would lose information in cases where that remainder is fractional in source
ticks.

The issue reaches mutation: `target.TrySetFrom(port, input)` produces target
value 6 and source tension 2. This observation-to-tension path treats the scaled
remainder as source participation. Existing tests, including the backward-ratio
case and [carrier import tests](../../Gojo.Tests/Core/CarrierSlotTests.cs), protect
this convention, so a passing suite does not resolve the physical interpretation.

The new blend fixture's half-read and four-to-one coarse read have unit target
extents; their retained remainder is directly in source ticks. Its gain-endpoint
examples are exact. Those checks therefore did not expose this more general
ratio problem. That limitation should narrow our earlier confidence about
universal remainder preservation.

This is the first issue to settle before adding more algebra or feedback.
Changing it requires auditing projection, import, operation residue, follow
responses, and higher transfer readers together. Preserve the explicit
remainder and its basis; do not normalize it into a misleading scalar.

## Other boundaries verified directly

The local probe at `artifacts/low-level-audit-2026-09-05` performed ten
observations against parsed source. Besides the ratio and import examples:

- `resolution(unit, -2)` and `resolution(unit, 0)` are accepted and return the
  original unit, as does factor 1. This follows `UnitShape.ResolutionOf`'s
  `<= 1` fallback. It is not negative resolution, inversion, or refinement.
  The authoring contract should make this explicit or reject misleading input.
- The signed minimum integer parses, but projecting it through a 1:1 frame
  throws `OverflowException` when magnitude is calculated.
- A 4,000,000,000:4,000,000,000 ratio applied to 8,000,000,000 also throws on
  intermediate multiplication, although the mathematical result fits in a
  signed 64-bit integer. Range guarantees need to cover intermediate arithmetic.
- Text can crystallize the orthogonal of a point, a zero-length span, and a
  nonzero span. These are present capabilities, not merely possible spellings.

These probes characterize boundaries. They are not new regression tests
endorsing the limitations or a new implementation of the missing laws.

## Dimensions and inversions need specific claims

The system distinguishes several meanings that ordinary notation often hides:

```text
negative coordinate
opposed carrier face
reversed ordered frame
backward ratio read
reciprocal unit participation
inverse transformation
```

The first four have executable footing. The last two are not generally solved.
In particular, `dual(carrier)` must not be advertised as `1 / unit`, and
`reverse(relation)` does not recover information discarded by a transformation.

Repeated dimensional products such as `u * u` can preserve two occurrences of
`u`. Incompatible additive units remain multi-participation structure; a
shape-only crystal can exist while projection into one scalar value is refused.
The active [crystallized-resolution notes](../axioms/Gojo%20Crystallized%20Resolution.md)
explicitly leave full `distance/time * time -> distance` cancellation for future
inverse-unit participation.

There is also tested complex-like opposed-face behavior: a pair of opposed
operands can produce a negative forward scalar, and fused operands distribute
into visible terms. The fused-term path intentionally retains opposed
participation before a scalar fold. That is useful intermediate structure, not
a reason to claim complete complex arithmetic or to read every term's displayed
face as its final scalar interpretation. We need explicit expanded-versus-folded
agreement tests before making that stronger claim through syntax.

## The next work should be mathematical conformance

Before another feedback-discovery fixture, build a compact set of authored
structures with independently knowable outcomes and declared bases:

1. **Projection and remainder accounting.** Exact and inexact ratios, both
   extents non-unit, offsets, reversed signs, and zero extents. Check quotient,
   remainder basis, reconstruction, and the observation/import boundary.
2. **Arithmetic composition and inversion.** Signed sums and products,
   opposed-face cycles, repeated units, and fused products before/after an
   explicit fold. Compare equivalent constructions without erasing topology.
3. **Dimensional compatibility.** Compatible and incompatible addition,
   squared/cubed units, calibration, and explicit unresolved denominator cases.
   Missing cancellation should remain visible rather than silently accepted.
4. **Resolution composition.** Coarse and fine reads, operation before versus
   after projection, remainder refinement, carry, and unsupported borrow.
   Different rounded scalar answers may be valid if their full evidence
   accounts for the difference; scalar equality alone is an inadequate law.
5. **Authoring coverage and range.** For each kernel primitive in scope,
   classify it as directly expressible, expressible through expanded relations,
   missing a construction path, or awaiting kernel semantics. Test numeric
   boundaries rather than assuming parser acceptance implies executability.

Deterministic projection residue is also different from measurement uncertainty.
Knowing the exact remainder of a rounded read does not establish how a product
of two approximate measurements should propagate bounds or correlated error.
Those uncertainty laws should build on the exact foundation, not replace it.

This checkpoint keeps the dynamic thesis intact while giving its local
participants mathematics whose guarantees can be stated precisely.

## Verification and scope

- 111 focused arithmetic, operation, unit, duality, ladder, and fill tests passed.
- `dotnet test Gojo.slnx --nologo`: 962 passed, none failed or skipped.
- Ten local probe observations reproduced the boundaries above.
- No runtime behavior was changed. Existing tests do not yet assert the
  source-reconstruction law that exposes the general-ratio discrepancy.
