using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MailForge.Resend
{
    /// <summary>The request payload sent to the Resend /emails endpoint.</summary>
    internal sealed class ResendSendRequest
    {
        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("to")]
        public string[] To { get; set; }

        [JsonPropertyName("cc")]
        public string[] Cc { get; set; }

        [JsonPropertyName("bcc")]
        public string[] Bcc { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("html")]
        public string Html { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("attachments")]
        public ResendAttachmentDto[] Attachments { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonPropertyName("tags")]
        public ResendTagDto[] Tags { get; set; }
    }

    /// <summary>An attachment encoded for the Resend API.</summary>
    internal sealed class ResendAttachmentDto
    {
        [JsonPropertyName("filename")]
        public string FileName { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }
    }

    /// <summary>A tag sent to the Resend API.</summary>
    internal sealed class ResendTagDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    /// <summary>The response returned by the Resend /emails endpoint on success.</summary>
    internal sealed class ResendSendResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    /// <summary>The error payload returned by the Resend API on failure.</summary>
    internal sealed class ResendErrorResponse
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
