using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotesApplication.Data;
using NotesApplication.Models;
using NotesApplication.ViewModels;
using Swashbuckle.AspNetCore.Annotations;
using static NuGet.Packaging.PackagingConstants;

namespace NotesApplication.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class NotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        [SwaggerOperation(Summary = "Get: Получает все заметки пользователя. Post: для поиска по названиям.")]
        [HttpPost]
        [Route("All")]
        public async Task<IActionResult> All([FromForm]string? searchQuery)
        {
            var query = String.IsNullOrEmpty(searchQuery) ? "" : searchQuery.ToUpper();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notes = await _context.Notes.Where(n => n.ParentFolder.OwnerId == userId && n.Title.ToUpper().Contains(query)).ToListAsync();
            return View(notes);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Избранные заметки.")]
        [HttpGet]
        [Route("Favourites")]
        public async Task<IActionResult> Favourites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notes = await _context.Notes.Where(n => n.ParentFolder.OwnerId == userId && n.IsFavourite == true).ToListAsync();
            return View(notes);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Информация о заметке.")]
        [HttpGet]
        [Route("Details/{id?}")]
        public async Task<IActionResult> Details(int id)
        {
            var note = await _context.Notes.Include(n => n.ParentFolder).FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                return NotFound();
            }
            if (note.ParentFolder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }

            return View(note);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Для страницы создания заметки.")]
        [HttpGet]
        [Route("Create")]
        public IActionResult Create(int parentFolderId)
        {
            var noteViewModel = new NoteViewModel{ ParentFolderId = parentFolderId };
            return View(noteViewModel);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Создает заметку.")]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm]NoteViewModel noteViewModel)
        {
            if (ModelState.IsValid)
            {
                var note = new Note
                {
                    Title = noteViewModel.Title,
                    Text = noteViewModel.Text,
                    IsFavourite = noteViewModel.IsFavourite,
                    CreationDate = DateTimeOffset.UtcNow,
                    EditDate = DateTimeOffset.UtcNow,
                    Priority = noteViewModel.Priority,
                    ParentFolderId = noteViewModel.ParentFolderId,
                };

                _context.Add(note);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = note.Id });
            }
            return View(noteViewModel);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Для страницы редактирования заметки.")]
        [HttpGet]
        [Route("Edit/{id?}")]
        public async Task<IActionResult> Edit(int id)
        {
            var note = await _context.Notes.Include(n => n.ParentFolder).FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (note.ParentFolder.OwnerId != userId)
            {
                return Unauthorized();
            }

            var noteViewModel = new NoteViewModel
            {
                Id = note.Id,
                Title = note.Title,
                Text = note.Text,
                IsFavourite = note.IsFavourite,
                Priority = note.Priority,
                ParentFolderId = note.ParentFolderId
            };

            var folders = new SelectList(_context.Folders.Where(f => f.OwnerId == userId), "Id", "Title");
            ViewData["Folders"] = folders;
            return View(noteViewModel);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Редактирует заметку.")]
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm]NoteViewModel noteViewModel)
        {
            if (id != noteViewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var note = await _context.Notes.FindAsync(id);

                note.Title = noteViewModel.Title;
                note.Text = noteViewModel.Text;
                note.IsFavourite = noteViewModel.IsFavourite;
                note.Priority = noteViewModel.Priority;
                note.EditDate = DateTimeOffset.UtcNow;
                note.ParentFolderId = noteViewModel.ParentFolderId;
                try
                {
                    _context.Update(note);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoteExists(note.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", new { id = note.Id });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var folders = new SelectList(_context.Folders.Where(f => f.OwnerId == userId), "Id", "Title");
            ViewData["Folders"] = folders;
            return View(noteViewModel);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Для страницы удаления заметки.")]
        [HttpGet]
        [Route("Delete/{id?}")]
        public async Task<IActionResult> Delete(int id)
        {
            var note = await _context.Notes.Include(n => n.ParentFolder).FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                return NotFound();
            }
            if (note.ParentFolder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }

            return View(note);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Удаляет заметку.")]
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var note = await _context.Notes.Include(n => n.ParentFolder).FirstOrDefaultAsync(n => n.Id == id);
            if (note != null)
            {
                _context.Notes.Remove(note);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Folders", new { id = note.ParentFolder.Id });
        }

        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.Id == id);
        }
    }
}
