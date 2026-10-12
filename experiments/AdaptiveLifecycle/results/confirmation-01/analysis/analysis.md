# Continuous adaptation analysis

8 seeds; 98,304 evaluation decisions; 112.3 seconds.

Means across seeds. Intervals are descriptive Student-t 95% intervals over seeds, not individual steps. Online arms receive extra feedback during evaluation. No multiple-comparison correction.

| Arm | Correct | Action coverage | Paid probes | Net return | Longest hold (mean) |
|---|---:|---:|---:|---:|---:|
| always-probe | 81.52% | 81.52% | 100.00% | 4.522 | 4.2 |
| clean-trained/frozen | 2.40% | 2.51% | 0.30% | 0.173 | 1995.1 |
| clean-trained/online | 93.68% | 97.41% | 12.99% | 6.788 | 3.4 |
| missing-trained/frozen | 95.12% | 99.02% | 4.83% | 7.045 | 3.2 |
| missing-trained/online | 93.87% | 97.74% | 11.58% | 6.812 | 3.2 |
| retry-if-unavailable | 93.25% | 96.56% | 18.62% | 6.690 | 3.1 |

## Paired contrasts

- clean-trained/online minus clean-trained/frozen: net return +6.615, 95% interval [6.406, 6.825]; coverage +94.90%.
- missing-trained/frozen minus clean-trained/frozen: net return +6.872, 95% interval [6.662, 7.082]; coverage +96.51%.
- missing-trained/online minus missing-trained/frozen: net return -0.232, 95% interval [-0.257, -0.207]; coverage -1.28%.
- clean-trained/online minus retry-if-unavailable: net return +0.098, 95% interval [-0.156, 0.352]; coverage +0.84%.
- missing-trained/frozen minus retry-if-unavailable: net return +0.355, 95% interval [0.113, 0.597]; coverage +2.45%.

## Verification

48 rows, 98,304 steps, 127 current source hashes, 6 actual saved archive checks.

First-publication recovery is conditional and does not establish sustained useful recovery. A new failed probe supersedes the prior interval as unresolved. See JSON for denominators and eligible-seed counts.

## Warnings

None.
