using Xunit;

namespace Sharingan.Providers.Yaml.Tests;

public class YamlFileSettingsProviderTests : IDisposable
{
    private readonly string _testFile;
    private readonly YamlFileSettingsProvider _provider;

    public YamlFileSettingsProviderTests()
    {
        _testFile = Path.Combine(Path.GetTempPath(), $"yaml_test_{Guid.NewGuid()}.yaml");
        _provider = new YamlFileSettingsProvider(new YamlProviderOptions
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
    public void Remove_RemovesKey()
    {
        _provider.Set("toRemove", "value");
        Assert.True(_provider.ContainsKey("toRemove"));
        _provider.Remove("toRemove");
        Assert.False(_provider.ContainsKey("toRemove"));
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        _provider.Set("key1", "value1");
        _provider.Set("key2", "value2");
        _provider.Clear();
        Assert.Equal(0, _provider.Count);
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
