using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotesApplication.Data;
using NotesApplication.Models;
using static NuGet.Packaging.PackagingConstants;

namespace NotesApplication.Controllers
{
    public class NotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Notes
        public async Task<IActionResult> All(string? searchQuery)
        {
            var query = String.IsNullOrEmpty(searchQuery) ? "" : searchQuery;
            var notes = await _context.Notes.Where(n => n.ParentFolder.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier) && n.Title.ToUpper().Contains(query.ToUpper())).ToListAsync();
            return View(notes);
        }

        public async Task<IActionResult> Favourites()
        {
            var notes = await _context.Notes.Where(n => n.ParentFolder.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier) && n.IsFavourite == true).ToListAsync();
            return View(notes);
        }

        // GET: Notes/Details/5
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

        // GET: Notes/Create
        public IActionResult Create(int? parentFolderId)
        {
            ViewData["ParentFolderId"] = parentFolderId;
            return View();
        }

        // POST: Notes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Text,IsFavourite,Priority,ParentFolderId")] Note note)
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

        // GET: Notes/Edit/5
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
            //var folders = await _context.Folders.Where(f => f.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier)).ToListAsync();
            var folders = new SelectList(_context.Folders.Where(f => f.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier)), "Id", "Title");
            ViewData["Folders"] = folders;
            return View(note);
        }

        // POST: Notes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Text,IsFavourite,Priority,ParentFolderId")] Note note)
        {
            if (id != note.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                note.EditDate = DateTime.UtcNow;
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
            return View(note);
        }

        // GET: Notes/Delete/5
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

        // POST: Notes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
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
