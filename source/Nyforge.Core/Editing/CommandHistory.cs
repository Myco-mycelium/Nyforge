namespace Nyforge.Core.Editing;

/// <summary>
/// The undo/redo stacks. One <see cref="IEditorCommand"/> per completed
/// user gesture — a drag commits a single MoveComponentCommand on release,
/// never a command per pointer-move (the 2026-08-17 architecture review,
/// item #5). New executes clear the redo stack; the undo stack is trimmed
/// to <see cref="Limit"/> entries so memory stays bounded.
/// </summary>
public sealed class CommandHistory
{
    private readonly List<IEditorCommand> _undo = new();
    private readonly List<IEditorCommand> _redo = new();

    /// <summary>Maximum undo depth. Oldest commands are dropped beyond this.</summary>
    public int Limit { get; init; } = 100;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Raised after every Execute/Undo/Redo so hosts can refresh their view of the model.</summary>
    public event EventHandler? Changed;

    /// <summary>Executes the command, pushes it onto the undo stack, and clears the redo stack.</summary>
    public void Execute(IEditorCommand command)
    {
        command.Execute();
        _undo.Add(command);
        if (_undo.Count > Limit) _undo.RemoveAt(0);
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var command = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        command.Undo();
        _redo.Add(command);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var command = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        command.Execute();
        _undo.Add(command);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
