# Operations Round 3 — Branch review and variable partitions

Documentation review · 26 November 2026

Round 3 follows the numeric implementation round, called Round 2 by Neon and Round A by Claude. This comparison pins Sol `9e5f8444`, Claude `a26f5b80` on `claude/youthful-newton-wh29co`, and Neon `059f5c45`. Their merge base is master `9b805964`. The source trees and benchmark contracts stay pinned here so later changes can be compared to this round.

The [Operations Round 4 checkpoint](GojoOperationsRound4.md) implements bounded pooling and repartition reads, and recommends an integration order.

## Where I land

**Keep the number of parts in the observed or constructed scale. Choose the bit encoding independently.** A five-star review can have five possible ratings. A calibrated whole can have five equal subdivisions. A rating can also name a region of an instrument whose five regions have different widths. Those are three useful contracts. Each should expose the structure that justifies its read.

For operations, retain Sol's evidence tests, borrow Claude's small equation proposals and explicit completion costs, and take Neon's weighted digits and guarded lowering as proven examples for their selected contracts. The next common numeric experiment should bind one actual five-part whole, compare a coarse and a finer publication, and keep an unauthorized or unavailable read open. General mixed-radix arithmetic and compiler integration can follow a case that needs them.

## Branch comparison

This is source footprint against the common master, measured with `git diff --numstat 9b805964...<head>`. Runtime counts cover changed C# outside the test directory. They describe how much each branch chose to build over several rounds. The saved sizes and speed comparisons from earlier rounds measured specific circuits. [Earlier matched workloads](GojoOperationsBranchComparison.md#speed-on-matched-workloads)

| Branch | Changed paths | Runtime lines added / removed | Test lines added / removed | Doc lines added / removed | EncodingC tests passed |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sol `9e5f8444` | 16 | 72 / 0 | 1,292 / 0 | 582 / 0 | 248 |
| Claude `a26f5b80` | 23 | 751 / 8 | 1,856 / 0 | 2,641 / 0 | 264 |
| Neon `059f5c45` | 32 | 4,297 / 7 | 2,837 / 5 | 1,429 / 7 | 257 |

### Sol

**Strength.** The numeric checkpoint tests the difference between a coarse publication, an unavailable fine port, and an observed zero. With a coarse digit of two, the receiver can settle whether there are at least eight or at least twelve. It needs more evidence for ten. The admission row keeps pressure, four-unit groups and single units in one checked sum. The sixteenth unit remains pressure, and the saved supplier and receiver resume a pending carry. A separate qualitative side read runs independently. The supplied-candidate fixture verifies all 16 ordered forward cases before accepting an exact substitution. An open observation and an altered entry get different results. [Rung law](../../Gojo.EncodingC/Operations/RadixFour.cs) · [Read tests](../../Gojo.EncodingC/Tests/OperationNumericReadTests.cs) · [Material tests](../../Gojo.EncodingC/Tests/OperationNumericMaterialTests.cs) · [Discovery tests](../../Gojo.EncodingC/Tests/OperationNumericDiscoveryTests.cs)

**Limit.** The builder hardcodes four and two digit roles. It is an exact `0..15` example with one material admission policy. Most composition remains in fixtures. Our discovery test compares supplied candidates, and the separating question is authored. It checks forward equality for distinct ordered operand bindings. A reverse query, carry observation, or particular enactment needs its own check. [Implementation scope](GojoOperationsDiscoveryProposal.md#first-bounded-numeric-implementation)

**Take.** Use those port, identity, skipped/open, weighted accounting and continuation cases as conformance tests. Keep the read separate from admission, and keep a very small oracle when an optimized network narrows less than the exact joint relation.

**Leave.** Leave the fixed four, the two-digit layout, and the exact 16-case loop as one worked scale and test domain. A general number needs local evidence for its own scale. The small fixture supplies that evidence through its law and bindings.

### Claude

**Strength.** Round A makes the radix a coefficient of a small column relation. The same connected columns add forward and narrow backward, retain an open digit, and expose the terminal carry. A separate grouping and refinement route conserves units. The new `Bind` accepts costs for free roles and retains all distinct after-states by default. The polynomial builder and recognizer can propose small balances, products and an exception fit from partial witnesses. The controlled learned-table test actually runs steps on acquired probe states. [Round A report](https://github.com/It3rate/Gojo/blob/a26f5b80/ops_claude/GojoOperationsProposal-2026-11.md#L359) · [Numbers and ladder](https://github.com/It3rate/Gojo/blob/a26f5b80/Gojo.EncodingC/Laws/Numbers.cs) · [Binding](https://github.com/It3rate/Gojo/blob/a26f5b80/Gojo.EncodingC/Laws/Operations.cs#L52) · [Recognition tests](https://github.com/It3rate/Gojo/blob/a26f5b80/Gojo.EncodingC/Tests/RecognitionTests.cs#L234)

**Limit.** Each ladder has one radix, and the sum builder requires the same store, radix and depth. Grouping material uses space for a full packet. A canonical digit of capacity four needs a staging or incoming role for the fifth unit. The learned-table fixture chooses and binds the probe states in the host. Its shortest vanishing equation can leave out a constraint. In a scratch check with four allowed bit tuples satisfying both `d=a` and `c=a AND b`, the lowest-degree result was `d=a`, which also admitted the forbidden tuple `(1,1,0,1)`. Reproducing each positive example can therefore be much weaker than reproducing the relation. Even a full table match is a comparison of the selected law. A scratch check rebuilt a material move from identical bits without its conservation invariant. The original could take through a source port, and the copy could not. Check accounting, site aliases and step order at the binding. [Grouping example](https://github.com/It3rate/Gojo/blob/a26f5b80/Gojo.EncodingC/Tests/NumberTests.cs#L149) · [Recognition and probe implementation](https://github.com/It3rate/Gojo/blob/a26f5b80/Gojo.EncodingC/Laws/Recognition.cs#L75)

**Take.** Use the weighted column relation as a compact candidate. Borrow explicit completion costs when a receiver actually wants a chosen completion. Use a bounded polynomial fit to propose relations and a discriminating probe to test them. Count new receipts and retain the cases in which no probe is authorized.

**Leave.** Keep the host polynomial and probe data as a search view. Keep exact qualification over the required tuple domain and execution surface. The sum-of-free-roles convention applies only when the receiver declares that cost, and any remainder needed later should have an exposed role. An equation's estimated bits are a useful search cost. Compare saved graphs with bindings, exceptions and required evidence when assessing transport.

### Neon

**Strength.** Round 2 covers the broadest implementation. Its place-value builders support binary and unary digit layouts and tested decimal addition, subtraction and multiplication. It explicitly connects carry, borrow and independent operand roles. It also tests recovering joint factor pairs with local suppositions, separates stretch and repeat traces under an edit, and extends compilation for recognized balances and dynamics. The peer-review changes already addressed the earlier end-state-only quiet check and silent multiply overflow. Neither sweep implementation now holds material merely because the receipt fills. [Round 2 report](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo/docs/GojoOperations-2026-11.md#L1167) · [Numeric builders](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Numbers/Numbers.cs) · [Peer-review fixes](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo/docs/GojoOperations-2026-11.md#fixed-immediately-on-ops_neon)

**Limit.** The host `RungNumber` uses one radix for the whole ladder. Construction accepts 2 through 16, and the newer digit arithmetic uses 2 through 10. A bare array of fields does not encode weights until the arithmetic or another actual law is wired. Its host write currently writes digits before reporting a too-large value. The sparse witness store holds host arrays, and the discriminator proposes domain points without selecting an authorized port. The fitted family, weighted binding, and realized path therefore have separate validity checks. The sparse product fixture checks a fresh restored inverse query, while independent exhaustive forward qualification belongs to a separate test. A full sweep receipt is a saturated aggregate, so exact full and clipped histories can read alike. [Number adapter](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Numbers/Numbers.cs#L10) · [Sparse observations and selection](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Learning/StructuralCompression.cs#L507) · [Sparse experiment](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Tests/SymbolicRegressionTests.cs#L161) · [Sweep](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Flow/Flows.cs#L93)

**Take.** Keep the nonbinary digit laws and split outputs as an execution option where the wired schedule and requested reads qualify. Keep the compiler checks and the ability to fall back to the graph. Borrow the sparse observations and trajectory tests as inputs to candidate search. Neon reports accelerated adder and oscillator cases at this pin. Compare each performance result under its own lane count, schedule and eligibility conditions before selecting a backend. [Compiler tests](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Tests/CompiledPlanTests.cs)

**Leave.** Leave the uniform-radix array as an adapter, and keep a too-large write from changing visible digits when promoting a reusable builder. Keep proposal lists, operation-family selectors, and compiler metadata below structural meaning. The enum makes the least-total versus all-admitted choice available, but the current default still selects least total. A published local cost or open alternative works better as the common contract. Keep transport version compatibility and exact witness retention as explicit gates before bringing over engine and format changes. [Completion policy](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Laws/Operations.cs#L59)

## The five-star question

### Five is the domain and can be the denominator

A normal submitted review offers the ordered labels `1,2,3,4,5`. That is five values. A five-slot fill that can be empty has six possible counts, `0..5`. A partition of one whole into five equal intervals has six boundary sites, from `0/5` through `5/5`. Use an open role for an absent or unresolved review. An explicit zero can participate if the rubric offers zero. Empty admissible support is another read.

Three bits are enough for either finite domain. With `Shape.Binary(6)`, the codes `000` through `101` hold exact integers 0 through 5 and `110` and `111` are invalid. A one-through-five review can use that shape plus an explicit membership restriction, or a five-value field with an explicit offset correspondence. `Shape.Binary(5)` alone decodes as `0..4`, so a radix-five digit differs from a closed `0..5` count. An optional presence or support read supplies an absent review separately. Physical slots for spare codes do not become ratings. [EncodingC shape and decode](../../Gojo.EncodingC/Substrate/Shape.cs)

A review can be ordinal. The ratings carry order even when the distance from one to two has no stated equivalence to the distance from four to five. In that case keep `3 stars` in the five-position scope. A calibrated equal-step scale can expose the exact share `3/5`, with zero and the whole as its endpoints. The selected frame matters. If an analyst maps one star to zero and five stars to one, the same label three instead reads `(3−1)/(5−1) = 1/2`. Choose that correspondence as a different read. Neither coding convention supplies a calibration for averaging ordinal judgments. Core4's five-star example made this distinction by keeping the five exposed distinctions separate from a latent decimal rating. [Historical Core4 read and resolution axioms](../axioms/Core4%20Carrier%20Read%20And%20Resolution%20Axioms.md#L157) · [Current sampling contract](../Structures/SamplingSurface.cs#L132)

### What already works

| Question | Implemented evidence | Current boundary |
| --- | --- | --- |
| Can a finite field hold exactly six cases, or a digit have base five? | EncodingC's shapes validate the actual domain. In isolated checks on Claude and Neon, the radix-five digit column used 500 table bits and admitted 50 tuples. For Neon, the split forward result for `4 + 4 + 1` was digit 4 and carry 1, satisfying `9 = 4 + 5×1`. Claude's two-digit base-five sum `44₅ + 1` reached `00₅` with a terminal carry of 1. | Those are uniform positional number builders. Each ladder chooses one radix. A base-five carry does not define the rating's whole, its observation scale or any intervals within that whole. [Claude numbers](https://github.com/It3rate/Gojo/blob/a26f5b80/Gojo.EncodingC/Laws/Numbers.cs#L24) · [Neon digit balance](https://github.com/It3rate/Gojo/blob/059f5c45/Gojo.EncodingC/Numbers/Numbers.cs#L121) |
| Can one whole have fifths, tenths or some other equally spaced subdivision? | Gojo's native `UnitShape.SubdivisionOf(whole, n)` takes a supported positive factor. It constructs a finer tick with a declared scale to its parent unit. Subdivide a fifth by two for tenths or by three for fifteenths. `ResolutionOf` expresses the converse grouping when the chosen scales align. | Unit promises are stable. Rereading with a different partition selects another frame and correspondence. A stored unit shape does not silently change how its old ticks are interpreted. Integer range and exactness still apply to composed factors. [Native unit subdivision](../Core/UnitShape.Subdivision.cs#L5) · [Resolution and material refinement](GojoMaterialRefinement.md) |
| Can regions have unequal widths or a count other than a power of two? | The experimental native `CoarseReadSurface` binds one named integer axis, a selected whole and an ordered group of contiguous exhaustive regions. It accepts one to eight regions in its bounded range. I checked the five unequal ranges `1..5, 6..10, 11..23, 24..26, 27..99`. Coordinate 22 selected the third region, while empty support and an open read remained distinct. | A published region can restrict a later finer read to that region's possibilities. If the publisher retained only membership, the finer read needs new evidence for an exact coordinate. For an ordinal rubric without a measured axis, keep ordinary ordered alternatives. [Native coarse partitions](../Structures/CoarseReadSurface.cs#L15) · [Coarse read tests](../../Tests/Gojo.Tests/Core/CoarseReadSurfaceTests.cs) |
| Can rungs have different links or depend on the situation? | Native scale and projection primitives let authored adjacent links choose their own compatible factors. Gojo already has authored resolution ladders with explicit bridges. | The new EncodingC numerals have a uniform radix per ladder, and Sol's example fixes four. A general mixed-radix EncodingC builder or automatic selection of a partition remains a design choice. A local change should produce a new basis and preserve what the earlier read actually disclosed. [Native per-step carry and ratio links](../Structures/ResolutionLadder.cs#L49) · [Native ladder tests](../../Tests/Gojo.Tests/Core/ResolutionLadderTests.cs#L151) · [Current EncodingC numeric fixture](GojoOperationsDiscoveryProposal.md#first-bounded-numeric-implementation) |

### Denominator, remainder, and a change of read

Use the denominator with a whole and a selected read. A minimal native construction for three fifths has a whole unit, a fifth-tick subdivision, a declared whole span covering five of those ticks, and a selected location three ticks from its origin.

```text
whole       = selected rating scale and its ordered anchors
fifths      = subdivision of the whole unit into 5 subticks
rating read = selected site at 3 ticks on the fifths span 0..5
projection  = fifths span 0..5 -> calibrated target span
```

The notation `3/5` is a good display for the equal-share read. Keep the source observation and partition attached when rendering it. If another reader establishes a common whole, `3/5` and `6/10` are equal magnitudes. A conversion of the old report into tenths alone does not establish that a reviewer could distinguish half stars. A fresh observation on a ten-step instrument would carry that evidence.

The native projection probe used actual fifth, tenth and eighth unit shapes and explicit same-origin frames. It returned these readings.

| Requested read of three fifths | Returned ticks | Retained source residue |
| --- | --- | --- |
| Tenths | 6 | 0 |
| Eighths with the integer truncating read | 4 | One half of a fifth, or one tenth of the whole |

The projection left both carriers' value and tension unchanged. The second row accounts exactly for the source since `4/8 + 1/10 = 3/5`. If a later reader needs a common exact grid for the two equal subdivisions, fortieths work because both scales divide 40. There is no need to expand into a power-of-two binary fraction. A binary fractional format with an implicit denominator `2^k` cannot represent a reduced fraction with denominator five in finitely many bits. Storing the integer value three together with the selected five-part scale represents it exactly. [Projection arithmetic and residue](../Core/Relation.Projection.cs#L296) · [Residue representation](../Core/ProjectionResidue.cs#L6)

The same bridge can be used differently. In a material fifths-to-eighths transfer, whole packet accounting takes five source ticks for eight target ticks. With only three fifths available the probe moved zero and kept all three in stock. It is legitimate for an integer observation to publish four eighths and residue while the material route waits. A general quotient has still another contract. A whole-tick quotient of `3 ÷ 5` returns quotient zero with a numerator-basis remainder of three. A quoted rate can carry different units. Preserve which denominator belongs to a whole, a divisor, or a unit expression. [Selected material transfer](../Structures/RefinementTransfer.cs#L29) · [Quotient read](../Structures/QuantityQuotient.cs#L36)

### How I would compose variable rungs

A local equal subdivision can use its own link weight. Five coarse cells followed by a factor of two gives ten equal fine cells. If one cell alone is split, keep the old coarse cells and the new child boundaries in explicit correspondence. The resulting leaf cells can have different widths. There is no single denominator that describes those widths just by counting leaves. An explicit region partition can represent those boundaries in a common calibrated basis when one is available. Order and containment can still be exposed without numerical widths.

A mixed-radix numeral is a different construction over ordered digit roles. A fine digit with five legal residues and a next digit with three legal residues represent `low + 5×high` from 0 to 14. A further place would have weight fifteen. The local links can carry different weights without requiring doubling or one global base. Keep the available digit values, carry thresholds, and overflow destinations connected to those links. In a completed review, the upper endpoint five is a legal result. In a radix-five **digit**, the same input calls for a carry to another role. Which one applies follows from the constructed whole and operation.

A prior publication should keep its original selected basis when the construction is refined. A new link can be authorized and tested in a workspace, and a reader with only the old support still has a valid coarser view. Identity of the whole and a corresponding location matters even when two readings have equal fractions. That makes a later symbolic-regression experiment more informative. A search can propose five from an observed carry or ratio of calibrated ticks, and test at the boundary or on a newly exposed sample. Three storage planes alone leave the scale unresolved.

## A small comparison experiment for the next code round

1. **Author the rating as ordinal first.** Expose five ordered possible responses and an unresolved slot. Accept labels one through five. Verify that silence is open and zero is outside this scope. Run an order or membership query without a unit refinement.
2. **Connect equal fifths explicitly.** Use the same selected whole for an independent equal-step example. Publish 3 on its fifths frame, refine to tenths, and compare the exact six-tick result to a new finer observation. Run the eighths projection, then try the material route separately. Check the source-basis residue and the held whole-packet remainder.
3. **Compare a different five-region instrument.** Supply unequal regions on a shared axis and select the third. Refine the third region using a new boundary or observation, carrying the others. A question whose answer varies within the third region stays open until the instrument publishes more. Keep that outcome distinct from an empty admissible set.
4. **Run one reusable carry comparison.** Use a weighted five-to-one relation and an authorized inlet. Start with a full coarse digit and an empty fine digit. Admit four fine offers and retain the fifth as pending. In a second case with coarse room, check the successful carry. Change the declared group size or disable the fine connection in a parallel case. Require all exact forward tests and the requested backward, capacity, and transport checks before promoting the helper.

Start with the same three boundaries in each branch: what the reader sees, what a connected operation is allowed to change, and what the retained evidence can still answer. That gives us a shared test for choosing among Claude's builder, Neon's broader numeric layer, and Sol's small reference. The optional numeric grade remains available through the selected connection while the pre-numeric parent still answers its own ordered read.

## Verification

The table above records the EncodingC suites rerun at the three pins, with Sol in Debug and Neon and Claude in Release. Local native checks passed 61 unit-shape, projection and refinement cases, 42 coarse-read cases, and 14 resolution-ladder cases. The base-five arithmetic, dual-scale projection, unequal regions, recognizer counterexample and port-accounting contrast were run in isolated scratch probes. The numbered review adds documentation only.
