using System;
using System.Collections.Generic;

namespace LostAndFoundTracker.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
        public DateTime JoinDate { get; set; }
        public List<UserItemViewModel> MyItems { get; set; }
    }

    public class UserItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime Date { get; set; }
    }
}