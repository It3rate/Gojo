# Primitive connections: exact encoding chart

Round 3, 30 October 2026. These layouts are **implemented experiments**, not a
general port format or a replacement for Gojo's kernel. The chosen layouts and
their local transition laws are exploratory. They make every attachment in these
small examples explicit without identifiers, receipts, or a connection registry.

**The next storage experiment is [positional arrays and stable ports](StablePortExperiment.md).**
It removes the shared O/D header bits by deriving constants from array position.
The physical header-bit attachments below belong to these earlier examples;
they are not the current target for positional storage. Both experiments remain
executable so their different mechanisms can be inspected directly.

An ODV word still means orientation, direction, and zero/nonzero. Conventional
word displays put O on the left: `011` is positive-right, negative, nonzero.
Storage charts below run from **bit 0 upward**. Hexadecimal bytes use ordinary
notation; consecutive bytes are listed in storage order. Padding is not a site.

## 1. Where the constants live

`ContextTable` stores the shared high bits once. Its enclosing layout supplies
the entry width and count; neither is repeated inside an entry.

| Instance width | Low-to-high physical layout | Meaningful bits for n entries |
| --- | --- | --- |
| 1 | `O D V0 V1 ...` | `2 + n` |
| 2 | `O V0 D0 V1 D1 ...` | `1 + 2n` |
| 3 | `V0 D0 O0 V1 D1 O1 ...` | `3n` |

For a width-one table, all entries' O reads address physical bit 0 and all D
reads address bit 1. Their V reads address different sites: 2, 3, and so on.
Sharing the sign is therefore sharing an actual stored bit. Sharing the same
sign **value** in two different tables does not share that bit.

Concrete two-entry examples:

| Effective words | Fixed OD table: 4 bits | Release D into instances: 5 bits |
| --- | --- | --- |
| `001, 000` | low-to-high `0 0 1 0`; byte `04` | `0 1 0 0 0`; byte `02` |
| `011, 010` | low-to-high `0 1 1 0`; byte `06` | `0 1 1 0 1`; byte `16` |

Every effective word survives release, including the direction-marked zero.
The upper four or three bits of each host byte are padding, respectively.
An instance update cannot change an inherited O or D: `TryWrite` refuses the
change and preserves the buffer. `Release` makes a replacement representation;
it does not move material, consume the old buffer, or narrow a signed instance
merely because its present value is positive.

Same-sign fixed inputs can keep that fixed context for a joint read. Opposing
fixed inputs cannot both fit one fixed-D table: construction refuses it.
Selecting the O-only table explicitly gives their joint read its own D bit.
That freedom remains even if the current observed net happens to be positive
or zero. The retained participants below still occupy their original fixed-sign
tables; selecting a net read does not consume them or determine its sign.

Repacking changes physical positions. An attachment to the old bit 2 does not
automatically follow an entry whose V moves to bit 1. The caller must explicitly
replace the representation and rebind its attachments. Releasing O likewise
does not establish a correspondence between previously different orientations.

## 2. The retained-participation tile

For the flow experiment, two width-one, two-entry tables occupy one byte. The
positive table uses the low nibble and the negative table the high nibble.
The fixed tile layout groups their **columns**: A contains A+ and A−, while B
contains B+ and B−. This grouping is part of the declared layout, not a rule
that arbitrary adjacent bits or equal values are automatically connected.

| Bit position | Stored role | Shared by / attached participant |
| --- | --- | --- |
| 0 | Positive table O = 0 | A+ and B+ orientation |
| 1 | Positive table D = 0 | A+ and B+ fixed positive sign |
| 2 | A+ V | A's retained positive participation |
| 3 | B+ V | B's retained positive participation |
| 4 | Negative table O = 0 | A− and B− orientation |
| 5 | Negative table D = 1 | A− and B− fixed negative sign |
| 6 | A− V | A's retained negative participation |
| 7 | B− V | B's retained negative participation |

```text
                    column A                  column B
positive O,D     ->  V at bit 2                V at bit 3
negative O,D     ->  V at bit 6                V at bit 7
                    A+ together with A−       B+ together with B−
                    one net read              one net read
```

There is no net-result field in this byte. A net read is derived from the
participants. Two occupied sides predict negative, zero, or positive; equal
presence bits do not establish equal amounts or exact balance.

The separate `RetainedFlowFixture` demonstration world holds four nonnegative
amounts in order `[A+, B+, A−, B−]`. Their exposed sites are `[2, 3, 6, 7]`.
`Observe` derives the byte from that world; there is no independently maintained
copy of V to synchronize. Fine amounts are demonstration apparatus, not hidden
fields in the primitive word. Writing an arbitrary observation bit does not
create or destroy those amounts.

| Demonstration world | Observed byte | Actual net A | Actual net B |
| --- | --- | --- | --- |
| `[0, 0, 0, 0]` | `20` | 0 | 0 |
| `[5, 0, 5, 0]` | `64` | 0 | 0 |
| `[5, 0, 2, 0]` | `64` | +3 | 0 |

The first two rows expose different retained participation despite equal net
reads. The last two expose the same occupancy byte despite different net reads.
Observation does not consume either participant.

## 3. A connection names two existing sites

`LocalConnection.Link` stores **source position followed by target position**:

```text
physical bits:     0 1 2 | 3 4 5
                  source | target
printed binary:   target source
```

The positions are three-bit offsets within this eight-bit tile. They are not
object IDs, calibrated magnitudes, or durable addresses outside this layout.

| Material connection | Source | Target | Six-bit encoding | Host byte |
| --- | --- | --- | --- | --- |
| A+ → B+ | 2 (`010`) | 3 (`011`) | `011010` | `1A` |
| A− → B− | 6 (`110`) | 7 (`111`) | `111110` | `3E` |
| B+ → A+ | 3 (`011`) | 2 (`010`) | `010011` | `13` |

The material reader permits endpoints only at V sites 2, 3, 6, and 7, and only
between matching fixed signs. An O or D header is not a material destination.
The V site exposes its participant; the presence bit is not a unit packet.
The six bits specify where; the selected material reader specifies the local
transfer law. A bare link does not itself schedule a transfer or choose between
observation and material use. There is no cached open flag or amount in a link.

For a whole same-sign transfer between distinct sites, the resulting occupancy
is particularly simple: `target.V = old target.V OR old source.V`, followed by
`source.V = 0`. All O/D header bits stay fixed. This describes the coarse state
change; it does not turn presence into the quantity one. A self-link is handled
as the same site, so it neither clears nor doubles its state.

An explicitly grouped pair stores the positive link first, then the negative
link: `Pair(1A, 3E) = 0F9A`. Its twelve meaningful bits occupy bytes `9A 0F`;
the upper four bits are padding. Its selected material law validates both links
as matching sides between the same distinct columns before changing anything.
The pair grouping is essential: two unrelated links do not imply simultaneous
paired transfer.

| Arrangement | Independent amounts | What establishes the connection? |
| --- | --- | --- |
| Two reads of one material site | One | Both address the same storage site |
| Equal values at different sites | Two | Nothing; equality does not connect them |
| A site linked to a different site | Two before transfer | Ordered endpoint positions |
| Two sites and a buffer between them | Three | Two links plus an actual buffer store |

A link is a third **structural item**, but not a third amount-bearing object.
A self-link moves nothing; it does not duplicate a source. A shared site is
already one state and needs no transfer to keep its reads together. Neither
proximity nor coincident coordinates silently welds two independently stored
values. This experiment encodes sites and relations, not geometric coordinates.

## 4. What the two passages actually do

Start with `[5, 0, 5, 0]`, observed as `64`:

| Executed passage | Amount moved | Remaining world | Byte | Net A |
| --- | --- | --- | --- | --- |
| A+ → B+ (`1A`) | 5 positive | `[0, 5, 5, 0]` | `68` | −5 |
| Grouped pair (`0F9A`) | 5 in each side | `[0, 5, 0, 5]` | `A8` | 0 |
| Grouped pair from empty `20` | 0 | `[0, 0, 0, 0]` | `20` | 0 |

The paired law transfers the smaller available source amount along **both**
links. It preserves each column's net read while moving retained opposition.
For `[5, 0, 2, 0]`, it moves 2 in each side, leaving `[3, 2, 0, 2]`: net A
remains +3. The returned amount is per side, not a newly created positive total.
A second execution cannot transfer participation that has already left.

This is a selected pairing law in the finer apparatus. The primitive occupancy
bits cannot calculate the hidden minimum. There is no orthogonal bit: the fact
that this operation preserves a net read follows from its connected paired
action. A single-side connection does not provide the same guarantee.

An excessive withdrawal stops at available participation, keeps the fixed sign,
and reports the unmet draw as requested minus moved. It does not manufacture
flow or automatically promote the receiver into a signed structure. Overflow
and invalid grouped connections are refused before partial consumption.

## 5. Attaching a condition to a particular bit

A conditional passage stores ten meaningful bits:

| Physical bits | Meaning |
| --- | --- |
| 0–5 | Material link, source then target |
| 6–8 | Control-site position |
| 9 | Matching face: open on observed 0 or observed 1 |

For B+ → A+ (`13`), controlled by A+ V at bit 2:

| Condition | Encoded value | Stored bytes |
| --- | --- | --- |
| A+ is empty: match bit 2 = 0 | `093` | `93 00` |
| A+ is occupied: match bit 2 = 1 | `293` | `93 02` |

The upper six bits of the two-byte container are padding. The control attachment
is a read of site 2. The material attachment still runs from site 3 to site 2.
These roles are knowable from their positions in the conditional layout.

With world `[0, 4, 0, 0]`, byte `28`, condition `093` opens. The 4 units in B+
move to A+, yielding `[4, 0, 0, 0]`, byte `24`. A+ V is now 1, so the same
condition is closed on the next read. Condition `293` would initially block.
No open/closed result is stored separately, and the controlling zero supplies
no material: the material came from B+. The control is sampled once before
each transfer. This is a local step, not an asynchronous circuit model.

## 6. Retained direction with independently selected resolution

The memory experiment has a fixed six-bit block `[input I | state M]`:

| Physical bits | 0 | 1 | 2 | 3 | 4 | 5 |
| --- | --- | --- | --- | --- | --- | --- |
| Role | I.V | I.D | I.O | M.V | M.D | M.O |

Its declared transition is `I.V → M.V`; when I.V is 1, `I.D → M.D`; otherwise
M.D retains its prior value through feedback. O must match. `UpdateFrame`
executes this layout in one host byte with two padding bits; it preserves I.
These paths are fixed by this selected layout and transition law, not stored
as general connection records in the six state bits.

Starting with M=`000`, the following are state words **after** each observation:

| Fine input | +3 | +1 | 0 | −1 | −3 | 0 | +1 | +3 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Threshold 0 | `001` | `001` | `000` | `011` | `011` | `010` | `001` | `001` |
| Threshold 2 | `001` | `000` | `000` | `000` | `011` | `010` | `010` | `001` |

This demonstration reader resolves an input only when its magnitude strictly
exceeds the chosen threshold. Inside that region it exposes neither fine sign
nor magnitude. The memory supplies retained direction; resolution alone does
not. Here V=0 means no magnitude resolved by that reader, which is an explicitly
different read contract from the original exact-zero ODV word. At threshold 0
they agree. True zero produces V=0 at every demonstrated threshold.

The memory state is a controller/read state, not a second amount of material.
There is no automatic material effect of changing M.D. Connecting it to a later
passage would require a declared control attachment, just as in the preceding
example. General wiring, automatic discovery of formats, rebinding after table
relocation, and arbitrary geometric ports remain later experiments.
