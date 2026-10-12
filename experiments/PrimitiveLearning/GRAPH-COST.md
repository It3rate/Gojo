# What makes the saved model large?

The selected response program is small. Most of the saved graph is **retained
training audit evidence**, not the four rules that issue an action. This is a
bounded but expensive retention choice in the experiment helpers.

## Measurement and reproduction

These measurements inspect `artifacts/primitive-learning/campaign-fresh`, whose
manifest starts at fresh seed identity **100**, with 1,536 training episodes and
128 warmup episodes. The saved archives come from that first seed. They are
transport sizes and unique reachable storage-table rows, **not managed heap
sizes, execution times, or measured speedups**.

From the repository root:

```powershell
python experiments/PrimitiveLearning/Inspect-GraphCost.py artifacts/primitive-learning/campaign-fresh/program.gojo --kind program
python experiments/PrimitiveLearning/Inspect-GraphCost.py artifacts/primitive-learning/campaign-fresh/query.gojo --kind query
python experiments/PrimitiveLearning/Inspect-GraphCost.py artifacts/primitive-learning/campaign-fresh/composite.gojo --kind composite
```

The inspection helper reads the documented numeric-format v2 tables without
restoring, settling, or editing the model. It follows relation endpoints,
carrier state tick references, inverse faces, attachments, unit surfaces and
unit descriptors. Its named fields use the exact local root layouts in
`ProgramLearner.BuildRoot`, `QueryLearner.BuildRoot`, and `RelayBrain.Connect`.
It is a diagnostic projection, not another execution backend.

The full file includes a 36-byte header and one 5-byte root selector. Component
costs below count only the unique reachable table rows. **Component unions
overlap and must not be added together.**

## Active structure versus retained evidence

| Selected graph or union | Reachable transport-row bytes |
| --- | ---: |
| Selected four-rule policy alone | 5,936 |
| All 16 candidates, ledgers and edit-parent graph | 116,769 |
| Active grammar, ledgers, selectors and history-boundary state | 117,695 |
| Latest program decision, including its inputs | 230,708 |
| Latest global program-retention audit | 237,138 |
| Latest program feedback | 327,955 |
| All global retention audits reached through 64 feedback receipts | 7,813,518 |
| Entire union reached through the 64 program receipts | 14,856,443 |
| Complete program archive, including header/root selector | **14,967,449** |
| Complete query archive, including header/root selector | **3,081,964** |
| Complete connected composite archive | **16,513,657** |

The program receipt union reaches approximately 99.3% of its file's bytes.
For comparison, traversing the root fields with only the receipt-group field
omitted reaches **440,067 bytes**. That is an inspection of which edge retains
which nodes, **not a valid pruned learner, a proposed new archive, or evidence
that the omitted audit semantics can be preserved for free**.

The query learner shows the same pattern: its active context rows and ledgers
reach only **8,100 bytes**, whereas the 64 receipt union reaches **3,067,098
bytes**. Omitting its receipt-group root field diagnostically reaches 46,464
bytes.

The composite is smaller than the sum of separately saved components because
they share native objects and evidence. Within it, the program-root union is
14,967,408 bytes and the query-root union is 3,081,923 bytes; those unions
overlap. The current adaptive and predictor roots reach 125,717 and 55,657
bytes respectively.

## Concrete retention paths

1. **Every program update retains a complete population comparison.**
   `ProgramLearner.Feedback` calls `RetainBest`, places that new retention
   witness in the feedback provenance, and stores the complete feedback in a
   64-element receipt ring ([ProgramLearner.cs](ProgramLearner.cs), lines
   163–178). `RetainBest` builds scores for all 16 candidates and four coverage
   claims per candidate, plus useful-improvement claims (lines 184–206).
   The saved 64 audits retain **1,024 score-bundle entries and 5,120 claims**.
   Their score bundles reach 3,611,924 bytes, and their coverage/improvement
   claims reach 3,779,547 bytes, with possible overlap.

2. **Scores rebuild equations and evidence for unchanged candidates.**
   `LearningGraph.Score` constructs weighted-sum ports, a mean frame, a penalty
   carrier and subtraction ports, then captures their predictions and ledger
   coordinates ([QueryLearner.cs](QueryLearner.cs), lines 260–279). Only one
   candidate ledger changed in the preceding program feedback, but all 16 are
   scored again. An additional initial incumbent score is also constructed
   outside the 16-entry bundle.

3. **Each credited decision carries rich input and selection ancestry.**
   `ProgramLearner.Act` creates a new 16-offer `ScopedChoice` and native rule
   selection, then keeps the supplied mode/direction publications in the
   decision (lines 142–157). Feedback bindings connect the physical outcome
   back to that exact decision. Across the receipt ring, mode-input provenance
   reaches 2,326,495 bytes and direction-input provenance reaches 1,595,260
   bytes. These include upstream rule reads, failed alternatives, snapshots and
   calibrations; they are not just two input coordinates.

4. **Fresh organizer structure has real storage cost.**
   `ReadWitness.Create` builds ordinary ordered relations rather than an opaque
   record. Repeated empty groups, proof groups and claim holders also allocate
   carriers. A default carrier has a fresh atomic unit promise
   (`Carrier.cs`, constructor; `UnitShape.Atom`). The program archive therefore
   contains 38,390 carriers, 38,241 unit shapes, 250,746 tick references and
   396,874 relations. Relation rows consume 7,143,732 bytes and tick-reference
   rows 4,011,936 bytes. The complete transport format faithfully stores this
   connected graph; it is not duplicating each shared object on every edge.

This is not a detected recursive retention of every complete prior learner
root. The dominant mechanism is the **64 full feedback bundles**, each of
which keeps a large global audit and input-read ancestry.

## Prioritized next changes

These are proposals, not changes made during the recorded studies.

1. **Separate recent duplicate-exclusion receipts from full global audits.**
   `LearningGraph.ReceiptSeen` only inspects the prior decision and outcome
   identity in each receipt's provenance (lines 309–315). Keep those native
   correspondences, candidate binding and frozen credited update in a narrow
   receipt; retain the latest full feedback/selection audit separately. Do not
   attach every full `RetainBest` audit to each receipt. Declare that older
   global-ranking proofs were forgotten. This directly targets the measured
   7.81 MB audit union without replacing native event identity with a hash or
   hidden bookkeeping table. Exact alias-rejection and archive tests must
   continue to pass.

2. **Reuse native operational structure and unchanged score reads.**
   Build the stable choice offers, calibration ports and fixed score equations
   once. A cache must remain a projection of native ledger state, with a
   connected invalidation/version premise. Recompute the changed candidate's
   score; reuse unchanged candidates' witnessed scores and coverage where
   justified. Retain enough frozen coordinates to explain each selection.
   Changing a reusable live frame must not rewrite a captured historical basis.

3. **Publish the smallest sufficient sensor service.**
   The receiver needs a fit class, direction/support, basis and unresolved
   status; an ordinary action does not need every upstream rule evaluation.
   A deliberately coarse published boundary can retain native observation
   identity and qualification while forgetting construction detail. This is a
   new explicit publication/retention contract, not permission to strip source
   fields from an existing exact witness. Full private diagnostics can remain
   in a separately requested audit.

4. **Then profile construction and traversal.**
   Reuse organizer/empty surfaces where their identity has no event meaning,
   and avoid rebuilding the complete proof population for one changed ledger.
   Measure actual allocation and runtime before pursuing a compact executor,
   SIMD or GPU implementation. Smaller archives alone do not establish faster
   decisions or faster training.

## Historical-evidence limit

The retained graphs do not promise an exact snapshot of every historical
premise. Scores and ledger before/after coordinates are captured, but program
probe/coverage claims receive `count.ValueSlot`, a live tick reference.
`OperationClaim.Build` retains that supplied coordinate as its source while
storing the computed residual coordinates. Later count updates can move the
old source premise. These are therefore retained audit graphs with some live
participants, not guaranteed exact historical replay packets. If exact
reconstruction is required, freeze the necessary premise coordinates; if the
promise is deliberately smaller, state the retained promise and forgotten
detail. No running learner semantics were changed for this inspection.
