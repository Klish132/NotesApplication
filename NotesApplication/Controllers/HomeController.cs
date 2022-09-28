using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NotesApplication.Data;
using NotesApplication.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Swashbuckle.AspNetCore.Annotations;

namespace NotesApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [SwaggerOperation(Summary = "Если нет залогиненного пользователя - показывает Index. Иначе - перенаправляет на корневую папку пользователя.")]
        [HttpGet]
        [Route("")]
        [Route("Folders")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var rootFolder = await _context.Folders.FirstOrDefaultAsync(f => f.IsRoot == true && f.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (rootFolder != null)
                    return RedirectToRoute(new {controller = "Folders", action="Details", id = rootFolder.Id });
            }
            return View();
            //return RedirectToAction("Account//Login", "Identity");
        }

        [HttpGet]
        [Route("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}