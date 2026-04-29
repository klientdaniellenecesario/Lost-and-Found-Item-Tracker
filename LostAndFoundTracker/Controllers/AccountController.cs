using Microsoft.AspNetCore.Mvc;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using LostAndFoundTracker.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace LostAndFoundTracker.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────
        // LOGIN
        // ─────────────────────────────────────────
        [Route("Login")]
        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            System.Diagnostics.Debug.WriteLine("=== LOGIN PAGE LOADED ===");
            return View();
        }

        [Route("Login")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT ===");
            System.Diagnostics.Debug.WriteLine($"Email: {model.Email}");

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(
                    u => u.Email.ToLower() == model.Email.ToLower()
                      && u.PasswordHash == model.Password);

                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserFullName", user.FullName);

                    TempData["Success"] = $"Welcome back, {user.FullName}!";
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ViewBag.Error = "Invalid email or password.";
                }
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    System.Diagnostics.Debug.WriteLine($"Model error: {error.ErrorMessage}");
            }

            return View(model);
        }

        // ─────────────────────────────────────────
        // REGISTER
        // ─────────────────────────────────────────
        [Route("Register")]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [Route("Register")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTER ATTEMPT ===");

            if (ModelState.IsValid)
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ViewBag.Error = "Passwords do not match!";
                    return View(model);
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(
                    u => u.Email.ToLower() == model.Email.ToLower());

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = model.Password,
                    ContactNumber = "",
                    ProfilePictureUrl = null
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Account created successfully! Please log in.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    System.Diagnostics.Debug.WriteLine($"Model error: {error.ErrorMessage}");
            }

            return View(model);
        }

        // ─────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────
        [Route("Logout")]
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out.";
            return RedirectToAction("Landing", "Home");
        }

        // ─────────────────────────────────────────
        // CHANGE PASSWORD
        // ─────────────────────────────────────────
        [Route("ChangePassword")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            // Validate fields are not null or empty
            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest("Password fields cannot be empty.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.PasswordHash != request.CurrentPassword)
                return BadRequest("Current password is incorrect.");

            // ?? "" ensures we never assign null to a non-nullable string
            user.PasswordHash = request.NewPassword ?? string.Empty;
            await _context.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"Password changed for user ID: {userId}");
            return Ok();
        }

        // ─────────────────────────────────────────
        // DELETE ACCOUNT
        // ─────────────────────────────────────────
        [Route("DeleteAccount")]
        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var userItems = _context.Items.Where(i => i.UserId == userId);
            _context.Items.RemoveRange(userItems);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();
            System.Diagnostics.Debug.WriteLine($"Account deleted for user ID: {userId}");
            return Ok();
        }

        // ─────────────────────────────────────────
        // DEBUG ENDPOINTS
        // ─────────────────────────────────────────
        [Route("CheckUsers")]
        [HttpGet]
        public async Task<IActionResult> CheckUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Json(new
            {
                totalUsers = users.Count,
                users = users.Select(u => new {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.ContactNumber,
                    passwordHashLength = u.PasswordHash?.Length ?? 0
                })
            });
        }

        [Route("TestSession")]
        [HttpGet]
        public IActionResult TestSession()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userName = HttpContext.Session.GetString("UserFullName");

            return Json(new
            {
                isLoggedIn = userId != null,
                userId,
                userEmail,
                userName,
                sessionId = HttpContext.Session.Id
            });
        }
    }

    // ─────────────────────────────────────────
    // REQUEST MODEL FOR CHANGE PASSWORD
    // ─────────────────────────────────────────
    public class ChangePasswordRequest
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}