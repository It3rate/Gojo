# Positional storage and stable ports

Round 4, 30 October 2026. This experiment tests two simple ideas: fixed orientation and direction come
from array positions, and an outside reader can keep the same port while its
value moves inside the owning structure. It does not change Gojo's core.

The next [connection experiments](ConnectionExperiments.md) attach calculated
reads and explicit held samples to these ports while preserving local ownership.

Run the example:

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --ports
```

## The arrays contain no constant header bits

Each structure owns its own `PositionalBuckets`. Their arrangement is fixed:

```text
root[0]                       orientation is 0
  child[0]                    direction is 0
  child[1]                    direction is 1
  child[2]                    each entry owns its direction
root[1]                       orientation is 1
  child[0]                    direction is 0
  child[1]                    direction is 1
  child[2]                    each entry owns its direction
root[2]                       each entry owns orientation and direction
  child[0]                    array position only; it does not fix direction
```

Under each child are buckets selected by **total entry width minus one**.
For example, the three-bit bucket is at index 2. Only a used bucket needs
packed payload storage. This implementation supports total widths up to 64.
Variable orientation with fixed direction is deliberately omitted for now.

| Where an entry lives | What its payload stores | Minimum total width |
| --- | --- | --- |
| Fixed orientation, fixed direction | Value bits only | 1 |
| Fixed orientation, own direction | Value bits, then D | 2 |
| Own orientation and direction | Value bits, then D, then O | 3 |

Value bits occupy the low positions. An owned D is immediately above them;
an owned O is the highest bit. There are no O/D headers before the entries.
For fixed fields, the containing array position supplies the answer instead.

Thus an ordinary entry write cannot turn a fixed positive entry negative:
there is no sign bit in that payload to write. A change of constraint requires
moving to another group. The host still has mutable memory; the private API
protects the arrangement. This is not a claim that RAM itself is immutable.

## Finding entry n

Every entry in one bucket has the same total width `w`, so its first bit is:

```text
bit position = n * w
byte index   = bit position / 8
bit in byte  = bit position % 8
```

These are zero-based indexes. Entry 3 in a three-bit bucket starts at bit 9:
byte 1, bit 1. No preceding value needs to be parsed. An entry can cross a
byte boundary. Unused bits at the end of the final byte are padding.

A private locator is the four-part `SlotAddress`:

```text
(orientation group, direction group, total width, entry index)
```

Its position supplies the omitted meaning. The raw payload by itself may not
say whether it is positive or negative. The locator is local to its owner;
another structure's identical four numbers can name a different value.

## Keeping the same outside attachment

The current experiment uses this actual mapping:

```text
outside connection: (owner reference, port index)
                         |
owner.ports[port index] = binding ordinal
                         |
owner.bindings[binding ordinal] = SlotAddress
                         |
owner's packed bucket and entry
```

The port and binding indexes are positions in this owner's lists. There is no
global ID for every value and no shared table containing all structures.
The owner reference and mapping indexes currently use ordinary host references
and integers. They are not yet a compact, serialized connection format.

The binding is the current mapping used by the port, not a second value store.
Two ports mapped to the same binding follow the same value when it moves.
An outside reader retains its owner and port index; it does not need the
private bucket address or the layout of every other value.

This connection is an explicit **read attachment**. Reading through it does
not merge structures, transfer material, or drain the source.

## One value moves through four layouts

Consider a negative, nonzero value in orientation 0. Give it one binding and
one exposed port, then move it through these locations. The example uses entry
index 0 in each previously empty destination bucket.
The console demonstration attaches a second port to the same binding as well;
both follow every move without changing either outside connection.

| Step | Private address `(O group,D group,width,index)` | Raw entry, high bit first | Meaning supplied by position |
| --- | --- | --- | --- |
| Fixed O and D | `(0,1,1,0)` | `1` | O=0, D=1 |
| Release D | `(0,2,2,0)` | `11` | O=0 |
| Widen value storage | `(0,2,3,0)` | `101` | O=0 |
| Release O too | `(2,0,4,0)` | `0101` | Neither O nor D |

The coarse ODV read is **`011` at every step**. The last two rows store the
value code `01`; D is the next bit, and the last row also stores O=0.
The port index and binding ordinal stay the same. Only the binding's private
address changes.

The extra value bit adds storage capacity. It does not discover a finer amount,
create calibration, or turn the code `1` into a known quantity of one. In this
experiment the coarse read asks only whether the value code is zero or nonzero.

Moves can release constants and add capacity, but cannot change inherited O/D,
reimpose a released constant, or shrink the value field. Releasing O/D needs
room for those bits: a full 64-bit value field cannot release either constant
within this experiment's 64-total-bit limit, even if its current code is small.

Migration runs under the owner's lock: create the replacement representation,
rebind the existing binding, then retire the old slot. Outside reads through
that owner cannot observe the intermediate mapping. This is one logical value
changing representation, not a material transfer between two values.

## Retired storage is not a zero value

Each bucket has a separate allocation-liveness bitmap. It is allocator state,
outside the primitive payload. A live entry whose payload is zero is still a
valid zero value. A retired entry is not readable as a value at all.

Retirement clears its liveness bit but deliberately leaves its old payload
bits dirty. Diagnostic byte dumps can therefore still show the old bits.
They cannot be read as a live value through a retired slot. This experiment
does not reuse slots, compact buckets, implement reference counting, or run a
garbage collector.

Future cleanup must consider local roots such as exposed ports, input ports
even when they have no consumer, and internal references before reclaiming a
value. Cleanup can later be scheduled per owner. Reference-count cycles and
automatic lifetime management remain unresolved here. A stable port preserves
the selected read attachment during these explicit moves; it does not promise
automatic survival through arbitrary restructuring or a change of meaning.
