namespace MailForge.Communication.Models
{
    /// <summary>Indicates the importance level of a notification.</summary>
    public enum NotificationPriority
    {
        /// <summary>Low importance.</summary>
        Low = 0,

        /// <summary>Normal importance (the default).</summary>
        Normal = 1,

        /// <summary>High importance.</summary>
        High = 2,

        /// <summary>Urgent, typically routed for immediate delivery.</summary>
        Urgent = 3
    }
}