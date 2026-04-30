namespace LostAndFoundTracker.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

        // Star Reward System Properties
        public int TotalStarPoints { get; set; } = 0;
        public int TotalCertificatesEarned { get; set; } = 0;
        public int BronzeCertificates { get; set; } = 0;
        public int SilverCertificates { get; set; } = 0;
        public int GoldCertificates { get; set; } = 0;
    }
}