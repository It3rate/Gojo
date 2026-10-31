# Controlled flow through two passages

Round 7, 31 October 2026. This experiment executes a whole handoff directly on
the primitive value stores. It adds a joint before/after law to the existing
positional arrays, local port mappings, and packed connection rows.

The later [conditional experiment](ConditionalExperiments.md) begins with a
simple recap and composes these passages into a two-way choice with continuations.

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --passages
dotnet test Tests/PrimitiveMeasure.Tests/PrimitiveMeasure.Tests.csproj
```

The small rule is: **when the control is present, move the source's whole
contents into an empty receiver**. A closed passage retains the source. An
occupied receiver blocks under this chosen no-merge rule. V=1 still means an
unknown nonzero amount; it does not establish one unit or a capacity of one.

## The three things being encoded

| Thing | Contents | Changes during a passage step? |
| --- | --- | --- |
| Value array | One V bit at each site; fixed O/D supplied by array position | Source and receiver may change together |
| Connection row | Three local port positions in order `[source, control, receiver]` | No |
| Shared transition law | Allowed combinations of all three bits before and after | No |

The connector owns no amount. A control is observed without consuming it.
The example's O=0 and D=0 are inherited by every value from its containing
array. Tests also exercise the other three fixed O/D contexts.

## One passage: exact locations and bits

All three values occupy `(orientation group 0, direction group 0, width 1)`.
Ports and bindings are published in allocation order for these examples.

| Role | Local port | Binding | Index in value array | Physical value bit |
| --- | ---: | ---: | ---: | ---: |
| S: source | 0 | 0 | 0 | 0 |
| C: control | 1 | 1 | 1 | 1 |
| T: receiver | 2 | 2 | 2 | 2 |

The row is `[0,1,2]`, with two bits per reference:

```text
packed row = 0 + (1 << 2) + (2 << 4) = 0x24
```

Each row reference follows this owner's existing `port -> binding -> address`
mapping. These are local port positions, not the even/odd endpoint numbers
used by the earlier calculated-read table. The containing table supplies that
interpretation; no reference-kind tag is stored in every row.

The value array packs S in its lowest bit:

```text
state code = S + 2*C + 4*T
```

These are the V bits of three participants, not the O/D/V bits of one participant.

We show tuples as `[S,C,T]`, in role order. A conventional binary numeral prints
the high bit first, so its order is `TCS`. Thus tuple `[1,1,0]` is code 3,
binary `011`; tuple `[0,1,1]` is code 6, binary `110`.

## The complete transition law

| Before tuple `[S,C,T]` | Before code | After tuple | After code | Meaning |
| --- | ---: | --- | ---: | --- |
| `[0,0,0]` | 0 | `[0,0,0]` | 0 | Empty source |
| `[1,0,0]` | 1 | `[1,0,0]` | 1 | Closed control retains contents |
| `[0,1,0]` | 2 | `[0,1,0]` | 2 | Open passage, empty source |
| `[1,1,0]` | 3 | `[0,1,1]` | 6 | Whole handoff |
| `[0,0,1]` | 4 | `[0,0,1]` | 4 | Receiver already occupied |
| `[1,0,1]` | 5 | `[1,0,1]` | 5 | No merge |
| `[0,1,1]` | 6 | `[0,1,1]` | 6 | Source already emptied |
| `[1,1,1]` | 7 | `[1,1,1]` | 7 | No merge |

Eight possible before-states and eight possible after-states require 64
admission bits, or eight bytes. A bit is addressed by:

```text
law bit = before + 8*after
law bytes, in storage order = 01 02 04 00 10 20 48 80
```

For the handoff, bit `3 + 8*6 = 51` is set: byte 6, bit 3. Byte 6 also admits
the unchanged `6 -> 6`, giving `0x48`. Every row in the passage table shares
these law bytes. The executor reads these bits; it does not dispatch on an
addition/transfer opcode.

The executable passage grammar accepts only laws that preserve C and either
hold S/T or hand the whole contents forward into an empty T. It rejects bitmaps
that create, discard, reverse, or duplicate participation. Alternative law
bits can permit a handoff when C=0; that behavior is also tested. This is a
small passage grammar, not a general material-transition interpreter.

## A resolved passage step

| Action | Value byte before | Value byte after | Connection byte |
| --- | --- | --- | --- |
| Execute with C closed | `01` | `01` | `24` |
| Supply a new control observation C=1 | `01` | `03` | `24` |
| Execute the passage | `03` | `06` | `24` |
| Execute again | `06` | `06` | `24` |

Supplying a control observation is separate from moving material. The passage
step validates a resolved before-observation against the current three sites,
then writes the source and receiver under the owner's lock. It never writes
the control. A single owner passage/scope observation cannot observe only half
the handoff. Separate read calls can straddle a step and are not a shared snapshot.

A successful step can be unchanged, such as a closed passage or an empty
source. Its returned after-state tells the caller what happened. A failed step
leaves every site unchanged.

## When the control is uncertain

The stored C bit is still concrete. The observer can deliberately read S and T
while leaving C unobserved. This is selected by a read-time visibility mask
`101`: read roles 0 and 2, omit role 1. The implementation does not load the
omitted value bit. The visibility mask is a query, not another stored primitive.

A joint support mask then describes complete possible states. Bit n means
state n is admitted. It is a read result, separate from the actual value array.

| Read | Complete candidate tuples `[S,C,T]` | Candidate codes | Support byte |
| --- | --- | --- | --- |
| Before: S present, T empty, C unobserved | `[1,0,0]` or `[1,1,0]` | 1 or 3 | `0A` |
| Predicted after | `[1,0,0]` or `[0,1,1]` | 1 or 6 | `42` |

This means **contents stay at S, or move to T**. It does not permit both stores
empty or both stores occupied. Independent uncertainty masks for S and T would
lose that constraint; the complete candidates preserve it.

`TryStep` refuses an unresolved before-observation before consulting hidden
value bits. Prediction alone never changes actual storage:

| Actual C in the fixture | Actual byte | Observer's before support | Attempt while C unobserved | After C is revealed and a fresh step executes |
| --- | --- | --- | --- | --- |
| 0 | `01` | `0A` | Refused; stays `01` | Resolved hold; stays `01` |
| 1 | `03` | `0A` | Refused; stays `03` | Resolved handoff; becomes `06` |

An empty support means no admitted state, not a measured zero. A custom law
with several after-states also remains prediction-only until a separate rule
resolves the choice. This experiment does not randomly select an alternative.

## Two passages in a chain

```text
        C1                    C2
         |                     |
A -- passage P1 --> B -- passage P2 --> T
```

One owner stores five ordinary V bits. Both passages use the same eight law
bytes. B is the actual shared intermediate site: P1's receiver is P2's source.

| Role | Port and binding | Packed address `(O group,D group,width,index)` | Value bit |
| --- | ---: | --- | ---: |
| A | 0 | `(0,0,1,0)` | 0 |
| C1 | 1 | `(0,0,1,1)` | 1 |
| B | 2 | `(0,0,1,2)` | 2 |
| C2 | 3 | `(0,0,1,3)` | 3 |
| T | 4 | `(0,0,1,4)` | 4 |

| Passage | Ordered port row | Reference width | First bit in connection array |
| --- | --- | --- | ---: |
| P1 | `[0,1,2]` | 3 bits | 0 |
| P2 | `[2,3,4]` | 3 bits | 9 |

Rows have no intervening padding:

```text
row P1 = 0 + (1 << 3) + (2 << 6) = 0x088
row P2 = 2 + (3 << 3) + (4 << 6) = 0x11A
combined = 0x088 | (0x11A << 9) = 0x23488
connection bytes = 88 34 02
```

There are 18 meaningful connection bits; only the end of the last byte is
unused. The five value bits fit in one byte. Including the shared law, these
payload arrays occupy 12 bytes (one value byte, three row bytes, eight law
bytes). The existing host objects, local port/binding maps and allocation
liveness are additional; this is not a claim that the complete runtime uses
12 bytes or that every future rung needs this same layout.

With both controls open, execute P1 and then P2:

| Step | Tuple `[A,C1,B,C2,T]` | Byte | Conventional binary |
| --- | --- | --- | --- |
| Start | `[1,1,0,1,0]` | `0B` | `00001011` |
| Execute P1 | `[0,1,1,1,0]` | `0E` | `00001110` |
| Execute P2 | `[0,1,0,1,1]` | `1A` | `00011010` |
| Repeat either passage | `[0,1,0,1,1]` | `1A` | `00011010` |

Each handoff moves the same whole contents along one passage. It is not a copy
into a second amount. The connection bytes stay `88 34 02` throughout.

If C2 starts closed, the progression is `03 -> 06 -> 06`: contents reach B and
remain there. Supplying C2=1 changes the control bit, giving `0E`; executing P2
then gives `1A`. Calling P2 before P1 instead gives `0B -> 0B -> 0E`. Order is
explicit and observable; there is no automatic scheduler or hidden chain run.

## An uncertain control across both passages

Keep the joint read over the whole selected five-site scope. Its state code is:

```text
state = A + 2*C1 + 4*B + 8*C2 + 16*T
```

There are 32 possible scope states, so a 32-bit read mask suffices. This mask
is a set of candidate states, not a new stored flow value or a global world.
Scope prediction replaces the selected passage's bits inside each complete
candidate and carries all the other bits forward unchanged.

With C1 unobserved and C2 known open:

| Stage | Complete candidate tuples `[A,C1,B,C2,T]` | State codes, hex | Joint support, hex |
| --- | --- | --- | --- |
| Before | `[1,0,0,1,0]` or `[1,1,0,1,0]` | `09`, `0B` | `00000A00` |
| Predict P1 | `[1,0,0,1,0]` or `[0,1,1,1,0]` | `09`, `0E` | `00004200` |
| Then predict P2 | `[1,0,0,1,0]` or `[0,1,0,1,1]` | `09`, `1A` | `04000200` |

The prediction says the contents remain at A or reach T. It does not leave a
possible copy at B after the second passage with C2 open. In the fixture the
actual array stays `0B` throughout prediction and the refused uncertain step.
After revealing C1, resolved execution follows `0B -> 0E -> 1A`.

With both controls unobserved:

```text
joint support: 00000A0A -> 00004242 -> 04000242
state codes:   {01,03,09,0B} -> {01,06,09,0E} -> {01,06,09,1A}
```

Now the contents can remain at A, stop at B, or reach T. Every complete
candidate has exactly one of those three material sites occupied. The
prediction assumes controls retain their sampled states across the chosen
steps. A control change would be another explicit input.

Tests also connect both passages to the same uncertain control. Keeping its
shared site in the scope prevents it from being treated as open for P1 and
closed for P2 within one unchanged-control prediction.

## Limits and checks

This is one owner-local, fixed-context experiment. The two passages do not
reach into another structure's private values. Boundary transfer, wider values,
sign/orientation release, merge/split, and cyclic scheduling remain later work.

Rows can outlive storage changes. Every read and step rechecks compatible fixed
O/D, one owned V bit, and distinct bindings, including aliases published through
different ports. Unsupported rows have no admitted read and cannot write. A
step compares its resolved before-observation with current state under the
same lock used for both writes. A stale competing passage cannot consume an
already emptied source. This is current-state validation, not a versioned
history or a guarantee that equal states belong to the same observation event.

The whole-handoff law promises movement of all the unmeasured contents at this
resolution. The encoding can check permitted site transitions and prevent
duplication; it cannot independently measure or reconstruct a finer amount.
No fine integer world is hidden behind these bits.

Focused tests cover the complete law in all four contexts, exact packed bytes,
blocking/reopening, repeated and competing steps, uncertain controls, both
chain orders, shared-control correspondence, scope reordering, stale reads,
aliases, migration, and refusal of missing, ambiguous or invalid transitions.

Verification: all 141 primitive tests pass, including 32 passage cases. The
`--passages` demonstration runs successfully. Full `dotnet test Gojo.slnx`
verification passes with 5,697 tests across 14 projects, no failures and no
skipped tests.
