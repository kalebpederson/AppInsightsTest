using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AppInsightsTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PerfController : ControllerBase
    {
        private static readonly Random _random = new();
        private static readonly object _syncLock = new();
        private static long _generatedCount = 0;

        [Route("")]
        [HttpGet]
        public async Task<int> Run([FromQuery] int cartId)
        {
            await ForcedDelayAsync().ConfigureAwait(false);
            // force a delay
            var delay = ForcedSynchronousWait();

            var vestige = await SomeOtherMethodAsync(delay);
            
            return vestige;
        }
        
        [Route("generated")]
        [HttpGet]
        public ValueTask<long> Generated()
        {
            lock (_syncLock)
            {
                return ValueTask.FromResult(_generatedCount);
            }
        }

        private static ValueTask<int> SomeOtherMethodAsync(int starter)
        {
            const int modulus = 1987;
            var vestige = starter;
            for (var i = 0; i < 100_000; ++i)
            {
                vestige = (vestige + i) % modulus;
            }
            return ValueTask.FromResult(vestige * 10_000 + starter);
        }

        private static int ForcedSynchronousWait()
        {
            return RandomDelayBetweenMs(300, 600).GetAwaiter().GetResult();
        }

        private static async Task ForcedDelayAsync()
        {
            await Task.Delay(11);
        }

        private static Task<int> RandomDelayBetweenMs(int min, int max)
        {
            var delay = GetRandomValueBetween(min, max);
            Thread.Sleep(delay);
            return Task.FromResult(delay);
        }

        private static int GetRandomValueBetween(int min, int max)
        {
            lock (_syncLock)
            {
                _generatedCount++;
                return _random.Next(min, max);
            }
        }

    }
}
