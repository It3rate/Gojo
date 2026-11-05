# One template, many local instances

This experiment asks whether a small connected construction can be stored once,
with many instances containing only its variable bits. The construction supplies
the fixed faces, shared joints and shared value roles. Width groups are ordinary
collections that appear when entries are appended. Their lengths describe storage;
they are not extra measures inside the modeled construction.

The intended structure, before implementation, is:

```text
local family
  -> fixed/free O/D field positions
  -> ordered connections (start site -> end site) -> value role
  -> populated width groups -> packed instances

same site reference within one instance -> same joint
same value reference within one instance -> same stored amount
reverse read -> opposite ordered endpoints, original amount preserved
bare value role -> no endpoint claim
```

Distinct instances instantiate distinct local sites and amounts. Reusing their
template does not merge their origins or establish a common physical calibration.
A closed connection cycle does not by itself establish a square or regular polygon.
An equal-looking coarse read does not establish equal hidden magnitudes.

The binary transport adapter may store lengths to delimit arrays and padding.
Those lengths are generated from the collections; callers never annotate a
conceptual holder with a separately maintained count. This first experiment is
append-only and does not add a sparse tree, settlement law, or automatic promotion.

## What to try

[Open the family inspector](https://gojo.inklingai.ca/encoding/explorer/?layer=40).
Choose **Bare signed amount** first: it has a direction and participation, but no
endpoint. Then compare the two four-cycle examples. They have the same ordered
connections; one owns four amounts and the other refers to one shared amount four
times. Choose the reverse view to see the same sites and amounts traversed in the
opposite order. Change the entry to inspect other stored values.

The diagram supplies convenient positions for inspection. Even spacing on the
screen is not evidence of equal distances. A four-cycle is not yet a square:
its geometry would also need the appropriate reach and turning relations.

## The three parts of a family

| Part | Stored here | What it establishes |
| --- | --- | --- |
| Field positions | Pairs of O-group and D-group selectors | Which bits are constant and which belong to each entry |
| Connections | Ordered `[start site, end site, value field]` rows | Shared endpoints and shared value ownership |
| Width groups | Packed entries, created when appended | Only the variable fields of each instance |

There is no integer, star, polygon or complex-number tag in this grammar. The
example names are captions. The reader uses the connected template and field
positions. A field can exist without any connection, preserving bare participation
before an endpoint is known.

The fixed O/D selectors use the existing positional grammar:

| O group | D group | Entry field |
| --- | --- | --- |
| 0 or 1 | 0 or 1 | Value bits only; O and D inherited |
| 0 or 1 | 2 | Value bits followed by owned D |
| 2 | 0 | Value bits followed by owned D and O |

O group 0 is positive-right, group 1 positive-left. D group 0 is positive, group
1 negative. Group 2 releases that constant into the entry. These are storage
choices, not additional motion instructions.

Each field description occupies four template bits: O group in the low two,
D group in the next two. Link references use the smallest common positive bit
width sufficient for this template. A link stores three references in order,
low bit first within each reference. Template bits are stored once per family.

## What connects to what

| Example | Fields | Ordered connections |
| --- | --- | --- |
| Bare signed amount | One owned-direction field | None |
| Independent opposed pair | One fixed positive and one fixed negative field | `[0,1,0] [0,2,1]` |
| Shared-tip pair | Two fields with opposite fixed orientations | `[0,2,0] [1,2,1]` |
| Three-arm hub | Three independent fixed-face fields | `[0,1,0] [0,2,1] [0,3,2]` |
| Independent four-cycle | Four independent fixed-face fields | `[0,1,0] [1,2,1] [2,3,2] [3,0,3]` |
| Shared-value four-cycle | One fixed-face field | `[0,1,0] [1,2,0] [2,3,0] [3,0,0]` |

References are positions within one instance. Repeated site 0 in a hub is its
common joint. Site 0 in the next instance belongs to that next instance; it is not
the same joint. The same scoping applies to value fields.

The shared-tip pair does not yet establish a fixed whole or complementary
football-field distances. The hub does not assert equal angles or a spatial
dimension. Those require further connections.

Four references to field 0 occupy one value field. They allow four observations
of the same stored amount; they do not create four reservoirs of material.
Conversely, four equal-looking independent fields still occupy four fields and
can differ beneath the coarse read.

## Reverse reads and zero participation

```text
stored:       0 -> 1 -> 2 -> 3 -> 0
reverse read: 0 -> 3 -> 2 -> 1 -> 0
```

The reverse read reverses row order and exchanges each row's start and end.
It retains the original site and value references and leaves all stored O/D/V
bits untouched. This is a borrowed view of the construction. Reorienting an ODV
word, negating a value, and materially reversing flow remain separate operations.
A reversed hub is still a hub; reversing its row sequence does not make it a path.

The closure reader checks actual ordered endpoint correspondence, including the
return to the first site. It reports a topological closed walk. It neither checks
pixel overlap nor claims a nondegenerate geometric polygon.

A zero-valued field does not delete a connection or identify its two endpoints.
A nonzero self-loop can also retain traversal participation despite having one
start/end site. These distinctions preserve potential structure.

For a shared signed field, the existing positive and negative side reads expose
at most one participating side. Both are empty at zero. The stored direction at
zero is retained. These observations have no separate amount storage, and the
selected side is an explicit read context, not inferred from endpoint order.

## Equal-looking, chosen equal, and shared

There are no per-value confidence tags added here. The evidence comes from the
structure and selected read:

1. Two independent nonzero fields can have the same coarse ODV word. Their hidden
   amounts remain unresolved relative to one another.
2. A diagram can choose equal spacing for those fields. That is a representative
   placement, and supplies no evidence back to the source.
3. Two connections can refer to the same field. They now observe the same stored
   source; equality of that source does not have to be inferred from matching bits.

This pass implements the first and third distinctions and labels the second in
the inspector. It does not add a general uncertain-placement or equality-settlement
solver. Shared state also does not by itself establish geometric units or angles.

## Width, seeking, and collection boundaries

With five value bits, one owned-direction field occupies six bits:

```text
ordinary written order: D vvvvv
physical low-first order: v0 v1 v2 v3 v4 D
```

Two independent signed fields occupy twelve bits. Four fixed-face independent
fields occupy twenty bits; four uses of one fixed-face field occupy five.
Each group's entry stride is calculated from its template and value width:

```text
entry start = payload start + entry index * entry stride
```

Rows can cross byte boundaries and exceed a machine word. Each individual field
is currently limited to 64 bits including its owned O/D bits. Additional bits are
capacity, not automatically finer measurement, equal subdivisions or binary
place-value semantics.

Width groups are ordinary owner-local collections. Appending to a new width
creates that group; appending another entry extends its packed sequence. The API
does not ask its caller to update a second count on a conceptual holder. The host
implementation maintains collection bounds so a read cannot consume final padding.

The only live entries in this initial experiment are the appended entries. There
is no deletion, free-list, liveness bitmap or compaction in this new table yet.
Those remain separate storage work from the existing positional-bucket experiment.
Likewise, many slots do not imply high conceptual importance or strong energy:
either would require a suitable read and basis.

## Transport framing is an adapter

An external byte stream needs some way to distinguish valid zero entries from
padding and the end of a collection. This codec uses lengths. A different medium
could supply physical boundaries or another framing convention.

The executable V1 layout is:

```text
version byte
field count, link count                 unsigned variable-length integers
reference-width byte
packed field selectors
packed link triples
width-group count                       unsigned variable-length integer
for each populated width, in ascending order:
    value-width byte
    entry count                         unsigned variable-length integer
    packed entries
```

Each count is calculated from the collection when encoding. It is storage framing,
not a measured quantity in the represented world. Site extent is derived from the
connections; it is not separately serialized. Final padding is zero and is not an
extra value. The codec rejects truncation, nonzero padding and references outside
the supplied template.

For a concrete independent decoding example, these ten bytes describe one
fixed-positive-right field, one `0 -> 1` connection using that field, and three
one-bit values `1, 0, 1`:

```text
01 | 01 01 | 01 | 00 | 02 | 01 | 01 03 | 05
```

| Bytes | Read |
| --- | --- |
| `01` | Transport version |
| `01 01` | One field, one link |
| `01` | One bit per local reference |
| `00` | Fixed O group 0, fixed D group 0 |
| `02` | Low-first references `[0,1,0]` |
| `01` | One populated width group |
| `01 03` | One value bit, three entries |
| `05` | Low-first values `1,0,1`; five padding bits |

This literal byte example is tested without calling its original builder. The
restored family can be read, reversed and extended. Its sites are local to the
restored owner, not magically identical to the original owner's sites.

This deliberately small codec has visible overhead: three useful payload bits
in the example occupy a ten-byte transport. It is not a claim of optimal wire
compression. Its framing and shared template are paid once per family/group;
new entries add their field bits, with byte padding only at the end. The in-memory
C# arrays and objects have further overhead beyond the reported payloads.

## Reproduce and inspect

```powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --families experiments/PrimitiveMeasure/data/families
dotnet test Tests/PrimitiveMeasure.Tests -c Release --filter FullyQualifiedName~Family
```

The six authored families each contain two widths and three entries per width.
The export decodes every binary file without the example builder and records its
reads. [All 36 entry reads](data/families/family-rows.csv) and
[template, payload and transport sizes](data/families/family-sizes.csv) are available.
Native files: [bare signed](data/families/family-1.bin),
[opposed pair](data/families/family-2.bin), [shared tips](data/families/family-3.bin),
[hub](data/families/family-4.bin), [independent cycle](data/families/family-5.bin),
[shared-value cycle](data/families/family-6.bin).

The tests cover local identity, aliases versus matching coarse values, reversal,
zero topology, constants, unaligned and wide rows, append validation, independent
decoding and malformed framing. The implementation does not yet settle unlike
units into a family, discover matching templates, establish angles, or encode
arbitrary physical flow laws. A future sparse lookup can index the same templates
without becoming the source of their meaning.
