using System.Text.Json.Serialization;

namespace MailForge.ZeptoMail
{
    /// <summary>The request payload sent to the ZeptoMail v1.1/email endpoint.</summary>
    internal sealed class ZeptoMailSendRequest
    {
        [JsonPropertyName("from")]
        public ZeptoMailAddressDto? From { get; set; }

        [JsonPropertyName("to")]
        public ZeptoMailRecipientDto[]? To { get; set; }

        [JsonPropertyName("cc")]
        public ZeptoMailRecipientDto[]? Cc { get; set; }

        [JsonPropertyName("bcc")]
        public ZeptoMailRecipientDto[]? Bcc { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("htmlbody")]
        public string? HtmlBody { get; set; }

        [JsonPropertyName("textbody")]
        public string? TextBody { get; set; }

        [JsonPropertyName("attachments")]
        public ZeptoMailAttachmentDto[]? Attachments { get; set; }

        [JsonPropertyName("bounce_address")]
        public string? BounceAddress { get; set; }
    }

    /// <summary>An address in the ZeptoMail API format.</summary>
    internal sealed class ZeptoMailAddressDto
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>A recipient in the ZeptoMail API format with nested email_address structure.</summary>
    internal sealed class ZeptoMailRecipientDto
    {
        [JsonPropertyName("email_address")]
        public ZeptoMailAddressDto? EmailAddress { get; set; }
    }

    /// <summary>An attachment encoded for the ZeptoMail API.</summary>
    internal sealed class ZeptoMailAttachmentDto
    {
        [JsonPropertyName("file")]
        public string? FileContent { get; set; }

        [JsonPropertyName("name")]
        public string? FileName { get; set; }

        [JsonPropertyName("mime_type")]
        public string? MimeType { get; set; }
    }

    /// <summary>The response returned by the ZeptoMail API on success.</summary>
    internal sealed class ZeptoMailSendResponse
    {
        [JsonPropertyName("data")]
        public ZeptoMailSendResponseData[]? Data { get; set; }
    }

    /// <summary>A single data entry in the ZeptoMail success response.</summary>
    internal sealed class ZeptoMailSendResponseData
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>The error payload returned by the ZeptoMail API on failure.</summary>
    internal sealed class ZeptoMailErrorResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
