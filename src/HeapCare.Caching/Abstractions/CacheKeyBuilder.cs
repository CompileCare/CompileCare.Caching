namespace HeapCare.Caching.Abstractions;

/// <summary>
/// Creates explicit global or tenant-scoped cache keys. Tenant-owned data must use
/// ForTenant; raw keys are not tenant-safe by themselves.
/// </summary>
public static class CacheKeyBuilder
{
    public static string ForTenant(Guid tenantId, string key)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));

        ValidateKey(key);
        return $"tenant:{tenantId:N}:{key}";
    }

    public static string ForGlobal(string key)
    {
        ValidateKey(key);
        return $"global:{key}";
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 256 || key.Any(char.IsControl))
            throw new ArgumentException("Cache key must be 1-256 characters and contain no control characters.", nameof(key));
    }
}