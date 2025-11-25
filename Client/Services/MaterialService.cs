using System.Net.Http.Json;
using Shared.DTOs;

public class MaterialService(HttpClient http)
{
    private readonly HttpClient _http = http;

    public Task<List<MaterialDto>> GetAll() => _http.GetFromJsonAsync<List<MaterialDto>>("materials")!;
    public Task<List<MaterialGeneralStockDto>> GetGeneralSummary() => _http.GetFromJsonAsync<List<MaterialGeneralStockDto>>("materials/summary")!;
    public async Task<MaterialDto?> Create(MaterialCreateRequest request)
    {
        var response = await _http.PostAsJsonAsync("materials", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MaterialDto>();
    }
    public Task Delete(Guid id) => _http.DeleteAsync($"materials/{id}");
    public Task<HttpResponseMessage> AddStock(Guid id, int quantity, decimal cost, decimal total) =>
        _http.PostAsJsonAsync($"materials/{id}/add-stock", new { quantity, cost, total });
    public Task<HttpResponseMessage> AssignToClinic(Guid materialId, Guid clinicId, int quantity) =>
        _http.PostAsJsonAsync($"materials/{materialId}/assign-to-clinic", new { clinicId, quantity });
}
