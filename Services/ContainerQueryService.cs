using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Services
{
    public class ContainerQueryService
    {
        private readonly AppDbContext _context;

        public ContainerQueryService(AppDbContext context)
        {
            _context = context;
        }

        // INDEX QUERY (Filtros + Ordenação + Estatísticas)
        public async Task<ContainerIndexViewModel> GetIndexDataAsync(
            string? searchString,
            string? statusFilter,
            string? sortOrder)
        {
            var query = _context.Contentores.AsQueryable();

            // 1. Aplicar filtros
            query = ApplySearchFilter(query, searchString);
            query = ApplyStatusFilter(query, statusFilter);

            // 2. Aplicar ordenação
            query = ApplySorting(query, sortOrder);

            // 3. Executar query
            var containers = await query.AsNoTracking().ToListAsync();

            // 4. Estatísticas
            var stats = await GetDashboardStatsAsync();

            return new ContainerIndexViewModel
            {
                Containers = containers,
                Search = searchString,
                StatusFilter = statusFilter,
                SortOrder = sortOrder,
                Stats = stats
            };
        }

        // FILTROS
        private IQueryable<Container> ApplySearchFilter(IQueryable<Container> query, string? search)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Code.Contains(search) ||
                    c.Location.Contains(search));
            }

            return query;
        }

        private IQueryable<Container> ApplyStatusFilter(IQueryable<Container> query, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            return filter switch
            {
                // Atividade
                "Ativos" => query.Where(c => c.IsActive),
                "Inativos" => query.Where(c => !c.IsActive),

                // Nível
                "Critico" => query.Where(c => c.FillLevel >= 90),
                "Medio" => query.Where(c => c.FillLevel >= 50 && c.FillLevel < 90),
                "Baixo" => query.Where(c => c.FillLevel < 50),

                // Tipo
                "Plástico" or "Papel" or "Vidro" or "Indiferenciado"
                    => query.Where(c => c.Type == filter),

                // Estado físico (enum)
                "Bom" => query.Where(c => c.Status == Container.ContainerStatus.Good),
                "Cheio" => query.Where(c => c.Status == Container.ContainerStatus.Full),
                "Vazio" => query.Where(c => c.Status == Container.ContainerStatus.Empty),
                "Avariado" => query.Where(c => c.Status == Container.ContainerStatus.Broken),
                "Manutenção" => query.Where(c => c.Status == Container.ContainerStatus.Maintenance),

                _ => query
            };
        }

        // ORDENAÇÃO
        private IQueryable<Container> ApplySorting(IQueryable<Container> query, string? sortOrder)
        {
            return sortOrder switch
            {
                "code_desc" => query.OrderByDescending(c => c.Code),

                "Level" => query.OrderBy(c => c.FillLevel),
                "level_desc" => query.OrderByDescending(c => c.FillLevel),

                "Status" => query.OrderBy(c => c.Status),
                "status_desc" => query.OrderByDescending(c => c.Status),

                // INSTALAÇÃO
                "Date" => query.OrderBy(c => c.InstallationDate),
                "date_desc" => query.OrderByDescending(c => c.InstallationDate),

                _ => query.OrderBy(c => c.Code)
            };
        }


        // ESTATÍSTICAS DO DASHBOARD
        private async Task<ContainerDashboardStats> GetDashboardStatsAsync()
        {
            return new ContainerDashboardStats
            {
                Total = await _context.Contentores.CountAsync(),
                TotalCheios = await _context.Contentores.CountAsync(c => c.FillLevel >= 90),
                TotalAvariados = await _context.Contentores.CountAsync(c => c.Status == Container.ContainerStatus.Broken),
                TotalAtivos = await _context.Contentores.CountAsync(c => c.IsActive)
            };
        }
        public async Task<List<Container>> GetAllAsync()
        {
            return await _context.Contentores.AsNoTracking().ToListAsync();
        }

        public async Task<Container?> GetByIdAsync(int id)
        {
            return await _context.Contentores.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

    }
}
