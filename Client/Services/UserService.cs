using System.Net.Http.Json;
using Shared.DTOs;

namespace Client.Services
{
    public class UserService
    {
        private readonly HttpClient _http;

        public UserService(HttpClient http)
        {
            _http = http;
        }

        public Task<List<UserDto>> GetAll() =>
            _http.GetFromJsonAsync<List<UserDto>>("api/users")!;

        public Task<UserDto?> Get(Guid id) =>
            _http.GetFromJsonAsync<UserDto>($"api/users/{id}");

        public async Task<UserDto?> Create(UserCreateRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/users", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }

        public async Task<UserDto?> Update(Guid id, UserUpdateRequest request)
        {
            var response = await _http.PutAsJsonAsync($"api/users/{id}", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }

        public Task<HttpResponseMessage> Delete(Guid id) =>
            _http.DeleteAsync($"api/users/{id}");
    }
}

