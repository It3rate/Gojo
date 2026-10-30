# Three bits, before a yardstick

Round 2, 30 October 2026. This replaces the rejected primitive-runtime experiment.
The starting object is simply **orientation, direction, zero/nonzero**. There are
no identifiers, connections, carriers, units, receipts or hidden magnitudes.
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

`Add` returns a plain list of possible three-bit answers. This list is the result
of the question, not an extra field attached to each value. Across orientations
it returns no selected answer; `Compare` returns no selected ordering. Neither
case manufactures a zero. There is no unknown sentinel squeezed into the eight
concrete words.

Subtraction reverses the second direction and applies the same addition. Thus
two read-equal `+?` words do **not** justify exact cancellation: `+? − +?` remains
uncertain. This is not ordinary scalar equality with unrestricted substitution.
The words have forgotten the information that would establish cancellation.

## Where this stops

We can communicate a local distinction, preserve orientation and direction,
compare at this coarse perspective, and combine some inputs. We cannot recover
relative nonzero amounts or decide which opposing input dominates. Nothing here
claims to model actual bacterial communication. That analogy motivates asking
what a receiver can learn without a shared yardstick; exchanges and additional
sensing are later experiments, not hidden mechanisms in these bits.

The historical Core4 `Measure`/`Relation` sources and current Gojo core informed
the separation of raw magnitude, ordered perspective and inversion. Their
carrier/graph machinery has not been imported. Multiplication, units, refinement,
network communication and a runtime can wait until a concrete need adds one
more distinction.

Run the eight encodings, packed bytes and addition examples:

```powershell
dotnet run --project experiments/PrimitiveMeasure
dotnet test Tests/PrimitiveMeasure.Tests/PrimitiveMeasure.Tests.csproj
```

The eight focused tests cover every operand pair, preserved zero encodings,
explicit reorientation, uncertain cancellation, and packing across byte
boundaries. Integer magnitudes appear only in independent test examples of
possible sign outcomes; the implementation stores and computes no such values.

Verification on 30 October 2026: all eight focused tests pass;
`dotnet test Gojo.slnx` passes 5,564 tests across 14 projects, with no failures
or skipped tests. The console example also runs successfully.
