using EcoCityWaste.Data;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador responsável por gerar o painel de estatísticas interno.
    /// O acesso é estritamente reservado aos Administradores e Funcionários da autarquia.
    /// </summary>
    [Authorize(Roles = "Admin,Funcionario")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Carrega a página principal do Dashboard.
        /// Faz os cálculos agregados todos na base de dados (contagens de ocorrências por estado, 
        /// contentores em estado crítico, médias de enchimento) para preparar os dados 
        /// que vão alimentar o ecrã.
        /// </summary>
        /// <returns>A vista do Dashboard preenchida com o <see cref="DashboardViewModel"/>.</returns>
        public async Task<IActionResult> Index()
        {
            var hoje = DateTime.Today;
            var semana = hoje.AddDays(-7);

            var contentores = await _context.Contentores.ToListAsync();

            var model = new DashboardViewModel
            {
                // Ocorrências
                TotalOcorrencias = await _context.Occurrences.CountAsync(),
                Pendente = await _context.Occurrences.CountAsync(o => o.Status == "Pendente"),
                EmAnalise = await _context.Occurrences.CountAsync(o => o.Status == "EmAnalise"),
                EmResolucao = await _context.Occurrences.CountAsync(o => o.Status == "EmResolucao"),
                Resolvido = await _context.Occurrences.CountAsync(o => o.Status == "Resolvido"),
                Rejeitado = await _context.Occurrences.CountAsync(o => o.Status == "Rejeitado"),

                // Contentores
                TotalContentores = contentores.Count,
                ContentoresCriticos = contentores.Count(c => c.FillLevel >= 90),

                // Indicadores temporais
                OcorrenciasHoje = await _context.Occurrences.CountAsync(o => o.ReportDate.Date == hoje),
                OcorrenciasSemana = await _context.Occurrences.CountAsync(o => o.ReportDate >= semana),

                // Indicadores Ambientais
                NivelMedioEnchimento = contentores.Any() ? contentores.Average(c => c.FillLevel) : 0,
                PercentagemCriticos = contentores.Any()
                    ? (double)contentores.Count(c => c.FillLevel >= 90) / contentores.Count * 100
                    : 0,
                NivelMedioPorTipo = contentores
                    .GroupBy(c => c.Type)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(c => c.FillLevel)
                    )
            };

            return View(model);
        }
    }
}