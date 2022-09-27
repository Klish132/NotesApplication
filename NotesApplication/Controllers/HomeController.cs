using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NotesApplication.Data;
using NotesApplication.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

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

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var rootFolder = await _context.Folders.FirstOrDefaultAsync(f => f.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier));
                return RedirectToRoute(new {controller = "Folders", action="Details", id = rootFolder.Id });
            }
            return View();
            //return RedirectToAction("Account//Login", "Identity");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}