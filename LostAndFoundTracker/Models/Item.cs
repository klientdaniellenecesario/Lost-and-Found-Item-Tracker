using System;

namespace LostAndFoundTracker.Models
{
    public class Item
    {
        public int Id { get; set; }

        // "lost" or "found"
        public string Type { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public DateTime Date { get; set; }
        public DateTime? LostDate { get; set; }
        public DateTime? FoundDate { get; set; }

        public string Description { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Foreign key
        public int UserId { get; set; }

        // Navigation property
        public User? User { get; set; }

        // Item Status: "active", "claimed", "returned"
        public string Status { get; set; } = "active";

        [Obsolete("Use Status property instead. IsResolved will be removed in future.")]
        public bool IsResolved { get; set; } = false;

        // Star rating given for this item (1-5)
        public int? StarRatingGiven { get; set; }

        // Who confirmed return (the owner who received the item back)
        public int? ConfirmedByUserId { get; set; }
        public DateTime? ConfirmedReturnDate { get; set; }
    }
}