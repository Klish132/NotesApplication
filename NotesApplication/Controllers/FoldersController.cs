using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using NotesApplication.Data;
using NotesApplication.Models;
using NotesApplication.ViewModels;
using static System.Net.WebRequestMethods;
using static NuGet.Packaging.PackagingConstants;

namespace NotesApplication.Controllers
{
    public class FoldersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FoldersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment; 
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

        public async Task WriteFile(IFormFile file)
        {
            var imagesPath = _webHostEnvironment.WebRootPath + "\\images\\";
            if (!Directory.Exists(imagesPath))
                Directory.CreateDirectory(imagesPath);
            var filePath = imagesPath + file.FileName;

            try
            {
                var fileStream = new FileStream(filePath, FileMode.Create);
                using (fileStream)
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        // POST: Folders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FolderViewModel folderViewModel)
        {
            if (ModelState.IsValid)
            {
                var folder = new Folder
                {
                    Title = folderViewModel.Title,
                    OwnerId = folderViewModel.OwnerId,
                    ParentFolderId = folderViewModel.ParentFolderId,
                };
                var file = folderViewModel.ImageFile;
                await WriteFile(file);
                folder.ImagePath = file.FileName;
                _context.Add(folder);
                await _context.SaveChangesAsync();
                /*var imagesPath = _webHostEnvironment.WebRootPath + "\\images\\";
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);
                var file = folderViewModel.ImageFile;
                var filePath = imagesPath + file.FileName;

                try
                {
                    var fileStream = new FileStream(filePath, FileMode.Create);
                    using (fileStream)
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    folder.ImagePath = file.FileName;
                    _context.Add(folder);
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    throw;
                }*/
                return RedirectToAction("Details", folder);
            }
            ViewData["OwnerId"] = folderViewModel.OwnerId;
            ViewData["ParentFolderId"] = folderViewModel.ParentFolderId;
            return View(folderViewModel);
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
            var folderViewModel = new FolderViewModel
            {
                Id = folder.Id,
                Title = folder.Title,
                OwnerId = folder.OwnerId,
                ParentFolderId = folder.ParentFolderId
            };
            ViewData["OwnerId"] = folder.OwnerId;
            ViewData["ParentFolderId"] = folder.ParentFolderId;
            return View(folderViewModel);
        }

        // POST: Folders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FolderViewModel folderViewModel)
        {
            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folderViewModel.Id);
            if (id != folderViewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                folder.Title = folderViewModel.Title;
                var file = folderViewModel.ImageFile;
                await WriteFile(file);
                folder.ImagePath = file.FileName;

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
            ViewData["OwnerId"] = folderViewModel.OwnerId;
            ViewData["ParentFolderId"] = folderViewModel.ParentFolderId;
            return View(folderViewModel);
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
            var parentFolder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folder.ParentFolderId);
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
