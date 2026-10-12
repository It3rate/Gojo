# Primitive learning: saved-run analysis

Intervals are two-sided 95% Student t intervals over independent seed means or paired seed differences. Episodes are not treated as independent replicates. Intervals describe this seed population; they are not corrected for multiple exploratory comparisons or a guarantee outside the authored task.

## persistent-fresh

Source: `D:\source\repos\It3rate\Gojo-phototaxis\artifacts\primitive-learning\persistent-fresh`. 4 seeds starting at 100; 1536 training and 768 test episodes per run.

### Main test results

| Variant | Return [95% CI] | Correct | Coverage | Covered accuracy | Query rate |
|---|---:|---:|---:|---:|---:|
| A0P0Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P0Q0S1 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P0Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A0P0Q1S1 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A0P1Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P1Q0S1 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P1Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A0P1Q1S1 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P0Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P0Q0S1 | 1.35 [0.32, 2.39] (n=4) | 66.93% [53.95, 79.91] (n=4) | 100.00% [100.00, 100.00] (n=4) | 66.93% [53.95, 79.91] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P0Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P0Q1S1 | 3.32 [3.29, 3.35] (n=4) | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P1Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P1Q0S1 | 1.43 [0.67, 2.20] (n=4) | 67.94% [58.33, 77.54] (n=4) | 100.00% [100.00, 100.00] (n=4) | 67.94% [58.33, 77.54] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P1Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P1Q1S1 | 3.32 [3.29, 3.35] (n=4) | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| always-query | 3.00 [3.00, 3.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) |
| inverted-fit | -4.32 [-4.34, -4.29] (n=4) | 2.25% [2.14, 2.35] (n=4) | 100.00% [100.00, 100.00] (n=4) | 2.25% [2.14, 2.35] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| inverted-flow | -4.32 [-4.34, -4.29] (n=4) | 2.25% [2.14, 2.35] (n=4) | 100.00% [100.00, 100.00] (n=4) | 2.25% [2.14, 2.35] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| neutral-alternating | 0.00 [-0.23, 0.23] (n=4) | 50.03% [47.14, 52.93] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.03% [47.14, 52.93] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| neutral-hold | 0.00 [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| neutral-left | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| never-query | 1.86 [1.54, 2.18] (n=4) | 73.24% [69.23, 77.25] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.24% [69.23, 77.25] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| no-communication | -0.50 [-0.53, -0.47] (n=4) | 0.00% [0.00, 0.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| shuffled-feedback | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| simultaneous-query | 3.32 [3.29, 3.35] (n=4) | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| whole-route-credit | 2.63 [0.74, 4.51] (n=4) | 92.19% [72.09, 112.29] (n=4) | 100.00% [100.00, 100.00] (n=4) | 92.19% [72.09, 112.29] (n=4) | 74.97% [28.97, 120.97] (n=4) |

### Prespecified paired primary contrasts

Positive differences favor the full controller; each contrast uses matching seed tapes.

| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |
|---|---:|---:|---:|
| A1P1Q1S1 versus A0P0Q0S0 | 3.31 [3.14, 3.49] (n=4) | 47.66 pp [45.34, 49.97] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus neutral-hold | 3.32 [3.29, 3.35] (n=4) | 97.75 pp [97.65, 97.86] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus neutral-left | 3.31 [3.14, 3.49] (n=4) | 47.66 pp [45.34, 49.97] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus neutral-alternating | 3.32 [3.09, 3.55] (n=4) | 47.72 pp [44.89, 50.55] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus always-query | 0.32 [0.29, 0.35] (n=4) | -2.25 pp [-2.35, -2.14] (n=4) | -50.20 pp [-52.94, -47.45] (n=4) |
| A1P1Q1S1 versus A1P0Q1S1 | 0.00 [0.00, 0.00] (n=4) | 0.00 pp [0.00, 0.00] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |

### Factorial contrasts

k-factor alternating sum of 2^k marginal cell means; remaining factors equally averaged. Main effects=on minus off; AS=difference of differences; APQS=fourth-order cell contrast.

| Contrast | Return [95% CI] | Correct-rate contrast |
|---|---:|---:|
| A | 1.30 [1.12, 1.47] (n=4) | 16.25 pp [14.06, 18.43] (n=4) |
| P | 0.01 [-0.14, 0.16] (n=4) | 0.13 pp [-1.71, 1.96] (n=4) |
| Q | 0.11 [-0.04, 0.26] (n=4) | 7.58 pp [5.41, 9.76] (n=4) |
| S | 1.30 [1.12, 1.47] (n=4) | 16.25 pp [14.06, 18.43] (n=4) |
| AS | 2.60 [2.25, 2.95] (n=4) | 32.50 pp [28.12, 36.87] (n=4) |
| APQS | -0.08 [-1.26, 1.09] (n=4) | -1.01 pp [-15.69, 13.68] (n=4) |

### Isolated primitives

| Primitive / control | Correct | Coverage | Accuracy when covered | Return |
|---|---:|---:|---:|---:|
| distinctions / frozen-initial | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | unavailable | 0.00 [0.00, 0.00] (n=4) |
| distinctions / learned | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 0.00 [0.00, 0.00] (n=4) |
| distinctions / shuffled-feedback | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | unavailable | 0.00 [0.00, 0.00] (n=4) |
| prediction / frozen-initial | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | unavailable | 0.00 [0.00, 0.00] (n=4) |
| prediction / learned | 73.24% [69.23, 77.25] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.24% [69.23, 77.25] (n=4) | 0.00 [0.00, 0.00] (n=4) |
| prediction / shuffled-feedback | 50.46% [21.47, 79.44] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.46% [21.47, 79.44] (n=4) | 0.00 [0.00, 0.00] (n=4) |
| queries / frozen-initial | 73.08% [70.86, 75.30] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.08% [70.86, 75.30] (n=4) | 1.85 [1.67, 2.02] (n=4) |
| queries / learned | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 3.32 [3.29, 3.35] (n=4) |
| queries / shuffled-feedback | 73.08% [70.86, 75.30] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.08% [70.86, 75.30] (n=4) | 1.85 [1.67, 2.02] (n=4) |
| programs / frozen-initial | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.01 [-0.18, 0.20] (n=4) |
| programs / learned | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 4.00 [4.00, 4.00] (n=4) |
| programs / shuffled-feedback | 43.72% [-7.62, 95.06] (n=4) | 100.00% [100.00, 100.00] (n=4) | 43.72% [-7.62, 95.06] (n=4) | -0.50 [-4.61, 3.60] (n=4) |

Isolated learned predictor: predictable cue0 coverage 100.00% [100.00, 100.00] (n=4), accuracy when covered 95.52% [95.16, 95.88] (n=4); unreliable cue1 coverage 100.00% [100.00, 100.00] (n=4), accuracy when covered 50.83% [44.65, 57.02] (n=4). Cue0 denotes a predictable relationship, which can be reversal or repetition; it does not imply physical persistence.

### Frozen test shifts

| Condition | Return [95% CI] | Change from baseline | Correct | Query rate |
|---|---:|---:|---:|---:|
| changed-fit | 2.58 [2.51, 2.65] (n=4) | -0.74 [-0.84, -0.64] (n=4) | 88.48% [87.30, 89.65] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| expensive-query | 1.33 [1.19, 1.47] (n=4) | -1.99 [-2.10, -1.88] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| less-predictable | 2.36 [2.16, 2.56] (n=4) | -0.96 [-1.18, -0.75] (n=4) | 85.71% [83.03, 88.39] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| missing-and-absent | 2.90 [2.85, 2.96] (n=4) | -0.42 [-0.45, -0.39] (n=4) | 89.49% [88.80, 90.17] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| novel-loads | 2.86 [2.81, 2.90] (n=4) | -0.47 [-0.54, -0.39] (n=4) | 91.93% [91.08, 92.77] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| reversed-flow-law | -0.29 [-0.46, -0.11] (n=4) | -3.61 [-3.81, -3.40] (n=4) | 52.64% [50.13, 55.15] (n=4) | 49.80% [47.06, 52.55] (n=4) |

### Query placement, work and retained graph

Full query rate in the predictable cue0 context: 0.00% [0.00, 0.00] (n=4); unreliable cue1 context: 100.00% [100.00, 100.00] (n=4).

Deployment comparisons/episode: 1.51 [1.48, 1.54] (n=4); fit probes/episode: 0.00 [0.00, 0.00] (n=4); shadow actions/episode: 0.00 [0.00, 0.00] (n=4).

Full graph: carriers 41486.75 [40883.69, 42089.81] (n=4); relations 435046.00 [429622.56, 440469.44] (n=4); placeholders 624.50 [598.69, 650.31] (n=4); coordinate occurrences 198427.75 [196267.94, 200587.56] (n=4). Active landmarks 2.00 [2.00, 2.00] (n=4); predictor rows 4.00 [4.00, 4.00] (n=4); accepted edits 3.25 [0.24, 6.26] (n=4); selected complexity 8.00 [8.00, 8.00] (n=4).

Seed 100 native archive sizes (transport bytes, not process memory): composite.gojo: 16,446,107; distinctions.gojo: 125,758; predictor.gojo: 55,698; program.gojo: 14,898,863; query.gojo: 3,025,766.

### Private encoding checks

- encoding-1: 4 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.
- encoding-2: 4 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.

### Ideal reference and scope

Balanced contexts; known fit distinction; fixed relation law; query gives exact current direction; no missing/absence. These are model expectations, not measurements.

Expected selective-query return 3.300; always-query 3.000; learned forecast without queries 1.800; fixed previous-direction without queries 1.800.

The complete seed curves and auxiliary paired contrasts are in analysis.json. Curves are training block outcomes, not frozen test performance. The saved trace is a small descriptive window, not another independent replicate.

## Limits shared by these runs

- All 16 program grammar candidates were constructed before the campaign. This evaluates bounded native selection and local composition, not unrestricted program discovery.
- Adaptive distinctions are piecewise-constant consequence hypotheses. Predictor count alternatives are empirical observations, not guaranteed sensor support.
- Query training uses a declared known-fitting shadow task. Shadow work is counted; comparison CPU work and training audits are not debited as the deployment resource fee.
- Frozen tests check fixed learned models. A changed law or price is not adapted during those tests, so degradation measures transfer of that fixed policy.
- Native graph sizes are retained reachable structures, including bounded receipts and evidence. They are neither process memory nor the number of active learned rules alone.
- Conditional accuracy is averaged over seeds with nonzero coverage and accompanies its own n. Zero-coverage seeds are excluded from that statistic rather than treated as0% accurate; paired accuracy contrasts require coverage on both sides.
- Student t intervals are not clipped to0–100%; small pilot samples can produce wide intervals beyond physical rate bounds.
- Neutral/ablated models may have limited information or initial grammar. Measured complementarity does not prove every remaining algorithm incapable.
