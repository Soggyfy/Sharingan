using Microsoft.Win32;
using Xunit;

namespace Sharingan.Providers.Registry.Tests;

public class RegistrySettingsProviderTests : IDisposable
{
    private readonly RegistrySettingsProvider _provider;
    private readonly string _testSubKey = $"Software\\SharinganTests\\{Guid.NewGuid()}";

    public RegistrySettingsProviderTests()
    {
        _provider = new RegistrySettingsProvider(new RegistryProviderOptions
        {
            SubKeyPath = _testSubKey,
            CreateIfNotExists = true,
            Hive = RegistryHive.CurrentUser
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
        _provider.Clear();
        _provider.Dispose();
        try
        {
            using RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\SharinganTests", true);
            key?.DeleteSubKeyTree(_testSubKey.Replace("Software\\SharinganTests\\", ""), false);
        }
        catch { }
    }
}
