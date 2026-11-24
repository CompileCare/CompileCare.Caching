namespace CompileCare.Caching;

/// <summary>Configuration for CompileCare HybridCache.</summary>
public sealed class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public bool UseRedis { get; set; }
    public string? RedisConnection { get; set; }
    public string KeyPrefix { get; set; } = "compilecare";
    public int DefaultExpirationSeconds { get; set; } = 300;
    public int LocalCacheExpirationSeconds { get; set; } = 120;

    public TimeSpan DefaultExpiration => TimeSpan.FromSeconds(DefaultExpirationSeconds);
    public TimeSpan LocalCacheExpiration => TimeSpan.FromSeconds(LocalCacheExpirationSeconds);

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