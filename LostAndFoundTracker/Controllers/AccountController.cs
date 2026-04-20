using Microsoft.AspNetCore.Mvc;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using LostAndFoundTracker.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

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

        [Route("Login")]
        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            return View();
        }

        [Route("Login")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find user by email (case‑insensitive) and compare plain password
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.PasswordHash == model.Password);
                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserFullName", user.FullName);

                    TempData["Success"] = "Welcome back!";
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ViewBag.Error = "Invalid email or password.";
                }
            }
            return View(model);
        }

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
            if (ModelState.IsValid)
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ViewBag.Error = "Passwords do not match!";
                    return View(model);
                }

                // Check if email already exists (case‑insensitive)
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    return View(model);
                }

                // Create new user – store plain password in PasswordHash field
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = model.Password,   // plain text (for learning only)
                    ContactNumber = "",
                    ProfilePictureUrl = null
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Redirect to Login page
                TempData["Success"] = "Account created successfully! Please log in.";
                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }

        [Route("Logout")]
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out.";
            return RedirectToAction("Landing", "Home");
        }
    }
}