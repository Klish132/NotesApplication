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
    public class FoldersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FoldersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Folders
        /*public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Folders.Include(f => f.Owner).Include(f => f.ParentFolder);
            return View(await applicationDbContext.ToListAsync());
        }*/

        public List<Folder> GetChildFolders(int id)
        {
            return _context.Folders.Where(f => f.ParentFolderId == id).ToList();
        }
        public List<Note> GetNotes(int id)
        {
            return _context.Notes.Where(f => f.ParentFolderId == id).ToList();
        }

        // GET: Folders/Details/5
        public async Task<IActionResult> Details(int? id, string? sortType, bool? dir)
        {
            if (id == null || _context.Folders == null)
            {
                return NotFound();
            }

            var folder = await _context.Folders.FindAsync(id);

            if (folder == null)
            {
                return NotFound();
            }

            if (folder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }
            var sort = String.IsNullOrEmpty(sortType) ? "title" : sortType;
            var ascending = dir is null ? true : false;

            var childFolders = await _context.Folders.OrderBy(f => f.Title).Where(f => f.ParentFolder == folder).ToListAsync();
            var notes = new List<Note>();

            if (ascending)
            {
                switch (sort)
                {
                    case "title":
                        notes = await _context.Notes.OrderBy(n => n.Title).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case "prio":
                        notes = await _context.Notes.OrderBy(n => n.Priority).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case "date":
                        notes = await _context.Notes.OrderBy(n => n.CreationDate).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (sort)
                {
                    case "title":
                        notes = await _context.Notes.OrderByDescending(n => n.Title).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case "prio":
                        notes = await _context.Notes.OrderByDescending(n => n.Priority).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case "date":
                        notes = await _context.Notes.OrderByDescending(n => n.CreationDate).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    default:
                        break;
                }
            }

            ViewData["ChildFolders"] = childFolders;
            ViewData["Notes"] = notes;
            return View(folder);
        }

        // GET: Folders/Create
        public IActionResult Create(int? parentFolderId)
        {
            ViewData["OwnerId"] = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["ParentFolderId"] = parentFolderId;
            return View();
        }

        // POST: Folders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Image,OwnerId,ParentFolderId")] Folder folder)
        {
            if (ModelState.IsValid)
            {
                _context.Add(folder);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", folder);
            }
            ViewData["OwnerId"] = folder.OwnerId;
            ViewData["ParentFolderId"] = folder.ParentFolderId;
            return View(folder);
        }

        // GET: Folders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Folders == null)
            {
                return NotFound();
            }

            var folder = await _context.Folders.FindAsync(id);
            if (folder == null)
            {
                return NotFound();
            }
            if (folder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }
            ViewData["OwnerId"] = folder.OwnerId;
            ViewData["ParentFolderId"] = folder.ParentFolderId;
            return View(folder);
        }

        // POST: Folders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Image")] Folder folder)
        {
            if (id != folder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(folder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FolderExists(folder.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", folder);
            }
            ViewData["OwnerId"] = folder.OwnerId;
            ViewData["ParentFolderId"] = folder.ParentFolderId;
            return View(folder);
        }

        // GET: Folders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Folders == null)
            {
                return NotFound();
            }

            var folder = await _context.Folders.FindAsync(id);

            if (folder == null)
            {
                return NotFound();
            }
            if (folder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }
            var childFolders = await _context.Folders.Where(f => f.ParentFolder == folder).ToListAsync();
            var notes = await _context.Notes.Where(f => f.ParentFolder == folder).ToListAsync();
            if (childFolders.Count != 0 || notes.Count != 0 || folder.IsRoot)
            {
                return RedirectToAction("Details", folder);
            }

            return View(folder);
        }

        // POST: Folders/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Folders == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Folders'  is null.");
            }
            var folder = await _context.Folders.FindAsync(id);
            var parentFolder = await _context.Folders.FirstOrDefaultAsync(f => f.ParentFolder == folder);
            if (folder != null)
            {
                _context.Folders.Remove(folder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", parentFolder);
        }

        private bool FolderExists(int id)
        {
            return _context.Folders.Any(e => e.Id == id);
        }
    }
}
