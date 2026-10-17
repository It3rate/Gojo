# Structural constraint experiments

Status: experimental local law implemented above the unchanged kernel,
2026-10-15. This follows the representation-only
[primitive sufficiency experiments](GojoPrimitiveSufficiencyExperiments.md).
The focused fixture is
[`StructuralConstraintTests.cs`](../../Tests/Gojo.Tests/Experiments/StructuralConstraintTests.cs);
the new local reader is
[`StructuralConstraint`](../Patterns/StructuralConstraint.cs).

The first fixture demonstrated that binding can insert a nonmember and that
`StructuralPattern.Advance` substitutes bindings without checking authored state
edges. Those remain useful reproductions of the original boundary. This follow-up
adds an explicit constrained passage; it does not silently change ordinary
binding into a domain-enforcement mechanism.

## What is being tested

Can relations and placeholders support finite nonnumeric membership,
directed-transition inference, alternative refinement and guarded application?
The distinction between representation, inference, enforcement and composition
is retained throughout. Adding this reader does not establish that the old
substitution helpers already performed these operations.

No experiment in this track constructs a carrier, carrier location, numeric
state code or enum. Assertions inspect the complete resulting graphs to verify
the absence of carriers and locations. Host iteration schedules local reads;
the test harness supplies no membership or transition decision procedure.

## Structural law

Write a finite exposed scope as an anchored spine:

```text
scope = seam -> (entry0 -> (entry1 -> ... -> tail))
closed enumeration: tail shares seam identity and face
open enumeration:   tail is a different placeholder
```

Entries are participants, including arbitrary relations. Reading the spine does
not mine their private interiors for more entries. Closure is shared topology;
another placeholder of the same primitive shape does not close the scope.
An open tail retains participation that the enumeration has not supplied.

The three presented states are:

```text
A = a -> a
B = a -> b
C = b -> a
outside = b -> b
```

Here `a` and `b` are identity-bearing placeholders. Their internal meaning
remains unbound. A local read can nevertheless distinguish the identities of
these relation participants without claiming that their finer contents have
been grounded. A selected query slot is an unknown for this read; every
placeholder elsewhere is not automatically a wildcard.

The query retains its template, explicitly selected variable scope, initial
binding scope and offered support:

```text
request = ((template -> variables) -> initialBindings) -> support
read    = (request -> variables) -> (candidates -> (blocked -> unresolved))
candidate = offered -> (instantiatedTemplate -> bindings)
```

A relation containing selected query variables is decomposed in order. A
participant without selected variables is compared at identity resolution.
Repeated variables must retain one correspondence. Initial conflicting
bindings remain unresolved instead of choosing the first binding.

For membership, use template `memberSlot`. Supplying `memberSlot -> B` asks a
concrete nominal membership question; leaving it unbound asks for supported
alternatives. For transitions, use template `fromSlot -> nextSlot` and retain
that template even when both slots have initial bindings. This permits reading
the directed clause while keeping each concrete endpoint's identity intact.

The source offer remains connected to each candidate. A reconstructed relation
with equal shape is not substituted for the original offer's identity. The
read exposes failed offered matches separately from unresolved premises;
neither is a hidden Boolean verdict about the entire source world.

## Results exercised by the fixture

| Probe | Observable consequence |
| --- | --- |
| A1, known closed-domain member | One witnessed identity correspondence; guarded application can use it. |
| A1, known closed-domain nonmember | No candidate; failed matches remain visible; guarded application leaves its target unchanged. |
| A1, unresolved candidate | All three offered alternatives survive; no first-binding selection. |
| A1, open domain | Unenumerated participation remains unresolved, including when no known offer matches. A known member still has its own candidate witness. |
| Identity versus shape | A fresh `a -> a` is not a nominal member merely because it equals A structurally. An explicitly selected repeated-slot pattern can request that shape read instead. |
| A2, authored A -> B | The ordered edge supplies bindings and an inspectable result; its actual source relation remains connected. |
| A2, missing A -> C or reversed B -> A | No accepted candidate. Two authored edges do not silently establish a one-step transitive edge. |
| A2/A3, unresolved destination from A | Closed support containing only A -> B refines the destination to B. An open support with that one known offer retains its missing alternatives. |
| A3, add A -> C | B and C both survive and guarded unique application stops choosing a destination. |
| A3, remove A -> B from the newly exposed support | C becomes the uniquely supported destination; the earlier read still explains its earlier B result. |
| A4, two local passages | Each projected candidate from the first passage is checked against the second. The shared participant carries the correspondence; one branch is admitted and another blocked. |
| A4, composed constraint read | The same branchwise operation composes directly through `Join`, retaining the originating branch and any open upstream remainder. |
| Higher-order participation | The actual offered edge is an endpoint in another constraint. A lookalike edge does not acquire that identity. |
| Conflicting initial bindings | Open evidence is retained; the reader does not inherit the substitution helper's first-binding convention. |
| Opposed placeholder faces and repeated offers | Opposed faces do not become one nominal participant. Repeated offered occurrences remain separately witnessed rather than silently deduplicated. |
| Selected versus unselected dual face | Binding an explicitly selected forward slot leaves its unselected dual face rigid during both matching and application. |

The unique reader deliberately requires exactly one candidate and no unresolved
premise. It can therefore leave a known admitted member uncommitted when the
overall enumeration is open. The candidate witness still establishes the
positive local correspondence. This conservative rule is not a claim that
membership in an open set cannot ever be known.

## What changed and what remains missing

**Representation** was already available: ordinary relations can contain states,
edges, constraints and other relations. Their composability did not depend on
introducing a wrapper type.

**Inference** is added by one local law: matching an explicit pattern against
an exposed finite support while preserving bindings and alternatives. The same
law handles a member slot, a directed edge, a repeated-participant pattern and
a relation-valued endpoint. It does not dispatch on state names or application
domains.

**Enforcement** is provided at the explicit application passage. `ApplyUnique`
returns an applied target only for a uniquely admitted read; otherwise it
returns the target unchanged. Raw construction, `StructuralPattern.Bind` and
ordinary `Advance` remain available and unconstrained. There is no global
ownership lock that prevents every other caller from building an incompatible
relation.

**Composition** is explicit, branchwise and non-draining. Projection retains
the original candidate as provenance, and each projected participant can bind
the next passage's selected slot. The fixture does not introduce material
conservation semantics for copying these observations. It also does not merge
alternative binding frames into one first-wins frame.

The helper performs a selected observation and produces a new structural
application result; it is not an attached autonomous structural settlement
loop. New exposed supports require new requests or reads connected to those
supports. An old request continues to refer to its old support, and an old
read does not automatically refresh. The fixture tests that limitation rather
than describing automatic dependency invalidation or a timeline scheduler.

These experiments support a narrow architectural conclusion: the existing
element, relation and placeholder primitives can carry useful pre-numeric
constraint computation when supplied with an explicit local interpretation
law. They do not demonstrate a missing primitive. They also do not establish
a general theorem prover, unrestricted unification, autonomous discovery,
arbitrary recursive propagation or quantitative mathematics emerging without
additional structure.

## Verification

The fixture contains 18 focused experiments. Test execution is recorded in the
combined implementation assessment after the production helper and both tracks
have been compiled and checked together.
