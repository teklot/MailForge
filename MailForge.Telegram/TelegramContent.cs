using System;
using System.Collections.Generic;
using MailForge.Communication.Models;

namespace MailForge.Telegram
{
    /// <summary>
    /// The Telegram channel payload: one or more chat ids, the message text (also used as
    /// the media caption when <see cref="Media"/> is set), an optional parse mode, and an
    /// optional inline keyboard.
    /// </summary>
    public sealed class TelegramContent : NotificationContent
    {
        /// <summary>The chats that receive this message.</summary>
        public IReadOnlyList<string> ChatIds { get; }

        /// <summary>The message text; sent as the caption when <see cref="Media"/> is set.</summary>
        public string Text { get; }

        /// <summary>The formatting mode applied to <see cref="Text"/>.</summary>
        public TelegramParseMode ParseMode { get; }

        /// <summary>Optional photo or document attached to the message.</summary>
        public TelegramMedia? Media { get; }

        /// <summary>Optional inline keyboard appended to the message.</summary>
        public TelegramKeyboard? Keyboard { get; }

        /// <summary>Creates a Telegram payload for a single chat.</summary>
        /// <param name="chatId">The target chat id or @channelusername.</param>
        /// <param name="text">The message text.</param>
        /// <param name="parseMode">The formatting mode applied to the text.</param>
        /// <param name="media">Optional photo or document.</param>
        /// <param name="keyboard">Optional inline keyboard.</param>
        public TelegramContent(
            string chatId,
            string text,
            TelegramParseMode parseMode = TelegramParseMode.None,
            TelegramMedia? media = null,
            TelegramKeyboard? keyboard = null)
            : this(new[] { chatId }, text, parseMode, media, keyboard)
        {
        }

        /// <summary>Creates a Telegram payload for several chats.</summary>
        /// <param name="chatIds">The target chat ids or @channelusernames.</param>
        /// <param name="text">The message text.</param>
        /// <param name="parseMode">The formatting mode applied to the text.</param>
        /// <param name="media">Optional photo or document.</param>
        /// <param name="keyboard">Optional inline keyboard.</param>
        public TelegramContent(
            IReadOnlyList<string> chatIds,
            string text,
            TelegramParseMode parseMode = TelegramParseMode.None,
            TelegramMedia? media = null,
            TelegramKeyboard? keyboard = null)
        {
            if (chatIds == null)
                throw new ArgumentNullException(nameof(chatIds));
            if (chatIds.Count == 0)
                throw new ArgumentException("At least one chat id is required.", nameof(chatIds));
            foreach (var chatId in chatIds)
            {
                if (string.IsNullOrWhiteSpace(chatId))
                    throw new ArgumentException("Chat ids cannot be null or empty.", nameof(chatIds));
            }
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (text.Length == 0)
                throw new ArgumentException("Message text cannot be empty.", nameof(text));

            ChatIds = new List<string>(chatIds);
            Text = text;
            ParseMode = parseMode;
            Media = media;
            Keyboard = keyboard;
        }
    }
}
