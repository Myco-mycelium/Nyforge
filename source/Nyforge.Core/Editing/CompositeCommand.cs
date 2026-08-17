namespace Nyforge.Core.Editing;

/// <summary>
/// Groups several commands into a single undo step — e.g. moving N
/// components as one multi-select gesture, or deleting N components at
/// once. Execute runs in order; Undo runs in reverse, so nested/overlapping
/// operations restore cleanly (a parent's undo re-attaches after its
/// children's undos have re-inserted into it).
/// </summary>
public sealed class CompositeCommand : IEditorCommand
{
    private readonly IReadOnlyList<IEditorCommand> _commands;

    public CompositeCommand(IEnumerable<IEditorCommand> commands)
    {
        _commands = commands.ToList();
    }

    public string Description => _commands.Count == 1
        ? _commands[0].Description
        : $"{_commands.Count} operations";

    public void Execute()
    {
        foreach (var command in _commands)
        {
            command.Execute();
        }
    }

    public void Undo()
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
