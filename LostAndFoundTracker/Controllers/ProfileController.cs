using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace LostAndFoundTracker.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var myLostItems = await _context.Items
                .Where(i => i.UserId == userId.Value && i.Type == "lost" && !i.IsResolved)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            var myFoundItems = await _context.Items
                .Where(i => i.UserId == userId.Value && i.Type == "found" && !i.IsResolved)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            var myResolvedItems = await _context.Items
                .Where(i => i.UserId == userId.Value && i.IsResolved)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            ViewBag.MyLostItems = myLostItems;
            ViewBag.MyFoundItems = myFoundItems;
            ViewBag.MyResolvedItems = myResolvedItems;
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

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            if (photo == null || photo.Length == 0)
                return BadRequest("No photo uploaded.");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            user.ProfilePictureUrl = "/uploads/" + uniqueFileName;
            await _context.SaveChangesAsync();

            return Ok(new { photoUrl = user.ProfilePictureUrl });
        }

        // GET: /Profile/GetCurrentUser
        // This endpoint returns the current logged-in user's information
        // Used by the notification modals to display user's contact info
        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            return Json(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                contactNumber = user.ContactNumber ?? ""
            });
        }
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? ContactNumber { get; set; }
    }
}