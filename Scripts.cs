using StackExchange.Redis;
namespace FixedRateLimiter
{
    public static class Scripts
    {
        public static LuaScript RateLimitScript => LuaScript.Prepare(RATE_LIMITER);

        private const string RATE_LIMITER = @"            
            local requests = redis.call('INCR',@key)
            redis.call('EXPIRE', @key, @expiry)
            if requests < tonumber(@maxRequests) then
                return 0
            else
                return 1
            end
            "; 
    }
}