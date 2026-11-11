# Gojo.EncodingC glossary

Status: **exploratory**, 11 November 2026. This describes the code as it is.
Each entry says what a word means and how it is held in bits. The
[README](README.md) explains how the parts are used together.

Entries are marked in three ways:

- Unmarked: a part of a running machine. It is saved with the machine.
- *(pattern)*: an arrangement of parts used in the tests and the README, such
  as a plan or a trust count. No type has that name, and the encoding does not
  know the word.
- *(host)*: code that reads or edits the encoding from outside. A saved machine
  does not contain it.

## Words used in two senses

| Word | One sense | The other sense |
| --- | --- | --- |
| Rung | A level of a ladder: a coarser or finer count of the same thing | `Rung`, the workspace type: a live view of a structure that holds only what was changed in it. This glossary says "ladder rung" and "workspace" |
| Open | An open *value* is not settled: more than one value is still possible | An open *gate* or valve lets flow through: its bit is set. A gate that nothing has decided is a gate whose value is open |
| Lane | A port's lane: the i-th position the port exposes | A lane of a running law: row i, when the table is applied to every row at once |
| State | The bits a structure holds now | One combination of a law's role values, numbered 0 to N − 1 |
| Read | To look at bits without changing them | One way of interpreting bits, as in "a coarse read" |

## Storage

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Position | One numbered place in a structure. It has no name. Its number and what is wired to it are its identity | Not stored. Bit i of every plane belongs to position i |
| Plane | One bit for every position. It is the only storage shape: state, selections, sets of rows and open values are all planes | A run of bits as long as its structure |
| Structure | Positions and the planes over them. It has as many of each as it needs | planes × positions bits, plus the next three rows |
| Constant plane | A bit every position of a structure shares, such as which way the whole structure faces | One bit for the whole structure |
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
| Version, epoch *(host)* | Two counters per structure. Version advances on every change of state, epoch on every change of shape. Nothing in a machine reads them; they let a host tell that something changed | Not saved |

## Connections

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Surface | Anything a law's role can be bound to: a structure, a port, a structure's life, a workspace, or a workspace's gap. Every read and write goes through one | No bits of its own. In a saved wiring: 0, 1 or 2 (structure, port, life) and which one |
| Port | What a structure shows to others: some of its positions, in order, and some of its planes, in order. It carries no unit, type or description | A list of references (the lanes) and a list of plane numbers |
| Lane (of a port) | The i-th position a port exposes. Connections hold lane numbers, so the owner can point a lane somewhere else and every connection follows | One reference |
| Read (copy) | A role its law never changes. Through a port this is a reference: the sender is untouched, and any number of receivers can hold one | No flag. It is a fact about the law's table |
| Written, move | A role its law changes. Writing through a port is allowed only as a move: what leaves there arrives somewhere the same law writes | No flag. The table changes the role, and the law's accounting holds a preserved sum over it |
| Gate, valve *(pattern)* | A bit a move law reads to decide whether flow passes | One bit, in whichever structure owns the decision |
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

## Running

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Wiring | A law bound to rows. A binding per role says which surface and planes; each row says which position each role uses there | A law number, the bindings, and the rows |
| Binding | Where one role reads and writes. Plain: the planes hold a code. Open: one plane per value, set where the value is ruled out | A surface, plane numbers and, for a relation, one bit for open or plain |
| Row | One use of a law: one position per role | A position in the wiring's own structure. Each role's reference is a binary value in that structure's planes |
| Rows | A wiring's rows, as a structure. What is connected to what is therefore state: a law can rewire a row, bring a spare one into use or retire one | An ordinary structure |
| Evidence | What each row did in the last step, kept once a wiring is asked to record | Six planes of the rows, in the order below |
| Changed | The row's bits changed | Evidence plane 0 |
| Unread | The row's bits were not codes | Evidence plane 1 |
| Unadmitted | The law admits nothing for the row's state. For a relation, this is a contradiction | Evidence plane 2 |
| Ambiguous | Several possibilities and nothing to choose: several next states, or a role still open | Evidence plane 3 |
| Skipped | The row touches a position that is not in use, or holds a reference to nowhere. It did not run | Evidence plane 4 |
| Aliased | Two roles met at one bit that one of them writes. The row did not run | Evidence plane 5 |
| Engine | Steps the wirings in their declared order | The order of the wirings. A separate list only when one runs more than once in a step |
| Step, tick | One pass through every wiring | Not saved |
| The two step rules | A transition row takes the next state its table gives. A relation row rules out, for its open roles, every value no admitted tuple supports | Both are lookups in the law's table, across all rows at once |
| Declared order | The order in which wirings were added and rows listed. It decides who goes first when two want the same thing | The order in the saved machine |
| Wave *(host)* | Wirings, or rows, that touch nothing another one writes. They could run at the same moment | Worked out each step. Not stored |
| Machine | An engine and every structure it reaches: what is saved | One run of bits |
| Inflow *(host)* | Units arriving from outside. They enter as units, never as a value written directly | `Unary.Add` shifts one bit into a count |

## What is not settled

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Open value | A value that is not settled yet: what it may still be | One plane per value, set where that value is **ruled out** |
| Ruled out | One thing learned about an open value | One set bit |
| Open, settled, contradiction | Nothing ruled out; all but one ruled out; everything ruled out | For a one-bit value, two planes ("0 is ruled out", "1 is ruled out"): `00` open, `10` the value is 1, `01` the value is 0, `11` contradiction |
| Supports *(host)* | The helper that reads and edits an open value's planes. In reports, "supports" also means the planes themselves | The planes above |
| Sample | A plain, settled value that a relation reads. It is never narrowed | A code in ordinary bits |
| Narrowing | Ruling out the values no admitted combination supports. It never rules out something possible. It can miss a conclusion that needs two rows taken together | Sets bits in ruled-out planes |
| Observe *(pattern)* | Narrow an open value to what a sample reads | `Same` wired from ordinary bits to an open value |
| Forget *(pattern)* | Open a value again. Wired to run every step, it is the passing of time | `Forget` clears the ruled-out planes |
| Enact, act *(pattern)* | Write a settled value into ordinary bits: what was worked out becomes what is | `Enact` wired from an open value to a field |
| Supposition *(pattern)* | Something taken as true to see what follows, kept apart from what is | Bits written in a workspace, or a separate structure (a probe) |
| Gap | Where a workspace and its source disagree: how a prediction missed, or what is left before a goal is reached | Not stored. A gap bit is set where the workspace holds a bit that differs from the source as it is now |
| Plan *(pattern)* | What is possible at several moments ahead | A structure of open values. In the tests, three positions per moment and five planes: three rule out counts 0, 1, 2 and two rule out gate 0, 1 |
| Moment *(pattern)* | One time step in a plan | A group of the plan's positions |
| Goal, aim *(pattern)* | A wanted value, treated as already settled at a later moment. Narrowing then works back to what has to happen now | Ordinary bits, observed into the plan's last moment |
| Giving way *(pattern)* | Lowering a goal that cannot be reached | A unary count, raised by a law that reads the plan's "unadmitted" plane |
| Probe *(pattern)* | A second plan in which one choice is supposed. If it contradicts itself, that choice is ruled out in the real plan | A second plan structure, and one row from its "unadmitted" plane to a ruled-out plane of the real plan |
| Prediction *(pattern)* | What a structure expects next, written before it happens | Bits in the predictor's own planes |
| Judge *(pattern)* | A law that compares a prediction with what happened and moves trust | A row |
| Trust *(pattern)* | How well a predictor has done | A small unary count |
| Attention, focus *(pattern)* | What a row is looking at | A reference in the rows' planes, which a law can move |
| Ladder, carry *(pattern)* | Counts of one thing at several resolutions. A carry turns several fine units into one coarse unit | Two unary counts and a `Convert` law. The weights of its preserved sum are the place values |
| Pair value *(pattern)* | Two values that vary together, held as one value | One open value with a·b values, and a relation from it to each part |
| Plant *(pattern)* | In the tests, the small structure being steered: a source, a gate and a target | Three positions: two planes for a count, one for the gate |

## Workspace *(host)*

A workspace is not saved, and the engine does not manage one. Inside a machine
the same job is done by ordinary structure, such as a plan or a probe.

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Workspace (`Rung`) | A live view of a structure, or of another workspace, that holds only what was changed in it. It is where something is noticed, supposed or attempted | For each plane it has written, three planes: which bits it holds, its values, and the source bits they replaced |
| Copy on write | Writing a position copies that whole position in. Everything else reads the source as it is now | As above. Positions never written cost nothing |
| Selection | What a workspace attends to. It makes no claim about what it leaves out | A short list of layers: an area plane or "all", and one keep-or-ignore bit each |
| Not selected | The nearest thing to nothing: no claim either way. A zero is different: it is a sample that keeps its place | Absence from the selection |
| Reach, selection by use | A row that touches something selected brings in what else it touches, up to a few steps from what was asked for | A unary count per position: steps left |
| Introduced, candidate | A new position that exists only in the workspace until it is committed | One reference per introduced position: 0, or s + 1 once adopted as source position s |
| Changed together | Changes one row made together, such as both ends of a move. They commit or revert together | One parent reference per position |
| Stale | The source moved under something the workspace changed. A stale commit writes nothing | Found by comparing the kept source bits with the source |
| Commit, revert, drop | Write the changes into the source; hand positions back to the live source; throw the workspace away | No bits of their own |

## Measure

| Term | What it means | How it is in bits |
| --- | --- | --- |
| ODV word | Three bits about a quantity, at one read's resolution | V is plane 0, D is plane 1, O is constant plane 2 |
| O, orientation | Which way this reader calls positive | One bit for the whole structure |
| D, direction | Positive or negative, within that orientation | One bit per position |
| V | Whether any magnitude is detectable at this resolution. V = 1 is not the number one | One bit per position |
| Undetectable | V = 0: a sample below this read's resolution. It is still an operand, and it is not nothing | V clear, D and O kept |
| Detection, exact zero | Two rules for adding words. Under detection, the default, two undetectable amounts may add up to a detectable one. Under exact zero, undetectable is a hidden zero; a reader has to ask for it | Two relation tables over three words, 512 bits each |

## Learning

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Learned table *(host)* | A transition table worked out from what happened | The counts below |
| Counts | How often each (before, after) pair was seen, how often each before-state was seen, and how many rows could not be read | A structure of N² + N + 1 small unary counts. A count stops at "often enough" |
| Witness *(host)* | To show the table one thing that happened | One unit into two counts |
| Admitted, unsupported, unknown | Seen often enough; its before-state was seen often enough and never led there; not seen enough to say | Read from the counts. "Admitted" is one plane of them |
| Crystallize *(host)* | Make a law from the admitted table once nothing is unknown. The law is checked against its accounting like any other | A new law table |

## Saved form and names

| Term | What it means | How it is in bits |
| --- | --- | --- |
| Transport *(host)* | Saving a machine as bits, and restoring it without the code that built it | One run of bits: structures, laws, wirings, then the order where needed. No names, headers or fixed widths |
| Number | Any whole number in the saved form | The Elias gamma code of n + 1: 0 is `1`, 1 is `010`, 2 is `011`, 3 is `00100` |
| Machine size | How many bits a saved machine takes, split into structures, laws and wirings | `Transport.Measure` |
| Lens *(host)* | Names and notation for people. The encoding never reads it | Outside the encoding |

## Kernel words that are not parts here

- **Tension** is not built. Units entering from outside and moving through laws
  stand in for it.
- **Residue** has no type. What a move could not take stays where it was, and
  what a coarse read drops is bounded by the relation that did the read.
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

- Saving a regular table by the rule that makes it, or as smaller laws wired
  together. The saved form stores the bits, except that a transition with one
  next state per state is saved as that list.
- A table held in planes, so that a machine can change its own rules.
- Building a ladder for a wide operation without wiring it by hand.

A Boolean table is not what an operation means. It relates bits, and what
those bits stand for comes from what is wired to them: presence, a selection,
a gate. AND over two presence bits is the read "both are here".
