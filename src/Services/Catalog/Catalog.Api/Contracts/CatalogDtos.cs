namespace Catalog.Api.Contracts;

public record RestaurantDto(Guid Id, string Name, string Cuisine, string Address, bool IsOpen);

public record MenuItemDto(Guid Id, string Name, string Description, decimal Price, bool IsAvailable, int Stock);

public record MenuDto(Guid RestaurantId, string RestaurantName, IReadOnlyList<MenuItemDto> Items);

public record CachedResult<T>(T Value, bool FromCache);

public record CreateRestaurantRequest(string Name, string Cuisine, string Address);

public record AddMenuItemRequest(string Name, string Description, decimal Price);
