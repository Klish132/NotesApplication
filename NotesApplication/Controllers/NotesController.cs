using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotesApplication.Data;
using NotesApplication.Models;
using Swashbuckle.AspNetCore.Annotations;

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

        [SwaggerOperation(Summary = "Get: Получает все заметки пользователя. Post: для поиска по названиям.")]
        [HttpGet]
        [HttpPost]
        [Route("All/")]
        public async Task<IActionResult> All([FromForm]string? searchQuery)
        {
            var query = String.IsNullOrEmpty(searchQuery) ? "" : searchQuery;
            var notes = await _context.Notes.Where(n => n.ParentFolder.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier) && n.Title.ToUpper().Contains(query.ToUpper())).ToListAsync();
            return View(notes);
        }

        [SwaggerOperation(Summary = "Избранные заметки.")]
        [HttpGet]
        [Route("Favourites/")]
        public async Task<IActionResult> Favourites()
        {
            var notes = await _context.Notes.Where(n => n.ParentFolder.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier) && n.IsFavourite == true).ToListAsync();
            return View(notes);
        }

        [SwaggerOperation(Summary = "Информация о заметке.")]
        [HttpGet]
        [Route("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Notes == null)
            {
                return NotFound();
            }

            var note = await _context.Notes
                .Include(n => n.ParentFolder)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (note == null)
            {
                return NotFound();
            }
            var parentFolder = await _context.Folders.FirstOrDefaultAsync(n => n.Id == note.ParentFolderId);
            if (parentFolder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }

            return View(note);
        }

        [SwaggerOperation(Summary = "Для страницы создания заметки.")]
        [HttpGet]
        [Route("Create/")]
        public IActionResult Create(int? parentFolderId)
        {
            ViewData["ParentFolderId"] = parentFolderId;
            return View();
        }

        [SwaggerOperation(Summary = "Создает заметку.")]
        [HttpPost]
        [Route("Create/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm]Note note)
        {
            if (ModelState.IsValid)
            {
                _context.Add(note);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", note);
            }

            ViewData["ParentFolderId"] = note.ParentFolderId;
            return View(note);
        }

        [SwaggerOperation(Summary = "Для страницы редактирования заметки.")]
        [HttpGet]
        [Route("Edit/{id?}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Notes == null)
            {
                return NotFound();
            }

            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }
            var parentFolder = await _context.Folders.FirstOrDefaultAsync(n => n.Id == note.ParentFolderId);
            if (parentFolder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }
            var folders = new SelectList(_context.Folders.Where(f => f.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier)), "Id", "Title");
            ViewData["Folders"] = folders;
            return View(note);
        }

        [SwaggerOperation(Summary = "Редактирует заметку.")]
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm]Note editedNote)
        {
            if (id != editedNote.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == editedNote.Id);

                note.Title = editedNote.Title;
                note.Text = editedNote.Text;
                note.IsFavourite = editedNote.IsFavourite;
                note.Priority = editedNote.Priority;
                note.EditDate = DateTime.UtcNow;
                note.ParentFolderId = editedNote.ParentFolderId;
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
                return RedirectToAction("Details", note);
            }
            var folders = await _context.Folders.Where(f => f.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier)).ToListAsync();
            ViewData["Folders"] = folders;
            return View(editedNote);
        }

        [SwaggerOperation(Summary = "Для страницы удаления заметки.")]
        [HttpGet]
        [Route("Delete/{id?}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Notes == null)
            {
                return NotFound();
            }

            var note = await _context.Notes
                .Include(n => n.ParentFolder)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (note == null)
            {
                return NotFound();
            }
            var parentFolder = await _context.Folders.FirstOrDefaultAsync(n => n.Id == note.ParentFolderId);
            if (parentFolder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }

            return View(note);
        }

        [SwaggerOperation(Summary = "Удаляет заметку.")]
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Notes == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Notes'  is null.");
            }
            var note = await _context.Notes.FindAsync(id);
            var parentFolder = await _context.Folders.FirstOrDefaultAsync(n => n.Id == note.ParentFolderId);
            if (note != null)
            {
                _context.Notes.Remove(note);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Folders", parentFolder);
        }

        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.Id == id);
        }
    }
}
