using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
        public enum SortType
        {
            Title,
            Priority,
            Date
        }

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FoldersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [Authorize]
        [SwaggerOperation(Summary = "Информация о папке, включая все лежащие в ней папки и заметки.")]
        [HttpGet]
        [Route("Details/{id}")]
        public async Task<IActionResult> Details(int id, int? sortType, bool? dir)
        {
            var folder = await _context.Folders
                .Include(f => f.ChildFolders.OrderBy(cf => cf.Title))
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
            {
                return NotFound();
            }

            if (folder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }
            var sort =  sortType is null ? (int)SortType.Title : sortType;
            var ascending = dir is null ? false : true;

            var notes = new List<Note>();

            if (ascending)
            {
                switch (sort)
                {
                    case (int)SortType.Title:
                        notes = await _context.Notes.OrderBy(n => n.Title).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case (int)SortType.Priority:
                        notes = await _context.Notes.OrderBy(n => n.Priority).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case (int)SortType.Date:
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
                    case (int)SortType.Title:
                        notes = await _context.Notes.OrderByDescending(n => n.Title).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case (int)SortType.Priority:
                        notes = await _context.Notes.OrderByDescending(n => n.Priority).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    case (int)SortType.Date:
                        notes = await _context.Notes.OrderByDescending(n => n.CreationDate).Where(n => n.ParentFolder == folder).ToListAsync();
                        break;
                    default:
                        break;
                }
            }

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

        [Authorize]
        [SwaggerOperation(Summary = "Для страницы создания папки.")]
        [HttpGet]
        [Route("Create")]
        public IActionResult Create(int parentFolderId)
        {
            var folderViewModel = new FolderViewModel
            {
                ParentFolderId = parentFolderId,
                OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };
            return View(folderViewModel);
        }

        [Authorize]
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

                return RedirectToAction("Details", new { id = folder.Id });
            }
            return View(folderViewModel);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Для страницы редактирования папки.")]
        [HttpGet]
        [Route("Edit/{id?}")]
        public async Task<IActionResult> Edit(int id)
        {
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
                return RedirectToAction("Details", new { id = folder.Id });
            }
            var folderViewModel = new FolderViewModel
            {
                Id = folder.Id,
                Title = folder.Title,
                ImageFileName = folder.ImageFileName
            };
            return View(folderViewModel);
        }

        [Authorize]
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
                var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == id);

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
                return RedirectToAction("Details", new { id = folder.Id });
            }
            return View(folderViewModel);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Для страницы удаления папки.")]
        [HttpGet]
        [Route("Delete/{id?}")]
        public async Task<IActionResult> Delete(int id)
        {
            var folder = await _context.Folders
                .Include(f => f.ChildFolders)
                .Include(f => f.Notes)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
            {
                return NotFound();
            }
            if (folder.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }
            if (folder.ChildFolders.Count != 0 || folder.Notes.Count != 0 || folder.IsRoot)
            {
                return RedirectToAction("Details", new { id = folder.Id });
            }

            return View(folder);
        }

        [Authorize]
        [SwaggerOperation(Summary = "Удаляет папку.")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var folder = await _context.Folders.Include(f => f.ParentFolder).FirstOrDefaultAsync(f => f.Id == id);
            if (folder != null)
            {
                _context.Folders.Remove(folder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = folder.ParentFolder.Id });
        }

        private bool FolderExists(int id)
        {
            return _context.Folders.Any(e => e.Id == id);
        }
    }
}
