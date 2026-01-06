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
using Infrastructure.Dat;
using Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURAÇÃO DE SERVIÇOS ---

// Configurar CORS (Importante estar no topo)
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

// Configurar DbContext com verificação de segurança
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("ERRO: A ConnectionString 'DefaultConnection' não foi configurada no Azure.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("Infrastructure")
    ));

builder.Services.AddScoped<TokenService>();

// Configuração JWT com verificação de erro 500 comum
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("ERRO: A chave 'Jwt:Key' não foi encontrada nas configurações do Azure.");
}

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
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
        // Tenta rodar o Seed. Se falhar, o erro aparecerá nos Logs, mas a API continuará rodando.
        SeedData.Seed(db);
        Console.WriteLine("Banco de dados inicializado com sucesso.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRO AO INICIALIZAR BANCO: {ex.Message}");
    }
}

// --- 3. MIDDLEWARES ---

// Habilitar Swagger em todos os ambientes para facilitar o seu teste no Azure
app.UseSwagger();   
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Alpha Dental API v1");
    c.RoutePrefix = string.Empty; // Swagger abre na raiz da URL da API
});

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
