namespace MentoraX.Api.Contracts.Auth;

public sealed record RegisterRequest(string FullName, string Email, string Password, string? TimeZone);
