using Sharingan.Abstractions;
using Sharingan.Providers;
using Xunit;

namespace Sharingan.Tests;

public class InMemoryProviderTests
{
    [Fact]
    public void Set_And_Get_ReturnsValue()
    {
        // Arrange
        using InMemorySettingsProvider provider = new();

        // Act
        provider.Set("test.key", "hello");
        string result = provider.Get("test.key", "default");

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsDefault()
    {
        // Arrange
        using InMemorySettingsProvider provider = new();

        // Act
        string result = provider.Get("nonexistent", "default");

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrue()
    {
        // Arrange
        using InMemorySettingsProvider provider = new();
        provider.Set("key", "value");

        // Act
        bool result = provider.Remove("key");

        // Assert
        Assert.True(result);
        Assert.False(provider.ContainsKey("key"));
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        // Arrange
        using InMemorySettingsProvider provider = new();
        provider.Set("key1", "value1");
        provider.Set("key2", "value2");

        // Act
        provider.Clear();

        // Assert
        Assert.Equal(0, provider.Count);
    }

    [Fact]
    public void GetAllKeys_ReturnsAllKeys()
    {
        // Arrange
        using InMemorySettingsProvider provider = new();
        provider.Set("key1", "value1");
        provider.Set("key2", "value2");

        // Act
        List<string> keys = provider.GetAllKeys().ToList();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
    }

    [Fact]
    public void Set_RaisesSettingsChangedEvent()
    {
        // Arrange
        using InMemorySettingsProvider provider = new();
        SettingsChangedEventArgs eventArgs = null;
        provider.SettingsChanged += (s, e) => eventArgs = e;

        // Act
        provider.Set("key", "value");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("key", eventArgs.Key);
        Assert.Equal(SettingsChangeType.Added, eventArgs.ChangeType);
    }
}
