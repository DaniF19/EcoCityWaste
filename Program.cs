using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// 1. REGISTO DE SERVIÇOS
// -----------------------------

builder.Services.AddControllersWithViews();

// Autenticação Google
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});

// Email
builder.Services.AddScoped<IEmailService, EmailService>();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
    sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Serviços internos
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<RouteOptimisationService>();
builder.Services.AddHttpClient<GeocodingService>();

// -----------------------------
// 2. SEED DA BASE DE DADOS (ANTES DO BUILD)
// -----------------------------

using (var scope = AppContext.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Criar Admin se não existir
    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        context.Users.Add(new User
        {
            Username = "Admin",
            Email = "admin@ecocity.com",
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
    }

    // Criar Funcionário se não existir
    if (!context.Users.Any(u => u.Role == "Funcionario"))
    {
        context.Users.Add(new User
        {
            Username = "Funcionario",
            Email = "funcionario@ecocity.com",
            Role = "Funcionario",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
    }

    context.SaveChanges();

    // Criar contentores se a tabela estiver vazia
    if (!context.Contentores.Any())
    {
        context.Contentores.AddRange(
            new Container
            {
                Code = "CNT-001",
                Location = "Praça do Bocage, Setúbal",
                Latitude = 38.5244,
                Longitude = -8.8882,
                Type = "Plástico",
                Status = Container.ContainerStatus.Good,
                FillLevel = 85,
                InstallationDate = DateTime.Now.AddDays(-120),
                LastUpdated = DateTime.Now.AddMinutes(-30),
                IsActive = true
            },
            new Container
            {
                Code = "CNT-002",
                Location = "Avenida Luísa Todi, Setúbal",
                Latitude = 38.5220,
                Longitude = -8.8920,
                Type = "Vidro",
                Status = Container.ContainerStatus.Good,
                FillLevel = 25,
                InstallationDate = DateTime.Now.AddDays(-200),
                LastUpdated = DateTime.Now.AddHours(-2),
                IsActive = true
            },
            new Container
            {
                Code = "CNT-003",
                Location = "Parque do Bonfim, Setúbal",
                Latitude = 38.5275,
                Longitude = -8.8890,
                Type = "Papel",
                Status = Container.ContainerStatus.Broken,
                FillLevel = 45,
                InstallationDate = DateTime.Now.AddDays(-300),
                LastUpdated = DateTime.Now.AddDays(-1),
                IsActive = false
            },
            new Container
            {
                Code = "CNT-004",
                Location = "Mercado do Livramento, Setúbal",
                Latitude = 38.5235,
                Longitude = -8.8950,
                Type = "Indiferenciado",
                Status = Container.ContainerStatus.Maintenance,
                FillLevel = 98,
                InstallationDate = DateTime.Now.AddDays(-50),
                LastUpdated = DateTime.Now.AddMinutes(-10),
                IsActive = false
            },
            new Container
            {
                Code = "CNT-005",
                Location = "Jardim do Quebedo, Setúbal",
                Latitude = 38.5260,
                Longitude = -8.8840,
                Type = "Plástico",
                Status = Container.ContainerStatus.Good,
                FillLevel = 60,
                InstallationDate = DateTime.Now.AddDays(-150),
                LastUpdated = DateTime.Now.AddHours(-5),
                IsActive = true
            }
        );

        context.SaveChanges();
    }
}

// -----------------------------
// 3. AGORA SIM: REGISTAR O SENSOR SIMULATION SERVICE
// -----------------------------
builder.Services.AddHostedService<SensorSimulationService>();

// -----------------------------
// 4. BUILD + PIPELINE
// -----------------------------

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
