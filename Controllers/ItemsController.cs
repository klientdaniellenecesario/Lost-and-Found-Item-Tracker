using Microsoft.AspNetCore.Mvc;
using LostAndFoundTracker.Models;
using LostAndFoundTracker.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;

namespace LostAndFoundTracker.Controllers
{
    [Route("Items")]
    public class ItemsController : Controller
    {
        // Make items public static so SearchController can access
        public static List<Item> items = new List<Item>();
        private static int nextId = 1;

        [Route("LostItems")]
        public IActionResult LostItems()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Account");
            }
            var lostItems = items.Where(i => i.Type == "lost" && !i.IsResolved).ToList();
            return View(lostItems);
        }

        [Route("FoundItems")]
        public IActionResult FoundItems()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Account");
            }
            var foundItems = items.Where(i => i.Type == "found" && !i.IsResolved).ToList();
            return View(foundItems);
        }

        [Route("ReportItem")]
        [HttpGet]
        public IActionResult ReportItem()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [Route("ReportItem")]
        [HttpPost]
        public IActionResult SubmitReport(ReportItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userEmail = HttpContext.Session.GetString("UserEmail") ?? model.Email;

                var item = new Item
                {
                    Id = nextId++,
                    Type = model.ItemType,
                    Name = model.ItemName,
                    Category = model.Category,
                    Location = model.Location,
                    Date = model.Date,
                    Description = model.Description,
                    ContactNumber = model.ContactNumber,
                    Email = userEmail,
                    UserId = int.Parse(userId ?? "1"),
                    IsResolved = false,
                    PhotoUrl = null
                };

                items.Add(item);

                TempData["Success"] = $"Your {model.ItemType} item has been reported successfully!";
                return RedirectToAction(model.ItemType == "lost" ? "LostItems" : "FoundItems");
            }
            return View("ReportItem", model);
        }

        [HttpPost]
        [Route("MarkFound/{id}")]
        public IActionResult MarkFound(int id)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                item.IsResolved = true;
                return Ok();
            }
            return NotFound();
        }

        [HttpPost]
        [Route("MarkClaimed/{id}")]
        public IActionResult MarkClaimed(int id)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                item.IsResolved = true;
                return Ok();
            }
            return NotFound();
        }

        [Route("Detail/{id}")]
        public IActionResult Detail(int id)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item == null)
                return NotFound();

            return View(item);
        }
    }
}