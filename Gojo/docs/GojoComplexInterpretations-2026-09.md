# Complex interpretations and fractional powers

September 11, 2026. This work follows the user's clarification that complex
arithmetic remains fundamental, while its construction is a selected Gojo
interpretation rather than the mandatory meaning of every primitive pair.

## Construction contract

Two coefficient roles and an explicit signed action establish the algebra:

```text
coordinates = group_on(holder, S(real,0,1), S(imaginary,0,1))
one         = group_on(holder, L(real,1),  L(imaginary,0))
generator   = group_on(holder, L(real,0),  L(imaginary,1))
minus_one   = group_on(holder, L(real,-1), L(imaginary,0))
minus_gen   = group_on(holder, L(real,0),  L(imaginary,-1))
action      = group(one -> generator, generator -> minus_one,
                    minus_one -> minus_gen, minus_gen -> one)
basis       = group(coordinates, action, construction_evidence)
value       = group_on(holder, real_coefficient, imaginary_coefficient)
```

The edges share their actual intermediate nodes. A pair of visually opposed
directions alone does not establish this action. Within the selected unital,
bilinear coefficient read, multiplication follows `J(1)=i`, `J(i)=-1` and
linear extension: `(a+bi)z = az + bJ(z)`. Four product contributions remain
inspectable. This is ordinary complex arithmetic, with a connected explanation
for the generator. The two coordinate spans calibrate scalar coefficients;
this read is not a physical dimensional product or an implicit unit conversion.

In the **opposed-face construction**, `imaginary = Dual(real)`. Positive
participation on both faces remains present as two components. Drawing those
faces left and right does not make the second component negative. Scalar
negation changes both signed coefficients, conjugation changes only the
generator coefficient, and applying the generator advances the signed cycle.
The primitive face dual remains an involution; it is not multiplication by i.

In the **contour construction**, the already constructed circle, metric and
ordered radial quarter landmarks justify the same cycle. Opposite and
perpendicular rays are checked; an arbitrary four-marker loop is insufficient.
The first ray is the unit reference and the next is the selected generator.
The radius supplies a scale, not an intrinsic angle constant. Correspondence
maps each radial landmark to its coefficient state. Reversing the selected
orientation can choose the conjugate interpretation without changing the laws.

Both structures can therefore express the same algebra while retaining
different provenance. A cross-basis read requires explicit correspondences
for the two unit states. Equal numbers or similar drawings do not create that
correspondence automatically.

## Fractional powers

For a reduced rational exponent p/q with q positive, retain the constraint
`w^q = z^p`. Reduction is essential: `2/2` requests z, not both square roots of
z squared. Negative p requires a nonzero z. Repetition and inversion remain
separate from choice among roots. This algebraic candidate-set contract does
not silently choose the principal analytic power or a logarithm branch.

A root candidate must reproduce the target under the selected product law.
When exact rational components are unavailable, keep an open coefficient pair
connected to its repeated-power constraint. This is an incomplete numeric read,
not evidence that the root is nonexistent. Alternative roots are not an
uncertainty interval or a probability distribution. A reference can select the
nearest exact candidate in the coefficient metric; a tie remains a tie. That
local choice is not yet continuous branch tracking around a loop.

Ordinary complex identities and the distinction between general and principal
powers are checked against [NIST DLMF 1.9](https://dlmf.nist.gov/1.9) and
[NIST DLMF 4.2(iv)](https://dlmf.nist.gov/4.2#iv). Their analytic definitions
provide conformance references, not a requirement to store radians or floats.

The existing fused carrier-face product and fold remain separate selected
reads. Their law must not be silently changed into complex multiplication.
Split-complex (`j*j=+1`) also requires a different action and is not an alias
for this algebra.

## What the two views share, and what they do not

For `3 + 2i`, the opposed construction carries positive 3 on the forward face
and positive 2 on the opposed face. Both are present. The contour construction
reads those coefficients along its first ray and next radial quarter. The
generator acts as follows in either view:

| Repeated generator application | Coefficients | Opposed-face read |
| --- | --- | --- |
| Original | `(3,2)` | `+3F`, `+2I` |
| Once | `(-2,3)` | `-2F`, `+3I` |
| Twice | `(-3,-2)` | `-3F`, `-2I` |
| Three times | `(2,-3)` | `+2F`, `-3I` |
| Four times | `(3,2)` | `+3F`, `+2I` |

The general flow codex calls this a phase step; it need not describe a physical
rotation. The circular construction additionally justifies the geometric read.
Nothing here assigns universal screen-left or screen-up meanings to the roles.

The two-coefficient representation does not embed all complex arithmetic into
one ordinary real coordinate. For example, the existing scalar fold sends
`3F + 2I` to `1F`. It cannot reconstruct the pair. Under that fold `i` becomes
`-1`, but folding `i*i` gives `-1`, whereas multiplying the folded values gives
`+1`. This is a useful explicit example of a read which does not preserve the
product. Here the scalar accounting read is forward minus opposed; the fold
itself retains the resulting carrier face. Retaining the pair allows both
readings without contradicting either
selected law.

Core2's `AlgebraTable.ApplyOpposition` already distinguished the four-step
operation from its `Mirror` operation. Its default product agrees with the
new coefficient read: recessive/recessive contributes negative dominant;
the two mixed products contribute recessive. Current
`FusedCarrierFaceReads` instead exposes a different four-term participation
law and a scalar fold. Both remain available; neither is silently renamed.

## Implemented readers

The implementation is in [ComplexFrame](../Structures/ComplexFrame.cs),
[contour construction](../Structures/ComplexFrame.Contour.cs) and
[powers](../Structures/ComplexFrame.Powers.cs). These are local readers over
ordinary relation surfaces. Their temporary rational arithmetic uses the
existing `ExactScalar`; durable fractions use grounded numerator/subdivision
structure. No new primitive element or host complex-number wrapper is added.

| Reader | Request structure | Result and evidence |
| --- | --- | --- |
| `Opposed` | One concrete forward carrier | Two unit spans, four shared signed states, the full directed cycle and face correspondence. A convenience builder; the fixture writes the same structure explicitly. |
| `TryFromContour` | `group(contour surface, coefficient coordinates)` | A complex basis with radial-to-coefficient correspondence, native contour and verified turn evidence. |
| `TryAdd`, `TrySubtract` | `group(basis,left,right)` | Exact or partial coefficient pair; signed coordinate calibrations distinguish addition from subtraction. |
| `TryMultiply` | `group(basis,left,right)` | Exact or partial pair and four source-product contributions; no scalar face fold. |
| `TryApplyGenerator`, `TryConjugate` | `group(basis,value)` | Calculated pair and the selected action or unit-state correspondences. |
| `TryDivide` | `group(basis,numerator,denominator)` | Conjugate, squared coefficient norm, numerator product and exact quotient; open or zero divisor remains unresolved. |
| `TryReexpress` | `group(source basis,target basis,two unit correspondences,value)` | Same coefficients on the explicitly corresponding target roles. Lookalike unit nodes do not substitute for actual connections. |
| `TryPower` | `group(basis,value,signed count span)` | Integer power with actual product compositions and inversion evidence. Binary composition is a derived evaluation strategy. |
| `TryFractionalPower` | `group(basis,value,p span,q span)` | `group(power constraint,candidate witnesses)`, with reduced exponent, target-power trace, branch correspondence and reproduction checks. |
| `TrySelectNearest` | `group(fractional-power request,reference)` | Root witness, exact squared coefficient distances, and nearest candidate group. A tie remains plural and unresolved as a unique selection. |

Count spans begin at zero. Numerator and denominator counts use the same
concrete count carrier; q must be positive. Known zero to a positive power
is zero. This selected domain leaves `0^0` and negative powers of zero
unresolved. Signed integer powers are not limited to positive exponents.

All these reads are observations. They introduce no carrier value or tension.
Numeric sites are captured before arithmetic, so a later retargeting produces
a new result while the earlier witness remains readable. Within a transported
world the graph preserves aliases; independently restored worlds still require
explicit identity correspondence.

The first numeric implementation computes over rational complex coefficients,
not every possible member of the complex continuum. Exact intermediates use
wide integer arithmetic; each stored coordinate or fraction must fit the
current signed-integer transport. Overflow retains an unresolved request,
without wraparound, saturation or a floating-point fallback.

## Fractional-power examples and boundaries

| Request | Current result |
| --- | --- |
| Solutions of `w^2 = -1` | Two exact candidates, `i` and `-i`; neither is automatically the chosen branch. |
| Solutions of `w^2 = 3+4i` | `2+i` and `-2-i`, each with a multiplication check. |
| Solutions of `w^2 = 4/9` | `2/3` and `-2/3`, with exact subdivisions. |
| `9^(3/2)` under the candidate-set contract | `27` and `-27`, satisfying `w^2=729`. This differs from a request for the positive principal real power. |
| `(3+4i)^(2/2)` | Only `3+4i`; reducing the exponent prevents inventing a second branch. |
| Solutions of `w^2 = 2` | Open real coefficient and known zero imaginary coefficient, attached to the square equation. No tolerance or decimal root is invented. |
| Solutions of `w^2 = i` | Correlated open real/imaginary coefficients in one candidate whose square must reproduce `i`. |
| Solutions of `w^3 = 8` | Retained cubic equation. Returning only `2` would falsely omit the other complex roots. |
| Roots of zero with any positive integer degree | The unique zero candidate. |

Exact integer results and square-root families with rational components are
implemented. Other nonzero root degrees currently retain their constraints;
they do not claim an enumerated candidate set. A missing rational answer is
different from a forbidden divisor and from a tie between exact candidates.
Known input, exponent, target and the unresolved candidate remain available
in the result/remainder so another reader can refine them.

There is no principal branch flag on a number. The fixture separately requests
the candidate nearest `-i` among the roots of `-1`; that selects `-i`. With a
reference at zero both candidates have the same distance, so neither wins.
An open reference retains the already calculated candidates and leaves their
distance reads open; it does not discard the root family.
This read uses the coefficient norm of the selected complex algebra. It does
not infer a physical distance metric for an arbitrary rendering of the faces.

## Syntax and validation

The [authored fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/complex-interpretations.gojo)
writes the entire opposed basis, not a literal whose meaning is hidden inside
the parser. Its circle request accepts the result of the existing
[constructed circle fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/constructed-circle-rotation.gojo).
No new parser forms are needed. As with the other semantic fixtures, the driver
selects the local reader. A future compiler must preserve that explicit read
selection in addition to compiling the basis and request graph; bare parsing
does not execute an overloaded group.

The [conformance tests](../../Gojo.Encoding.Tests/GojoSyntaxComplexTests.cs)
exercise independent integer arithmetic identities, associativity,
distributivity, inverse reconstruction and exact Gaussian square roots across
all quadrants. They compare the circular generator against the existing
geometric rotation reader under ordinary and sheared charts and both choices
of side. False bases, false shared cycle sites, non-quarter landmarks, zero
divisors, independent unknowns, integer range, branch ties and two numeric
restorations are included. Current full-solution verification passes 1,653
tests, including 35 focused complex cases and the new fixture inventory entry.

## Next mathematical steps

1. **Refine correlated algebraic roots.** Connect shared radical constraints
   to exact rational enclosures and available refinement reads. For `sqrt(i)`,
   retain the shared magnitude and signs rather than independent error bars
   on its two components. An exact algebraic value need not become a float.
2. **Construct higher root families.** Use intrinsic contour subdivision and
   repeated-product constraints, retaining every admissible branch. A cubic
   should account for all three branches before claiming a complete read.
3. **Follow branches through change.** Add source samples, continuation
   correspondences, path/winding evidence and ties. A loop around zero can
   exchange square-root branches even though the source returns to its start;
   a nearest-current-sample rule alone is insufficient to encode that history.
4. **Extend selected algebras and quantities deliberately.** A split-complex
   action, dimensional complex quantities, broader calibrations and analytic
   functions need their own laws. A two-cycle or positive square cannot be
   accepted by the present complex-basis reader merely because it looks similar.

The key recovered principle is that the algebra is fundamental enough to be
constructed and tested, while its applicability and representation remain local
choices. These two constructions are a starting point for those further reads.
