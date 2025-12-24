using Sharingan.Providers;
using Xunit;

namespace Sharingan.Tests;

public class SettingsExtensionsTests
{
    [Fact]
    public void GetString_ReturnsStringValue()
    {
        // Arrange
        using InMemorySettingsProvider store = new();
        store.Set("key", "value");

        // Act
        string result = store.GetString("key");

        // Assert
        Assert.Equal("value", result);
    }

    [Fact]
    public void GetInt_ReturnsIntValue()
    {
        // Arrange
        using InMemorySettingsProvider store = new();
        store.Set("key", 42);

        // Act
        int result = store.GetInt("key");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void GetBool_ReturnsBoolValue()
    {
        // Arrange
        using InMemorySettingsProvider store = new();
        store.Set("key", true);

        // Act
        bool result = store.GetBool("key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetOrSet_WhenKeyMissing_SetsAndReturnsValue()
    {
        // Arrange
        using InMemorySettingsProvider store = new();

        // Act
        string result = store.GetOrSet("key", "default");

        // Assert
        Assert.Equal("default", result);
        Assert.Equal("default", store.Get("key", ""));
    }

    [Fact]
    public void GetOrSet_WhenKeyExists_ReturnsExistingValue()
    {
        // Arrange
        using InMemorySettingsProvider store = new();
        store.Set("key", "existing");

        // Act
        string result = store.GetOrSet("key", "default");

        // Assert
        Assert.Equal("existing", result);
    }
}
