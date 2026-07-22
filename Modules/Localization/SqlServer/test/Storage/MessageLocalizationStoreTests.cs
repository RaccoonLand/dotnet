using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Storage;

public sealed class MessageLocalizationStoreTests
{
    [Fact]
    public void Replace_AtomicallySwapsSnapshot()
    {
        var store = new MessageLocalizationStore();
        store.Replace(
        [
            new LocalizationEntry("en-US", "Hello", "Hello"),
        ]);

        Assert.True(store.TryGet("en-US", "Hello", out var before));
        Assert.Equal("Hello", before);

        store.Replace(
        [
            new LocalizationEntry("fa-IR", "Hello", "سلام"),
        ]);

        Assert.False(store.TryGet("en-US", "Hello", out _));
        Assert.True(store.TryGet("fa-IR", "Hello", out var after));
        Assert.Equal("سلام", after);
        Assert.Equal(1, store.CultureCount);
    }

    [Fact]
    public void TryGet_CultureIsCaseInsensitive()
    {
        var store = new MessageLocalizationStore();
        store.Replace([new LocalizationEntry("en-US", "Key", "Value")]);

        Assert.True(store.TryGet("EN-us", "Key", out var value));
        Assert.Equal("Value", value);
    }

    [Fact]
    public void TryGet_KeyIsCaseSensitive()
    {
        var store = new MessageLocalizationStore();
        store.Replace([new LocalizationEntry("en-US", "Key", "Value")]);

        Assert.True(store.TryGet("en-US", "Key", out _));
        Assert.False(store.TryGet("en-US", "key", out _));
        Assert.False(store.TryGet("en-US", "KEY", out _));
    }

    [Fact]
    public void Replace_DuplicateKey_LastValueWins()
    {
        var store = new MessageLocalizationStore();
        store.Replace(
        [
            new LocalizationEntry("en-US", "Key", "first"),
            new LocalizationEntry("en-US", "Key", "second"),
        ]);

        Assert.True(store.TryGet("en-US", "Key", out var value));
        Assert.Equal("second", value);
    }

    [Fact]
    public void TryGet_AfterReplace_SeesConsistentSnapshot()
    {
        var store = new MessageLocalizationStore();
        store.Replace(
        [
            new LocalizationEntry("en-US", "A", "1"),
            new LocalizationEntry("en-US", "B", "2"),
            new LocalizationEntry("fa-IR", "A", "۳"),
        ]);

        Assert.Equal(2, store.CultureCount);
        Assert.True(store.TryGet("en-US", "A", out var a));
        Assert.True(store.TryGet("en-US", "B", out var b));
        Assert.True(store.TryGet("fa-IR", "A", out var fa));
        Assert.Equal("1", a);
        Assert.Equal("2", b);
        Assert.Equal("۳", fa);
    }
}
