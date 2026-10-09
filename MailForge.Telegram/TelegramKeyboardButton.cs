using System;

namespace MailForge.Telegram
{
    /// <summary>A single button in a Telegram inline keyboard.</summary>
    public sealed class TelegramKeyboardButton
    {
        /// <summary>The button label.</summary>
        public string Text { get; }

        /// <summary>The URL opened when the button is tapped (null for callback buttons).</summary>
        public string? Url { get; }

        /// <summary>The callback payload delivered on tap (null for URL buttons).</summary>
        public string? CallbackData { get; }

        private TelegramKeyboardButton(string text, string? url, string? callbackData)
        {
            Text = text;
            Url = url;
            CallbackData = callbackData;
        }

        /// <summary>Creates a button that opens a URL.</summary>
        /// <param name="text">The button label.</param>
        /// <param name="url">The URL to open.</param>
        public static TelegramKeyboardButton WithUrl(string text, string url)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("A button label is required.", nameof(text));
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("A button URL is required.", nameof(url));
            return new TelegramKeyboardButton(text, url, null);
        }

        /// <summary>Creates a button that delivers callback data to the application.</summary>
        /// <param name="text">The button label.</param>
        /// <param name="callbackData">The callback payload (1-64 bytes).</param>
        public static TelegramKeyboardButton WithCallbackData(string text, string callbackData)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("A button label is required.", nameof(text));
            if (string.IsNullOrWhiteSpace(callbackData))
                throw new ArgumentException("Callback data is required.", nameof(callbackData));
            if (System.Text.Encoding.UTF8.GetByteCount(callbackData) > 64)
                throw new ArgumentException("Callback data cannot exceed 64 bytes.", nameof(callbackData));
            return new TelegramKeyboardButton(text, null, callbackData);
        }
    }
}
