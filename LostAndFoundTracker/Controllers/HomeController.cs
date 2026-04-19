using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace LostAndFoundTracker.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Landing()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        public IActionResult GetDashboardData()
        {
            var userId = HttpContext.Session.GetString("UserId");

            var lostCount = ItemsController.items.Count(i => i.Type == "lost" && !i.IsResolved);
            var foundCount = ItemsController.items.Count(i => i.Type == "found" && !i.IsResolved);
            var resolvedCount = ItemsController.items.Count(i => i.IsResolved);
            var myItemsCount = ItemsController.items.Count(i => i.UserId.ToString() == userId);

            var recentItems = ItemsController.items.OrderByDescending(i => i.Date)
                                   .Take(6)
                                   .Select(i => new {
                                       id = i.Id,
                                       name = i.Name,
                                       location = i.Location,
                                       date = i.Date.ToString("MMM dd, yyyy"),
                                       type = i.Type
                                   }).ToList();

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