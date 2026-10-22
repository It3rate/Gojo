# Evidence calculations before measured geometry

Exploratory above-kernel fixture, 22 October 2026. The purpose of this round is
to test the calculation laws that an ornament receiver could use, rather than
to train a recognizer or discover how squares are constructed. The connected
Gojo surfaces are the calculation contract; ordinary ratios and percentages
are optional diagnostic reads of those surfaces.

This does not change the kernel's foundational rules. In particular,
observation does not introduce carrier tension, a desired result does not
establish observed truth, and independent identities do not establish
statistical independence.

## Three different questions

A compatibility read asks whether an observation can be accounted for by a
candidate under its connected premises. A parallel-line construction and an
equal-partition circular construction can both account for the same square.
Both can remain useful descriptions. Compatibility does not require an
exclusive choice between them.

A model comparison asks how a particular observation changes the relative
weights of explicitly selected alternatives. Probabilities over alternatives
require a selection basis and an exclusive, exhaustive scope. Without those
premises, weights can still expose a relative preference within the selected
scope, but they do not claim that one of those candidates must be the answer.
Counting two compatible recipes does not itself supply equal prior weights.

An empirical support read asks what a collection of scoped observations says
about a repeatable event rate. Its amount of supporting and opposing
participation is different from the model-comparison weights. A hundred
logical checks derived from one square are not automatically a hundred
independent calibration observations.

## Structural calculation contract

The fixture retains these connected participants where applicable:

```text
alternative scope and selection basis
  -> prior relative participation
  -> observation event and oriented question/response
  -> conditional likelihood under each alternative
  -> admitted or unresolved dependency witness
  -> updated relative participation
  -> selected normalization or coarser read
  -> result, remainder and provenance
```

An event is not just a feature name. Its source instance, observation occasion,
selected read and conditions distinguish a new observation from a second route
to the same observation. A likelihood concerns that event under a particular
candidate and its retained conditions. It is not a free confidence score.

The algebra being tested is ordinary finite Bayesian conditioning under the
declared premises. The experiment does not invent an alternative arithmetic
law. Its exploratory work is expressing the premises, factors, unknowns and
revisions as sparse native participation before measured geometry is needed.

The model calculation is implemented in
[`StatisticalEvidence.cs`](../../experiments/QualitativeExecution/StatisticalEvidence.cs).
The separate empirical-support interpretation is implemented in
[`EvidenceSupport.cs`](../../experiments/QualitativeExecution/EvidenceSupport.cs).
Neither mechanism automatically discovers likelihoods from ornament labels or
geometry.

The native model surface is deliberately narrow:

```text
frame = (one selector -> Closed(alternatives)) -> (count unit -> selection basis)
partition = (unit -> justification) -> (selected repetitions -> whole repetitions)
question = ((claim -> contrary) -> Closed(claim, contrary)) -> context
observation = (event -> question) -> (question orientation -> response direction)
factor = (observation -> (conditioned events -> independent events))
         -> (Closed(model -> positive-claim partition) -> premise)
state = (frame -> Closed(model -> prior partition)) -> Closed(admitted factors)
```

A posterior read retains each model's ordered product of prior and event
partitions, then selects its share of the whole sum of products. These native
expressions survive transport. The host `BigInteger` fractions compile a read
of that participation; they are not serialized as a second belief graph.
Both quantity and exact zero are represented by actual repeated participation
and closed scopes. Open participation does not turn into zero.

## Updating relative alternatives

For an admitted event `E` and a candidate `H`, the diagnostic law is:

```text
updatedWeight(H) = previousWeight(H) * likelihood(E | H, retainedConditions)
```

Normalization is a separate read over the selected alternative scope. For two
alternatives, their posterior odds are their prior odds multiplied by the
likelihood ratio. This is the standard distinction between prior preference
and the discriminating information supplied by the observation.
[Kass and Raftery, Bayes Factors](https://www.stat.cmu.edu/~kass/papers/bayesfactors.pdf).

Equal likelihoods preserve relative odds. Even a hundred admitted observations
that are equally expected under both constructions do not favour one over the
other. Their observation histories and absolute compatibility can still
matter. If both candidates regard an observation as improbable, a relative tie
does not make either a good account of the situation.

The fixture therefore also exposes `EvidenceProbability`, the prior-weighted
probability of the complete accepted report history within the selected model
scope:

```text
priorMass = sum(previously selected initial model weights)
jointMass = sum(initial weight(H) * conditional report factors for H)
EvidenceProbability = jointMass / priorMass
```

The native `EvidenceExpression` is an ordered correspondence from the group of
prior products to the group of report-weighted joint products. Each joint
product retains the same initial prior participant plus its report factors.
It is different from a posterior query's selected-products-within-whole
grammar. Scaling every initial weight equally changes neither the normalized
posterior nor this report probability. Two candidates can remain half-and-half
while the report probability is `1/100`, rather than `1`.

This joint report probability is not a universal aesthetic or anomaly score.
A long sequence can have very small joint probability even under a good
model. A next-report question uses its conditional predictive probability,
which can be read as the ratio of successive joint report probabilities when
that ratio's domain is established. The event partition and reporting basis
remain part of its interpretation.

A single observation can be decisive when the alternatives predict it very
differently. It is not averaged as one more generic successful check. An exact
zero likelihood means incompatibility under the supplied premises; a small
nonzero likelihood permits later recovery. A reliability model can make an
apparently contrary report possible without silently relaxing a construction's
necessary promises.

An exact zero prior is also consequential: ordinary finite conditioning does
not revive it. Reopening that alternative requires an explicit revision of the
prior or the selected model scope. The previous calculation remains a valid
read under its earlier premises.

## Orientation and response

A question can face either retaining or rejecting a claim. Its response can
affirm or oppose that selected orientation:

| Question orientation | Response | Underlying contribution |
| --- | --- | --- |
| Retain | Affirm | Towards the claim |
| Retain | Oppose | Away from the claim |
| Reject | Affirm | Away from the claim |
| Reject | Oppose | Towards the claim |

Reversing both the question orientation and response preserves the underlying
observation. This correspondence must not produce a second observation or
additional evidential strength. An unanswered question remains open; it is not
an opposed response.

Complementing an event likelihood is justified only within the same exhaustive
binary event partition. A negative report about one claim is not automatically
a positive report about an unrelated alternative. These question faces also do
not stand in for geometric direction, frame reversal or a carrier's flow face.

## Dependence, repetition and conditional evidence

Returning the same observation through two ports supplies one observation.
Repeated route structure remains inspectable, but it does not multiply the
likelihood again. Distinct source or event identities alone do not authorize
multiplication either: the sources may share an unresolved cause.

Placeholder identity includes its native slot and face, rather than its host
object reference. A double dual is the same participant and cannot create an
extra event, alternative or unit. Its genuinely opposed face stays distinct.
Equal-looking relation structures do not automatically become one identity.

For successive observations the general law uses conditional factors:

```text
P(E1, E2 | H) = P(E1 | H) * P(E2 | H, E1)
```

An independence declaration is one possible justification for replacing the
second factor with `P(E2 | H)`. A supplied joint model or a likelihood explicitly
conditioned on the retained event history is another. Missing dependence
information remains unresolved rather than being interpreted as independence.
Exact updates commute only when their selected factorization justifies that
read; arbitrary context-sensitive factors cannot be reordered freely.

The reader checks that each previously admitted event is accounted for by a
conditional or independence premise. It does not prove that a supplier's
independence assertion is causally correct, or reconstruct a coherent joint
model for unoffered branches of a conditional table. The reference tests supply
coherent tables and compare their alternative chain factorizations explicitly.

## Balance, empirical concentration and publication resolution

An empirical binary-support interpretation may retain positive prior
participation `a` and `b`, then incorporate admitted supporting observations
`s` and opposing observations `f`:

```text
posterior support = (a + s, b + f)
mean = (a + s) / (a + b + s + f)
concentration = a + b + s + f
```

This is a selected beta/Bernoulli interpretation, not the meaning of every
native quotient. The [Stan mean/concentration parameterization](https://mc-stan.org/docs/functions-reference/continuous_distributions_on_0_1.html#beta-proportion-distribution)
provides a numerical reference for the distinction. Prior pseudo-participation
and acquired observations retain separate provenance even when this read adds
them.

`(1,1)` and `(50,50)` have the same mean but different concentration under that
contract. One additional supporting observation moves them to means `2/3`
and `51/101`, respectively. In contrast, merely displaying an unchanged
half-and-half read as `1/2` or `50/100` creates no concentration. Likewise,
model odds `1:1` and `50:50` are the same relative preference unless a separate
support interpretation supplies the evidential difference.

The uncertainty of an unknown repeatable event rate is also different from
the unpredictability of the next event. Knowing a fair coin's rate extremely
well does not make its next outcome predictable. A low-resolution publication
can deliberately omit distinctions even when its supplier has precise evidence.
No denominator alone establishes which of these situations applies.

## Unknowns, zero and failed scopes

An unknown likelihood is an open operand. It is neither zero, one half nor an
uninformative unit factor. A reader may preserve unaffected relative
comparisons when its declared contract permits them, but it must not publish a
fully resolved posterior by inventing the missing factor.

If all selected candidates have zero updated weight, normalization fails.
This is evidence against the combined model scope, observation contract or
premises. Returning equal probabilities would hide the failure. Coarsening a
small positive value to a displayed zero must retain its remainder or loss; it
must not manufacture exact impossibility.

## Reliability and revision

A report's reliability needs a connected meaning. For a binary report channel,
one explicit interpretation is sensitivity `s` and false-positive rate `f`.
If a candidate predicts an underlying event with likelihood `q`, the positive
report likelihood is:

```text
P(report+ | H) = s*q + f*(1-q)
```

Equal sensitivity and false-positive rates make the report uninformative
between candidates. A systematically inverted report can still be informative.
It is different from an absent or unknown report. An unexplained strength
multiplier or exponent is not automatically a Bayesian reliability model.
The current helper does not infer or automatically execute that report
channel. Its supplied conditional tables can represent a channel calculated
elsewhere. Empirical support counts admitted report outcomes; it does not
correct those counts for source mistakes or noise.

Changing the prior, a construction premise or the report channel creates a new
calculation context with an explicit correspondence to the old one. Earlier
hypotheses and results need not be deleted. Evidence used to invent or tune a
candidate must not then be counted as independent validation of that same
candidate. A replay or held-out observation contract can distinguish those
uses.

`RevisePrior` currently changes prior participation within the same finite
candidate frame, retaining the observation factors and the previous state in
its receipt. It does not invent a new construction or rewrite a zero-likelihood
factor. Broader structural revision needs its own correspondence and updated
predictions. In particular, a hard exclusion cannot be undone by merely
changing a nonzero prior.

## The square fixture tests the law

The initial square is deliberately compatible with multiple constructions.
The parallel-family construction can expose independent separation changes;
an equally partitioned circular construction can expose a partition change.
A rectangle or pentagon continuation is a controlled likelihood fixture under
those exact retained promises. The recipe's label does not determine its
predictions. For example, relaxing equal circular partition can admit a
rectangle, and that relaxation must remain visible.

The test is whether common checks leave the balance unchanged, a genuinely
discriminating continuation changes it as specified, aliases do not fabricate
independence, unknowns remain open, and premise revision preserves the earlier
calculation. It is not evidence that the receiver has learned either
construction from an image.

The same mechanism can later qualify collinearity, attachment, closure,
equivalence or gap-relative-to-segment relations. Such inputs can be
dimensionless and uncalibrated. Their structural or geometric estimation law
still needs its own justification; probability algebra does not discover that
law for free.

## Results

The retained run contains **16 cases and 53 passing calculation checks**.
The recorded calculation/archive phase took **2.972 seconds**, and the complete
CLI run including page output took **3.070 seconds** on the local machine while
other checks were running. This is a small correctness fixture, not a speed
benchmark or evidence of trained improvement.

Open the [interactive calculations](../../experiments/QualitativeFrieze/results/evidence-calculation/gallery.html),
[single-page overview](../../experiments/QualitativeFrieze/results/evidence-calculation/overview.html)
or [run summary](../../experiments/QualitativeFrieze/results/evidence-calculation/summary.json).

| Cases | Tested consequence |
| --- | --- |
| E01 | Four question/response orientations give the intended corresponding evidence. |
| E02–E05 | 100 common square checks leave odds tied; a supplied 99:1 continuation produces 99:1 odds; exact zero and a partition-change context remain distinct. |
| E06–E08 | A repeated route is not another event; unknown likelihood remains open; all-zero model support fails rather than normalizing to a tie. |
| E09–E11 | Rewriting a half as 50/100 creates no support; declared independent updates commute; missing dependence information blocks the new calculation. |
| E12–E13 | A zero-weight candidate is retained but cannot revive under unchanged premises; coarse publication encloses a probability without changing it. |
| E14 | No acquired history versus 98 balanced acquisitions gives equal means, different latent-rate dispersion, and different responses to one further event. |
| E15 | Explicit prior revision changes the selected comparison while preserving earlier evidence and results. |
| E16 | Equal posterior preference can coexist with low probability for the observed report under the entire selected model scope. |

E14 is the key concentration control. Starting with prior support `(1,1)`,
49 positive and 49 opposed acquisitions yield `(50,50)`. The mean remains
`1/2`, while the latent-rate variance changes from `1/12` to `1/404`. One more
positive event gives `2/3` without that history and `51/101` with it. The
variance of one next binary outcome is still `1/4` in both balanced states;
knowing a rate better has not made the individual event predictable.

The focused suite passes **52 tests**, including **135 independent exact
finite-partition oracle combinations**, three- and 100-alternative scopes,
coherent conditional chain factorizations, malformed/open domain rejection,
native identity, alias transport and unreduced empirical support. The browser
audit passes 22 states and 35 local references with zero errors, checking all
31 diagrams and the 52,049-node native receipt archive. Solution-wide
`dotnet test Gojo.slnx` passes **5,073 tests**, with no failures or skips. The
qualitative test project completes in 6 minutes 43 seconds in that run.

## Present boundaries

This is a reusable exploratory law fixture above the kernel. It does not yet
replace the ornament search's ordinal critic or learn its input likelihoods.
The input partitions declare a selected measure; counting compatible recipes
does not establish that measure. Likelihood uncertainty beyond the supplied
exact partitions remains open rather than receiving an invented estimate.

Native transport preserves aliases inside the restored packet. It does not
identify separately imported external reports without a supplied correspondence.
Dependence discovery, noise-channel estimation, dynamic model-family growth,
utility/cost calibration and general structural-prior mutation remain open.
The empirical fixture currently uses positive integer prior pseudo-participation
and one full contribution per admitted acquisition. Fractional support,
reliability-weighted acquisitions and arbitrary concentration scales need an
explicit additional calibration; they cannot be inferred from the number of
display partitions or an ordinal strength label.

An unaccepted update leaves the last complete state available, while its full
receipt retains the offered event and failure or missing premise. That old
state is not a posterior incorporating the unaccepted event. An incomplete
empirical read similarly exposes a calculation conditional on admitted reports,
alongside its conflicts and unresolved reports; `Complete` is false.

Output coarsening here publishes enclosing partitions of an exact small read.
It does not claim low-precision execution throughout, automatic query-budget
selection, or learned error bars. The frame, factor history and unresolved
participation make those later interpretations possible without changing the
laws tested in this round.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --evidence-calculation
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~EvidenceModelMathTests|FullyQualifiedName~EvidenceSupportTests|FullyQualifiedName~EvidenceScopeTests"
node experiments/QualitativeFrieze/tools/check-evidence-calculation.cjs
dotnet test Gojo.slnx
```
