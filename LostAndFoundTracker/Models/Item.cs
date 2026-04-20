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
        public string PhotoUrl { get; set; } = string.Empty;  // ✅ Non‑nullable, default empty string
        public string ContactNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Foreign key
        public int UserId { get; set; }

        // Navigation property (required for EF relationship)
        public User? User { get; set; }

        public bool IsResolved { get; set; }
    }
}