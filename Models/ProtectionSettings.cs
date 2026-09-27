namespace PersonalLibrary.Models
{
    /// <summary>
    /// Password protection settings, read from protection.json (section "Protection").
    /// The file is optional: without it protection is disabled.
    /// Changes in the file are applied without restarting the application.
    /// </summary>
    public class ProtectionSettings
    {
        public const string SectionName = "Protection";

        /// <summary>true — the whole site requires login; false — open access.</summary>
        public bool Enabled { get; set; }

        public string Username { get; set; } = "";

        /// <summary>Password hash generated with: dotnet PersonalLibrary.dll --hash-password</summary>
        public string PasswordHash { get; set; } = "";

        /// <summary>Plain-text password (used only when PasswordHash is empty). Not recommended.</summary>
        public string Password { get; set; } = "";

        /// <summary>Failed attempts from one IP before a temporary lockout.</summary>
        public int MaxFailedAttempts { get; set; } = 5;

        /// <summary>Failed attempts from all IPs together before a temporary lockout of the login form.</summary>
        public int GlobalMaxFailedAttempts { get; set; } = 20;

        /// <summary>Lockout duration in minutes.</summary>
        public int LockoutMinutes { get; set; } = 15;

        /// <summary>How long the login session lasts (days, sliding).</summary>
        public int SessionDays { get; set; } = 30;

        public bool HasCredentials =>
            !string.IsNullOrWhiteSpace(Username) &&
            (!string.IsNullOrWhiteSpace(PasswordHash) || !string.IsNullOrEmpty(Password));
    }
}
