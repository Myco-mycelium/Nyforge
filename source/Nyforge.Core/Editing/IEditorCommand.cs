namespace Nyforge.Core.Editing;

/// <summary>
/// One undoable editor operation. Commands mutate the document model by
/// reference (the Shell's ViewModels wrap the same NuiComponent instances),
/// so Execute and Undo just apply/restore the captured before/after state —
/// no snapshots of the whole project (see engineering/ROADMAP.md v0.2 and
/// the 2026-08-17 architecture review, item #5).
///
/// Commands are pure with respect to host services: a command that needs
/// the host (e.g. re-applying a theme) is implemented in the Shell and
/// pushed onto the same <see cref="CommandHistory"/> — the history only
/// knows the interface.
/// </summary>
public interface IEditorCommand
{
    /// <summary>Applies the change. Must be idempotent with respect to Redo.</summary>
    void Execute();

    /// <summary>Reverses the change. Must restore the exact pre-Execute state.</summary>
    void Undo();

    /// <summary>Short human-readable label, e.g. "Move button_save".</summary>
    string Description { get; }
}
