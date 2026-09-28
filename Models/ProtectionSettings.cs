namespace PersonalLibrary.Models
{
    public class ProtectionSettings
    {
        public const string SectionName = "Protection";

        public bool Enabled { get; set; }

        public string Username { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public string Password { get; set; } = "";

        public int MaxFailedAttempts { get; set; } = 5;

        public int GlobalMaxFailedAttempts { get; set; } = 20;

        public int LockoutMinutes { get; set; } = 15;

        public int SessionDays { get; set; } = 30;

        public bool HasCredentials =>
            !string.IsNullOrWhiteSpace(Username) &&
            (!string.IsNullOrWhiteSpace(PasswordHash) || !string.IsNullOrEmpty(Password));
    }
}
