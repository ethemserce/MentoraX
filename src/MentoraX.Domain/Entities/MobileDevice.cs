namespace MentoraX.Domain.Entities;

public sealed class MobileDevice : BaseEntity
{
    public Guid UserId { get; set; }

    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}