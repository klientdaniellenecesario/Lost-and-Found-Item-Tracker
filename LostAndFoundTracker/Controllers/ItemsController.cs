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
        public async Task<IActionResult> ReportItem()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new ReportItemViewModel
            {
                Date = DateTime.Now,
                Email = user.Email ?? "",
                ContactNumber = user.ContactNumber ?? ""
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

            if (model.Photo == null || model.Photo.Length == 0)
                ModelState.AddModelError("Photo", "Please upload a photo of the item.");

            if (ModelState.IsValid)
            {
                try
                {
                    string photoUrl = string.Empty;

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
                        LostDate = model.ItemType == "lost" ? model.Date : null,
                        FoundDate = model.ItemType == "found" ? model.Date : null,
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
                catch (Exception ex)
                {
                    TempData["Error"] = $"Database error: {ex.Message}";
                    return View(model);
                }
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

        // POST: /Items/ReportFoundMatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportFoundMatch([FromBody] FoundMatchRequest request)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { error = "Please login first" });
                }

                if (request == null || request.ItemId == 0)
                {
                    return BadRequest(new { error = "Invalid request. Item ID is required." });
                }

                var lostItem = await _context.Items
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.Type == "lost");

                if (lostItem == null)
                {
                    return NotFound(new { error = "Lost item not found" });
                }

                if (lostItem.UserId == userId.Value)
                {
                    return BadRequest(new { error = "You cannot report your own item" });
                }

                var finder = await _context.Users.FindAsync(userId.Value);
                if (finder == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                var notification = new Notification
                {
                    ReceiverId = lostItem.UserId,
                    SenderId = userId.Value,
                    ItemId = request.ItemId,
                    NotificationType = "FoundMatch",
                    Message = string.IsNullOrEmpty(request.Message)
                        ? $"I found your item '{lostItem.Name}'. Please contact me."
                        : request.Message,
                    Status = "Unread",
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Owner notified successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: /Items/ClaimFound
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimFound([FromBody] ClaimFoundRequest request)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { error = "Please login first" });
                }

                if (request == null || request.ItemId == 0)
                {
                    return BadRequest(new { error = "Invalid request. Item ID is required." });
                }

                var foundItem = await _context.Items
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.Type == "found");

                if (foundItem == null)
                {
                    return NotFound(new { error = "Found item not found" });
                }

                if (foundItem.UserId == userId.Value)
                {
                    return BadRequest(new { error = "You cannot claim your own item" });
                }

                var claimant = await _context.Users.FindAsync(userId.Value);
                if (claimant == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                var notification = new Notification
                {
                    ReceiverId = foundItem.UserId,
                    SenderId = userId.Value,
                    ItemId = request.ItemId,
                    NotificationType = "Claim",
                    Message = string.IsNullOrEmpty(request.Message)
                        ? $"This '{foundItem.Name}' belongs to me. Please contact me."
                        : request.Message,
                    Status = "Unread",
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Finder notified successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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

            if (item.UserId != userId.Value)
                return Forbid();

            var model = new ReportItemViewModel
            {
                Id = item.Id,
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

        // POST: /Items/Edit/{id}  [FIXED]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReportItemViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (id != model.Id)
            {
                TempData["Error"] = "Item ID mismatch.";
                return RedirectToAction("LostItems");
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            if (item.UserId != userId.Value)
                return Forbid();

            // Remove Photo from validation (optional during edit)
            ModelState.Remove("Photo");

            if (ModelState.IsValid)
            {
                // Update basic fields
                item.Type = model.ItemType;
                item.Name = model.ItemName;
                item.Category = model.Category;
                item.Location = model.Location;
                item.Date = model.Date;
                item.Description = model.Description;
                item.ContactNumber = model.ContactNumber;
                item.Email = model.Email;

                // Handle new photo upload if provided
                if (model.Photo != null && model.Photo.Length > 0)
                {
                    // Delete old photo file if exists
                    if (!string.IsNullOrEmpty(item.PhotoUrl))
                    {
                        string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, item.PhotoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    // Save new photo
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Photo.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Photo.CopyToAsync(fileStream);
                    }
                    item.PhotoUrl = "/uploads/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Item updated successfully!";
                return RedirectToAction("Detail", new { id = item.Id });
            }

            return View(model);
        }

        // POST: /Items/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            if (item.UserId != userId.Value)
                return Forbid();

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item deleted successfully.";
            return RedirectToAction("Index", "Profile");
        }

        // GET: /Items/Reuse/{id}
        [HttpGet]
        public async Task<IActionResult> Reuse(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            if (item.UserId != userId.Value)
                return Forbid();

            var model = new ReportItemViewModel
            {
                ItemType = item.Type,
                ItemName = item.Name,
                Category = item.Category,
                Location = item.Location,
                Date = DateTime.Now,
                Description = item.Description,
                ContactNumber = item.ContactNumber,
                Email = item.Email
            };

            TempData["Info"] = "We pre-filled the form based on your previous report. You can edit before submitting.";
            return View("ReportItem", model);
        }
    }

    // Request models for notifications
    public class FoundMatchRequest
    {
        public int ItemId { get; set; }
        public string? Message { get; set; }
    }

    public class ClaimFoundRequest
    {
        public int ItemId { get; set; }
        public string? Message { get; set; }
    }
}