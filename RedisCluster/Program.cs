using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CacheManager.Core;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisCluster
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //await RedisInit();
            CacheMangerInit();

            Console.ReadKey();
        }

        private static async Task RedisInit()
        {
            var redisService = new Redis();

            for (int i = 0; i < 25; i++)
            {
                await redisService.SetCache("name", "xdAndrew");
                var data = await redisService.GetCache($"name");
                Console.WriteLine($"Fetching data from redis cache '{data}'.");
            }
            

            var employee = new Employee("1", "Micheal", 25);
            await redisService.SetCache("Employee1", JsonConvert.SerializeObject(employee));
            var employeeFromCache = JsonConvert.DeserializeObject<Employee>(await redisService.GetCache("Employee1"));

            Console.WriteLine(employeeFromCache.Id);
            Console.WriteLine(employeeFromCache.Name);
            Console.WriteLine(employeeFromCache.Age);
            Console.ReadKey();
        }

        private static void CacheMangerInit()
        {
            var redisService = new RedisCacheManager();

            for (int i = 0; i < 10; i++)
            {
                redisService.SetCache($"name{i}", "xdAndrew");
                var data = redisService.GetCache($"name{i}");
                Console.WriteLine($"Fetching data from redis cache '{data}'.");
            }


            var employee = new Employee("1", "Micheal", 25);
            redisService.SetCache("Employee1", JsonConvert.SerializeObject(employee));
            var employeeFromCache = JsonConvert.DeserializeObject<Employee>(redisService.GetCache("Employee1"));

            Console.WriteLine(employeeFromCache.Id);
            Console.WriteLine(employeeFromCache.Name);
            Console.WriteLine(employeeFromCache.Age);
            Console.ReadKey();
        }
    }

    public interface IRedis
    {
        Task<string> GetCache(string key);
        Task SetCache(string key, string value);
    }


    public class RedisCacheManager 
    {
        private readonly ICacheManager<object> _cache;

        public RedisCacheManager()
        {

            var options = new ConfigurationOptions
            {

                EndPoints = { "address" },
                Password = "password",
                Ssl = false
            };

            var redisMultiplexer = ConnectionMultiplexer.Connect(options);
            

            _cache = CacheFactory.Build<object>(settings =>
            {
                settings
                   .WithRedisConfiguration("redis", redisMultiplexer, enableKeyspaceNotifications: true)
                    .WithMaxRetries(100)
                    .WithRetryTimeout(50)
                    .WithRedisBackplane("redis")
                    .WithRedisCacheHandle("redis", true);
            });

            //Load config from app.settings
            //_cache = CacheFactory.FromConfiguration<object>("redisAppConfig");
        }

        public string GetCache(string key)
        {
            return (string) _cache.Get(key);
        }

        public void SetCache(string key, string value)
        {
            _cache.Add(key, value);
        }

        public T GetOrAdd<T>(string key, Func<T> getFunc)
        {
            return (T)_cache.GetOrAdd(key, k => getFunc());
        }
    }


    public class Redis : IRedis
    {
        private readonly IDatabase _database;

        public Redis()
        {
            // Connect to docker container - default machine win 7 

            var options = new ConfigurationOptions
            {
                
                EndPoints = { "Address" },
                Password = "Password",
                Ssl = false
            };
          
             //options.CertificateSelection += OptionsOnCertificateSelection;

            var redis = ConnectionMultiplexer.Connect(options);
            _database = redis.GetDatabase();
        }

        //private static X509Certificate OptionsOnCertificateSelection(object s, string t,
        //    X509CertificateCollection local, X509Certificate remote, string[] a)
        //{
        //    var certPath = ConfigManager.GetAppSettings("certRoute");
        //    return new X509Certificate2(@"path);
        //}

        public async Task<string> GetCache(string key)
            => await _database.StringGetAsync(key);

        public async Task SetCache(string key, string value)
            => await _database.StringSetAsync(key, value);
    }
}

