# Enough: construction, questions and accountable reads

This small experiment implements the sender/receiver fixture discussed on
14 October 2026. It tests construction and contextual reads, including open
and failed binding. It is an **exploratory composition over existing kernel
laws**, not an autonomous learning mechanism or a change to Gojo foundations.

Run from the repository root:

```powershell
dotnet test experiments/EnoughStructure.Tests/EnoughStructure.Tests.csproj
dotnet run --project experiments/EnoughStructure -- --output experiments/EnoughStructure/results/my-run
```

Use a new empty output directory. Omitting `--output` prints the report without
saving files. The saved `report.md` is an observer projection; `experiment.gojo`
is the native graph containing the final live worlds and retained observations.
The first retained run is in [results/first-run/report.md](results/first-run/report.md).

## Physical construction

`P(C,n) = L(C,n) -> C`; `S(C,a,b) = P(C,a) -> P(C,b)`.
`G(...)` is the existing holder-grounded ordered group, not a new primitive.

Each independent world has a sender `S`, receiver `R`, and exposed ports:

```text
Port(C) = G(C.ValueSlot, C.LiveTensionSlot, S(C,0,1))

free receiver:     P(S,1) -> P(R,0)
limited receiver:  P(S,1) -> P(R,0), P(R,3) -> P(R,3)

arrival:           G(L(S,+n))
```

The ports deliberately advertise the native tick frame and pressure. Inspection
uses those surfaces. Arrivals go through `Carrier.ReceiveTogether`; all external
value change enters as signed tension. Worlds being compared share a declared
stable unit promise, while every sender and receiver retains independent identity.

The explicit route drains source participation. At four supplied ticks, the free
receiver has value 4; the limited receiver has value 3 and its sender holds one
blocked tick as tension. Its receiver tension is zero. With no attached route,
the sender retains value 4, rather than labeling it blocked route tension.

The direct builder authors the route and clamp immediately. The pattern builder
starts with `a -> b`, binds `a -> P(S,1)` and `b -> P(R,0)`, and refines the
construction. The clamp pattern is `k -> k` with one shared bound point.

```text
Application = G(pattern, G(bindings...), construction)
```

Both constructions execute through the same kernel; the application retains
which parts were authored or bound. A second test constructs the question
directly and through five placeholder bindings and reads both identically.
Binding does not activate a relation. `Activate` checks the exact advertised
1:1 source-to-receiver origin passage before calling native `Attach`.

Open applications keep their placeholders. Nonzero targets, reversed routes,
forged construction/binding correspondence, and conflicting alternatives remain
visible and do not activate this selected passage. This admission law is
deliberately narrower than every possible kernel route. Conflicting binding
rows remain in the application; the fixture does not use the existing helper's
first-binding convention as evidence that an alternative won.

## Questions and concepts

A quantity question is a reusable live surface:

```text
Qamount = G(exposedPort, alignment, domain)

alignment = S(R,0,1) -> S(B,0,1)
domain    = S(B,0,8)
```

The interval question adds acceptable participation and a coarse answer basis:

```text
Qenough = G(exposedPort, alignment, domain, acceptable, answerPartition)

acceptable     = S(B,3,8)
answerPartition = Partition(E, [0,0], [1,8])
```

These are exact, positional relation schemas. The three-part quantity question
and five-part interval question have different connected shapes, with no
operation enum, concept tag or named receiver category. A declared upper bound
is essential: this is "at least 3 within the 0..8 observation domain," not an
unbounded inequality supplied through a magic infinity.

The interval claim retains signed residuals and positive violations of its two
boundaries. A resolved zero total violation occupies the first answer region;
positive violation occupies the second. No boolean value or carrier tension is
created by asking. Distinct shortfalls (for example 1 and 2) can have the same
coarse answer while retaining their different evidence.

Inspection captures the exposed numeric premises, projects the selected value,
checks its declared domain, then reads the acceptable interval and publishes
answer-region membership. The ordered alignment's source must be the advertised
port's carrier and its target must be the declared read basis. This is an
explicit fixture contract: the general kernel can also rebase an origin frame
onto a compatible same-unit source, which alone does not establish site identity.
Zero source span, unbound operands, wrong carrier
identity, reversed or unaligned bounds, an out-of-domain value, and nonzero
projection residue leave a `Placeholder` result with attempted-read evidence.
The selected fixture asks for an exact integral projection; it preserves the
remainder rather than automatically refining or treating it as false.

Live ports and questions are reused. Each historical read copies the exposed
numeric sites and their internal aliases, retaining carrier identity and unit
promise. It does not follow private attachments. Mutating a live input or
retargeting the reusable acceptable interval changes a new observation, while
old sampled claims stay readable. This is an explicit numeric observation
contract, not an independent physical-world snapshot for every time step.

## Witnesses and goal

```text
QuestionWitness = ReadWitness(
  sampledPort,
  sampledQuestion,
  projectedAmount OR native occupied-region publication,
  projectionResidue,
  G(projectionRead, domainClaim, optionalAcceptableClaim))

Goal = G(question, availableSourcePort, permittedPassage, candidateApplication,
         L(E,0), beforeMinusAfterDemandEquation)

World = G(sourcePort, receiverPort, boundaryApplications, goal, clock)
```

The goal's target is explicitly zero demand in the question's residual basis.
Its response equation has the ordered coefficients `after: -1`, `before: +1`.
The equation itself is retained, not merely its numeric result. The goal also
connects the supply port, permitted `P(S,1) -> P(R,0)` passage and actual candidate
action. Admission compares the candidate endpoints against that stored passage;
the permitted action is readable before the admission code runs.

```text
Step = G(timeSpan, beforePorts, G(arrival, transition, materialAccount),
         afterPorts, G(sourcePressure, receiverPressure))

GoalResponse = ReadWitness(
  sampledGoal,
  Step,
  G(beforeQuestionWitness, afterQuestionWitness, demandDifferenceRead),
  G(materialAccount, retainedPressure),
  appliedTransition)
```

An open or failed route can leave a **resolved unmet goal**: its question still
knows the receiver is empty. An open question leaves demand unresolved. Zero
improvement with an unmet requirement differs from zero improvement after
satisfaction. Positive response is read evidence; it does not automatically
reward, strengthen, withdraw or grow a connection, or introduce carrier tension.

Native attachment immediately settles. The activation step records that action
and its before/after accounting; it does not invent an internal pass-by-pass
attachment trace. Pulse steps additionally retain the kernel's bounded execution
observation. The runner schedules these actions and clock increments.

## Contextual equivalence

The free and limited receivers agree under the enough question throughout the
declared nonnegative total domain, including split arrivals. They differ under
the amount question once total supplied exceeds 3. Earlier enough witnesses are
retained, so changing the question does not rewrite prior evidence.

The comparison checks the question's exact shape, declared domain, acceptable
interval, native unit compatibility and partition under an explicitly selected
1:1 correspondence. It compares occupied region endpoints through aligned
equality claims. Quantity comparisons similarly align the two amounts. Both
source witnesses and the correspondence remain connected to the equality witness.
Equal coordinates do not imply shared identity; different unit promises or
different question contracts do not become equivalent from coincident outputs.

These witnesses establish equality of selected observations. Tests exhaust the
finite 0..8 total domain and selected pulse decompositions. They do not synthesize
a universally quantified proof or infer an arbitrary concept from examples.

## Calculation inspection

An amount read performs one affine projection and two one-sided domain claims.
An enough read adds two one-sided acceptable-interval claims and a two-region
membership read. The numeric laws are the existing kernel projection, claims
and coarse-publication mechanisms, with their premises retained in relations.

For a same-unit scalar sample `x`, projection retains:

```text
y = c + truncate_toward_zero((x-a)*(d-c)/(b-a))
source remainder = (x-a) - (y-c)*(b-a)/(d-c)
```

For the declared 1:1 frames, `y=x` with zero remainder. The interval read uses
`lowerViolation=max(0,lower-y)` and `upperViolation=max(0,y-upper)`.
The helper computes these local reads; the authored interval and its retained
claim witnesses carry the scope and result. This fixture adds no new primitive
comparison law or hidden general constraint solver.

Every material-account witness retains **seven native equation predictions**:

1. Before sender held amount: value + pressure.
2. Before receiver held amount: value + pressure.
3. After sender held amount: value + pressure.
4. After receiver held amount: value + pressure.
5. Available participation: before sender held + introduced delta.
6. Accepted participation: after receiver held - before receiver held.
7. Accounted total: accepted + after sender held.

An equality claim checks `available = accounted`. Post-source value and pressure
remain separate as remainder and blocked participation. They are not added twice.
For activation, the introduced delta is zero and available supply comes from
the sender's retained pre-action state. Each goal response adds one native
before-minus-after demand prediction. Statement residue and physical pressure
remain different, and read-side accounting does not mutate the system.

The report includes reachable graph counts for a selected witness. They include
attachments and unit evidence, so they are neither minimal query cost nor a
runtime benchmark. Captured calculation witnesses allocate new relation
surfaces; this fixture prioritizes inspectability over compact execution.

## Boundary of the result

This implements a small native construction, reusable questions, connected
goals, sampled response and material witnesses, and contextual comparison.
It establishes what can be expressed and inspected with current mechanisms.
Host scheduling, proposed bindings and the fixture's exact readers remain
supplied. Learning useful proposals, assigning causal credit, automatically
retaining structure, and mutual biological-style maintenance remain future work.
