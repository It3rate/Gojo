# Gojo Workspace

`Gojo.Workspace` is reserved for bounded, experimental Gojo structures. A
workspace is not the whole world and not a detached DTO snapshot. It is a
selected, inspectable scope that can run local structure with controlled access
to live world structure.

## Core Shape

```text
WorkspaceRoot
  + quoted membership
  + active relation group
  + proxy carriers
  + boundary ports
  + supplied input witnesses
  + provenance links
  + residue / unresolved frontier
```

Quoted membership says what the workspace can inspect. Active relations say
what may settle or compute inside the workspace. These are deliberately
separate: including a relation for context should not automatically reconnect
the whole world into the local settlement problem.

## Proxy Structure

Workspace computation should usually happen through proxy carriers and copied
or quoted relations. The proxy keeps local experiments from mutating world
state, while provenance links keep the proxy from becoming an amnesiac copy.

```text
world heat carrier
  -> workspace heat proxy
  -> provenance link back to world heat
```

Boundary ports control when a workspace reads from or writes toward the world.
They also preserve residue when the workspace imports only a coarse or partial
view.

## Resolution And Witnesses

Workspace inputs must record what was admitted, not merely what value a carrier
currently holds. This is how Gojo distinguishes:

```text
hundreds = 3
```

from:

```text
hundreds = 3, tens = 0, ones = 0
```

The first is a coarse supplied witness: about `300`, with lower detail outside
the workspace boundary. The second is exact at ones resolution because the lower
slots participated explicitly.

Coarse participation can use `UnitShape.ResolutionOf(source, ticksPerTick)`.
For example, a hundreds proxy can have a unit shape whose one tick represents
100 ticks of the source unit. In a workspace this may be temporary and dynamic;
in core/world structure it may harden into a stable coarse carrier when that is
the concept being represented.

## Flexible Boundaries

Workspace boundaries should be soft in the practical sense: they are explicit
but adjustable. Boundary residue and unresolved frontier structure are signals
that the workspace may need to grow, shrink, or change resolution.

```text
too much residue at lower value slots -> import finer resolution
unused detail                         -> collapse to a coarser proxy
unresolved relation support           -> include nearby support structure
poor metaphor fit                     -> remove or remap source structure
```

## Learning And Hardening

A workspace is a place to try structure safely:

```text
select / proxy / simulate / inspect residue / adjust / harden
```

If a workspace structure repeatedly works, parts of it can be hardened back
into world structure. That hardening path is the likely core of learning, but
it should remain explicit Gojo structure: selected relations, provenance,
boundary residue, and the reason a local pattern became durable should stay
inspectable.

## Metaphor

Metaphor can use the same mechanism. A source structure is selected at low
resolution, proxied into a target workspace, and allowed to fit or fail.

```text
source rhythm / turn-taking / recovery
  -> target conversation workspace
  -> residue shows where the metaphor stops fitting
```

Useful residue is not an error. It is information about the boundary between
the sampled structure and the target context.
