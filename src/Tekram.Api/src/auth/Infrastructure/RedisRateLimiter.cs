namespace Tekram.Api.src.auth.Infrastructure;

using StackExchange.Redis;

/// <summary>
/// Redis-backed fixed-window rate limiter helper.
///
/// Uses Redis INCR + EXPIRE for atomic, cross-instance rate limiting.
/// Key format: "ratelimit:{policy}:{identifier}" (e.g. "ratelimit:login:user@x.com:192.168.1.1").
///
/// Decision: fail-open on Redis connection errors — transient Redis outages
/// must not block customer login/OTP flows. Rate limiting degrades gracefully
/// under infrastructure stress.
/// </summary>
public static class RedisRateLimiter
{
    /// <summary>
    /// Atomically increments a rate-limit counter and returns whether the
    /// caller is within the allowed limit.
    ///
    /// The key is auto-expired after the window duration on first increment.
    /// </summary>
    public static async Task<bool> IsAllowedAsync(IDatabase db, string key, int limit, TimeSpan window)
    {
        try
        {
            long count = await db.StringIncrementAsync(key);

            if (count == 1)
            {
                await db.KeyExpireAsync(key, window);
            }

            return count <= limit;
        }
        catch (RedisConnectionException)
        {
            // Fail-open: allow through when Redis is unreachable
            return true;
        }
        catch (RedisServerException)
        {
            // Fail-open: allow through on Redis server errors (OOM, etc.)
            return true;
        }
        catch (TimeoutException)
        {
            // Fail-open: Redis timeout should not block auth
            return true;
        }
    }
}
