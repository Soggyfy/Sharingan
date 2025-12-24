using Xunit;

namespace Sharingan.Providers.Xml.Tests;

public class XmlFileSettingsProviderTests : IDisposable
{
    private readonly string _testFile;
    private readonly XmlFileSettingsProvider _provider;

    public XmlFileSettingsProviderTests()
    {
        _testFile = Path.Combine(Path.GetTempPath(), $"xml_test_{Guid.NewGuid()}.xml");
        _provider = new XmlFileSettingsProvider(new XmlProviderOptions
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
        _provider.Set("app.database.host", "localhost");
        string result = _provider.Get("app.database.host", "");
        Assert.Equal("localhost", result);
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
