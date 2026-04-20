using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace LostAndFoundTracker.Models.ViewModels
{
    public class ReportItemViewModel
    {
        [Required(ErrorMessage = "Please select if you lost or found the item")]
        public string ItemType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Item name is required")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Now;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please upload a photo of the item")]
        public IFormFile? Photo { get; set; }  // Required, but still nullable for model binding

        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string ContactNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
    }
}