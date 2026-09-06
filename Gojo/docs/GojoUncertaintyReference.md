# Gojo Uncertainty Reference

September 6, 2026. Design analysis following the
[numeric structure milestone](GojoNumericMilestoneReview-2026-09.md).

**Status:** this is a working inventory and a proposal for subsequent fixtures.
The implementation survey below distinguishes existing mechanisms from missing
laws. The inventory is not a closed ontology, an `UncertaintyKind` enum, a new
syntax grammar, or a claim that uncertainty propagation is already implemented.
The [canonical prompt](GojoSystemPrompt.md) remains the repository authority.

## 1. The Main Distinction

An uncertain read concerns a source, a question, a perspective, and the evidence
available to that perspective. It may leave several values, structures,
histories, or possible outcomes admissible. Another local space can have a
different useful read without either space being defective.

Three questions must remain separate:

1. **What may be true?** Evidence, assumptions, possible completions and their
   dependencies constrain a descriptive read.
2. **What is relevant here?** A selected perspective determines which
   participation and detail the current question needs.
3. **What should become true, or is allowed?** A goal or specification defines
   a desired or permitted condition. It is not evidence that the condition
   currently holds.

Gojo's broader concern includes uncertainty, incomplete construction,
unaccounted participation and unresolved change. These overlap, but they are
not synonyms. A known rational remainder, known blocked transfer, exact
one-unit mismatch, or intentional clearance can have no numerical uncertainty.
Conversely, a settled carrier or fully bound pattern can describe a quantity
that remains uncertain.

Measurement uncertainty has a narrower established meaning: it characterizes
the dispersion of values attributed to a specified measurand given available
information. Gojo also needs uncertainty about relations, applicability,
identity, access and action. These need not all acquire a numerical dispersion.
See [JCGM VIM, measurement uncertainty](https://jcgm.bipm.org/vim/en/2.26.html).

## 2. Working Inventory

The rows below describe overlapping circumstances. One observation can have
several at once. The response depends on their connected explanation, not on
dispatch through the row's name.

### 2.1 Values, Observations And Explanations

| Circumstance | What remains open | Consequence for calculation or exploration |
| --- | --- | --- |
| Unbound construction | A participant, relation, parameter or result has not been supplied. | Preserve the slot and its constraints. Do not substitute zero. Some coarser questions may already be answerable. |
| Bounded but unresolved quantity | The value is somewhere in an explicitly justified set. | Propagate the admissible joint values; retain the origin and conditions of the bound. A bound alone gives no probability distribution. |
| Discrete alternatives | Several classes, routes, models or configurations fit. | Preserve candidates and their evidence. Do not average candidates unless the resulting mixture itself has a defined interpretation. |
| Vague description | “About a cup,” “many leaves,” or “warm” does not determine a sharp boundary. | An explicit context may supply a useful coarse read or bound. The word alone does not supply exact endpoints or a probability law. |
| Quantization and representational resolution | A read cannot distinguish finer participation. | Preserve available remainder or a stated cell promise. A tick size is not automatically a measurement uncertainty, and an absent digit does not mean zero. |
| Observation noise and repeatability | The observation process varies between events. | Attach observation events and their possible errors to the source; two readings of one source are not automatically the same reading. |
| Calibration and shared systematic effects | An instrument, conversion or reference may introduce a common unknown contribution. | Preserve shared causes and coefficients. Contributions may cancel or reinforce; treating them as independent can misstate uncertainty. |
| Physical variation | Different objects, times or outcomes can actually differ. | Distinguish a population or process from an individual sample. A fixed part's unknown size and a changing process's spread answer different questions. |
| Unselected outcome of a stochastic model | The process description can be complete while its next outcome remains unresolved. | Carry the supplied probability law and joint assumptions when relevant. Outcome variability and uncertainty about the law's parameters are different. |
| Measurand ambiguity | Which property is being measured, where, when or under which conditions is underspecified. | Refine the quantity definition before combining apparent measurements. More digits cannot repair different questions. |
| Uncertain instance association | It is unclear whether observations concern the same object, site or event. | Preserve alternative correspondences. Automatic aliasing can create false cancellation; automatic separation can lose a real shared cause. |
| Measurement participation in change | The act of obtaining information also changes the source. | Represent the observation and its material interaction separately, including the state transition. A Gojo projection remains observational; a physical measuring process can require an explicit action around it. |
| Unknown model parameters | A relation is selected, but its parameters are not established. | Keep the parameter as connected unknown participation, shared wherever the same parameter is used. |
| Competing relation structures | Different explanatory structures remain plausible. | Retain alternatives and seek observations or interventions that distinguish them. A narrow result inside one model does not settle model selection. |
| Limited model applicability | A useful relation may cease to hold outside a scale, condition or horizon. | Preserve its conditions of use and evidence. Extrapolation is a new claim, not automatic refinement. |
| Approximation and omitted influences | A read deliberately neglects participation that may affect its result. | State the scope and, when needed, bound the effect or retain a route for refinement. Omission is not a claim of zero influence. |

“Physical” and “perspective based” are useful distinctions, but not exclusive
bins. Variable production creates physically different bolts; not knowing which
bolt was selected is uncertainty for an observer. The same variation can be
irrelevant to one task and decisive to another.

Metrology's Type A and Type B labels distinguish methods of evaluating
uncertainty, not a universal split between physical and epistemic uncertainty.
See [NIST's classification](https://www.nist.gov/pml/nist-technical-note-1297/nist-tn-1297-2-classification-components-uncertainty).

### 2.2 Perspective, Access, Dependence And Resources

| Circumstance | What remains open | Consequence for calculation or exploration |
| --- | --- | --- |
| Deliberately irrelevant detail | A view does not request information that may exist. | Exclude it from that read. Retain a complement or source reference when useful; do not invent a penalty or force exploration. |
| Unknown locally, readable elsewhere | This space lacks an interpretation or observation another space may provide. | Use an exposed port, explicit basis and provenance. Local opacity does not imply global absence. |
| Delayed or unavailable information | A dependency cannot currently be obtained. | Preserve what is pending and the conditions of access. Waiting, requesting and estimating are distinct actions. |
| Missing or censored observation | No sample arrived, or a detector reported only a threshold relation. | Missing is not zero; “below limit” is a constraint, not necessarily the limit value. Missingness can itself depend on the source. |
| Stale information and future evolution | A valid earlier observation may no longer describe the current state. | Connect state, observation time and transition evidence. Reusing an old value needs an explicit persistence or prediction premise. |
| Unknown dependence | Marginal descriptions exist, but joint behavior is not established. | Do not assume independence from distinct identities. Use a conservative joint read or keep the dependence question open. |
| Non-identifiability | Several underlying states give the same available observations. | Seek a different relation or intervention. Repeating the same question more precisely may never distinguish the states. |
| Inaccessible under current conditions | The available ports, instruments or perspective cannot distinguish an answer. | State the limitation relative to a question and access model. Do not silently promote “unavailable here” to “impossible everywhere.” |
| Search or computation incomplete | A useful result has not been found within available effort. | Distinguish “not found” from “does not exist.” Preserve bounds, explored alternatives and remaining work when useful. |
| Compressed or forgotten history | Detail or dependency was intentionally discarded. | Preserve the remaining promise and acknowledge loss. A later read may need wider bounds or renewed observation. |
| Unanticipated model gaps | Existing structure may omit a relevant distinction entirely. | Keep room for failed predictions and new structure. No finite inventory can pre-encode every future surprise. |

For example, observing only `x + y = 5` does not identify `x` and `y` separately.
More decimal places in that same sum do not provide the missing relation. This
is different from measurement noise and from insufficient computation.

Questions outside present observation, or about another organism's experience,
need explicit limits on what the local evidence can establish. A numerical
interval should not be fabricated when no suitable quantity or ordered answer
space has been defined.

### 2.3 Goals, Disagreement And Unresolved Operations

| Circumstance | What remains open or unsettled | Consequence |
| --- | --- | --- |
| Desired goal | A specified future condition may not hold or may not be reachable. | Keep the target separate from evidence about the current state. Explore admissible transformations and preserve unsuccessful attempts. |
| Tolerance or permitted variation | A specification allows a set of outcomes. | Treat that set as normative. It does not prove the actual item belongs to it or establish how likely each value is. |
| Intentional difference or clearance | A relation wants a nonzero gap. | Compare against the allowed gap, not automatically against equality. The gap itself need not be uncertain or defective. |
| Contradictory evidence or premises | No interpretation satisfies all currently selected constraints. | Expose the incompatible participants. Do not assign fault by default, and do not treat an empty possibility set as success. |
| Error or mistaken belief | A value disagrees with a reference, or a relied-on premise may be false. | Separate a known discrepancy, uncertainty about its size or cause, and an unrecognized mistake. Confidence is not a correctness proof. |
| Projection residue | A selected read leaves participation unrepresented. | Keep the remainder available. It can be exactly known and does not automatically demand physical change. |
| Illegal lanes, blocked transfer and capacity overflow | A route cannot interpret or accept some participation. | Preserve the relevant witness and remainder. A blocked amount may be exactly known; material accounting still applies. |
| Unsettled carrier tension | Introduced signed change has not been absorbed or routed. | Continue or expose local settlement through explicit connections. Uncertainty does not itself inject a delta. |

Measurement error is a difference from a reference; it is not defined as
“uncertainty we mistakenly consider certain.” That phrase describes an
important failure of belief, but a discrepancy can also be known exactly.
See [JCGM VIM, measurement error](https://jcgm.bipm.org/vim/en/2.16.html).

## 3. Nominal Stops, Tolerance And Planned Clearance

### 3.1 A Catalogue Is Not A Measuring Instrument

The labels `10 mm`, `11 mm`, `12 mm`, or a series of fractional-inch sizes can
be nominal stops in a catalogue. Their spacing does not establish the
resolution of a micrometer or prove that physical parts cannot have an
intermediate dimension.

A useful structure connects, as needed:

- the nominal class and the selected standard;
- the actual instance and feature being considered;
- the permitted dimensions under that specification;
- the observation event, calibration and conditions;
- the evidence about the actual feature;
- a comparison or classification witness.

A bolt has several dimensions. A `10.2 mm` observation is not interpretable as a
nominal-size violation until the intended feature and its conditions are
specified. Finite detail in the measurand definition can itself contribute
uncertainty; see [JCGM VIM, definitional uncertainty](https://jcgm.bipm.org/vim/en/2.27.html).

One explanatory measurement relation is

```text
observed diameter = actual feature diameter + calibration contribution
                    + event-specific observation contribution
```

This is mathematical notation for a connected relation, not new executable
Gojo syntax. More complicated measurement processes need different relations.

If the class and manufacturing history are well supported, the measurement
process may be a useful first place to investigate. It is still an inference,
not a law assigning fault. An out-of-spec part, another measured feature, a
wrong class or an inappropriate calibration remain possible explanations.

### 3.2 Tolerance Does Not Establish Actual Size

A tolerance interval specifies permissible values. An uncertainty interval
describes values supported by information and assumptions. An acceptance rule
decides how evidence is used to make a conformance decision. These can have
different boundaries. See [JCGM 106, conformity assessment](https://www.bipm.org/documents/20126/2071204/JCGM_106_2012_E.pdf).

Using a tolerance as a bound on an actual part requires an explicit premise
that the part conforms. A catalogue alone does not demonstrate that premise.
Likewise, a tolerance does not imply a uniform probability distribution or a
particular process variation. One item may have a fixed but unknown dimension;
an ensemble of produced items has a different question attached to it.

### 3.3 The Desired Difference Can Be Nonzero

For an illustrative diameter model, assume the following are justified bounds
on two actual features. These numbers are not a fastener standard or a complete
mechanical fit model:

```text
bolt diameter B in [9.90, 10.00] mm
hole diameter H in [10.10, 10.20] mm
diametral clearance C = H - B in [0.10, 0.30] mm
```

This is a conservative interval when no tighter joint constraint is known.
If the desired clearance is also `[0.10, 0.30] mm`, the nonzero difference is
compatible with the design. A simple violation read for a known clearance is

```text
v(c) = max(c_min - c, 0, c - c_max)
```

Every allowed clearance has zero violation, despite a positive physical gap.
Whether that gap is represented as stored tension depends on an explicit
physical coupling and its reference condition. Mere difference does not inject
carrier tension.

For a nonempty possible-clearance set `C` and a permitted set `T`:

- `C` contained in `T`: all represented possibilities satisfy this criterion;
- `C` disjoint from `T`: none satisfy it;
- otherwise: the available description does not settle conformance.

An empty `C` means contradictory premises or a failed construction, not a
vacuous pass. All three useful decisions remain conditional on the validity of
the bounds and the selected fit model.

## 4. The Two Cups: Dependence Is Part Of The Arithmetic

### 4.1 Same Quantity Versus Same Description

Let a particular volume at a particular state be `X`, expressed in a chosen cup
unit, with a justified bound `[0.9, 1.1]`.

```text
X - X = 0
```

The cancellation is exact for the same finite quantity in a compatible basis.
It does not require discovering the value of `X`. Treating the two occurrences
as independent intervals and returning `[-0.2, 0.2]` loses known dependence.
The zero result need not erase the input structure, unit or cancellation
witness; those may still matter to later reads.

Now let two separate volumes `X` and `Y` each satisfy the same bound:

```text
X in [0.9, 1.1] cups
Y in [0.9, 1.1] cups
X - Y in [-0.2, 0.2] cups
```

The shared description is a reusable view, not an identity link between the
contents. Distinct identities also do not prove statistical independence: both
volumes could have been produced by a shared dispenser or constrained to sum
to a known amount.

Subtracting the exact whole physical parcel leaves zero under a complete
transfer model. Real removal can have retained droplets, loss, inflow or other
process participation; those belong to the action model. Algebraic
cancellation of an observed value does not prove that an uncertain physical
transfer completed.

### 4.2 Source Identity And Observation Identity Are Different

Two observations of the same physical quantity might have the structure

```text
O1 = Q + b + e1
O2 = Q + b + e2
O1 - O2 = e1 - e2
```

Here `Q` is the shared quantity at the same modeled state, `b` a shared bias
with the same coefficient, and `e1`, `e2` distinct observation contributions.
The common contributions cancel; the event-specific ones need not. If the
source or calibration changed between observations, that change must appear
in the relation. Conversely, two references to the exact captured reading
`O1` cancel as `O1 - O1`, regardless of whether `O1` accurately describes `Q`.
An event record also needs the promised captured state: a live mutable reference
alone does not guarantee preservation of its earlier value.

This requires distinguishing at least a reusable category, a physical instance,
its state, an observation event, a shared cause and a read result. These are
roles in connected structure, not necessarily six new primitive classes.

### 4.3 A Small Dependency-Preserving Example

An explanatory affine form is

```text
X = 1 + 0.1 e       where e in [-1, 1]
Y = 1 + 0.1 f       where f in [-1, 1]

X - X = 0
X + X = 2 + 0.2 e
X - Y = 0.1 e - 0.1 f
```

The same `e` means the same unresolved contribution; it is not freshly sampled
at every occurrence. In Gojo this suggests shared slots and contribution
relations. The notation does not replace ordinary Gojo topology with a
canonical expression tree.

Affine arithmetic offers a relevant implementation precedent: shared bounded
terms retain dependencies, while nonlinear operations require additional
enclosures and remainders. It is a possible derived read, not a universal model
of uncertainty. See [the original researchers' overview](https://ic.unicamp.br/~stolfi/EXPORT/projects/affine-arith/Welcome.html).

For probabilistic reads with finite second moments:

```text
Var(X - Y) = Var(X) + Var(Y) - 2 Cov(X, Y)
```

Dropping covariance needs justification. NIST's propagation law explicitly
includes covariance; its general nonlinear formula uses a first-order
approximation. See [NIST's law of propagation](https://www.nist.gov/pml/nist-technical-note-1297/nist-tn-1297-appendix-law-propagation-uncertainty).

Pairwise covariance is itself a limited read: nonlinear dependence may require
the actual relation. An interval is not a standard deviation, a standard
deviation is not a guaranteed bound, and neither establishes a distribution
without further assumptions.

Cancellation also has domain conditions. `X / X = 1` requires `X != 0`.
An untyped open slot is not automatically a finite number, a valid divisor or
a compatible physical quantity.

## 5. Ignored Detail, Locality And Crystallization

Not knowing the number of leaves need not weaken a useful tree read. If that
detail cannot affect the selected question at its promised resolution, there
is no reason to force it into the calculation or explore it immediately.
The structure can retain the source, a carried complement, or a potential
route for another space to inspect. Deliberately discarding detail is also
valid when the retained promise acknowledges that loss.

There are two different situations:

1. All admissible finer completions give the same answer to the selected
   question. The local answer can be exact at that resolution.
2. Omitted participation can affect the answer, but the approximation is useful
   for a chosen purpose, scale and horizon. The read needs those conditions,
   and an effect bound or refinement route when the task requires one.

A planetary-motion view illustrates the second possibility as well as useful
abstraction: a selected model uses a limited set of quantities and influences.
Excluding surface detail or distant influences does not claim their physical
effects are universally zero.

Relevance therefore changes the requested read and allocation of effort, not
the physical truth of omitted participation. A tiny uncertainty near a critical
decision boundary can matter more than a huge unknown that cannot change the
decision. An exploration policy needs consequences and goals as well as
uncertainty; maximizing uncertainty reduction alone is not an adequate law.

Crystallization can stabilize a reusable unit or view promise. It must preserve
the distinction between that promise and an individual sample's unresolved
value. Reusing an “about one cup” view should not make every cup share one
unknown volume. Nor should an approximate quantity make the chosen cup unit
definition itself approximate unless the calibration is also unresolved.

A coarse read such as two hundreds ticks with an exactly retained remainder
of 47 can fully account for a known value 247. The remainder is not random.
A receiver seeing only the coarse ticks has less information, but what it may
infer depends on the read's declared cell and domain; an absent remainder is
not an implicit exact zero or an automatic symmetric error bar.

## 6. Goals Introduce A Different Kind Of Question

A goal can be completely specified even when its feasibility, required action
and eventual outcome remain unknown. Desire defines a target; it supplies no
evidence that the target already holds.

The useful reading of “2 + 2 = 5, with five fixed” is

```text
current a = 2, current b = 2
desired future a' + b' = 5
a' = 2 + delta_a, b' = 2 + delta_b
therefore delta_a + delta_b = 1
```

The equality `2 + 2 = 4` remains exact. The future constraint exposes a one-unit
gap. Its allocation depends on available inputs, legal transformations,
capacity, costs, history and other connected commitments. If both inputs and
the addition relation are fixed, the target is unreachable under those
premises. The system must preserve that conflict rather than report success.

Three processes need different witnesses:

- **Observation or inference:** changes what is justified about a state.
- **Material action:** changes the state through explicit import, transfer or
  settlement, with its accounting and timeline.
- **Model revision:** changes the selected explanation, calibration or
  applicability assumptions.

All can be useful responses to tension. Changing a belief or a desired answer
does not demonstrate that the corresponding material goal was achieved.
Likewise, evaluating a hypothetical transformation is not evidence it occurred.

## 7. Unknown Here, Interpretable Elsewhere

A local space can contribute without understanding the larger concept. It
needs enough of a contract to expose participation, read compatible inputs,
route outputs and preserve unresolved evidence. Another space may interpret
what remains opaque here.

Converting to a common carrier or “raw currency” must itself be an explicit
projection or transfer with a basis and retained promise. Heat, sound and other
sources do not become interchangeable merely because each has a scalar read.
The local view may legitimately discard some distinctions, but should retain
source correspondence when later interpretation depends on them.

There are two separate duplication hazards:

- Material branching must account for accepted, blocked and remaining
  participation without creating conserved quantity.
- Evidence branching must not make one observation into several independent
  observations merely because it returns through several routes.

The second is especially important for a system that learns from local
agreement. Ten spaces echoing one source can provide evidence of distribution
or usefulness, but not ten independent measurements of the world. Returning
feedback should preserve the dependencies needed by its evidence-combination
read. This does not require a global judge or retaining every historical event
forever; a compressed account must retain an appropriate dependency promise or
admit that it no longer supports the stronger inference.

## 8. Proposed Calculation Contract

### 8.1 Preserve Joint Completions, Not Just Separate Error Bars

For compatible quantities with a joint-value model, let `Omega` be the
admissible joint completions of a selected local structure under its declared
premises and evidence. A read `f` should describe `f(Omega)` at its chosen
resolution.
This is a way to state a local law, not a requirement to enumerate every state
or introduce a global solver. It does not assert that every possible question
has a simultaneous hidden scalar value. A probability read additionally needs
its justified law over the selected possibilities; support alone is insufficient.

Independent marginal intervals can overapproximate `Omega`, but lose shared
constraints. For `X - X`, the two occurrences range together along a diagonal,
not independently across a square. The representation should permit local
elimination, exact cancellation and conservative enclosures without erasing
the relation that justifies them.

Keep a specification or target set separate from `Omega`. A target constrains
success; it must not remove inconvenient current possibilities merely because
they are unwanted. Explore reachable future structures through explicit
transformations instead.

If evidence contradicts itself, retain the conflict. If a quantity or operation
has no defined domain, retain that missing premise. These differ from a valid
zero, an empty material remainder and an unbounded set of possible values.

### 8.2 Bounds Need Their Own Arithmetic And Rounding Laws

For `w = w0 + dw`, `h = h0 + dh`, with `|dw| <= a`, `|dh| <= b`:

```text
wh - w0 h0 = h0 dw + w0 dh + dw dh
|wh - w0 h0| <= |h0| a + |w0| b + a b
```

The last term cannot be discarded when claiming this guaranteed bound. For
`w in [4,6]`, `h in [9,11]`, ordinary positive multiplication gives
`wh in [36,66]`. The nominal product is 50, and first-order sensitivity of 15
misses the possible upper deviation of 16. These intervals assert neither a
uniform distribution nor that every pair is achievable if additional joint
constraints exist.

Consequently:

- Unit compatibility, calibration and domains precede numerical combination.
- A support-set read may select numeric lower and upper bounds without erasing
  the source frame's order, orientation or standard. An occupied segment and
  a set of possible unknown values need different connected interpretations.
- Cancellation of unit factors does not by itself cancel uncertain quantities.
  That requires the corresponding shared sources and relations.
- Picking a coarse output tick is a representation policy, not an uncertainty
  propagation theorem.
- A guaranteed enclosure rounded onto integer ticks needs outward endpoint
  rounding. Ordinary point projection with a retained remainder is a different
  read and cannot silently stand in for that law.
- A denominator whose possibilities include zero needs explicit domain
  handling; the result may have an undefined branch, be unbounded, or require
  disconnected alternatives. Do not return a misleading finite interval.
- Shared causes must survive multiplication and other nonlinear operations,
  even if a derived approximation introduces an explicit remainder.
- Additional consistent evidence under a fixed valid model can refine a read.
  Actual state changes, revised assumptions or lost information can instead
  require a wider read and a new correspondence.

### 8.3 Structure To Attach When The Question Needs It

No mandatory giant record is proposed. Compose the relevant participants:

```text
source instance / selected feature / state or observation event
    -> exposed surface and calibration
    -> open quantity or alternative construction
    -> constraints and shared causes, with their supporting premises
    -> selected read and carried complement
    -> result, remainder and provenance

specification or goal -> comparison / experiment / permitted transformation
                     -> witnessed result and timeline
```

This is a design sketch, not a newly accepted constructor spelling. The
connections must explain the reading; a text label such as “tolerance,”
“independent” or “goal” cannot be its sole semantics. Candidate weights,
probabilities, confidence and attention can be additional calibrated reads
when justified. None is a universal replacement for the source structure.

## 9. Current Gojo Support And Boundaries

| Existing mechanism | What it contributes now | What it does not establish |
| --- | --- | --- |
| [`StructuralPattern`](../Patterns/StructuralPattern.cs) and placeholders | Shared slots, partial constructions and explicit binding. | `IsComplete` means slots are bound, not that a claim is true or a quantity is certain. Binding a slot to a span does not automatically enforce a numerical interval law. |
| Carrier, site and tick-reference identity | Distinguishes shared participation from merely equal coordinates. | Sharing a category or a unit does not establish shared sample uncertainty. The graph must connect the appropriate instance, event and cause. |
| [`ProjectionResidue`](../Core/ProjectionResidue.cs) | Retains exact rational participation left by a selected projection. | A retained remainder is not a probability or a generic measurement error bar. |
| [`BoundedWholeSurface`](../ConceptSurface/BoundaryFlowPrimitives.cs) | Connects a selected whole, two boundaries and a partition/resolution witness; tests exercise those reads. | A bounded whole does not by itself mean an unknown sample drawn from the whole or supply a law for propagating its uncertainty. |
| [`ReadWitness`](../Structures/ReadWitness.cs) and [`FocusSurface`](../Structures/FocusSurface.cs) | Connect source, selected read, result, complement and provenance where supplied. | A placeholder for missing evidence is not proof that no complement or uncertainty exists. |
| [`OperationClaim`](../Structures/OperationClaim.cs) | Compares readable participants and exposes residual structure under selected premises. | Does not identify fault or globally verify the model. Unresolved reads retain participants; their sentinel scalars are not uncertainty measures or verdicts in isolation. |
| [`ResistanceValueSurface`](../../GojoCAD/electronics/components/resistors/ResistanceValueSurface.cs) | A concrete nominal series and a separately connected relative tolerance surface. | `RelativeTolerance` does not encode exact ohm endpoints, a probability distribution or a demonstrated conformance bound on a physical sample. |
| [Local blend fixture](GojoLocalBlendFixture.md) | Partial construction, alternative mixtures, goals, explicit response steps and coarse reads. | Does not yet supply a general learning rule, guaranteed interval propagation or automatic assignment of responsibility. |
| [Numeric format](GojoNumericFormat.md) | Preserves encoded graph structure and within-world aliases across restoration. | Cannot invent absent dependency relations or confer uncertainty semantics merely by serializing a graph. |

The crucial distinction is **structural completeness versus certainty**. A
fully constructed bounded-quantity model can remain uncertain; an unfinished
pattern can already answer a particular question exactly. Syntax and numeric
transport need to preserve both situations without conflating them.

The exploratory [streaming-data note](../axioms/ideas/Gojo%20Streaming%20Data%20Frames%20And%20Interpretive%20Graphics.md)
already asks how unread available detail differs from absent detail, and how
predicted detail should retain different provenance from observation. The
distinctions here support that direction without promoting its proposed
reservoir, attention or confidence mechanisms to implemented behavior.

The older [crystallized resolution note](../axioms/Gojo%20Crystallized%20Resolution.md)
has been clarified alongside this reference: choosing the coarsest participating
basis is an output-basis heuristic, not a guaranteed uncertainty bound. This
documentation change does not add propagation behavior to the kernel.

## 10. Recommended Next Fixture

Start with **two bounded quantities and explicitly shared causes**, not an
independent-interval wrapper around every scalar. The first fixture should
already distinguish an instance from its category and its observations.

The fixture should establish these laws before broader adaptive behavior:

1. **Same unknown twice:** valid `X - X` is exactly zero; `X + X` preserves one
   shared contribution with doubled participation.
2. **Two instances of one description:** no automatic cancellation or inference
   of statistical independence; preserve a conservative difference read.
3. **Two observations and a common calibration:** cancel only shared
   contributions with matching coefficients and conditions.
4. **Normative versus evidential bounds:** a nominal stop or permitted tolerance
   does not prove actual size. An allowed positive clearance has zero violation.
5. **Partial versus coarse:** missing fine detail is not zero; retained known
   residue is not random; a requested coarse answer can already be settled.
6. **Nonlinear and domain limits:** conservative products, outward rounding,
   zero-crossing division, incompatible bases and contradictory constraints
   preserve their unresolved participation.
7. **Goal versus achieved change:** an exact one-unit target gap produces a
   proposed or actual witnessed transformation without rewriting addition.
8. **Transport and feedback:** repeat the dependency-sensitive reads after
   numeric restoration; returning an observation through a second route does
   not make it independent evidence.

Later lossy compression can intentionally weaken the retained dependency
promise. That should widen or limit the available reads, not silently preserve
claims whose supporting correspondence was forgotten.

These are mathematical and representational guardrails. Selecting exploration,
allocating resistance to change, learning applicability, and deciding which
history is worth retaining remain local policy questions to develop through
experiments. They should build on the distinctions above rather than replace
them with a single uncertainty score.
