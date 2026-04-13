using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;

namespace EcoCityWaste.Services
{
    public interface IRouteService
    {
        Task<List<EcoCityWaste.Models.Route>> GetRoutesAsync(string? statusFilter, string? username, bool isEmployee);
        Task<EcoCityWaste.Models.Route?> GetRouteWithDetailsAsync(int id);
        Task<(bool Success, string Code)> CreateRouteAsync(RouteCreateViewModel model, string createdBy);
        Task<bool> EditRouteAsync(RouteEditViewModel model);
        Task<bool> CompleteRouteAsync(int id, string? username, bool isEmployee);
        Task AssignRouteAsync(RouteAssignViewModel model, EcoCityWaste.Models.Route route, User employee);
        Task<bool> DeleteRouteAsync(int id);
        Task<bool> ApplyOptimisationAsync(int routeId, List<int> orderedContainerIds);
        Task<List<Container>> GetActiveContainersAsync();
        Task<List<User>> GetEmployeesAsync();
        Task<(EcoCityWaste.Models.Route? Route, OptimisedRouteDto? Result)> GetOptimisedRouteAsync(int id);
    }
}