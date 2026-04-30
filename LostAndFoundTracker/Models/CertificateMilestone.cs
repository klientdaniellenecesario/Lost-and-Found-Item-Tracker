namespace LostAndFoundTracker.Models
{
    public static class CertificateMilestone
    {
        public const int BronzeStars = 20;
        public const int SilverStars = 50;
        public const int GoldStars = 100;

        public static string GetCertificateType(int stars)
        {
            if (stars >= GoldStars) return "Gold";
            if (stars >= SilverStars) return "Silver";
            if (stars >= BronzeStars) return "Bronze";
            return "None";
        }

        public static int GetNextMilestone(int currentStars)
        {
            if (currentStars < BronzeStars) return BronzeStars;
            if (currentStars < SilverStars) return SilverStars;
            if (currentStars < GoldStars) return GoldStars;
            return GoldStars; // Already at max
        }

        public static int GetProgressToNextMilestone(int currentStars)
        {
            int next = GetNextMilestone(currentStars);
            if (next <= currentStars) return 100;

            int previous = GetPreviousMilestone(currentStars);
            int range = next - previous;
            int progress = currentStars - previous;

            return (progress * 100) / range;
        }

        private static int GetPreviousMilestone(int stars)
        {
            if (stars >= GoldStars) return GoldStars;
            if (stars >= SilverStars) return SilverStars;
            if (stars >= BronzeStars) return BronzeStars;
            return 0;
        }
    }
}