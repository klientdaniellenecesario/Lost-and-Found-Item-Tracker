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
                return Json(new
                {
                    myLostCount = 0,
                    myFoundCount = 0,
                    resolvedCount = 0,
                    myLostItems = new object[0],
                    myFoundItems = new object[0],
                    myClaimedItems = new object[0],
                    myHelpingItems = new object[0]
                });
            }

            // Count user's own lost items (active, not yet resolved)
            var myLostCount = await _context.Items
                .CountAsync(i => i.UserId == userId.Value && i.Type == "lost" && i.Status != "returned");

            // Count user's own found items (active, not yet resolved)
            var myFoundCount = await _context.Items
                .CountAsync(i => i.UserId == userId.Value && i.Type == "found" && i.Status != "returned");

            // Count user's own resolved items (status = "returned")
            var resolvedCount = await _context.Items
                .CountAsync(i => i.UserId == userId.Value && i.Status == "returned");

            // MY LOST ITEMS (only user's own, not yet resolved)
            var myLostItems = await _context.Items
                .Where(i => i.UserId == userId.Value && i.Type == "lost" && i.Status != "returned")
                .OrderByDescending(i => i.Date)
                .Take(6)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    type = i.Type,
                    photoUrl = i.PhotoUrl ?? "",
                    status = i.Status ?? "active"
                })
                .ToListAsync();

            // MY FOUND ITEMS (only user's own, not yet resolved)
            var myFoundItems = await _context.Items
                .Where(i => i.UserId == userId.Value && i.Type == "found" && i.Status != "returned")
                .OrderByDescending(i => i.Date)
                .Take(6)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    type = i.Type,
                    photoUrl = i.PhotoUrl ?? "",
                    status = i.Status ?? "active",
                    starRatingGiven = i.StarRatingGiven ?? 0
                })
                .ToListAsync();

            // MY CLAIMED ITEMS — found items posted by others that this user has claimed
            var claimedItemIds = await _context.Notifications
                .Where(n => n.SenderId == userId.Value && n.NotificationType == "Claim")
                .Select(n => n.ItemId)
                .Distinct()
                .ToListAsync();

            var myClaimedItems = await _context.Items
                .Include(i => i.User)
                .Where(i => claimedItemIds.Contains(i.Id)
                         && i.Type == "found"
                         && i.UserId != userId.Value
                         && i.Status != "returned")
                .OrderByDescending(i => i.Date)
                .Take(6)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    photoUrl = i.PhotoUrl ?? "",
                    status = i.Status ?? "active",
                    starRatingGiven = i.StarRatingGiven ?? 0,
                    finderName = i.User != null ? i.User.FullName : "Unknown",
                    isSelectedClaimant = i.SelectedClaimantId == userId.Value
                })
                .ToListAsync();

            // ✅ NEW: MY HELPING ITEMS — lost items that the current user reported as found (Finder perspective)
            var helpingItemIds = await _context.Notifications
                .Where(n => n.SenderId == userId.Value && n.NotificationType == "FoundMatch")
                .Select(n => n.ItemId)
                .Distinct()
                .ToListAsync();

            var myHelpingItems = await _context.Items
                .Include(i => i.User)
                .Where(i => helpingItemIds.Contains(i.Id)
                         && i.Type == "lost"
                         && i.UserId != userId.Value  // Not your own lost item
                         && i.Status != "returned")   // Still active, not yet returned
                .OrderByDescending(i => i.Date)
                .Take(6)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    location = i.Location,
                    date = i.Date.ToString("MMM dd, yyyy"),
                    photoUrl = i.PhotoUrl ?? "",
                    status = i.Status ?? "active",
                    ownerName = i.User != null ? i.User.FullName : "Unknown",
                    ownerEmail = i.User != null ? i.User.Email : ""
                })
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"My lost count: {myLostCount}");
            System.Diagnostics.Debug.WriteLine($"My found count: {myFoundCount}");
            System.Diagnostics.Debug.WriteLine($"Resolved count: {resolvedCount}");
            System.Diagnostics.Debug.WriteLine($"My lost items found: {myLostItems.Count}");
            System.Diagnostics.Debug.WriteLine($"My found items found: {myFoundItems.Count}");
            System.Diagnostics.Debug.WriteLine($"My claimed items: {myClaimedItems.Count}");
            System.Diagnostics.Debug.WriteLine($"My helping items: {myHelpingItems.Count}");

            return Json(new
            {
                myLostCount,
                myFoundCount,
                resolvedCount,
                myLostItems,
                myFoundItems,
                myClaimedItems,
                myHelpingItems  // ✅ NEW
            });
        }

        public IActionResult Index()
        {
            return RedirectToAction("Landing");
        }

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
                    Status = i.Status,
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