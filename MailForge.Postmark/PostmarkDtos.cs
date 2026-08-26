using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MailForge.Postmark
{
    /// <summary>The request payload sent to the Postmark /email endpoint.</summary>
    internal sealed class PostmarkSendRequest
    {
        [JsonPropertyName("From")]
        public string? From { get; set; }

        [JsonPropertyName("To")]
        public string? To { get; set; }

        [JsonPropertyName("Cc")]
        public string? Cc { get; set; }

        [JsonPropertyName("Bcc")]
        public string? Bcc { get; set; }

        [JsonPropertyName("Subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("HtmlBody")]
        public string? HtmlBody { get; set; }

        [JsonPropertyName("TextBody")]
        public string? TextBody { get; set; }

        [JsonPropertyName("Attachments")]
        public PostmarkAttachmentDto[]? Attachments { get; set; }

        [JsonPropertyName("Headers")]
        public PostmarkHeaderDto[]? Headers { get; set; }

        [JsonPropertyName("Metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonPropertyName("MessageStream")]
        public string? MessageStream { get; set; }

        [JsonPropertyName("TrackOpens")]
        public bool? TrackOpens { get; set; }

        [JsonPropertyName("TrackLinks")]
        public string? TrackLinks { get; set; }
    }

    /// <summary>An attachment encoded for the Postmark API.</summary>
    internal sealed class PostmarkAttachmentDto
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Content")]
        public string? Content { get; set; }

        [JsonPropertyName("ContentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("ContentID")]
        public string? ContentID { get; set; }
    }

    /// <summary>A custom header sent to the Postmark API.</summary>
    internal sealed class PostmarkHeaderDto
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Value")]
        public string? Value { get; set; }
    }

    /// <summary>The response returned by the Postmark /email endpoint on success.</summary>
    internal sealed class PostmarkSendResponse
    {
        [JsonPropertyName("To")]
        public string? To { get; set; }

        [JsonPropertyName("SubmittedAt")]
        public string? SubmittedAt { get; set; }

        [JsonPropertyName("MessageID")]
        public string? MessageID { get; set; }

        [JsonPropertyName("ErrorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("Message")]
        public string? Message { get; set; }
    }

    /// <summary>The error payload returned by the Postmark API on failure.</summary>
    internal sealed class PostmarkErrorResponse
    {
        [JsonPropertyName("ErrorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("Message")]
        public string? Message { get; set; }
    }
}
