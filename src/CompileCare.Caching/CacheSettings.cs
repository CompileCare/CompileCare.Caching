namespace CompileCare.Caching;

/// <summary>Configuration for CompileCare HybridCache.</summary>
public sealed class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public bool UseRedis { get; set; }
    public string? RedisConnection { get; set; }
    public string KeyPrefix { get; set; } = "compilecare";
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan LocalCacheExpiration { get; set; } = TimeSpan.FromMinutes(2);

    public int DefaultExpirationSeconds
    {
        get => (int)DefaultExpiration.TotalSeconds;
        set => DefaultExpiration = TimeSpan.FromSeconds(value);
    }

    public int LocalCacheExpirationSeconds
    {
        get => (int)LocalCacheExpiration.TotalSeconds;
        set => LocalCacheExpiration = TimeSpan.FromSeconds(value);
    }

    internal static bool IsValidKeyPrefix(string? prefix)
    {
        if (string.IsNullOrEmpty(prefix) || prefix.Length > 64)
            return false;

        foreach (var character in prefix)
        {
            if ((character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9') ||
                character is '.' or '_' or '-')
                continue;

            return false;
        }

        return true;
    }
}