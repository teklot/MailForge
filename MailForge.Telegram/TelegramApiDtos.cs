using System.Collections.Generic;

namespace MailForge.Telegram
{
    internal sealed class TelegramTextRequest
    {
        public string ChatId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? ParseMode { get; set; }
        public TelegramKeyboardDto? ReplyMarkup { get; set; }
    }

    internal sealed class TelegramMediaRequest
    {
        public string ChatId { get; set; } = string.Empty;
        public string? Photo { get; set; }
        public string? Document { get; set; }
        public string Caption { get; set; } = string.Empty;
        public string? ParseMode { get; set; }
        public TelegramKeyboardDto? ReplyMarkup { get; set; }
    }

    internal sealed class TelegramKeyboardDto
    {
        public List<List<TelegramKeyboardButtonDto>> InlineKeyboard { get; set; } = new List<List<TelegramKeyboardButtonDto>>();
    }

    internal sealed class TelegramKeyboardButtonDto
    {
        public string Text { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? CallbackData { get; set; }
    }

    internal sealed class TelegramApiResponse
    {
        public bool Ok { get; set; }
        public TelegramMessageDto? Result { get; set; }
        public string? Description { get; set; }
        public int ErrorCode { get; set; }
        public TelegramResponseParametersDto? Parameters { get; set; }
    }

    internal sealed class TelegramMessageDto
    {
        public int MessageId { get; set; }
    }

    internal sealed class TelegramResponseParametersDto
    {
        public int RetryAfter { get; set; }
    }
}
