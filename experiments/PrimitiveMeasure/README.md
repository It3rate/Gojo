# Three bits, before a yardstick

Round 2, 30 October 2026. This replaces the rejected primitive-runtime experiment.
The starting object is simply **orientation, direction, zero/nonzero**. The word
contains no identifiers, connections, carriers, units, receipts or hidden magnitudes.
The current Gojo kernel is unchanged. This is an exploratory lower-level encoding,
following the user's corrected scope rather than the earlier runtime handoff.

```text
O D V
│ │ └─ 0: zero             1: nonzero, magnitude unspecified
│ └─── 0: positive         1: negative, within this orientation
└───── 0: positive-right   1: positive-left
```

`V=1` does **not** mean the quantity one. Direction and orientation remain separate
even at zero. `?` below means some nonzero magnitude, not an unknown zero/nonzero bit.

| Word | Orientation | Direction | Value |
| --- | --- | --- | --- |
| 000 | positive-right | positive | zero |
| 001 | positive-right | positive | nonzero |
| 010 | positive-right | negative | zero |
| 011 | positive-right | negative | nonzero |
| 100 | positive-left | positive | zero |
| 101 | positive-left | positive | nonzero |
| 110 | positive-left | negative | zero |
| 111 | positive-left | negative | nonzero |

The code operates directly on these bits. A host byte holds a word in its low
three bits; packing stores successive words at bit offsets `0, 3, 6, ...`, low
bit first. The eight words above pack as hexadecimal **88 C6 FA**: exactly three
bytes. Position in the stream supplies order. There is no header or per-word
metadata; the caller supplies the word count when the final byte has padding.

## What can be read

Within one orientation:

```text
negative nonzero < zero < positive nonzero
```

Identical words are equal at this perspective. The two direction-marked zeros
have distinct encodings but compare equal in this magnitude read. Positive
direction is therefore greater than or equal to negative direction; both empty
is the equality case. Presence makes a positive value greater, and a negative
value smaller, than its corresponding zero.

This comparison cannot distinguish two positive nonzero magnitudes, or two
negative nonzero magnitudes. Equal encodings do not assert equal hidden amounts.
There is no stored hidden amount to consult.

As explicitly selected by the user, different orientations remain incomparable
until reoriented. Reorientation flips **O and D together**, retaining the
represented sense in the opposite orientation: `001 -> 111`. Reversing direction
alone flips **D**: `001 -> 011`. Both preserve V, including the zero cases.
This supplies no scale conversion or translation between different origins.

## The first addition

For a chosen common orientation, the full table is:

| + | +0 | +? | −0 | −? |
| --- | --- | --- | --- | --- |
| **+0** | +0 | +? | {+0, −0} | −? |
| **+?** | +? | +? | +? | {+?, −?, +0, −0} |
| **−0** | {+0, −0} | +? | −0 | −? |
| **−?** | −? | {+?, −?, +0, −0} | −? | −? |

Same-direction nonzero addition stays nonzero: this read cannot expose increased
magnitude. Zero adds no magnitude. Opposing nonzero inputs leave negative, zero,
and positive all possible because their relative magnitudes are unavailable.

For zero results this experiment keeps a settled direction when both operands
agree, and permits both zero encodings when they do not. That is a conservative
starting rule, not an invented preferred positive zero. The two zero words still
describe one zero magnitude read.

`Add` is a **prediction**, returning a plain list of possible three-bit answers.
It does not execute a physical merge. This list is the result of the question,
not an extra field attached to each value. Across orientations
it returns no selected answer; `Compare` returns no selected ordering. Neither
case manufactures a zero. There is no unknown sentinel squeezed into the eight
concrete words.

Subtraction reverses the second direction and applies the same addition. Thus
two read-equal `+?` words do **not** justify exact cancellation: `+? − +?` remains
uncertain. This is not ordinary scalar equality with unrestricted substitution.
The words have forgotten the information that would establish cancellation.

## One flow, two opposed reads

The next experiment adds a shared read without adding bits to the word:

```text
negative read <- [one O D V word] -> positive read
```

`ReadSide(shared, negative)` derives occupancy in the chosen side of the word's
orientation. For `001`, the positive read is `001` and the negative read is `010`:
positive nonzero, negative zero. For `011`, these become `000` and `011`.
At zero both sides read empty. The shared word retains its zero direction even
though these side reads do not distinguish it.

Only the shared word owns state. Every fresh side read refers to that one word,
so the two sides cannot acquire independently drifting amounts. A saved read
is a past observation, not a second live side. No identity field is needed for
this arrangement. Shared storage supplies the correspondence while it persists;
it does not imply a constant value or recognition after separation and recontact.
This exclusivity is the selected one-flow law, not a claim that every connected
Gojo structure must collapse to one scalar.

## Prediction, joining, and actual flow

Four different things are now exercised:

1. **Structure:** one shared word and its opposed read surfaces.
2. **Joining:** consuming two independent flows into one flow.
3. **Prediction:** what their coarse words permit us to say before joining.
4. **Execution:** actual settlement, followed by an observation of its result.

The same coarse inputs can have different actual outcomes:

| Coarse inputs | Prediction | Demonstration world | Actual observation |
| --- | --- | --- | --- |
| +? and -? | negative, zero, or positive | +5 joins -2 | +? |
| +? and -? | negative, zero, or positive | +2 joins -2 | zero |
| +? and -? | negative, zero, or positive | +2 joins -5 | -? |

`FineFlowFixture` is explicitly separate demonstration apparatus. Its signed
integers stand in for finer physical material in one selected local,
positive-right basis. `Join` adds those amounts and empties the two consumed
stores, leaving one returned amount. The three-bit predictor receives only
observations, never those integers. There is no fine magnitude stored beside
an ODV word, no random choice among predictions, and no assumption that read-equal
amounts cancel. The apparatus chooses positive direction when observing exact
zero; scalar zero does not establish that direction. It therefore supplies
positive-zero inputs, rather than pretending to realize every zero-direction code.

This fixture selects immediate finite additive settlement. That makes the
actual sign definite in each run. General connectedness alone does not guarantee
settlement; delayed or unsettled flow is outside this first experiment.
Host integer overflow refuses the operation before consuming anything.

A closed computation with only the two coarse words cannot recover the missing
relative magnitude. Actual settlement must come from a participating world or a
later, richer flow encoding. The integer apparatus demonstrates that boundary;
it is not the proposed primitive runtime or a universal yardstick.

## A first conditional

The smallest additional connection shape here is three openings in fixed order:

```text
                       negative outlet (position 0)
settled shared flow -> zero outlet     (position 1)
                       positive outlet (position 2)
```

`SignPassage` stores one presence bit per opening: bit 0 negative, bit 1 zero,
bit 2 positive. Thus `111` permits every sign, `100` permits only positive,
and `000` closes every opening. These are **connection bits**, not another ODV
measure word. Their interpretation comes from this declared local layout.
There are no destination identifiers or instruction tags. This is an exploratory
fixed layout, not a general network encoding or a new fundamental kind of connector.

`Select` reads one settled word and selects its open position. Both zero
direction codes select the same zero position. Closed means no selected passage,
not a zero result. Sign positions are relative to the observed orientation;
no comparison or connection across orientations is inferred.

The demonstration's routing layout is `[flow, negative, zero, positive]`.
Execution moves the actual amount into exactly one matching open outlet and
empties the source. A blocked flow stays at its source. Unselected outlets
remain unchanged. Signed participation is conserved in the fixture's chosen
basis; prediction neither moves nor duplicates it. A passage checks the incoming
sign; settlement into an already occupied receiver can yield a different sign.

Prediction can report all possible branches before a merge, but it executes
none of them. After the merge, only the actual observation selects a branch.
Removing the negative opening holds a negative result; reopening it releases
that same held flow. The opening does not decide which sign the merge produces.
This is early conditional behavior: the layout constrains what can happen, and
the runtime flow determines which available path is used.

The zero branch needs particular care. An observed zero can select its position,
but transports zero material. The returned selection demonstrates the read;
it does not create a nonzero pulse. The caller invokes this read after settlement.
Autonomous detection of completion, distinguishing no observation from an actual
zero, and powering an action on zero need further structure. Missing settlement
must never be supplied as a zero word.

## A passage can resemble a bond

```text
[independent store] -> [sign openings] -> [independent store]
```

`FineFlowFixture.Transfer` uses the same openings to move all permitted source
flow into its receiver. An unrestricted passage can leave `[+5, -2]` as `[0, +3]`,
matching the net outcome of a join. The two stores still exist independently.
Later local inputs and a closed passage can leave both occupied, even oppositely.
A passage that usually stays open can often behave like one flow; that observed
behavior does not supply the guarantee of a single shared store.

For this foothold, the demonstration stops using the consumed input slots and
makes subsequent opposed reads from one result. `Join` clears those slots; it
does not prevent their reuse or enforce ownership. The guarantee comes from
the opposed reads addressing one shared word. Communication preserves two
stores and an intervening opening. Later
work can build these openings from ordinary connected structures, rather than
giving connectors an unrelated set of primitive capabilities.

## Where this stops

We can communicate a local distinction, preserve orientation and direction,
compare at this coarse perspective, and combine some inputs. We cannot recover
relative nonzero amounts or predict which opposing input dominates. Nothing here
claims to model actual bacterial communication. That analogy motivates asking
what a receiver can learn without a shared yardstick. The new demonstration
separates coarse prediction, actual flow, shared state, and filtered communication;
additional sensing is not hidden in the bits.

The historical Core4 `Measure`/`Relation` sources and current Gojo core informed
the separation of raw magnitude, ordered perspective and inversion. Their
carrier/graph machinery has not been imported. Multiplication, units, refinement,
general connective structures and a primitive runtime remain later work.

Run the encodings, predictions, actual merges, opposed reads and passages:

```powershell
dotnet run --project experiments/PrimitiveMeasure
dotnet test Tests/PrimitiveMeasure.Tests/PrimitiveMeasure.Tests.csproj
```

The focused tests cover every operand pair, preserved zero encodings, explicit
reorientation, uncertain cancellation, packing, shared-side exclusivity, the
separation of prediction from actual settlement, conditional openings, material
conservation, blocked flow, and refusal before overflow can consume an input.
Finer integers occur only in the explicitly named demonstration apparatus and
tests; the primitive word and its predictor still contain no such values.

Verification on 30 October 2026: all 18 focused tests pass, the console
demonstration runs successfully, and `dotnet test Gojo.slnx` passes.
