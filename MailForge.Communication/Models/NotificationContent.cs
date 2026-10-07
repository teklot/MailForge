namespace MailForge.Communication.Models
{
    /// <summary>
    /// The channel-specific payload carried by a <see cref="Notification"/>. Each channel
    /// contributes a concrete subclass describing how it delivers (for example
    /// <see cref="EmailContent"/> for the email channel).
    /// </summary>
    public abstract class NotificationContent
    {
    }
}