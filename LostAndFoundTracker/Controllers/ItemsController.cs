#nullable enable

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
using System.Collections.Generic;

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
        public async Task<IActionResult> LostItems(string sort = "recent")
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var query = _context.Items.Where(i => i.Type == "lost" && i.Status != "returned" && i.UserId != userId.Value);

            query = sort switch
            {
                "oldest" => query.OrderBy(i => i.Date),
                _ => query.OrderByDescending(i => i.Date)
            };

            var lostItems = await query.ToListAsync();

            ViewBag.CurrentUserId = userId;
            ViewBag.CurrentSort = sort;
            return View(lostItems);
        }

        // GET: /Items/FoundItems
        public async Task<IActionResult> FoundItems(string sort = "recent")
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var query = _context.Items
                .Include(i => i.User)
                .Where(i => i.Type == "found" && i.Status != "returned" && i.UserId != userId.Value);

            query = sort switch
            {
                "oldest" => query.OrderBy(i => i.Date),
                _ => query.OrderByDescending(i => i.Date)
            };

            var foundItems = await query.ToListAsync();

            ViewBag.CurrentUserId = userId;
            ViewBag.CurrentSort = sort;

            return View(foundItems);
        }

        // GET: /Items/GetClaimantsForItem/{id}
        [HttpGet]
        public async Task<IActionResult> GetClaimantsForItem(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var claims = await _context.Notifications
                .Where(n => n.ItemId == id && n.NotificationType == "Claim")
                .Include(n => n.Sender)
                .Select(n => new
                {
                    id = n.SenderId,
                    name = n.Sender!.FullName,
                    email = n.Sender.Email,
                    message = n.Message,
                    claimedAt = n.CreatedAt
                })
                .ToListAsync();

            return Json(claims);
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
                        Status = "active",
                        PhotoUrl = photoUrl
                    };

                    _context.Items.Add(item);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Your {model.ItemType} item has been reported successfully!";
                    return RedirectToAction("Dashboard", "Home");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Database error: {ex.Message}";
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Items/GetUsersForSearch
        [HttpGet]
        public async Task<IActionResult> GetUsersForSearch(string term)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            var users = await _context.Users
                .Where(u => u.Id != currentUserId.Value &&
                            (u.FullName.Contains(term) || u.Email.Contains(term)))
                .Take(10)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.FullName,
                    email = u.Email
                })
                .ToListAsync();

            return Json(users);
        }

        // POST: /Items/MarkFoundWithReward/{id} - FOR LOST ITEMS ONLY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFoundWithReward(int id, [FromBody] MarkFoundRewardRequest request)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { error = "Please login first" });
                }

                var item = await _context.Items
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null)
                {
                    return NotFound(new { error = "Item not found" });
                }

                if (item.UserId != userId.Value)
                {
                    return BadRequest(new { error = "You are not the owner of this item" });
                }

                if (item.Type != "lost")
                {
                    return BadRequest(new { error = "Only lost items can be marked as found" });
                }

                if (item.Status == "returned")
                {
                    return BadRequest(new { error = "This item has already been marked as found" });
                }

                // ✅ FIX: Lost items go directly to "returned" (disappear immediately)
                item.Status = "returned";
                item.ConfirmedReturnDate = DateTime.Now;

                if (request.HelperId.HasValue && request.HelperId.Value > 0 && request.StarRating.HasValue)
                {
                    if (request.StarRating.Value < 1 || request.StarRating.Value > 5)
                    {
                        return BadRequest(new { error = "Star rating must be between 1 and 5" });
                    }

                    if (request.HelperId.Value == userId.Value)
                    {
                        return BadRequest(new { error = "You cannot give stars to yourself" });
                    }

                    int helperId = request.HelperId.Value;
                    var helper = await _context.Users.FindAsync(helperId);
                    if (helper == null)
                    {
                        return BadRequest(new { error = "Helper not found" });
                    }

                    var starTransaction = new StarTransaction
                    {
                        ReceiverId = helperId,
                        GiverId = userId.Value,
                        ItemId = id,
                        StarsGiven = request.StarRating.Value,
                        ThankYouMessage = request.ThankYouMessage ?? "",
                        CreatedAt = DateTime.Now
                    };
                    _context.StarTransactions.Add(starTransaction);

                    int oldStars = helper.TotalStarPoints;
                    helper.TotalStarPoints += request.StarRating.Value;

                    var newCertificateType = CertificateMilestone.GetCertificateType(helper.TotalStarPoints);
                    var oldCertificateType = CertificateMilestone.GetCertificateType(oldStars);

                    if (newCertificateType != oldCertificateType && newCertificateType != "None")
                    {
                        var certificate = new Certificate
                        {
                            UserId = helperId,
                            CertificateType = newCertificateType,
                            StarsRequired = newCertificateType == "Gold" ? CertificateMilestone.GoldStars :
                                           (newCertificateType == "Silver" ? CertificateMilestone.SilverStars : CertificateMilestone.BronzeStars),
                            StarsEarned = helper.TotalStarPoints,
                            CertificateCode = GenerateCertificateCode(helperId, newCertificateType),
                            EarnedAt = DateTime.Now
                        };
                        _context.Certificates.Add(certificate);
                        helper.TotalCertificatesEarned++;

                        if (newCertificateType == "Bronze") helper.BronzeCertificates++;
                        else if (newCertificateType == "Silver") helper.SilverCertificates++;
                        else if (newCertificateType == "Gold") helper.GoldCertificates++;

                        var certificateNotification = new Notification
                        {
                            ReceiverId = helperId,
                            SenderId = userId.Value,
                            ItemId = id,
                            NotificationType = "CertificateEarned",
                            Message = $"Congratulations! You've earned a {newCertificateType} Certificate for your helpfulness! You now have {helper.TotalStarPoints} total stars.",
                            Status = "Unread",
                            CreatedAt = DateTime.Now
                        };
                        _context.Notifications.Add(certificateNotification);
                    }

                    await _context.SaveChangesAsync();

                    var owner = await _context.Users.FindAsync(userId.Value);
                    var starNotification = new Notification
                    {
                        ReceiverId = helperId,
                        SenderId = userId.Value,
                        ItemId = id,
                        NotificationType = "StarsReceived",
                        Message = $"{owner?.FullName ?? "Someone"} gave you {request.StarRating.Value} stars for helping them find '{item.Name}'! You now have {helper.TotalStarPoints} total stars.",
                        Status = "Unread",
                        CreatedAt = DateTime.Now
                    };
                    _context.Notifications.Add(starNotification);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "Item marked as found and removed from lost items!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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

            item.Status = "returned";
            item.ConfirmedReturnDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item marked as found.";
            return RedirectToAction("LostItems");
        }

        // POST: /Items/MarkClaimed/{id} - Finder selects the REAL owner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkClaimed(int id, [FromBody] MarkClaimedRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var item = await _context.Items
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound();

            if (item.UserId != userId.Value)
                return Forbid();

            if (item.Type != "found")
                return BadRequest("Only found items can be marked as claimed.");

            int selectedClaimantId = request.SelectedClaimantId;

            // Verify this claimant actually claimed the item
            var claimExists = await _context.Notifications
                .AnyAsync(n => n.ItemId == id && n.NotificationType == "Claim" && n.SenderId == selectedClaimantId);

            if (!claimExists)
            {
                return BadRequest("Invalid claimant selected.");
            }

            item.Status = "ready_for_rating";
            item.SelectedClaimantId = selectedClaimantId;

            await _context.SaveChangesAsync();

            // Send notification ONLY to the SELECTED owner
            var notification = new Notification
            {
                ReceiverId = selectedClaimantId,
                SenderId = userId.Value,
                ItemId = id,
                NotificationType = "ClaimConfirmed",
                Message = $"The finder has confirmed that '{item.Name}' belongs to you! Please confirm pickup and rate them.",
                Status = "Unread",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Owner has been notified to confirm pickup.";
            return RedirectToAction("FoundItems");
        }

        // POST: /Items/ConfirmReturn/{id} - Owner rates finder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReturn(int id, [FromBody] ConfirmReturnRequest request)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { error = "Please login first" });
                }

                var item = await _context.Items
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null)
                {
                    return NotFound(new { error = "Item not found" });
                }

                // Check if current user is the SELECTED claimant
                bool isSelectedClaimant = (item.SelectedClaimantId == userId.Value);

                if (!isSelectedClaimant && item.UserId != userId)
                {
                    return BadRequest(new { error = "You are not authorized to confirm return for this item" });
                }

                if (item.Status == "returned")
                {
                    return BadRequest(new { error = "This item has already been confirmed as returned" });
                }

                if (request.StarRating < 1 || request.StarRating > 5)
                {
                    return BadRequest(new { error = "Star rating must be between 1 and 5" });
                }

                int finderId = item.UserId;

                item.Status = "returned";
                item.StarRatingGiven = request.StarRating;
                item.ConfirmedByUserId = userId;
                item.ConfirmedReturnDate = DateTime.Now;

                var starTransaction = new StarTransaction
                {
                    ReceiverId = finderId,
                    GiverId = userId.Value,
                    ItemId = id,
                    StarsGiven = request.StarRating,
                    ThankYouMessage = request.ThankYouMessage ?? "",
                    CreatedAt = DateTime.Now
                };
                _context.StarTransactions.Add(starTransaction);

                var finder = await _context.Users.FindAsync(finderId);
                if (finder != null)
                {
                    int oldStars = finder.TotalStarPoints;
                    finder.TotalStarPoints += request.StarRating;

                    var newCertificateType = CertificateMilestone.GetCertificateType(finder.TotalStarPoints);
                    var oldCertificateType = CertificateMilestone.GetCertificateType(oldStars);

                    if (newCertificateType != oldCertificateType && newCertificateType != "None")
                    {
                        var certificate = new Certificate
                        {
                            UserId = finderId,
                            CertificateType = newCertificateType,
                            StarsRequired = newCertificateType == "Gold" ? CertificateMilestone.GoldStars :
                                           (newCertificateType == "Silver" ? CertificateMilestone.SilverStars : CertificateMilestone.BronzeStars),
                            StarsEarned = finder.TotalStarPoints,
                            CertificateCode = GenerateCertificateCode(finderId, newCertificateType),
                            EarnedAt = DateTime.Now
                        };
                        _context.Certificates.Add(certificate);
                        finder.TotalCertificatesEarned++;

                        if (newCertificateType == "Bronze") finder.BronzeCertificates++;
                        else if (newCertificateType == "Silver") finder.SilverCertificates++;
                        else if (newCertificateType == "Gold") finder.GoldCertificates++;

                        var certificateNotification = new Notification
                        {
                            ReceiverId = finderId,
                            SenderId = userId.Value,
                            ItemId = id,
                            NotificationType = "CertificateEarned",
                            Message = $"Congratulations! You've earned a {newCertificateType} Certificate for your helpfulness! You now have {finder.TotalStarPoints} total stars.",
                            Status = "Unread",
                            CreatedAt = DateTime.Now
                        };
                        _context.Notifications.Add(certificateNotification);
                    }
                }

                await _context.SaveChangesAsync();

                var giver = await _context.Users.FindAsync(userId.Value);
                var starNotification = new Notification
                {
                    ReceiverId = finderId,
                    SenderId = userId.Value,
                    ItemId = id,
                    NotificationType = "StarsReceived",
                    Message = $"{giver?.FullName ?? "Someone"} gave you {request.StarRating} stars for returning '{item.Name}'! You now have {(finder?.TotalStarPoints ?? 0)} total stars.",
                    Status = "Unread",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(starNotification);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Return confirmed and stars awarded!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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

                string message = string.IsNullOrEmpty(request.Message)
                    ? $"I found your item '{lostItem.Name}'. Please contact me."
                    : request.Message;

                var notification = new Notification
                {
                    ReceiverId = lostItem.UserId,
                    SenderId = userId.Value,
                    ItemId = request.ItemId,
                    NotificationType = "FoundMatch",
                    Message = message,
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

        // POST: /Items/ClaimFound - Owner claims item
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

                string message = string.IsNullOrEmpty(request.Message)
                    ? $"This '{foundItem.Name}' belongs to me. Please contact me."
                    : request.Message;

                var notification = new Notification
                {
                    ReceiverId = foundItem.UserId,
                    SenderId = userId.Value,
                    ItemId = request.ItemId,
                    NotificationType = "Claim",
                    Message = message,
                    Status = "Unread",
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);

                foundItem.Status = "claimed";

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Finder notified successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: /Items/GetFinderInfo/{id}
        [HttpGet]
        public async Task<IActionResult> GetFinderInfo(int id)
        {
            var item = await _context.Items
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound(new { error = "Item not found" });

            return Json(new
            {
                finderName = item.User?.FullName ?? "the finder",
                itemName = item.Name
            });
        }

        // GET: /Items/Detail/{id}
        public async Task<IActionResult> Detail(int id, string? returnUrl = null)
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

            if (string.IsNullOrEmpty(returnUrl))
                ViewBag.ReturnUrl = item.Type == "lost" ? "/Items/LostItems" : "/Items/FoundItems";
            else
                ViewBag.ReturnUrl = returnUrl;

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

        // POST: /Items/Edit/{id}
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

            ModelState.Remove("Photo");

            if (ModelState.IsValid)
            {
                item.Type = model.ItemType;
                item.Name = model.ItemName;
                item.Category = model.Category;
                item.Location = model.Location;
                item.Date = model.Date;
                item.Description = model.Description;
                item.ContactNumber = model.ContactNumber;
                item.Email = model.Email;

                if (model.Photo != null && model.Photo.Length > 0)
                {
                    if (!string.IsNullOrEmpty(item.PhotoUrl))
                    {
                        string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, item.PhotoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

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
                return RedirectToAction("Detail", new { id = item.Id, returnUrl = HttpContext.Request.Query["returnUrl"].ToString() });
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

            var relatedNotifications = _context.Notifications.Where(n => n.ItemId == id);
            _context.Notifications.RemoveRange(relatedNotifications);

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item and its related notifications deleted successfully.";
            return RedirectToAction("Index", "Profile");
        }

        private string GenerateCertificateCode(int userId, string certificateType)
        {
            return $"{certificateType.ToUpper()}-{userId}-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }

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

    public class ConfirmReturnRequest
    {
        public int StarRating { get; set; }
        public string? ThankYouMessage { get; set; }
    }

    public class MarkFoundRewardRequest
    {
        public int? HelperId { get; set; }
        public int? StarRating { get; set; }
        public string? ThankYouMessage { get; set; }
    }

    public class MarkClaimedRequest
    {
        public int SelectedClaimantId { get; set; }
    }
}