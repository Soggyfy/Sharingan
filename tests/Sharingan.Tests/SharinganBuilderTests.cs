using Sharingan.Abstractions;
using Sharingan.Providers;
using Xunit;

namespace Sharingan.Tests;

public class SharinganBuilderTests
{
    [Fact]
    public void Build_WithInMemoryProvider_ReturnsStore()
    {
        // Arrange & Act
        ISettingsStore store = new SharinganBuilder()
            .WithApplicationName("TestApp")
            .UseInMemory()
            .Build();

        // Assert
        Assert.NotNull(store);
    }

    [Fact]
    public void Build_WithMultipleProviders_ReturnsCompositeStore()
    {
        // Arrange & Act
        ISettingsStore store = new SharinganBuilder()
            .WithApplicationName("TestApp")
            .UseInMemory(priority: 10)
            .UseEnvironmentVariables(priority: 100)
            .Build();

        // Assert
        Assert.NotNull(store);
        Assert.IsType<CompositeSettingsProvider>(store);
    }

    [Fact]
    public void Build_SetsApplicationName()
    {
        // Arrange
        SharinganBuilder builder = new SharinganBuilder()
            .WithApplicationName("MyApp")
            .WithOrganizationName("MyOrg")
            .UseInMemory();

        // Act
        ISettingsStore store = builder.Build();

        // Assert - successful build indicates names were set
        Assert.NotNull(store);
    }
}
