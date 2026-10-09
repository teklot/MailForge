using System;
using System.Collections.Generic;

namespace MailForge.Telegram
{
    /// <summary>An inline keyboard appended to a Telegram message.</summary>
    public sealed class TelegramKeyboard
    {
        /// <summary>The keyboard rows; each row holds its buttons left to right.</summary>
        public IReadOnlyList<IReadOnlyList<TelegramKeyboardButton>> Rows { get; }

        /// <summary>Creates a single-row keyboard from the given buttons.</summary>
        /// <param name="buttons">The buttons in the row.</param>
        public TelegramKeyboard(params TelegramKeyboardButton[] buttons)
            : this(new[] { (IReadOnlyList<TelegramKeyboardButton>)(buttons ?? throw new ArgumentNullException(nameof(buttons))) })
        {
        }

        /// <summary>Creates a keyboard from rows of buttons.</summary>
        /// <param name="rows">The keyboard rows.</param>
        public TelegramKeyboard(IReadOnlyList<IReadOnlyList<TelegramKeyboardButton>> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (rows.Count == 0)
                throw new ArgumentException("A keyboard requires at least one row.", nameof(rows));

            var copy = new List<IReadOnlyList<TelegramKeyboardButton>>(rows.Count);
            foreach (var row in rows)
            {
                if (row == null)
                    throw new ArgumentException("Keyboard rows cannot be null.", nameof(rows));
                if (row.Count == 0)
                    throw new ArgumentException("Keyboard rows cannot be empty.", nameof(rows));
                foreach (var button in row)
                {
                    if (button == null)
                        throw new ArgumentException("Keyboard buttons cannot be null.", nameof(rows));
                }
                copy.Add(row);
            }

            Rows = copy;
        }
    }
}
