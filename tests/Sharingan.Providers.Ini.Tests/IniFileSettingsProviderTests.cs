using Xunit;

namespace Sharingan.Providers.Ini.Tests;

public class IniFileSettingsProviderTests : IDisposable
{
    private readonly string _testFile;
    private readonly IniFileSettingsProvider _provider;

    public IniFileSettingsProviderTests()
    {
        _testFile = Path.Combine(Path.GetTempPath(), $"ini_test_{Guid.NewGuid()}.ini");
        _provider = new IniFileSettingsProvider(new IniProviderOptions
        {
            FilePath = _testFile,
            CreateIfNotExists = true
        });
    }

    [Fact]
    public void Set_And_Get_StringValue()
    {
        _provider.Set("General.Name", "TestApp");
        string result = _provider.Get("General.Name", "default");
        Assert.Equal("TestApp", result);
    }

    [Fact]
    public void Set_And_Get_IntValue()
    {
        _provider.Set("Settings.Count", 42);
        int result = _provider.Get("Settings.Count", 0);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Set_And_Get_BoolValue()
    {
        _provider.Set("Settings.Enabled", true);
        bool result = _provider.Get("Settings.Enabled", false);
        Assert.True(result);
    }

    [Fact]
    public void Remove_RemovesKey()
    {
        _provider.Set("ToRemove.Key", "value");
        Assert.True(_provider.ContainsKey("ToRemove.Key"));
        _provider.Remove("ToRemove.Key");
        Assert.False(_provider.ContainsKey("ToRemove.Key"));
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        _provider.Set("Key1", "value1");
        _provider.Set("Key2", "value2");
        Assert.True(_provider.Count >= 2);
        _provider.Clear();
        Assert.Equal(0, _provider.Count);
    }

    [Fact]
    public void GetAllKeys_ReturnsAllKeys()
    {
        _provider.Clear();
        _provider.Set("Section.Key1", "v1");
        _provider.Set("Section.Key2", "v2");
        List<string> keys = _provider.GetAllKeys().ToList();
        Assert.Contains("Section.Key1", keys);
        Assert.Contains("Section.Key2", keys);
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
