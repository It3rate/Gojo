# Interchangeable construction machines

This is an exploratory, executable fixture, not a new universal geometry or
settlement law. Its native relation graph retains the construction, open roles,
inputs, output expressions, dependencies, repeat controller, closure demand,
public interface and application history. Names and SVGs are presentation reads.

The initial review contains 13 cases: an open interface, three equivalent
rectangle makers, shared-phrase widening, centre-preserving widening, a failed
seam, an offset roof ridge, an arched cap, a moved opening, a one-beat square,
an unfinished five-beat phrase, and a hinged leaf.

The [bidirectional negotiation extension](Negotiation.md) adds ten connected
wall/roof/door experiments with 140 saved observations. It compares private
response constructions, a geometry-preserving in-place replacement, limited
capacity, coarse response packets, fixed conflicts and oscillation. Each world
has a retained round budget, executable native state and an offline review:

```powershell
dotnet run --project experiments/ConstructionMachines -- negotiate D:/_quickDraw/data/construction-negotiation-2026-10-28-v1
```

The [joint construction extension](Joints.md) adds six experiments with 83
observations: fixed controls, T-to-fan relaxation, T-to-corner splitting, branch
retirement/reintroduction, and coincident-site folding. It retains node/edge
correspondence and external path obligations alongside mechanical negotiation.

```powershell
dotnet run --project experiments/ConstructionMachines -- joints D:/_quickDraw/data/joint-constructions-2026-10-28-v1
```

The [coupled shape and joint extension](Coupled.md) combines both mechanisms.
A moving, resizing outline develops a ridge/eave conflict with an initially
compatible rigid joint. Observed mismatch triggers bounded local previews;
an eligible replacement preserves public sites before later responses change
the geometry. Five controls and 266 observations expose remaining disagreement,
load accounting and rejected alternatives, including loss of a required path.

```powershell
dotnet run --project experiments/ConstructionMachines -- coupled D:/_quickDraw/data/coupled-constructions-2026-10-28-v1
```

## Selected structural contract

`G` denotes an ordered native group. All references below are actual elements.

```text
site = open role -> G(x expression, y expression)
boundary = G(A -> B, B -> C, C -> D, D -> A)
interface = G(G(A,B,C,D), boundary)
machine = TransformSurface(inputs, operation, outputs, residue policy, provenance)
application = G(machine, bindings, calculated outputs, witnesses, remainder, output bindings)

affine expression = G(first, second, G(sample, parameter frame, support, basis)) -> output
turn expression = G(retained contour traversal, G(direction))
repeat = RepeatSurface(template, count span, transition, feedback, closure demand, timeline)
```

The first implementation uses the existing exact affine and contour-turn readers
inside a bounded interpreter. It explicitly publishes accepted observations to
stable structural sites. Numeric parameter changes use carrier tension. This is
not automatic bidirectional geometric settlement. Failed closure, unsupported
turns and incompatible publications remain visible rather than being forced shut.

Three private constructions share one cyclic four-site public interface:

* A two-beat move/turn phrase repeated twice, including the turn after each move.
* Two independent ordered perpendicular spans and their shared offset families.
* A centre with two opposite pairs, constructed through signed half-spans in
  the selected perpendicular frame. This is the antipodal construction, not
  equally spaced contour quarters (which would give a square).

The roof and door consume public sides. The door opening is a separate selected
interval with carried complement; it does not delete the complete boundary.
Geometry-local corner participation need not describe all branches at that site.

Regular pentagon turns and asymmetric pentagon closure are distinct further
questions: a five-beat length pattern alone does not establish closure. The
fixture must not smuggle an arbitrary angle or a forced closing edge into that
construction.

## Run and inspect

From the repository root:

```powershell
dotnet run --project experiments/ConstructionMachines -- D:/_quickDraw/data/construction-machines-2026-10-28-v1
dotnet test Tests/ConstructionMachines.Tests/ConstructionMachines.Tests.csproj
```

The output folder contains `review.html`, derived `observations.json`, and
`construction-machines.gojo.bin`. The review is offline and can be regenerated;
there is no dependency on a website or an AI service. The binary is a native
Gojo archive with executable definitions and retained observations, not a list
of final SVG coordinates. A restored recipe can be run without its builder and
its parameter carriers can be changed independently of the original process.

## What the experiment establishes

| Construction | Reused structure | Response illustrated | Unfinished demand |
| --- | --- | --- | --- |
| Perimeter | A move/turn transition and two-beat phrase, repeated twice | Changing the shared long length changes both opposite sides while retaining the start | A separately changed occurrence can miss the seam |
| Offset families | Two perpendicular frame directions and two common displacements | Width and height are separate inputs; no traversal dependency between the two spans | A nonperpendicular or incompatible realization cannot publish as this rectangle |
| Opposite pairs | Centre, signed half-spans, reflected radial pairs | Enlarging the half-width moves both sides while keeping the centre fixed | Equal reach alone does not determine turn order or angular spacing |

The third construction uses two antipodal pairs rather than the previously
suggested contour-square/stretch route. Its four offsets have the same squared
reach in the declared metric; their equal spacing around a circle is neither
assumed nor needed. The point-derived circle and equal-quarter construction is
exercised separately by the path's turn machine. A future circle-parameterized
rectangle can expose radius and paired landmark choice as different controls.

The common interface retains four open corner roles and four ordered sides.
Before metric binding it is a closed four-role topology, not a claim that any
four measured points make a rectangle. Publishing a realization checks the
selected Cartesian metric, coherent actual axes, nonzero edges, right angles,
closure, and whole-tick representability. A failed publication retains its
candidate and remainder while leaving every accepted site unchanged. The
whole-tick interface is a deliberately narrow contract; exact fractional
construction results remain available to a future finer port.

After first binding, publications retarget the same actual coordinate tick
references through one grouped structural port. The roof and door depend on
the public roles, so they need no knowledge of the current rectangle maker.
Role correspondence is explicit and ordered; no proximity matching selects it.
This is not yet general automatic matching between arbitrary T/Y interfaces.
Every declared output port is bound to its calculated result. A direct maker
exposes one port per corner; a repeated path exposes its whole ordered route,
including the return site. Publication retains the correspondence from these
private output ports to the stable public roles. Equal-looking coordinates
cannot replace that producer-to-interface connection.

## Reuse, state and history

Definitions are retained references. Repeated beats share one transition and
the same parameter carrier where the phrase asks for reuse. Each invocation
has separate state and a witnessed timeline. A direct construction is a native
affine expression graph, not a call to a host `drawRectangle` function.

In compact explanatory notation, the path transition is
`(position, direction, reach) -> (position + reach*direction, turn(direction))`.
The retained native structure expresses the sum and scaling as affine requests,
and the turn as application of a witnessed contour traversal. The phrase selects
the reach source for each beat. Its two output roles feed the next position and
direction roles. A missing turn is itself a connected request consuming the
current direction; later binding that law completes the same machine.

The other recipes make different dependency choices. Offsets retain a shared
displacement for each opposite-side pair. The centre construction retains
half-spans and their sign reversals. These formulas explain the graphs; they are
not a symbolic replacement for the native operation requests and references.

This provides a small form of the proposed muscle memory: retain the instruction
graph, then supply local bindings and invocation state. It does not yet discover
which graph to reuse. Shared instruction identity need not mean shared mutable
parameters: two future window instances can use one template with distinct
binding frames, while a repetition relation deliberately shares chosen extents.
Copying the whole mutable state would be a different, explicitly witnessed act.

The selected controller executes a finite schedule. Carrier changes use the
kernel's signed delta settlement; geometry is then observed and explicitly
published. A closure discrepancy is a read remainder, not automatically new
carrier tension. There is no global geometry solver, automatic choice of maker,
learned response distribution, or claim of convergence here.

Publications capture before/after coordinates. Definitions and source parameter
references remain live: an older application retains its calculated result, but
its referenced recipe can now read newer inputs. The archive is an exact copy
of the retained world at save time, not a promise to reconstruct every previous
live parameter value. Replaying older input states requires their own captured
binding frame.

The review archive therefore has two roots inside its outer group: an ordered
group of deliberately frozen case worlds, and the final operational world.
Each case captures its input state, native machine and attachments before the
next perturbation. Within a captured world all shared sites remain shared;
different cases have independent restored identities. These historical snapshots
do not replace the live interface during execution. This prevents a saved old
roof read from acquiring moved endpoints while retaining an earlier ridge.

## Context and extensions

* The roof consumes the top side. Its ridge can move along that side without
  changing the wall machine. A rectangle-local corner still selects its two
  boundary branches even when the roof adds a third global branch.
* The doorway retains the full side, selected opening interval, carried
  complement, and doorway structure. Moving the opening does not damage the
  complete wall. This is a boundary edit, not a Boolean area fill or learned
  recognition of a door.
* An arched cap is a retained nested-affine curve instruction, with a sample
  role and control sites. Its render samples are observations of that machine.
* A window leaf retains a hinge, reach and contour traversal. Rotating the leaf
  leaves its opening reference available. This is a planar hinge experiment,
  not a mechanical simulation of glass or a three-dimensional window.
* A five-beat phrase can retain unresolved turning. Short/long rhythm is useful
  structure, but does not by itself supply a regular or elongated pentagon.

No human-function ontology is introduced. A door's supporting line may later
be a landing rather than geographic ground. A top-down room boundary can reuse
the same rectangle machine under a different observer and passage context.
Those different connections, rather than a renamed shape, should carry the
eventual functional interpretation.

## Connected response experiment

The original publication-only fixture remains available. Its next step is now
implemented separately in `Negotiation.cs` and `NegotiationExperiment.cs`:
independent machines exchange captured scalar publications through actual public
ports, then admit calibrated displacement requests into their own parameter
carriers. The new engine also supports an explicitly authored replacement under
the same ordered public interface. See [the contract and limits](Negotiation.md).
Neither fixture supplies a learning law or a universal convergence claim.

## Initial fixture checkpoint — 28 October 2026

* 44 focused construction tests passed, including native restoration, late turn
  binding, output-port correspondence, shared-site identity, atomic rejection,
  historical capture, open inputs, rational reads, closure and curve sampling.
* `dotnet test Gojo.slnx`: 5,451 tests passed across 13 test projects.
* All 13 report cases were regenerated and their native archive restored.
* Browser checks found no script errors or network requests; navigation,
  guide toggles, earlier-geometry toggles and narrow-screen layout passed.
