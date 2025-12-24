using Xunit;

namespace Sharingan.Providers.SQLite.Tests;

public class SQLiteSettingsProviderTests : IDisposable
{
    private readonly string _testDb;
    private readonly SQLiteSettingsProvider _provider;

    public SQLiteSettingsProviderTests()
    {
        _testDb = Path.Combine(Path.GetTempPath(), $"sqlite_test_{Guid.NewGuid()}.db");
        _provider = new SQLiteSettingsProvider(new SQLiteProviderOptions
        {
            DatabasePath = _testDb,
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
    public void Set_And_Get_BoolValue()
    {
        _provider.Set("enabled", true);
        bool result = _provider.Get("enabled", false);
        Assert.True(result);
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

        // SQLite may hold the file briefly after disposal, retry deletion with backoff
        if (File.Exists(_testDb))
        {
            int maxRetries = 5;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Delete(_testDb);
                    break;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(100 * (i + 1)); // Exponential backoff
                }
            }
        }
    }
}
