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

    internal static IReadOnlyList<string> GetValidationErrors(CacheSettings settings)
    {
        var errors = new List<string>();

        if (settings.DefaultExpirationSeconds <= 0)
            errors.Add("Cache default expiration must be positive.");

        if (settings.LocalCacheExpirationSeconds <= 0)
            errors.Add("Cache local expiration must be positive.");

        if (settings.LocalCacheExpirationSeconds > settings.DefaultExpirationSeconds)
            errors.Add("Local cache expiration cannot exceed distributed cache expiration.");

        if (settings.UseRedis && string.IsNullOrWhiteSpace(settings.RedisConnection))
            errors.Add("RedisConnection is required when UseRedis is enabled.");

        if (!IsValidKeyPrefix(settings.KeyPrefix))
            errors.Add("Cache key prefix must contain 1-64 ASCII letters, digits, dots, underscores or hyphens.");

        return errors;
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