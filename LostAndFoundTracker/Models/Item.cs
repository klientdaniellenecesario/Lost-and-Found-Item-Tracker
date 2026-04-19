using System;

namespace LostAndFoundTracker.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Type { get; set; } // "lost" or "found"
        public string Name { get; set; }
        public string Category { get; set; }
        public string Location { get; set; }
        public DateTime Date { get; set; }
        public DateTime? LostDate { get; set; }
        public DateTime? FoundDate { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public int UserId { get; set; }
        public bool IsResolved { get; set; }
    }
}