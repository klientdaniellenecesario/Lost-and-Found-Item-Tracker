using Microsoft.AspNetCore.Mvc;

namespace LostAndFoundTracker.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                HttpContext.Session.SetString("UserName", request.FullName);
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "No changes made" });
        }
    }

    public class UpdateProfileRequest
    {
        public string FullName { get; set; }
        public string ContactNumber { get; set; }
    }
}