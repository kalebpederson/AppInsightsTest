using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AppInsightsTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ILogger<CartController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IRandomNumberProvider _randomNumberProvider;
        private readonly TelemetryClient _telemetryClient;

        public CartController(IMemoryCache memoryCache, IRandomNumberProvider randomNumberProvider, TelemetryClient telemetryClient, ILogger<CartController> logger)
        {
            _memoryCache = memoryCache;
            _randomNumberProvider = randomNumberProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<CartItem>> GetItems([FromQuery]int cartId)
        {
            // NOTE: Bad practice to use an external id as a cache key :)
            _logger.LogInformation("Get request for cart {id}", cartId);
            await RandomDelayUpToMs(100);
            return _memoryCache.GetOrCreate(cartId, _ => new List<CartItem>());
        }


        [Route("")]
        [HttpPost]
        public async Task<ActionResult> AddItem([FromQuery]int cartId, [FromBody] CartItem item)
        {
            _logger.LogInformation("Post request for cart item {@item}", item);
            var items = _memoryCache.GetOrCreate(cartId, _ => new List<CartItem>());
            items.Add(item);
            _memoryCache.Set(cartId, items);
            await TryOrLogRequiredItemDelayAsync(cartId);
            return Ok();
        }

        private async Task TryOrLogRequiredItemDelayAsync(int cartId)
        {
            try
            {
                await RequiredItemDelayAsync(cartId);
            }
            catch (Exception ex)
            {
                // TODO: Step 13 - Explicitly track the exceptions that could/should show up within the
                // debug snapshots.
                _telemetryClient.TrackException(ex, new Dictionary<string,string>
                {
                    {"cartId", cartId.ToString()}
                });
                throw;
            }
        }

        private static async Task RequiredItemDelayAsync(int cartId)
        {
            var delay = cartId switch
            {
                <= 500 => 50,
                <= 1000 => 100,
                <= 2000 => 300,
                _ => throw new ArgumentOutOfRangeException($"{nameof(cartId)} must not be greater than 2000")
            };
            await Task.Delay(delay);
        }
        
        private async Task RandomDelayUpToMs(int maxValue)
        {
            // TODO: Bonus 1 - We can track dependencies by recording operations which will create an operation
            // hierarchy.
            using (_telemetryClient.StartOperation<DependencyTelemetry>("RandomDelayDependency"))
            {
                await Task.Delay(RandomValueUpTo(maxValue));
            }
        }

        private int RandomValueUpTo(int maxValue)
        {
            return _randomNumberProvider.GetRandomNumberUpTo(maxValue);
        }
        
    }
}