using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

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
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Debug: Log session info
            System.Diagnostics.Debug.WriteLine($"=== DASHBOARD DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"User ID from session: {userId}");
            System.Diagnostics.Debug.WriteLine($"User Email: {HttpContext.Session.GetString("UserEmail")}");
            System.Diagnostics.Debug.WriteLine($"User Name: {HttpContext.Session.GetString("UserFullName")}");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            System.Diagnostics.Debug.WriteLine($"=== GET DASHBOARD DATA DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"User ID from session: {userId}");

            if (userId == null)
            {
                System.Diagnostics.Debug.WriteLine("UserId is null - returning empty data");
                return Json(new { lostCount = 0, foundCount = 0, resolvedCount = 0, myItemsCount = 0, recentLostItems = new object[0], recentFoundItems = new object[0] });
            }

            // Get all items first to debug
            var allItems = await _context.Items.ToListAsync();
            System.Diagnostics.Debug.WriteLine($"Total items in database: {allItems.Count}");

            if (allItems.Any())
            {
                foreach (var item in allItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Item: ID={item.Id}, Name={item.Name}, Type={item.Type}, IsResolved={item.IsResolved}, UserId={item.UserId}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No items found in database!");
            }

            var lostCount = await _context.Items.CountAsync(i => i.Type == "lost" && !i.IsResolved);
            var foundCount = await _context.Items.CountAsync(i => i.Type == "found" && !i.IsResolved);
            var resolvedCount = await _context.Items.CountAsync(i => i.IsResolved);
            var myItemsCount = await _context.Items.CountAsync(i => i.UserId == userId.Value);

            System.Diagnostics.Debug.WriteLine($"Lost count: {lostCount}");
            System.Diagnostics.Debug.WriteLine($"Found count: {foundCount}");
            System.Diagnostics.Debug.WriteLine($"Resolved count: {resolvedCount}");
            System.Diagnostics.Debug.WriteLine($"My items count: {myItemsCount}");

            // Recent Lost Items (unresolved, limit 4)
            var recentLostItems = await _context.Items
                .Where(i => i.Type == "lost" && !i.IsResolved)
                .OrderByDescending(i => i.Date)
                .Take(4)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    type = i.Type,
                    photoUrl = i.PhotoUrl ?? ""
                })
                .ToListAsync();

            // Recent Found Items (unresolved, limit 4)
            var recentFoundItems = await _context.Items
                .Where(i => i.Type == "found" && !i.IsResolved)
                .OrderByDescending(i => i.Date)
                .Take(4)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    type = i.Type,
                    photoUrl = i.PhotoUrl ?? ""
                })
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"Recent lost items found: {recentLostItems.Count}");
            System.Diagnostics.Debug.WriteLine($"Recent found items found: {recentFoundItems.Count}");

            return Json(new
            {
                lostCount,
                foundCount,
                resolvedCount,
                myItemsCount,
                recentLostItems,
                recentFoundItems
            });
        }

        public IActionResult Index()
        {
            return RedirectToAction("Landing");
        }

        // GET: /Home/DebugDatabase - Debug endpoint to check database contents
        [HttpGet]
        public async Task<IActionResult> DebugDatabase()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            var allItems = await _context.Items.ToListAsync();
            var allUsers = await _context.Users.ToListAsync();

            var debug = new
            {
                SessionUserId = userId,
                SessionUserEmail = HttpContext.Session.GetString("UserEmail"),
                SessionUserName = HttpContext.Session.GetString("UserFullName"),
                TotalItems = allItems.Count,
                Items = allItems.Select(i => new {
                    i.Id,
                    i.Name,
                    i.Type,
                    i.IsResolved,
                    i.UserId,
                    i.Date,
                    i.Location
                }),
                TotalUsers = allUsers.Count,
                Users = allUsers.Select(u => new {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.ContactNumber
                })
            };

            return Json(debug);
        }

        // GET: /Home/CheckItems - Quick check of items
        [HttpGet]
        public async Task<IActionResult> CheckItems()
        {
            var lostItems = await _context.Items.Where(i => i.Type == "lost").ToListAsync();
            var foundItems = await _context.Items.Where(i => i.Type == "found").ToListAsync();

            return Content($@"
                === ITEMS SUMMARY ===
                Total Lost Items: {lostItems.Count}
                Total Found Items: {foundItems.Count}
                
                Lost Items:
                {string.Join("\n", lostItems.Select(i => $"  - {i.Name} (ID: {i.Id})"))}
                
                Found Items:
                {string.Join("\n", foundItems.Select(i => $"  - {i.Name} (ID: {i.Id})"))}
            ");
        }
    }
}