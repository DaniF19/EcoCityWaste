using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Interface que define a assinatura do método para a gestão de rotas de recolha.
    /// Centraliza toda a lógica de negócio relacionada com o planeamento, otimização e monitorização de trajetos.
    /// </summary>
    public interface IRouteService
    {
        /// <summary>
        /// Obtém uma lista de rotas filtrada. Se for um funcionário, o serviço garante que este apenas vê os seus próprios trajetos.
        /// </summary>
        Task<List<EcoCityWaste.Models.Route>> GetRoutesAsync(string? statusFilter, string? username, bool isEmployee);

        /// <summary>
        /// Recupera os dados detalhados de uma rota, incluindo os contentores associados e o funcionário atribuído.
        /// </summary>
        Task<EcoCityWaste.Models.Route?> GetRouteWithDetailsAsync(int id);

        /// <summary>
        /// Cria uma nova rota no sistema, gerando automaticamente um código único e associando os contentores selecionados.
        /// </summary>
        Task<(bool Success, string Code)> CreateRouteAsync(RouteCreateViewModel model, string createdBy);

        /// <summary>
        /// Permite a edição das informações básicas e da lista de contentores de uma rota existente.
        /// </summary>
        Task<bool> EditRouteAsync(RouteEditViewModel model);

        /// <summary>
        /// Finaliza uma rota, registando o momento da conclusão e libertando o funcionário para novas tarefas.
        /// </summary>
        Task<bool> CompleteRouteAsync(int id, string? username, bool isEmployee);

        /// <summary>
        /// Atribui formalmente uma rota a um funcionário e despoleta os mecanismos de notificação.
        /// </summary>
        Task AssignRouteAsync(RouteAssignViewModel model, EcoCityWaste.Models.Route route, User employee);

        /// <summary>
        /// Remove uma rota e as suas associações da base de dados.
        /// </summary>
        Task<bool> DeleteRouteAsync(int id);

        /// <summary>
        /// Aplica a nova ordem de recolha de contentores após um processo de otimização de trajeto.
        /// </summary>
        Task<bool> ApplyOptimisationAsync(int routeId, List<int> orderedContainerIds);

        /// <summary>
        /// Devolve apenas os contentores que estão em estado "Ativo" e aptos para entrar numa rota.
        /// </summary>
        Task<List<Container>> GetActiveContainersAsync();

        /// <summary>
        /// Devolve a lista de utilizadores que possuem permissões de "Funcionario" para efeitos de atribuição.
        /// </summary>
        Task<List<User>> GetEmployeesAsync();

        /// <summary>
        /// Calcula e devolve uma sugestão de rota otimizada com base na localização geográfica dos contentores.
        /// </summary>
        Task<(EcoCityWaste.Models.Route? Route, OptimisedRouteDto? Result)> GetOptimisedRouteAsync(int id);
    }
}