# A conditional made from connected passages

Round 8, 31 October 2026.

**Overview:** the previous pass moved contents along one path, with gates that
could allow or hold movement. This pass gives the same source two possible
outlets. One control chooses which outlet receives the contents. Each outlet
then feeds another passage. The two paths eventually meet at a shared receiver.

```text
                   C present -> Yes outlet -> continuation --+
source S ----------+                                         +-> Done
                   C absent  -> No outlet  -> continuation --+
```

That is a small `if / else` expressed as connected flow. Only the selected
outlet receives this source's new contents. An empty source supplies nothing,
even when a route is open. A blocked selected outlet holds the source.

An **eight-byte transition law** is simply a compact table of which complete
before-and-after bit patterns one passage allows. We reuse that mechanism for
the two complementary routes and the continuations. No conditional opcode or
branch-choice field is added to the values or connection rows.

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --conditionals
dotnet test Tests/PrimitiveMeasure.Tests/PrimitiveMeasure.Tests.csproj
```

## What the previous pass did

The [previous passage experiment](PassageExperiments.md) had three material
sites arranged as `A -> B -> T`. Each passage also read a control site. An open
passage moved the source's whole contents into an empty receiver, changing
their presence bits together. A closed passage left those contents where they
were. Repeating a step after its source emptied could not send a second copy.

Its uncertain read meant "the contents stay here, or move there." Prediction
retained both complete possibilities. It did not choose one or update storage.

This was already a conditional gate. The new experiment makes an explicit
two-way choice and shows that each resulting outlet can feed further structure.

## What is attached to the outlets

Each outlet is an ordinary value site. The same site is the receiver of its
selection passage and the source of its continuation passage. That shared
attachment is what joins the two pieces; equal values elsewhere would not.

The continuation law permits a whole handoff for either control value. Once
contents enter an outlet, changing the original condition does not veto their
continuation. The current three-role layout still references C, but its value
does not determine whether that continuation is open.

The two continuations share Done so that the example also demonstrates paths
rejoining. Done's presence alone does not reveal which path was taken. A larger
branch could attach different connected work, but this pass demonstrates
routing and continued movement, not an arbitrary instruction body or scheduler.

Connections therefore carry more than inventory. The value sites retain what
is currently present; their shared references retain correspondence; the wiring
and local laws determine which interactions and changes are possible.

## Five stored value bits

Every site has fixed O=0 and D=0, inherited from its array position, and owns
one V bit. V=1 means unmeasured nonzero contents, not a quantity of one.

| Site | Role | Local port / binding | Value-bit position |
| --- | --- | ---: | ---: |
| S | Contents to route | 0 | 0 |
| C | Condition: absent or present | 1 | 1 |
| Yes | Receives S when C is present | 2 | 2 |
| No | Receives S when C is absent | 3 | 3 |
| Done | Common receiver after either continuation | 4 | 4 |

The packed addresses are `(0,0,1,index)`, with index equal to the final column.
References still use the owner's existing `port -> binding -> address` mapping.
These are local port positions, not global identifiers.

```text
stored state = S + 2*C + 4*Yes + 8*No + 16*Done
tuple order  = [S,C,Yes,No,Done]
```

## The connections and their laws

Each row remains `[source, control, receiver]`. Every reference uses three bits.
Rows sharing a law occupy that law's table; the operation is not tagged on each
row. The two continuations share one table and pack without a gap between rows.

| Law table | Row(s) of local port positions | Packed row bytes |
| --- | --- | --- |
| Open when C is present | `[0,1,2]`: S, C, Yes | `88 00` |
| Open when C is absent | `[0,1,3]`: S, C, No | `C8 00` |
| Continue for either C value | `[2,1,4]`, `[3,1,4]`: Yes/No, C, Done | `0A 17 02` |

For example, the continuation rows are `0x10A` and `0x10B`. Packing each in
nine bits gives `0x10A | (0x10B << 9) = 0x2170A`, stored low byte first.

The law still describes three V bits, using local state code
`source + 2*control + 4*receiver`. Law bit `before + 8*after` admits that edge.

| Law | Non-identity transition(s) | Eight stored law bytes |
| --- | --- | --- |
| C present | `3 -> 6`: `[1,1,0] -> [0,1,1]` | `01 02 04 00 10 20 48 80` |
| C absent | `1 -> 4`: `[1,0,0] -> [0,0,1]` | `01 00 04 08 12 20 40 80` |
| Either C value | Both transitions above | `01 00 04 00 12 20 48 80` |

Every other local state stays unchanged under these laws. In particular,
neither a closed gate nor an occupied receiver consumes the source.

## One choice, then separate continuations

Selection applies the two complementary rows as one owner-local step using a
single resolved observation of `[S,C,Yes,No]`. It computes their intermediate
states, validates the whole sequence, and commits only the final differences
under one lock. This prevents C from changing between the two route checks.

The selection scope deliberately excludes Done. A separate change at Done
cannot invalidate a choice that does not touch it. The continuation later checks
Done's actual state when it attempts its own handoff.

This grouping is a general operation over explicitly supplied existing rows,
not a special conditional instruction. Reversing these two selector rows gives
the same choice because their control conditions are complementary. A missing,
ambiguous, stale, incompatible, or cross-owner step prevents the entire grouped
update; no earlier partial movement remains.

The demo then explicitly steps each continuation. A fresh unselected outlet is
empty, so its step changes nothing. An unselected outlet already containing
older contents can still continue those contents when explicitly stepped: this
conditional routes the new source, not every activity in the surrounding system.

## Actual bits: the two cases

| Case and stage | Tuple `[S,C,Yes,No,Done]` | Stored byte |
| --- | --- | --- |
| C absent: start | `[1,0,0,0,0]` | `01` |
| Select No | `[0,0,0,1,0]` | `08` |
| Step Yes continuation: empty | `[0,0,0,1,0]` | `08` |
| Step No continuation | `[0,0,0,0,1]` | `10` |
| C present: start | `[1,1,0,0,0]` | `03` |
| Select Yes | `[0,1,1,0,0]` | `06` |
| Step Yes continuation | `[0,1,0,0,1]` | `12` |
| Step No continuation: empty | `[0,1,0,0,1]` | `12` |

The rows and law bytes remain unchanged. Repeating selection with the source
empty sends nothing further.

Two edge cases clarify what the conditional means:

- **Selected outlet blocked:** with C=1 and Yes occupied, byte `07` stays `07`.
  The empty No outlet does not become a fallback. With C=0 and No occupied,
  byte `09` stays `09` for the same reason.
- **Control changes after selection:** choosing Yes gives `06`. Supplying C=0
  changes only the control bit, giving `04`. The Yes continuation still moves
  the already chosen contents to Done, giving `10`.

Changing C is a separately supplied observation, not material transfer. A
successful unchanged step means a resolved legal hold, not that flow moved.

## When the condition is uncertain

As before, uncertainty belongs to the read. The fixture stores a concrete C,
but the observer can omit that site. No extra "unknown" value is written into
the primitive.

For a nonempty source and empty outlets/Done, the observer admits initial
states `01` or `03`. Its joint support mask is `0000000A`: bit n admits the
complete five-site state n. The prediction carries each whole candidate through
both selection laws and both continuations.

| Predicted stage | Possible complete state codes, hex | Joint support mask |
| --- | --- | --- |
| Before | `01`, `03` | `0000000A` |
| After C-present row | `01`, `06` | `00000042` |
| After C-absent row: selected outlet | `06`, `08` | `00000140` |
| After Yes continuation | `08`, `12` | `00040100` |
| After No continuation | `10`, `12` | `00050000` |

After selection, the possibilities are exactly:

```text
[0,1,1,0,0]  contents at Yes, C present
or
[0,0,0,1,0]  contents at No, C absent
```

After both continuations, the possibilities are:

```text
[0,0,0,0,1]  Done present, C absent
or
[0,1,0,0,1]  Done present, C present
```

This yields a useful coarse conclusion: **either route would reach Done** under
these empty-receiver premises and the explicit sequence of steps. The condition
and selected route remain unresolved. No candidate duplicates the source or
makes its contents disappear.

A definite prediction about Done does not execute either branch. Grouped
execution still requires a resolved before-observation and uniquely resolved
intermediates. With C hidden, the attempt is refused and the fixture's actual
byte remains `03`. Revealing C=1 allows actual selection and continuation:
`03 -> 06 -> 12`.

## How this maps to settlement and other conditionals

An outlet can be the input to more connected structure; this pass makes that
attachment literal by sharing the outlet's value site between two rows. At a
structure boundary, a later implementation must use exposed ports rather than
reaching into the peer's private sites.

This experiment selects **presence versus absence** in one fixed O/D context.
It does not yet select positive, zero, and negative outcomes of a signed merge.
Those outcomes could supply conditions for corresponding passages once their
reads are explicitly connected. Here C is a supplied observation.

In particular, a zero condition can open a passage carrying material from S.
It does not create material from zero. If the observed quantity has net zero
but retained opposing participation, access to that participation must come
through its actual retained structure; a bare zero read cannot recover it.

The current executor remains explicitly stepped, local to one owner, and limited
to compatible one-bit stores. It provides conditional routing and continuation,
not a general automatic scheduler, side-effect interpreter, or proof of emergent
intelligence. No extra numeric resolution or per-value history record was added.

## Verification

The `--conditionals` demonstration runs successfully. All 179 focused tests
pass, including 38 new conditional cases. Full `dotnet test Gojo.slnx`
verification passes with 5,735 tests across 14 projects, with no failures or
skipped tests. The cases cover exact encodings, both routes, blocked outlets,
changed controls, joint uncertainty, stale observations, and atomic refusal
without partial movement.
