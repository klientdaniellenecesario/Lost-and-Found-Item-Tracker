using Microsoft.AspNetCore.Mvc;

namespace LostAndFoundTracker.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        public IActionResult Results(string keyword, string category, string location, string type)
        {
            var results = ItemsController.items.Where(i => !i.IsResolved).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                results = results.Where(i => i.Name.Contains(keyword) || i.Description.Contains(keyword));

            if (!string.IsNullOrEmpty(category) && category != "All Categories" && category != "")
                results = results.Where(i => i.Category == category);

            if (!string.IsNullOrEmpty(location))
                results = results.Where(i => i.Location.Contains(location));

            if (type == "lost")
                results = results.Where(i => i.Type == "lost");
            else if (type == "found")
                results = results.Where(i => i.Type == "found");

            return Json(results.Select(i => new
            {
                id = i.Id,
                name = i.Name,
                location = i.Location,
                date = i.Date.ToString("MMM dd, yyyy"),
                type = i.Type,
                category = i.Category
            }).ToList());
        }
    }
}