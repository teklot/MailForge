using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MailForge.Brevo
{
    /// <summary>The request payload sent to the Brevo v3/smtp/email endpoint.</summary>
    internal sealed class BrevoSendRequest
    {
        [JsonPropertyName("sender")]
        public BrevoSenderDto? Sender { get; set; }

        [JsonPropertyName("to")]
        public BrevoRecipientDto[]? To { get; set; }

        [JsonPropertyName("cc")]
        public BrevoRecipientDto[]? Cc { get; set; }

        [JsonPropertyName("bcc")]
        public BrevoRecipientDto[]? Bcc { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("htmlContent")]
        public string? HtmlContent { get; set; }

        [JsonPropertyName("textContent")]
        public string? TextContent { get; set; }

        [JsonPropertyName("attachment")]
        public BrevoAttachmentDto[]? Attachment { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
    }

    /// <summary>A sender address encoded for the Brevo API.</summary>
    internal sealed class BrevoSenderDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    /// <summary>A recipient address encoded for the Brevo API.</summary>
    internal sealed class BrevoRecipientDto
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>An attachment encoded for the Brevo API.</summary>
    internal sealed class BrevoAttachmentDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    /// <summary>The response returned by the Brevo v3/smtp/email endpoint on success.</summary>
    internal sealed class BrevoSendResponse
    {
        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }
    }

    /// <summary>The error payload returned by the Brevo API on failure.</summary>
    internal sealed class BrevoErrorResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
