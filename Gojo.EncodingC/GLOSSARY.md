# Gojo.EncodingC glossary

Status: **exploratory**, 12 November 2026. This describes the code as it is.
Each entry says what a word means and how it is held in bits. The
[README](README.md) explains how the parts are used together.

Entries are marked in three ways:

- Unmarked: a part of a running machine. It is saved with the machine.
- *(pattern)*: an arrangement of parts used in the tests and the README, such
  as a plan or a trust count. No type has that name, and the encoding does not
  know the word.
- *(host)*: code that reads or edits the encoding from outside. A saved machine
  does not contain it.
- *(not built)*: something the project works toward. No code holds or checks
  it yet.

## Words used in two senses

| Word | One sense | The other sense |
| --- | --- | --- |
| Structure | Connected parts that compute. A *local structure* is one isolated group of them: a local intelligence | Until 12 November 2026 the code called a block of state `Structure`. That is now a *store*, and this glossary never uses "structure" for it |
| Open | An open *value* is not settled: more than one value is still possible | An open *gate* or valve lets flow through: its bit is set. A gate that nothing has decided is a gate whose value is open |
| Lane | A port's lane: the i-th position the port exposes | A lane of a running law: row i, when the table is applied to every row at once |
| State | The bits a store holds now | One combination of a law's role values, numbered 0 to N − 1 |
| Read | To look at bits without changing them | One way of interpreting bits, as in "a coarse read" |

## Storage

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Position | One numbered place in a store. It has no name. Its number and what is wired to it are its identity | Not stored. Bit i of every plane belongs to position i |
| Plane | One bit for every position. It is the only storage shape: state, selections, sets of rows and open values are all planes | A run of bits as long as its store |
| Store | A block of state: positions and the planes over them. It holds bits and does nothing. It has as many positions and planes as it needs | planes × positions bits, plus the next three rows |
| Constant plane | A bit every position of a store shares, such as which way the whole store faces | One bit for the whole store |
| Spare, in use, retired | A position is spare (room, not yet in use), in use, or retired (never reused). It only moves forward through these | Two bits per position: in use, and ever used. Saved only when some position is not in use |
| Life | "In use" shown as something a law can bind to. A law recruits a spare position by setting its bit and retires one by clearing it | The in-use bits, read as one plane |
| Shape | How many values something has, and which of two layouts holds them. It says nothing about what they mean | In a saved law: one bit (unary or binary) and the size |
| Binary value | One value among s, as an ordinary binary number | ⌈log₂ s⌉ bits, low bit first |
| Unary count | A count from 0 to k in which more of something sets more bits. Adding one shifts a bit in, and comparing is a mask test | k bits. Bit j means "more than j", so 3 of 4 is `0111` |
| Unit | One of whatever a count counts. It has no meaning beyond its amount | One set bit of a unary count |
| Code | A bit pattern that is a value of its shape | Binary: a number below the size. Unary: a run of set bits from the low end |
| Not a code, broken count | Bits that no value of the shape matches, such as unary `010`. Noise this read cannot interpret | Left exactly as they are, and reported |
| Reference | A value that names a position or a lane | Binary, ⌈log₂ n⌉ bits for n targets. A plane is added when a larger target arrives |
| Field *(host)* | Where one value sits: a shape and the planes that hold its bits. Different readers can read the same planes as different fields | Not stored. In a machine, a binding's list of planes does this job |

## Connections

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Local structure | The stores and wirings that form one local intelligence. Inside it, laws connect to any of its stores freely and everything settles together. Between two local structures only ports connect, and each steps on its own | An `Engine`: its stores, and its wirings in order. A store belongs to one engine, and an engine refuses to bind another's store directly. Two engines fuse into one with `Fuse`. Saved, it is the grouping of stores in one machine; nothing names it |
| Surface | Anything a law's role can be bound to: a store, a port, a socket, a store's life, a workspace, or a workspace's gap. Every read and write goes through one | No bits of its own. In a saved wiring: 0, 1, 2, 3 or 4 (store, port, life, socket, a flipped view of a store) and which one |
| Port | What a local structure shows to others, from one of its stores: some of that store's positions, in order, and some of its planes, in order. It carries no unit, type or description | A list of references (the lanes) and a list of plane numbers |
| Socket | The receiving end of a connection: the shape a local structure expects from another's port, and which port fills it now. Empty, or on a lane the port lacks, it reaches nowhere, and rows through it are skipped | Saved as its shape (lanes, planes, which planes are constant) the first time a wiring binds it. Which port fills it is saved only with structures saved together |
| Taker | The one connection that takes through a port: a wiring bound to it, or a socket plugged into it. Readers are unlimited. No position is shown by two taken ports | Not stored as bits; it follows from which wirings write through the port |
| Attention *(of a wiring)* | Which rows run: selection connected to execution, in a running structure | A plane of the wiring's rows, filling with 1; only rows whose bit is 1 run. A law can write it |
| Lane (of a port) | The i-th position a port exposes. Connections hold lane numbers, so the owner can point a lane somewhere else and every connection follows | One reference |
| Read (copy) | A role its law never changes. Through a port this is a reference: the sender is untouched, and any number of receivers can hold one | No flag. It is a fact about the law's table |
| Written, move | A role its law changes. Writing through a port is allowed only as a move: what leaves there arrives somewhere the same law writes | No flag. The table changes the role, and the law's accounting holds a preserved sum over it |
| Gate, valve *(pattern)* | A bit a move law reads to decide whether flow passes | One bit, in whichever store holds it |
| Blocked, remainder | What did not move because there was nothing to take or no room. It stays where it was | The count is unchanged |
| Calibration *(pattern)* | A rate between two things, for a structure that wants one. A connection never needs one | A law whose preserved sum has unequal weights, or a small structure with an input and an output |

## Rules

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Law | A rule shared by every row wired to it. It is always a table of bits (see [below](#is-a-law-always-a-table)) | One bit per combination of role values |
| Role | One numbered argument of a law, with a shape and no name | Its place in the law's list of roles |
| Tuple | One combination of values, one per role | A number. Role 0 is the lowest digit, and each role is a digit in its own size |
| Relation law | The combinations that can hold together. It never changes state. It rules out what cannot be | One bit per tuple, set when admitted. AND over (a, b, out) is the byte `87` |
| Transition law | Which whole state may follow which. It changes state | Bit `before + N·after` for N states. Saved as a list of next states, N·⌈log₂ N⌉ bits, when every state has exactly one |
| Admitted | A combination or a transition the law allows | A set bit in the table |
| Functional | Every state has exactly one next state, so the law runs on bits with nothing left to choose | A property of the table |
| Accounting, promise | What a transition law promises about every admitted pair: a sum is preserved, a role is unchanged, a role never rises or never falls. Checked over the whole table when the law is made. A table that breaks a promise is refused | A short list: the kind (two bits), the roles, and the weights of a sum |
| Preserved sum, conservation | Σ weight·value is the same before and after. With every weight positive, nothing is made or lost, only moved | Roles and weights in the accounting |
| A law read as a relation | A transition table read as a relation between a before and an after. It can then be followed in either direction without being run | No new bits. Saved as "a reading of law n" |
| Choice role | One more role that picks among several next states, for a law that does not say which | An extra role, as large as the most options |
| Stock law | A small table filled once by a function, then used as a table like any other | An ordinary law. Nothing knows it by name |

The stock laws:

| Law | Roles | What its table says |
| --- | --- | --- |
| `Passage` | source, gate, target: one bit each | The unit hands over when the source is set, the gate is set and the target is clear. 64 bits, `01 02 04 00 10 20 48 80`; 24 saved |
| `Move`, `Convert` | source count, target count | Take some units and give some, or do nothing. `Move` is one for one |
| `GatedMove` | source count, gate bit, target count | Move one unit while the gate is set |
| `Toggle` | one bit | Flip every step: an oscillator |
| `Same` (relation) | two roles of one shape | They hold the same value. Between a sample and an open value, this is observing |
| `Forget` | an open value's planes, as plain bits | Clear them: the value is open again |
| `Enact` | an open value's planes, then a field | Write the one value left into the field. Otherwise leave the field alone |

## Operations and numbers

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Operation | A relation and a binding: what holds, and how one step uses it. One relation gives several operations that differ only in binding | A relation law, and the transition law `Operations.Bind` makes from it |
| Binding (of an operation) | For each relation role: read a law role before the step, write one after it, or neither | Two lists of law role numbers, −1 for none. Only the table it makes is saved |
| Free role, residue | A relation role a binding neither reads nor writes: what the step had nowhere to put, such as the excess of a shortfall or the overflow of an accumulation | A relation role with no law role |
| Cost | What leaving residue in each free role costs. The completions with the least cost are kept; with no cost, every completion stays open | One integer per relation role, read as Σ cost · value over the free roles |
| Holding, blocked bit | Where no completion fits, the step leaves the written roles as they were and the relation unmet. The blocked bit tells quiet because met from quiet because held | An optional bit role of the law: 1 where it held, 0 where it wrote |
| Balance | Σ weight · value = 0. A weight's sign says which pan, its size the rate. *Along*: units trade one for one at weight 1 | A polynomial whose terms are single roles at exponent 1 |
| Product | a · b = c. *Across*: a unit of c is there only where a unit of a and a unit of b are, a corner at every cell | The polynomial ab − c |
| Used up, kept | Whether a written role's gain is paid for by another role (a move) or by nothing (a read) | Not stored: read from the table and its accounting |
| Polynomial *(host)* | A relation as integers: Σ weight · Π value^exponent = 0, in canonical form, so equations compare and are searched as numbers | Per term, a weight and one exponent per role. `Bits` is its description length. The table is built from it once and is the only part saved |
| Digit | A count with as many values as its rung's radix | A field of `Numbers.Digit(radix)`: unary (radix − 1 planes) or binary-coded |
| Rung, radix, partition | One place of a number, and how many parts it has. Each rung has its own radix, so fifths can sit under whole stars | A digit field. The radix is the field's shape size |
| Ladder *(host)* | A number as rungs, finest first, each worth the product of the radices below it. The helper that wires a ladder and reads it | `Ladder`: digit fields and their supports, all in one store |
| Column | a + b + carry in = radix · carry out + sum: one rung of a sum. For a digit times a digit: a · k + carry in = radix · carry out + digit | A balance relation, wired once per rung |
| Carry, overflow, borrow | What one rung passes to the next. The last carry is what the number cannot hold: overflow in a sum, borrow in a difference | Supports of a count of 0 or 1 (in a sum) or of a digit (in a scaling). The finest carry in is a constant 0 |
| Count, measurement (resolution) | Below the rung that was read, the finer rungs are closed (zero) for a count, which is exact, and open for a measurement, which is not | Supports narrowed to 0, or left open |
| Interval | The least and greatest values the rungs still allow, read rung by rung. Every jointly allowed value lies inside it | Read from the supports; not stored |
| Same share, rescale | Two shares are equal when the numerators balance, with the denominators as weights. Rescaling reads a count out of one partition as a count out of another, and keeps what does not divide | `Numbers.SameShare`, a balance; `Numbers.Rescale`, a balance with a residue role |
| Reopen *(host)* | Open a ladder again, or every open binding of a network, before a new observation. Constants stay | Sets the supports' planes back to 1 |
| Unit rung, ticks per unit | Which existing rung of a ladder reads as one unit of a locally agreed standard, and how many finest ticks it holds. Sums need the same selection; a different standard needs an explicit weighted relation | `Ladder.AtUnitRung`, a view over the same supports; `TicksPerUnit` is the product of the radices below it |
| Scope, disjoint positions | The members a count is read from. Two scopes pool only if their live published positions are different sites | `Surface.DisjointPositions`, read from the ports as they are now; not stored |
| Portion *(live denominator)* | A count out of a denominator that is itself open, such as a pooled review count | `Portions`: relations for validity (d ≥ 1, n ≤ d), pooling, same-whole addition, projection with remainder and comparison |
| System (of equations) | Several polynomials over the same roles, read as one table | `Operations.System` |
| Residues | Hidden free roles found together, such as the two faces that make min, max or \|a − b\| | `Recognition.Residues` |
| Compiled plan *(host)* | A faster way to run an engine using two general table kernels (small-table transitions and word-outer bit-sliced relations, lowered strictly from a law's own bits), falling back to the engine everywhere else | `CompiledPlan`. Never saved; dropping it changes nothing |
| Recognition *(host)* | Finding the equations a table or a few witnesses keep | `Recognition`. It returns polynomials, tuples and witness indexes, never a kind |
| Unexplained | Tuples that a found system admits and nothing witnessed. Empty means the system is exact | A list of tuples |
| Exception | A witness that a fit does not explain | Its index |
| Restore *(host)* | Rebuild a law from an equation and a binding, only where the table is the same, and attach the original's accounting | A new transition law |

## Running

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Wiring | A law bound to rows. A binding per role says which surface and planes; each row says which position each role uses there | A law number, the bindings, and the rows |
| Binding | Where one role reads and writes. Plain: the planes hold a code. Open: one plane per value, set where the value is still possible | A surface, plane numbers and, for a relation, one bit for open or plain |
| Row | One use of a law: one position per role | A position in the wiring's own store. Each role's reference is a binary value in that store's planes |
| Rows | A wiring's rows, as a store. What is connected to what is therefore state: a law can rewire a row, bring a spare one into use or retire one | An ordinary store |
| Evidence | What each row did in the last step, kept once a wiring is asked to record | Seven planes of the rows, in the order below |
| Changed | The row's bits changed | Evidence plane 0 |
| Unread | The row's bits were not codes | Evidence plane 1 |
| Unadmitted | The law admits nothing for the row's state. For a relation, this is a contradiction | Evidence plane 2 |
| Ambiguous | Several possibilities and nothing to choose: several next states, or a role still open | Evidence plane 3 |
| Skipped | The row touches a position that is not in use, or holds a reference to nowhere. It did not run | Evidence plane 4 |
| Aliased | Two roles met at one bit that one of them writes. The row did not run | Evidence plane 5 |
| Contested | A step taken together held the row back, because another row would write the same bit: a choice of taker that nothing settled. Never set by a step in order, where declared order chooses | Evidence plane 6 |
| Engine | Steps the wirings in their declared order | The order of the wirings. A separate list only when one runs more than once in a step |
| Step, tick | One pass through every wiring. The mechanism as a whole is expected to lower its energy from step to step, but a step need not, and a machine may never get there | Not saved |
| The two step rules | A transition row takes the next state its table gives. A relation row rules out, for its open roles, every value no admitted tuple supports | Both are lookups in the law's table, across all rows at once |
| Declared order | The order in which wirings were added and rows listed. It decides who goes first when two want the same thing | The order in the saved machine |
| Wave *(host)* | Wirings, or rows, that touch nothing another one writes. They could run at the same moment | Worked out each step. Not stored |
| Machine | An engine and every store it reaches: what is saved. So far a machine is one local structure | One run of bits |
| Inflow *(host)* | Units arriving from outside. They enter as units, never as a value written directly | `Unary.Add` shifts one bit into a count |

## What is not settled

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Open value | A value that is not settled yet: what it may still be | One plane per value, set where that value is **still possible**. The planes fill new positions with 1 |
| Ruled out | One thing learned about an open value | One set bit |
| Open, settled, contradiction | Everything possible; one value possible; nothing possible | For a one-bit value, two planes ("0 is possible", "1 is possible"): `11` open, `10` the value is 1, `01` the value is 0, `00` contradiction |
| Supports *(host)* | The helper that reads and edits an open value's planes. In reports, "supports" also means the planes themselves | The planes above |
| Sample | A plain, settled value that a relation reads. It is never narrowed | A code in ordinary bits |
| Narrowing | Ruling out the values no admitted combination supports. It never rules out something possible. It can miss a conclusion that needs two rows taken together | Sets bits in ruled-out planes |
| Observe *(pattern)* | Narrow an open value to what a sample reads | `Same` wired from ordinary bits to an open value |
| Forget *(pattern)* | Open a value again. Wired to run every step, it is the passing of time | `Forget` clears the ruled-out planes |
| Enact, act *(pattern)* | Write a settled value into ordinary bits: what was worked out becomes what is | `Enact` wired from an open value to a field |
| Supposition *(pattern)* | Something taken as true to see what follows, kept apart from what is | Bits written in a workspace, or a separate store (a probe) |
| Gap | Where a workspace and its source disagree: how a prediction missed, or what is left before a goal is reached | Not stored. A gap bit is set where the workspace holds a bit that differs from the source as it is now. A per-bit disagreement: for a unary count, a set of units rather than a count, and with no direction. A two-faced error (deficit and excess counts) is a later direction |
| Counting law | Arithmetic written as a small table: a sum, a shortfall, a tally, a threshold, an accumulation | `Counts`. Each reads counts and writes one count or bit, only when it changes; the emulation of numeric support |
| Rank | Which of two tiers of tension decides whether a step lowered or raised tension | `Counts.Rank`: the higher tier's dropped and rose bits when it changed, otherwise the lower's |
| Value gap | Whether two open values disagree | `Settling.ValueGap`: a relation narrowing both values and the disagreement every way |
| Plan *(pattern)* | What is possible at several moments ahead | A store of open values. In the tests, three positions per moment and five planes: three hold whether counts 0, 1, 2 are possible and two whether gate 0, 1 are |
| Moment *(pattern)* | One time step in a plan | A group of the plan's positions |
| Goal, aim *(pattern)* | A wanted value, treated as already settled at a later moment. Narrowing then works back to what has to happen now | Ordinary bits, observed into the plan's last moment |
| Giving way *(pattern)* | Lowering a goal that cannot be reached | A unary count, raised by a law that reads the plan's "unadmitted" plane |
| Probe *(pattern)* | A second plan in which one choice is supposed. If it contradicts itself, that choice is ruled out in the real plan | A second plan store, and one row from its "unadmitted" plane that clears a "possible" plane of the real plan |
| Prediction *(pattern)* | What a structure expects next, written before it happens | Bits in the predictor's own planes |
| Judge *(pattern)* | A law that compares a prediction with what happened and moves trust | A row |
| Trust *(pattern)* | How well a predictor has done | A small unary count |
| Attention, focus *(pattern)* | What a row is looking at | A reference in the rows' planes, which a law can move |
| Ladder, rung, carry | Counts of one thing at several resolutions. Each level is a rung. A carry turns several fine units into one coarse unit (see [Operations and numbers](#operations-and-numbers)) | In material, unary counts and a `Convert` law (`Numbers.Group`, `Refine`, `Admit`), the weights of the preserved sum being the place values; as a number, a `Ladder` of column relations |
| Pair value *(pattern)* | Two values that vary together, held as one value | One open value with a·b values, and a relation from it to each part |
| Plant *(pattern)* | In the tests, the small structure being steered: a source, a gate and a target | Three positions: two planes for a count, one for the gate |

## Workspace *(host)*

A workspace is not saved, and the engine does not manage one. Inside a machine
the same job is done by ordinary structure, such as a plan or a probe.

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Workspace | A fragment of an existing structure, held apart from it. Changes made there are the workspace's own values, and its work is refining the structure it refers to. It can later be dropped or folded back in. It is a live view of a store, or of another workspace, that holds only what was changed in it. The type was called `Rung` until 12 November 2026 | For each plane it has written, three planes: which bits it holds, its values, and the source bits they replaced |
| Copy on write | Writing a position copies that whole position in. Everything else reads the source as it is now | As above. Positions never written cost nothing |
| Selection | Whatever is in a workspace. It makes no claim about what it leaves out. Kept that primitive for now | A short list of layers: an area plane or "all", and one keep-or-ignore bit each |
| Not selected | The nearest thing to nothing: no claim either way. A zero is different: it is a sample that keeps its place | Absence from the selection |
| Reach, selection by use | A row that touches something selected brings in what else it touches, up to a few steps from what was asked for | A unary count per position: steps left |
| Introduced, candidate | A new position that exists only in the workspace until it is committed | One reference per introduced position: 0, or s + 1 once adopted as source position s |
| Changed together | Changes one row made together, such as both ends of a move. They commit or revert together | One parent reference per position |
| Stale | The source moved under something the workspace changed. A stale commit writes nothing | Found by comparing the kept source bits with the source |
| Commit, revert, drop | Write the changes into the source; hand positions back to the live source; throw the workspace away | No bits of their own |

## Measure

| Term | What it means | How it is in bits |
| --- | --- | --- |
| ODV word | Three bits about a quantity, at one read's resolution | V is plane 0, D is plane 1, O is constant plane 2. Each is 1 for the dominant member: detectable, positive, positive-right |
| O, orientation | Which way this reader calls positive | One bit for the whole store |
| D, direction | Positive or negative, within that orientation | One bit per position |
| V | Whether any magnitude is detectable at this resolution. V = 1 is not the number one | One bit per position |
| Undetectable | V = 0: a sample below this read's resolution. It is still an operand, and it is not nothing | V clear, D and O kept |
| Detection, exact zero | Two rules for adding words. Under detection, the default, two undetectable amounts may add up to a detectable one. Under exact zero, undetectable is a hidden zero; a reader has to ask for it | Two relation tables over three words, 512 bits each |

## Learning

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Learned table *(host)* | A transition table worked out from what happened | The counts below |
| Counts | How often each (before, after) pair was seen, how often each before-state was seen, and how many rows could not be read | A store of N² + N + 1 small unary counts. A count stops at "often enough" |
| Witness *(host)* | To show the table one thing that happened | One unit into two counts |
| Admitted, unsupported, unknown | Seen often enough; its before-state was seen often enough and never led there; not seen enough to say | Read from the counts. "Admitted" is one plane of them |
| Crystallize *(host)* | Make a law from the admitted table once nothing is unknown. The law is checked against its accounting like any other | A new law table |

## Saved form and names

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Transport *(host)* | Saving a machine as bits, and restoring it without the code that built it. A machine is one local structure; a foreign port becomes a socket | One run of bits: stores, laws, wirings, then the order where needed. No names, headers or fixed widths. Several structures together: a count, each structure, then for each socket nothing or (structure, store, port) |
| Number | Any whole number in the saved form | The Elias gamma code of n + 1: 0 is `1`, 1 is `010`, 2 is `011`, 3 is `00100` |
| Machine size | How many bits a saved machine takes, split into stores, laws and wirings | `Transport.Measure` |
| Lens *(host)* | Names and notation for people. The encoding never reads it | Outside the encoding |

## Kernel words that are not parts here

- **Tension** is built for material only (`Tension`): pressure and debt as two
  unary counts beside a value. Information and structural tension are reads
  proposed in the tension note.
- **Residue** has no type. What a move could not take stays where it was, and
  what a coarse read drops is bounded by the relation that did the read. In an
  operation, residue is a free role with a stated cost. In a number, it is the
  last carry, or the residue role of a rescaling.
- **Carrier, location, relation, unit shape** belong to the kernel. This
  project does not depend on the kernel, and "relation" here means a relation
  law.

## Is a law always a table?

Yes. Every law is a table of bits, and nothing else runs. A relation has one
bit per combination of role values. A transition has one bit per
(before, after) pair.

A table comes from one of three places: bytes written out, a function that
fills the table once and is then gone (`FromFunction`, `FromPredicate`), or
counts of what happened (`Crystallize`). Afterwards only the table exists. The
engine does not know what "move" or "add" is.

Primitive operations are small tables:

| Operation | Roles | Table |
| --- | --- | --- |
| AND, OR, XOR, as relations | a, b, out: one bit each | 8 bits each: `87`, `E1`, `69` |
| The passage law | source, gate, target | 64 bits, 24 saved |
| Adding counts of 0 to 3 | a, b, sum | 112 bits, 16 of them set |
| ODV addition | three words | 512 bits |

`ABooleanOperationIsOneByteReadInEveryDirection` and
`AddingTwoCountsIsOneRelationThatAlsoSubtracts` pin the first and third rows.

**Why an operation is a relation.** One table then reads in every direction.
The AND table gives the output from the inputs, and gives the inputs from the
output when they are forced: an output of 1 settles both inputs. The add table
also subtracts: a sum and one part settle the other part. What is open passes
through: a first operand of 1 or 2, added to 3, leaves the sum as 4 or 5. The
price is that an open value takes one plane per value. An operation that only
ever runs forward on settled bits can be a transition law, which needs no such
planes and reads in one direction only.

**What keeps tables small.** A table has one bit per combination, so its size
is the product of the role sizes. Adding two counts of 0 to 15 takes
16 × 16 × 31 = 7,936 bits. So operands stay coarse, and a wider operation is
the same small table used at every rung of a ladder with a carry between
rungs, the way written arithmetic works digit by digit. One table, many rows.

**The level below.** The engine runs a table as AND, OR and NOT across whole
planes, one term per state. The passage law reduces to
`S' = S & (~C | T)` and `T' = T | (S & C)`. Boolean operations are therefore
both the smallest laws and what every law is run with. This adapter does not
reduce tables this way; it walks the states.

**Not built.**

- Saving a regular table by the rule that makes it. A `Polynomial` is that
  rule in memory, and `Recognition` finds it, but the saved form stores the
  bits, except that a transition with one next state per state is saved as
  that list.
- A table held in planes, so that a machine can change its own rules.

A wide operation as smaller laws wired together is built: `Ladder.Add`,
`Scale` and `Multiply` wire one column per rung.

A Boolean table is not what an operation means. It relates bits, and what
those bits stand for comes from what is wired to them: presence, a selection,
a gate. AND over two presence bits is the read "both are here".
