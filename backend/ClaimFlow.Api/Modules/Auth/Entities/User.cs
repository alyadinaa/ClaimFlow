namespace ClaimFlow.Api.Modules.Auth.Entities;

public class User
{
    private User()
    {
    }

    public User(string email, string displayName, string passwordHash, UserRole role)
    {
        Email = email.ToLowerInvariant();
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Role = role;
    }

    public int Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }
}
