using Microsoft.AspNetCore.Mvc;
using Mozart.Play.Common;
using Mozart.Play.Inventory.Service.Entities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Mozart.Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _itemsRepository;

        public ItemsController(IRepository<InventoryItem> itemsRepository)
        {
            _itemsRepository = itemsRepository;
        }

        // GET: api/items
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var result = (await _itemsRepository.GetManyAsync(q => q.UserId == userId))
                        .Select(q => q.AsDto());

            return Ok(result);
        }

        // GET api/items/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/items
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] GrantItemsDto body)
        {
            var existing = await _itemsRepository.GetAsync(q => q.UserId == body.userId && q.CatalogItemId == body.CatalogItemId);

            if (existing == null)
            {
                existing = new InventoryItem
                {
                    CatalogItemId = body.CatalogItemId,
                    UserId = body.userId,
                    Quantity = body.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _itemsRepository.CreateAsync(existing);
            }
            else
            {
                existing.Quantity += body.Quantity;
                await _itemsRepository.UpdateAsync(existing);
            }

            return Ok();
        }

        // PUT api/items/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/items/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

