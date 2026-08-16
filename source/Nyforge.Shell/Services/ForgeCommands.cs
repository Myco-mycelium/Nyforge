namespace Nyforge.Shell.Services;

/// <summary>
/// Recognized command ids for self-hosted Forge chrome (screens designed
/// in Forge and rendered as part of Forge's own UI, e.g. the Home panel).
///
/// Deliberately separate from NuiSystemActions (Nyforge.Core.Nui): that
/// table describes the Nyrqis API surface a *designed app* runs against.
/// This table describes Forge's own editor commands. Conflating them would
/// mean a "New Project" button designed in Forge's Home screen looks, in
/// the .nstudio file, indistinguishable from a real Nyrqis app calling a
/// real Nyrqis API — which isn't true and would be a quiet, misleading
/// blur of what the schema means. See docs/how-to/redesigning-the-home-screen.md
/// and engineering/NFS-004-self-hosted-home-screen.md for the full rationale.
///
/// A Button's <c>id</c> matching one of these keys is how a self-hosted
/// screen wires up a real Forge action — NOT the Behaviors/Events system,
/// which stays reserved for describing target-app logic.
/// </summary>
public static class ForgeCommands
{
    public const string NewProject = "cmd_new_project";
    public const string OpenProject = "cmd_open_project";
    public const string SaveProject = "cmd_save_project";

    public static readonly IReadOnlyList<string> All = new[] { NewProject, OpenProject, SaveProject };
}
