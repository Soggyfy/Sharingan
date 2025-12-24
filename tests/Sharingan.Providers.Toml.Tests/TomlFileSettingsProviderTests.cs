using Xunit;

namespace Sharingan.Providers.Toml.Tests;

public class TomlFileSettingsProviderTests : IDisposable
{
    private readonly string _testFile;
    private readonly TomlFileSettingsProvider _provider;

    public TomlFileSettingsProviderTests()
    {
        _testFile = Path.Combine(Path.GetTempPath(), $"toml_test_{Guid.NewGuid()}.toml");
        _provider = new TomlFileSettingsProvider(new TomlProviderOptions
        {
            FilePath = _testFile,
            CreateIfNotExists = true
        });
    }

    [Fact]
    public void Set_And_Get_StringValue()
    {
        _provider.Set("name", "TestApp");
        string result = _provider.Get("name", "default");
        Assert.Equal("TestApp", result);
    }

    [Fact]
    public void Set_And_Get_IntValue()
    {
        _provider.Set("count", 42);
        int result = _provider.Get("count", 0);
        Assert.Equal(42, result);
    }

    [Fact]
    public void NestedKeys_Work()
    {
        _provider.Set("database.host", "localhost");
        _provider.Set("database.port", 5432);
        Assert.Equal("localhost", _provider.Get("database.host", ""));
        Assert.Equal(5432, _provider.Get("database.port", 0));
    }

    [Fact]
    public void Remove_RemovesKey()
    {
        _provider.Set("toRemove", "value");
        Assert.True(_provider.ContainsKey("toRemove"));
        _provider.Remove("toRemove");
        Assert.False(_provider.ContainsKey("toRemove"));
    }

    public void Dispose()
    {
        _provider.Dispose();
        if (File.Exists(_testFile))
        {
            File.Delete(_testFile);
        }
    }
}
