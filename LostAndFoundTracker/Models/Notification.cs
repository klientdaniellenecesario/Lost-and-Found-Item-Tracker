using System;

namespace LostAndFoundTracker.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // Who is receiving the notification
        public int ReceiverId { get; set; }

        // Who triggered the notification (the finder or owner)
        public int SenderId { get; set; }

        // Which item this is about
        public int ItemId { get; set; }

        // Type: "FoundMatch" (someone found your lost item) or "Claim" (owner claims found item)
        public string NotificationType { get; set; } = string.Empty;

        // Message content from the sender
        public string Message { get; set; } = string.Empty;

        // Status: "Unread", "Read"
        public string Status { get; set; } = "Unread";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public User? Receiver { get; set; }
        public User? Sender { get; set; }
        public Item? Item { get; set; }
    }
}