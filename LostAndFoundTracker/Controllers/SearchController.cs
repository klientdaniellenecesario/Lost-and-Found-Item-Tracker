using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace LostAndFoundTracker.Controllers
{
    public class SearchController : Controller
    {
        [Route("Search")]  // makes /Search work
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
            // Temporary: use static list if it exists, otherwise return empty list
            var itemsList = ItemsController.items;
            if (itemsList == null)
            {
                return Json(new object[0]); // empty result
            }

            var results = itemsList.Where(i => !i.IsResolved).AsQueryable();

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