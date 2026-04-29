namespace LostAndFoundTracker.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}