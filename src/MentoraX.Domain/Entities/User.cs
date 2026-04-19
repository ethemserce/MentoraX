namespace MentoraX.Domain.Entities;

public sealed class User : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string TimeZone { get; private set; } = "Europe/Istanbul";
    public bool IsActive { get; private set; } = true;

    private readonly List<LearningMaterial> _materials = [];
    private readonly List<StudyPlan> _studyPlans = [];

    public IReadOnlyCollection<LearningMaterial> Materials => _materials;
    public IReadOnlyCollection<StudyPlan> StudyPlans => _studyPlans;
    public ICollection<StudyProgress> StudyProgresses { get; set; } = new List<StudyProgress>();
    public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
    public ICollection<MobileDevice> MobileDevices { get; set; } = new List<MobileDevice>();
    private User() { }

    public User(string fullName, string email, string passwordHash, string timeZone = "Europe/Istanbul")
    {
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        TimeZone = timeZone;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        Touch();
    }
}
