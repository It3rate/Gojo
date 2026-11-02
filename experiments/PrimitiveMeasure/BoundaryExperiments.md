# Closure, claims and circulating flow

This playground asks three separate questions: does the structure close, can
its claims agree, and can an activation move through it? No lengths or angles
are needed to answer the first question. The same cycle can contradict a
strict scalar ordering while supporting a useful repeating passage.

Open the [visual playground](https://gojo.inklingai.ca/encoding/explorer/?layer=16).
Layers 16–18 extend the existing [Primitive Explorer](../../Visualizations/Gojo.PrimitiveExplorer/README.md).
Hover a connection or its bits to follow the correspondence. All results come
from the C# experiments, including in the browser.

Status: executable exploratory fixture. The Gojo kernel and the original ODV
word are unchanged. This borrows signed incidence from the Hodge discussion;
it does not implement Hodge decomposition, a metric, automatic geometry,
orthogonal escape, or structure growth.

## 1. The smallest structural promise

```text
edge 0: A -> B
edge 1: B -> C
edge 2: C -> A
walk:   edge 0 -> edge 1 -> edge 2
```

A departure contributes minus its site; an arrival contributes plus its site.
Every occurrence at a shared site can be paired with its opposite occurrence.
This is exact correspondence, not cancellation of independently unknown
nonzero magnitudes. A `+1` in this chart means one arrival occurrence, **not**
one unit of charge, one tick of length, or the V bit interpreted as quantity one.

| Use | A | B | C |
| --- | ---: | ---: | ---: |
| A -> B | -1 | +1 | 0 |
| B -> C | 0 | -1 | +1 |
| C -> A | +1 | 0 | -1 |
| Signed boundary | 0 | 0 | 0 |

Removing the last use leaves `-A + C`. Reversing the middle use instead leaves
`+2B - 2C`. Parity cannot see the latter: each site still occurs an even number
of times. XORing binary addresses is weaker still; `0 XOR 1 XOR 2 XOR 3 = 0`
although no reference in that set has a matching occurrence.

## 2. Concrete encoding

All arrays belong to one local structure. Positions supply references, with no
global IDs, coordinates, per-edge operation tags, lengths or amounts. Table
context supplies counts and reference widths; the byte counts below exclude
host containers and table context. Fields and rows are packed low bit first.

| Array | Fields | Triangle bytes | Meaning |
| --- | --- | --- | --- |
| Ordered edges | `[departure, arrival]`, each 2 bits | `94 02` | `[0,1] [1,2] [2,0]` |
| Ordered uses | one 3-bit signed edge reference | `10 01` | `0,2,4`, naming edges 0,1,2 forward |
| Optional patch boundary | one 2-bit use reference | `24` | use positions 0,1,2 |

A signed use is `(edge position << 1) | reverse`. Its low bit reverses the
selected edge **for this use**. This is not the ODV direction bit. Repetition
can name an edge several times without copying its endpoints or erasing the
occurrences. The triangle takes four payload bytes; a supplied patch adds one.
Final-byte padding is zero and is never a participant.

The optional patch boundary is a separate array referencing the existing
uses. Its boundary can be inspected independently. Omitting a use from that
array makes the patch's supplied boundary fail even while the original walk
still closes. This is an abstract attached patch, not proof of a filled
Euclidean region, geometric area, simple outline or a Hodge star.

## 3. Cases to try

| Case | Signed balance | One ordered closed walk | Important distinction |
| --- | --- | --- | --- |
| Open chain | No | No | Unmatched departure and arrival |
| Closed triangle | Yes | Yes | No lengths or right angles established |
| Middle use reversed | No | No | Even parity hides signed mismatch |
| Two separate loops | Yes | No | Two connected components remain distinct |
| Coincident A and D | No | No | The renderer overlays endpoints; references differ |
| Cycle with supplied patch | Yes | Yes | Patch exists through its own boundary references |
| Self-loop | Yes | Yes | Zero boundary does not delete the edge |
| Out and back | Yes | Yes | Ordered closure does not imply a simple outline or area |

“Inspect first use only” deliberately leaves the read incomplete. Its displayed
site counts describe the inspected prefix. Later uses could cancel them, so
the whole-structure verdict stays unresolved. Even a balanced prefix cannot
certify unread structure. No coordinates are consulted by this reader.

## 4. Same cycle, different attached laws

In the strict-order interpretation, edges express `A > B`, `B > C`, `C > A`
in one compatible scalar basis. The selected law checks for directed cycles
among participating claims. All three present is a contradiction; the closing
claim absent leaves an acyclic order; uncertain or missing closing evidence
leaves the verdict unresolved. This is not a numeric ranking or a Hodge
decomposition, and it neither identifies a culprit nor chooses a repair.

In the passage interpretation the existing `PulseCircuit` moves a single
activation through state sites A=0, B=1, C=2. Guard=3 is supplied through an
exposed read port. A and B continue unconditionally; C returns to A only when
that observation is present.

| Table | Rows / bytes | Law bytes |
| --- | --- | --- |
| Unconditional | `[0,3,1] [1,3,2]` / `5C 0B` | `01 00 04 00 12 20 48 80` |
| Present controlled | `[2,3,0]` / `0E` | `01 02 04 00 10 20 48 80` |

An eight-byte transition law is a bitmap of allowed complete before/after
source, control and target patterns. It moves the source and receiver jointly.

| Observation at closing guard | Actual V bytes from initial state | Meaning |
| --- | --- | --- |
| Present | `01 -> 02 -> 04 -> 09 -> 0A -> 0C -> 09 ...` | Activation repeats; imported guard is also present |
| Absent | `01 -> 02 -> 04 -> 04 ...` | Activation waits at C |
| Uncertain | `01 -> 02 -> 04 -> 04 ...` | Predict C or A, commit neither at C |
| Missing | `01 -> 02 -> 04 -> 04 ...` | No observation permits a closing decision |

Earlier unconditional steps do not query the irrelevant guard. Observing the
guard does not drain its source. Unknown control is not another stored amount,
and the prediction is not committed as actual flow. Topological closure remains
true even when the closing passage cannot currently move.

This combines the earlier work on shared observations, conditionals, local
comparisons and repetition without adding an unmeasured numeric flow field.

## 5. Measuring computational cost

Layer 18 constructs rings of 16, 256, 4,096 or 16,384 sites, then reads either
the whole ring, the first quarter, or one use. These are sample sizes, not a
proposed cognitive capacity limit. The current packed fixture's 16-bit local
references are an implementation format limit; a future wider encoding is a
separate question from affordable computation.

The chart separates:

- packed endpoint/use payload bytes;
- construction elapsed time, including temporary input arrays;
- median reader elapsed time across nine samples after one warm-up;
- median bytes allocated on the current thread during a read;
- inspected uses and whether closure was actually established.

Allocations are cumulative allocations during the operation, **not peak or
retained heap size**. Timing excludes rendering and is not a throughput promise.
JIT compilation, tiering, GC, browser runtime, machine load and debugger state
can change results. Construction timing is a single sample and may include JIT.

Run the same apparatus without rendering:

```powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --boundary-scaling
```

An initial desktop Release run on 2 November 2026 gave these illustrative
results; use the live Measure button for the current environment:

| Sites / edges | Packed bytes | Whole read median | Read allocated bytes |
| ---: | ---: | ---: | ---: |
| 16 | 26 | 0.0114 ms | 4,200 |
| 256 | 800 | 0.2483 ms | 66,384 |
| 4,096 | 18,944 | 4.0049 ms | 1,247,208 |
| 16,384 | 88,064 | 25.6794 ms | 3,134,088 |

This is deliberately a straightforward reader: reference decoding allocates
small arrays; dictionary-based balance and component reads allocate scratch
state. `EndpointRows` currently grows exact byte arrays as rows are appended,
so construction also copies earlier payload repeatedly. These are measured
implementation costs, not intrinsic lower bounds of the encoding. A later
optimization can preserve identical bytes while testing allocation-free
decoding, reserved construction capacity and reusable local scratch memory.

Boundary inspection visits each selected use once, plus component resolution
over the touched sites. It does not enumerate all possible magnitudes or
uncertain states. Other computations, such as many interacting uncertain
branches, can have very different costs even with fewer sites.

The allowance is a **read budget**, not a claim that size makes the world
uncertain. An unfinished reader reports what it has not established. The
fixture currently restarts each read; resumable inspection, adaptive budgets,
splitting owners, thin-channel summaries and their coordination costs remain
next experiments. No automatic splitting or intelligence claim is made here.

## Verification

`OrderedBoundaryTests` checks exact bytes, signed reversal, the XOR-address
counterexample, disconnected loops, retained self-loops/repetition, explicit
patch boundaries, unresolved budgets and strict-order contradictions.
`ExplorerTests` exercises every choice and selected beat counts, validates all
bit spans/padding, and verifies that an uncertain closing guard leaves actual
activation unchanged. Visual capture includes reversed and coincident cases,
a supplied patch, uncertainty, contradiction and a measured partial read.

The connection between Hodge theory and this experiment remains exploratory.
No global optimizer, field law, metric, or automatic classification of residue
as “error versus feature” is required for these checks.
