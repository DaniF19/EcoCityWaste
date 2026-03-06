using EcoCityWaste.Data;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Services
{
    public class SensorSimulationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Random _random = new Random();

        public SensorSimulationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Vai buscar todos os contentores ativos
                    var containers = await context.Contentores.Where(c => c.IsActive).ToListAsync();

                    foreach (var container in containers)
                    {
                        // Simula um aumento aleatório no nível de enchimento (entre 0 e 10%)
                        int increase = _random.Next(0, 11);
                        container.FillLevel = Math.Min(100, container.FillLevel + increase);

                        container.LastUpdated = DateTime.Now;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }

                // Aguarda 30 segundos antes da próxima atualização
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}