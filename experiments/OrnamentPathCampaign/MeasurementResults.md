# Which judges affected the drawings?

A criterion can be requested yet have no evidence to read. It can also reward the same change as several other criteria. The following counts distinguish availability from occasions when a criterion actually tipped a search decision.

Each row pools the five full briefs and four search replicates. **Available** counts the 480 blind full-finalist drawings on which the criterion supplied a reading. **Compared** counts single-parent training mutations on which the parent or child supplied evidence. **For** means the child beat its parent on the full score but would tie or lose without this criterion. **Against** is the analogous deflection of a losing child. These are pivotal numerical comparisons, including proposals that another elite eventually displaced. The independent training omissions below address a different question.

The final column is a separate, post-hoc visual check. Of 14 unanimous non-tied choices on a fresh panel, how often did the higher individual reading agree with the observers? Only pairs with both readings present and a difference of at least 0.02 enter that criterion's denominator. These small, overlapping samples concern two AI observers and selected pairs.

| Perspective | Criterion | What it reads | Available | Compared | For | Against | Visual agreement |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| craft | no-overdraw | Unique drawn length as a fraction of deposited length. | 480/480 | 5,440 | 40 | 56 | 6/9 |
| craft | no-crossings | Share of edges free from proper interior crossings. | 480/480 | 5,440 | 2 | 7 | 1/3 |
| craft | no-false-joins | Share of edges free from unbound tips touching other ink. | 480/480 | 5,440 | 73 | 99 | 4/8 |
| craft | forward-turns | Continuation at connected consecutive strokes without doubling back. | 480/480 | 5,440 | 1 | 10 | 4/6 |
| rhythm | period-match | Agreement of finite ink support one measured period apart. | 480/480 | 5,440 | 8 | 40 | 4/12 |
| rhythm | turn-recurrence | Connected turn phrases that recur across executed cells. | 440/480 | 5,288 | 3 | 19 | — |
| rhythm | turn-variety | Diversity of signed turns among actual connected strokes. | 480/480 | 5,440 | 12 | 22 | 3/5 |
| rhythm | extent-contrast | Moderate variation of stroke extent. | 480/480 | 5,440 | 28 | 26 | 2/7 |
| rhythm | accent-alternation | Fraction of connected triples alternating shorter and longer strokes. | 464/480 | 5,413 | 48 | 39 | 5/11 |
| field | band-reach | Fraction of the strip length reached by the horizontal projection of ink. | 480/480 | 5,440 | 3 | 1 | 1/2 |
| field | register-balance | Length centroid near the midpoint of the drawn vertical span. | 480/480 | 5,440 | 22 | 52 | 4/12 |
| field | breathing-room | Clearance of neighboring strands on transverse sections. | 480/480 | 5,439 | 3 | 10 | 2/8 |
| field | gap-evenness | Consistency of adjacent gaps observed on transverse sections. | 480/480 | 5,439 | 28 | 25 | 2/12 |
| field | density-window | Unique stroke length per area at the selected period, in a moderate range. | 480/480 | 5,440 | 15 | 13 | 8/10 |
| field | weight-contrast | Moderate alternation in line mass over successive quarter-periods. | 480/480 | 5,440 | 8 | 6 | 3/7 |
| field | frame-respect | Fraction of unique line length lying inside the declared horizontal frame. | 480/480 | 5,440 | 10 | 18 | 5/12 |
| symmetry | parallel-mirror | Reflection across the horizontal midline of the occupied band. | 480/480 | 5,440 | 39 | 65 | 6/13 |
| symmetry | end-mirror | Reflection across the vertical midline of the complete frame. | 480/480 | 5,440 | 14 | 11 | 5/12 |
| symmetry | half-turn | A half-turn around the horizontal and vertical midlines. | 480/480 | 5,440 | 35 | 33 | 5/13 |
| symmetry | half-glide | A half-period shift combined with a horizontal reflection. | 480/480 | 5,440 | 27 | 43 | 5/12 |
| topology | connected-run | Consecutive marks of one owner sharing the actual endpoint. | 480/480 | 5,440 | 25 | 56 | 6/13 |
| topology | branch-use | Cells containing a witnessed branch at a material fork. | 480/480 | 5,440 | 9 | 20 | 6/8 |
| topology | merge-use | Cells containing a witnessed merge into a participating destination. | 480/480 | 5,440 | 21 | 22 | 9/10 |
| topology | loop-use | Cells completing an identity-connected cycle of distinct drawing edges. | 480/480 | 5,440 | 4 | 15 | 6/6 |
| topology | owner-dialogue | Cells with distinct owners incident on the same material site. | 456/480 | 5,376 | 6 | 2 | 7/8 |
| seam | boundary-match | Opposite cell faces presenting ink at matching heights. | 480/480 | 5,424 | 8 | 14 | 7/9 |
| seam | tangent-match | Opposite cell faces matching both height and supporting slope. | 480/480 | 5,424 | 8 | 14 | 7/9 |
| seam | shared-continuation | Opposite cell faces sharing their actual endpoint identity. | 480/480 | 5,424 | 13 | 18 | 8/9 |

## What the combinations selected

Here the full utility of every frozen machine is read under its named brief on the same 24 held-out realizations. Each mean includes four independently selected finalists. Each independently searched arm received the same number of paid proposals and selected its own finalist under its training rule. The foundation and runtime interventions are controls.

| Selection perspective | quiet-border | turning-key | countermarch | branched-spray | stepped-lace |
| --- | ---: | ---: | ---: | ---: | ---: |
| full | 0.861 | 0.885 | 0.831 | 0.841 | 0.838 |
| foundation | 0.815 | 0.680 | 0.638 | 0.716 | 0.702 |
| random | 0.821 | 0.835 | 0.673 | 0.799 | 0.744 |
| unseeded | 0.845 | 0.862 | 0.758 | 0.842 | 0.788 |
| field | 0.810 | 0.570 | 0.448 | 0.581 | 0.590 |
| craft+field | 0.797 | 0.453 | 0.445 | 0.527 | 0.554 |
| seam+rhythm | 0.797 | 0.872 | 0.758 | 0.637 | 0.679 |
| topology+craft | 0.768 | 0.587 | 0.500 | 0.779 | 0.700 |
| symmetry+rhythm | 0.759 | 0.698 | 0.835 | 0.479 | 0.655 |
| omit-craft | 0.845 | 0.861 | 0.825 | 0.813 | 0.777 |
| omit-rhythm | 0.831 | 0.866 | 0.793 | 0.819 | 0.716 |
| omit-field | 0.861 | 0.881 | 0.843 | 0.821 | 0.808 |
| omit-symmetry | 0.849 | 0.871 | 0.697 | 0.838 | 0.810 |
| omit-topology | 0.901 | 0.861 | 0.834 | 0.687 | 0.702 |
| omit-seam | 0.812 | 0.730 | 0.794 | 0.788 | 0.803 |
| cut | 0.729 | 0.660 | 0.834 | 0.548 | 0.602 |
| frozen | 0.861 | 0.885 | 0.831 | 0.829 | 0.838 |
| open | 0.861 | 0.885 | 0.831 | 0.824 | 0.838 |

Cut, frozen and open change the same full winner at execution. Foundation is validation selection from six supplied starting programs. See the [criterion definitions and source evidence](Criteria.md) for what each judgment can establish.
