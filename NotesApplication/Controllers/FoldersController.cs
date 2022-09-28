using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesApplication.Data;
using NotesApplication.Models;
using NotesApplication.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace NotesApplication.Controllers
{
    [Route("[controller]")]
    [ApiController]
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

        [SwaggerOperation(Summary = "Информация о папке, включая все лежащие в ней папки и заметки.")]
        [HttpGet]
        [Route("Details/{id}")]
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
            var ascending = dir is null ? false : true;

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

        [NonAction]
        private async Task WriteFile(IFormFile file, string fileName)
        {
            var imagesPath = _webHostEnvironment.WebRootPath + "\\images\\";
            if (!Directory.Exists(imagesPath))
                Directory.CreateDirectory(imagesPath);
            var filePath = imagesPath + fileName;

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

        [SwaggerOperation(Summary = "Для страницы создания папки.")]
        [HttpGet]
        [Route("Create/")]
        public IActionResult Create(int? parentFolderId)
        {
            ViewData["OwnerId"] = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["ParentFolderId"] = parentFolderId;
            return View();
        }

        [SwaggerOperation(Summary = "Создает папку.")]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm]FolderViewModel folderViewModel)
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
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                await WriteFile(file, fileName);
                folder.ImageFileName = fileName;

                _context.Add(folder);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", folder);
            }
            ViewData["OwnerId"] = folderViewModel.OwnerId;
            ViewData["ParentFolderId"] = folderViewModel.ParentFolderId;
            return View(folderViewModel);
        }

        [SwaggerOperation(Summary = "Для страницы редактирования папки.")]
        [HttpGet]
        [Route("Edit/{id?}")]
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
            if (folder.IsRoot)
            {
                return RedirectToAction("Details", folder);
            }
            var folderViewModel = new FolderViewModel
            {
                Id = folder.Id,
                Title = folder.Title,
                ImageFileName = folder.ImageFileName,
                OwnerId = folder.OwnerId,
                ParentFolderId = folder.ParentFolderId
            };
            ViewData["OwnerId"] = folder.OwnerId;
            ViewData["ParentFolderId"] = folder.ParentFolderId;
            return View(folderViewModel);
        }

        [SwaggerOperation(Summary = "Редактирует папку.")]
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm]FolderViewModel folderViewModel)
        {
            if (id != folderViewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folderViewModel.Id);

                folder.Title = folderViewModel.Title;
                var file = folderViewModel.ImageFile;
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                await WriteFile(file, fileName);
                folder.ImageFileName = fileName;

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

        [SwaggerOperation(Summary = "Для страницы удаления папки.")]
        [HttpGet]
        [Route("Delete/{id?}")]
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

        [SwaggerOperation(Summary = "Удаляет папку.")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id}")]
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
