using Microsoft.AspNetCore.Mvc;
using LostAndFoundTracker.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using System;

namespace LostAndFoundTracker.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        [Route("Login")]
        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            return View();
        }

        [Route("Login")]
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Demo authentication
                if (model.Email == "student@campus.edu" && model.Password == "password123")
                {
                    HttpContext.Session.SetString("UserId", "1");
                    HttpContext.Session.SetString("UserEmail", model.Email);
                    HttpContext.Session.SetString("UserName", "John Smith");

                    TempData["Success"] = "Welcome back! You have successfully logged in.";
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ViewBag.Error = "Invalid email or password. Try: student@campus.edu / password123";
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
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ViewBag.Error = "Passwords do not match!";
                    return View(model);
                }

                HttpContext.Session.SetString("UserId", "1");
                HttpContext.Session.SetString("UserEmail", model.Email);
                HttpContext.Session.SetString("UserName", model.FullName);

                TempData["Success"] = "Account created successfully! Welcome to the Lost & Found system.";
                return RedirectToAction("Dashboard", "Home");
            }
            return View(model);
        }

        [Route("Logout")]
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Landing", "Home");
        }
    }
}