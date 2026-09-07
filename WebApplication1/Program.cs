using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ---------------- Banco de dados (Supabase / PostgreSQL) ----------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A connection string 'DefaultConnection' não foi encontrada. " +
        "Configure-a via appsettings, user-secrets (dev) ou variável de ambiente ConnectionStrings__DefaultConnection (produção).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ---------------- ASP.NET Core Identity ----------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ---------------- Porta dinâmica (Render/containers) ----------------
// Render injeta a variável de ambiente PORT e espera que a aplicação escute nela.
// Localmente (sem essa variável), o Kestrel continua usando o launchSettings.json normalmente.
var renderPort = Environment.GetEnvironmentVariable("PORT");

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(renderPort))
{
    app.Urls.Clear();
    app.Urls.Add($"http://0.0.0.0:{renderPort}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// O Render já termina o HTTPS na borda e encaminha via HTTP internamente.
// Forçar o redirect aqui dentro do container causaria loop de redirecionamento.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();