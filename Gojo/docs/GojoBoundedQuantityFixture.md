# Bounded quantities, shared causes and selected detail

This fixture extends the [uncertainty reference](GojoUncertaintyReference.md).
Status: implemented first fixture, September 6, 2026. The first law is a
guaranteed enclosure under explicit local premises.
It does not require access to an external source's private history or supply a
probability, confidence-update, clustering or attention-allocation rule.

## Structural contract

The following notation describes ordinary version-1 Gojo structure. It precedes
the helper names; no operation tags or new primitive kinds are required.

```text
bounded sample = group(sample slot or exact sample, support span, evidence)
contribution   = bounded sample -> (source frame -> target frame)
affine read    = group(exact offset, ports(contributions...), provenance)
```

The support span is read as a closed real-valued enclosure in its carrier's
tick calibration. Its authored endpoint order remains present. This selected
quantity read is distinct from reading the same span as occupied material.
A bare span does not assert that any particular part conforms to a tolerance.

The sample slot identifies a local unresolved quantity. The evidence can be
opaque or incomplete. Repeating that slot preserves dependency; using separate
slots supplies no statistical-independence claim. Several bounds on the same
slot are intersected in the same explicit source basis. Empty intersection,
unbound calibration, mismatched basis and unsupported slot faces remain
unresolved, even if a later coefficient would cancel the quantity.

Every contribution has an ordinary affine calibration:

```text
S(source,a,b) -> S(target,c,d)
y = c + (x-a)(d-c)/(b-a)
```

Exact offsets and endpoints use the existing rational participation shape
`L(C,n) -> P(C,d)` when they need a positive subdivision. They do not create
fractional carrier state. The backend may calculate rational coefficients, but
the resulting shared slots, intersected supports and calibrated contributions
must remain ordinary Gojo structure with the original input as evidence.

Further selected operations use exposed source surfaces:

```text
combination = ports(affine source -> calibration, ...) -> P(output,1)
product     = ports(left affine source, right affine source) -> S(output,0,1)
quotient    = group(numerator affine source, denominator affine source,
                    output carrier, standard)
conformance = group(quantity observation, permitted span, specification evidence)
```

Combination retains the shared causes in another affine read. Product and
quotient retain their input structures and provide conservative enclosures;
they do not silently manufacture independent affine causes for nonlinear
results. Native output units must match the selected operation. Division
checks the denominator's domain before any same-source cancellation.

Each successful read produces a common `ReadWitness`. The result contains exact
lower and upper endpoints and an enclosing whole-tick span. Rounding is outward;
the lower and upper rounding excesses remain separate exact participations.
The result is observational and introduces no carrier tension.

Conformance observes the distance from a nonempty possible-value interval to
the permitted interval. Zero maximum violation settles this criterion for all
represented possibilities. Positive minimum violation excludes conformance.
A zero minimum with positive maximum preserves an unresolved decision. It
does not say which premise or participant should change.

The quantity result retains variation inside tolerance. For example, a known
two-tick discrepancy has zero violation against `[-3,3]` and two ticks of
violation against `[0,0]`. The accepted coarse read never overwrites that
underlying discrepancy.

## Implemented local reads

[`BoundedQuantity`](../Structures/BoundedQuantity.cs) builds samples and affine
surfaces and reads exact endpoints. Its
[affine implementation](../Structures/BoundedQuantity.Affine.cs) collects
calibrated contributions by explicit slot identity. The
[selected operations](../Structures/BoundedQuantity.Operations.cs) provide:

| Entry point | Selected operation | Result or unresolved condition |
| --- | --- | --- |
| `TryObserve` | Offset plus a flat group of calibrated bounded samples | Exact affine coefficients and a conservative closed enclosure |
| `TryCombine` | An ordered group of affine-source/calibration pairs into a unit output point | Another affine surface retaining shared causes and all domain premises |
| `TryMultiply` | Two affine inputs into a native product frame | Four-corner enclosure, with a tighter nonnegative same-function square read |
| `TryDivide` | Two affine inputs, native quotient output and standard | Rational enclosure; zero-containing denominator support remains open before cancellation |
| `TryObserveConformance` | A quantity observation and a permitted span | Minimum/maximum violation distance; a readable result need not mean conformance |
| `TryReadBounds` | A successful witness's result | Exact endpoints and their minimal outward integer enclosure; malformed or open results fail |

For a cause `x_i` with several explicit supports, let `[l_i,u_i]` be their
intersection in one source basis. After applying each calibration, the affine
read has the form

```text
y = k + sum(a_i * x_i)
lower = k + sum(min(a_i * l_i, a_i * u_i))
upper = k + sum(max(a_i * l_i, a_i * u_i))
```

Coefficients for a shared slot are combined first. Distinct slots are allowed
to vary across a Cartesian enclosure; this gives a conservative bound without
asserting statistical independence. Exact rational arithmetic precedes
endpoint rounding. Known sample bindings must satisfy their support, and even
zero coefficients retain their domain conditions.

The normalized affine surface encodes each combined coefficient with an
ordinary zero-origin calibration and retains the original constraints as
evidence. No private dependency table must accompany numeric transport.
Intermediate fractions use wide arithmetic; each persisted coordinate,
numerator, denominator and frame endpoint must fit the kernel's stored range.
An unrepresentable result throws overflow rather than silently saturating.

## Authored fixture

[The version-1 source](../../Tests/Gojo.Encoding.Tests/Fixtures/Syntax/bounded-quantities.gojo)
uses existing constructors throughout. Parsing constructs the graph; the
test driver selects the mathematical read or material action. No additional
parser keyword or implicit scheduler was introduced.

| Root | Connected experiment | Observed result |
| --- | --- | --- |
| 0 | One cup description, one repeated sample, two distinct samples, and a binding | In hundredths of a cup: `[90,110]`, exact self-difference `0`, separate difference `[-20,20]`, repeated sum `[180,220]`; binding refines without rewriting the original |
| 1 | Two observations sharing quantity and calibration contributions | Each is `[84,116]`; their difference retains event noise `[-2,2]`; two occurrences of the same event cancel |
| 2 | Open calibration, conflicting supports, zero-crossing division, valid self-division, square | Binding repairs calibration; contradiction and zero division stay open; nonzero self-division is `1`; a shared square is nonnegative |
| 3 | Bolt/hole evidence, permitted clearance and nominal size stops | Clearance is `[0.10,0.30] mm`; permitted clearance has zero violation; a stricter band leaves a mixed decision; the catalogue supplies no actual-value bound |
| 4 | Distance 113 miles through an explicit hundred-mile communication focus | One coarse tick is published and the known 13-mile remainder and original distance are retained |
| 5 | Current `2 + 2`, desired `5`, a one-unit reserve and an explicit transfer | The initial gap is `1`; transfer changes one input to `3`; total stock stays `5`; the timeline retains before, actual transfer witness, after and remaining violation |
| 6 | One observation returning along two exposed routes | Averaging the aliases retains its original bounds; subtracting the original observation cancels even after restoring the computed witness |

## Current limits

- Bounds are conditional mathematical promises, not externally verified facts.
  Their supporting evidence may remain opaque or partial. The reader does not
  inspect that evidence to infer authority, independence or ancestry.
- The first sample supports use integer endpoints but describe closed
  real-valued intervals in the selected tick calibration. Exact fractional
  samples, offsets, projected endpoints and rounding excesses use structured
  rational participation. Open/disconnected sets, general discrete-candidate
  constraints and automatic nominal classification remain future work.
- A sample slot must have a forward placeholder face. Carrier frames are
  concrete forward faces; signed coordinates and reversed frames are supported.
  Opposed-face quantity laws require a separate explicit extension.
- Samples of a shared unknown use one source carrier. Compatible units on
  distinct carriers do not infer reference equality. Cross-basis contribution
  and output conversion require an explicit calibration.
- The affine surface is a flat local construction; combination emits another
  flat surface. The reader does not recursively execute arbitrary expression
  graphs or traverse provenance in search of operands.
- Nonlinear reads preserve the source relation but need not produce tight
  bounds. For `x in [-1,1]`, this product read encloses `(1+x)(1-x)` in `[0,4]`,
  while a stronger reader could derive `[0,1]`. It does not return a fake new
  independent affine cause or treat every point in the enclosure as attainable.
  Further affine composition of that nonlinear result is not implicit.
- Division refuses an enclosure containing zero even when an additional,
  currently unexpressed constraint could exclude zero. Native unit promises
  and standard evidence remain required; no unit simplification bypasses the
  numeric domain check.
- Emitted endpoints and normalized coefficients capture the selected read.
  They do not automatically update after an upstream change. Source and
  correspondence remain attached for a fresh read or explicit dependency
  mechanism; live references alone are not an immutable historical sample.
- Confidence, learned similarity, utility-based weighting/atrophy, automatic
  attention and selection of a useful response remain exploratory policies.

## Focus and opaque sources

“About 100 miles” can communicate that finer distance is outside the current
focus. It need not mean the speaker lacks a finer observation. A focus can
expose a coarse read and retain the fine complement without manufacturing
physical uncertainty. That attention implication is contextual; a precise
number alone does not prove importance or accuracy.

Outside sources may remain black boxes. A local space can retain observed
reliability, timing, processing or shared-variation evidence through exposed
surfaces, without traversing an entire ancestry. Learned similarity can guide
selection, weighting or pruning; it does not justify exact alias cancellation.
An explicit source correspondence may also be supplied by another mechanism.

An error inside a permitted tolerance can cease to demand correction for that
task. Its known magnitude, uncertainty and acceptability remain separately
readable. Losing that separation would prevent a stricter later query.

## Verification

[Kernel conformance tests](../../Tests/Gojo.Tests/Core/BoundedQuantityTests.cs) cover
shared and distinct slots, event-specific noise, opaque evidence, intersected
and contradictory supports, partial binding, reversed calibration, exact
fractions, outward rounding, unsupported faces/bases, stored range limits,
nonlinear bounds, division domains and tolerance decisions. A 500-case signed
calibration matrix checks rational extrema. A further 625 shared-source
assignments check product and quotient enclosure by evaluating concrete samples.

[Syntax tests](../../Tests/Gojo.Encoding.Tests/GojoSyntaxBoundedQuantityTests.cs) execute
all seven authored experiments before restoration and after one and two numeric
round trips. They also restore a completed material timeline and a computed
affine witness, then verify their historical values and dependency-sensitive
reads. The existing inventory includes the new fixture automatically.

`dotnet test Gojo.slnx --nologo` passes **1,148 tests**, with no failures or skips:
745 kernel, 234 encoding, 21 gear ladder and 148 CAD. This adds 47 cases to the
preceding checkpoint, including the generated matrices inside individual cases.
All ten syntax fixtures and all 208 exported Blocks specimens pass the existing
two-round-trip graph inventory. This is evidence for the stated local laws,
not a general convergence, learning or complete uncertainty-algebra claim.
