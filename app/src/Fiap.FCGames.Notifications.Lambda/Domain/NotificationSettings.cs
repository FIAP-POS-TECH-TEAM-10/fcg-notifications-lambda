namespace Fiap.FCGames.Notifications.Lambda.Domain
{
    public class NotificationSettings
    {
        public string SmsProviderUrl { get; set; } = string.Empty;
        public int MaxRetryAttempts { get; set; }
        public bool EnableEmailNotifications { get; set; }
    }
}
