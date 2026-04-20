using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LostAndFoundTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Landing()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { lostCount = 0, foundCount = 0, resolvedCount = 0, myItemsCount = 0, recentItems = new object[0] });
            }

            var lostCount = await _context.Items.CountAsync(i => i.Type == "lost" && !i.IsResolved);
            var foundCount = await _context.Items.CountAsync(i => i.Type == "found" && !i.IsResolved);
            var resolvedCount = await _context.Items.CountAsync(i => i.IsResolved);
            var myItemsCount = await _context.Items.CountAsync(i => i.UserId == userId.Value);

            var recentItems = await _context.Items
                .Where(i => !i.IsResolved)
                .OrderByDescending(i => i.Date)
                .Take(6)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    type = i.Type
                })
                .ToListAsync();

            return Json(new
            {
                lostCount,
                foundCount,
                resolvedCount,
                myItemsCount,
                recentItems
            });
        }

        public IActionResult Index()
        {
            return RedirectToAction("Landing");
        }
    }
}