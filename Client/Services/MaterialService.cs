using System.Net.Http.Json;
using Shared.DTOs;

public class MaterialService
{
    private readonly HttpClient _http;
    public MaterialService(HttpClient http) => _http = http;

    public Task<List<MaterialDto>> GetAll() => _http.GetFromJsonAsync<List<MaterialDto>>("materials")!;
    public async Task<MaterialDto?> Create(MaterialCreateRequest request)
    {
        var response = await _http.PostAsJsonAsync("materials", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MaterialDto>();
    }
    public Task Delete(Guid id) => _http.DeleteAsync($"materials/{id}");
    public Task AddStock(Guid id, int quantity) =>
        _http.PostAsJsonAsync($"materials/{id}/add-stock", new { quantity });
}
