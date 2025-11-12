/*using Client;
using Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress)
    });
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();*/
using Client;
using Client.Auth;
using Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Pega a URL da API do arquivo de configura��o
string apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl")
                    ?? throw new InvalidOperationException("ApiBaseUrl not found in configuration.");

// Configura o HttpClient para usar a URL da API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// --- Configura��o de Autentica��o ---

// Seu servi�o de l�gica de autentica��o (login, logout, etc.)
builder.Services.AddScoped<AuthService>();

// Adiciona os servi�os principais de autoriza��o (ex: para usar [Authorize])
builder.Services.AddAuthorizationCore();

// Registra seu provedor de estado de autentica��o personalizado
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// **LINHA FALTANTE ADICIONADA AQUI**
// Habilita o componente CascadingAuthenticationState a receber o estado de autentica��o
// do provedor e distribu�-lo para o resto da aplica��o.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ClinicService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, Client.Auth.CustomAuthStateProvider>();



// --- Fim da Configura��o de Autentica��o ---

await builder.Build().RunAsync();


