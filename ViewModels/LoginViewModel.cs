namespace PersonalLibrary.ViewModels
{
    public class LoginViewModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool RememberMe { get; set; } = true;
        public string? ReturnUrl { get; set; }
        public string? Error { get; set; }
    }
}
