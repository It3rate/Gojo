using System.Runtime.CompilerServices;
using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class ExecutionConformancePath
{
    private static readonly ConditionalWeakTable<Engine, CompiledPlan> Plans = new();

    private readonly string name;
    private readonly Func<Engine, bool, EngineStep> step;
    private readonly Func<Engine, int, bool, int?> settle;
    private readonly Func<Workspace, Wiring, WorkspaceStep> preview;

    private ExecutionConformancePath(string name, Func<Engine, bool, EngineStep> step,
        Func<Engine, int, bool, int?> settle, Func<Workspace, Wiring, WorkspaceStep> preview)
    {
        this.name = name;
        this.step = step;
        this.settle = settle;
        this.preview = preview;
    }

    public static TheoryData<ExecutionConformancePath> All => new()
    {
        new ExecutionConformancePath(
            "Engine",
            (engine, together) => together ? engine.StepTogether() : engine.Step(),
            (engine, limit, together) => engine.Settle(limit, together),
            (workspace, wiring) => workspace.Apply(wiring)),
        new ExecutionConformancePath(
            "CompiledPlan",
            (engine, together) => PlanFor(engine).Step(together),
            (engine, limit, together) => PlanFor(engine).Settle(limit, together),
            CompiledPlan.Preview),
    };

    private static CompiledPlan PlanFor(Engine engine) => Plans.GetValue(engine, CompiledPlan.Compile);

    public override string ToString() => name;

    public EngineStep Step(Engine engine, bool together = false) => step(engine, together);

    public int? Settle(Engine engine, int limit, bool together = false) => settle(engine, limit, together);

    public WorkspaceStep Preview(Workspace workspace, Wiring wiring) => preview(workspace, wiring);
}
