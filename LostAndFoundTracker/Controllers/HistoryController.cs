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

            // === QUERY 1: Items you posted that are now resolved ===
            var myPostedItems = await _context.Items
                .Include(i => i.User)
                .Where(i => i.Status == "returned" && i.UserId == userId.Value)
                .OrderByDescending(i => i.ConfirmedReturnDate ?? i.Date)
                .ToListAsync();

            // === QUERY 2: Found items posted by others that you claimed and are now resolved ===
            var claimedItemIds = await _context.Notifications
                .Where(n => n.SenderId == userId.Value && n.NotificationType == "Claim")
                .Select(n => n.ItemId)
                .Distinct()
                .ToListAsync();

            var myClaimedResolvedItems = await _context.Items
                .Include(i => i.User)
                .Where(i => claimedItemIds.Contains(i.Id)
                         && i.Type == "found"
                         && i.Status == "returned"
                         && i.UserId != userId.Value)  // exclude ones you posted yourself
                .OrderByDescending(i => i.ConfirmedReturnDate ?? i.Date)
                .ToListAsync();

            var resolvedItemsWithDetails = new List<ResolvedItemDetails>();

            // --- Build details for your own posted items ---
            foreach (var item in myPostedItems)
            {
                var details = new ResolvedItemDetails
                {
                    Item = item,
                    Poster = item.User,
                    HelperName = null,
                    ClaimantName = null,
                    StarsGiven = null,
                    ThankYouMessage = null,
                    IsClaimedByCurrentUser = false
                };

                if (item.Type == "lost")
                {
                    var starTransaction = await _context.StarTransactions
                        .Include(s => s.Receiver)
                        .FirstOrDefaultAsync(s => s.ItemId == item.Id);

                    if (starTransaction != null && starTransaction.Receiver != null)
                    {
                        details.HelperName = starTransaction.Receiver.FullName;
                        details.StarsGiven = starTransaction.StarsGiven;
                        details.ThankYouMessage = starTransaction.ThankYouMessage;
                    }
                }
                else if (item.Type == "found")
                {
                    var claimNotification = await _context.Notifications
                        .FirstOrDefaultAsync(n => n.ItemId == item.Id && n.NotificationType == "Claim");

                    if (claimNotification != null)
                    {
                        var claimant = await _context.Users.FindAsync(claimNotification.SenderId);
                        if (claimant != null)
                            details.ClaimantName = claimant.FullName;
                    }

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

            // --- Build details for found items you claimed from others ---
            foreach (var item in myClaimedResolvedItems)
            {
                var details = new ResolvedItemDetails
                {
                    Item = item,
                    Poster = item.User,   // the finder/poster
                    HelperName = null,
                    ClaimantName = null,
                    StarsGiven = null,
                    ThankYouMessage = null,
                    IsClaimedByCurrentUser = true
                };

                var starTransaction = await _context.StarTransactions
                    .FirstOrDefaultAsync(s => s.ItemId == item.Id);

                if (starTransaction != null)
                {
                    details.StarsGiven = starTransaction.StarsGiven;
                    details.ThankYouMessage = starTransaction.ThankYouMessage;
                }

                resolvedItemsWithDetails.Add(details);
            }

            // Sort the merged list together by resolved date descending
            resolvedItemsWithDetails = resolvedItemsWithDetails
                .OrderByDescending(d => d.Item?.ConfirmedReturnDate ?? d.Item?.Date)
                .ToList();

            // Star transactions involving current user
            var starTransactions = await _context.StarTransactions
                .Include(s => s.Giver)
                .Include(s => s.Receiver)
                .Include(s => s.Item)
                .Where(s => s.GiverId == userId.Value || s.ReceiverId == userId.Value)
                .OrderByDescending(s => s.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.ResolvedItems = resolvedItemsWithDetails;
            ViewBag.StarTransactions = starTransactions;

            return View();
        }
    }

    public class ResolvedItemDetails
    {
        public Item? Item { get; set; }
        public User? Poster { get; set; }
        public string? HelperName { get; set; }
        public string? ClaimantName { get; set; }
        public int? StarsGiven { get; set; }
        public string? ThankYouMessage { get; set; }
        public bool IsClaimedByCurrentUser { get; set; } = false;
    }
}