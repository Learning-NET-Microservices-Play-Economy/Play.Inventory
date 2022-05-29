using Mozart.Play.Inventory.Service.Entities;

namespace Mozart.Play.Inventory.Service
{
    public static class Extensions
    {
        public static InventoryItemDto AsDto(this InventoryItem item)
        {
            return new InventoryItemDto(
                item.CatalogItemId,
                item.Quantity,
                item.AcquiredDate
            );
        }
    }
}

