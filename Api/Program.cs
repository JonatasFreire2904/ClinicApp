// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.IdentityModel.Tokens;
// using Microsoft.EntityFrameworkCore;
// using System.Text;
// using Infrastructure.Dat;
// using Api.Services;

// var builder = WebApplication.CreateBuilder(args);

// // Configurar CORS para o Blazor
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("CorsPolicy", policy =>
//     {
//         policy.AllowAnyOrigin()
//               .AllowAnyMethod()
//               .AllowAnyHeader();
//     });
// });

// // Adiciona suporte a Controllers
// builder.Services.AddControllers();

// // Adiciona os servios do Swagger
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// // Configurar DbContext
// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         b => b.MigrationsAssembly("Infrastructure")
//     ));



// // Registrar TokenService
// builder.Services.AddScoped<TokenService>();

// // Recupera a chave secreta do JWT
// var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada");
// var key = Encoding.ASCII.GetBytes(jwtKey);

// // Configuracao de autenticacao JWT
// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
// })
// .AddJwtBearer(options =>
// {
//     options.RequireHttpsMetadata = false;
//     options.SaveToken = true;
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         ValidateIssuerSigningKey = true,
//         IssuerSigningKey = new SymmetricSecurityKey(key),
//         ValidateIssuer = false,
//         ValidateAudience = false,
//         ClockSkew = TimeSpan.Zero
//     };
// });

// var app = builder.Build();

// // Inicializar banco de dados e seed data
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
//     // Criar pasta Data se não existir
//     //var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Data");
//     //if (!Directory.Exists(dataPath))
//     //{
//     //    Directory.CreateDirectory(dataPath);
//     //}
    
//     //db.Database.Migrate();
//     //SeedData.Seed(db);
// }

// // Adiciona o middleware do Swagger em ambiente de desenvolvimento
// app.UseSwagger();   
// app.UseSwaggerUI();

// app.UseCors("CorsPolicy");

// // Middleware de autenticacao e autorizacao
// app.UseAuthentication();
// app.UseAuthorization();

// app.MapControllers();

// app.Run();

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Infrastructure.Dat; // Confirme se o namespace está correto conforme seu projeto
using Api.Services;       // Confirme se o namespace está correto conforme seu projeto

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURAÇÃO DE SERVIÇOS ---

// Configurar CORS (Deve estar no topo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CONFIGURAÇÃO DO BANCO DE DADOS ---
// O .NET busca automaticamente em "ConnectionStrings:DefaultConnection"
// No Azure, certifique-se que a Connection String se chama "DefaultConnection"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // Esse erro vai aparecer no Log Stream do Azure se a string não for encontrada
    throw new Exception("ERRO CRÍTICO: A ConnectionString 'DefaultConnection' não foi encontrada. Verifique as configurações do Azure App Service.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("Infrastructure")
    ));

builder.Services.AddScoped<TokenService>();

// --- CONFIGURAÇÃO DO JWT ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

// Verificações de segurança para evitar Erro 500 genérico
if (string.IsNullOrEmpty(jwtKey)) throw new Exception("ERRO JWT: A chave 'Jwt:Key' está vazia.");
if (string.IsNullOrEmpty(jwtSettings["Issuer"])) throw new Exception("ERRO JWT: O 'Jwt:Issuer' está vazio.");
if (string.IsNullOrEmpty(jwtSettings["Audience"])) throw new Exception("ERRO JWT: O 'Jwt:Audience' está vazio.");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        
        ValidateIssuer = true,
        // CORREÇÃO: Usar apenas a chave simples. O .NET resolve o "Jwt__" do Azure automaticamente.
        ValidIssuer = jwtSettings["Issuer"], 
        
        ValidateAudience = true,
        // CORREÇÃO: Usar apenas a chave simples.
        ValidAudience = jwtSettings["Audience"], 
        
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// --- 2. INICIALIZAÇÃO DO BANCO (SEED) ---

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        // Opcional: Aplica migrações pendentes automaticamente ao iniciar
        // db.Database.Migrate(); 
        
        SeedData.Seed(db);
        Console.WriteLine("Banco de dados inicializado com sucesso.");
    }
    catch (Exception ex)
    {
        // Esse log é vital para ver erros de conexão SQL no Azure Log Stream
        Console.WriteLine($"ERRO AO INICIALIZAR BANCO: {ex.Message}");
        // Não damos throw aqui para a API subir mesmo se o banco falhar temporariamente
    }
}

// --- 3. MIDDLEWARES ---

// Mantido fora do "IsDevelopment" para você conseguir testar no Azure
app.UseSwagger();   
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Alpha Dental API v1");
    c.RoutePrefix = string.Empty; 
});

// A ordem aqui é importante: CORS -> Auth -> Controllers
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
