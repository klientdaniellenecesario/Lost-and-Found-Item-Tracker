using Microsoft.AspNetCore.Mvc;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundTracker.Controllers
{
    [Route("History")]
    public class HistoryController : Controller
    {
        private readonly AppDbContext _context;

        public HistoryController(AppDbContext context)
        {
            _context = context;
        }

        [Route("Index")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var allMyItems = await _context.Items
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            ViewBag.MyLostItems = allMyItems
                .Where(i => i.Type == "lost" && !i.IsResolved)  // ← lowercase
                .ToList();

            ViewBag.MyFoundItems = allMyItems
                .Where(i => i.Type == "found" && !i.IsResolved)  // ← lowercase
                .ToList();

            ViewBag.MyResolvedItems = allMyItems
                .Where(i => i.IsResolved)
                .ToList();

            return View();
        }
    }
}