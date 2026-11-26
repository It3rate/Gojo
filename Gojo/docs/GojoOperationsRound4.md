# Operations Round 4 — Resolution, uncertainty and integration

26 November 2026

The comparison starts from Sol `ba0746cf`, Claude `27f8e483`, Neon `c331c000`, and master `9b805964`. Claude's latest changes are the Round 14 notes over executable `a26f5b80`. Neon's latest changes are the Round 2 review over executable `059f5c45`. The code on this branch exercises one additional bounded read path. The system specification and master integration are for the next joint decision. [Round 3 comparison](GojoOperationsRound3.md) · [Claude Round 14](https://github.com/It3rate/Gojo/blob/27f8e483/ops_claude/GojoOperationsRound14-2026-11.md) · [Neon Round 2 review](https://github.com/It3rate/Gojo/blob/c331c000/Gojo/docs/GojoOperationsRound2Review-2026-11.md)

## What the arithmetic is about

When the two reports are counts from disjoint pools in the same counting basis, **three of five and two of five pool to five of ten**. The ten is the size of the joined scope. Keep both source observations. The same normalized value can be displayed as `1/2`, with the original count and coverage still connected. Exact empirical count and statistical certainty are separate. Five of ten supplies neither a confidence interval nor a warrant that the ten observations were independent.

The same writing can request other operations. Two compatible portions of one whole, each on a scale of fifths, add to `5/5`. A physical union of filled cells could give a different number if they overlap. If a three-of-five window and a two-of-five window share one actual cell, there are nine cells in the union. A known occupied shared cell makes the union `4/9`, and a known empty shared cell makes it `5/9`. If the shared cell's occupancy is unresolved, keep those as two supported possibilities under the stated counts. When the overlap itself is unavailable, leave the scope for the pool open. Distinct numeric codes, stores or publications alone cannot prove that two reports refer to different events.

The fractional notation also leaves the unit and acquisition policy open. On a proven equal-step frame, `3/5` can be an exact site. On an ordinal review it can be an ordered answer. On a coarse instrument it can name a cell within which a finer number is unresolved. Attach that cell or its support if it was supplied. A maximum of five specifies the selected grade. An error interval around an observation needs its own evidence. A reframe to tenths can place the nominal value exactly while retaining the original five-part acquisition and any variation that came with it. [Round 3 five-star example](GojoOperationsRound3.md#the-five-star-question) · [Read qualifications](../Structures/QualifiedReadSurface.cs)

| Situation | Evidence to preserve |
| --- | --- |
| Both pools have settled outcomes | The exact pooled count, both originals, and the eligible whole. |
| Each five-member pool has one pending outcome | With three and two established successes, respectively, the final pooled count has support `{5,6,7}/10` under the declared finite model. With hidden dependence, treat this as conservative support until more joint evidence is exposed. A response-only count of `5/8` would be a different read. |
| A selected observation or proof of disjoint scope is unavailable | Leave the dependent read open. Keep the missing connection or request visible. |
| An input was explicitly zero or had empty support | Zero is a possible count. Empty support is a contradiction across the declared roles. |
| A new integer scale loses representable detail | Retain the exact remainder in its basis and keep the original observation. An interval read rounds its lower and upper bounds outward. |
| A material move cannot fit | Preserve waiting stock, pressure, debt or a blocked passage through the selected settlement law. |

Unrepresented detail in a read is residue. Disagreement with a prediction can remain a claim residual. Introduce material tension only through a connected action that authorizes change. A finer port can answer a question without spending stock.

## Implemented in this round

**Finite counts in EncodingC.** `PartitionReads` builds ordinary finite relation laws on exact count domains. One addition binds a pair of declared disjoint scopes and publishes the sum and the joined whole. Another binds compatible amounts on a common whole and returns the sum on that same basis, including an amount larger than the whole if the arithmetic requires it. Their bindings and whole roles explain why the same written example can give `5/10` or `5/5`. The disjoint-scope law relies on the caller's selected scope evidence. Overlap and independence stay questions for that evidence. The fixtures build reports from actual members, preserve open support, and keep a refused scope from connecting. [Partition reads](../../Gojo.EncodingC/Operations/PartitionReads.cs) · [Pooling fixtures](../../Gojo.EncodingC/Tests/OperationPartitionPoolingTests.cs)

**Remainder and joint support.** A repartition on a common whole uses the bounded relation
```text
source count × target parts = target count × source parts + remainder
0 ≤ remainder < source parts
```
The integer remainder is in units of `1/(source parts × target parts)` of the whole. Reading `4/5` in thirds gives `2/3` and integer remainder `2` in fifteenths, equivalent to `2/3` of one source fifth. This is the basis correction to Neon's §2.4 example. Reading `3/5` in eighths gives four eighths and `4/40` of the whole. Reading the same sample in tenths gives six tenths and zero remainder. These operations change the representation and retain the finite source constraint. Multiplying denominators during an explicitly aligned calculation supplies a grid for the calculation, and carries no new observation by itself. [Neon's example](https://github.com/It3rate/Gojo/blob/c331c000/Gojo/docs/GojoOperationsRound2Review-2026-11.md#L193)

When the source supports both three and four fifths, the output and remainder are linked. The two admitted pairs of integer codes are `(4,4)` and `(6,2)`. The two marginal supports allow four combinations if the link is discarded. The test reloads the connected relation and narrows it again, and a separate test rejects the crossed pair `(4,2)`. Another test takes `2/5` and `1/3` as equal-step counts. The disjoint pool gives `3/8`. In a separately declared common-whole operation, exact refinement to fifteenths and addition give `11/15`. [Repartition and common-frame tests](../../Gojo.EncodingC/Tests/OperationRepartitionTests.cs)

**Native qualified read correction.** The existing native surface already connects basis, value, variation, coverage and source history. Its outward-bound helper previously handled only a whole-tick residue. A fractional residue could make a bound round inward. It now reads the existing projection-residue structure for both forms. An authored `[2,3]` interval in fifths yields the enclosing eighth-tick bounds `[3,5]`. In the reversed target frame it yields `[-5,-3]`. The nominal residue, original scope and coverage remain connected. Existing native affine, finite-read and bounded-quantity code remains available for appropriate calibrated arithmetic. The EncodingC experiment uses its existing finite supports where discrete holes and the exact admitted tuples matter. [Qualified read implementation](../Structures/QualifiedReadSurface.cs) · [Regression tests](../../Tests/Gojo.Tests/Core/QualifiedReadSurfaceTests.cs) · [Native finite reads](GojoFiniteReadFixture.md) · [Native bounded quantities](GojoBoundedQuantityFixture.md)

For multiplication, the underlying mechanism determines the result. A product for two explicitly constructed independent cell axes can retain the Cartesian pairing and the candidates that filled it. Two predicates selecting the same cells need a shared-scope overlap or Boolean read. A physical stretch needs its datum and units. The existing pair-formation, factored-product and scoped-gain fixtures already exercise those differences. A general propagation rule for nonlinear functions, including inferred dependence, is outside this bounded slice. [Pair formation](../../Gojo.EncodingC/Tests/OperationPairingTests.cs) · [Shared support and factoring](../../Gojo.EncodingC/Tests/OperationFactoringTests.cs) · [Scoped gain](../../Gojo.EncodingC/Tests/OperationScopeTests.cs)

## What I would take and leave

| Branch | Take into the integration candidate | Leave at its current scope |
| --- | --- | --- |
| **Sol** | Use the smallest runnable read and material distinction, true scope and ownership in fixtures, equivalent-forward versus exact-joint checks, checked grouped edits, and save/load evidence. Retain a separate result whole and the repartition remainder, with the native outward-bound fix. | This branch still has small internal builders rather than a general arithmetic library. It also supplies the candidate families and the membership qualification in its fixtures. Keep the reference numbers and selected policies as tests. Avoid giving them authority over unrelated encodings or source scopes. |
| **Claude** | Take the compact balance/polynomial construction as a candidate producer. Take numeric free-role costs when an operation explicitly chooses a completion, and the examples of asking a useful distinguishing question of an incomplete model. Keep the hand-wired mixed-column and exact common refinement experiments as useful reference cases for a later rung. | Keep proposals conditional on observations and the actual read. One positive-witness equation can admit forbidden cases even if it fits every supplied tuple. Equal table bits can carry different port accounting. Keep a count's capacity separate from calibration, resolution, coverage and admissibility of the scope. Exact fifths can be represented on an aligned grid divisible by five. A single radix per adapter is a helpful encoding choice with limited authority. |
| **Neon** | Take the demonstrated nonbinary column arithmetic, explicit weighted links, conservative residue principle, and differential compiler checks. Keep the idea of retaining review count alongside the accumulated total. Use verified, eligible fast paths as separate proposals under the same observable contracts. | Keep bulk and native kernels out of the first master change. The older transport bits need an explicit compatibility story. An isolated load of a master-saved Toggle machine threw a format error on Neon's current code. Shared receipt writes in a separate test still made simultaneous sweep moves contend, even though receipt capacity alone no longer gates a move. Publish optional history from actual accepted activity on a separate route when its capacity should be independent. Treat mixed-radix builders and live-denominator schemas in the latest notes as proposals. |

Claude’s exact common-refinement example is good for **compatible portions**. It leaves open the choice of statistical model and the role that can supply more information. Neon’s representation of the result and review count as distinct incidences is the better starting point for **pooled reports**. The native qualified reader is the reference for a published interval and its provenance. Connect one of those read contracts explicitly before an optimization chooses wording, range or layout. [Claude proposals and bounds](https://github.com/It3rate/Gojo/blob/27f8e483/ops_claude/GojoOperationsRound14-2026-11.md#L207) · [Neon partition and observation discussion](https://github.com/It3rate/Gojo/blob/c331c000/Gojo/docs/GojoOperationsRound2Review-2026-11.md#L228) · [Neon transport](https://github.com/It3rate/Gojo/blob/c331c000/Gojo.EncodingC/Transport/Transport.cs#L219) · [Sweep law](https://github.com/It3rate/Gojo/blob/c331c000/Gojo.EncodingC/Flow/Flows.cs#L93)

## Who should write the master change

I would use **the Sol approach as the integration base and have one writer assemble the first master change**. I would take that role if the team chooses this specification. Sol has the least engine and format surface to reconcile and the most direct tests for held states and what a selected read actually meant. That fit matters more to me for the first integration than how many operation families any branch demonstrates. Sol does have a weakness. Its fixtures carry policy that a reusable interface would still need to expose. I would use Claude's review to push back on unnecessary specialised laws and Neon's review to catch persistence, schedule and backend assumptions.

I would ask **Claude to lead the follow-on algebra and recognition work**. The weighted equation and explicit completion cost are a better reusable direction than growing a hand-named law for every case. Prove the accepted relation over the full declared domain, preserve an open hypothesis when there is no authorized probe, and keep accounting and timing tests on any derived enactment.

I would ask **Neon to lead the follow-on compiler work**. That branch has the strongest concrete execution evidence. Version or preserve the transport first. Compare the interpreted and compiled paths on the same ports, traces, outputs, time budgets, and witnesses. The speed test should include layout conversion, eligibility and fallback as well as the arithmetic kernel.

I would bring three things into the first integration candidate together:

1. The native fractional-bound correction and published-scope evidence.
2. A small EncodingC read slice with explicit whole, unresolved scope and retained residue.
3. Conformance fixtures that can run against later builders and backends.

The wider work to defer includes a general mixed-radix adapter, arbitrary nonlinear interval inference, probabilistic confidence, automatic derivation of overlap from opaque publications, automatic acceptance of learned operations, and new wire-format bits. These need distinct observation or compatibility contracts. A first merge can preserve the places where that evidence belongs and let an unsupported request stay open. There is no reason to reduce the ordinal, qualitative, and open-support grades to exact numerals to do that.

## Acceptance checks for the shared specification

- Two actual scopes can yield exactly `5/10`. A replicated view of the same member sites cannot serve as a disjoint second scope.
- Two pending outcomes keep `{5,6,7}/10` across save and load. A later authorized observation narrows the same graph. A missing scope, exact zero, and contradiction remain distinguishable.
- The selected whole and calibration distinguish `5/10`, `5/5`, and common refinement of compatible portions.
- A reframe preserves source-relative loss and never rounds away admissible possibilities. A reader can recheck a joint value and residue after load. A finer display alone does not raise the source acquisition resolution.
- Pre-numeric order, occupancy, orientation, and held-material behavior remain usable without a numeric binding. Every admitted action has its own accounting and permission.
- A fit, compiler, or future codec qualifies against these reads and preserves the relevant old save before replacing an execution path.

## Verification

The branch run passed 261 EncodingC tests and 1,074 native Gojo tests, including 13 new EncodingC cases and the two signed-frame qualified-read cases. The comparison also used isolated probes for transport compatibility, simultaneous receipt contention, and native bounded quantity composition. The companion executable trees match the last pinned suite runs at 264 Claude and 257 Neon tests.
