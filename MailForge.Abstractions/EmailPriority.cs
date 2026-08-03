namespace MailForge.Abstractions
{
    /// <summary>Indicates the importance level of an email message.</summary>
    public enum EmailPriority
    {
        /// <summary>Low importance.</summary>
        Low = 0,

        /// <summary>Normal importance (the default).</summary>
        Normal = 1,

        /// <summary>High importance.</summary>
        High = 2
    }
}
