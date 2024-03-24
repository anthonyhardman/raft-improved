using System.Text.Json;

namespace Raft.Shop;

public class OrderItem
{
    public string ItemName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class Order
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal Total => Items.Sum(i => i.Price * i.Quantity);

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }  
}