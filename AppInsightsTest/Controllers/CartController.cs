using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AppInsightsTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CartController : ControllerBase
    {
        private static readonly Random _Random = new Random();
        private static readonly object _Lock = new object();
        private readonly ILogger<CartController> _logger;
        private readonly IMemoryCache _memoryCache;

        public CartController(IMemoryCache memoryCache, ILogger<CartController> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<CartItem>> GetItems([FromQuery]int cartId)
        {
            // NOTE: Bad practice to use an external id as a cache key :)
            _logger.LogInformation("Get request for cart {id}", cartId);
            await RandomDelayUpTo100ms();
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
            await RequiredItemDelayAsync(item.ItemId);
            return Ok();
        }

        private static async Task RequiredItemDelayAsync(int cartId)
        {
            var delay = cartId switch
            {
                <= 500 => 20,
                <= 1000 => 50,
                <= 2000 => 100,
                _ => throw new ArgumentOutOfRangeException($"{nameof(cartId)} must not be greater than 2000")
            };
            await Task.Delay(delay);
        }
        
        private async Task RandomDelayUpTo100ms()
        {
            int delay;
            lock (_Lock)
            {
                delay = _Random.Next(100);
            }
            await Task.Delay(delay);
        }
        
    }
}