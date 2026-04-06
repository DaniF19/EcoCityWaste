using EcoCityWaste.Data;
using EcoCityWaste.Dtos;
using EcoCityWaste.Models;
using EcoCityWaste.Services;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Admin,Funcionario")]
    public class ContainersController : Controller
    {
        private readonly ContainerService _containerService;
        private readonly ContainerQueryService _queryService;
        private readonly ContainerHistoryService _historyService;

        public ContainersController(
            ContainerService containerService,
            ContainerQueryService queryService,
            ContainerHistoryService historyService)
        {
            _containerService = containerService;
            _queryService = queryService;
            _historyService = historyService;
        }

        public async Task<IActionResult> Index(string searchString, string statusFilter, string sortOrder)
        {
            var vm = await _queryService.GetIndexDataAsync(searchString, statusFilter, sortOrder);
            return View(vm);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(ContainerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _containerService.CreateAsync(model, User?.Identity?.Name);
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

        public async Task<IActionResult> List()
        {
            var containers = await _queryService.GetAllAsync();
            return View(containers);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var container = await _queryService.GetByIdAsync(id);
            if (container == null)
                return NotFound();

            var vm = new ContainerEditViewModel
            {
                Id = container.Id,
                Location = container.Location,
                Type = container.Type,
                Status = container.Status
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ContainerEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var updated = await _containerService.EditAsync(model, User?.Identity?.Name);

            if (updated == null)
                return NotFound();

            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Deactivate(int id)
        {
            var ok = await _containerService.DeactivateAsync(id, User?.Identity?.Name);

            if (!ok)
                return NotFound();

            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> History(int id)
        {
            var history = await _historyService.GetHistoryAsync(id);
            return View(history);
        }

        public async Task<IActionResult> ListStatus()
        {
            var containers = await _queryService.GetAllAsync();
            return View(containers);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, UpdateContainerStatusDto dto)
        {
            var updated = await _containerService.UpdateStatusAsync(id, dto.Status, User?.Identity?.Name);

            if (updated == null)
                return NotFound("Contentor não encontrado.");

            return RedirectToAction(nameof(ListStatus));
        }

        [HttpGet]
        public async Task<IActionResult> EditStatus(int id)
        {
            var container = await _queryService.GetByIdAsync(id);
            if (container == null)
                return NotFound();

            return View(new UpdateContainerStatusDto
            {
                Id = container.Id,
                Status = container.Status.ToString()
            });
        }
    }
}
