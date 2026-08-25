using System.Text.Json;
using Ecunexo.Billing.Api.Contracts.Menu;

namespace Ecunexo.Billing.Api.Menu;

public interface IBillingMenuProvider
{
    MenuConfigResponse GetFullMenu();
    MenuConfigResponse GetMenuForPermissions(IReadOnlyCollection<string> userPermissions);
}

public sealed class BillingMenuProvider : IBillingMenuProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly MenuConfigResponse _fullMenu;

    public BillingMenuProvider(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Menu", "billing-menu.json");
        var json = File.ReadAllText(path);
        _fullMenu = JsonSerializer.Deserialize<MenuConfigResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("No se pudo cargar billing-menu.json.");
    }

    public MenuConfigResponse GetFullMenu() => _fullMenu;

    public MenuConfigResponse GetMenuForPermissions(IReadOnlyCollection<string> userPermissions) =>
        new(FilterItems(_fullMenu.Items, userPermissions));

    private static IReadOnlyList<MenuItemResponse> FilterItems(
        IReadOnlyList<MenuItemResponse> items,
        IReadOnlyCollection<string> userPermissions) =>
        items
            .Select(item => FilterTopItem(item, userPermissions))
            .Where(item => item is not null)
            .Cast<MenuItemResponse>()
            .ToList();

    private static MenuItemResponse? FilterTopItem(
        MenuItemResponse item,
        IReadOnlyCollection<string> userPermissions)
    {
        var children = FilterSubItems(item.Children, userPermissions);
        if (!IsVisible(item.Permissions, userPermissions) && children.Count == 0)
            return null;

        return item with { Children = children.Count > 0 ? children : null };
    }

    private static IReadOnlyList<MenuSubItemResponse> FilterSubItems(
        IReadOnlyList<MenuSubItemResponse>? items,
        IReadOnlyCollection<string> userPermissions)
    {
        if (items is null || items.Count == 0)
            return [];

        return items
            .Select(item => FilterSubItem(item, userPermissions))
            .Where(item => item is not null)
            .Cast<MenuSubItemResponse>()
            .ToList();
    }

    private static MenuSubItemResponse? FilterSubItem(
        MenuSubItemResponse item,
        IReadOnlyCollection<string> userPermissions)
    {
        var children = FilterSubItems(item.Children, userPermissions);
        if (!IsVisible(item.Permissions, userPermissions) && children.Count == 0)
            return null;

        return item with { Children = children.Count > 0 ? children : null };
    }

    private static bool IsVisible(IReadOnlyList<string>? required, IReadOnlyCollection<string> userPermissions)
    {
        if (required is null || required.Count == 0)
            return true;

        return required.Any(userPermissions.Contains);
    }
}
