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
        private readonly IRepository<InventoryItem> _inventoryItemsRepository;
        private readonly IRepository<CatalogItem> _catalogItemRepository;

        public ItemsController(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemRepository)
        {
            _inventoryItemsRepository = inventoryItemsRepository;
            _catalogItemRepository = catalogItemRepository;
        }

        // GET: api/items
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var inventoryItemEntities = await _inventoryItemsRepository.GetManyAsync(q => q.UserId == userId);
            var itemIds = inventoryItemEntities.Select(q => q.CatalogItemId);

            var catalogItemEntities = await _catalogItemRepository.GetManyAsync(q => itemIds.Contains(q.Id));

            var result = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });


            return Ok(result);
        }

        // POST api/items
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] GrantItemsDto body)
        {
            var existing = await _inventoryItemsRepository.GetAsync(q => q.UserId == body.userId && q.CatalogItemId == body.CatalogItemId);

            if (existing == null)
            {
                existing = new InventoryItem
                {
                    CatalogItemId = body.CatalogItemId,
                    UserId = body.userId,
                    Quantity = body.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _inventoryItemsRepository.CreateAsync(existing);
            }
            else
            {
                existing.Quantity += body.Quantity;
                await _inventoryItemsRepository.UpdateAsync(existing);
            }

            return Ok();
        }
    }
}

