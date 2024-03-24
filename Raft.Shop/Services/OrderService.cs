using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace Raft.Shop;

public class OrderService
{
    private readonly RaftService _raftService;
    private readonly BalanceService _balanceService;
    private readonly InventoryService _inventoryService;

    public OrderService(RaftService raftService, BalanceService balanceService, InventoryService inventoryService)
    {
        _raftService = raftService;
        _balanceService = balanceService;
        _inventoryService = inventoryService;
    }

    public async Task<bool> PlaceOrder(Order order)
    {
        order.Id = Guid.NewGuid().ToString();
        var orderKey = $"order-info-{order.Id}";
        var orderStatusKey = $"order-status-{order.Id}";

        try
        {
            var storeOrderSuccessful = await _raftService.CompareAndSwap(orderKey, order.ToString(), "", 0);
            var orderStatusSuccessful = await _raftService.CompareAndSwap(orderStatusKey, "pending", "", 0);

            var addedToPendingOrders = false;
            while (!addedToPendingOrders)
            {
                var (value, version) = await GetPendingOrdersList();
                var currentPendingOrdersCopy = value.ToList();
                currentPendingOrdersCopy.Add(order.Id);
                var pendingOrdersSuccessful = await _raftService.CompareAndSwap("pending-orders", currentPendingOrdersCopy, value, version);
                addedToPendingOrders = pendingOrdersSuccessful;
            }

            return storeOrderSuccessful && orderStatusSuccessful && addedToPendingOrders;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error placing order {e.Message}");
            return false;
        }
    }

    public async Task<(List<string> pendingOrders, int version)> GetPendingOrdersList()
    {
        try
        {
            return await _raftService.StrongGet<List<string>>("pending-orders");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting pending orders {e.Message}");
            return (new List<string>(), 0);
        }
    }

    public async Task<(List<string> rejectedOrders, int version)> GetRejectedOrdersList()
    {
        try
        {
            return await _raftService.StrongGet<List<string>>("rejected-orders");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting rejected orders {e.Message}");
            return (new List<string>(), 0);
        }
    }

    public async Task<(List<string> processedOrders, int version)> GetProcessedOrdersList()
    {
        try
        {
            return await _raftService.StrongGet<List<string>>("processed-orders");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting processed orders {e.Message}");
            return (new List<string>(), 0);
        }
    }

    public async Task<(Order order, string orderStatus)> GetOrder(string orderId)
    {
        var orderKey = $"order-info-{orderId}";
        var orderStatusKey = $"order-status-{orderId}";

        var (order, _) = await _raftService.StrongGet<Order>(orderKey);
        var (status, _) = await _raftService.StrongGet(orderStatusKey);

        return (order, status);
    }

    public async Task<bool> ProcessOrder(Order order)
    {
        var processOrderTransaction = new Transaction()
            .AddStep(order, DebitCustomer, RollbackDebitCustomer)
            .AddStep(order, ReduceStock, RollbackReduceStock)
            .AddStep(order, CreditVendor, RollbackCreditVendor)
            .AddStep(order, UpdateOrderStatus)
            .AddStep(order, RemoveFromPendingOrders)
            .AddStep(order, AddToProcessedOrders);

        return await processOrderTransaction.Execute();
    }

    private async Task<bool> DebitCustomer(Order order)
    {
        Console.WriteLine($"Debiting customer {order.UserId} for {order.Total} {order.Id}");
        Console.WriteLine($"------------------------------------------------");
        while (true)
        {
            var (balance, version) = await _balanceService.GetUserBalance(order.UserId);

            if (balance - order.Total < 0)
            {
                Console.WriteLine($"Not enough balance for user {order.UserId}");
                await RejectOrder(order);
                return false;
            }

            var success = await _balanceService.DepositOrWithdraw(order.UserId, balance, version, -order.Total);

            if (success)
            {
                return true;
            }
        }
    }

    private async Task<bool> ReduceStock(Order order)
    {
        Console.WriteLine($"Reducing stock for order {order.Id}");
        Console.WriteLine($"------------------------------------------------");
        var reduceStockTransaction = new Transaction();

        foreach (var item in order.Items)
        {
            reduceStockTransaction.AddStep(item, ReduceItemStock, RollbackReduceItemStock);
        }

        var success = await reduceStockTransaction.Execute();

        if (!success)
        {
            await RejectOrder(order);
        }

        return success;
    }

    private async Task<bool> ReduceItemStock(OrderItem item)
    {
        Console.WriteLine($"Reducing stock for item {item.ItemName}");
        Console.WriteLine($"------------------------------------------------");
        while (true)
        {
            var (stock, version) = await _inventoryService.GetStoreItemStock(item.ItemName);

            if (stock - item.Quantity < 0)
            {
                Console.WriteLine($"Not enough stock for item {item.ItemName}");
                return false;
            }

            var success = await _inventoryService.AddOrRemoveStock(item.ItemName, stock, version, -item.Quantity);

            if (success)
            {
                return true;
            }
        }
    }

    private async Task<bool> CreditVendor(Order order)
    {
        Console.WriteLine($"Crediting vendor for order {order.Id}");
        Console.WriteLine($"------------------------------------------------");
        while (true)
        {
            var (vendorBalance, vendorVersion) = await _balanceService.GetUserBalance("vendor");
            var success = await _balanceService.DepositOrWithdraw("vendor", vendorBalance, vendorVersion, order.Total);

            if (success)
            {
                return true;
            }
        }
    }

    private async Task<bool> UpdateOrderStatus(Order order)
    {
        Console.WriteLine($"Updating order status for order {order.Id}");
        Console.WriteLine($"------------------------------------------------");
        while (true)
        {
            var (orderStatus, version) = await _raftService.StrongGet($"order-status-{order.Id}");
            if (orderStatus != "pending")
            {
                Console.WriteLine($"Order {order.Id} is already processed");
                return false;
            }

            var success = await _raftService.CompareAndSwap($"order-status-{order.Id}", $"processed-by-{Guid.NewGuid()}", orderStatus, version);

            if (success)
            {
                return true;
            }
        }
    }

    private async Task<bool> RemoveFromPendingOrders(Order order)
    {
        Console.WriteLine($"Removing order {order.Id} from pending orders");
        Console.WriteLine($"------------------------------------------------");
        while (true)
        {
            var (pendingOrders, version) = await GetPendingOrdersList();

            var updatedPendingOrders = pendingOrders.ToList();

            updatedPendingOrders.Remove(order.Id);

            var success = await _raftService.CompareAndSwap("pending-orders", updatedPendingOrders, pendingOrders, version);

            if (success)
            {
                return true;
            }
        }
    }

    private async Task RollbackDebitCustomer(Order order)
    {
        Console.WriteLine($"Rolling back debit from {order.UserId} for order {order.Id}");
        Console.WriteLine($"------------------------------------------------");
        while (true)
        {
            var (balance, version) = await _balanceService.GetUserBalance(order.UserId);
            var success = await _balanceService.DepositOrWithdraw(order.UserId, balance, version, order.Total);

            if (success)
            {
                return;
            }
        }
    }

    private async Task RollbackReduceStock(Order order)
    {
        Console.WriteLine($"Rolling back stock reduction for order {order.Id}");

        foreach (var item in order.Items)
        {
            await RollbackReduceItemStock(item);
        }
    }

    private async Task RollbackCreditVendor(Order order)
    {
        Console.WriteLine($"Rolling back credit to vendor for order {order.Id}");

        while (true)
        {
            var (vendorBalance, vendorVersion) = await _balanceService.GetUserBalance("vendor");
            var success = await _balanceService.DepositOrWithdraw("vendor", vendorBalance, vendorVersion, -order.Total);

            if (success)
            {
                return;
            }
        }
    }

    private async Task RollbackReduceItemStock(OrderItem item)
    {
        Console.WriteLine($"Rolling back stock reduction for item {item.ItemName}");

        while (true)
        {
            var (stock, version) = await _inventoryService.GetStoreItemStock(item.ItemName);

            var success = await _inventoryService.AddOrRemoveStock(item.ItemName, stock, version, item.Quantity);

            if (success)
            {
                return;
            }
        }
    }

    private async Task RejectOrder(Order order)
    {
        Console.WriteLine($"Rejecting order {order.Id}");
        Console.WriteLine($"------------------------------------------------");

        while (true)
        {
            var (orderStatus, version) = await _raftService.StrongGet($"order-status-{order.Id}");
            if (orderStatus != "pending")
            {
                return;
            }

            var success = await _raftService.CompareAndSwap($"order-status-{order.Id}", $"rejected-by-{Guid.NewGuid()}", orderStatus, version);

            if (success)
            {
                await RemoveFromPendingOrders(order);
                await AddToRejectedOrders(order);
                return;
            }
        }
    }

    private async Task AddToRejectedOrders(Order order)
    {
        Console.WriteLine($"Adding order {order.Id} to rejected orders");
        Console.WriteLine($"------------------------------------------------");

        while (true)
        {
            var (rejectedOrders, version) = await GetRejectedOrdersList();

            var updatedRejectedOrders = rejectedOrders.ToList();

            updatedRejectedOrders.Add(order.Id);

            var success = await _raftService.CompareAndSwap("rejected-orders", updatedRejectedOrders, rejectedOrders, version);

            if (success)
            {
                return;
            }
        }
    }

    private async Task<bool> AddToProcessedOrders(Order order)
    {
        Console.WriteLine($"Adding order {order.Id} to processed orders");
        Console.WriteLine($"------------------------------------------------");

        while (true)
        {
            var (processedOrders, version) = await GetProcessedOrdersList();

            var updatedProcessedOrders = processedOrders.ToList();

            updatedProcessedOrders.Add(order.Id);

            var success = await _raftService.CompareAndSwap("processed-orders", updatedProcessedOrders, processedOrders, version);

            if (success)
            {
                return true;
            }
        }
    }
}
