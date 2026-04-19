namespace LostAndFoundTracker.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }  // nullable because optional
    }
}