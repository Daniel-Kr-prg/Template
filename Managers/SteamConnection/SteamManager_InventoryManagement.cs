using Steamworks;
using System.Threading.Tasks;

public class SteamManager_InventoryManagement
{
    public static async Task RefreshUserInventory()
    {
        if (!SteamClient.IsValid)
        {
            SteamManager.Instance.DebugWarning("[SteamManager_InventoryManagement] SteamClient not initialized!");
            return;
        }

        var result = await SteamInventory.GetAllItemsAsync();
        if (!result.HasValue)
        {
            SteamManager.Instance.DebugError("[SteamManager_InventoryManagement] Can't get user inventory.");
            return;
        }

        var items = result.Value.GetItems();
        SteamManager.Instance.DebugMessage($"[SteamManager_InventoryManagement] Inventory items count: {items.Length}");
    }

    public static async Task GrantItems(InventoryDef target, int amount)
    {
        if (!SteamClient.IsValid)
        {
            SteamManager.Instance.DebugWarning("[SteamManager_InventoryManagement] SteamClient not initialized!");
            return;
        }

        var result = await SteamInventory.GenerateItemAsync(target, amount);
        if (!result.HasValue)
        {
            SteamManager.Instance.DebugError("[SteamManager_InventoryManagement] Can't get user inventory.");
            return;
        }
    }
}
