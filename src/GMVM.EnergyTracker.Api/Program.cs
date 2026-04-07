using System.Text;
using GMVM.EnergyTracker.Domain.Services;
using GMVM.EnergyTracker.Infrastructure;
using GMVM.EnergyTracker.Infrastructure.Seed;
using GMVM.EnergyTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// =============================================================
// Configuration
// =============================================================
var connectionString = builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=energytracker.db";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GMVM.EnergyTracker";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GMVM.EnergyTracker.Api";
var jwtKey = builder.Configuration["Jwt:Key"]
            ?? "DEMO_ONLY_REPLACE_ME_IN_PRODUCTION_AT_LEAST_32_CHARS";

// =============================================================
// Services
// =============================================================
builder.Services.AddDbContext<EnergyTrackerDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<ICalculadoraConsumo, CalculadoraConsumo>();
builder.Services.AddScoped<IMedidorService, MedidorService>();

// JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// =============================================================
// Migrate + Seed
// =============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EnergyTrackerDbContext>();
    db.Database.EnsureCreated();
    SeedData.Seed(db);
}

// =============================================================
// Pipeline
// =============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program accessible to integration tests via WebApplicationFactory<Program>.
public partial class Program { }
