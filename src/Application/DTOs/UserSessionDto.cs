namespace Behsazan.Application.DTOs;

public class UserSessionDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    public List<string> Permissions { get; set; } = new();
}
