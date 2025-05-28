using System;
using System.Collections.Concurrent;

namespace Occtoo.Onboarding.Sdk.AuthenticationHandler;
public sealed class AccessTokensCacheManager
{
	private static readonly object _lock = new object();

	private readonly ConcurrentDictionary<string, AccessTokenCacheEntry> _cache = new ConcurrentDictionary<string, AccessTokenCacheEntry>();

    public void AddOrUpdateToken(string clientId, TokenInfo accessToken)
    {
        var newToken = new AccessTokenCacheEntry(accessToken);
        _cache.TryRemove(clientId, out _);
        _cache.TryAdd(clientId, newToken);
    }

    public TokenInfo? GetToken(string clientId)
    {
	    lock (_lock)
	    {
		    _cache.TryGetValue(clientId, out var tokenCacheEntry);
		    return tokenCacheEntry?.IsValid == true ? tokenCacheEntry.Token : null;
		}
    }

    private class AccessTokenCacheEntry(TokenInfo token)
    {
        public TokenInfo Token { get; } = token;
        private DateTime RefreshAfterDate { get; } = DateTime.UtcNow + TimeSpan.FromSeconds(token.ExpiresIn / 2.0);
        public bool IsValid => DateTime.UtcNow < RefreshAfterDate;
    }
}
