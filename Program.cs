using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using EcoCityWaste.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Regista o servi�o de simula��o de sensores
builder.Services.AddHostedService<EcoCityWaste.Services.SensorSimulationService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Auth Google
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

// Servico envio email para recuperar password
builder.Services.AddScoped<IEmailService, EmailService>();
// DbContext 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
    sqlServerOptionsAction: sqlOptions =>
    {
        // Ativa tentativas automáticas em caso de falha de ligação
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, // Número máximo de tentativas
            maxRetryDelay: TimeSpan.FromSeconds(10), // Espera máxima entre tentativas
            errorNumbersToAdd: null);
    }));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// servico de notificacoes
builder.Services.AddScoped<NotificationService>();

// servico de optimizacao rotas
builder.Services.AddScoped<RouteOptimisationService>();

// servico para obter coordenadas da location introduzida
builder.Services.AddHttpClient<GeocodingService>();

// servico para os controllers utilizarem o container history
builder.Services.AddScoped<ContainerHistoryService>();

builder.Services.AddSignalR();
builder.Services.AddScoped<IRouteService, RouteService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.MapHub<SimulationHub>("/simulationHub");

// app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// .WithStaticAssets();


// Cria Utilizadores automáticos, caso não existam
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    bool hasChanges = false;

    // ADMIN
    if (!context.Users.Any(u => u.Email == "admin@teste.com"))
    {
        context.Users.Add(new User
        {
            Username = "Administrador",
            Email = "admin@teste.com",
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        hasChanges = true;
    }

    // FUNCIONÁRIO
    if (!context.Users.Any(u => u.Email == "funcionario@teste.com"))
    {
        context.Users.Add(new User
        {
            Username = "Trabalhador EcoCity",
            Email = "funcionario@teste.com",
            Role = "Funcionario",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        hasChanges = true;
    }

    // CIDADÃO
    if (!context.Users.Any(u => u.Email == "cidadao@teste.com"))
    {
        context.Users.Add(new User
        {
            Username = "Cidadão Teste",
            Email = "cidadao@teste.com",
            Role = "Cidadao",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        hasChanges = true;
    }

    if (!context.Occurrences.Any())
    {
        // Buscar um utilizador Cidadão para associar à ocorrência
        var cidadao = context.Users.FirstOrDefault(u => u.Role == "Cidadao");

        if (cidadao != null)
        {
            context.Occurrences.Add(new Occurrence
            {
                ContainerCode = "CNT-001", // um dos contentores que já criaste
                OccurrenceType = "Lixo",
                Description = "Contentor cheio e a transbordar",
                ReportDate = DateTime.Now.AddHours(-3),
                Status = OccurrenceStatus.Pendente.ToString(),
                UserId = cidadao.Id,
                AssignedEmployeeId = null,
                AssignedAt = null,
                ImagePath = null,
            });

            context.SaveChanges();
        }
    }
    // Guarda, so se houver novos utilizadores
    if (hasChanges)
    {
        context.SaveChanges();
    }
}


// Cria contentores autom�ticos para testes, caso a tabela esteja vazia
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Verifica se não existe nenhum contentor na base de dados
    if (!context.Contentores.Any())
    {
        context.Contentores.AddRange(
            new EcoCityWaste.Models.Container
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
            new EcoCityWaste.Models.Container
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
            new EcoCityWaste.Models.Container
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
            new EcoCityWaste.Models.Container
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
            new EcoCityWaste.Models.Container
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


app.Run();

public partial class Program { }
