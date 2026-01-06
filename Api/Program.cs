using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Infrastructure.Dat;
using Api.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// LOGGING (Azure-friendly)
// ==============================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ==============================
// CORS (Blazor)
// ==============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ==============================
// Controllers + Swagger
// ==============================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ==============================
// Database
// ==============================
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString DefaultConnection não configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        b => b.MigrationsAssembly("Infrastructure"))
);

// ==============================
// Services
// ==============================
builder.Services.AddScoped<TokenService>();

// ==============================
// JWT
// ==============================
var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key não configurada.");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// ==============================
// Build
// ==============================
var app = builder.Build();

// ==============================
// Error Handling
// ==============================
app.UseDeveloperExceptionPage();
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Alpha Dental API v1");
    c.RoutePrefix = "swagger"; // padrão
});

// ==============================
// Database Migration + Seed
// ==============================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        logger.LogInformation("Iniciando migrations...");
        db.Database.Migrate();

        logger.LogInformation("Executando seed...");
        SeedData.Seed(db);

        logger.LogInformation("Banco inicializado com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Erro ao inicializar o banco de dados.");
        throw; // força o Azure mostrar erro nos logs
    }
}

// ==============================
// Middlewares
// ==============================
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// ==============================
// Endpoints
// ==============================
app.MapControllers();

app.Run();
