# What freezing, retry and retirement mean

A frozen manager still senses, remembers, ages its last observation and acts.
We only stop changing its learned evidence and stop exploratory trials during
evaluation. This lets us distinguish a learned capability from learning that
happens while measuring it.

In the previous failure, a probe returned no information. The manager encountered
an untrained “information unavailable” situation. With no evidence for either
choice, its frozen default reused the absent information. The consumer then held,
and no new learning or retry broke the cycle.

We have now tested this using the existing Gojo market, on eight fresh seeds and
98,304 measured decisions:

| Manager | Able to act | Net resource per step |
|---|---:|---:|
| Trained on clean signals, then frozen | 2.5% | 0.17 |
| Same starting manager, keeps learning | 97.4% | 6.79 |
| Trained with missing signals, then frozen | 99.0% | 7.04 |

The learner can therefore acquire retry; the original failure was not evidence
that its available operations were incapable of it. Ongoing learners receive
2,048 extra feedback updates during this comparison.

Continued learning is not always better. Once trained with missing signals,
leaving exploration on reduced net resource by 0.23 per step. The next useful
question is how a local structure recognizes that it knows enough, and reopens
learning when its situation changes.

Separately, we built a local maintenance and replacement demonstration. Reserves
pay an elapsed cost even if a participant does nothing. When it cannot pay, its
supplied connection withdraws, while its structure and unpaid draw remain
inspectable. A funded nursery can release that slot and create an independent
participant from a supplied seed. Finite capacity and missing funds block birth;
unpaid maintenance remains in aggregate history.

The swimming tank already supports bounded circuit growth/pruning, paid trials,
parental component selection, funded breeding and death. The small manager study
has fixed ports and an authored operation grammar, with native rows and evidence
that grow through learning. We have not yet shown a population autonomously
inventing better architectures.

The main distinction is that **learning can acquire retry; retirement creates
room**. We need both to cooperate, with construction and computation priced, and
then compare the combination against learning or replacement alone. Killing
failed participants cannot by itself establish intelligence or useful evolution.

The lifecycle demonstration is separate from the learning comparison. It funds
new reserves, while cloning and construction work are still unpriced. Withdrawal
stops one supplied connection and preserves the body; it does not destroy an
organism or prove that all its internal work has stopped.

All 2,200 repository tests passed. Six saved native model archives were reloaded
and checked, with no analysis warnings. The confirmation took about 112 seconds
and used no paid Jev calls.

[Full report](REPORT.md) · [Run instructions](README.md)
