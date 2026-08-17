using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Resources (NUI-SCHEMA §8.2): the managed asset catalog — sha256
/// hashing and deduplication via AssetCatalog, serialization on the
/// document, and the validator's $asset: reference and file checks.
/// </summary>
public class AssetCatalogTests
{
    private static NuiDocument DocWithWallpaper()
    {
        var doc = NyforgeProject.CreateBlank();
        doc.Resources.Assets.Add(new NuiAsset
        {
            Id = "wallpaper",
            Kind = "image",
            Path = "assets/wallpaper.png",
            Sha256 = "a".PadLeft(64, 'a'),
        });
        return doc;
    }

    [Fact]
    public void ComputeSha256_is_64_hex_and_content_sensitive()
    {
        var a = Path.GetTempFileName();
        var b = Path.GetTempFileName();
        try
        {
            File.WriteAllText(a, "hello");
            File.WriteAllText(b, "hello!");
            var ha = AssetCatalog.ComputeSha256(a);
            var hb = AssetCatalog.ComputeSha256(b);
            Assert.Equal(64, ha.Length);
            Assert.All(ha, c => Assert.Contains(c, "0123456789abcdef"));
            Assert.NotEqual(ha, hb);
        }
        finally
        {
            File.Delete(a);
            File.Delete(b);
        }
    }

    [Fact]
    public void RegisterFromFile_flags_duplicate_content()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "same bytes");
            var first = AssetCatalog.RegisterFromFile(
                "a", NuiAssetKind.Image, "assets/a.png", path, Array.Empty<NuiAsset>(),
                out var dup1);
            Assert.False(dup1);
            var second = AssetCatalog.RegisterFromFile(
                "b", NuiAssetKind.Image, "assets/b.png", path,
                new[] { first }, out var dup2);
            Assert.True(dup2);
            Assert.Equal(first.Sha256, second.Sha256);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Resources_section_survives_round_trip()
    {
        var doc = DocWithWallpaper();
        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        var asset = reloaded.Resources.Assets.Single();
        Assert.Equal("wallpaper", asset.Id);
        Assert.Equal("image", asset.Kind);
        Assert.Equal("assets/wallpaper.png", asset.Path);
        Assert.NotNull(asset.Sha256);
    }

    [Fact]
    public void Validator_accepts_declared_asset_reference()
    {
        var doc = DocWithWallpaper();
        var surface = new NuiComponent
        {
            Id = "desktop",
            Type = "DesktopSurface",
            Properties = new Dictionary<string, object?>
            {
                ["wallpaper"] = "$asset:wallpaper",
                ["accent"] = "Primary",
                ["iconSize"] = 96,
            },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 1440, Height = 820 },
        };
        doc.Screens[0].Root.Children.Add(surface);

        var result = NuiValidator.Validate(doc);

        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Validator_rejects_undeclared_asset_reference()
    {
        var doc = DocWithWallpaper();
        var surface = new NuiComponent
        {
            Id = "desktop",
            Type = "DesktopSurface",
            Properties = new Dictionary<string, object?>
            {
                ["wallpaper"] = "$asset:ghost",
                ["accent"] = "Primary",
                ["iconSize"] = 96,
            },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 1440, Height = 820 },
        };
        doc.Screens[0].Root.Children.Add(surface);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-020");
    }

    [Fact]
    public void Validator_rejects_asset_ref_without_resources_section()
    {
        var doc = NyforgeProject.CreateBlank(); // no resources
        var surface = new NuiComponent
        {
            Id = "desktop",
            Type = "DesktopSurface",
            Properties = new Dictionary<string, object?>
            {
                ["wallpaper"] = "$asset:wallpaper",
                ["accent"] = "Primary",
                ["iconSize"] = 96,
            },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 1440, Height = 820 },
        };
        doc.Screens[0].Root.Children.Add(surface);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-020");
    }

    [Fact]
    public void Validator_warns_on_missing_resource_file()
    {
        var doc = DocWithWallpaper();
        doc.Resources.Assets[0].Path = "assets/missing.png";

        var result = NuiValidator.Validate(doc, projectDirectory: "/nonexistent/dir");

        Assert.Contains(result.Warnings, i => i.Code == "WN-NUI-007");
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Validator_warns_on_duplicate_content()
    {
        var doc = NyforgeProject.CreateBlank();
        doc.Resources.Assets.Add(new NuiAsset
        {
            Id = "a", Kind = "image", Path = "a.png", Sha256 = "b".PadLeft(64, 'b'),
        });
        doc.Resources.Assets.Add(new NuiAsset
        {
            Id = "b", Kind = "image", Path = "b.png", Sha256 = "b".PadLeft(64, 'b'),
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Warnings, i => i.Code == "WN-NUI-008");
        Assert.False(result.HasErrors);
    }
}
