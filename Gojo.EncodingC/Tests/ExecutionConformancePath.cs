using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class ExecutionConformancePath
{
    private readonly Func<Engine, bool, EngineStep> step;
    private readonly Func<Engine, int, bool, int?> settle;
    private readonly Func<Workspace, Wiring, WorkspaceStep> preview;

    private ExecutionConformancePath(Func<Engine, bool, EngineStep> step,
        Func<Engine, int, bool, int?> settle, Func<Workspace, Wiring, WorkspaceStep> preview)
    {
        this.step = step;
        this.settle = settle;
        this.preview = preview;
    }

    public static TheoryData<ExecutionConformancePath> All => new()
    {
        new ExecutionConformancePath(
            (engine, together) => together ? engine.StepTogether() : engine.Step(),
            (engine, limit, together) => engine.Settle(limit, together),
            (workspace, wiring) => workspace.Apply(wiring)),
    };

    public EngineStep Step(Engine engine, bool together = false) => step(engine, together);

    public int? Settle(Engine engine, int limit, bool together = false) => settle(engine, limit, together);

    public WorkspaceStep Preview(Workspace workspace, Wiring wiring) => preview(workspace, wiring);
}
