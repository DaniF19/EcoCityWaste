using BCrypt.Net;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão administrativa de utilizadores.
    /// Permite criar, editar, visualizar e remover contas de utilizador do sistema.
    /// </summary>
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista todos os utilizadores registados na plataforma.
        /// </summary>
        /// <returns>Uma vista com a listagem completa de utilizadores.</returns>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        /// <summary>
        /// Apresenta os detalhes completos de um utilizador específico através do seu ID.
        /// </summary>
        /// <param name="id">Identificador único do utilizador.</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
        /// <summary>
        /// Apresenta os detalhes completos de um funcionário específico através do seu ID.
        /// </summary>
        /// <param name="id">Identificador único do utilizador.</param>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetEmployeeDetails(int id)
        {
            var emp = _context.Users
                .FirstOrDefault(u => u.Id == id && u.Role == "Funcionario");

            if (emp == null)
                return NotFound();

            // Ocorrências resolvidas
            var resolved = _context.Occurrences
                .Count(o => o.AssignedEmployeeId == id &&
                            (o.Status == OccurrenceStatus.Resolvido.ToString() ||
                             o.Status == OccurrenceStatus.Rejeitado.ToString()));

            // Ocorrências por resolver
            var pending = _context.Occurrences
                .Count(o => o.AssignedEmployeeId == id &&
                            (o.Status == OccurrenceStatus.EmAnalise.ToString() ||
                             o.Status == OccurrenceStatus.EmResolucao.ToString()));

            return Json(new
            {
                username = emp.Username,
                email = emp.Email,
                resolvedCount = resolved,
                pendingCount = pending
            });
        }

        /// <summary>
        /// Abre o formulário de criação de um novo utilizador.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processa a criação de um novo utilizador. 
        /// Utiliza o BCrypt para garantir que a password é guardada como uma Hash segura.
        /// </summary>
        /// <param name="model">ViewModel com os dados do novo utilizador.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                // Segurança: Nunca guardar passwords em texto limpo
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Token = null,
                TokenExpiry = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Carrega o formulário para editar os dados de um utilizador existente.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        /// <summary>
        /// Guarda as alterações feitas ao perfil de um utilizador. 
        /// Inclui proteção contra ataques e gestão de concorrência de base de dados.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,PasswordHash,Role,Token,TokenExpiry")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        /// <summary>
        /// Mostra a página de confirmação para remover um utilizador.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// <summary>
        /// Confirma a remoção definitiva do utilizador da base de dados.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Método auxiliar para verificar se um utilizador ainda existe na base de dados.
        /// </summary>
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}