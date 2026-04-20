using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LostAndFoundTracker.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LostAndFoundTracker.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        [Route("Search")]
        [Route("Search/Index")]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Results(string keyword, string category, string location, string type)
        {
            var query = _context.Items.Where(i => !i.IsResolved).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(i => i.Name.Contains(keyword) || i.Description.Contains(keyword));

            if (!string.IsNullOrEmpty(category) && category != "All Categories" && category != "")
                query = query.Where(i => i.Category == category);

            if (!string.IsNullOrEmpty(location))
                query = query.Where(i => i.Location.Contains(location));

            if (type == "lost")
                query = query.Where(i => i.Type == "lost");
            else if (type == "found")
                query = query.Where(i => i.Type == "found");

            var results = await query
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    type = i.Type,
                    category = i.Category,
                    photoUrl = i.PhotoUrl ?? ""   // include photo URL
                })
                .ToListAsync();

            return Json(results);
        }
    }
}