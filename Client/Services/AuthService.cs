using Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Shared.DTOs.Auth;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(HttpClient http, IJSRuntime js, AuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _js = js;
            _authStateProvider = authStateProvider;
        }

        public async Task<string?> Login(LoginRequest request)
        {
            var result = await _http.PostAsJsonAsync("api/auth/login", request);

            if (!result.IsSuccessStatusCode)
                return null;

            var response = await result.Content.ReadFromJsonAsync<LoginResponse>();
            if (response is null) return null;

            // 🔹 Salva o token no LocalStorage
            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", response.Token);

            // 🔹 Atualiza o estado de autenticação no Blazor
            (_authStateProvider as CustomAuthStateProvider)?
                .NotifyUserAuthentication(response.Token);

            // 🔹 Adiciona o token nas requisições subsequentes
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", response.Token);

            return response.Token;
        }

        public async Task Logout()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            if (_authStateProvider is CustomAuthStateProvider customProvider)
                await customProvider.LogoutAsync();

            _http.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<string?> GetToken()
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", "authToken");
        }

        public async Task InitializeAsync()
        {
            var savedToken = await GetToken();
            if (string.IsNullOrWhiteSpace(savedToken))
                return;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", savedToken);

            if (_authStateProvider is CustomAuthStateProvider customProvider)
            {
                customProvider.NotifyUserAuthentication(savedToken);
            }
        }
    }
}
