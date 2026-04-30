using Microsoft.EntityFrameworkCore;
using LostAndFoundTracker.Models;

namespace LostAndFoundTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<StarTransaction> StarTransactions { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relationship: one User can have many Items
            modelBuilder.Entity<Item>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId);

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Receiver)
                .WithMany()
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Sender)
                .WithMany()
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Item)
                .WithMany()
                .HasForeignKey(n => n.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // StarTransaction relationships
            modelBuilder.Entity<StarTransaction>()
                .HasOne(s => s.Receiver)
                .WithMany()
                .HasForeignKey(s => s.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StarTransaction>()
                .HasOne(s => s.Giver)
                .WithMany()
                .HasForeignKey(s => s.GiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StarTransaction>()
                .HasOne(s => s.Item)
                .WithMany()
                .HasForeignKey(s => s.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Certificate relationships
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}