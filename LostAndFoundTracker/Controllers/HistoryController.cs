using Microsoft.AspNetCore.Mvc;
using LostAndFoundTracker.Data;
using LostAndFoundTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LostAndFoundTracker.Controllers
{
    [Route("History")]
    public class HistoryController : Controller
    {
        private readonly AppDbContext _context;

        public HistoryController(AppDbContext context)
        {
            _context = context;
        }

        [Route("Index")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // Get ALL resolved items (not just user's own)
            var resolvedItems = await _context.Items
                .Include(i => i.User)
                .Where(i => i.IsResolved && i.Status == "returned")
                .OrderByDescending(i => i.ConfirmedReturnDate ?? i.Date)
                .ToListAsync();

            // For each resolved item, get the related parties
            var resolvedItemsWithDetails = new List<ResolvedItemDetails>();

            foreach (var item in resolvedItems)
            {
                var details = new ResolvedItemDetails
                {
                    Item = item,
                    Poster = item.User,
                    HelperName = null,
                    HelperEmail = null,
                    HelperContact = null,
                    ClaimantName = null,
                    ClaimantEmail = null,
                    ClaimantContact = null
                };

                if (item.Type == "lost")
                {
                    // For lost items: find who helped (star transaction receiver)
                    var starTransaction = await _context.StarTransactions
                        .Include(s => s.Receiver)
                        .FirstOrDefaultAsync(s => s.ItemId == item.Id);

                    if (starTransaction != null && starTransaction.Receiver != null)
                    {
                        details.HelperName = starTransaction.Receiver.FullName;
                        details.HelperEmail = starTransaction.Receiver.Email;
                        details.HelperContact = starTransaction.Receiver.ContactNumber ?? "Not provided";
                        details.StarsGiven = starTransaction.StarsGiven;
                        details.ThankYouMessage = starTransaction.ThankYouMessage;
                    }
                }
                else if (item.Type == "found")
                {
                    // For found items: find who claimed it
                    var claimNotification = await _context.Notifications
                        .FirstOrDefaultAsync(n => n.ItemId == item.Id && n.NotificationType == "Claim");

                    if (claimNotification != null)
                    {
                        var claimant = await _context.Users.FindAsync(claimNotification.SenderId);
                        if (claimant != null)
                        {
                            details.ClaimantName = claimant.FullName;
                            details.ClaimantEmail = claimant.Email;
                            details.ClaimantContact = claimant.ContactNumber ?? "Not provided";
                        }
                    }

                    // Also get star transaction if exists
                    var starTransaction = await _context.StarTransactions
                        .FirstOrDefaultAsync(s => s.ItemId == item.Id);

                    if (starTransaction != null)
                    {
                        details.StarsGiven = starTransaction.StarsGiven;
                        details.ThankYouMessage = starTransaction.ThankYouMessage;
                    }
                }

                resolvedItemsWithDetails.Add(details);
            }

            var starTransactions = await _context.StarTransactions
                .Include(s => s.Giver)
                .Include(s => s.Receiver)
                .Include(s => s.Item)
                .OrderByDescending(s => s.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.ResolvedItems = resolvedItemsWithDetails;
            ViewBag.StarTransactions = starTransactions;

            return View();
        }
    }

    // Helper class for resolved item details
    public class ResolvedItemDetails
    {
        public Item? Item { get; set; }
        public User? Poster { get; set; }
        public string? HelperName { get; set; }
        public string? HelperEmail { get; set; }
        public string? HelperContact { get; set; }
        public string? ClaimantName { get; set; }
        public string? ClaimantEmail { get; set; }
        public string? ClaimantContact { get; set; }
        public int? StarsGiven { get; set; }
        public string? ThankYouMessage { get; set; }
    }
}