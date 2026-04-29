using Microsoft.AspNetCore.Mvc;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using LostAndFoundTracker.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;

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
            System.Diagnostics.Debug.WriteLine("=== LOGIN PAGE LOADED ===");
            System.Diagnostics.Debug.WriteLine("Session cleared");
            return View();
        }

        [Route("Login")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT ===");
            System.Diagnostics.Debug.WriteLine($"Email: {model.Email}");
            System.Diagnostics.Debug.WriteLine($"Password length: {model.Password?.Length ?? 0}");

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.PasswordHash == model.Password);

                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User found! ID: {user.Id}, Name: {user.FullName}, Email: {user.Email}");

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
                    System.Diagnostics.Debug.WriteLine("User not found or password incorrect");
                    ViewBag.Error = "Invalid email or password.";
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Model error: {error.ErrorMessage}");
                }
            }
            return View(model);
        }

        [Route("Register")]
        [HttpGet]
        public IActionResult Register()
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTER PAGE LOADED ===");
            return View();
        }

        [Route("Register")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTER ATTEMPT ===");
            System.Diagnostics.Debug.WriteLine($"FullName: {model.FullName}");
            System.Diagnostics.Debug.WriteLine($"Email: {model.Email}");
            System.Diagnostics.Debug.WriteLine($"Password length: {model.Password?.Length ?? 0}");

            if (ModelState.IsValid)
            {
                if (model.Password != model.ConfirmPassword)
                {
                    System.Diagnostics.Debug.WriteLine("Passwords do not match!");
                    ViewBag.Error = "Passwords do not match!";
                    return View(model);
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
                if (existingUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Email already exists: {model.Email}");
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
                int result = await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"User saved! Result: {result} record(s) affected");
                System.Diagnostics.Debug.WriteLine($"New User ID: {user.Id}");
                System.Diagnostics.Debug.WriteLine($"New User Email: {user.Email}");

                TempData["Success"] = "Account created successfully! Please log in.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Model error: {error.ErrorMessage}");
                }
            }
            return View(model);
        }

        [Route("Logout")]
        [HttpGet]
        public IActionResult Logout()
        {
            System.Diagnostics.Debug.WriteLine("=== LOGOUT ===");
            var userId = HttpContext.Session.GetInt32("UserId");
            System.Diagnostics.Debug.WriteLine($"Logging out user ID: {userId}");

            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out.";
            return RedirectToAction("Landing", "Home");
        }

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
                userId = userId,
                userEmail = userEmail,
                userName = userName,
                sessionId = HttpContext.Session.Id
            });
        }

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
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)
            {
                ViewBag.Info = "If that email is registered, you can now reset your password.";
                return View(model);
            }

            TempData["ResetEmail"] = user.Email;
            return RedirectToAction("ResetPassword", "Account");
        }

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
            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match or are empty.";
                TempData["ResetEmail"] = model.Email;
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)
                return RedirectToAction("ForgotPassword");

            user.PasswordHash = model.NewPassword;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully! Please sign in with your new password.";
            return RedirectToAction("Login", "Account");
        }
    }
}