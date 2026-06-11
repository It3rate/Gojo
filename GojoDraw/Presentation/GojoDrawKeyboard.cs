namespace GojoDraw;

/// <summary>
/// Dependency-free keyboard input vocabulary for GojoDraw hosts. UI frameworks
/// should map their native key events into this shape, then application layers
/// decide what the commands mean.
/// </summary>
public readonly record struct GojoDrawKeyInput(
    GojoDrawKey Key,
    GojoDrawKeyModifiers Modifiers = GojoDrawKeyModifiers.None);

public static class GojoDrawKeyboard
{
    public static GojoDrawKeyCommand ReadCommand(GojoDrawKeyInput input) =>
        input.Key switch
        {
            GojoDrawKey.Left or GojoDrawKey.PageUp => GojoDrawKeyCommand.Previous,
            GojoDrawKey.Right or GojoDrawKey.PageDown or GojoDrawKey.Space or GojoDrawKey.Enter => GojoDrawKeyCommand.Next,
            GojoDrawKey.Home => GojoDrawKeyCommand.First,
            GojoDrawKey.End => GojoDrawKeyCommand.Last,
            GojoDrawKey.N => GojoDrawKeyCommand.ToggleNotes,
            GojoDrawKey.Escape => GojoDrawKeyCommand.Close,
            GojoDrawKey.R => GojoDrawKeyCommand.Reset,
            _ => GojoDrawKeyCommand.None
        };
}

public enum GojoDrawKey
{
    Unknown,
    Left,
    Right,
    Up,
    Down,
    PageUp,
    PageDown,
    Home,
    End,
    Space,
    Enter,
    Escape,
    N,
    R
}

[Flags]
public enum GojoDrawKeyModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Meta = 8
}

public enum GojoDrawKeyCommand
{
    None,
    Previous,
    Next,
    First,
    Last,
    ToggleNotes,
    Close,
    Reset
}
