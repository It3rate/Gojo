# Connected uses and construction history

This group reuses the native core to describe two separate things: **how a
structure was constructed**, and **what a later use permits to flow through it**.
A continuous corner and a later attached bar can look alike while retaining
different histories. Neither history alone authorizes material to move.

## RoutedPassage

`RoutedPassage` connects actual sites into paths, exposes selected sites through
ports, and applies a finite permission law to packet movement. Its grammar is:

```text
edge         = actual site -> actual site
port         = actual site -> exposed boundary slot
edge use     = actual edge -> (from site -> to site)
route        = actual input port -> actual output port
path         = actual route -> Closed(ordered edge uses)
history      = Closed(strokes of ordered edge uses)
permission   = (actual input port, control) -> possible routes
capacity     = port -> Closed(storage sites)
```

`RoutedPassage.Port(site)` constructs the exposed relation. Including it in the
owner's port inventory makes that site available to this protocol; it does not
expose every other site or supply a route automatically. A path must visit
connected sites in order and terminate at its actual destination port. Shared
tick identity can establish connection; coordinate coincidence cannot.

The finite law is structure, evaluated by `Native.FiniteLaw`. Packet ownership
and receiver capacity use `Native.QualitativePacketTransport`. A closed,
unambiguous permitted action can move packets. If two possible controls both
permit that action, it can still move. If one permits movement and the other
stops it, the result remains unresolved and material stays where it is. Merely
finding one possible route in a union is insufficient.

Movement is an atomic packet transfer between exposed ports. It is not a
simulation of speed, collisions or flow at every intermediate site. The witness
retains candidates, accepted and blocked packets, explicit remainder,
unresolved premises, before/after state and the transfer receipt. Reports such
as `moved` or `choice` summarize this structure; they do not select behavior.

## ConstructionHistory

`ConstructionHistory.Read` reads an ordered construction:

```text
construction = (curve frame -> Closed(actual sections)) -> Closed(strokes)
stroke       = Closed(ordered visits)
visit        = actual section -> ((from site -> to site) -> ordered control words)
placement    = ((target stroke -> earlier source stroke) ->
                (earlier landmark -> target start)) -> offset word
```

The reader keeps actual reference evidence for starts, ends and visits. It can
distinguish a continuous turn within a stroke from returning to an existing
endpoint, leaving a branch from an earlier interior landmark, or arriving at
that landmark and stopping. It can also read a declared offset placement of a
later stroke from an earlier landmark. Future strokes cannot supply evidence
for an earlier placement.

These distinctions depend on the supplied history and frame. They are not
recovered uniquely from the finished silhouette. The caller still supplies the
actual landmarks and visits; this reader does not infer hidden intersections,
choose a handwriting convention or build a flow policy from a glyph label.

The result's `Surface` retains native chronological event rows and unresolved
premises. Strings such as `endpoint-attachment`, `outward-branch`, and
`stop-at-interior` are human-facing summaries of those rows, not new source
tags or a vocabulary dispatch table.

## Identity, transport and tests

The shared [ParticipantIdentity](../ParticipantIdentity.cs) helper compares
actual participants. Reboxed locations with the same carrier identity and
shared tick are the same site. Two independent ticks at equal coordinates are
not. Its comparer remains stable when a shared tick moves.

[CompositionTests](../../../Tests/Gojo.Encoding.Core.Tests/CompositionTests.cs)
exercise port exposure, uncertain permission, packet ownership, disconnected
routes, shared tick identity, chronological attachment/branch/stop reads and
future-landmark refusal. Numeric graph round trips check that construction
reads survive transport and that a restored passage definition can instantiate
a fresh local runtime. That latter test is not a claim of live runtime resume.

## Provenance

Promoted on 10 November 2026 from
`experiments/QualitativeFrieze/Composition/GlyphJointUsage.cs` and
`GlyphConstructionHistory.cs`. The source experiment remains intact.

- Only the generic runtime portion of `GlyphJointUsage` was copied and renamed
  `RoutedPassage`; glyph charts and fixtures remain experimental.
- `GlyphConstructionHistory` became `ConstructionHistory` and now depends on
  the promoted native curve/frame implementations.
- A small explicit port builder was added. Path-site comparisons use shared
  participant identity rather than requiring the same boxed location object.
- The definition reader rejects a duplicated storage site within one capacity
  row, matching the transport contract. Capacity shared across different ports
  remains valid.
- The identity helper was extracted from the experiment's `FieldNative`
  utilities; unrelated field construction was not copied.

These are optional higher-level uses of connected paths. They do not require
every primitive port to carry history, permission tables or curve coordinates.
