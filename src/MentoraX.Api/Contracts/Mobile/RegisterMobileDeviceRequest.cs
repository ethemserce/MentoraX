namespace MentoraX.Api.Contracts.Mobile;

public sealed class RegisterMobileDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}