# Mathematical conformance checkpoint

Status: implemented corrections and executable conformance fixtures, September
5, 2026. This follows the [low-level audit](GojoLowLevelMathStatus-2026-09.md).
It strengthens exact local mathematics before broader feedback discovery.

Follow-up: the [September 6 milestone review](GojoNumericMilestoneReview-2026-09.md)
records explicit nested quotient composition, material refinement/borrow,
complete numeric transport and additional exact-range corrections. Counts below
remain evidence for this earlier checkpoint, not current solution totals.

## Corrected projection accounting

For source frame `[a,b]`, target frame `[c,d]`, and input `x`, write
`A = b-a`, `B = d-c`. The current integer read is:

```text
q = truncate_toward_zero((x-a) * B / A)
y = c + q
r = (x-a) - q * A / B
x = a + (y-c) * A / B + r
```

The last two equations require nonzero `A` and `B`. `r` is in **source ticks**.
The old implementation placed `((x-a)*B) % A` on the source carrier. That
numerator still needs division by `B`; it is generally the wrong amount.

| Source span → target span; input | Integer target | Correct source residue |
| --- | ---: | ---: |
| 3 → 2; 10 | 6 | 1 |
| 3 → 10; 1 | 3 | 1/10 |
| 10 → 3; 7 | 2 | 1/3 |
| 3 → 2; -10 | -6 | -1 |
| 3 → -10; 1 | -3 | 1/10 |

Whole residue remains `L(source,n)`. Fractional residue is ordinary structure:

```text
L(source,n) -> P(source,d)
```

In this residue context, it means `n` counts at the `d`-part subdivision of one
source tick. The denominator is positive and the fraction reduced. Both sides
refer to the actual input carrier, including when a port accepts a compatible
carrier or an unbound source basis. No new element kind or fractional carrier
state is introduced. The builder and narrow reader are
[`ProjectionResidue`](../Core/ProjectionResidue.cs); `TryReadProjection` reads
the target plus this whole-or-fractional residue. `TryGetCarrierLocations`
continues to require two direct locations.

This is already writable as `at($source,1) -> point($source,10)`. It is a
contextual scaled remainder, not a claim that every relation has rational
semantics or that general reciprocal-unit algebra exists. Generic dual and
reversal operations do not constitute a rational inverse API.

A zero source span refuses projection and retains the input in the fallback;
it must not activate an import. A zero target span is a constant affine read:
it reports the target's start coordinate and retains the whole original input,
because it accounts for no varying source participation. Thus `[2,5] -> [7,7]`
reads 7, not 0. Constant operand ports remain usable.

## Observation and material import

Observation preserves the original source and never adds its residue to source
tension. Operation reads keep each operand's remainder. Follow steps distinguish
unapplied claim mismatch from the response route's source remainder. Boundary
projection keeps source residue even if the target lane is blocked: blocked
target participation and source remainder are both visible.

Scalar carrier tension stores whole ticks. Existing scalar value and structural
imports therefore refuse fractional source remainder **before applying any
collected slot update**. Existing values, coordinates and source tension remain
intact. An observational read still provides the target and fraction. This
changes previously accepted imports whose accounting was incorrect. Whole
remainders continue through signed tension; the 3 → 2 import of 10 now yields
target 6 and source tension 1.

This is a current material-storage boundary, not a ban on fractional amounts.
An explicitly finer source basis can make participation whole, or future
material routes can retain and settle the scaled structure directly. That
needs an explicit accounting mechanism. Silently rounding a fraction or calling
its numerator whole-tick tension is not valid.

## Numeric range and resolution

Projection, exact structural ratio scaling, and operation sum/product
accumulation use exact wide intermediate arithmetic, then check coordinates
when storing them. Examples that now succeed include minimum signed ticks in
a 1:1 read, an 8-billion input through a 4-billion:4-billion frame, cancellation
in a large sum, and a large product with a final zero factor. Projection and
direct operation output frames support endpoint differences wider than `long`
when stored results fit. Carrier reads preserve the unsigned magnitude of the
minimum signed location and apply direction before checking final signed range.

This does not change storage to arbitrary precision. Locations, residue
numerators and positive denominators still fit signed 64-bit ticks; carrier
values retain unsigned storage. Unrepresentable final coordinates, fractions
or resolution factors throw instead of wrapping or saturating. Other readers
that require a single signed extent now check its subtraction; this is not a
promise that every kernel operation accepts every wide frame. Each retained
intermediate Gojo result must fit its own representation, even if a later
operation could cancel it.

`ResolutionOf(unit,n)` and text `resolution(unit,n)` require `n > 0`. Factor 1
preserves the source unit. Nested factors multiply; overflow fails instead of
saturating into a different unit promise. The parser reports invalid factors
and nested overflow as located syntax errors.

Resolution order is observable. For 17 + 28, summing before a ten-tick read
gives 4 coarse ticks and 5 fine ticks. Reading each operand first gives 3 coarse
ticks plus separate fine residues 7 and 8. Both reconstruct 45. Neither result
licenses discarding its residue or treating coarsening as an algebraic identity.

## Conformance evidence

- [Projection tests](../../Tests/Gojo.Tests/Core/ProjectionConformanceTests.cs):
  5,000 signed/offset frame combinations checked by input reconstruction;
  fractional examples; direction and range; zero spans; whole versus fractional
  import; later-slot rejection without an earlier partial update; shared-site
  structural scaling; follow remainders.
- [Operation tests](../../Tests/Gojo.Tests/Core/OperationConformanceTests.cs): signed
  sums and products against integer arithmetic, distributivity of exact reads,
  wide accumulation and final overflow, per-operand fractional residue, and
  `(3+2i)(5+4i) = 7+22i` under the existing opposed-face product law. Fused
  expansion retains four terms; the test explicitly re-reads their source pairs
  through the concrete product law before summing. Their displayed participation
  faces are not mistaken for completed scalar folds.
- [Resolution tests](../../Tests/Gojo.Tests/Core/ResolutionConformanceTests.cs):
  positive factors, stable-unit overflow, 471 signed direct/staged coarse reads,
  and addition before versus after coarsening with complete accounting.
- [Boundary projection tests](../../Tests/Gojo.Tests/ConceptSurface/BoundaryProjectionConformanceTests.cs):
  legal and illegal target lanes both preserve fractional source residue.
- [Text fixture](../../Tests/Gojo.Encoding.Tests/Fixtures/Syntax/math-conformance.gojo)
  and [17 syntax cases](../../Tests/Gojo.Encoding.Tests/GojoSyntaxMathConformanceTests.cs):
  executable signed operations, ratios, distinct inversions, squared/cubed
  units, crystallization, resolution, partial construction, explicit carry
  activation, orthogonal shape distinctions, incompatible scalar crystallization
  refusal, component identity, and numeric validation.

The fixture roots 0–13 contain participants, sum, product, two ratios, scaled
residue, dual/reversal examples, units, resolution ladder, partial operation,
carry recipe, orthogonal shapes, incompatible additive shape, and three
component/site reads. Indexes organize tests; relations carry interpretation.
Parsing constructs them; tests call readers or attach the authored carry route.

## What remains unfinished

These checks establish a bounded mathematical foundation. They are not a proof
of conformance for every experimental structure in Blocks or Gojo. The next
low-level work should address:

1. **Reciprocal unit participation and cancellation.** Repeated numerator units
   work. The subsequent [explicit quotient read](GojoUnitQuotientReads.md) now
   expresses ordered denominator factors and witnesses local cancellation for
   `distance/time * time -> distance`. Its follow-up captures stable ordered
   reciprocal `UnitShape` promises and observes native-basis numeric quotients
   through authored syntax. Explicit nested composition and reduced output reads
   now work; automatic operation integration remains incomplete. Carrier dual
   is not a reciprocal.
2. **Material refinement and borrow.** Explicit calibrated packet transfer,
   coarsening and local borrowing now work. General fractional settlement and
   automatic finer-basis admission remain incomplete.
3. **Uncertainty propagation.** Exact remainder is not a measured interval or
   correlated uncertainty. Bounds, approximate products and inequalities need
   connected claims, premises, and refinement tests.
4. **Broader component algebras.** The opposed-face example does not establish
   complete complex, vector or tensor arithmetic. Metrics, rotations, inverses
   and component transformations need declared laws.
5. **Authoring and persistence.** No new constructor was needed here, but the
   parser lacks a direct unbound carrier-basis form and arbitrary unit descriptor
   construction. Open element slots work; unknown integer or unit arguments
   remain partial patterns around concrete constructors. Numeric graph transport
   now preserves placeholders, shared references, attachments and retained
   history. Canonical text writing and distributed identity remain separate.

Keep these mathematical boundaries ahead of a general feedback allocator or
learning policy. Local uncertainty needs reliable accounting underneath it.

## Verification

`dotnet test Gojo.slnx --nologo` passes: **1,021 tests**, no failures or skips
(674 kernel, 179 encoding, 20 gear-ladder, 148 CAD). This adds 59 cases to the
previous checkpoint, including the generated conformance matrices within those
cases. Eight earlier expectations that endorsed incorrect source remainders or
fractional-to-whole tension imports were corrected. Documentation links and
`git diff --check` pass.
