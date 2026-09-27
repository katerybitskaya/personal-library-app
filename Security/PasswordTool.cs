using Microsoft.AspNetCore.Identity;
using System.Text;

namespace PersonalLibrary.Security
{
    /// <summary>
    /// Console helper: "dotnet PersonalLibrary.dll --hash-password"
    /// asks for a password and prints its hash for protection.json.
    /// </summary>
    public static class PasswordTool
    {
        public const string Argument = "--hash-password";

        public static void Run()
        {
            Console.WriteLine("Personal Library — password hash generator");
            var first = ReadHidden("Password: ");
            if (string.IsNullOrEmpty(first))
            {
                Console.WriteLine("Empty password — nothing to do.");
                return;
            }
            var second = ReadHidden("Repeat password: ");
            if (first != second)
            {
                Console.WriteLine("Passwords do not match.");
                Environment.ExitCode = 1;
                return;
            }

            var hash = new PasswordHasher<object>().HashPassword(new object(), first);
            Console.WriteLine();
            Console.WriteLine("Copy this value into \"PasswordHash\" in protection.json:");
            Console.WriteLine(hash);
        }

        private static string ReadHidden(string prompt)
        {
            Console.Write(prompt);
            if (Console.IsInputRedirected)
                return Console.ReadLine() ?? "";

            var sb = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
                    continue;
                }
                if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
            Console.WriteLine();
            return sb.ToString();
        }
    }
}
