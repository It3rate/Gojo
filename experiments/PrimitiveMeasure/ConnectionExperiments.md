# Ordered connections, calculated reads, and retained results

Round 5, 30 October 2026. These are small implemented experiments over the
positional value arrays and stable ports. They preserve a connection's roles
even when every attached value reads zero. No geometric direction, radial field,
or rectangle is inferred merely from those bits.

[Round 6 below](#round-6-one-site-two-sites-and-one-site-used-twice) exercises
that distinction with one site, two sites, and one site referenced twice.
The later [Round 7 passage chart](PassageExperiments.md) adds explicit joint
before/after transitions and a two-passage chain over the primitive stores.

ODV still means orientation, direction, and zero/nonzero. V=1 means some
nonzero participation, not a calibrated quantity of one.

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --relations
dotnet test Tests/PrimitiveMeasure.Tests/PrimitiveMeasure.Tests.csproj
```

## What is actually stored

There are three separate things:

| Storage | Contents |
| --- | --- |
| Existing value arrays | Owned O/D/V bits; constants still come from position |
| Connection rows | Ordered local endpoint positions |
| Read-law bitmap | Which combinations of endpoint observations are allowed |

A connection row has no amount, operation opcode, or global ID. `EndpointRows`
groups rows by endpoint count and bits per reference. It stores endpoint 0
first, then endpoint 1, and so on, low bit first within each reference.

```text
first bit of row n = n * endpoint count * bits per reference
```

There are no headers or gaps between rows. Only the final byte can have unused
bits. The experiment supports one to eight endpoints and reference widths from
one to sixteen bits. Endpoint order and repeated references are preserved.

## How a reference finds its endpoint

`ReadConnections` has two local lists:

```text
endpoint 2*n     -> imported public port n
endpoint 2*n+1   -> calculated read n -> table and connection row
```

Thus the low address bit selects the list. A calculated endpoint owns no value
slot. Its port reads the inputs named in its row through the table's actual law.
Imported ports can expose a local value or another structure's public output.
Reading does not inspect the peer's private arrays or hidden fine amounts.

The host maps still use ordinary references and integers. This is a packed
endpoint experiment, not a complete compact wire protocol. Every endpoint
position is meaningful only within its owning `ReadConnections` instance.

Reimporting a stored port from the same owner and public port position keeps
the same endpoint, even through a fresh connection wrapper. Equal readings
from different public ports do not establish that correspondence. Existing stored
ports continue following their private binding when their value moves arrays.

## The law is data, not a name on each row

Each read-table role is an ODV word from `000` to `111`. For three roles
`(a,b,c)`, one bit in `BitRelation` states whether that tuple is allowed:

```text
tuple bit = a + 8*b + 64*c
```

For example, `(001,000,001)` addresses bit 65: byte 8, bit 1. The last role is
the output of a forward prediction. The relation can also be inspected with
`Allows` to see which complete tuples it admits.

One-, two-, and three-role laws occupy 1, 8, and 64 bytes respectively. This
fixed bitmap is an inspectable prototype, not an optimized compression claim.
One table shares its bitmap across all its connection rows. Builder helpers
compile presence, identity, and the existing coarse addition law into bits;
the reader executes those bits without dispatching on the helper's name.

The two-role identity law has bytes `01 02 04 08 10 20 40 80`: only `(w,w)`
is admitted. Different law bits can give the same wiring different behavior.

## A result can contain alternatives

A read returns an eight-bit support mask: bit `w` admits ODV word `w`.
This is a set of possible observations, not another primitive magnitude field.

| Support mask, hex | Meaning |
| --- | --- |
| `00` | No admitted result; not a measured zero |
| `01` | Exactly ODV `000`: orientation 0, positive-marked zero |
| `04` | Exactly ODV `010`: orientation 0, negative-marked zero |
| `05` | Zero magnitude, but its direction marking remains unresolved |
| `0F` | Any orientation-0 word: positive, negative, or either marked zero |

Repeated uses of one endpoint are sampled once per local read and must choose
the same candidate word. Equal masks alone do not impose that constraint.
This preserves correspondence of the published word; it does not infer exact
hidden magnitudes or statistical independence.

## Three concrete connection arrangements

The first two examples below use separate fresh read networks, each importing
its first two ports as endpoints 0 and 2. Their first calculated output is 1.

| Arrangement | Ordered row | Reference width | Packed row bytes |
| --- | --- | --- | --- |
| Two parts into a coarse presence read | `[0,2,1]` | 2 bits | `18` |
| Two operands into calculated addition | `[0,2,1]` | 2 bits | `18` |
| Calculated output into a held sample | `[1,4]` | 3 bits | `21` |
| Active opposition into retained opposition | `[0,1,2,3]` | 2 bits | `E4` |

For `[0,2,1]`, the packed integer is `0 + (2 << 2) + (1 << 4) = 0x18`.
The table's law distinguishes the first two uses; their wiring is identical.
The last row is interpreted by the separate fine settlement fixture below.

## Two parts and their coarse read

The parts have the same fixed orientation and direction. Their V bits are
separately readable; the coarse output says whether either is occupied.

| First V | Second V | Coarse V |
| --- | --- | --- |
| 0 | 0 | 0 |
| 0 | 1 | 1 |
| 1 | 0 | 1 |
| 1 | 1 | 1 |

This is qualitative refinement of occupancy, not calibrated binary magnitude.
Fine evidence determines coarse presence. Coarse presence 1 permits three
finer patterns and cannot select one. Increasing storage width does not supply
that missing evidence. The coarse port is calculated, so it creates no second
independently owned amount. Opposing signs do not fit this presence law.

## Calculated addition and an explicitly held result

The addition bitmap reproduces `ThreeBitMeasure.Add`. Positive nonzero plus
positive zero gives one candidate. Positive nonzero plus negative nonzero gives
support `0F`; the connector does not guess which actual amount dominates.
Different orientations have no admitted addition until explicitly reoriented.

To retain a result, create an ordinary value, publish its port, and import it
as endpoint 4. The identity row `[1,4]` connects calculated output 1 to it.
`TryCapture` is an explicit sampling step. It checks that the last endpoint
really exposes the supplied destination binding, then stores only a single
resolved ODV word that the target's fixed context accepts.

Empty support, several candidates, unresolved zero direction, incompatible
context, or a target with more than one value bit all leave the held value
unchanged. A successful capture stores an observation; it drains no source.
Changing an input changes the calculated read immediately on the next read.
The held sample remains its old value until another successful capture.
There is no automatic feedback, freshness flag, or hidden update schedule.

## A positive/negative pair can settle without losing its two ends

`OpposedConnectionFixture` interprets a four-ended row as:

```text
[active positive, active negative, retained positive, retained negative]
```

Its fine nonnegative integers are demonstration apparatus, not hidden fields
on the encodings. It routes `min(active positive, active negative)` along both
signs into the retained pair, leaving at most one active side occupied.

| Before, `[P,N,Rp,Rn]` | After | Active net read |
| --- | --- | --- |
| `[5,2,0,0]` | `[3,0,2,2]` | Positive nonzero |
| `[2,5,0,0]` | `[0,3,2,2]` | Negative nonzero |
| `[5,5,0,0]` | `[0,0,5,5]` | Zero, with retained opposition still present |

Each sign's total, gross participation, and overall net are preserved. The
matched amount is moved on each side; it is not a new third net amount.
Aliases between these four stores and overflowing destination additions are
rejected before any store changes. Repeating settlement moves nothing further.

`ReadSettled` refuses to guess while both active sides are occupied. At active
zero it keeps the previous direction using the existing direction-memory rule.
Two empty occupancy slots alone cannot remember which sign was last active.
Here the previous ODV word is supplied explicitly by the reader; it is not
hidden state on the four-ended row. The existing direction-memory experiment
supplies the feedback law. A general connection encoding for that feedback
remains separate work.

The same row remains present before and after zero. Two distinct endpoints
remain distinct even when their observations agree. A single retained ODV word
cannot reconstruct that pair, its route, or the retained opposing participation.
These tests preserve the roles needed to express that structural potential; they do not
simulate radial radiation, a perpendicular direction, or full geometry.

## Boundaries of the Round 5 read table

Read tables execute observational bitmap laws; the four-ended fine fixture
executes its explicitly selected material routing law. The latter is not yet
a general encoded material-transition interpreter. Shared row storage does not
make those two uses interchangeable.

Calculated graphs connect existing local endpoints and are acyclic. Correlations
through different calculated paths can remain conservatively unresolved. There
is no global sampling clock, general constraint solver, automatic feedback,
connection collector, or compaction. Published ports are sampled through their
advertised read, and explicit capture is a held observation rather than a
material transfer or a guarantee of simultaneous sampling across structures.

## Round 6: one site, two sites, and one site used twice

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --potential
```

This experiment uses existing value arrays, ports and connection rows. The only
additional law builder is `ExclusivePresence`: exactly one of two input roles
reads present. Its bitmap executes the Boolean XOR of their V bits within one
shared O/D context. It does not subtract or cancel underlying quantities.

### Exactly what is connected

Each arrangement has its own local tables. All value sites use fixed O=0, D=0
and one owned V bit. Endpoint 0 reaches A; endpoint 2, where present, reaches B.
Odd endpoints are calculated reads with no separately stored amount.

A is at packed address `(0,0,1,0)`, through port 0 and binding 0. In the pair,
B is at `(0,0,1,1)`, through port 1 and binding 1. Thus A owns bit 0 and B owns
bit 1 of that value array. The repeated arrangement allocates only A's slot.

| Arrangement | Value slots | Law | Ordered row | Packed row byte |
| --- | --- | --- | --- | --- |
| One site | A | Identity | `[0,1]` | `04` |
| Two sites | A, B | Presence of either | `[0,2,1]` | `18` |
| Two sites | Same A, B | Exclusive presence | `[0,2,3]` | `38` |
| One site, two roles | A | Presence of either | `[0,0,1]` | `10` |
| One site, two roles | Same A | Exclusive presence | `[0,0,3]` | `30` |

Each reference is two bits, packed low first. For `[0,2,3]`, for example:
`0 + (2 << 2) + (3 << 4) = 0x38`. Each listed law has one row in its own table;
the final byte's unused bits are zero. Law storage is additional to row storage.

The two-site arrangement stores two V bits. The repeated-site arrangement
stores one V bit and mentions the same endpoint twice. A calculation adds a
connection row and an output endpoint, not a third material value. The actual
rows carry this distinction; no pair/repeated category flag is stored.

### What happens when the observations change

The caller explicitly supplies observations. No transfer or settlement is
implied by changing a stored observation in this demonstration.

| Stage | Single V | Two-site input reads | Repeated-site input reads | Two-site XOR | Repeated-site XOR |
| --- | --- | --- | --- | --- | --- |
| Initially empty | 0 | `[0,0]` | `[0,0]` | 0 | 0 |
| A becomes present | 1 | `[1,0]` | `[1,1]` | 1 | 0 |
| B also becomes present in the pair | 1 | `[1,1]` | `[1,1]` | 0 | 0 |
| Everything returns to empty | 0 | `[0,0]` | `[0,0]` | 0 | 0 |

All three coarse presence reads follow `0,1,1,0`. That read alone cannot
distinguish the structures. Inspecting the rows shows the difference; observing
the response when A changes reveals a behavioral consequence. When the pair
later reads `[1,1]`, its sites remain distinct. Returning to zero preserves
their connection and their ability to differ on a later observation.

With unresolved input reads, each admitting V=0 or V=1, the same distinction
matters before any new observation arrives:

```text
exclusive presence of distinct A and B -> {0,1}
exclusive presence of A used twice     -> {0}
```

The first result leaves agreement unresolved; distinct references do not prove
statistical independence. The second requires both roles to choose the same
candidate word. It establishes presence agreement only, not equality or
cancellation of hidden magnitudes. Correlation through separate calculated
paths remains limited as described above.

### Internal direct addresses are a valid alternative

The current path is `endpoint -> port -> binding -> packed address`. The
address contains orientation group, direction group, total width, and index;
the bit offset inside the selected array is `index * width`.

The demonstration moves A from `(0,0,1,0)` to `(0,0,3,0)`. Only its binding's
address changes. The old slot becomes non-live, the connection rows remain
unchanged, and their coarse reads remain valid. The added bits supply capacity,
not new measurement.

An internal row could instead contain that address directly. This would remove
lookup steps, while movement would require rewriting all affected local
references. Small structures can reasonably scan their own rows. This is an
exploratory alternative, not an implemented second address format or a measured
performance improvement. Stable external ports can be retained either way.

The main requirements for such a move would be:

- Update every local reference holder, including the external-port map, before
  reads resume. Apply a relocation map to original addresses once so swaps do
  not accidentally become cascading substitutions.
- Preserve repeated references together and keep distinct sites distinct.
- Prevent a stale reference from silently reaching a new occupant after reuse.
- Distinguish a stored-value address from a calculated-read address. A calculated
  output has no value slot unless a separate sample is explicitly captured.

Indirection stores a potentially longer address once; direct references may
repeat it, although shared table context can omit some address bits. Neither
choice requires global identifiers. Cycles can be encoded using either choice;
their evaluation still needs a separate timing or settlement law. The current
calculated reader remains acyclic. This experiment retains its existing mapping
so the structural comparison is independent of a storage redesign.

### What refinement preserves

The two-part presence relation is already a small structural ladder:

```text
parts [0,0]             -> coarse 0
parts [0,1], [1,0], [1,1] -> coarse 1
```

Learning which connected parts are present refines a previously valid coarse
1. It does not supply a calibrated magnitude. More generally, for the same
state and the same read contract, finer evidence should project into the
possibilities already admitted by the coarse read. A changed physical state or
changed meaning of zero is a different observation, not that refinement.

The tests exhaust the exclusive-presence law and exercise the structural
distinction across all four fixed O/D contexts, including uncertain inputs and
migration. No geometric line, radial dynamics, automatic feedback, or hidden
numeric resolution is inferred from these rows.
