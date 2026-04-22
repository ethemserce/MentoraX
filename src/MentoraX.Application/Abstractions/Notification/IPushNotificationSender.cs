using System;
using System.Collections.Generic;
using System.Text;

namespace MentoraX.Application.Abstractions.Notification
{
    public interface IPushNotificationSender
    {
        Task SendAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default);
    }
}
