using System;

namespace LostAndFoundTracker.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        // Who earned the certificate
        public int UserId { get; set; }

        // Certificate Type: "Bronze", "Silver", "Gold"
        public string CertificateType { get; set; } = string.Empty;

        // Stars required for this certificate
        public int StarsRequired { get; set; }

        // Stars earned at the time
        public int StarsEarned { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.Now;

        // Unique certificate code
        public string CertificateCode { get; set; } = string.Empty;

        // Navigation property
        public User? User { get; set; }
    }
}