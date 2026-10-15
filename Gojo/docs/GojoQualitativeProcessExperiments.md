# Qualitative process experiments

This is an exploratory process fixture, implemented in
`Gojo.Tests/Experiments/QualitativeProcessTests.cs`. It uses the same general
`Gojo.Patterns.StructuralConstraint` law as the structural-constraint track.
It adds no primitive, water-process engine, sign table, or carrier settlement
rule. The process premises are selected local models, not discovered physical
truths.

## What is actually being tested

The earlier sufficiency fixture established representation: relations can
hold other relations and nonnumeric alternatives. `StructuralPattern.Refine`
and `Advance` do not interpret an influence or a dominance edge as an
inference rule. A baseline test preserves that boundary.

The added mechanism matches only explicitly selected variables against
exposed relation structure. Shared variables constrain repeated participation;
fixed participants preserve identity. A closed scope ends at its own seam;
an open scope has a distinct terminal. Matching, joining supported binding
frames, and projecting a consequent are general local reads. They contain no
process-specific branches. Candidates, blocked comparisons, open evidence and
the earlier reads remain connected.

## B1: direction before magnitude

Choose two uncalibrated landmark identities `a` and `b` as a local orientation:

```text
forward = a -> b
reverse = b -> a
stable  = a -> a
directions = closed[forward, reverse, stable]
property = current-value-slot -> direction-slot
```

These names are presentation aids. The ordered topology and shared landmarks
carry the distinctions. There are no numerical state codes, zero-valued
stand-ins, units, or measured extents. Reversal changes direction; it is not
placeholder duality. Selecting forward does not fill the current-value slot.
This is a useful local three-way model, not a proof that every change domain
has exactly these alternatives.

## B2: one influence and explicit closure

An exposed influence is `process -> direction`. The local schema is:

```text
closed[process -> direction]  => direction
```

The schema's process, direction and closing seam are selected variables. The
same matcher reads either oriented influence without knowing their names.
An open tail cannot satisfy the closed single-influence premise. A second
influence cannot satisfy its exact shape either.

After deactivation, an explicitly selected no-spontaneous-change schema reads
`closed[] => stable`. An empty *closed* active scope is required. The mere
absence of a listed process establishes neither exhaustiveness nor stability.
Earlier observations remain earlier evidence; rereading the changed active
surface does not overwrite them.

## B3: opposing influences

The active scope contains both actual influence relations:

```text
incoming = incoming-process -> forward
outgoing = outgoing-process -> reverse
active = closed[incoming, outgoing]
```

Without a justified dominance premise, the result remains connected to all
three direction candidates. In particular, reversing the influence frame
does not supply an amount, and the two relations do not cancel. The fixture
performs no hidden addition of signed codes.

## B4: dominance is a relation between influence relations

Dominance is authored as `winning-effect -> other-effect`. The local law is
conditional on an exhaustive closed pair of active effects. It is evaluated
with one sequence of general reads:

1. Match the active scope against an exact closed two-entry pattern.
2. Match the dominance relation against `winning-effect -> other-effect`,
   retaining each effect's identity.
3. Join that edge against the exposed ordered pairs of distinct active
   occurrences. Both orientations are exposed unconditionally, so the edge
   must account for both active participants.
4. Join each effect's identity to its own inspectable shape through the
   self-correspondence `effect -> (process -> direction)`.
5. Project `winning-direction` from the shared binding frame.

The active scope retains the **same identity and ordering** when dominance
reverses. Only `incoming -> outgoing` becomes `outgoing -> incoming`; the
matcher computes the corresponding direction. No C# branch selects a winner.
Removing the premise restores an unresolved direction read. An open active
tail, a third active influence, or dominance involving an unlisted process
cannot justify this two-influence consequence. An equal-looking cloned effect
does not inherit the original effect's membership. Self-dominance cannot leave
the opposing active participant unaccounted for.

This establishes local inference under an explicit *strict net dominance*
premise. It does not establish how dominance is learned, measured, checked
against a wider world, or extended to arbitrary sets of influences. In
particular, pairwise dominance over one influence cannot account for an
unrepresented third influence.

## B5: a nonnumeric activation landmark

An observation `landmark -> higher-site` supplies a qualitative position in
an explicitly selected landmark frame. The activation surface connects that
condition to an exposed drain influence. A general schema requires the
higher-site binding and projects the effect only when the observed relation
matches it. A lower-site observation or missing position structure cannot
justify the activation.

The output is an influence relation, so it remains available as a participant
in later structure. The test joins that actual influence's exposed
single-active response scope into the same B2 schema and derives its direction.
This extra closure premise is required before reading the net direction. The
effect alone does not establish the net direction of a property with other
active influences. Relation projection also composes a selected direction
into a larger relation without converting it into a scalar.

## Mixed qualitative and measured structure

A measured incoming value enters a carrier through ordinary tension input
and attaches to the **same incoming influence** through a measurement
relation. The original closed active scope, dominance premise, influence
identities and qualitative direction remain intact. The outgoing influence
stays unmeasured; the fixture does not invent its rate or a numerical net
rate.

This is a modest but executable mixed-model result: measured detail can
coexist with sufficient qualitative evidence without replacing that evidence.
It is not derivation of arithmetic from topology, numerical verification of
dominance, or automatic interval-to-direction refinement. Existing bounded
quantity and support-decision readers provide arithmetic and support laws;
connecting their measured comparison witnesses to a revisable qualitative
dominance premise remains a distinct extension.

The companion
[`QualitativeNumericRefinementTests.cs`](../../Gojo.Tests/Experiments/QualitativeNumericRefinementTests.cs)
adds an explicit numeric-to-qualitative bridge in five cases. Existing finite
difference and support-decision laws read a measured difference through declared
positive, negative and zero frames. Each response publishes the same already
constructed direction identities. `StructuralConstraint.Join` then narrows the
original qualitative alternatives. Inconclusive coarse support retains all
three; a finer publication can select one without replacing the qualitative
model. A measured direction incompatible with a qualitative premise leaves
blocked intersection evidence and preserves the original read. The selection
and refined read are connected in one result surface.

This bridge is a selected calibration, not automatic inference of a calibration
or a new arithmetic law. It does not claim the tightest qualitative support on
every inconclusive interval, nor establish which premise is wrong in a conflict.

## Representation, inference, enforcement and limits

- **Representation:** all directions, influences, active scopes, dominance
  and conditions are ordinary relations and placeholders. Pure qualitative
  tests have no carriers or carrier locations.
- **Inference:** the new shared selected-variable matcher and branchwise joins
  derive consequences from the authored premises. This was not behavior of
  existing substitution or numeric settlement.
- **Enforcement:** this track supplies supported reads. It does not attach a
  qualitative process to numeric carrier settlement or prevent arbitrary
  callers from bypassing a guarded structural application passage. The
  structural-constraint track tests that explicit passage separately.
- **Composition:** relations are both evidence and results. Shared slots,
  candidate binding frames and projected relations carry correspondence
  through subsequent local reads.

A failed exact pattern match is not a global verdict about physical truth.
A rigid placeholder used as an identity is different from a selected open
query slot; an opaque identity alone supplies no position or dominance
evidence. The tests preserve unresolved direction support when the required
premise is absent instead of calling the property stable or false. A read's
supported candidates are possible matches, not automatically proof that every
alternative observation satisfies a condition.

These experiments support a narrow architectural conclusion: existing
primitive structure can carry nonnumeric computation once a general local
interpretation law is supplied. Numeric tension settlement remains a
different local law. The shared substrate is useful, but this fixture does
not establish a universal inference system or show numerical mathematics
emerging without additional laws and calibration.
