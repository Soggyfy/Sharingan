using Sharingan.Abstractions;
using Xunit;

namespace Sharingan.Extensions.DependencyInjection.Tests;

/// <summary>
/// Tests for ServiceCollectionExtensions.
/// Note: These tests focus on the AddSharingan API itself.
/// Integration tests requiring a full DI container should use Microsoft.Extensions.Hosting.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void SharinganBuilder_UseInMemory_Builds()
    {
        SharinganBuilder builder = new();
        builder.UseInMemory();

        ISettingsStore store = builder.Build();

        Assert.NotNull(store);
        Assert.IsAssignableFrom<ISettingsStore>(store);
    }

    [Fact]
    public void SharinganBuilder_UseInMemory_SetAndGet()
    {
        SharinganBuilder builder = new();
        builder.UseInMemory();

        ISettingsStore store = builder.Build();

        store.Set("test", "value");
        Assert.Equal("value", store.Get("test", ""));
    }

    [Fact]
    public void SharinganBuilder_WithApplicationName_Works()
    {
        SharinganBuilder builder = new();
        builder.WithApplicationName("TestApp");
        builder.UseInMemory();

        ISettingsStore store = builder.Build();

        Assert.NotNull(store);
    }

    [Fact]
    public void SharinganBuilder_WithOrganizationName_Works()
    {
        SharinganBuilder builder = new();
        builder.WithApplicationName("TestApp");
        builder.WithOrganizationName("TestOrg");
        builder.UseInMemory();

        ISettingsStore store = builder.Build();

        Assert.NotNull(store);
    }
}
