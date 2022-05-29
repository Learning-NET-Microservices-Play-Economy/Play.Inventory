using Microsoft.AspNetCore.Mvc;
using Mozart.Play.Common;
using Mozart.Play.Inventory.Service.Clients;
using Mozart.Play.Inventory.Service.Entities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Mozart.Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _itemsRepository;
        private readonly CatalogClient _catalogClient;

        public ItemsController(IRepository<InventoryItem> itemsRepository, CatalogClient catalogClient)
        {
            _itemsRepository = itemsRepository;
            _catalogClient = catalogClient;
        }

        // GET: api/items
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var catalogItems = await _catalogClient.GetCatalogItemsAsync();
            var inventoryItemEntities = await _itemsRepository.GetManyAsync(q => q.UserId == userId);

            var result = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });


            return Ok(result);
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
    }
}

