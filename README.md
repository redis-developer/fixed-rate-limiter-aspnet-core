# Fixed Window Rate Limiter ASP.NET Core Example

This example demonstrates how to setup rate limiting for ASP.NET Core apps. This example uses [Basic Authentication](https://en.wikipedia.org/wiki/Basic_access_authentication) to limit api requests per-route per-api key.

## Implementation Details

The `RateLimitedController` sets up a `Simple` route accessible at `http://localhost:5000/api/ratelimited/simple`, it extracts the api key from the authorization header, and checks to see if that api key is being rate limited

```csharp
[HttpPost("simple")]
public async Task<IActionResult> Simple([FromHeader]string authorization)
{
    var encoded = string.Empty;
    if(!string.IsNullOrEmpty(authorization)) encoded = AuthenticationHeaderValue.Parse(authorization).Parameter;
    if (string.IsNullOrEmpty(encoded)) return new UnauthorizedResult();
    var apiKey = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split(':')[0];
    var script = Scripts.RateLimitScript;
    var key = $"{Request.Path.Value}:{apiKey}:{DateTime.UtcNow:hh:mm}";
    var res = await _db.ScriptEvaluateAsync(script, new {key = new RedisKey(key), expiry = 60, maxRequests = 10});
    if ((int) res == 1)
        return new StatusCodeResult(429);
    return Ok();
}
```

This flow uses a lua script to increment the counter and indicate whether a particular request has been throttled or not:

```bash
local requests = redis.call('INCR',@key)
redis.call('EXPIRE', @key, @expiry)
if requests < tonumber(@maxRequests) then
    return 0
else
    return 1
end
```

This script is run through the preparation engine of the [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/Scripting.html) library and is consequentially useable with the typical deference to KEYS/ARGV array access.

## Testing

To test this simply use `dotnet run`

and then send a series of API requests to the endpoint `http://localhost:5000/api/ratelimited/simple`

```bash
for n in {1..21}; do echo $(curl -s -w " HTTP %{http_code}, %{time_total} s" -X POST -H "Content-Length: 0" --user "foobar:password" http://localhost:5000/api/ratelimited/simple); sleep 0.5; done
```

Will Elicit between 1 and 11 429 responses (indicating that it has been throttled)