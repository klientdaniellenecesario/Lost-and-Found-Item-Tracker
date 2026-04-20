using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using LostAndFoundTracker.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LostAndFoundTracker.Controllers
{
    public class ItemsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ItemsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Items/LostItems
        public async Task<IActionResult> LostItems()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var lostItems = await _context.Items
                .Where(i => i.Type == "lost" && !i.IsResolved)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            ViewBag.CurrentUserId = userId;
            return View(lostItems);
        }

        // GET: /Items/FoundItems
        public async Task<IActionResult> FoundItems()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var foundItems = await _context.Items
                .Where(i => i.Type == "found" && !i.IsResolved)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            ViewBag.CurrentUserId = userId;
            return View(foundItems);
        }

        // GET: /Items/ReportItem
        [HttpGet]
        public IActionResult ReportItem()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var model = new ReportItemViewModel
            {
                Date = DateTime.Now,
                Email = HttpContext.Session.GetString("UserEmail") ?? ""
            };
            return View(model);
        }

        // POST: /Items/ReportItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportItem(ReportItemViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                string photoUrl = string.Empty;

                // Handle photo upload
                if (model.Photo != null && model.Photo.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Photo.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Photo.CopyToAsync(fileStream);
                    }
                    photoUrl = "/uploads/" + uniqueFileName;
                }

                var item = new Item
                {
                    Type = model.ItemType,
                    Name = model.ItemName,
                    Category = model.Category,
                    Location = model.Location,
                    Date = model.Date,
                    Description = model.Description,
                    ContactNumber = model.ContactNumber,
                    Email = model.Email,
                    UserId = userId.Value,
                    IsResolved = false,
                    PhotoUrl = photoUrl
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Your {model.ItemType} item has been reported successfully!";
                return RedirectToAction(model.ItemType == "lost" ? "LostItems" : "FoundItems");
            }
            return View(model);
        }

        // POST: /Items/MarkFound/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFound(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            if (item.UserId != userId.Value)
                return Forbid();

            item.IsResolved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item marked as found.";
            return RedirectToAction("LostItems");
        }

        // POST: /Items/MarkClaimed/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkClaimed(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            if (item.UserId != userId.Value)
                return Forbid();

            item.IsResolved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item marked as claimed.";
            return RedirectToAction("FoundItems");
        }

        // POST: /Items/ReportFoundByFinder/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportFoundByFinder(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            // Prevent owner from using this button (though view already hides it)
            if (item.UserId == userId.Value)
                return BadRequest("You cannot report your own item as found.");

            // TODO: Send actual notification (email, in-app message, etc.)
            TempData["Success"] = $"Thank you! The owner of '{item.Name}' has been notified that you found it.";

            return RedirectToAction("Detail", new { id });
        }

        // GET: /Items/Detail/{id}
        public async Task<IActionResult> Detail(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.Items
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
                return NotFound();

            ViewBag.CurrentUserId = userId;
            return View(item);
        }

        // GET: /Items/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            // Only the owner can edit
            if (item.UserId != userId.Value)
                return Forbid();

            // Map Item to ReportItemViewModel for editing
            var model = new ReportItemViewModel
            {
                ItemType = item.Type,
                ItemName = item.Name,
                Category = item.Category,
                Location = item.Location,
                Date = item.Date,
                Description = item.Description,
                ContactNumber = item.ContactNumber,
                Email = item.Email
            };

            return View(model);
        }

        // POST: /Items/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReportItemViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            if (item.UserId != userId.Value)
                return Forbid();

            if (ModelState.IsValid)
            {
                // Update fields (photo remains unchanged)
                item.Type = model.ItemType;
                item.Name = model.ItemName;
                item.Category = model.Category;
                item.Location = model.Location;
                item.Date = model.Date;
                item.Description = model.Description;
                item.ContactNumber = model.ContactNumber;
                item.Email = model.Email;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Item updated successfully!";
                return RedirectToAction("Detail", new { id });
            }

            return View(model);
        }
    }
}