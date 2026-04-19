using Microsoft.AspNetCore.Http;

namespace LostAndFoundTracker.Models.ViewModels
{
    public class ReportItemViewModel
    {
        public string ItemType { get; set; } // "lost" or "found"
        public string ItemName { get; set; }
        public string Category { get; set; }
        public string Location { get; set; }
        public System.DateTime Date { get; set; }
        public string Description { get; set; }
        public IFormFile Photo { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
    }
}