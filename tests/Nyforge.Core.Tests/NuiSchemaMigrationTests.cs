using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Schema migrations (NFC-001 §4.2): old .nstudio files must keep
/// opening. The chain moves a document forward to the current schema
/// version before parsing, in memory only — the input string and the
/// file on disk are never touched.
/// </summary>
public class NuiSchemaMigrationTests
{
    private const string OldV2 = """
    {
      "version": "0.2.0",
      "project": { "name": "old", "id": "old" },
      "themes": { "active": "Eclipse" },
      "screens": []
    }
    """;

    private const string V3WithBindings = """
    {
      "version": "0.3.0",
      "project": { "name": "mid", "id": "mid" },
      "themes": { "active": "Eclipse" },
      "states": { "themeName": "Eclipse" },
      "bindings": [],
      "screens": []
    }
    """;

    [Fact]
    public void V2_document_runs_the_full_chain()
    {
        var result = NuiSchemaMigrations.MigrateIfNeeded(OldV2);

        Assert.NotNull(result);
        Assert.Equal("0.2.0", result!.FromVersion);
        Assert.Equal(NuiSchemaVersion.Current, result.ToVersion);
        Assert.Equal(new[] { "0.2.0 -> 0.3.0", "0.3.0 -> 0.4.0" }, result.Applied);

        var migrated = System.Text.Json.Nodes.JsonNode.Parse(result.MigratedJson)!;
        Assert.Equal("0.4.0", migrated["version"]!.GetValue<string>());
        Assert.NotNull(migrated["bindings"]); // added by 0.2.0 -> 0.3.0
        Assert.NotNull(migrated["states"]);   // added by 0.3.0 -> 0.4.0
    }

    [Fact]
    public void V3_document_runs_only_the_remaining_step()
    {
        var result = NuiSchemaMigrations.MigrateIfNeeded(V3WithBindings);

        Assert.NotNull(result);
        Assert.Equal(new[] { "0.3.0 -> 0.4.0" }, result!.Applied);
        var migrated = System.Text.Json.Nodes.JsonNode.Parse(result.MigratedJson)!;
        Assert.Equal("0.4.0", migrated["version"]!.GetValue<string>());
        // Existing sections are left alone (idempotent steps).
        Assert.Equal("Eclipse", migrated["states"]!["themeName"]!.GetValue<string>());
        Assert.NotNull(migrated["bindings"]);
    }

    [Fact]
    public void Current_version_is_not_touched()
    {
        var doc = NyforgeProject.CreateBlank();
        var json = ProjectSerializer.Serialize(doc);

        var result = NuiSchemaMigrations.MigrateIfNeeded(json);

        Assert.Null(result);
    }

    [Fact]
    public void Future_version_is_not_migrated()
    {
        var future = """
        {
          "version": "9.9.0",
          "project": { "name": "x", "id": "x" },
          "themes": { "active": "Eclipse" },
          "screens": []
        }
        """;

        Assert.Null(NuiSchemaMigrations.MigrateIfNeeded(future));
    }

    [Fact]
    public void Malformed_json_is_not_a_migration_concern()
    {
        Assert.Null(NuiSchemaMigrations.MigrateIfNeeded("{not json"));
    }

    [Fact]
    public void Input_string_is_never_mutated()
    {
        var before = OldV2;
        _ = NuiSchemaMigrations.MigrateIfNeeded(before);
        Assert.Equal(OldV2, before);
    }

    [Fact]
    public void Old_v2_file_opens_instead_of_throwing()
    {
        // Before the migration chain this threw NuiVersionMismatchException
        // (same MAJOR required, MINOR equal required). Now it opens.
        var result = ProjectSerializer.DeserializeWithMigration(OldV2);

        Assert.NotNull(result.Migration);
        Assert.Equal(NuiSchemaVersion.Current, result.Document.Version);
        Assert.False(string.IsNullOrEmpty(result.Document.Project.Name));
    }

    [Fact]
    public void Current_file_reports_no_migration()
    {
        var doc = NyforgeProject.CreateBlank();
        var json = ProjectSerializer.Serialize(doc);

        var result = ProjectSerializer.DeserializeWithMigration(json);

        Assert.Null(result.Migration);
        Assert.Equal("window_main", result.Document.Screens[0].Root.Id);
    }

    [Fact]
    public void Genuinely_incompatible_version_still_throws()
    {
        var future = """
        {
          "version": "9.9.0",
          "project": { "name": "x", "id": "x" },
          "themes": { "active": "Eclipse" },
          "screens": []
        }
        """;

        Assert.Throws<NuiVersionMismatchException>(
            () => ProjectSerializer.DeserializeWithMigration(future));
    }
}
