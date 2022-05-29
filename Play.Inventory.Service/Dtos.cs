namespace Mozart.Play.Inventory.Service
{
    public record GrantItemsDto(Guid userId, Guid CatalogItemId, int Quantity);

    public record InventoryItemDto(Guid CatalogItemId, string Name, string? Description, int Quantity, DateTimeOffset AcquiredDate);

    public record CatalogItemDto(Guid Id, string Name, string? Description);
}

