namespace Mozart.Play.Inventory.Service
{
    public record GrantItemsDto(Guid userId, Guid CatalogItemId, int Quantity);
    public record InventoryItemDto(Guid CatalogItemId, int Quantity, DateTimeOffset AcquiredDate);
}

