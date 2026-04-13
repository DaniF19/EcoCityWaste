using EcoCityWaste.Data;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Serviço de segundo plano que simula o comportamento de sensores IoT.
    /// Este serviço corre continuamente enquanto a aplicação estiver ligada, atualizando 
    /// automaticamente o nível de enchimento dos contentores para simular o uso real pelos cidadãos.
    /// </summary>
    public class SensorSimulationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Random _random = new Random();

        /// <summary>
        /// Construtor do serviço. Recebe o IServiceProvider para poder criar âmbitos
        /// de base de dados, já que este serviço tem um tempo de vida mais longo que os controladores.
        /// </summary>
        public SensorSimulationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Método principal que executa o ciclo de simulação.
        /// Implementa uma lógica de atualização periódica que aumenta o lixo nos contentores.
        /// </summary>
        /// <param name="stoppingToken">Token para interromper o serviço de forma segura quando a app desliga.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Aguarda 10 segundos após o arranque da aplicação para garantir que tudo está estabilizado
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Criamos um scope manual para obter o AppDbContext, garantindo que as ligações 
                // à base de dados são abertas e fechadas corretamente em cada ciclo.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                    // Simulação apenas em contentores operacionais
                    var containers = await context.Contentores.Where(c => c.IsActive).ToListAsync();

                    foreach (var container in containers)
                    {
                        // Simula a deposição de resíduos: aumento aleatório entre 0% e 10%
                        int increase = _random.Next(0, 11);
                        container.FillLevel = Math.Min(100, container.FillLevel + increase);
                        container.LastUpdated = DateTime.Now;

                        // Lógica de Alerta: Se o contentor ficar crítico durante a simulação, 
                        // o serviço de notificações é acionado imediatamente.
                        if (container.FillLevel >= 90)
                        {
                            await notificationService.CreateCriticalLevelNotification(container);
                        }
                    }

                    // Grava as alterações simuladas na base de dados
                    await context.SaveChangesAsync(stoppingToken);
                }

                // Intervalo entre simulações: o camião virtual "espera" 30 segundos antes de nova leitura
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}