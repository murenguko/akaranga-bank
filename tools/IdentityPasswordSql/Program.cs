using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

if (args.Length != 1 || !MailAddress.TryCreate(args[0], out var address) ||
    !string.Equals(address.Address, args[0], StringComparison.Ordinal))
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/IdentityPasswordSql -- user@example.com");
    return 1;
}

static string ReadPassword(string prompt)
{
    Console.Error.Write(prompt);
    var password = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
            break;
        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
                password.Length--;
            continue;
        }
        if (!char.IsControl(key.KeyChar))
            password.Append(key.KeyChar);
    }
    Console.Error.WriteLine();
    return password.ToString();
}

var password = ReadPassword("New password: ");
var confirmation = ReadPassword("Confirm password: ");
if (password != confirmation || password.Length < 8 ||
    !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
    !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit))
{
    Console.Error.WriteLine("Passwords must match and meet the app's length, uppercase, lowercase, digit, and symbol requirements.");
    return 1;
}

var hash = new PasswordHasher<object>().HashPassword(new object(), password);
var normalizedEmail = args[0].ToUpperInvariant().Replace("'", "''", StringComparison.Ordinal);
var securityStamp = Guid.NewGuid().ToString("N");
Console.WriteLine($$"""
UPDATE public."Utilisateurs"
SET "PasswordHash" = '{{hash}}',
    "EstActif" = TRUE,
    "SecurityStamp" = '{{securityStamp}}',
    "AccessFailedCount" = 0,
    "LockoutEnd" = NULL
WHERE "NormalizedEmail" = '{{normalizedEmail}}'
RETURNING "Id", "Email", "EstActif";
""");
return 0;
