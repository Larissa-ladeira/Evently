using StackExchange.Redis;

namespace Evently.Api.Services
{
    public class RedisService
    {
        private const string ReleaseLockScript =
            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public RedisService(IConfiguration configuration)
        {
            var connection = configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
            var options = ConfigurationOptions.Parse(connection);
            options.AbortOnConnectFail = false;

            _redis = ConnectionMultiplexer.Connect(options);
            _database = _redis.GetDatabase();
        }

        public async Task<string?> AcquireLockAsync(string key, TimeSpan expiry)
        {
            var token = Guid.NewGuid().ToString("N");
            var acquired = await _database.StringSetAsync(key, token, expiry, When.NotExists);
            return acquired ? token : null;
        }

        public async Task<bool> ReleaseLockAsync(string key, string token)
        {
            var result = await _database.ScriptEvaluateAsync(
                ReleaseLockScript,
                new RedisKey[] { key },
                new RedisValue[] { token });

            return (long)result == 1;
        }

        public async Task<long> IncrementAsync(string key, TimeSpan? expiry = null)
        {
            var result = await _database.StringIncrementAsync(key);
            if (expiry.HasValue && result == 1)
            {
                await _database.KeyExpireAsync(key, expiry.Value);
            }
            return result;
        }

        public async Task<long> GetAsync(string key)
        {
            var value = await _database.StringGetAsync(key);
            return value.HasValue ? (long)value : 0;
        }
    }
}
