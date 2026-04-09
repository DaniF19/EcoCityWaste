using EcoCityWaste.Data;
using EcoCityWaste.Dtos;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static EcoCityWaste.Models.Container;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador principal para a gestão dos contentores de lixo.
    /// Apenas Administradores e Funcionários têm permissão para aceder a estas páginas.
    /// </summary>
    [Authorize(Roles = "Admin,Funcionario")]
    public class ContainersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GeocodingService _geo;
        private readonly ContainerHistoryService _historyService;

        public ContainersController(AppDbContext context, GeocodingService geo, ContainerHistoryService historyService)
        {
            _context = context;
            _geo = geo;
            _historyService = historyService;
        }

        /// <summary>
        /// Carrega a página principal de gestão de contentores.
        /// Calcula as estatísticas para os cartões de topo e aplica filtros de pesquisa e ordenação na tabela.
        /// </summary>
        /// <param name="searchString">Texto pesquisado (código ou localização).</param>
        /// <param name="statusFilter">Filtro aplicado (ex: "Cheio", "Ativos", "Papel").</param>
        /// <param name="sortOrder">Ordem das colunas (crescente/decrescente).</param>
        /// <returns>A vista com a lista de contentores filtrada.</returns>
        public async Task<IActionResult> Index(string searchString, string statusFilter, string sortOrder)
        {
            try
            {
                // Estatísticas para o dashboard
                ViewBag.TotalContainers = await _context.Contentores.CountAsync();
                ViewBag.TotalCheios = await _context.Contentores.CountAsync(c => c.FillLevel >= 90);
                ViewBag.TotalAvariados = await _context.Contentores.CountAsync(c => c.Status == ContainerStatus.Broken);
                ViewBag.TotalAtivos = await _context.Contentores.CountAsync(c => c.IsActive);

                // Manter os valores para a View
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentStatus = statusFilter;

                // Parâmetros de ordenação
                ViewBag.CodeSortParam = String.IsNullOrEmpty(sortOrder) ? "code_desc" : "";
                ViewBag.LevelSortParam = sortOrder == "Level" ? "level_desc" : "Level";
                ViewBag.StatusSortParam = sortOrder == "Status" ? "status_desc" : "Status";

                var containers = _context.Contentores.AsQueryable();

                // Filtro de Pesquisa por Texto
                if (!String.IsNullOrEmpty(searchString))
                {
                    containers = containers.Where(c => c.Code.Contains(searchString) || c.Location.Contains(searchString));
                }
                Container.ContainerStatus? statusEnum = statusFilter switch
                {
                    "Bom" => Container.ContainerStatus.Good,
                    "Cheio" => Container.ContainerStatus.Full,
                    "Vazio" => Container.ContainerStatus.Empty,
                    "Avariado" => Container.ContainerStatus.Broken,
                    "Manutenção" => Container.ContainerStatus.Maintenance,
                    _ => null
                };

                // Filtros
                if (!String.IsNullOrEmpty(statusFilter))
                {
                    containers = statusFilter switch
                    {
                        // Filtros de Atividade
                        "Ativos" => containers.Where(c => c.IsActive),
                        "Inativos" => containers.Where(c => !c.IsActive),

                        // Filtros de Nível
                        "Critico" or "Cheio" => containers.Where(c => c.FillLevel >= 90),
                        "Medio" => containers.Where(c => c.FillLevel >= 50 && c.FillLevel < 90),
                        "Baixo" => containers.Where(c => c.FillLevel < 50),

                        // Filtros de Tipo de Resíduo
                        "Plástico" or "Papel" or "Vidro" or "Indiferenciado"
                            => containers.Where(c => c.Type == statusFilter),

                        // Filtros Físicos (ENUM)
                        _ => statusEnum.HasValue
                                ? containers.Where(c => c.Status == statusEnum.Value)
                                : containers
                    };
                }

                // Ordenação 
                switch (sortOrder)
                {
                    case "code_desc": containers = containers.OrderByDescending(c => c.Code); break;
                    case "Level": containers = containers.OrderBy(c => c.FillLevel); break;
                    case "level_desc": containers = containers.OrderByDescending(c => c.FillLevel); break;
                    case "Status": containers = containers.OrderBy(c => c.Status); break;
                    case "status_desc": containers = containers.OrderByDescending(c => c.Status); break;
                    default: containers = containers.OrderBy(c => c.Code); break;
                }

                return View(await containers.ToListAsync());
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocorreu um problema ao tentar aceder à base de dados.";
                return View(new List<EcoCityWaste.Models.Container>());
            }
        }

        /// <summary>
        /// Devolve a vista com o formulário para registar um novo contentor.
        /// </summary>
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Processa o formulário de registo. Usa o GeocodingService para transformar a morada 
        /// introduzida em coordenadas GPS reais antes de guardar na base de dados.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Register(ContainerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Converter Status (string → enum)
                if (!Enum.TryParse<ContainerStatus>(model.Status, out var statusEnum))
                {
                    ModelState.AddModelError("Status", "Estado inválido.");
                    return View(model);
                }

                // Definir se o contentor é criado ativo ou inativo
                bool isContainerActive = true;
                if (statusEnum == ContainerStatus.Broken || statusEnum == ContainerStatus.Maintenance)
                {
                    isContainerActive = false;
                }

                var coords = await _geo.GetCoordinates(model.Location);

                var container = new Container
                {
                    Code = GenerateContainerCode(),
                    Location = model.Location,
                    Type = model.Type,
                    Status = statusEnum,
                    Latitude = coords.lat,
                    Longitude = coords.lon,
                    FillLevel = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = isContainerActive
                };

                _context.Contentores.Add(container);
                await _context.SaveChangesAsync();

                // Regista a criação no histórico
                await _historyService.AddHistory(container, User?.Identity?.Name);

                ViewBag.Success = "Contentor registado com sucesso!";
                ModelState.Clear();

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        /// <summary>
        /// Lista simples de todos os contentores (sem os filtros complexos do Index).
        /// </summary>
        public async Task<IActionResult> List()
        {
            var containers = await _context.Contentores.ToListAsync();
            return View(containers);
        }

        /// <summary>
        /// Mostra a página de edição para um contentor específico.
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var container = await _context.Contentores.FindAsync(id);

            if (container == null)
                return NotFound();

            return View(container);
        }

        /// <summary>
        /// Atualiza os dados principais do contentor. Se o estado mudar para avariado ou manutenção,
        /// desativa o contentor automaticamente para que não entre nas rotas de recolha.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(ContainerEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var container = await _context.Contentores.FindAsync(model.Id);

                if (container == null)
                    return NotFound();

                container.Location = model.Location;
                container.Type = model.Type;
                container.Status = model.Status;

                // Desativar o contentor se estiver avariado ou em manutenção
                if (model.Status == ContainerStatus.Broken || model.Status == ContainerStatus.Maintenance)
                {
                    container.IsActive = false;
                }
                else
                {
                    container.IsActive = true;
                }
                container.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                // Grava a alteração no histórico
                await _historyService.AddHistory(container, User?.Identity?.Name);
                return RedirectToAction("List");
            }
            catch
            {
                ViewBag.Error = "Erro ao atualizar o contentor.";
                return View(model);
            }
        }

        /// <summary>
        /// O contentor deixa de estar ativo no sistema, 
        /// mas os seus dados não são apagados para não quebrar os relatórios antigos.
        /// </summary>
        public async Task<IActionResult> Deactivate(int id)
        {
            var container = await _context.Contentores.FindAsync(id);

            if (container == null)
                return NotFound();

            container.IsActive = false;
            container.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            await _historyService.AddHistory(container, User?.Identity?.Name);
            return RedirectToAction("List");
        }

        /// <summary>
        /// Devolve a vista com a cronologia de todas as mudanças de estado ou nível deste contentor.
        /// </summary>
        public async Task<IActionResult> History(int id)
        {
            var history = await _context.ContainerStatusHistories
                .Where(h => h.ContainerId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            return View(history);
        }

        // Método interno de backup caso o serviço falhe, mantém a mesma lógica do HistoryService.
        private async Task AddHistory(Container container)
        {
            if (_context.ContainerStatusHistories == null)
                return;

            var history = new ContainerStatusHistory
            {
                ContainerId = container.Id,
                Status = container.Status,
                FillLevel = container.FillLevel,
                IsActive = container.IsActive,
                ChangedAt = DateTime.Now,
                ChangedBy = User?.Identity?.Name ?? "Sistema"
            };

            _context.ContainerStatusHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Vista simplificada em lista, focada apenas nos estados físicos atuais.
        /// </summary>
        public async Task<IActionResult> ListStatus()
        {
            var containers = await _context.Contentores
                .ToListAsync();

            return View(containers);
        }

        /// <summary>
        /// Ecrã rápido (normalmente para os funcionários no terreno) para alterar apenas o estado físico do contentor.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditStatus(int id)
        {
            var container = await _context.Contentores.FindAsync(id);
            if (container == null)
                return NotFound();

            var model = new UpdateContainerStatusDto
            {
                Id = container.Id,
                Status = container.Status.ToString()
            };

            return View(model);
        }

        /// <summary>
        /// Processa a alteração rápida de estado. Volta a aplicar a regra de desativar automaticamente se estiver avariado.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, UpdateContainerStatusDto dto)
        {
            var container = await _context.Contentores.FindAsync(dto.Id);

            if (container == null)
                return NotFound("Contentor não encontrado.");

            if (!Enum.TryParse<Container.ContainerStatus>(dto.Status, true, out var newStatus))
                return BadRequest("Estado inválido.");

            container.Status = newStatus;

            // Desativar o contentor se estiver avariado ou em manutenção
            if (newStatus == Container.ContainerStatus.Broken || newStatus == Container.ContainerStatus.Maintenance)
            {
                container.IsActive = false;
            }
            else
            {
                container.IsActive = true;
            }

            container.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _historyService.AddHistory(container, User?.Identity?.Name);

            return RedirectToAction(nameof(ListStatus));
        }

        /// <summary>
        /// Método auxiliar para criar códigos incrementais bonitos para os contentores (Ex: CNT-001, CNT-002).
        /// </summary>
        private string GenerateContainerCode()
        {
            var count = _context.Contentores.Count() + 1;
            return $"CNT-{count:D3}";
        }
    }
}