using Xunit;

namespace Sharingan.Providers.Encrypted.Tests;

public class EncryptedSettingsProviderTests : IDisposable
{
    private readonly InMemorySettingsProvider _innerProvider;
    private readonly EncryptedSettingsProvider _provider;

    public EncryptedSettingsProviderTests()
    {
        _innerProvider = new InMemorySettingsProvider();
        _provider = new EncryptedSettingsProvider(_innerProvider, new EncryptedProviderOptions
        {
            EncryptedKeyPatterns = ["*"]
        });
    }

    [Fact]
    public void Set_And_Get_StringValue_Encrypted()
    {
        _provider.Set("secret", "MyPassword123");
        string result = _provider.Get("secret", "default");
        Assert.Equal("MyPassword123", result);
    }

    [Fact]
    public void InnerProvider_HasDifferentValue()
    {
        _provider.Set("apiKey", "SecretKey123");
        string innerValue = _innerProvider.Get<string>("apiKey", "");
        Assert.NotEqual("SecretKey123", innerValue);
        Assert.True(innerValue.Length > 0);
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
    public void IsKeyEncrypted_ReturnsTrue()
    {
        Assert.True(_provider.IsKeyEncrypted("anyKey"));
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
