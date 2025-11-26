using System.Net.Http.Json;
using Shared.DTOs;

public class DashboardService(HttpClient http)
{
    private readonly HttpClient _http = http;

    public Task<DashboardFinancialSummaryDto> GetSummary() =>
        _http.GetFromJsonAsync<DashboardFinancialSummaryDto>("dashboard/financial-summary")!;
    public Task<object> GetClinicDetails(Guid id) =>
        _http.GetFromJsonAsync<object>($"dashboard/clinic/{id}")!;
}
