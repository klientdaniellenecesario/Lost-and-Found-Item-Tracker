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
            // Add null check
            if (model == null)
            {
                ViewBag.Error = "Invalid form data. Please try again.";
                return View();
            }

            System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT ===");
            System.Diagnostics.Debug.WriteLine($"Email: {model.Email}");

            // Check if email or password is empty
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "Email and password are required.";
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // First check if email exists
                var userExists = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

                if (userExists == null)
                {
                    ViewBag.Error = "No account found with this email address. Please register first.";
                    return View(model);
                }

                // Check password
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.PasswordHash == model.Password);

                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserFullName", user.FullName);

                    var testUserId = HttpContext.Session.GetInt32("UserId");
                    var testUserEmail = HttpContext.Session.GetString("UserEmail");
                    System.Diagnostics.Debug.WriteLine($"Session verification - UserId: {testUserId}, Email: {testUserEmail}");

                    TempData["Success"] = $"Welcome back, {user.FullName}!";
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ViewBag.Error = "Incorrect password. Please try again.";
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

            if (model == null)
            {
                ViewBag.Error = "Invalid form data.";
                return View();
            }

            if (ModelState.IsValid)
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ViewBag.Error = "Passwords do not match!";
                    return View(model);
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
                if (existingUser != null)
                {
                    ViewBag.Error = "Email already registered. Please login instead.";
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

                System.Diagnostics.Debug.WriteLine($"New User ID: {user.Id}");
                System.Diagnostics.Debug.WriteLine($"New User Email: {user.Email}");

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
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction("Landing", "Home");
        }

        // ─────────────────────────────────────────
        // CHANGE PASSWORD
        // ─────────────────────────────────────────
        [Route("ChangePassword")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized(new { error = "Please login first" });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { error = "User not found" });

            if (user.PasswordHash != request.CurrentPassword)
                return BadRequest(new { error = "Current password is incorrect" });

            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest(new { error = "New password must be at least 6 characters" });

            user.PasswordHash = request.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        // ─────────────────────────────────────────
        // DELETE ACCOUNT - FIXED (includes Certificates)
        // ─────────────────────────────────────────
        [Route("DeleteAccount")]
        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            // 1. Delete user's items
            var userItems = _context.Items.Where(i => i.UserId == userId.Value);
            _context.Items.RemoveRange(userItems);

            // 2. Delete user's notifications
            var notifications = _context.Notifications
                .Where(n => n.SenderId == userId.Value || n.ReceiverId == userId.Value);
            _context.Notifications.RemoveRange(notifications);

            // 3. Delete star transactions
            var starTransactions = _context.StarTransactions
                .Where(st => st.GiverId == userId.Value || st.ReceiverId == userId.Value);
            _context.StarTransactions.RemoveRange(starTransactions);

            // 4. ✅ DELETE CERTIFICATES (THIS WAS MISSING!)
            var certificates = _context.Certificates
                .Where(c => c.UserId == userId.Value);
            _context.Certificates.RemoveRange(certificates);

            // Save all deletions
            await _context.SaveChangesAsync();

            // 5. Delete the user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();
            return Ok(new { message = "Account deleted successfully" });
        }

        // ─────────────────────────────────────────
        // FORGOT PASSWORD
        // ─────────────────────────────────────────
        [Route("ForgotPassword")]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [Route("ForgotPassword")]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)
            {
                ViewBag.Info = "If that email is registered, you will receive a reset link.";
                return View(model);
            }

            TempData["ResetEmail"] = user.Email;
            TempData["Success"] = "Email verified! Please enter your new password.";
            return RedirectToAction("ResetPassword", "Account");
        }

        // ─────────────────────────────────────────
        // RESET PASSWORD
        // ─────────────────────────────────────────
        [Route("ResetPassword")]
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["ResetEmail"] as string;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("ForgotPassword");

            TempData["ResetEmail"] = email;
            return View(new ResetPasswordViewModel { Email = email });
        }

        [Route("ResetPassword")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match or are empty.";
                TempData["ResetEmail"] = model?.Email;
                return View(model);
            }

            if (model.NewPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                TempData["ResetEmail"] = model.Email;
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return RedirectToAction("ForgotPassword");
            }

            user.PasswordHash = model.NewPassword;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully! Please sign in with your new password.";
            return RedirectToAction("Login", "Account");
        }

        // ─────────────────────────────────────────
        // DEBUG HELPERS
        // ─────────────────────────────────────────
        [Route("CheckUsers")]
        [HttpGet]
        public async Task<IActionResult> CheckUsers()
        {
            var users = await _context.Users.ToListAsync();
            System.Diagnostics.Debug.WriteLine($"=== TOTAL USERS IN DB: {users.Count} ===");

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