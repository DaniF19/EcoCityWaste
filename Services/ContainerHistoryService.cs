using EcoCityWaste.Data;
using EcoCityWaste.Models;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Serviço especializado na gestão e registo do histórico de estados dos contentores.
    /// Garante que todas as alterações relevantes fiquem gravadas.
    /// </summary>
    public class ContainerHistoryService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Injeta o contexto da base de dados para permitir a persistência dos registos históricos.
        /// </summary>
        public ContainerHistoryService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria e guarda um novo registo na tabela de histórico sempre que um contentor é alterado.
        /// Captura uma "fotografia" do estado, nível de enchimento e atividade no momento exato da modificação.
        /// </summary>
        /// <param name="container">O objeto do contentor que sofreu a alteração.</param>
        /// <param name="changedBy">O nome do utilizador que efetuou a alteração (ou nulo, se for uma atualização automática do sistema).</param>
        /// <returns>Uma Task assíncrona que representa a operação de salvaguarda na base de dados.</returns>
        public async Task AddHistory(Container container, string? changedBy)
        {
            var history = new ContainerStatusHistory
            {
                ContainerId = container.Id,
                Status = container.Status,
                FillLevel = container.FillLevel,
                IsActive = container.IsActive,
                ChangedAt = DateTime.Now,
                // Se não for fornecido um utilizador, assume-se que foi uma ação automática do "Sistema"
                ChangedBy = changedBy ?? "Sistema"
            };

            _context.ContainerStatusHistories.Add(history);
            await _context.SaveChangesAsync();
        }
    }
}