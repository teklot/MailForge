using System;

namespace MailForge.Telegram
{
    /// <summary>Configuration for the Telegram provider.</summary>
    public sealed class TelegramOptions
    {
        /// <summary>Initializes a new instance of the <see cref="TelegramOptions"/> class.</summary>
        public TelegramOptions() { }

        /// <summary>The Telegram bot token issued by BotFather.</summary>
        public string? BotToken { get; set; }

        /// <summary>The Telegram Bot API base address (defaults to https://api.telegram.org/).</summary>
        public Uri BaseUri { get; set; } = new Uri("https://api.telegram.org/");

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
