using System.Text.Json;
using Raft.Grpc;

namespace Raft.Shop;

public class RaftService
{
    private readonly HttpClient _httpClient;

    public RaftService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("RaftClient");
    }

    
    public async Task<(string value, int version)> StrongGet(string key)
    {
        var response = await _httpClient.GetFromJsonAsync<StrongGetResponse>($"api/storage/strong?key={key}");

        if (response?.Value == null)
        {
            throw new Exception("Value not found");
        }

        return (response.Value, response.Version);
    }

    public async Task<(T value, int version)> StrongGet<T>(string key)
    {
        var response = await _httpClient.GetFromJsonAsync<StrongGetResponse>($"api/storage/strong?key={key}");

        if (response?.Value == null)
        {
            throw new Exception("Value not found");
        }

        return (JsonSerializer.Deserialize<T>(response.Value), response.Version);
    }

    public async Task<T> EventualGet<T>(string key)
    {
        var response = await _httpClient.GetFromJsonAsync<EventualGetResponse>($"api/storage/eventual?key={key}");

        if (response?.Value == null)
        {
            throw new Exception("Value not found");
        }

        return JsonSerializer.Deserialize<T>(response.Value);
    }

    public async Task<bool> CompareAndSwap(string key, string value, string expectedValue, int version)
    {
        var response = await _httpClient.PostAsJsonAsync("api/storage/compare-and-swap", new CompareAndSwapRequest
        {
            Key = key,
            NewValue = value,
            ExpectedValue = expectedValue,
            Version = version
        });

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("CAS failed");
        }

        var casResponse = await response.Content.ReadFromJsonAsync<CompareAndSwapResponse>();

        return casResponse.Success;
    }

    public async Task<bool> CompareAndSwap<T>(string key, T value, T expectedValue, int version)
    {
        var valueJson = JsonSerializer.Serialize(value);
        var expectedValueJson = JsonSerializer.Serialize(expectedValue);
        return await CompareAndSwap(key, valueJson, expectedValueJson, version);
    }
}
