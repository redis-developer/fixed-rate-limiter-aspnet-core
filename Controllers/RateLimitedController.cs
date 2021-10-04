using System;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace FixedRateLimiter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimitedController : ControllerBase
    {
        private readonly IDatabase _db;
        public RateLimitedController(IConnectionMultiplexer mux)
        {
            _db = mux.GetDatabase();
        }
        
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
    }
}