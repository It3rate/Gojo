# Continuous adaptation analysis

2 seeds; 24,576 evaluation decisions; 41.6 seconds.

Means across seeds. Intervals are descriptive Student-t 95% intervals over seeds, not individual steps. Online arms receive extra feedback during evaluation. No multiple-comparison correction.

| Arm | Correct | Action coverage | Paid probes | Net return | Longest hold (mean) |
|---|---:|---:|---:|---:|---:|
| always-probe | 79.59% | 79.59% | 100.00% | 4.367 | 3.5 |
| clean-trained/frozen | 2.25% | 2.25% | 2.17% | 0.136 | 1994.0 |
| clean-trained/online | 92.87% | 96.92% | 14.26% | 6.658 | 3.5 |
| missing-trained/frozen | 94.53% | 98.80% | 5.47% | 6.940 | 2.5 |
| missing-trained/online | 93.38% | 97.49% | 12.06% | 6.737 | 3.0 |
| retry-if-unavailable | 85.18% | 86.74% | 65.31% | 5.321 | 3.5 |

## Paired contrasts

- clean-trained/online minus clean-trained/frozen: net return +6.522, 95% interval [6.417, 6.627]; coverage +94.68%.
- missing-trained/frozen minus clean-trained/frozen: net return +6.804, 95% interval [6.488, 7.121]; coverage +96.56%.
- missing-trained/online minus missing-trained/frozen: net return -0.203, 95% interval [-0.575, 0.169]; coverage -1.32%.
- clean-trained/online minus retry-if-unavailable: net return +1.337, 95% interval [-11.176, 13.851]; coverage +10.18%.
- missing-trained/frozen minus retry-if-unavailable: net return +1.620, 95% interval [-10.472, 13.712]; coverage +12.06%.

## Verification

12 rows, 24,576 steps, 127 current source hashes, 0 actual saved archive checks.

First-publication recovery is conditional and does not establish sustained useful recovery. A new failed probe supersedes the prior interval as unresolved. See JSON for denominators and eligible-seed counts.

## Warnings

- Current source differs from recorded source: experiments/AdaptiveLifecycle/LifecycleSurface.cs
- Current source differs from recorded source: experiments/AdaptiveLifecycle/Program.cs
