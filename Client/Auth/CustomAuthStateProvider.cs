using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Client.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        private bool _isInitialized = false;

        public CustomAuthStateProvider(IJSRuntime js)
        {
            _js = js;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // 🔹 PROTEÇÃO CONTRA LOOP: Só tenta ler do localStorage após o primeiro render
            if (!_isInitialized)
            {
                _isInitialized = true;

                try
                {
                    var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");

                    if (string.IsNullOrWhiteSpace(token))
                        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

                    var claims = ParseClaimsFromJwt(token);
                    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

                    return new AuthenticationState(user);
                }
                catch
                {
                    // ⚠️ Evita loops infinitos caso algo dê errado no carregamento do estado
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            // Retorna usuário anônimo durante a inicialização
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // 🔹 Notifica autenticação (login)
        public void NotifyUserAuthentication(string token)
        {
            var claims = ParseClaimsFromJwt(token);
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }

        // 🔹 Notifica logout
        public async Task LogoutAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }

        // 🔹 Decodifica o JWT sem depender de libs externas
        private IEnumerable<Claim> ParseClaimsFromJwt(string token)
        {
            var payload = token.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();
            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                            claims.Add(new Claim(kvp.Key, item.GetString() ?? ""));
                    }
                    else
                    {
                        claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
                    }
                }
            }

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}