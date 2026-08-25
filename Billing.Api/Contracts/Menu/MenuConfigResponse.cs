namespace Ecunexo.Billing.Api.Contracts.Menu;

public sealed record MenuConfigResponse(IReadOnlyList<MenuItemResponse> Items);

public sealed record MenuItemResponse(
    string Id,
    string Label,
    string? Icon,
    string? Path,
    IReadOnlyList<string>? Permissions,
    IReadOnlyList<MenuSubItemResponse>? Children,
    string? Position);

public sealed record MenuSubItemResponse(
    string Id,
    string Label,
    string? Path,
    IReadOnlyList<string>? Permissions,
    IReadOnlyList<MenuSubItemResponse>? Children);
