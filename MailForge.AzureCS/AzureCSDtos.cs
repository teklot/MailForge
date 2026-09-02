using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MailForge.AzureCS
{
    /// <summary>The request payload sent to the Azure /emails:send endpoint.</summary>
    internal sealed class AzureCSSendRequest
    {
        [JsonPropertyName("senderAddress")]
        public string? SenderAddress { get; set; }

        [JsonPropertyName("content")]
        public AzureCSContentDto? Content { get; set; }

        [JsonPropertyName("recipients")]
        public AzureCSRecipientsDto? Recipients { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("replyTo")]
        public AzureCSAddressDto[]? ReplyTo { get; set; }

        [JsonPropertyName("userEngagementTrackingDisabled")]
        public bool UserEngagementTrackingDisabled { get; set; }

        [JsonPropertyName("attachments")]
        public AzureCSAttachmentDto[]? Attachments { get; set; }
    }

    /// <summary>The message content block.</summary>
    internal sealed class AzureCSContentDto
    {
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("plainText")]
        public string? PlainText { get; set; }

        [JsonPropertyName("html")]
        public string? Html { get; set; }
    }

    /// <summary>The recipient groupings.</summary>
    internal sealed class AzureCSRecipientsDto
    {
        [JsonPropertyName("to")]
        public AzureCSAddressDto[]? To { get; set; }

        [JsonPropertyName("cc")]
        public AzureCSAddressDto[]? Cc { get; set; }

        [JsonPropertyName("bcc")]
        public AzureCSAddressDto[]? Bcc { get; set; }
    }

    /// <summary>An email address with an optional display name.</summary>
    internal sealed class AzureCSAddressDto
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }

    /// <summary>An attachment encoded for the Azure API.</summary>
    internal sealed class AzureCSAttachmentDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("contentInBase64")]
        public string? ContentInBase64 { get; set; }
    }

    /// <summary>The response returned by the Azure endpoint on success (HTTP 202).</summary>
    internal sealed class AzureCSSendResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>The error payload returned by the Azure API on failure.</summary>
    internal sealed class AzureCSErrorResponse
    {
        [JsonPropertyName("error")]
        public AzureCSErrorDto? Error { get; set; }
    }

    /// <summary>The detail of an error.</summary>
    internal sealed class AzureCSErrorDto
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}