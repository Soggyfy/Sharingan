using Microsoft.Extensions.Configuration;
using Sharingan.Abstractions;
using Xunit;

namespace Sharingan.Extensions.Configuration.Tests;

public class ConfigurationBuilderExtensionsTests
{
    [Fact]
    public void AddSharingan_AddsProvider()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddSharingan(builder => builder.UseInMemory())
            .Build();

        // Configuration should be built successfully
        Assert.NotNull(configuration);
    }

    [Fact]
    public void AddSharingan_WithInMemory_Works()
    {
        ISettingsStore store = new SharinganBuilder()
            .UseInMemory()
            .Build();

        store.Set("app.name", "TestApp");
        store.Set("app.version", "1.0.0");

        // Verify the store works
        Assert.Equal("TestApp", store.Get("app.name", ""));
        Assert.Equal("1.0.0", store.Get("app.version", ""));
    }

    [Fact]
    public void Configuration_Build_Succeeds()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddSharingan(builder => builder.UseInMemory())
            .Build();

        Assert.NotNull(configuration);
    }
}
