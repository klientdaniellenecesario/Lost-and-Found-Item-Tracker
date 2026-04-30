using System;

namespace LostAndFoundTracker.Models
{
    public class StarTransaction
    {
        public int Id { get; set; }

        // Who received the stars (the finder)
        public int ReceiverId { get; set; }

        // Who gave the stars (the owner)
        public int GiverId { get; set; }

        // Which item this transaction is for
        public int ItemId { get; set; }

        // Number of stars given (1-5)
        public int StarsGiven { get; set; }

        // Thank you message from owner
        public string ThankYouMessage { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User? Receiver { get; set; }
        public User? Giver { get; set; }
        public Item? Item { get; set; }
    }
}