namespace MailForge.Telegram
{
    /// <summary>The formatting mode applied to a Telegram message.</summary>
    public enum TelegramParseMode
    {
        /// <summary>Plain text without markup parsing.</summary>
        None = 0,

        /// <summary>Telegram HTML entities (bold, italic, links, code, and so on).</summary>
        Html = 1,

        /// <summary>Telegram MarkdownV2 entities.</summary>
        MarkdownV2 = 2
    }
}
