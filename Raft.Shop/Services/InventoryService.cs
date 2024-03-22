namespace Raft.Shop;

public class InventoryService
{
    private readonly RaftService _raftService;

    public InventoryService(RaftService raftService)
    {
        _raftService = raftService;
    }

    public List<StoreItem> GetStoreItems()
    {
        var itemColors = new List<string> { "Red", "Blue", "Green", "Yellow", "Purple" };

        return itemColors.Select(color =>
            new StoreItem
            {
                Name = $"{color} Marble",
                Price = 1.00m,
                Stock = 0,
                StockVersion = 0
            }
        ).ToList();
    }

    public async Task<bool> AddOrRemoveStock(string itemName, int currentStock, int currentVersion, int amount)
    {
        var itemKey = $"{itemName.Replace(" ", "-").ToLower()}-stock";
        var newStock = currentStock + amount;
        
        try
        {
            var response = await _raftService.CompareAndSwap(itemKey, newStock.ToString(), currentStock.ToString(), currentVersion);
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error updating store item stock {e.Message}");
            return false;
        }
    }

    public async Task<(int stock, int version)> GetStoreItemStock(string itemName)
    {
        var itemKey = $"{itemName.Replace(" ", "-").ToLower()}-stock";

        try
        {
            return await _raftService.StrongGet<int>(itemKey);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting store item stock {e.Message}");
            return (0, 0);
        }
    }
}
