# What survives a change?

## What this experiment is trying to show

Two patterns can look the same today and respond differently tomorrow. A shared
junction cannot separate into two endpoints through a value change. Two separately
stored endpoints can currently agree, but an explicit retargeting can separate
them. Neither representation is less truthful about its present connections.

This experiment compares the **current read**, the **changes supported by the
encoding**, and the **structural revision needed to change a guarantee**. It does
not guess a missing construction history. It does not turn editability into a
confidence score.

Open [What survives a change?](https://gojo.inklingai.ca/encoding/explorer/?layer=43).
Start with the three loop layouts, apply the same endpoint change, then inspect
the result. Compare shared reaches with independent reaches. Finally stop a
request at the gate of a closed route and reopen it.

The pictures are connection diagrams. A cycle drawn as a square does not assert
straightness, right angles, equal lengths or enclosed area. Changes in highlights
show actual endpoint bindings, ordinal reads or activation state; screen distances
are not measurements.

## The connected structures

```text
pair rows:       [A,B] [B,C] [C,D] [D,A]
shared vertices: [A, B, C, D, A]
cyclic vertices: [A, B, C, D] with the final successor at the first field

one reach source -> four occurrence bindings
four reach sources -> one occurrence binding each

one source -> forward use
           -> reverse use

closed route -> explicitly controlled passage -> next site
                     ^
                 public gate read
```

In vertex storage, adjoining steps read the same actual vertex field. Pair rows
must agree on their independently stored references. The cyclic layout also
shares the final arrival with its first vertex field. It has no final-arrival
field that an ordinary endpoint edit could change independently.

The layout discriminator selects a storage grammar, not a square, circle or
construction-method label. All endpoint references are local. The O/D bucket
grammar independently determines which word bits belong to an entry. The original
ODV meaning and its three-bit coarse read are unchanged.

## A concrete encoding chart

Here sites A..D are local references 0..3, with site 4 reserved for the split
experiment. Each reference therefore occupies three bits. Fields are packed
low bit first; padding is not a participant.

| Layout | Stored references | Topology bits | Actual topology bytes | Participation bytes |
| --- | --- | ---: | --- | --- |
| Pair rows | `[0,1] [1,2] [2,3] [3,0]` | 24 | `48 A4 0D` | `0F` |
| Shared vertex chain | `[0,1,2,3,0]` | 15 | `88 06` | `0F` |
| Positional cycle | `[0,1,2,3]`, final successor wraps | 12 | `88 06` | `0F` |

The last two happen to have the same padded topology bytes. Their **complete
encodings differ**: the enclosing layout and row boundary give those bits
different meanings. This is not inference from naked payload bits. Four
participation bits are present in every case; none stores a length of one.

The transport carries version, layout, O/D bucket positions, site count, row
count, packed topology and packed observations. Counts delimit host storage and
padding; they are not extra geometric or arithmetic quantities. Loading validates
all references and framing before a compact read may rely on the grammar.

## Three different limits on a read

1. **Not yet checked:** the endpoint question exists, but the local inspection has
   not inspected enough of its source. The current support remains unresolved.
2. **Unresolved observation:** the gate or response reader admits multiple values,
   or a required observation has not been supplied. Execution follows its existing
   conservative passage policy and retains the request.
3. **Not represented:** there is no geometric basis for asking whether this loop
   is a square. Inspecting every endpoint does not supply that missing meaning.

An absent geometric claim is not a failed geometric claim. An uninspected chain
is not a broken chain. A missing control observation is not a known zero. The
same display colour must not be the only explanation of these differences.

Positive-context support reads use `01` for a known negative answer, `10` for a
known positive answer, and `11` while either remains possible. Gate support `00`
means no supplied observation in this fixture. These are read masks, not extra
material participation or probabilities.

## What the permitted edits mean

| Edit | What it changes | What it does not establish |
| --- | --- | --- |
| Supply an ODV observation | Owned word bits in the selected bucket | It does not retarget an endpoint or introduce a hidden routing law. |
| Retarget one pair endpoint | One explicit local reference | It does not move any other reference merely because their numbers matched before. |
| Retarget a shared vertex field | Its adjoining step(s): two at an interior or cyclic field, one at a chain end | It does not split a shared interior junction into independent endpoints. |
| Expand to pair rows | Representation while preserving every current ordered endpoint | It does not itself destroy a proven connection or reveal a generator. |
| Inspect another unit | The locally published continuity/closure answer | It does not repair the route. |
| Restore encoded bytes | A fresh owner with the same retained layout, words and aliases | It does not restore an old inspection cursor or pretend an old proof was rechecked. |
| Offer one reach request | A real existing passage moves a request into an ordinal dial | It does not assign a calibrated distance or change the original coarse participation. |
| Change a gate read | Evidence used by one controlled passage | It does not cut the stored loop. |

Topology edits invalidate the affected inspection. Rejected edits leave both
payload and existing evidence unchanged. Word observations are an explicit input
apparatus, not physical charge transfer. The separate reach and gate trials use
the existing material-request passage laws.

## Shared variation and symmetry

Four independent participating reaches can have identical coarse words. This
only establishes the same coarse read. Four occurrences referring to one reach
share that source, so a refinement of it is visible at every occurrence.

The second comparison gives one source two opposed uses, or supplies two detached
sources whose initial observations match. Reversing a use swaps its ordered
endpoints and perspective; it does not flip the source ODV direction bit. The
same source can then support opposed reads that vary together. Detached sources
have no such guarantee merely because their drawings look symmetrical.

This is an **opposed-read correspondence**, a narrow precursor to a geometric
mirror. It supplies no Euclidean reflection, mirror plane or automatic symmetry
discovery. Independent local ordinal positions also do not create a common metric.

The request has one material location: reserve, waiting site or a dial. Multiple
reads of a shared dial do not create multiple requests. Concealing the response
control leaves the request waiting; revealing it can continue that same request.

## Closure and circulation

The gate trial has an actual closed endpoint graph and a separate activation
circuit. All ordinary passages are unconditional; one passage has an explicit
control law. Known closed control holds the activation. Unknown or missing
required control also holds it, for a different reason. Reopening the gate
allows the same activation to continue. Every case retains one activation and
the original closed graph.

An eight-byte transition law contains the allowed before/after combinations of
source, control and target presence. These bits determine the passage response;
the diagram does not choose it.

An absent V observation does not delete a retained link. If an active-path read
should omit that link, its participation rule must be connected separately.
Likewise, a closed walk can retrace itself or cross itself; closure alone supplies
no geometric enclosure.

## Validation and its boundary

The survey runs with:

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --structural-guarantees experiments/PrimitiveMeasure/data/structural-guarantees
```

It checks 1,110 endpoint configurations: all pair, chain and cyclic layouts with
one to three sites and one to three occurrences. This exhausts that declared
endpoint domain, not all ODV assignments, geometries or future operations. Each
case checks partial-read soundness, completed truth, transport, owned-bit changes
and exact expansion. There are also 32 shared-variation attempts and 960 gate
pulses, with exactly one material request retained in each trial.

For a 16,384-step cycle with fixed O/D observations:

| Layout | Padded topology + word bytes | Complete transport bytes | Inspection units | Endpoint rows read by inspection |
| --- | ---: | ---: | ---: | ---: |
| Pairs | 59,392 | 59,402 | 16,384 | 16,384 |
| Shared vertex chain | 30,722 | 30,732 | 1 | 2 |
| Positional cycle | 30,720 | 30,730 | 1 | 0 |

Construction and loading still validate all supplied rows. Their cost is not
included in the subsequent inspection columns. The cycle read checks its already
validated grammar. These are logical operation and byte counts, not elapsed
timings or a claim that arbitrary graph verification is free. This initial edit
implementation reconstructs its packed topology table; its edit witnesses count
all decoded and written rows rather than pretending only one byte was touched.

Larger route sizes test storage and inspection work without claiming exhaustive
coverage of all possible graphs. Tests additionally cover shared-site edits,
malformed transport, independent versus shared variation, unresolved controls,
retained activation and a consumer connected only to the public closure read.

The raw records are [finite routes](data/structural-guarantees/finite-routes.csv),
[inspection costs](data/structural-guarantees/inspection-costs.csv),
[shared variation](data/structural-guarantees/shared-variation.csv),
[gate closure](data/structural-guarantees/gate-closure.csv) and
[the result summary](data/structural-guarantees/summary.txt).

The observer records outcomes after execution. It does not choose a route, create
a structural repair or supply a learned default. No exhaustive enumeration of
unknown geometry or all possible construction histories is attempted.

The guarantees remain conditional on this small grammar and the named edits.
Future operations may add new ways to revise a structure. Learning which
construction is likely from these signatures, automatic packing and splitting,
mechanical compliance, and general geometric settlement remain future work.
