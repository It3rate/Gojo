# Gojo numeric graph format, version 1

Status: implemented integer-table transport, September 6, 2026.

[`GojoNumericFormat`](../Core/Serialization/GojoNumericFormat.cs) provides:

```csharp
byte[] bytes = GojoNumericFormat.Encode(rootA, rootB);
GojoEncodedSnapshot tables = GojoNumericFormat.ReadSnapshot(bytes);
IElement[] independentRoots = GojoNumericFormat.Decode(bytes);
```

`GojoEncodedSnapshot.CaptureGraph(roots)` captures ordered roots, including
repetitions, and all transitively reachable carrier attachments. `Encode(tables)`
and `Restore(tables)` require that complete-graph contract. Legacy `Capture`
and `CaptureWorld` remain diagnostic views and are refused for restoration.

## What is preserved

- Ordered relation endpoints and repeated relation references.
- Concrete carrier identity within the restored world, native unit shape,
  value, signed tension and cached opposed-face pairs.
- Tick sharing, including live `ValueSlot` references and shared coordinates
  on different carriers. Coordinate-equal independent ticks stay independent.
- Placeholder slot identity and opposed faces, including repeated open slots.
- Native unit algebra, descriptor provenance and their captured coordinates.
- Attachment ownership, not merely a relation that looks like an active route.
- Retained transforms, timelines, residue and witnesses as ordinary graphs.
- Carrier/unit reference cycles and ordinary relation cycles without flattening.

Loading allocates and links the whole graph before exposing roots. It does not
call `Set`, `Attach`, or settlement. A detached carry recipe remains detached;
a pending borrow remains pending. On subsequent input, restored attachments can
settle normally.

Each restored concrete carrier, relation, tick and unit receives a **fresh
runtime ID**. Allocation follows relative source ID order within each kind,
preserving ordering used by today's settlement. Source IDs remain readable in
the transport tables. They are not installed as process-global IDs and are not
a persistent distributed naming scheme. The wildcard carrier remains the shared
unbound sentinel and cannot transport physical state.

## Physical layout

All multibyte integers use little-endian order. A reference is one table-kind
byte plus a signed 32-bit zero-based index.

The header is four ASCII bytes `GOJO`, signed 32-bit version `1`, then seven
signed 32-bit table counts, in the following order. Rows have implicit sequential
indices.

| Table | Row fields, in order |
| --- | --- |
| Roots | Element reference |
| Carriers | source ID u64; inverse and unknown flags (one byte each); value u64; tension i64; unit index i32; opposite index i32 (-1 absent); value-tick index i32; attachment count i32; relation indices i32[] |
| Tick refs | source ID u64; ticks i64 |
| Locations | carrier index i32; tick-ref index i32 |
| Relations | source ID u64; left reference; right reference |
| Unit shapes | source ID u64; surface reference; descriptor count i32; descriptor references |
| Placeholders | packet-local slot index i32; inverse flag (one byte) |

Kind values are 1 carrier, 2 tick, 3 location, 4 relation, 5 unit-table selector,
6 placeholder. Unit shapes are carrier promises, not `IElement` roots:
element references of kind 5 are therefore refused in version 1. Kind 0
diagnostic absence is also refused. The selectors address physical tables;
they are not operation tags or a second semantic grammar.

Although a carrier value field is u64, its shared value tick restricts current
valid state to 0 through `long.MaxValue`. Coordinates and tension are i64.
Version 1 provides explicit bounds, not arbitrary precision or compression.

## Validation and limits

Readers reject unsupported versions/kinds, invalid flags, counts, indices,
truncation, trailing data, duplicate runtime identities or slot faces, invalid
value sites, inconsistent opposite links/promises, and mutable aliases into
frozen units. Stable unit ticks must be grounded in locations. Cyclic relation
structure inside a frozen unit is refused.

Transport and primitive carrier/location traversal support ordinary relation
cycles. This does **not** make every existing recursive algebra reader,
formatter, equality implementation or dual operation a general cyclic-graph
algorithm. Use the format as a graph boundary, not permission to execute an
arbitrary cyclic relation through every legacy reader. Deep ordinary ordered
groups have a dedicated 12,000-participant transport/traversal test.

Capture requires a quiescent world. No synchronization, application process
state, callbacks, UI animation closures or external service handles are
serialized. A saved timeline contains the evidence retained at capture time:
live references that already drifted cannot recreate an earlier sample.
Independent packet loads do not share concrete identity automatically; reconnect
through explicit correspondence and import.

This is a versioned graph transport, not canonical text, canonical bytes,
distributed graph identity, semantic migration across changed kernels, or a
lossy learned-memory format. Runtime IDs change after restoration, so byte
equality is not its round-trip criterion.

## Evidence

[Core tests](../../Gojo.Tests/Core/NumericFormatTests.cs) check graph identity,
partial patterns, descriptors, malformed packets, deep groups, cycles, pending
tension and restored live output/carry behavior. [Inventory tests](../../Gojo.Encoding.Tests/NumericMilestoneInventoryTests.cs)
compare every table reference and state field, except intentionally replaced
runtime IDs, across two round trips for all current authored syntax fixtures and
every exported Blocks specimen. Quotient and material syntax tests also execute
after restoration. The gear tests reread every restored rung, contour and shared
pitch identity. These tests establish fidelity of those graphs and behaviors,
not mathematical completeness of every depicted concept.

The [prospective fixture](GojoProspectiveCalibrationFixture.md) also restores a
pending issued forecast, admits a later observation through the restored port,
and retains the assessment's consumed-issue identity on another round trip.
