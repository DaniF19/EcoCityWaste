using Microsoft.AspNetCore.Mvc;
using EcoCityWaste.Models;
using EcoCityWaste.Data;

namespace EcoCityWaste.Controllers
{
    public class ContainerController : Controller
    {
        // GET: /Container/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Container/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(ContainerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var container = new Container
                {
                    Location = model.Location,
                    Type = model.Type,
                    InitialState = model.InitialState
                };

                ContainerRepository.Add(container);

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

        public IActionResult List()
        {
            var containers = ContainerRepository.GetAll();
            return View(containers);
        }

        public IActionResult Edit(int id)
        {
            var container = ContainerRepository.GetById(id);

            if (container == null)
                return NotFound();

            return View(container);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Container model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                ContainerRepository.Update(model);
                return RedirectToAction("List");
            }
            catch
            {
                ViewBag.Error = "Erro ao atualizar o contentor.";
                return View(model);
            }
        }

        
    }
}