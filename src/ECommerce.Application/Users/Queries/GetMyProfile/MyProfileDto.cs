namespace ECommerce.Application.Users.Queries.GetMyProfile;

public sealed class MyProfileDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}