using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Collections.Generic;

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
                .Where(i => i.UserId == userId.Value && i.Type == "lost" && i.Status != "returned")
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            var myFoundItems = await _context.Items
                .Where(i => i.UserId == userId.Value && i.Type == "found" && i.Status != "returned")
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            var myResolvedItems = await _context.Items
                .Where(i => i.UserId == userId.Value && i.Status == "returned")
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

            // ✅ Update session with new name
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

            // ✅ Update session with new profile picture
            HttpContext.Session.SetString("UserProfilePic", user.ProfilePictureUrl);

            return Ok(new { photoUrl = user.ProfilePictureUrl });
        }

        // GET: /Profile/GetCurrentUser
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

        // GET: /Profile/GetStarSummary
        [HttpGet]
        public async Task<IActionResult> GetStarSummary()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            var certificates = await _context.Certificates
                .Where(c => c.UserId == userId.Value)
                .OrderByDescending(c => c.EarnedAt)
                .ToListAsync();

            var starTransactions = await _context.StarTransactions
                .Include(s => s.Giver)
                .Include(s => s.Item)
                .Where(s => s.ReceiverId == userId.Value)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();

            int nextMilestone = CertificateMilestone.GetNextMilestone(user.TotalStarPoints);
            int progressPercentage = CertificateMilestone.GetProgressToNextMilestone(user.TotalStarPoints);

            return Json(new
            {
                totalStars = user.TotalStarPoints,
                bronzeCertificates = user.BronzeCertificates,
                silverCertificates = user.SilverCertificates,
                goldCertificates = user.GoldCertificates,
                totalCertificates = user.TotalCertificatesEarned,
                nextMilestone = nextMilestone,
                progressPercentage = progressPercentage,
                certificates = certificates.Select(c => new
                {
                    c.Id,
                    c.CertificateType,
                    c.CertificateCode,
                    c.EarnedAt,
                    c.StarsEarned
                }),
                recentTransactions = starTransactions.Select(s => new
                {
                    stars = s.StarsGiven,
                    fromName = s.Giver?.FullName ?? "Someone",
                    itemName = s.Item?.Name ?? "an item",
                    message = s.ThankYouMessage,
                    date = s.CreatedAt.ToString("MMM dd, yyyy")
                })
            });
        }

        // GET: /Profile/GetAllCertificates
        [HttpGet]
        public async Task<IActionResult> GetAllCertificates()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var certificates = await _context.Certificates
                .Where(c => c.UserId == userId.Value)
                .OrderByDescending(c => c.EarnedAt)
                .ToListAsync();

            return Json(certificates.Select(c => new
            {
                c.Id,
                c.CertificateType,
                c.CertificateCode,
                c.EarnedAt,
                c.StarsEarned
            }));
        }

        // GET: /Profile/DownloadCertificate/{id}
        [HttpGet]
        public async Task<IActionResult> DownloadCertificate(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var certificate = await _context.Certificates
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

            if (certificate == null)
                return NotFound();

            string certificateHtml = GenerateCertificateHtml(certificate);
            return Content(certificateHtml, "text/html");
        }

        private string GenerateCertificateHtml(Certificate certificate)
        {
            string borderColor = certificate.CertificateType == "Gold" ? "#FFD700" :
                                (certificate.CertificateType == "Silver" ? "#C0C0C0" : "#CD7F32");

            string starColor = certificate.CertificateType == "Gold" ? "#f59e0b" :
                               (certificate.CertificateType == "Silver" ? "#9ca3af" : "#d97706");

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Certificate of Achievement - {certificate.CertificateType}</title>
                <meta charset=""utf-8"" />
                <style>
                    body {{
                        font-family: 'Georgia', 'Times New Roman', serif;
                        margin: 0;
                        padding: 0;
                        background: #f5f0e8;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                    }}
                    .certificate {{
                        width: 800px;
                        padding: 50px;
                        background: white;
                        border: 20px solid {borderColor};
                        border-radius: 20px;
                        text-align: center;
                        box-shadow: 0 10px 30px rgba(0,0,0,0.2);
                        position: relative;
                    }}
                    .certificate:before {{
                        content: '🏆';
                        font-size: 3rem;
                        position: absolute;
                        top: -30px;
                        left: 50%;
                        transform: translateX(-50%);
                        background: white;
                        padding: 0 20px;
                    }}
                    .certificate h1 {{
                        font-size: 2.5rem;
                        color: {borderColor};
                        margin-bottom: 20px;
                        text-transform: uppercase;
                        letter-spacing: 2px;
                    }}
                    .certificate h2 {{
                        font-size: 1.3rem;
                        color: #555;
                        font-weight: normal;
                        margin-bottom: 10px;
                    }}
                    .certificate .recipient {{
                        font-size: 2rem;
                        font-weight: bold;
                        color: #008080;
                        margin: 30px 0;
                        font-style: italic;
                        border-bottom: 1px solid #ddd;
                        display: inline-block;
                        padding-bottom: 10px;
                    }}
                    .certificate .message {{
                        font-size: 1.1rem;
                        color: #555;
                        margin: 20px 0;
                        line-height: 1.6;
                    }}
                    .certificate .stars {{
                        font-size: 2rem;
                        color: {starColor};
                        margin: 20px 0;
                        letter-spacing: 5px;
                    }}
                    .certificate .footer {{
                        margin-top: 40px;
                        font-size: 0.85rem;
                        color: #888;
                        border-top: 1px solid #ddd;
                        padding-top: 20px;
                    }}
                    .certificate .date {{
                        margin: 10px 0;
                    }}
                    .certificate .code {{
                        font-family: monospace;
                        font-size: 0.8rem;
                        color: #666;
                    }}
                    .print-btn {{
                        position: fixed;
                        bottom: 20px;
                        right: 20px;
                        padding: 10px 20px;
                        background: #008080;
                        color: white;
                        border: none;
                        border-radius: 5px;
                        cursor: pointer;
                        font-size: 1rem;
                        z-index: 1000;
                    }}
                    .print-btn:hover {{
                        background: #006666;
                    }}
                    @@media print {{
                        body {{ background: white; margin: 0; padding: 0; }}
                        .certificate {{ box-shadow: none; border: 15px solid {borderColor}; }}
                        .print-btn {{ display: none; }}
                    }}
                </style>
            </head>
            <body>
                <button class=""print-btn"" onclick=""window.print();"">🖨️ Print / Save as PDF</button>
                <div class='certificate'>
                    <h1>🏆 Certificate of Achievement 🏆</h1>
                    <h2>This certificate is proudly presented to</h2>
                    <div class='recipient'>{certificate.User?.FullName}</div>
                    <div class='message'>
                        For earning the <strong>{certificate.CertificateType}</strong> Certificate in the<br/>
                        <strong>Campus Lost & Found System</strong><br/>
                        Your dedication to helping others and being a responsible member of our campus community<br/>
                        is truly appreciated and recognized.
                    </div>
                    <div class='stars'>
                        {'★' + '★' + '★' + '★' + '★'}
                    </div>
                    <div class='footer'>
                        <div class='code'>Certificate Code: {certificate.CertificateCode}</div>
                        <div class='date'>Awarded on: {certificate.EarnedAt:MMMM dd, yyyy}</div>
                        <div>Stars Earned: {certificate.StarsEarned} ⭐</div>
                        <div style='margin-top: 15px; font-size: 0.7rem;'>Campus Lost & Found - Reuniting What Matters</div>
                    </div>
                </div>
            </body>
            </html>";
        }
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? ContactNumber { get; set; }
    }
}