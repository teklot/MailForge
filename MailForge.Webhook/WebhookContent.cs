using System;
using MailForge.Communication.Models;

namespace MailForge.Webhook
{
    /// <summary>
    /// The Webhook channel payload: a raw request body (typically JSON) posted verbatim to the
    /// configured endpoint, plus the content type used for the request.
    /// </summary>
    public sealed class WebhookContent : NotificationContent
    {
        /// <summary>The request body sent to the endpoint.</summary>
        public string Payload { get; }

        /// <summary>The request content type (defaults to <c>application/json</c>).</summary>
        public string ContentType { get; }

        /// <summary>Creates a Webhook payload.</summary>
        /// <param name="payload">The request body posted to the endpoint.</param>
        /// <param name="contentType">The request content type; defaults to <c>application/json</c>.</param>
        public WebhookContent(string payload, string contentType = "application/json")
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (payload.Length == 0)
                throw new ArgumentException("Payload cannot be empty.", nameof(payload));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be null or empty.", nameof(contentType));

            Payload = payload;
            ContentType = contentType;
        }
    }
}
