using Nyforge.Core.Nui;
using Nyforge.Core.Runtime;
using Xunit;

namespace Nyforge.Core.Tests;

public class RendererRegistryTests
{
    private sealed class MockRenderer : IComponentRenderer
    {
        public string Name { get; }
        public IReadOnlyList<string> SupportedTypes { get; }
        public int LookupCount { get; private set; }

        public MockRenderer(string name, params string[] types)
        {
            Name = name;
            SupportedTypes = types;
        }
    }

    private sealed class MockPropertyRenderer : IPropertyRenderer
    {
        public string Name => "PropertyRenderer";
        public IReadOnlyList<string> SupportedTypes { get; }
        public IReadOnlyDictionary<string, object?> DefaultProperties(NuiComponent c) =>
            new Dictionary<string, object?> { ["text"] = "default" };

        public MockPropertyRenderer(params string[] types) => SupportedTypes = types;
    }

    private sealed class MockLayoutRenderer : ILayoutRenderer
    {
        public string Name => "LayoutRenderer";
        public IReadOnlyList<string> SupportedTypes { get; }
        public bool IsContainer { get; }

        public MockLayoutRenderer(bool isContainer, params string[] types)
        {
            IsContainer = isContainer;
            SupportedTypes = types;
        }
    }

    private sealed class MockEventRenderer : IEventRenderer
    {
        public string Name => "EventRenderer";
        public IReadOnlyList<string> SupportedTypes { get; }
        public IReadOnlyList<string> SupportedEvents { get; }

        public MockEventRenderer(string[] types, string[] events)
        {
            SupportedTypes = types;
            SupportedEvents = events;
        }
    }

    [Fact]
    public void Register_And_Lookup()
    {
        var reg = new ComponentRendererRegistry();
        var r = new MockRenderer("Btn", "Button");
        reg.Register(r);

        Assert.Equal(1, reg.RendererCount);
        Assert.Equal(1, reg.TypeCount);
        Assert.True(reg.HasRenderer("Button"));
        Assert.Same(r, reg.GetRenderer("Button"));
    }

    [Fact]
    public void Lookup_Missing_Returns_Null()
    {
        var reg = new ComponentRendererRegistry();
        Assert.Null(reg.GetRenderer("Nope"));
        Assert.False(reg.HasRenderer("Nope"));
    }

    [Fact]
    public void Multi_Type_Renderer()
    {
        var reg = new ComponentRendererRegistry();
        var r = new MockRenderer("Toggle", "Toggle", "Checkbox", "Switch");
        reg.Register(r);

        Assert.Equal(1, reg.RendererCount);
        Assert.Equal(3, reg.TypeCount);
        Assert.Same(r, reg.GetRenderer("Toggle"));
        Assert.Same(r, reg.GetRenderer("Checkbox"));
        Assert.Same(r, reg.GetRenderer("Switch"));
    }

    [Fact]
    public void Last_Writer_Wins()
    {
        var reg = new ComponentRendererRegistry();
        var old = new MockRenderer("Old", "Button");
        var @new = new MockRenderer("New", "Button");
        reg.Register(old);
        reg.Register(@new);

        Assert.Equal(2, reg.RendererCount);
        Assert.Same(@new, reg.GetRenderer("Button"));
    }

    [Fact]
    public void GetAll_Returns_All_Registrations()
    {
        var reg = new ComponentRendererRegistry();
        reg.Register(new MockRenderer("A", "Button"));
        reg.Register(new MockRenderer("B", "Text", "Label"));

        var all = reg.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Property_Renderer_Interface()
    {
        var r = new MockPropertyRenderer("Text");
        Assert.IsAssignableFrom<IPropertyRenderer>(r);
        var defaults = r.DefaultProperties(new NuiComponent { Type = "Text" });
        Assert.Equal("default", defaults["text"]);
    }

    [Fact]
    public void Layout_Container_Flag()
    {
        var container = new MockLayoutRenderer(true, "Window", "Container");
        Assert.True(container.IsContainer);

        var leaf = new MockLayoutRenderer(false, "Button");
        Assert.False(leaf.IsContainer);
    }

    [Fact]
    public void Event_Renderer_Supported_Events()
    {
        var r = new MockEventRenderer(
            new[] { "Button" },
            new[] { "clicked", "pressed", "released" });

        Assert.Equal(3, r.SupportedEvents.Count);
        Assert.Contains("clicked", r.SupportedEvents);
        Assert.Contains("pressed", r.SupportedEvents);
        Assert.Contains("released", r.SupportedEvents);
    }

    [Fact]
    public void Register_Null_Throws()
    {
        var reg = new ComponentRendererRegistry();
        Assert.Throws<ArgumentNullException>(() => reg.Register(null!));
    }

    [Fact]
    public void Empty_Registry()
    {
        var reg = new ComponentRendererRegistry();
        Assert.Equal(0, reg.RendererCount);
        Assert.Equal(0, reg.TypeCount);
        Assert.Empty(reg.GetAll());
    }
}
