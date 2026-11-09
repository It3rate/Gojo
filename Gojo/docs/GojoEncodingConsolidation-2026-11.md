# Consolidating the encoding experiments into one plane–law substrate

Status: **exploratory proposal**, 9 November 2026. Documentation only. No code,
kernel rule, test or experiment was changed. It reviews the ODV / primitive
measure line (30 October – 7 November), the native ornament machine studies
up to `bb6856d7`, the finite-law compilers, the boundary/flow codex, the
uncertainty and numeric-format references, and the `Gojo.Encoding` notes. It
then proposes one structure they could all share.

Labels follow the canonical prompt: **implemented** (code and tests),
**documented direction**, **exploratory**, **superseded**. Everything in
sections 5–10 is exploratory unless it says otherwise.

## Reading guide

| If you want | Read |
| --- | --- |
| The honest map of what exists | §1 |
| What to keep and what to change | §2, §3 |
| Where uncertainty, resolution, missing structure and prediction live | §4 |
| The proposed structure | §5 |
| Concrete bit-level examples | §6 |
| CPU / GPU / FPGA mapping | §7 |
| The path toward learning, concepts and language | §8 |
| Gaps, risks and a step-by-step plan | §9, §10 |

## 1. What exists today

| # | Lineage | Where | What it contributes | Status |
| --- | --- | --- | --- | --- |
| 1 | ODV word and coarse arithmetic | `experiments/PrimitiveMeasure` (`ThreeBitMeasure`) | Orientation / direction / zero-nonzero; addition as a set of possible words; reorient ≠ reverse | implemented (experiment) |
| 2 | Positional storage, stable ports | `PositionalBuckets`, [StablePortExperiment](../../experiments/PrimitiveMeasure/StablePortExperiment.md) | Constants supplied by array position; port → binding → address; migration widens without changing reads | implemented (experiment) |
| 3 | Rows, read laws, support masks | `EndpointRows`, `BitRelation`, `ReadConnections` | Ordered local references; `8^n` tuple bitmaps shared by rows; 8-bit support sets; same-reference constraint | implemented (experiment) |
| 4 | Transition laws, scope prediction | `PassageLaw`, conditional, saturation fixtures | 64-bit before/after laws; joint support over a scope (`2^sites` bits); prediction never executes | implemented (experiment) |
| 5 | Families, signed uses, guarantees | `FamilyTable`, `FamilyCodec`, structural guarantees, seam ladders | Template stored once, packed instances; shared value references; reversal as a read; positional cycles carry guarantees | implemented (experiment) |
| 6 | Unary quantities and settlement | `GapSettlement`, `StripWalk` | Prefix-occupied unit slots, shifted comparisons, conserving one-unit transfers | implemented (experiment) |
| 7 | Repetition, goals, evidence-seeking | repetition, goal-flow, tension-choice, washer and timing-socket fixtures | Phase cycles, request/completion, work packets, probe selection, an earned test socket | implemented (experiment) |
| 8 | Native finite laws and compiled plans | `experiments/QualitativeExecution` (`FiniteLaw`, `FiniteLawPlan`, `FiniteLawBatchPlan`) | Laws as relation rows with open/closed scopes; derived mask backends with staleness checks; ≈196× on a recurrent circuit without per-read witnesses | implemented (experiment) |
| 9 | Native machine studies | `experiments/QualitativeFrieze/Composition` | Programs as native relations; boundary / flow / topology exchange reads; coordinated trials; pass-level preference memory; `.gojo` archives | implemented (experiment) |
| 10 | Pressure routing | `experiments/OrnamentTension` | Signed unit pulses, capacity, next-beat acceptance, route strength 0–4 revised by achieved relief, policy bytes, one public byte | implemented (experiment) |
| 11 | Kernel carriers and transport | `Gojo/Core`, [numeric format v2](GojoNumericFormat.md) | Carrier value u64 / tension i64, settlement, graph transport tables | implemented kernel |
| 12 | Boundary / flow lanes | `Gojo/ConceptSurface` codices | (left, right boundary) × (forward, inverse flow): sixteen summary reads; `PF → NF` transitions | documented direction, partly prototyped |
| 13 | Concept addresses and hint tokens | [Gojo.Encoding](../../Gojo.Encoding/README.md) | Integer-first; hint tokens (axis, lane, polarity, strength); bundle sketch | documented direction |
| 14 | Bounded and finite quantities | [bounded quantities](GojoBoundedQuantityFixture.md), [finite reads](GojoFiniteReadFixture.md) | Shared-slot affine bounds; finite capacity and quantum publication | implemented fixtures |

The ingredients are mostly present. Three things are missing: one substrate
they all lower into, one tick contract that says what a parallel step means,
and one account of tension. Counting conservatively, the compact experiments
define more than ten independent bit layouts, each with its own framing,
reference widths and host-driven stepping.

## 2. What should be kept

These are the decisions I would protect through any consolidation.

**K1. Possibility is a separate plane from value.** ODV never squeezes
"unknown" into its eight words. Uncertainty is a support set *over* words.
This is the most important design decision in the line, and it generalizes
cleanly (§5.6).

**K2. Laws are data shared by rows.** Topology (which sites) and rule (which
tuples) are separate. The executor reads bits, not names. Small laws can be
verified exhaustively, and tables lower directly to hardware (§7).

**K3. Constraints live in position.** A fixed-sign entry has no sign bit to
corrupt; releasing a constraint is a structural move, not a value write. This
is a type system expressed in the address space. It is the most physical idea
in the work, and §5.2 generalizes it into the inspectability contract.

**K4. Identity is carried by references.** `X ⊕ X = 0` but `X ⊕ Y ∈ {0,1}`;
one site used twice differs from two sites; a shared seam differs from
coincident edges. Every lowering must preserve this.

**K5. Prediction is not execution, and observation is not material.** Joint
support for predictions, refused steps on unresolved control, non-draining
reads.

**K6. Accounting is an identity.** `offered = accepted + blocked + remainder`,
and opposed participation is retained beneath a zero net.

**K7. Native authority with derived compiled plans.** `FiniteLawPlan.IsCurrent`
is the right shape for every CPU, GPU or FPGA backend: a plan derived from
connected structure, refusing to run after that structure changes.

**K8. Bounded claims.** Preregistered protocols, controls, retained
counterexamples and explicit "what this does not establish" sections are rare
in machine-learning work and worth keeping.

## 3. What I would change

**C1. Stop inventing a packed layout per experiment.** Every compact fixture
designed its own framing and stepping. Each new experiment should lower into
one substrate (§5) or document why it cannot.

**C2. Retire ODV as a storage word; keep it as a named read.** The word mixes
three concerns that later work separated:

- **O** is a property of the basis (which sense is positive). It belongs to the
  tile's position, as positional buckets already showed, not to each value.
- **D** is a sign within that basis: a plane.
- **V** is the coarsest magnitude rung under an exact-zero contract. It was
  also used for participation (deep dive §3) and for "resolved by this reader"
  (the threshold contract in `DirectionMemory`). Those are three planes.

D at zero also doubles as direction memory, which is controller state and
should have its own plane. ODV's single direction bit also has to be
reconciled with the canonical distinctions between scalar sign, opposed flow
face, boundary side and reversed traversal (Axiom 4, §4.3). The substrate
gives each its own plane where a structure needs it; traversal reversal stays a
property of row reads, as `FamilyTable` already does.

**C3. Make tension one thing.** Today there are three: kernel carrier tension
(a signed i64 absorbed into value), OrnamentTension's unit pulses with
capacity and acknowledgements, and the compact layer's "the step holds", which
has no tension plane at all. §5.9 proposes one definition with a tiered energy
read.

**C4. Replace exponential joint masks with scoped supports.** `BitRelation` is
`8^arity` bits, scope masks are `2^sites`, and `FiniteLawPlan` is
`2^L × 2^R`. Keep exact joints inside law scopes, propagate between scopes,
promote clusters only when needed, and record any lost correlation (§5.6).

**C5. Declare the schedule.** Each fixture has its own host beat. A tick
contract with phases and a confluence class per law (§5.10) gives parallel
hardware a defined meaning.

**C6. Reproduce witnesses rather than store them.** The repository tracks 615
`.gojo` archives (about 65 MB in the working tree; the pack is about 943 MB).
Determinism, retained inputs and checkpoints let a witness be rematerialized
on request (§5.12). The ≈196× compute lane already depends on dropping
per-read witnesses.

**C7. Give laws their own uncertainty.** Every law today is an authored,
certain bitmap. Learning needs rows that are admitted, unsupported, or not yet
observed (§6.4).

**C8. Write a positive specification.** The documents are excellent at saying
what a fixture does not establish. No single page says what the current
encoding contract *is*. Once the first roadmap step lands, §5 of this note
could become that page, with a conformance suite built from existing fixtures.

## 4. The non-signal parts in one place

Each kind of non-signal participation gets its own physical location, so a
bit can never be ambiguous between them. This table is the core of the
proposal.

| Kind | Example | Where it lives | What it is not |
| --- | --- | --- | --- |
| Missing structure | Unbound role, unknown seam | `B` (bound) plane = 0; row reference to a placeholder site | Not zero, not an unknown value |
| Unobserved | Control hidden by a visibility mask | `E` (observed) plane = 0 this epoch; support planes span the domain | Not missing structure, not zero |
| Participating zero | Zero-extent notch phase | `B = 1, E = 1, P = 0` | Not absent; a finer rung may read nonzero |
| Competing candidates | Bowl side of b / d | Support popcount > 1 | Not a probability |
| Contradiction | No admitted tuple | Support popcount = 0, plus a residual on the laws that emptied it | Not a measured zero |
| Resolution loss | Coarse publication | Rung planes plus residue planes (exact) or loss-support planes (lossy) | Not uncertainty about the source |
| Projection residue | Integer affine read | Residue planes on the read | Not tension unless explicitly imported |
| Blocked / overflow | Occupied receiver | Per-passage blocked mask; per-receiver overflow count | Not destroyed participation |
| Tension | Unmet draw, goal gap | `T+` / `T−` planes at sites; residuals at laws | Not confidence |
| Stale | Captured plan after an edit | Epoch mismatch between plan and source | Not wrong, only unverified |
| Prediction | Forecast after a passage | Shadow planes, a `Q` provenance plane, issue epoch and horizon | Not observation; never moves material |
| Decorrelation (new) | Correlation dropped between scopes | Loss record on the propagation witness | Not independence |

## 5. The proposed structure: a plane–law substrate

### 5.1 Seven nouns

```text
law store (shared, immutable)
  law = admitted-tuple table | transition table | rung map
        + accounting signature + confluence class

tile (one owner; fixed capacity N sites; N = 64 by default)
  site grammar   which planes exist and in which order (positional, declared once)
  basis          orientation and unit promise (positional constants)
  planes         bit-sliced state: plane[p] is one N-bit word
  rows           ordered local references (CSR), or implicit topology
  law refs       row group -> law
  ports          exposed (plane subset, site subset, basis, accounting class)
  epoch          increments on every structural edit

ladder           rung tiles + rung maps + residue / loss planes
tick contract    ingest -> observe -> propagate -> offer -> accept -> account
                 -> (adapt) -> (restructure)
witness lane     checkpoints + ingest log + law hashes -> replay -> witnesses
```

Tile, plane, row, law, port, rung and tick. Everything in the current compact
experiments maps onto these; §6 shows four of them.

### 5.2 The address-meaning invariant

> Every stored bit has an address — (tile, plane, site) or (law, tuple index) —
> and that address, read through the tile's site grammar and the law store,
> determines what the bit means. No bit's meaning depends on a host object, a
> name, or another bit's value except through declared rows. Padding is
> declared padding.

A universal decoder can then print the meaning of any bit in any buffer. That
is the inspectability contract, and it is K3 generalized: position supplies
meaning everywhere, not only for O and D. A plane that is constant across a
tile (for example "all sites bound") is not stored at all; the grammar supplies
it.

**Authority: a clarification is needed.** The canonical prompt makes connected
Gojo structure the source of meaning, and a packed tile must not become a
second ontology. The reconciliation I propose: the site grammar and law store
are themselves connected native structure (relation-shaped descriptors), so a
tile is a *crystallized positional basis* in the sense of Axioms 11 and 12.
Suggested one-sentence clarification for the prompt (not applied here):

> A packed layout whose site grammar and laws are connected Gojo structure is a
> crystallized basis; its bits carry meaning through that basis. Host buffers
> without such a descriptor are adapters.

Editing either the native structure or the tile makes the other stale until it
is re-crystallized or decoded, exactly as `FiniteLawPlan.IsCurrent` behaves.

### 5.3 Planes

A site grammar selects a subset of these. Per-site planes are bit-sliced:
plane `p` of a 64-site tile is one `u64`, so one instruction applies one law
to 64 sites. This struct-of-arrays layout is what SIMD units, GPU warps and FPGA
registers want.

| Group | Plane | Bits per site | Meaning | Written by | Today's ancestor |
| --- | --- | --- | --- | --- | --- |
| Structure | `B` bound | 1 | Role is bound to a participant | Structural edit only (epoch++) | Placeholder binding; liveness bitmap |
| Evidence | `E` observed | 1 | Evidence present this epoch | Observe phase | Visibility mask |
| Evidence | `S[v]` support | domain size | Value `v` still possible | Propagate (narrow only within an epoch) | Support masks |
| Evidence | `Q` predicted | 1 | Support derived by a forward law, not observed | Shadow propagation | `PredictScope` |
| Participation | `P` present | 1 | Nonzero at the rung-0 exact-zero contract | Settlement | V |
| Participation | `D` sign | 1 | Negative within the tile basis | Settlement, reversal | D |
| Participation | `F` face | 1 | Opposed flow face, where the grammar has lanes | Lane transition | ConceptSurface F/I |
| Participation | `M[k]` magnitude | K | Thermometer (`bit k ⇔ magnitude > k`) or a declared place-value code | Settlement | Unary gap slots |
| Tension | `T+[k]`, `T−[k]` | 2K | Unaccounted positive / negative participation, kept opposed | Ingest, settlement | Carrier tension; pulse stock |
| Controller | `H` history | h | Last resolved sign, phase bits | Controller law | `DirectionMemory` M.D; phase cycles |

Adaptation state belongs to row groups rather than sites:

| Plane | Bits | Meaning | Ancestor |
| --- | --- | --- | --- |
| `W` strength | 2–3 | Saturating route preference | Route strength 0–4 |
| `L` eligibility | 1–2 | Recently carried a unit; decays | none (proposed) |
| `C` counts | small | Witnessed tuple counts for learned laws | none (proposed) |

### 5.4 Topology grades

1. **Implicit.** Chain, ring, grid or tree by index; a neighbor is index
   arithmetic and no rows are stored. Guarantees come from the grammar: the
   positional 16,384-step cycle in the structural-guarantees survey reads zero
   endpoint rows. This is the most physical grade and should be preferred.
2. **Explicit local rows.** Ordered references within a tile, stored as CSR
   (offsets plus packed references). A repeated reference is the same site
   (K4).
3. **Ports.** Cross-tile participation only through exposed surfaces (§5.13).

Moving between grades is an explicit structural edit that increments the
epoch, as "expand to pair rows" already is.

### 5.5 Laws

Three table kinds:

- **Relation table**: admitted tuples over role domains. Observation only; it
  never mutates. (`BitRelation`, `FiniteLaw`.)
- **Transition table**: admitted (before, after) pairs over a scope. Material
  or state change. (`PassageLaw`.)
- **Rung map**: coarse ← fine abstraction with its residue rule, and the fine
  supports admitted by each coarse value. (§5.8.)

Each law carries:

- **An accounting signature**: invariants checked by exhaustive enumeration
  when the law is loaded, such as "popcount of material roles preserved",
  "control role unchanged" or "no after-state duplicates a source". The passage
  grammar already does this for one law family; this generalizes it.
- **A confluence class** (§5.10): C1 monotone narrowing, C2 self-local
  transfer, C3 descent with a declared schedule, C4 driven or periodic.
- **A content hash.** This gives *table* identity, not concept identity. The
  same table attached to different planes means different things, and carriers
  and sites remain identity-bearing (Axiom 2).

A relation or transition table compiles to a sum of products over bit-sliced
planes: for each admitted row, AND the matching input planes and OR the result
into the output planes. Section 6 shows this concretely.

### 5.6 Supports and scopes: the uncertainty ladder

| Rung | Representation | Exactness | Today |
| --- | --- | --- | --- |
| U0 marginal | `S[v]` per site | Loses all joint information | Per-role support masks |
| U1 row-local joint | `J[t]` planes, one per tuple of a law's scope | Exact within one law | Single-passage prediction |
| U2 cluster joint | Merged scopes | Exact within the cluster | The five-site 32-bit mask |
| U3 invariant-restored | U1 plus accounting signatures used as constraints | Recovers much of U2 at U1 cost | none (proposed; §6.2) |
| Quantity supports | Affine forms with shared noise symbols; each symbol is a source or observation identity | `X − X` cancels exactly; two instances do not | `BoundedQuantity` shared slots |

Propagation is generalized arc consistency: project each scope's joint support
onto shared sites, intersect, push the result back, and repeat to a fixed
point. Narrowing operators are monotone, so the result does not depend on the
order of application (Apt 1999). Every projection that discards correlation
writes a decorrelation record into the witness. Promoting a cluster is an
explicit, costed choice.

**Epoch rule.** Within an epoch, supports only narrow. Evidence that a source
*changed* starts a new epoch in which supports may widen; the old epoch's
result remains history. This is the formal version of the distinction
between refinement and changed state in Round 6 of the connection experiments.

### 5.7 Semiring reads

The same rows and tables can be read under different algebras. The algebra is
the read's declared law (Bistarelli, Montanari and Rossi 1997).

| Semiring | ⊕ / ⊗ | Read | Hardware |
| --- | --- | --- | --- |
| Boolean | OR / AND | Possibility support (default) | Bitwise |
| Counting | + / × | Number of admitted completions: ambiguity size without probability | popcount, integer add |
| Tropical | min / + | Least-tension or least-cost completion; planning | Integer min/add; race logic |
| Bottleneck | max / min | Capacity through routes | Integer max/min |
| Probability | + / × | Only when a justified probability law is connected | Fixed point at the boundary |

This answers "how can alternatives be ranked without an invented confidence
field?" Counting and tropical reads are exact, integer and inspectable.
Probability remains opt-in, as canonical prompt §5.2 requires.

### 5.8 Quantities and rungs

Rung codes, in order of increasing promise:

| Rung | Code | Operations | Status |
| --- | --- | --- | --- |
| R0 presence | 1 bit (`P`) | OR, AND | implemented (ODV V, presence sites) |
| R1 thermometer / unary | K bits; bit k ⇔ magnitude > k | add = shift in, subtract = shift out, compare = mask test | implemented as gap stores |
| R2 place value | K bits ⇒ 2^K levels, only under a declared weight grammar | binary arithmetic | documented direction (deep dive §22) |
| R3 rational / affine | numerator and subdivision; calibrated reads | kernel arithmetic | implemented kernel |

R1 is monotone and physical: more participation sets more bits (canonical
prompt §9.2). R2 arrives as a crystallization of R1 when the required queries
and updates make it worthwhile; it is never assumed from a payload width.

Rung maps form a Galois connection. The abstraction `α` maps fine to coarse
(for example `α₁→₀(t) = t ≠ 0`); the concretization `γ` gives the fine supports
compatible with a coarse value (for example `γ₀(P = 1) = {1..K}`). Soundness
means `α(fine) ∈ coarse support`: a refinement never contradicts a valid
coarse read. An exact rung keeps residue planes; a lossy rung keeps
loss-support planes and promises no reconstruction.

Coarse presence over children is an OR-pool, and a coarse count is a sum-pool
with carry residue. These are the reductions GPU mipmaps already perform.

### 5.9 Tension and energy

- **Site tension** is held in `T+` and `T−` planes. They are never netted
  automatically (K6). Ingest adds packets (Axiom 5); absorption moves them
  into `P` / `M` within capacity; unmet draw stays.
- **A law residual** `r_k` is the count of violated tuples given committed
  supports, or an empty support, for a relation law; for a claim or goal it is
  the signed mismatch in its own basis.
- **Energy** is read per tier and compared lexicographically:

```text
E_hard        accounting violations          always 0; nonzero = bug or contradiction
E_obligation  construction equalities, claims
E_goal        desired relations
E_pref        preferences, costs, aesthetics
```

**Admissibility.** A transition-law application is admissible if it does not
raise lexicographic energy and either strictly lowers it or is a declared
neutral transfer. Finite state plus strict descent guarantees termination.

**Quiet is not optimal.** A quiet state with nonzero `E_goal` is a legitimate
output. Its witness names the blocking topology. This makes Axiom 11
computable: the orthogonal remainder is residual that no available route can
lower.

**Escapes**, each visible:

1. Widen the scope by connecting a route.
2. Take an explicit *tension loan*: a budgeted, recorded temporary increase,
   like min-conflicts or WalkSAT noise steps but accounted.
3. Propose a structural edit on the slow clock.

Which tension goes to which route is a learned C3 schedule (route strengths),
not a global optimizer.

### 5.10 The tick contract

| Phase | Reads | Writes | Notes |
| --- | --- | --- | --- |
| 0 ingest | ports | `T±` | Axiom 5 |
| 1 observe | sensors, ports | `E`, `S`; epoch | New epoch if a source changed |
| 2 propagate | `S`, `J`, relation laws | `S`, `J`, residuals | C1 fixed point; an empty support becomes a residual |
| 3 offer | committed state | offer buffer | Read only |
| 4 accept | offers | each receiver's own planes | Capacity, declared priority or rotation |
| 5 return | offers, accepts | blocked, remainder | Checks `offered = accepted + blocked + remainder` (`E_hard`) |
| 6 account | all | residuals, energy, public summaries | Port publications |
| 7 adapt (every k) | acknowledged relief | `W`, `L`, `C` | Medium clock |
| 8 restructure (every m) | persistent residuals | rows, laws, templates; epoch | Slow clock |

Phases 3 to 5 are double-buffered, so they are race-free on parallel hardware.
This matches OrnamentTension's next-beat acceptance.

Confluence classes give each law a defined meaning under parallel execution:

| Class | Example | Property |
| --- | --- | --- |
| C1 monotone narrowing | Support propagation | Any fair order reaches the same fixed point |
| C2 self-local transfer | A site emits based only on its own state (chip-firing-like) | Order-independent final state when the abelian property holds |
| C3 descent with schedule | Pairwise gap balancing, competing outlets, priority merges | Terminates; order can matter, so the rotation or priority is state and part of the witness |
| C4 driven / periodic | Clocks, phase cycles, repeat controllers | Needs an explicit controller and stop evidence |

There are three clocks: settle (phases 0–6), adapt (7) and restructure (8).
`PassPolicyMemory`'s pass-level revision already acts on the middle clock.

### 5.11 Prediction as shadow state

Prediction runs phases 2–5 on a shadow copy of the support, joint and material
planes, with the `Q` plane marking derived support. It never writes committed
planes, so prediction cannot move material by construction. A forecast records
its issue epoch, horizon and assumed scenario (which actions it assumed). When
the outcome arrives, the mismatch becomes a claim residual on the forecast,
and that residual feeds the adapt clock. This is speculative execution with a
commit point. It is also the shape needed later for coarse rungs predicting
fine supports (§8).

### 5.12 Witness lane

Compute lanes retain no per-read witness. The evidence lane retains
checkpoints every n ticks, the ingest log, law hashes and C3 schedule state.
Any witness at a retained tick can be rematerialized by replaying the native
interpreter over the selected site's dependency cone. Native/compiled parity
tests, which already exist for selected traces, become the guarantee.

Forgetting means dropping checkpoints and logs beyond a horizon. Witnesses
before it become explicitly unrecoverable (open), while counters and summaries
remain. This is the existing lost-history surface, made systematic.

### 5.13 Ports and basis contracts

A port publishes a plane subset, a site subset, a basis descriptor (law hashes
for its codes, rung and unit promise) and an accounting class (observation,
material or coupled). A receiver connects only through a declared
correspondence law from that basis to its own. Without one, the connection is
an open slot with residue rather than a silent reinterpretation. This closes
the gap noted in the invariance experiments: the generic port interface does
not yet carry a self-describing basis. Observation ports may fan out; material
ports cannot, and the accounting signature enforces that.

## 6. Worked examples

### 6.1 ODV as a read over planes

Site grammar `{P, D}`, with O supplied by the tile basis and `B`, `E` elided as
constant. Then `word(i) = (O, D[i], P[i])`.

```text
reorient tile:        O ^= 1;  D ^= B          (flips O and D together)
reverse selection m:  D ^= m
```

For addition in one orientation, give each operand four support planes
(`+0`, `+?`, `−0`, `−?`), and let `any_a` be the OR of operand a's planes:

```text
r[+?] = (a[+?] & any_b) | (b[+?] & any_a)
r[−?] = (a[−?] & any_b) | (b[−?] & any_a)
r[+0] = a[+0] & (b[+0] | b[−0]) | a[−0] & b[+0] | a[+?] & b[−?] | a[−?] & b[+?]
r[−0] = a[−0] & (b[−0] | b[+0]) | a[+0] & b[−0] | a[+?] & b[−?] | a[−?] & b[+?]
```

That is about twenty bitwise operations for 64 independent additions with the
full possibility semantics of `ThreeBitMeasure.Add`. An empty operand support
yields an empty result, as `BitRelation.Predict` requires. When both operands
use the *same* reference, only diagonal tuples are admitted, so `r = a`: at
this rung `X + X` keeps `X`'s word. A same-reference subtraction law could
admit only zero (the bounded-quantity fixture's `X − X = 0`); that is a new law
choice, not a property of the historical codec.

### 6.2 Sixty-four passages at once, and the correlation problem

Planes `S`, `C`, `T`; bit i belongs to passage i.

```text
S' = S & (~C | T)
T' = T | (S & C)
C' = C

offered  = S & C        accepted = S & C & ~T
blocked  = S & C & T    held     = S & ~C
```

The accounting signature, checked once at load over the 64-bit table:
`popcount(S) + popcount(T)` is preserved and `C` is unchanged.

`Allows(before, after)` is a six-input Boolean function. Its 64-bit table, with
storage bytes `01 02 04 00 10 20 48 80`, is exactly the initialization value of
one FPGA LUT6: `INIT = 64'h8048201000040201`. The deterministic step is three
LUT3s, one of which is a wire.

**Row-local joint (U1).** Give each passage eight planes `J[0..7]`, indexed by
`S + 2C + 4T`. Prediction is:

```text
J[6] |= J[3];  J[3] = 0
```

Two operations predict 64 passages. Before `{1, 3}` (`0x0A`) becomes after
`{1, 6}` (`0x42`), matching the passage chart.

**The chain `A → B → T`, with C1 unknown and C2 open.** Under U1, the scopes
`(A, C1, B)` and `(B, C2, T)` meet only through B's marginal. After both
predictions the marginals are `A ∈ {0,1}`, `B = 0`, `T ∈ {0,1}`, and their
product admits `A = T = 0` (a lost item) and `A = T = 1` (a duplicated item).
The true answer is `{A = 1, T = 0}` or `{A = 0, T = 1}`.

- U2 recovers it with the five-site, 32-plane cluster (the existing mask).
- U3 recovers it at U1 cost: the passages' accounting signature implies
  `popcount(A, B, T) = 1`, and that cardinality constraint removes both
  spurious states during propagation.
- With neither, the witness records the decorrelation instead of presenting
  the product as an answer.

Accounting signatures therefore do double duty: they validate laws, and they
cheaply restore exactly the correlations that conservation implies.

### 6.3 Gap balancing as descent

Each gap is a thermometer plane `t`. On an exchange row `(a, b)`:

```text
admissible  ⇔ g_a ≥ g_b + 2 ⇔ (t_a & 0b10) ≠ 0  and  (t_b & ~(t_a >> 2)) = 0
move        t_a ← t_a >> 1;   t_b ← (t_b << 1) | 1
```

This is the shifted-slot comparison that `GapSettlement` already implements.
Each move lowers `Σ g²` by `2(g_a − g_b) − 2 ≥ 2`, so settlement terminates.
On the path `[1, 2, 3]` nothing is admissible: the state is quiet with
`E_goal` (spread) = 2, and the witness names the missing route. Adding the row
`(1, 3)` admits a move to `[2, 2, 2]`. A move depends on a neighbor's state, so
this is not abelian chip-firing; treat it as C3 unless an order-independence
proof is supplied. The rotation pointer is state, so replay stays exact.

### 6.4 Learning a law from witnessed transitions

Keep two tables over the same tuple index:

```text
A[t]  admitted:     tuple t witnessed at least θ times
U[t]  unsupported:  its before-state witnessed n times, this after-state never
unknown = ~A & ~U
```

Prediction through an unknown row yields an open result. `FiniteLaw` already
represents open scopes natively. The counts and witness epochs remain as
evidence, so every admitted bit can be traced to the transitions that admitted
it. Laws thereby acquire the same non-signal structure as values.

## 7. Mapping to CPUs, GPUs and FPGAs

| Concept | CPU | GPU | FPGA |
| --- | --- | --- | --- |
| Plane (64 sites) | `u64`; `Vector256` / `Vector512` handle 4–8 tiles per instruction | `u32` per warp, or `u64` per thread-tile | Register or BRAM word |
| Small law | Bit-sliced sum of products; `pshufb` for 4-bit domains | Bitwise; tables in constant memory | LUTs (a passage law is one LUT6) |
| Implicit topology | Shifts and rotates of planes | Shuffles, stencil indexing | Wires |
| Explicit rows | CSR gather | CSR gather through shared memory | Static wiring (crystallized) or a small crossbar (dynamic) |
| OR-pools, counts | OR-reduce, `popcnt` | `__ballot_sync`, `__popc` | OR trees, popcount adders |
| Offer / accept | Double buffer | Two phases with a barrier; receivers pull | Two clock phases |
| Tension counts | Small integers or thermometer planes | Integers | Thermometer codes or unary pulse trains |
| Tropical reads | min / add | min / add | Race logic: first arrival computes min |
| Learning counters | Small integers | Integers | Saturating counters, as in branch predictors |
| Inspection | Universal decoder over buffers | Copy back, then decode | Scan chains and readback |

The measured ≈196× fast path (`FiniteLawBatchPlan`) came from finite tables,
compact state and omitted witnesses, on one thread, with no SIMD and no
cross-cell bit packing. Bit-slicing 64 cells per word is the obvious next
multiplier. No number is claimed here; it must be measured.

## 8. A learning ladder toward concepts and language

| Rung | Mechanism | What keeps it inspectable | Status |
| --- | --- | --- | --- |
| L0 | Fixed laws, settlement only | Every move is a law row | implemented |
| L1 | Saturating route counters revised by acknowledged relief | A counter on a named route | implemented (OrnamentTension, `PassPolicyMemory`) |
| L2 | Eligibility traces for delayed and shared credit | A decaying bit per route that carried a unit | exploratory |
| L3 | Learned law tables (admitted / unsupported / unknown); history-indexed tables like n-grams or two-level branch predictors | Counts per tuple, with witness epochs | exploratory (§6.4; signal plan S1–S2) |
| L4 | Structural edits from a declared edit grammar, retained by held-out utility | Edit, evidence and retention criterion are structure | implemented narrowly (timing socket) |
| L5 | Motif crystallization when it pays in actual bits (minimum description length) | Template plus instances plus residual, counted in bits | storage half implemented (`FamilyTable`); discovery missing |
| L6 | Structure mapping between motif networks (analogy, metaphor) | A partial row-to-row and law-to-law correspondence | exploratory; preference transfer is a small same-family step |
| L7 | Language: tokens address motifs; parsing binds ports; ambiguity is support | Every reading is a retained binding with residue | fixtures only (`Gojo.Encoding`) |

Why this can scale metaphorically:

- **A concept is a crystallized motif:** a template of rows, laws and ports
  whose instances are packed variable fields.
- **Crystallize when it pays in bits.** Keep a template if template bits plus
  instance bits plus residual bits are fewer than the raw bits, and held-out
  predictions survive. Because the encoding is physical, description length
  is literally countable. `JoinedFamilies` already reports 5 payload bits for
  four uses of one field against 20 for four independent fields.
- **A metaphor is a structure mapping:** a partial correspondence between two
  motif networks that maps rows to rows and laws to laws, preferring large
  connected mappings (Gentner's systematicity principle). The canonical
  prompt's "structural ghost" is the mapped law network; descriptor
  provenance is rebound.
- **Language:** words reach motifs through hint tokens; a parse is a sequence
  of binding operations over ports that retains alternative bindings as
  support; meaning is the settled scene structure; next-token prediction is a
  forward support read over shadow state; surprise is the prediction residual.
  The "red trace near pin" fixture and the scene-mechanic read frames are the
  existing footholds.
- **Hierarchy:** rung ladders give a predictive-coding shape. Coarse rungs
  predict fine supports through `γ`, and fine evidence returns residuals
  upward through `α`.

The caution: the historical failure of symbolic AI was hand-built structure
that did not scale. The decisive work is L3–L5, acquiring laws and motifs from
data. More authored machines will not settle the question.

## 9. Gaps and risks

Ranked by how much they block the goal:

1. **No shared substrate or tick contract.** Each fixture has its own layout
   and beat (C1, C5).
2. **Acquisition.** Landmarks, frames, seams, candidate families and engines
   are authored. Discovery from drawings, signals or experience is the central
   open problem.
3. **Scalable joint uncertainty.** Exponential masks do not scale; scoped
   propagation with decorrelation records is untested (§5.6).
4. **Tension is not unified**, and there is no energy read or tier ordering
   (§5.9).
5. **Delayed credit.** Route updates need immediate, acknowledged relief.
6. **The quantity ladder between presence and calibrated numbers** is
   described but has no single implemented grammar (§5.8).
7. **Self-describing port bases** (§5.13).
8. **Witness and archive growth** (C6).
9. **Perception front-ends** that turn strokes, images or signals into planes.
10. **Parallel backends** beyond single-thread `ulong` masks.

Risks:

- **Exactness costs.** The fast path came from dropping detail and witnesses.
  Accept the separation of compute lane and evidence lane explicitly.
- **Authored knowledge presented as learned.** The current protocols guard
  against this; the substrate must keep "authored", "selected" and "learned"
  as separate provenance.
- **Unranked alternatives.** Support sets alone cannot rank; counting and
  tropical reads are the principled answer, not a confidence field.
- **Non-confluent laws on parallel hardware** produce nondeterminism unless
  their schedule is declared state.
- **Combinatorial growth of structure.** Tiles, ladders, coarse-first reads
  and crystallization are the mitigations, and each needs measurement.

## 10. Roadmap

Each step is small, testable and changes no kernel law.

| Step | Work | Acceptance |
| --- | --- | --- |
| R1 | Substrate contract fixture: tile grammar, planes, law store with accounting signatures and confluence class. Re-express ODV addition, the passage chain, the conditional and gap balancing. | Bit-for-bit agreement with the existing PrimitiveMeasure expectations; the universal decoder prints a meaning for every stored bit. |
| R2 | Scoped supports U0–U3 with decorrelation witnesses. | The passage chain matches the 32-bit joint mask under U2 and U3; U1 alone reports its loss. |
| R3 | Counting and tropical reads on `FiniteLawBatchPlan`. | Counting equals enumerated completions; tropical equals brute-force minimum on small cases. |
| R4 | Unified tension planes, law residuals, energy tiers; min-conflicts settlement with accounted loans. Apply to gap balancing and paired caps. | Monotone descent traces; local minima reported with their blocking topology. |
| R5 | Tick contract with offer/accept double buffering. | C1 and C2 laws give identical results under shuffled site order; C3 laws replay exactly from their schedule state. |
| R6 | Witnesses by replay. | Rematerialized witnesses equal stored witnesses for current studies; compute-lane runs retain none. |
| R7 | Learned law tables and eligibility traces on signal-plan stages S1 and S2. | Predictions precede outcomes; unknown rows stay open; every admitted row cites its witness epochs. |
| R8 | SIMD backend over tiles, then a GPU compute prototype. | Parity with the native interpreter; measured throughput with raw samples retained. |
| R9 | Description-length crystallization of repeated row motifs in ornament programs. | Retained templates reduce total bits and preserve held-out predictions; rejected candidates are recorded. |

## 11. Practical notes

- **Archives in git.** 615 tracked `.gojo` files are about 65 MB in the working
  tree, and the pack is about 943 MB. Consider keeping generator commands and
  checksums in git and moving large regenerable archives to Git LFS or release
  assets. R6 would make most of them unnecessary.
- **One positive contract page.** After R1, promote §5 to a living contract,
  give each lineage in §1 a status row, and have new study documents link to
  the contract instead of re-deriving their scope.
- **Keep the hedging.** It is the reason the results can be trusted. Pair it
  with the positive contract so a reader can find what is promised.

## 12. References

- K. R. Apt, "The essence of constraint propagation," *Theoretical Computer
  Science* 221 (1999). Chaotic iteration of monotone narrowing operators.
- S. Bistarelli, U. Montanari, F. Rossi, "Semiring-based constraint
  satisfaction and optimization," *Journal of the ACM* 44 (1997).
- P. Cousot, R. Cousot, "Abstract interpretation," *POPL* (1977). Galois
  connections for rung maps.
- L. H. de Figueiredo, J. Stolfi, "Affine arithmetic: concepts and
  applications," *Numerical Algorithms* 37 (2004). Shared noise symbols.
- D. Gentner, "Structure-mapping: a theoretical framework for analogy,"
  *Cognitive Science* 7 (1983).
- A. Madhavan, T. Sherwood, D. Strukov, "Race logic: a hardware acceleration
  for dynamic programming algorithms," *ISCA* (2014).
- S. Minton et al., "Minimizing conflicts: a heuristic repair method,"
  *Artificial Intelligence* 58 (1992); B. Selman, H. Kautz, B. Cohen, "Noise
  strategies for improving local search," *AAAI* (1994).
- J. Rissanen, "Modeling by shortest data description," *Automatica* 14
  (1978).
- R. S. Sutton, A. G. Barto, *Reinforcement Learning: An Introduction*, 2nd
  ed. (2018). Eligibility traces.
- D. A. Jiménez, C. Lin, "Dynamic branch prediction with perceptrons," *HPCA*
  (2001). Inspectable bit-level learners.
- E. Biham, "A fast new DES implementation in software," *FSE* (1997).
  Bit-slicing.

Repository sources: [encoding deep dive](../../experiments/PrimitiveMeasure/GojoEncodingDeepDive.md),
[PrimitiveMeasure README](../../experiments/PrimitiveMeasure/README.md),
[connection experiments](../../experiments/PrimitiveMeasure/ConnectionExperiments.md),
[passages](../../experiments/PrimitiveMeasure/PassageExperiments.md),
[joined families](../../experiments/PrimitiveMeasure/JoinedFamilies.md),
[structural guarantees](../../experiments/PrimitiveMeasure/StructuralGuarantees.md),
[invariance](../../experiments/PrimitiveMeasure/InvarianceExperiments.md),
[machine glossary](../../experiments/QualitativeFrieze/MachineGlossary.md),
[construction exchange](../../experiments/QualitativeFrieze/ConstructionExchangeStudy.md),
[partial decisions](../../experiments/QualitativeFrieze/PartialDecisionStudy.md),
[ornament tension runtime](../../experiments/OrnamentTension/Runtime.md),
[refinement compute comparison](GojoRefinementComputeComparison.md),
[uncertainty reference](GojoUncertaintyReference.md),
[signal pattern learning plan](GojoSignalPatternLearningPlan-2026-09.md),
[boundary/flow codex](../ConceptSurface/BoundaryFlowCodex.md),
[structure through tension](../axioms/Gojo%20Structure%20Through%20Tension.md).
