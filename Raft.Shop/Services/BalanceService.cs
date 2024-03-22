namespace Raft.Shop;

public class BalanceService
{
    private readonly RaftService _raftService;

    public BalanceService(RaftService raftService)
    {
        _raftService = raftService;
    }


    public async Task<(decimal balance, int version)> GetUserBalance(string userId)
    {
        try
        {
            var userBalanceKey = $"{userId.Replace(" ", "-")}-balance";
            Console.WriteLine($"Getting balance for {userBalanceKey}");
            return await _raftService.StrongGet<decimal>(userBalanceKey);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting balance {e.Message}");
            return (0, 0);
        }
    }

    public async Task<bool> DepositOrWithdraw(string userId, decimal currentBalance, int currentVersion, decimal amount)
    {
        var newBalance = currentBalance + amount;

        try
        {
            var userBalanceKey = $"{userId.Replace(" ", "-")}-balance";
            var response = await _raftService.CompareAndSwap(userBalanceKey, newBalance.ToString(), currentBalance.ToString(), currentVersion);
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error updating balance {e.Message}");
            return false;
        }
    }
}
