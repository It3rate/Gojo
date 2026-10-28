# Joint constructions, relaxation and topology changes

Status: implemented exploratory fixture, 28 October 2026. The selected
examples extend the retained construction/negotiation experiment.

## Native shape before implementation

```text
joint definition = G(kinematic transform,
                     G(node role -> coordinate expression),
                     G(ordered edge between actual node roles),
                     G(external role -> internal node role), provenance)
joint instance = G(definition, exposed coordinate-port correspondence,
                   requested paths between external roles,
                   current read, timeline, mechanical instance)
rewrite = TransformSurface.Patch(previous definition, explicit change,
                                 selected topology, carried participation,
                                 replacement, correspondence, new definition,
                                 unresolved obligations, provenance)
node correspondence = G(old role -> new role,
                        old role -> G(new roles),
                        G(old roles) -> new role,
                        retired role -> open, open -> introduced role)
```

The internal graph supplies connectivity. Equal coordinates do not create an
edge or a shared node. External ports expose coordinate observations; several
can read one internal node, or later read distinct nodes after a witnessed
split. Their existing identities remain stable.

Kinematic expressions and response routes determine allowed movement. Names
such as T, Y and L belong to the examples and review. The executor never
selects behaviour from those names.

The rigid motif has a shared reach, an anchored hub, an opposed horizontal
span and a retained contour quarter-turn. Its foot and optional upper arm are
constructed from that turn. Changing the shared reach changes all branches
through the same native dependencies. The fan introduces a shared arm-rise
parameter; the paired corners introduce a shared opposed spread.

## Selected demonstrations

1. A constrained T receives diagonal targets and retains its mismatch.
2. The same T is replaced at identical positions by a three-branch construction
   with a shared arm-rise parameter. It can now accommodate the diagonal targets.
3. A constrained T receives two separating lower targets and cannot follow.
4. An explicit split replaces its common hub and common foot with two pairs of
   distinct sites. Two corner constructions can then move apart.
5. A cross loses an upper branch while the upper coordinate remains remembered.
   An external path request becomes unresolved. Restoring that actual branch
   makes the request readable again.
6. Two corners begin at coincident positions but remain disconnected. Folding
   their hubs and feet creates a shared T without moving the drawing.

The last two examples make topology visible independently of positional error.
A drawing with zero coordinate mismatch can still fail a requested path.

## Local reads and scope

Connectivity uses an explicitly selected undirected read of the retained ordered
edges. A route witness keeps each actual edge and its chosen traversal, so this
read does not discard relation order or redefine primitive topology.

Every pass uses the existing captured-publication negotiation schedule. Numeric
change enters private parameter carriers through signed tension and the retained
construction republishes its outputs. Rewrites are authored proposals applied at
declared rounds. Their introduction, retirement, split, fold and correspondence
are explicit native structure. This fixture does not learn which rewrite to try.

The offline review shows actual geometry, requested targets, the connection
graph, positional mismatch, unresolved paths, and the change at each round.
Its schematic connection diagram is deliberately not a spatial measurement.

The connection diagram and group counts select drawn-branch incidence. Two
disconnected stroke groups can still share a kinematic parameter, as the paired
corners do here. This read does not claim the complete underlying Gojo world
has been disconnected. Parameter coupling, response routes and their witnesses
remain in the native construction.

## Run and inspect

```powershell
dotnet run --project experiments/ConstructionMachines -- joints D:/_quickDraw/data/joint-constructions-2026-10-28-v1
dotnet test Tests/ConstructionMachines.Tests/ConstructionMachines.Tests.csproj
```

The review contains six experiments and 83 observations. Reinterpretation gets
its own frame before the next numeric pass, making an unchanged-position rewrite
visible. Play or use left/right to advance observations; Shift + left/right
changes experiments. Initial geometry, targets and site markers can be toggled.

`joints.gojo.bin` retains an ordered group of
`G(round controller, joint instance, negotiation world, target definition)`.
The joint instance and world share the actual mechanical participants. To resume,
restore the negotiation world first, then restore the joint instance against its
first machine. Generation verifies this by advancing every restored world.
`joint-observations.json` and `review.html` are derived presentation artifacts.

## Results and boundaries

The rigid controls retain an endpoint-coordinate gap of 80. Releasing the fan
and splitting the corners each reduce that diagnostic gap to 2 after twelve
passes. The remaining single-tick discrepancies are below the admitted response
at the selected fraction/quantum; motionlessness does not erase them.

The retirement case has zero coordinate gap throughout. Its upper-to-left path
is readable, becomes unresolved after retirement, and becomes readable again
after reintroduction. The fold case also has zero coordinate gap throughout;
its requested inter-foot path only becomes readable when the sites are folded.
Equal results therefore retain different connectivity and history.

Four external coordinate roles are retained in these examples. This deliberately
tests changing interior node/edge counts behind a stable external interface.
Retiring a branch preserves its remembered terminal and the external path
obligation; general removal of an externally exposed port remains a future
interface operation. Path reads preserve unresolved claims rather than turning
them into zero-valued failures.

Rewrite timing is authored. The experiment demonstrates that a supplied change
permits useful motion or changes path support; it does not discover or rank the
proposals, learn joint labels, or prove that the selected target is correct.
The next learning question is which local residuals and neighbouring claims
justify retaining, relaxing, splitting, folding or replacing a construction.

Targets are independently captured native worlds. They share neither private
parameter carriers nor coordinate-carrier identities with the live construction;
the actual port links carry the calibrated correspondence. Generation checks
that separation before running. Copying the reference is an explicit historical
capture, distinct from rebuilding the live operational world during each pass.

## Verified checkpoint

* 86 construction tests passed, including 14 new joint-construction tests.
* `dotnet test Gojo.slnx`: 5,493 tests passed across 13 test projects.
* All six native worlds were independently restored and advanced.
* Browser checks covered all 83 observations, geometry and graph edge counts,
  stats, fixed views, rewrite frames, keyboard navigation, playback, deep links,
  toggles and mobile layout. No script errors or external network requests.
* Focused tests cover shared construction dependencies, unchanged-position
  relaxation/split/fold, remembered branch obligations, ordered path witnesses,
  rejected correspondence, stale requests and historical coordinate reads.
