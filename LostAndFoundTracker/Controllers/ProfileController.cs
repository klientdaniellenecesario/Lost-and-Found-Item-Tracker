using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace LostAndFoundTracker.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var myItems = await _context.Items
                .Where(i => i.UserId == userId.Value)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            ViewBag.MyItems = myItems;
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrEmpty(request.FullName))
                user.FullName = request.FullName;
            if (!string.IsNullOrEmpty(request.ContactNumber))
                user.ContactNumber = request.ContactNumber;

            await _context.SaveChangesAsync();

            // Update session
            HttpContext.Session.SetString("UserFullName", user.FullName);

            return Ok();
        }
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? ContactNumber { get; set; }
    }
}