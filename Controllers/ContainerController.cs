using Microsoft.AspNetCore.Mvc;
using EcoCityWaste.Models;
using EcoCityWaste.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EcoCityWaste.Controllers
{
    public class ContainerController : Controller
    {
        private readonly AppDbContext _context;

        public ContainerController(AppDbContext context)
        {
            _context = context;
        }
        // GET: /Container/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Container/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(ContainerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var container = new Container
                {
                    Code = GenerateContainerCode(),
                    Location = model.Location,
                    Type = model.Type,
                    Status = model.Status,
                    Latitude = 0,
                    Longitude = 0,
                    FillLevel = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                };

                //ContainerRepository.Add(container);
                _context.Contentores.Add(container);
                await _context.SaveChangesAsync();

                ViewBag.Success = "Contentor registado com sucesso!";
                ModelState.Clear();

                return View();
            }
            catch
            {
                ViewBag.Error = "Erro ao registar o contentor.";
                return View(model);
            }
        }

        public async Task<IActionResult> List()
        {
            var containers = await _context.Contentores
                                           .Where(c => c.IsActive)
                                           .ToListAsync();

            return View(containers);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var container = await _context.Contentores.FindAsync(id);

            if (container == null)
                return NotFound();

            return View(container);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                /*container.Latitude = model.Latitude;
                container.Longitude = model.Longitude;
                container.FillLevel = model.FillLevel;
                container.IsActive = model.IsActive;*/
                container.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return RedirectToAction("List");
            }
            catch
            {
                ViewBag.Error = "Erro ao atualizar o contentor.";
                return View(model);
            }
        }

        // Desativar contentor
        public async Task<IActionResult> Deactivate(int id)
        {
            var container = await _context.Contentores.FindAsync(id);

            if (container == null)
                return NotFound();

            container.IsActive = false;
            container.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("List");
        }

        // Container code generator
        private string GenerateContainerCode()
        {
            var count = _context.Contentores.Count() + 1;
            return $"CNT-{count:D3}";
        }

        
    }
}