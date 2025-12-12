using System.Net.Http.Json;
using Shared.DTOs;

public class ClinicService(HttpClient http)
{
    private readonly HttpClient _http = http;

    public Task<List<ClinicDto>> GetAll() => _http.GetFromJsonAsync<List<ClinicDto>>("clinics")!;
    public Task<ClinicDetailDto?> Get(Guid id) => _http.GetFromJsonAsync<ClinicDetailDto>($"clinics/{id}");

    public async Task<ClinicDto?> Create(ClinicDto dto)
    {
        var response = await _http.PostAsJsonAsync("clinics", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ClinicDto>();
    }

    public Task<HttpResponseMessage> Allocate(Guid clinicId, ClinicAllocateRequest request) =>
        _http.PostAsJsonAsync($"clinics/{clinicId}/allocate", request);

    public Task<HttpResponseMessage> AddStock(Guid clinicId, ClinicAddStockRequest request) =>
        _http.PostAsJsonAsync($"clinics/{clinicId}/stock/add", request);

    public Task<HttpResponseMessage> SetStockOpen(Guid clinicId, Guid materialId, bool isOpen) =>
        _http.PostAsJsonAsync($"clinics/{clinicId}/stock/{materialId}/open", new ClinicStockOpenRequest(isOpen));

    public Task<List<ClinicSummaryDto>> GetMyClinics() =>
        _http.GetFromJsonAsync<List<ClinicSummaryDto>>("clinics/my-clinics")!;

    public Task<HttpResponseMessage> ConsumeMaterial(Guid clinicId, ClinicConsumeRequest request) =>
        _http.PostAsJsonAsync($"clinics/{clinicId}/consume", request);

    public Task<HttpResponseMessage> ClearMovements(Guid clinicId) =>
        _http.DeleteAsync($"clinics/{clinicId}/movements");
}
