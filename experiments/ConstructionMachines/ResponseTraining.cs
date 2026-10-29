using Gojo.Core;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Offline numerical fitting over derived finite patch reads. The
/// exported affine frames, domains and bounds own execution after fitting.</summary>
public static class ResponseTraining
{
    public sealed record Epoch(int epoch, double training, double validation);
    public sealed record Fit(string Name, LocalResponseProgram Program, int Parameters, int Seed, int BestEpoch,
        double TrainingMse, double ValidationMse, Epoch[] History, double[][][] Weights);
    public sealed record Metrics(int count, double mse, double meanRegret, double bestActionRate, double meanRelief, double fixedRelief, double worstRegret);
    private static readonly (int Feature, long Cut)[] Splits = [(0, 256), (1, 512), (2, 341), (4, 256)];

    public static Fit Table(LocalResponseData.Sets data)
    {
        var global = Enumerable.Range(0, LocalResponseProgram.Caps.Length).Select(i => data.Train.Average(p => p.Rewards[i])).ToArray();
        var cells = Enumerable.Range(0, 1 << Splits.Length).Select(bin =>
        {
            var selected = data.Train.Where(p => Bin(p.X) == bin).ToArray(); var sparse = selected.Length < 4;
            var means = global.Select((fallback, i) => sparse ? fallback : selected.Average(p => p.Rewards[i])).ToArray();
            var weights = new[] { means.Select(mean => new[] { mean, 0d, 0d, 0d, 0d, 0d, 0d }).ToArray() };
            var domains = Enumerable.Range(0, LocalResponseProgram.InputCount).Select(_ => new[] { 0L, LocalResponseProgram.InputScale }).ToArray();
            for (var bit = 0; bit < Splits.Length; bit++)
            {
                var (feature, cut) = Splits[bit]; domains[feature] = (bin & (1 << bit)) == 0 ? [0, cut] : [cut + 1, LocalResponseProgram.InputScale];
            }
            return new LocalResponseProgram.Cell(domains, weights, selected.Length,
                G(data.Receipt, new CarrierLocation(new Carrier(), sparse ? data.Train.Length : selected.Length)));
        }).ToArray();
        var program = LocalResponseProgram.Build(cells, data.Receipt);
        var train = Evaluate(program, data.Train); var validation = Evaluate(program, data.Validation);
        return new("Context table", program, cells.Length * LocalResponseProgram.Caps.Length, 0, 1, train.mse, validation.mse, [], []);
    }

    public static Fit Linear(LocalResponseData.Sets data)
    {
        const int n = LocalResponseProgram.InputCount + 1; var gram = new double[n, n]; var cross = new double[n, LocalResponseProgram.Caps.Length];
        foreach (var patch in data.Train)
        {
            var x = new[] { 1d }.Concat(patch.X).ToArray();
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++) gram[i, j] += x[i] * x[j];
                for (var k = 0; k < LocalResponseProgram.Caps.Length; k++) cross[i, k] += x[i] * patch.Rewards[k];
            }
        }
        for (var i = 1; i < n; i++) gram[i, i] += data.Train.Length * .001;
        var solved = Solve(gram, cross); var weights = new[] { Enumerable.Range(0, LocalResponseProgram.Caps.Length)
            .Select(k => Enumerable.Range(0, n).Select(i => solved[i, k]).ToArray()).ToArray() };
        var program = Network(weights, data.Receipt, data.Train.Length);
        var train = Evaluate(program, data.Train); var validation = Evaluate(program, data.Validation);
        return new("Small regression", program, n * LocalResponseProgram.Caps.Length, 0, 1, train.mse, validation.mse, [], weights);
    }

    public static Fit Neural(LocalResponseData.Sets data, int seed, int maxEpochs = 600)
    {
        var random = new Random(seed); const int hidden = 8, outputs = 6;
        double Gaussian() => Math.Sqrt(-2 * Math.Log(Math.Max(1e-12, random.NextDouble()))) * Math.Cos(2 * Math.PI * random.NextDouble());
        var weights = new[]
        {
            Enumerable.Range(0, hidden).Select(_ => Enumerable.Range(0, 7).Select(i => i == 0 ? .05 : Gaussian() * Math.Sqrt(2d / 6)).ToArray()).ToArray(),
            Enumerable.Range(0, outputs).Select(_ => Enumerable.Range(0, hidden + 1).Select(i => i == 0 ? 0 : Gaussian() * .05).ToArray()).ToArray()
        };
        var first = Zero(weights); var second = Zero(weights); var history = new List<Epoch>();
        var best = Copy(weights); var bestLoss = double.PositiveInfinity; var bestEpoch = 0; var update = 0;
        var order = Enumerable.Range(0, data.Train.Length).ToArray();
        for (var epoch = 1; epoch <= maxEpochs; epoch++)
        {
            random.Shuffle(order);
            for (var start = 0; start < order.Length; start += 64)
            {
                var count = Math.Min(64, order.Length - start); var gradient = Zero(weights);
                for (var row = start; row < start + count; row++)
                {
                    var patch = data.Train[order[row]]; var h = Forward(weights[0], patch.X, 0, 4); var y = Forward(weights[1], h, -1, 1);
                    var dh = new double[hidden];
                    for (var k = 0; k < outputs; k++)
                    {
                        var dy = y[k] <= -1 || y[k] >= 1 ? 0 : 2 * (y[k] - patch.Rewards[k]) / (count * outputs);
                        gradient[1][k][0] += dy;
                        for (var j = 0; j < hidden; j++) { gradient[1][k][j + 1] += dy * h[j]; dh[j] += dy * weights[1][k][j + 1]; }
                    }
                    for (var j = 0; j < hidden; j++)
                    {
                        var d = h[j] <= 0 || h[j] >= 4 ? 0 : dh[j]; gradient[0][j][0] += d;
                        for (var i = 0; i < patch.X.Length; i++) gradient[0][j][i + 1] += d * patch.X[i];
                    }
                }
                update++; var correction1 = 1 - Math.Pow(.9, update); var correction2 = 1 - Math.Pow(.999, update);
                for (var l = 0; l < weights.Length; l++) for (var j = 0; j < weights[l].Length; j++) for (var i = 0; i < weights[l][j].Length; i++)
                {
                    var g = gradient[l][j][i] + (i == 0 ? 0 : .0001 * weights[l][j][i]);
                    first[l][j][i] = .9 * first[l][j][i] + .1 * g;
                    second[l][j][i] = .999 * second[l][j][i] + .001 * g * g;
                    weights[l][j][i] -= .01 * first[l][j][i] / correction1 / (Math.Sqrt(second[l][j][i] / correction2) + 1e-8);
                }
            }
            if (epoch % 10 != 0 && epoch != maxEpochs) continue;
            var trainLoss = Loss(weights, data.Train); var valLoss = Loss(weights, data.Validation); history.Add(new(epoch, trainLoss, valLoss));
            if (valLoss < bestLoss - 1e-8) { bestLoss = valLoss; best = Copy(weights); bestEpoch = epoch; }
            if (epoch - bestEpoch >= 100) break;
        }
        var program = Network(best, G(data.Receipt, new CarrierLocation(new Carrier(), seed), new CarrierLocation(new Carrier(), bestEpoch)), data.Train.Length);
        var training = Evaluate(program, data.Train); var validation = Evaluate(program, data.Validation);
        return new("Small neural model", program, 110, seed, bestEpoch, training.mse, validation.mse, history.ToArray(), best);
    }

    public static LocalResponseProgram Network(double[][][] weights, IElement source, int support) => LocalResponseProgram.Build(
        [new(Enumerable.Range(0, LocalResponseProgram.InputCount).Select(_ => new[] { 0L, LocalResponseProgram.InputScale }).ToArray(), weights, support, source)], source);

    public static Metrics Evaluate(LocalResponseProgram program, LocalResponseData.Patch[] patches)
    {
        var mse = 0d; var regret = 0d; var best = 0; var relief = 0d; var fixedRelief = 0d; var worst = 0d;
        foreach (var patch in patches)
        {
            var context = RequestContext.Rebind((Relation)F(patch.Surface)[2], program.Inputs);
            var prediction = program.Predict(context); var choice = LocalResponseProgram.Choice(program.Select(prediction));
            if (choice < 0) throw new InvalidOperationException("A finite fitted function did not supply a patch response.");
            var scores = F(F(prediction)[2]).Select(x => CoupledSettlement.Number(x) / LocalResponseProgram.OutputScale).ToArray();
            mse += scores.Select((x, i) => Math.Pow(x - patch.Rewards[i], 2)).Average();
            var missed = patch.Rewards.Max() - patch.Rewards[choice]; regret += missed; worst = Math.Max(worst, missed);
            if (missed <= 1e-12) best++; relief += patch.Rewards[choice]; fixedRelief += patch.Rewards[0];
        }
        return new(patches.Length, mse / patches.Length, regret / patches.Length, (double)best / patches.Length,
            relief / patches.Length, fixedRelief / patches.Length, worst);
    }

    public static int Bin(double[] x)
    {
        var bin = 0; for (var bit = 0; bit < Splits.Length; bit++) if (Math.Round(x[Splits[bit].Feature] * LocalResponseProgram.InputScale) > Splits[bit].Cut) bin |= 1 << bit;
        return bin;
    }
    public static double[] PredictBackend(double[][][] weights, double[] x)
    { for (var l = 0; l < weights.Length; l++) x = Forward(weights[l], x, l == weights.Length - 1 ? -1 : 0, l == weights.Length - 1 ? 1 : 4); return x; }
    private static double[] Forward(double[][] weights, double[] x, double lo, double hi) => weights.Select(w => Math.Clamp(w[0] + x.Select((v, i) => v * w[i + 1]).Sum(), lo, hi)).ToArray();
    private static double Loss(double[][][] weights, LocalResponseData.Patch[] patches) => patches.Average(p => PredictBackend(weights, p.X).Select((v, i) => Math.Pow(v - p.Rewards[i], 2)).Average());
    private static double[][][] Zero(double[][][] weights) => weights.Select(l => l.Select(w => new double[w.Length]).ToArray()).ToArray();
    private static double[][][] Copy(double[][][] weights) => weights.Select(l => l.Select(w => w.ToArray()).ToArray()).ToArray();

    private static double[,] Solve(double[,] a, double[,] b)
    {
        var n = a.GetLength(0); var outputs = b.GetLength(1);
        for (var k = 0; k < n; k++)
        {
            var pivot = Enumerable.Range(k, n - k).MaxBy(i => Math.Abs(a[i, k]));
            if (Math.Abs(a[pivot, k]) < 1e-12) throw new InvalidOperationException("The regularized fit is singular.");
            for (var j = 0; j < n; j++) (a[k, j], a[pivot, j]) = (a[pivot, j], a[k, j]);
            for (var j = 0; j < outputs; j++) (b[k, j], b[pivot, j]) = (b[pivot, j], b[k, j]);
            var d = a[k, k]; for (var j = k; j < n; j++) a[k, j] /= d; for (var j = 0; j < outputs; j++) b[k, j] /= d;
            for (var i = 0; i < n; i++) if (i != k)
            {
                var factor = a[i, k]; for (var j = k; j < n; j++) a[i, j] -= factor * a[k, j];
                for (var j = 0; j < outputs; j++) b[i, j] -= factor * b[k, j];
            }
        }
        return b;
    }
}
