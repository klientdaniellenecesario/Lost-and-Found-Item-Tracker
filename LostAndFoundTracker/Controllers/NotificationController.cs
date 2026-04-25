using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LostAndFoundTracker.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Get unread count for bell badge
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(0);

            var count = await _context.Notifications
                .Where(n => n.ReceiverId == userId.Value && n.Status == "Unread")
                .CountAsync();

            return Json(count);
        }

        // GET: Notifications page (separate full page)
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var notifications = await _context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Item)
                .Where(n => n.ReceiverId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // GET: Notification details page
        public async Task<IActionResult> Details(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var notification = await _context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .Include(n => n.Item)
                .FirstOrDefaultAsync(n => n.Id == id && n.ReceiverId == userId.Value);

            if (notification == null)
                return NotFound();

            // Mark as read when viewed
            if (notification.Status == "Unread")
            {
                notification.Status = "Read";
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return View(notification);
        }
    }
}