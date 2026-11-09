# Gojo.EncodingB

Status: **exploratory**, November 2026. A runnable form of the plane–law
substrate proposed in the
[encoding consolidation note](../Gojo/docs/GojoEncodingConsolidation-2026-11.md)
(§5, §6), including the workspace contract in
[Gojo/Workspace](../Gojo/Workspace/README.md). Hardware backends (SIMD, GPU,
FPGA) are out of scope; planes are plain `ulong` words.

The tests live in this project under `Tests/`:

```powershell
dotnet test Gojo.EncodingB/Gojo.EncodingB.csproj
```

## Boundary

Like `Gojo.Encoding`, this project does not own Gojo meaning. It has no
dependency on the kernel. Site grammars and laws here are host descriptors that
make every stored bit decodable. Consolidation §5.2 proposes that a packed layout
becomes a crystallized Gojo basis only when its grammar and laws are themselves
connected Gojo structure. That bridge is **not built**, so a tile here is an
adapter in the canonical prompt's sense, not a second ontology.

## The rule

A **tile** holds what *is*: structure, value at its resolution, and tension.
A **workspace** holds what is *noticed, supposed or attempted*: selection,
supplied evidence, support, predictions and candidates. Every computation other
than the owner's own settlement runs in a workspace, and nothing reaches the
source except through a validated commit.

## Nouns and types

| Noun | Types | What it is |
| --- | --- | --- |
| Plane | `ulong`, `Bits` | Bit i is site i (or lane i, or row i) |
| Tile | `Tile`, `SiteGrammar`, `PlaneDescriptor`, `CodeField`, `ThermometerField` | Up to 64 sites, bit-sliced planes, a declared grammar, epoch and version |
| Row | `RowSet` | Ordered site references (CSR) with per-row provenance |
| Law | `RelationLaw`, `TransitionLaw`, `AccountingSignature`, `ConfluenceClass`, `LawRows` | Tables over role tuples; topology kept separate from rule |
| Rung | `Thermometer`, `RungMap`, `Rungs`, `CoarseRead` | Codes R0–R1 and Galois maps between them |
| Workspace | `Selection`, `SelectionMachines`, `Workspace`, `TrialWorkspace`, `PredictionWorkspace` | Selections, provenance, local work, validated writeback |
| Tick | `Runtime`, `TensionField`, `RouteTable`, `GapBalance`, `Energy` | Phased settlement, accounting, adaptation and replay |
| Support | `SupportPlanes`, `Propagation`, `JointSupport`, `UncertaintyReads` | U0–U3, decorrelation records, semiring reads |
| Learning | `LearnedTransitionTable`, `RouteTable` | L1 route strengths, L3 learned law tables |
| Inspection | `BitDecoder` | Prints the meaning of any tile or law bit from its address |

Ports between tiles (consolidation §5.13) are not implemented yet.

## Where the bits mean something

**ODV word** (`OdvWord`, `Odv`). O D V, high to low. V = 0 reads "no magnitude
detectable at this resolution", and V = 1 is not the quantity one. Reversing
flips D (`001 → 011`). Reorienting flips O and D together (`001 → 111`). In
`Odv.Grammar`, V and D are stored planes and O is supplied by the tile basis,
so a whole tile reorients with `O ^= 1; D ^= ALL`. All eight words pack as
`88 C6 FA`.

`Odv.Addition(threshold)` derives the addition relation by enumerating a
threshold reader rather than listing rows:

- Threshold 1 reproduces the historical exact-zero table cell for cell.
- Threshold 2 or more gives the detection contract. It widens six ordered pairs:
  `+0 + +0 → {+0, +?}`, `−0 + −0 → {−0, −?}`, and the four mixed pairs
  `(+0, −?)`, `(−?, +0)`, `(−0, +?)`, `(+?, −0)`.
- One site used twice admits only diagonal tuples, so `X + X` with X either `+?`
  or `−?` excludes zero.

**Passage law** (`Passage`). State `S + 2C + 4T`. Bit `before + 8 × after` admits
a whole transition. The table is `01 02 04 00 10 20 48 80`, which as one
integer is the LUT6 value `0x8048201000040201`. The signature is checked over all
64 pairs at load: it is functional, preserves `S + T`, never changes C, never
increases S and never decreases T. A table that breaks it is refused. Run over
64 lanes, the law's sum of products reduces to `S' = S & (~C | T)` and
`T' = T | (S & C)`.

**Thermometer code** (`Thermometer`). Bit k means "greater than k".
`ExceedsByTwo(a, b)` is `(a & 0b10) != 0 && (b & ~(a >> 2)) == 0`.
`AddOneAt` and `RemoveOneAt` shift a unit into or out of every selected site at
once on bit-sliced planes.

**Workspace planes.**
- `Sel` is the focused source sites plus introduced sites.
- `Sup` marks sites with supplied evidence.
- `S[v]` lives in `SupportPlanes`.
- `K0` and `K1` hold provenance: `00` referenced live, `01` captured,
  `10` introduced, `11` diverged (copy on write).
- The commit rule is `(accountingScope & ~Sel) == 0`.

**Tension.** `TensionField` keeps three thermometers per site: M (absorbed), P
(positive tension) and N (negative tension, unmet draw). P and N are never
netted directly.

## Non-signal parts

| Kind | Where it lives here |
| --- | --- |
| Not selected | `Selection` bit 0. `Workspace.ReadBit` returns null, not false |
| Closed vs open | `Selection.Closed`. Only closed selections read `ExcludedWithinScope` |
| Selected, not supplied | `Workspace.Sup` = 0; support stays open (all values) |
| Competing candidates | Support popcount > 1 |
| Contradiction | Empty support plus a `Contradiction` naming the law and rows |
| Lost correlation | `DecorrelationRecord` from `JointSupport.ToMarginals` |
| Resolution loss | `RungMap` γ bounds; `CoarseRead` without residue gives a range, not a value |
| Blocked / remainder | `TransferRecord` |
| Overflow | `TensionField.Overflow`, kept as residue |
| Stale | `ReadVersion` / `ReadEpoch` mismatch: the commit is refused and the proposal kept |
| Introduced | Provenance `10`; never observable as source evidence; adopted only with a labelled edit |
| Prediction | `PredictionWorkspace`, which has no commit method |
| Unknown law rows | `LearnedRead.Unknown`; prediction through them is open |

## Tick contract

`Runtime.Tick` runs these phases in order:

1. ingest (change enters as tension)
2. observe (non-material planes only)
3. absorb (C2, bit-sliced)
4. offer, accept and return along routes, double-buffered
5. declared transition laws, in waves
6. gap descent (C3, with the rotation pointer as state)
7. absorb
8. adapt (route strengths, on the medium clock)
9. account (tiered `Energy`)

Every tick checks `pending = offered + remainder` and
`offered = accepted + blocked`. A violation is hard energy. Inputs are logged,
so `Replay(checkpoint, inputs)` rematerializes every tick instead of storing a
witness for each read.

Rows that share sites run in waves. A row runs in the wave after every earlier
row it conflicts with, so row order is the declared schedule.

## Choices this code makes beyond the note

- **Epoch and version.** A tile has both. Structural and basis edits advance the
  epoch; every state change advances the version. Material commits validate the
  version. Structural adoption validates the epoch.
- **Absorption order.** Draw comes first, then fill. Opposed packets arriving in
  the same tick meet through M on a later absorb rather than being netted.
- **Material commit.** A trial workspace proposes a transition and the source
  applies its own law. This is how this layer reads Axiom 5: value never arrives
  as a direct write from a workspace.
- **Closed selections.** "Closed" means a machine decided membership for every
  element of the scope (`All`, `EverySecond`, `Within`, `Where`, and so on).
  `These` is open.
- **Ambiguous laws.** A non-functional transition law leaves ambiguous lanes
  unchanged and reports them. It never picks one after-state.

## Status against the consolidation roadmap

| Step | Here |
| --- | --- |
| R1 substrate contract | Implemented: grammar, planes, decoder, laws with signatures and confluence classes, ODV, passage chain and gap balancing |
| R2 house workspaces | Implemented as tests: opposed seam reads, introduced closure, validated adoption, four-of-six markers, commit rule |
| R3 scoped supports | U0, U1, U2 and U3 on the passage chain, with decorrelation records |
| R4 semiring reads and zero contracts | Counting, tropical and bottleneck reads; exact and detection ODV tables |
| R5 tension and energy | Tension planes, tiers, gap descent and missing-route witness. Accounted loans are not done |
| R6 tick contract | Phases, waves and accounting identities. Shuffled-order tests are not done |
| R7 witnesses by replay | Checkpoint plus input log replay |
| R8 learned law tables | Admitted / unsupported / unknown; crystallization refuses tables that break the signature. Eligibility traces are not done |
| R9 SIMD / GPU | Out of scope |
| R10 crystallization by description length | Not done |

The test suite also checks everything the earlier sections claim: the exact
table against the PrimitiveMeasure README, the six widened detection rows, the
passage law bytes and LUT value, and that learning from witnessed transitions
crystallizes a table with the same content hash as the authored passage law.
