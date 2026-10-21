namespace PersonalLibrary.Models
{
    public class AppUser
    {
        public const string AdminId = "admin";

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Login { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string Color { get; set; } = UserColors.Default;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? PasswordChangedAt { get; set; }
    }

    public class ProfileLogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string UserId { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class UsersData
    {
        public List<AppUser> Users { get; set; } = new();
        public List<ProfileLogEntry> ProfileLog { get; set; } = new();
        public DateTime? AdminLogSeenAt { get; set; }
    }

    public static class UserColors
    {
        public const string Default = "rose";

        public static readonly (string Key, string Hex)[] Palette =
        {
            ("rose",   "#e8697f"),
            ("coral",  "#f08a5d"),
            ("amber",  "#e8b84a"),
            ("lime",   "#a8d35f"),
            ("mint",   "#5fd3a8"),
            ("teal",   "#4fbcd1"),
            ("sky",    "#6f9ff0"),
            ("violet", "#a387f0"),
            ("orchid", "#d47fe0"),
            ("silver", "#b8c0cc"),
        };

        public static bool IsValid(string? key) => Palette.Any(p => p.Key == key);

        public static string Hex(string? key) =>
            Palette.FirstOrDefault(p => p.Key == key).Hex ?? "#8a97a8";
    }
}
