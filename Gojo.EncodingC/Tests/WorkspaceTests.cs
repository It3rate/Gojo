using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class WorkspaceTests
{
    /// <summary>A passage chain over six positions: plane 0 holds a unit, plane 1
    /// a gate. Rows (0, 1, 2) and (2, 3, 4).</summary>
    private static (Structure Chain, Wiring Rows) Chain()
    {
        var chain = new Structure(2, 6);
        var rows = new Wiring(Flows.Passage, new Binding(chain, 0), new Binding(chain, 1), new Binding(chain, 0));
        rows.Add(0, 1, 2);
        rows.Add(2, 3, 4);
        chain.Write(0, 0, true);
        chain.Write(1, 1, true);
        chain.Write(1, 3, true);
        return (chain, rows);
    }

    [Fact]
    public void AWorkspaceWorksOnACopyUntilItCommits()
    {
        var (chain, rows) = Chain();
        var workspace = new Workspace(chain, Plane.Of(6, 0, 1, 2, 3, 4));
        var (local, missing) = workspace.Map(rows);
        Assert.True(missing.None);

        local.Apply();

        Assert.True(workspace.Local.Read(0, workspace.LocalOf(4)!.Value));
        Assert.True(chain.Read(0, 0));

        var commit = workspace.Commit();

        Assert.True(commit.Committed);
        Assert.Equal(Plane.Of(6, 0, 4), commit.Written);
        Assert.False(chain.Read(0, 0));
        Assert.True(chain.Read(0, 4));
    }

    [Fact]
    public void AStaleCommitWritesNothing()
    {
        var source = new Structure(1, 3);
        var workspace = new Workspace(source, Plane.Of(3, 0, 1));
        workspace.Local.Write(0, 0, true);
        source.Write(0, 1, true);

        var refused = workspace.Commit();

        Assert.False(refused.Committed);
        Assert.Equal(Plane.Of(3, 1), refused.Stale);
        Assert.False(source.Read(0, 0));

        source.Write(0, 1, false);
        source.Write(0, 2, true);
        Assert.True(workspace.Commit().Committed);
        Assert.True(source.Read(0, 0));
    }

    [Fact]
    public void RowsThatReachOutsideTheSelectionCannotRunHere()
    {
        var (chain, rows) = Chain();
        var workspace = new Workspace(chain, Plane.Of(6, 0, 1, 2));

        var (local, missing) = workspace.Map(rows);

        Assert.Equal(Plane.Of(2, 1), missing);
        Assert.Equal(1, local.Count);
    }

    [Fact]
    public void AnIntroducedPositionIsAdoptedOnCommit()
    {
        var source = new Structure(1, 2);
        var workspace = new Workspace(source, Plane.Ones(2));
        var candidate = workspace.Introduce();
        workspace.Local.Write(0, candidate, true);
        Assert.Null(workspace.SourceOf(candidate));

        var commit = workspace.Commit();

        var adopted = Assert.Single(commit.Adopted);
        Assert.Equal(2, adopted);
        Assert.True(source.Read(0, 2));
        Assert.Equal(adopted, workspace.SourceOf(candidate));
        Assert.Equal(candidate, workspace.LocalOf(adopted));
    }

    [Fact]
    public void SupportsNarrowToAFixedPointAndKeepContradictions()
    {
        var equal = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit], t => t[0] == t[1]);
        (Structure, Wiring, Supports) Line()
        {
            var line = new Structure(1, 4);
            var rows = new Wiring(equal, new Binding(line, 0), new Binding(line, 0));
            rows.Add(0, 1);
            rows.Add(1, 2);
            rows.Add(2, 3);
            return (line, rows, new Supports(line, Field.Bit(0)));
        }

        var (line, rows, support) = Line();
        line.Write(0, 0, true);
        support.Observe(0);
        Assert.Equal(3, support.Open.Count);

        Supports.Propagate(new[] { (rows, new[] { support, support }) });

        Assert.Equal(Plane.Ones(4), support.Settled);
        Assert.Equal(Plane.Ones(4), support.Of(1));

        var (other, otherRows, contradiction) = Line();
        other.Write(0, 0, true);
        contradiction.Observe(0);
        contradiction.Observe(3);

        Supports.Propagate(new[] { (otherRows, new[] { contradiction, contradiction }) });

        Assert.True(contradiction.Empty.Any);
    }

    [Fact]
    public void SelectionsComeFromStructureNotFromNames()
    {
        var line = new Structure(1, 4);
        var equal = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit], t => t[0] == t[1]);
        var rows = new Wiring(equal, new Binding(line, 0), new Binding(line, 0));
        rows.Add(0, 1);
        rows.Add(1, 2);
        rows.Add(2, 3);

        Assert.Equal("101010", Selections.Every(6, 2).ToString());
        Assert.Equal(Plane.Of(4, 0, 1, 2), Selections.Within(Plane.Of(4, 0), 2, rows, line));
    }
}
